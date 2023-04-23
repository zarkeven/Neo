﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using SharpDX;
using WoWEditor6.IO;
using WoWEditor6.IO.Files.Sky;
using WoWEditor6.IO.Files.Terrain;
using WoWEditor6.UI;

namespace WoWEditor6.Scene.Terrain
{
    class MapManager
    {
        private const int MapRadius = 2;

        private Vector2 mEntryPoint;
        private int mTotalLoadSteps;
        private int mLoadStepsDone;
        private readonly List<MapArea> mDataToLoad = new List<MapArea>();
        private readonly List<MapArea> mLoadedData = new List<MapArea>();
        private readonly List<MapAreaRender> mUnloadList = new List<MapAreaRender>();
        private readonly Dictionary<int, MapAreaRender> mAreas = new Dictionary<int, MapAreaRender>();
        private Thread mLoadThread;
        private Thread mUnloadThread;
        private Thread mLightUpdateThread;
        private bool mIsRunning;
        private readonly MapLowManager mAreaLowManager = new MapLowManager();
        private readonly List<int> mCurrentValidLinks = new List<int>();

        public string Continent { get; private set; }
        public WdtFile CurrentWdt { get; private set; }
        public bool HasNewBlend { get; private set; }
        public bool IsInitialLoad { get; private set; }
        public SkySphere SkySphere { get; private set; }
        public bool HideTerrain { get; set; } = false;

        public void Initialize()
        {
            SkySphere = new SkySphere(999.0f, 25, 25, WorldFrame.Instance.GraphicsContext);
            mIsRunning = true;
            mLoadThread = new Thread(LoadProc);
            mLoadThread.Start();
            mLightUpdateThread = new Thread(LightUpdateProc);
            mLightUpdateThread.Start();
            mUnloadThread = new Thread(UnloadProc);
            mUnloadThread.Start();
        }

        public MapAreaRender GetAreaByIndex(int ix, int iy)
        {
            var index = iy * 0xFF + ix;
            lock (mAreas)
                return mAreas.ContainsKey(index) ? mAreas[index] : null;
        }

        public MapAreaRender GetAreaByPosition(Vector3 position)
        {
            lock(mAreas)
                return mAreas.Values.FirstOrDefault(x => x.AreaFile.BoundingBox.Contains(position) == ContainmentType.Contains);
        }

        public void OnEditTerrain(Editing.TerrainChangeParameters parameters)
        {
            // ReSharper disable once InconsistentlySynchronizedField
            foreach (var pair in mAreas)
                pair.Value.OnTerrainChange(parameters);

            if(parameters.AlignModels)
            {
                foreach (var pair in mAreas)
                    pair.Value.OnUpdateModelPositions(parameters);
            }
        }

        public void OnTextureTerrain(Editing.TextureChangeParameters parameters)
        {
            foreach (var pair in mAreas)
                pair.Value.OnTextureChange(parameters);
        }

        public void OnSaveAllFiles()
        {
            foreach (var pair in mAreas)
                pair.Value.AreaFile.Save();
        }

        public void Shutdown()
        {
            mIsRunning = false;
            mLoadThread.Join();
            mLightUpdateThread.Join();
            mUnloadThread.Join();
            mAreaLowManager.Shutdown();
        }

        public void OnFrame(Camera camera)
        {
            ProcessLoadedTiles();

            if (WorldFrame.Instance.State == AppState.World)
            {
                SkyManager.Instance.SyncUpdate();
                SkySphere.Render();
                mAreaLowManager.OnFrame();
            }

            if(!HideTerrain)
            {
                MapChunkRender.ChunkMesh.BeginDraw();
                MapChunkRender.ChunkMesh.Program.SetPixelSampler(0, MapChunkRender.ColorSampler);
                MapChunkRender.ChunkMesh.Program.SetPixelSampler(1, MapChunkRender.AlphaSampler);

                if (WorldFrame.Instance.LastMouseIntersection.ChunkHit != null)
                {
                    EditorWindowController.GetInstance().OnUpdateChunkIndex(WorldFrame.Instance.LastMouseIntersection.ChunkHit.IndexX, WorldFrame.Instance.LastMouseIntersection.ChunkHit.IndexY);
                }
                else
                {
                    EditorWindowController.GetInstance().OnUpdateChunkIndex(0, 0);
                }
                
                foreach (var pair in mAreas)
                    pair.Value.OnFrame();
            }           
        }

        public void EnterWorld(Vector2 entryPoint, int mapId)
        {
            var row = Storage.DbcStorage.Map.GetRowById(mapId);
            if (row == null)
                return;

            var continent = row.GetString(Storage.MapFormatGuess.FieldMapName);

            MapChunkRender.InitIndices();
            WorldFrame.Instance.LeftHandedCamera = FileManager.Instance.Version > FileDataVersion.Cataclysm;

            mEntryPoint = entryPoint;
            Continent = continent;
            WorldFrame.Instance.State = AppState.LoadingScreen;

            CurrentWdt = new WdtFile();
            CurrentWdt.Load(continent);
            HasNewBlend = (CurrentWdt.Flags & 0x84) != 0;

            MapChunkRender.ChunkMesh.Program = HasNewBlend ? MapChunkRender.BlendNew : MapChunkRender.BlendOld;

            IsInitialLoad = true;

            SkyManager.Instance.OnEnterWorld(mapId);
            mAreaLowManager.OnEnterWorld(Continent, ref entryPoint);
            LoadInitial();
        }

        public void OnLoadProgress()
        {
            Interlocked.Increment(ref mLoadStepsDone);
            var pct = (float) mLoadStepsDone / mTotalLoadSteps;
            if (IsInitialLoad)
                EditorWindowController.GetInstance().LoadingScreen.UpdateProgress(pct);

            if (mLoadStepsDone < mTotalLoadSteps || !IsInitialLoad) return;

            IsInitialLoad = false;
            OnInitialLoadDone();
        }

        public void UpdatePosition(Vector3 position, bool updateTerrain)
        {
            WorldFrame.Instance.UpdatePosition(position);
            SkySphere.UpdatePosition(position);

            if(updateTerrain)
            {
                SkyManager.Instance.UpdatePosition(position);
                var pos2D = new Vector2(position.X, position.Y);
                mAreaLowManager.UpdatePosition(ref pos2D);
                UpdateVisibility(ref position);
            }

            EditorWindowController.GetInstance().OnUpdatePosition(position);

            var x = position.X;
            var y = position.Y;

            if (FileManager.Instance.Version <= FileDataVersion.Warlords)
                y = 64.0f * Metrics.TileSize - y;

            var tilex = (int)Math.Floor(x / Metrics.TileSize);
            var tiley = (int)Math.Floor(y / Metrics.TileSize);

            EditorWindowController.GetInstance().OnUpdateTileIndex(tilex, tiley);

        }

        public bool GetLandHeight(float x, float y, out float z)
        {
            z = 0.0f;

            var tilex = (int) Math.Floor(x / Metrics.TileSize);
            var tiley = (int) Math.Floor(y / Metrics.TileSize);

            x -= tilex * Metrics.TileSize;
            y -= tiley * Metrics.TileSize;

            var index = tilex + tiley * 0xFF;
            MapAreaRender tile;
            lock(mAreas)
                mAreas.TryGetValue(index, out tile);

            if (tile == null)
                return false;

            var chunkx = (int) Math.Floor(x / Metrics.ChunkSize);
            var chunky = (int) Math.Floor(y / Metrics.ChunkSize);

            if (chunkx < 0 || chunky < 0 || chunkx > 15 || chunky > 15)
                return false;

            var chunk = tile.AreaFile.GetChunk(chunkx + chunky * 16);
            if (chunk == null)
                return false;

            x -= chunkx * Metrics.ChunkSize;
            y -= chunky * Metrics.ChunkSize;

            var row = (int)(y / (Metrics.UnitSize * 0.5f) + 0.5f);
            var col = (int)((x - Metrics.UnitSize * 0.5f * (row % 2)) / Metrics.UnitSize + 0.5f);

            if (row < 0 || col < 0 || row > 16 || col > (((row % 2) != 0) ? 8 : 9))
                return false;

            z = chunk.Vertices[17 * (row / 2) + (((row % 2) != 0) ? 9 : 0) + col].Position.Z;
            return true;
        }

        public bool ToggleWireframe()
        {
            MapChunkRender.WireFrame = !MapChunkRender.WireFrame;
            return MapChunkRender.WireFrame;
        }

        public void Intersect(IntersectionParams parameters)
        {
            var ray = Picking.Build(ref parameters.ScreenPosition,
                ref parameters.InverseView, ref parameters.InverseProjection);

            var hasHit = false;
            var minDist = float.MaxValue;
            MapChunk chunkHit = null;

            // ReSharper disable once InconsistentlySynchronizedField
            foreach(var pair in mAreas)
            {
                MapChunk chunk;
                float distance;
                if (!pair.Value.AreaFile.Intersect(ref ray, out chunk, out distance)) continue;

                hasHit = true;
                if ((distance >= minDist)) continue;

                minDist = distance;
                chunkHit = chunk;
            }

            parameters.TerrainHit = hasHit;
            if (hasHit)
            {
                parameters.TerrainPosition = ray.Position + minDist * ray.Direction;
                parameters.TerrainDistance = minDist;
            }
            else
                parameters.TerrainPosition = new Vector3(float.MaxValue);

            parameters.ChunkHit = chunkHit;
        }

        private void LoadInitial()
        {
            var ix = (int) Math.Floor(mEntryPoint.X / Metrics.TileSize);
            var iy = (int) Math.Floor((64.0f * Metrics.TileSize - mEntryPoint.Y) / Metrics.TileSize);

            mTotalLoadSteps = 0;
            mLoadStepsDone = 0;

            lock(mDataToLoad)
            {
                mDataToLoad.Clear();
                mLoadedData.Clear();
                mAreas.Clear();

                for (var x = ix - MapRadius; x <= ix + MapRadius; ++x)
                {
                    for (var y = iy - MapRadius; y <= iy + MapRadius; ++y)
                    {
                        if (x < 0 || y < 0 || x > 63 || y > 63)
                            continue;

                        if (FileManager.Instance.Provider.Exists(string.Format(@"World\Maps\{0}\{0}_{1}_{2}.adt", Continent, x, y)) == false)
                            continue;

                        var tile = AdtFactory.Instance.CreateArea(Continent, x, y);
                        mDataToLoad.Add(tile);
                        mTotalLoadSteps += 2 * 256;
                        mCurrentValidLinks.Add(x + y * 64);
                    }
                }
            }
        }

        private void OnInitialLoadDone()
        {
            float height;
            if (GetLandHeight(mEntryPoint.X, 64.0f * Metrics.TileSize - mEntryPoint.Y, out height))
            {
                height += 50.0f;
                SkyManager.Instance.UpdatePosition(new Vector3(mEntryPoint, height));

                var entryPoint = new Vector3(mEntryPoint, height);
                if (FileManager.Instance.Version > FileDataVersion.Mists)
                    entryPoint.Y = 64.0f * Metrics.TileSize - mEntryPoint.Y;

                SkyManager.Instance.AsyncUpdate();
                EditorWindowController.GetInstance().OnEnterWorld();
                WorldFrame.Instance.OnEnterWorld(entryPoint);
                WorldFrame.Instance.Dispatcher.BeginInvoke(
                    () =>
                    {
                        SkySphere.UpdatePosition(new Vector3(mEntryPoint, height));
                        SkyManager.Instance.SyncUpdate();
                        WorldFrame.Instance.CamControl.ForceUpdate(WorldFrame.Instance.ActiveCamera.Position);
                        WorldFrame.Instance.M2Manager.ViewChanged();
                    });
            }

            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        private void LoadProc()
        {
            while(mIsRunning)
            {
                MapArea loadTile = null;
                lock(mDataToLoad)
                {
                    if (mDataToLoad.Count > 0)
                    {
                        loadTile = mDataToLoad[0];
                        mDataToLoad.RemoveAt(0);
                    }
                }

                if (loadTile != null)
                {
                    loadTile.AsyncLoad();
                    lock (mLoadedData)
                        mLoadedData.Add(loadTile);
                }
                else
                    Thread.Sleep(30);
            }
        }

        private void ProcessLoadedTiles()
        {
            MapArea data = null;
            var index = 0;
            lock(mLoadedData)
            {
                if (mLoadedData.Count > 0)
                {
                    data = mLoadedData[0];
                    mLoadedData.RemoveAt(0);
                    index = data.IndexX + data.IndexY * 0xFF;
                    if (mAreas.ContainsKey(index))
                    {
                        data.Dispose();
                        return;
                    }
                }
            }

            if (data == null)
                return;

            var tile = new MapAreaRender(data.IndexX, data.IndexY);
            tile.AsyncLoaded(data);
            mAreas.Add(index, tile);
        }

        private void LightUpdateProc()
        {
            while(mIsRunning)
            {
                if (SkyManager.Instance != null)
                    SkyManager.Instance.AsyncUpdate();

                Thread.Sleep(100);
            }
        }

        private void UpdateVisibility(ref Vector3 position)
        {
            var cx = position.X;
            var cy = position.Y;
            if (FileManager.Instance.Version < FileDataVersion.Lichking)
                cy = 64.0f * Metrics.TileSize - cy;

            var ix = (int) Math.Floor(cx / Metrics.TileSize);
            var iy = (int) Math.Floor(cy / Metrics.TileSize);

            var countPref = mCurrentValidLinks.Count;
            mCurrentValidLinks.RemoveAll(index =>
            {
                var x = index % 64;
                var y = index / 64;
                return (x > ix + 2 || x < ix - 2 || y > iy + 2 || y < iy - 2);
            });

            if (countPref == mCurrentValidLinks.Count)
                return;

            mCurrentValidLinks.Clear();
            for (var x = ix - MapRadius; x <= ix + MapRadius; ++x)
            {
                for (var y = iy - MapRadius; y <= iy + MapRadius; ++y)
                {
                    if (x < 0 || y < 0 || x > 63 || y > 63)
                        continue;

                    mCurrentValidLinks.Add(y * 64 + x);
                }
            }

            var loadMask = new List<int>();
            var invalidList = new List<MapAreaRender>();
            // ReSharper disable once InconsistentlySynchronizedField
            foreach(var pair in mAreas)
            {
                var tile = pair.Value;
                var index = tile.IndexX + tile.IndexY * 64;
                var indexX = tile.IndexX;
                var indexY = tile.IndexY;
                if (indexX < ix - 2 || indexX > ix + 2 || indexY < iy - 2 || indexY > iy + 2)
                {
                    invalidList.Add(tile);
                    continue;
                }

                loadMask.Add(index);
            }

            lock(mUnloadList)
            {
                foreach(var tile in invalidList)
                {
                    mUnloadList.Add(tile);
                    mAreas.Remove(tile.IndexX + tile.IndexY * 0xFF);
                }
            }

            invalidList.Clear();

            lock(mDataToLoad)
            {
                loadMask.AddRange(mDataToLoad.Select(tile => tile.IndexX + tile.IndexY * 64));
            }

            lock(mLoadedData)
            {
                loadMask.AddRange(mLoadedData.Select(tile => tile.IndexX + tile.IndexY * 64));
            }

            foreach(var link in mCurrentValidLinks)
            {
                if (loadMask.Contains(link))
                    continue;

                var x = link % 64;
                var y = link / 64;

                var area = AdtFactory.Instance.CreateArea(Continent, x, y);
                lock (mDataToLoad)
                    mDataToLoad.Add(area);
            }
        }

        private void UnloadProc()
        {
            while(mIsRunning)
            {
                lock(mUnloadList)
                {
                    foreach (var tile in mUnloadList)
                        tile.Dispose();

                    mUnloadList.Clear();
                }
                Thread.Sleep(500);
            }
        }
    }
}
