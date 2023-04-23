﻿using System;
using SharpDX;
using WoWEditor6.Scene;
using WoWEditor6.UI;
using WoWEditor6.Utils;
using System.Windows.Forms;
using Point = System.Drawing.Point;
using WintabDN;
using System.Windows;

namespace WoWEditor6.Editing
{
    class EditManager
    {
        public static EditManager Instance { get; private set; }

        private DateTime mLastChange = DateTime.Now;

        private Point mLastCursorPosition = Cursor.Position;

        private float mInnerRadius = 18.0f;
        private float mOuterRadius = 20.0f;
        private float mIntensity = 32.0f;
        private float mAmount = 32.0f;
        private float mOpacity = 255.0f;
        private float mPenSensivity;
        private float mAmplitude;
        private float mInnerAmplitude;
        private float mSprayParticleSize;
        private float mSprayParticleAmount;
        private float mSprayParticleHardness;

        private bool mIsTabletOn = false;
        private bool mIsTablet_RChange = false;
        private bool mIsTablet_IRChange = false;
        private bool mIsTablet_PChange = false;
        private bool mIsSprayOn = false;
        private bool mIsSpraySolidInnerRadius = false;

        private TerrainChangeType mChangeType;

        public TerrainChangeType ChangeType
        {
            get { return mChangeType; }
        }

        public float InnerRadius
        {
            get { return mInnerRadius; }
            set { HandleInnerRadiusChanged(value); }
        }

        public float OuterRadius
        {
            get { return mOuterRadius; }
            set { HandleOuterRadiusChanged(value); }
        }

        public float Intensity
        {
            get { return mIntensity; }
            set { HandleIntensityChanged(value); }
        }

        public float Amount
        {
            get { return mAmount; }
            set { HandleAmountChanged(value); }
        }

        public float Opacity
        {
            get { return mOpacity; }
            set { HandleOpacityChanged(value); }

        }

        public float PenSensivity
        {
            get { return mPenSensivity; }
            set { HandlePenSensivityChanged(value);  }
        }

        public bool IsTabletOn
        {
            get { return mIsTabletOn; }
            set { HandleTabletControlChanged(value);  }
        }

        public bool IsSprayOn
        {
            get { return mIsSprayOn;  }
            set { HandleSprayModeChanged(value);  }
        }
        
        public bool IsTablet_RChange
        {
            get { return mIsTablet_RChange; }
            set { HandleTabletRadiusChanged(value); }
        }

        public bool IsTablet_IRChange
        {
            get { return mIsTablet_IRChange; }
            set { HandleTabletInnerRadiusChanged(value); }
        }

        public bool IsTablet_PChange 
        {
            get { return mIsTablet_PChange; }
            set { HandleTabletControlPressureChanged(value); }
        }

        public float Amplitude
        {
            get { return mAmplitude;  }
            set { HandleAllowedAmplitudeChanged(value); }
        }

        public float InnerAmplitude
        {
            get { return mInnerAmplitude; }
            set { HandleAllowedInnerAmplitudeChanged(value); }
        }

        public float SprayParticleSize
        {
            get { return mSprayParticleSize;  }
            set { HandleParticleSizeChanged(value);  }
        }

        public float SprayParticleAmount
        {
            get { return mSprayParticleAmount; }
            set { HandleParticleAmountChanged(value); }
        }

        public float SprayParticleHarndess
        {
            get { return mSprayParticleHardness;  }
            set { HandleParticleHardnessChanged(value);  }
        }

        public bool IsSpraySolidInnerRadius
        {
            get { return mIsSpraySolidInnerRadius; }
            set { HandleSpraySolidInnerRadiusChanged(value);  }
        }

        public bool IsTexturing { get { return (CurrentMode & EditMode.Texturing) != 0; } }

        public Vector3 MousePosition { get; set; }
        public bool IsTerrainHovered { get; set; }

        public EditMode CurrentMode { get; private set; }

        static EditManager()
        {
            Instance = new EditManager();
        }


        public void UpdateChanges()
        {
            ModelSpawnManager.Instance.OnUpdate();
            ModelEditManager.Instance.Update();

            var diff = DateTime.Now - mLastChange;
            if (diff.TotalMilliseconds < (IsTexturing ? 40 : 20))
                return;

            mLastChange = DateTime.Now;
            if ((CurrentMode & EditMode.Sculpting) != 0)
                TerrainChangeManager.Instance.OnChange(diff);
            else if ((CurrentMode & EditMode.Texturing) != 0)
                TextureChangeManager.Instance.OnChange(diff);
            else if ((CurrentMode & EditMode.Chunk) != 0)
                ChunkEditManager.Instance.OnChange(diff);

            var keyState = new byte[256];
            UnsafeNativeMethods.GetKeyboardState(keyState);
            var altDown = KeyHelper.IsKeyDown(keyState, Keys.Menu);
            var LMBDown = KeyHelper.IsKeyDown(keyState, Keys.LButton);
            var RMBDown = KeyHelper.IsKeyDown(keyState, Keys.RButton);
            var spaceDown = KeyHelper.IsKeyDown(keyState, Keys.Space);
            var MMBDown = KeyHelper.IsKeyDown(keyState, Keys.MButton);
            var tDown = KeyHelper.IsKeyDown(keyState, Keys.T);

            var curPos = Cursor.Position;
            var amount = -(mLastCursorPosition.X - curPos.X) / 32.0f;

            if (mIsTabletOn) // All tablet control editing is here.
            {
                if (mIsTablet_PChange)
                {
                    if (EditorWindowController.GetInstance().TexturingModel != null)
                    {
                        mAmount = TabletManager.Instance.TabletPressure * mPenSensivity / 10.0f;
                        HandleAmountChanged(mAmount);
                    }

                    if (EditorWindowController.GetInstance().TerrainManager != null)
                    {
                        mIntensity = TabletManager.Instance.TabletPressure * mPenSensivity / 10.0f;
                        HandleIntensityChanged(mIntensity);
                    }
                }

                if (mIsTablet_RChange) // If outer radius change is enabled.
                {
                    if (EditorWindowController.GetInstance().TexturingModel != null)
                    {
                        
                        mOuterRadius = Math.Max( TabletManager.Instance.TabletPressure * (mAmplitude / 10.0f), 0.1f );

                        mInnerRadius = Math.Min(mInnerRadius, mAmplitude);

                        mInnerRadius = Math.Min(mInnerRadius, mOuterRadius);


                        HandleOuterRadiusChanged(mOuterRadius);
                        HandleInnerRadiusChanged(mInnerRadius);
                    }
                        
                }

                if (mIsTablet_IRChange) // If inner radius change is enabled.
                {
                    if (EditorWindowController.GetInstance().TexturingModel != null)
                    {
                        mInnerRadius = Math.Max( TabletManager.Instance.TabletPressure * (mInnerAmplitude / 10.0f), 0.1f);

                        mInnerRadius = Math.Min(mInnerRadius, mOuterRadius);


                        HandleInnerRadiusChanged(mInnerRadius);

                    }
                }
            }

            
            /*if (!LMBDown && IsTabletOn) // When tablet mode is on we always set those to minimal value (NEEDS TO BE MOVED OUT OF HERE).
            {
                mAmount = 1.0f;
                mIntensity = 1.0f;
            }*/
                 

            if (curPos != mLastCursorPosition)
            { 
                if (altDown && RMBDown)
                {
                    mInnerRadius += amount;

                    mInnerRadius = Math.Max(mInnerRadius, 0.1f);
                    mInnerRadius = Math.Min(mInnerRadius, 200.0f);
                    mInnerRadius = Math.Min(mInnerRadius, mOuterRadius);

                    HandleInnerRadiusChanged(mInnerRadius);

                }
                

                if (altDown && LMBDown)
                {
                    mInnerRadius += amount;
                    mOuterRadius += amount;

                    mInnerRadius = Math.Max(mInnerRadius, 0.1f);
                    mInnerRadius = Math.Min(mInnerRadius, 200.0f);

                    mOuterRadius = Math.Max(mOuterRadius, 0.1f);
                    mOuterRadius = Math.Min(mOuterRadius, 200.0f);

                    mInnerRadius = Math.Min(mInnerRadius, mOuterRadius);

                    HandleInnerRadiusChanged(mInnerRadius);
                    HandleOuterRadiusChanged(mOuterRadius);
                    

                }

                if(spaceDown && LMBDown)
                {
                    mIntensity += amount;
                    mAmount += amount;

                    if (EditorWindowController.GetInstance().TerrainManager != null)
                    {

                        mIntensity = Math.Max(mIntensity, 1.0f);
                        mIntensity = Math.Min(mIntensity, 40.0f);

                        HandleIntensityChanged(mIntensity);
                    }

                    if (EditorWindowController.GetInstance().TexturingModel != null)
                    {
                        mAmount = Math.Max(mAmount, 1.0f);
                        mAmount = Math.Min(mAmount, 40.0f);

                        HandleAmountChanged(mAmount);
                    }
                                     
                }

                if(altDown && MMBDown)
                {
                    mOpacity += amount;

                    if (EditorWindowController.GetInstance().TexturingModel != null)
                    {

                        mOpacity = Math.Max(mOpacity, 0.0f);
                        mOpacity = Math.Min(mOpacity, 255.0f);

                        HandleOpacityChanged(mOpacity);
                    }
                }

                if(spaceDown && MMBDown)
                {
                    mPenSensivity += amount / 32.0f;

                    if(EditorWindowController.GetInstance().TexturingModel != null)
                    {
                        mPenSensivity = Math.Max(mPenSensivity, 0.1f);
                        mPenSensivity = Math.Min(mPenSensivity, 1.0f);

                        HandlePenSensivityChanged(mPenSensivity);
                    }
                }

                if (spaceDown && tDown) // DOES NOT WORK PROPERLY. NEEDS TO BE MOVED OUT OF THIS METHOD.
                {
                    if (mIsTabletOn)
                    {
                        mIsTabletOn = false;
                    }
                    else
                    {
                        mIsTabletOn = true;
                    }
                    HandleTabletControlChanged(mIsTabletOn);
                }

                mLastCursorPosition = Cursor.Position;

            }


        }

        public void EnableShading()
        {
            CurrentMode |= EditMode.Sculpting;
            CurrentMode &= ~EditMode.Texturing;
            CurrentMode &= ~EditMode.Chunk;
            mChangeType = TerrainChangeType.Shading;

        }

        public void EnableSculpting()
        {
            CurrentMode |= EditMode.Sculpting;
            CurrentMode &= ~EditMode.Texturing;
            CurrentMode &= ~EditMode.Chunk;
            mChangeType = TerrainChangeType.Elevate;
        }

        public void DisableSculpting()
        {
            CurrentMode &= ~EditMode.Sculpting;
        }

        public void EnableTexturing()
        {
            CurrentMode |= EditMode.Texturing;
            CurrentMode &= ~EditMode.Sculpting;
            CurrentMode &= ~EditMode.Chunk;
        }

        public void DisableTexturing()
        {
            CurrentMode &= ~EditMode.Texturing;
        }

        public void EnableChunkEditing()
        {
            CurrentMode = EditMode.Chunk;
        }

        public void DisableChunkEditing()
        {
            CurrentMode &= ~EditMode.Chunk;
        }

        private void HandleInnerRadiusChanged(float value)
        {
            mInnerRadius = value;
            WorldFrame.Instance.UpdateBrush(mInnerRadius, mOuterRadius);
            if (EditorWindowController.GetInstance().TexturingModel != null)
                EditorWindowController.GetInstance().TexturingModel.HandleInnerRadiusChanged(value);

            if (EditorWindowController.GetInstance().TerrainManager != null)
                EditorWindowController.GetInstance().TerrainManager.HandleInnerRadiusChanged(value);

            if (EditorWindowController.GetInstance().ShadingModel != null)
                EditorWindowController.GetInstance().ShadingModel.HandleInnerRadiusChanged(value);
        }

        private void HandleOuterRadiusChanged(float value)
        {
            mOuterRadius = value;
            WorldFrame.Instance.UpdateBrush(mInnerRadius, mOuterRadius);
            if (EditorWindowController.GetInstance().TexturingModel != null)
                EditorWindowController.GetInstance().TexturingModel.HandleOuterRadiusChanged(value);

            if (EditorWindowController.GetInstance().TerrainManager != null)
                EditorWindowController.GetInstance().TerrainManager.HandleOuterRadiusChanged(value);

            if (EditorWindowController.GetInstance().ShadingModel != null)
                EditorWindowController.GetInstance().ShadingModel.HandleOuterRadiusChanged(value);
        }

        private void HandleIntensityChanged(float value)
        {
            mIntensity = value;
            if (EditorWindowController.GetInstance().TerrainManager != null)
                EditorWindowController.GetInstance().TerrainManager.HandleIntensityChanged(value);

            if (EditorWindowController.GetInstance().ShadingModel != null)
                EditorWindowController.GetInstance().ShadingModel.HandleIntensityChanged(value);

        }

        private void HandleAmountChanged(float value)
        {
            mAmount = value;
            if (EditorWindowController.GetInstance().TexturingModel != null)
                EditorWindowController.GetInstance().TexturingModel.HandleAmoutChanged(value);
        }

        private void HandleOpacityChanged(float value)
        {
            mOpacity = value;
            if (EditorWindowController.GetInstance().TexturingModel != null)
                EditorWindowController.GetInstance().TexturingModel.HandleOpacityChanged(value);
        }

        private void HandlePenSensivityChanged(float value)
        {
            mPenSensivity = value;
            if (EditorWindowController.GetInstance().TexturingModel != null)
                EditorWindowController.GetInstance().TexturingModel.HandlePenSensivityChanged(value);
            if (EditorWindowController.GetInstance().TerrainManager != null)
                EditorWindowController.GetInstance().TerrainManager.HandlePenSensivityChanged(value);
        }

        private void HandleTabletControlChanged(bool value)
        {
            mIsTabletOn = value;
            if (EditorWindowController.GetInstance().TexturingModel != null)
                EditorWindowController.GetInstance().TexturingModel.HandleTabletControlChanged(value);
            if (EditorWindowController.GetInstance().TerrainManager != null)
                EditorWindowController.GetInstance().TerrainManager.HandleTabletControlChanged(value);
            if (EditorWindowController.GetInstance().ShadingModel != null)
                EditorWindowController.GetInstance().ShadingModel.HandleTabletControlChanged(value);
        }

        private void HandleTabletRadiusChanged(bool value)
        {
            mIsTablet_RChange = value;
            if (EditorWindowController.GetInstance().TexturingModel != null)
                EditorWindowController.GetInstance().TexturingModel.HandleTabletChangeRadiusChanged(value);
        }

        private void HandleTabletInnerRadiusChanged(bool value)
        {
            mIsTablet_IRChange = value;
            if (EditorWindowController.GetInstance().TexturingModel != null)
                EditorWindowController.GetInstance().TexturingModel.HandleTabletChangeInnerRadiusChanged(value);
        }

        private void HandleAllowedAmplitudeChanged(float value)
        {
            mAmplitude = value;
            if (EditorWindowController.GetInstance().TexturingModel != null)
                EditorWindowController.GetInstance().TexturingModel.HandleAllowedAmplitudeChanged(value);
        }

        private void HandleAllowedInnerAmplitudeChanged(float value)
        {
            mInnerAmplitude = value;
            if (EditorWindowController.GetInstance().TexturingModel != null)
                EditorWindowController.GetInstance().TexturingModel.HandleAllowedInnerAmplitudeChanged(value);
        }

        private void HandleTabletControlPressureChanged(bool value)
        {
            mIsTablet_PChange = value;
            if (EditorWindowController.GetInstance().TexturingModel != null)
                EditorWindowController.GetInstance().TexturingModel.HandleTabletControlPressureChanged(value);
        }

        private void HandleSprayModeChanged(bool value)
        {
            mIsSprayOn = value;
            if (EditorWindowController.GetInstance().TexturingModel != null)
                EditorWindowController.GetInstance().TexturingModel.HandleSprayModeChanged(value);
        }

        private void HandleParticleSizeChanged(float value)
        {
            mSprayParticleSize = value;
            if (EditorWindowController.GetInstance().TexturingModel != null)
                EditorWindowController.GetInstance().TexturingModel.HandleParticleSizeChanged(value);
        }

        private void HandleParticleAmountChanged(float value)
        {
            mSprayParticleAmount = value;
            if (EditorWindowController.GetInstance().TexturingModel != null)
                EditorWindowController.GetInstance().TexturingModel.HandleParticleAmountChanged(value);
        }

        private void HandleParticleHardnessChanged(float value)
        {
            mSprayParticleHardness = value;
            if (EditorWindowController.GetInstance().TexturingModel != null)
                EditorWindowController.GetInstance().TexturingModel.HandleParticleHardnessChanged(value);
        }

        private void HandleSpraySolidInnerRadiusChanged(bool value)
        {
            mIsSpraySolidInnerRadius = value;
            if (EditorWindowController.GetInstance().TexturingModel != null)
                EditorWindowController.GetInstance().TexturingModel.HandleSpraySolidInnerRadiusChanged(value);
        }
    }
}
