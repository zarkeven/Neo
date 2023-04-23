﻿using WoWEditor6.UI.Dialogs;
using System.Windows.Forms;
using SharpDX;

namespace WoWEditor6.UI.Models
{
    class SculptingViewModel
    {
        private readonly TerrainSettingsWidget mWidget;
        private bool mIsValueChangedSurpressed;

        public TerrainSettingsWidget Widget { get { return mWidget; } }

        public SculptingViewModel(TerrainSettingsWidget widget)
        {
            if (EditorWindowController.GetInstance() != null)
                EditorWindowController.GetInstance().TerrainManager = this;

            mWidget = widget;
        }

        #region HandleSliders
        public void HandleIntensitySlider(float value)
        {
            mIsValueChangedSurpressed = true;
            Editing.TerrainChangeManager.Instance.Amount = value;
            mIsValueChangedSurpressed = false;
        }

        public void HandleInnerRadiusSlider(float value)
        {
            mIsValueChangedSurpressed = true;
            Editing.EditManager.Instance.InnerRadius = value;
            mIsValueChangedSurpressed = false;
        }

        public void HandleOuterRadiusSlider(float value)
        {
            mIsValueChangedSurpressed = true;
            Editing.EditManager.Instance.OuterRadius = value;
            mIsValueChangedSurpressed = false;
        }

        public void HandleShadingMultiplier(Vector3 value)
        {
            mIsValueChangedSurpressed = true;
            Editing.TerrainChangeManager.Instance.ShadingMultiplier = value;
            mIsValueChangedSurpressed = false;
        }

        public void HandleType(Editing.TerrainChangeType value)
        {
            mIsValueChangedSurpressed = true;
            Editing.TerrainChangeManager.Instance.ChangeType = value;
            mIsValueChangedSurpressed = false;
        }

        public void HandleAlgorithm(Editing.TerrainAlgorithm value)
        {
            mIsValueChangedSurpressed = true;
            Editing.TerrainChangeManager.Instance.ChangeAlgorithm = value;
            mIsValueChangedSurpressed = false;
        }

        public void HandleAlignToGround(bool value)
        {
            mIsValueChangedSurpressed = true;
            Editing.TerrainChangeManager.Instance.AlignModelsToGround = value;
            mIsValueChangedSurpressed = false;
        }

        public void HandlePenSensivity(float value)
        {
            mIsValueChangedSurpressed = true;
            Editing.EditManager.Instance.PenSensivity = value;
            mIsValueChangedSurpressed = false;
        }

        public void HandleTabletControl(bool value)
        {
            mIsValueChangedSurpressed = true;
            Editing.EditManager.Instance.IsTabletOn = value;
            mIsValueChangedSurpressed = false;
        }
        #endregion

        #region HandleChange
        public void HandleInnerRadiusChanged(float newRadius)
        {
            if (mIsValueChangedSurpressed)
                return;

            mWidget.InnerRadiusSlider.Value = newRadius;
        }

        public void HandleOuterRadiusChanged(float newRadius)
        {
            if (mIsValueChangedSurpressed)
                return;

            mWidget.OuterRadiusSlider.Value = newRadius;
        }

        public void HandleIntensityChanged(float newAmount)
        {
            if (mIsValueChangedSurpressed)
                return;

            mWidget.IntensitySlider.Value = newAmount;
        }

        public void HandlePenSensivityChanged(float newSensivity)
        {
            if (mIsValueChangedSurpressed)
                return;

            mWidget.Tablet_SensivitySlider.Value = newSensivity;
        }

        public void HandleTabletControlChanged(bool newIsTabletOn)
        {
            if (mIsValueChangedSurpressed)
                return;

            mWidget.TabletControlBox.IsChecked = newIsTabletOn;
        }

        public void HandleTypeChanged(Editing.TerrainChangeType value)
        {
            if (mIsValueChangedSurpressed)
                return;

            switch(value)
            {
                case Editing.TerrainChangeType.Blur:
                    mWidget.BlurRadio.IsChecked = true;
                    break;
                case Editing.TerrainChangeType.Elevate:
                    mWidget.RaiseRadio.IsChecked = true;
                    break;
                case Editing.TerrainChangeType.Flatten:
                    mWidget.FlattenRadio.IsChecked = true;
                    break;
                case Editing.TerrainChangeType.Shading:
                    mWidget.ShadingRadio.IsChecked = true;
                    break;
            }
        }

        public void HandleAlgorithmChanged(Editing.TerrainAlgorithm value)
        {
            if (mIsValueChangedSurpressed)
                return;

            switch (value)
            {
                case Editing.TerrainAlgorithm.Flat:
                    mWidget.FlatRadio.IsChecked = true;
                    break;
                case Editing.TerrainAlgorithm.Linear:
                    mWidget.LinearRadio.IsChecked = true;
                    break;
                case Editing.TerrainAlgorithm.Quadratic:
                    mWidget.QuadraticRadio.IsChecked = true;
                    break;
                case Editing.TerrainAlgorithm.Trigonometric:
                    mWidget.TrigonometricRadio.IsChecked = true;
                    break;
            }
        }

        public void HandleAlignToGroundChanged(bool value)
        {
            if (mIsValueChangedSurpressed)
                return;

            mWidget.AlignModelsBox.IsChecked = value;
        }
        #endregion

        public void SwitchToSculpting()
        {
            Editing.EditManager.Instance.EnableSculpting();
        }
    }
}
