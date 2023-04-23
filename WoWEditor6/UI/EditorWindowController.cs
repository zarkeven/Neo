using SharpDX;
using System.Windows;
using System.Windows.Threading;
using WoWEditor6.UI.Components;
using WoWEditor6.UI.Models;

namespace WoWEditor6.UI
{
    internal class EditorWindowController
    {
        public static EditorWindowController GetInstance()
        {
            return Instance;
        }

        private static void SetInstance(EditorWindowController value)
        {
            Instance = value;
        }

        private readonly EditorWindow mWindow;

        public LoadingScreenControl LoadingScreen { get { return mWindow.LoadingScreenView; } }
        public TexturingViewModel TexturingModel { get; set; }
        public SculptingViewModel TerrainManager { get; set; }
        public IEditingViewModel IEditingModel { get; set; }
        public ShadingViewModel ShadingModel { get; set; }
        public AssetBrowserViewModel AssetBrowserModel { get; set; }
        public ObjectSpawnModel SpawnModel { get; set; }

        public Dispatcher WindowDispatcher { get { return mWindow.Dispatcher; } }

        internal static EditorWindowController Instance { get; set; }

        public EditorWindowController(EditorWindow window)
        {
            SetInstance(this);
            mWindow = window;
        }

        public EditorWindowController(EditorWindow mWindow, TexturingViewModel texturingModel, SculptingViewModel terrainManager, IEditingViewModel iEditingModel, ShadingViewModel shadingModel, AssetBrowserViewModel assetBrowserModel, ObjectSpawnModel spawnModel) : this(mWindow)
        {
            TexturingModel = texturingModel;
            TerrainManager = terrainManager;
            IEditingModel = iEditingModel;
            ShadingModel = shadingModel;
            AssetBrowserModel = assetBrowserModel;
            SpawnModel = spawnModel;
        }

        public void ShowMapOverview()
        {
            mWindow.SplashDocument.Visibility = Visibility.Collapsed;
            mWindow.LoadingDocument.Visibility = Visibility.Collapsed;
            mWindow.EntrySelectView.Visibility = Visibility.Collapsed;
            mWindow.LoadingScreenView.Visibility = Visibility.Collapsed;
            mWindow.MapOverviewGrid.Visibility = Visibility.Visible;
        }

        public void ShowAssetBrowser()
        {
            mWindow.AssetBrowserDocument.IsSelected = true;
        }

        public void OnEnterWorld()
        {
            mWindow.WelcomeDocument.Close();
        }

        public void OnUpdatePosition(Vector3 position)
        {
            mWindow.OnUpdatePosition(position);
        }

        public void OnUpdate(Vector3 modelPosition, Vector3 namePlatePosition)
        {
            mWindow.OnUpdate(modelPosition, namePlatePosition);
        }

        public void OnUpdateTileIndex(int x, int y)
        {
            mWindow.OnUpdateCurrentAdt(x, y);
        }

        public void OnUpdateChunkIndex(int x, int y)
        {
            mWindow.OnUpdateCurrentChunk(x, y);
        }
    }
}
