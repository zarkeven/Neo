﻿using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Threading;
using Microsoft.Win32;
using SharpDX;
using WoWEditor6.Editing;
using WoWEditor6.Resources;
using WoWEditor6.Scene;
using WoWEditor6.UI.Components;
using WoWEditor6.Win32;
using Color = System.Windows.Media.Color;
using System.Windows.Input;
using WoWEditor6.UI.Dialogs;
using WoWEditor6.UI.Models;
using WoWEditor6.UI.Widget;
using WoWEditor6.UI.Widgets;
using Xceed.Wpf.AvalonDock.Layout;
using System.Linq;

namespace WoWEditor6.UI
{
    /// <summary>
    /// Interaction logic for EditorWindow.xaml
    /// </summary>
    public partial class EditorWindow : ILogSink
    {
        public EditorWindow()
        {
            DataContext = new EditorWindowController(this);
            InitializeComponent();

            PathTextBox.Text = Properties.Settings.Default.DataPath;
        }

        public RenderControl DrawTarget { get { return RenderTarget; } }

        private void MenuSaveItem_Click(object sender, RoutedEventArgs e)
        {
            WorldFrame.Instance.MapManager.OnSaveAllFiles();
        }

        private void ModelRenderTest_Click(object sender, RoutedEventArgs e)
        {
            if (WorldFrame.Instance.State == AppState.FileSystemInit ||
                WorldFrame.Instance.State == AppState.Splash)
                return;

            var mrt = new ModelRenderTest();
            mrt.Show();
        }

        private void Add2DText_Click(object sender, RoutedEventArgs e)
        {
            var worldText = new WorldText
            {
                Position = WorldFrame.Instance.ActiveCamera.Position,
                Text = "This is a 2D text!",
                DrawMode = WorldText.TextDrawMode.TextDraw2D_World
            };

            WorldFrame.Instance.WorldTextManager.AddText(worldText);
        }

        private void Add3DText_Click(object sender, RoutedEventArgs e)
        {
            var worldText = new WorldText
            {
                Position = WorldFrame.Instance.ActiveCamera.Position,
                Text = "This is a 3D text!",
                DrawMode = WorldText.TextDrawMode.TextDraw3D
            };

            WorldFrame.Instance.WorldTextManager.AddText(worldText);
        }

        private void DatabaseConfiguration_Click(object sender, RoutedEventArgs e)
        {
            var window = new Window
            {
                Title = "Database Configuration",
                Height = 345,
                Width = 380,
                ResizeMode = ResizeMode.NoResize,
                Content = new Widgets.DatabaseSettings()
            };
            window.ShowDialog();
        }

        private void CreatureEditor_Click(object sender, RoutedEventArgs e)
        {
            if (Storage.Database.MySqlConnector.Instance.CheckConnection())
            {
                var creatureEditor = new Dialogs.CreatureEditor();
                creatureEditor.ShowDialog();
            }
            else
            {
                MessageBox.Show("Please connect to the database before you use the creature editor");
            }
        }

        private void GameObjectEditor_Click(object sender, RoutedEventArgs e)
        {
            if (Storage.Database.MySqlConnector.Instance.CheckConnection())
            {
                var GameObjectEditor = new Dialogs.GameObjectEditor();
                GameObjectEditor.ShowDialog();
            }
            else
            {
                MessageBox.Show("Please connect to the database before you use the game object editor");
            }
        }

        private void ItemEditor_Click(object sender, RoutedEventArgs e)
        {
            if (Storage.Database.MySqlConnector.Instance.CheckConnection())
            {
                var ItemEditor = new Dialogs.ItemEditor();
                ItemEditor.ShowDialog();
            }
            else
            {
                MessageBox.Show("Please connect to the database before you use the item editor");
            }
        }

        private void ExempleEditor_Click(object sender, RoutedEventArgs e)
        {
            var ExempleEditor = new DbcEditors.Exemple.ExempleEditor();
            ExempleEditor.ShowDialog();
        }

        private void ChrRacesEditor_Click(object sender, RoutedEventArgs e)
        {
            var ChrRacesEditor = new DbcEditors.ChrRaces.ChrRacesEditor();
            ChrRacesEditor.ShowDialog();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            EventHandler method = null;
            method = (s, args) =>
            {
                EditorWindowController.GetInstance().TexturingModel.Widget.SelectedTileWrapPanel.HandleCreated -= method;
                //Dispatcher.BeginInvoke(new Action(() => TexturingPane.ToggleAutoHide()));
            };
            EditorWindowController.GetInstance().TexturingModel.Widget.SelectedTileWrapPanel.HandleCreated += method;

            if (EditorWindowController.GetInstance().IEditingModel != null)
            {
                EditorWindowController.GetInstance().IEditingModel.SwitchWidgets(0);
                IEditingPane.Hide();
            }

            Log.AddSink(this);
        }

        private void Window_Unloaded(object sender, RoutedEventArgs e)
        {
            ThumbnailCache.Write(true);
        }

        public void OnUpdateCurrentAdt(int x, int y)
        {
            CurrentAdtLabel.Content = "ADT: " + x + "/" + y;
        }

        public void OnUpdateCurrentChunk(int x, int y)
        {
            CurrentChunkLabel.Content = "Chunk: " + x + "/" + y;
        }

        public void OnUpdatePosition(Vector3 position)
        {
            CurrentPositionLabel.Content = "Position: " + position;
        }

        public void OnUpdate(Vector3 modelPosition, Vector3 namePlatePosition)
        {
            CurrentModelPositionLabel.Content = "Model Position: " + modelPosition;
            CurrentNamePlatePositionLabel.Content = "NamePlate Position: " + namePlatePosition;
        }

        public void AddMessage(LogLevel logLevel, string title, string message)
        {
            if (Dispatcher.HasShutdownFinished || Dispatcher.HasShutdownStarted)
                return;

            Dispatcher.BeginInvoke(new Action(() =>
            {
                var pr = new Paragraph { Margin = new Thickness(0, 0, 0, 0) };
                var titleRun = new Run(title);
                switch (logLevel)
                {
                    case LogLevel.Debug:
                        titleRun.Foreground = new SolidColorBrush(Color.FromRgb(70, 70, 70));
                        break;

                    case LogLevel.Error:
                        titleRun.Foreground = new SolidColorBrush(Color.FromRgb(180, 30, 20));
                        break;

                    case LogLevel.Fatal:
                        titleRun.Foreground = new SolidColorBrush(Color.FromRgb(220, 50, 40));
                        break;

                    case LogLevel.Warning:
                        titleRun.Foreground = new SolidColorBrush(Color.FromRgb(250, 250, 18));
                        break;
                }

                pr.Inlines.Add(titleRun);
                pr.Inlines.Add(new Run(message));
                LogDocument.Blocks.Add(pr);
                var scroller = LogDocument.Parent as FlowDocumentScrollViewer;
                if (scroller == null)
                    return;

                if (VisualTreeHelper.GetChildrenCount(scroller) == 0)
                    return;

                var child = VisualTreeHelper.GetChild(scroller, 0);
                if (child == null)
                    return;

                var border = VisualTreeHelper.GetChild(child, 0) as Decorator;
                if (border == null)
                    return;

                var scrollView = border.Child as ScrollViewer;
                if (scrollView != null) scrollView.ScrollToBottom();
            }));
        }

        private void BrowseFolderButton_Click(object sender, RoutedEventArgs e)
        {
            var fd = (IFileOpenDialog)
                Activator.CreateInstance(Type.GetTypeFromCLSID(new Guid("{DC1C5A9C-E88A-4DDE-A5A1-60F82A20AEF7}")));

            Fos options;
            fd.GetOptions(out options);
            options |= Fos.FosPickfolders;
            fd.SetOptions(options);


            var wnd = Dispatcher;
            var result = 0;
            wnd.Invoke(new Action(() => result = fd.Show(/*new WindowInteropHelper(this).Handle))*/IntPtr.Zero)));

            if (result != 0)
                return;

            IShellItem item;
            fd.GetResult(out item);
            if (item == null)
                return;

            var ptrOut = IntPtr.Zero;
            try
            {
                item.GetDisplayName(Sigdn.Filesyspath, out ptrOut);
                PathTextBox.Text = Marshal.PtrToStringUni(ptrOut);
            }
            catch (Exception)
            {
                item.GetDisplayName(Sigdn.Normaldisplay, out ptrOut);
                PathTextBox.Text = Marshal.PtrToStringUni(ptrOut);
            }
            finally
            {
                if (ptrOut != IntPtr.Zero)
                    Marshal.FreeCoTaskMem(ptrOut);
            }
        }

        private void LoadButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(PathTextBox.Text))
                return;

            Properties.Settings.Default.DataPath = PathTextBox.Text;
            Properties.Settings.Default.Save();

            SplashDocument.Visibility = Visibility.Collapsed;
            LoadingDocument.Visibility = Visibility.Visible;

            IO.FileManager.Instance.DataPath = PathTextBox.Text;
            IO.FileManager.Instance.LoadComplete += () =>
            {
                Dispatcher.Invoke(() =>
                {
                    LoadingDocument.Visibility = Visibility.Collapsed;
                    InitMaps();
                });
            };

            IO.FileManager.Instance.InitFromPath();
        }

        private void InitMaps()
        {
            MapTileGrid.Children.Clear();

            var filter = MapFilterTextBox.Text;

            var children = new List<StackPanel>();
            for (var i = 0; i < Storage.DbcStorage.Map.NumRows; ++i)
            {
                var row = Storage.DbcStorage.Map.GetRow(i);
                var title = row.GetString(Storage.MapFormatGuess.FieldMapTitle);
                if (string.IsNullOrEmpty(filter) == false && title.ToLowerInvariant().Contains(filter.ToLowerInvariant()) == false)
                    continue;

                var panel = new StackPanel
                {
                    Orientation = Orientation.Vertical,
                    Width = 120,
                    Height = 120,
                    Background = new SolidColorBrush(Color.FromRgb(80, 80, 80)),
                    Margin = new Thickness(5, 5, 0, 0)
                };

                var titleLabel = new TextBlock
                {
                    Text = title,
                    VerticalAlignment = VerticalAlignment.Stretch,
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    FontSize = 16,
                    Foreground = Brushes.White,
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(3, 3, 3, 3)
                };

                panel.Children.Add(titleLabel);
                panel.Tag = false;

                panel.MouseEnter += (sender, args) =>
                {
                    var down = panel.Tag as Boolean?;
                    if (down ?? true)
                        return;

                    panel.Background = new SolidColorBrush(Color.FromRgb(120, 120, 120));

                };

                panel.MouseLeave += (sender, args) =>
                {
                    var down = panel.Tag as Boolean?;
                    if (down ?? true)
                        return;
                    panel.Background = new SolidColorBrush(Color.FromRgb(80, 80, 80));
                };

                panel.MouseDown += (sender, args) =>
                {
                    panel.Background = new SolidColorBrush(Color.FromRgb(60, 60, 60));
                    panel.Tag = true;
                    panel.CaptureMouse();
                };

                panel.MouseUp += (sender, args) =>
                {
                    if ((panel.Tag as Boolean?) ?? false)
                    {
                        EntrySelectView.MapSelected(row.GetInt32(0));
                        var scroll = MapTileGrid.Parent as ScrollViewer;
                        if (scroll == null)
                            return;

                        var g = scroll.Parent as Grid;
                        if (g != null)
                            g.Visibility = Visibility.Collapsed;

                        EntrySelectView.Visibility = Visibility.Visible;
                    }

                    panel.Background =
                        new SolidColorBrush(panel.IsMouseOver ? Color.FromRgb(120, 120, 120) : Color.FromRgb(80, 80, 80));
                    panel.Tag = false;
                    panel.ReleaseMouseCapture();
                };

                children.Add(panel);
            }

            if (MapSortTypeCheckBox.IsChecked ?? false)
                children.Sort((e1, e2) => String.Compare(((TextBlock)e1.Children[0]).Text, ((TextBlock)e2.Children[0]).Text, StringComparison.Ordinal));

            children.ForEach(c => MapTileGrid.Children.Add(c));

            var scroller = MapTileGrid.Parent as ScrollViewer;
            if (scroller == null)
                return;

            var grid = scroller.Parent as Grid;
            if (grid != null)
                grid.Visibility = Visibility.Visible;
        }

        private void RegistryButton_Click(object sender, RoutedEventArgs e)
        {
            var result = LoadFromKey(Registry.CurrentUser) ?? LoadFromKey(Registry.LocalMachine);

            if (result == null)
                return;

            PathTextBox.Text = result;
        }

        private static string LoadFromKey(RegistryKey baseKey)
        {
            var rootKey = IntPtr.Size == 8
                ? "Software\\WoW6432Node\\Blizzard Entertainment\\World of Warcraft"
                : "Software\\Blizzard Entertainment\\World of Warcraft";

            try
            {
                var wowKey = baseKey.OpenSubKey(rootKey);
                if (wowKey == null)
                    return null;
                return wowKey.GetValue("InstallPath") as string;
            }
            catch (Exception)
            {
                return null;
            }
        }

        private void MapTileGrid_Loaded(object sender, RoutedEventArgs e)
        {
            var element = (sender as Grid);
            if (element == null)
                return;

            var parent = element.Parent as Grid;
            if (parent == null)
                return;

            element.Height = parent.ActualHeight;
        }

        private void WelcomePanel_Resize(object sender, SizeChangedEventArgs e) // Causes a crash on idling on 1920x1080 and (?) some other screen resoulutions. Some element resizes to negative value.
        {
            var element = sender as Grid;
            if (element == null)
                return;

            var scroller = MapTileGrid.Parent as ScrollViewer;
            if (scroller == null)
                return;

            var grid = scroller.Parent as Grid;
            if (grid == null)
                return;

            grid.Height = element.ActualHeight;
            grid.RowDefinitions[1].Height = new GridLength(Math.Max(0, element.ActualHeight - grid.RowDefinitions[0].Height.Value));
        }

        private void FilterText_Changed(object sender, TextChangedEventArgs e)
        {
            InitMaps();
        }

        private void MapSortCheck_Click(object sender, RoutedEventArgs e)
        {
            InitMaps();
        }

        private void About_MenuItem_Click(object sender, RoutedEventArgs e)
        {
            var window = new Window
            {
                Title = "About",
                Height = 170,
                Width = 300,
                Content = new Dialogs.AboutBox()
            };
            window.ShowDialog();
        }

        private void MenuItem_Settings_Click(object sender, RoutedEventArgs e)
        {
            var window = new Window
            {
                Title = "About",
                Height = 400,
                Width = 600,
                Content = new Dialogs.Settings()
            };
            window.ShowDialog();
        }

        private void GenerateWdlButton_OnClick(object sender, RoutedEventArgs e)
        {
            var mapManager = WorldFrame.Instance.MapManager;
            if (mapManager.Continent == null)
            {
                MessageBox.Show("You have not entered the world yet. Cannot generate WDL.");
                return;
            }

            if (CheckedMessageBox.Show("Autosave for WDL", Strings.Message_AutosaveOnWdl,
                CheckedMessageBox.TagSaveOnWdlGeneration))
            {
                WorldFrame.Instance.MapManager.OnSaveAllFiles();
            }

            GenerateWdlButton.IsEnabled = false;
            var loadDialog = new WdlLoadingDialog();
            Action<string, float> progressCallback = (action, progress) =>
            {
                loadDialog.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() =>
                {
                    loadDialog.Progress = Math.Max(Math.Min(progress * 100, 100), 0);
                    loadDialog.Action = action;
                }));
            };

            WdlGenerator.Generate(mapManager.Continent, progressCallback,
                () => loadDialog.Dispatcher.BeginInvoke(new Action(() =>
                {
                    GenerateWdlButton.IsEnabled = true;
                    loadDialog.ShouldClose = true;
                    loadDialog.Close();
                })));

            loadDialog.Owner = Window.GetWindow(this);
            loadDialog.ShowDialog();
        }

        private void RaiseLowerClick(object sender, RoutedEventArgs e)
        {

            if ((IEditingPane.IsHidden || IEditingPane.IsAutoHidden) && !IEditingPane.IsFloating)
            {
                IEditingPane.Dock();
            }

            if (EditorWindowController.GetInstance().IEditingModel != null)
                EditorWindowController.GetInstance().IEditingModel.SwitchWidgets(1);

        }

        private void TexturingClick(object sender, RoutedEventArgs e)
        {
            if ((IEditingPane.IsHidden || IEditingPane.IsAutoHidden) && !IEditingPane.IsFloating)
            {
                IEditingPane.Dock();
            }

            if (EditorWindowController.GetInstance().IEditingModel != null)
                EditorWindowController.GetInstance().IEditingModel.SwitchWidgets(3);

        }

        private void ShadingClick(object sender, RoutedEventArgs e)
        {
            if ((IEditingPane.IsHidden || IEditingPane.IsAutoHidden) && !IEditingPane.IsFloating)
            {
                IEditingPane.Dock();
            }

            if (EditorWindowController.GetInstance().IEditingModel != null)
                EditorWindowController.GetInstance().IEditingModel.SwitchWidgets(4);
        }

        private void ModelSpawningClick(object sender, RoutedEventArgs e)
        {
            if ((IEditingPane.IsHidden || IEditingPane.IsAutoHidden) && !IEditingPane.IsFloating)
            {
                IEditingPane.Dock();
            }

            if (EditorWindowController.GetInstance().IEditingModel != null)
                EditorWindowController.GetInstance().IEditingModel.SwitchWidgets(5);
        }

        private void ChunkEditingClick(object sender, RoutedEventArgs e)
        {
            if ((IEditingPane.IsHidden || IEditingPane.IsAutoHidden) && !IEditingPane.IsFloating)
            {
                IEditingPane.Dock();
            }

            if (EditorWindowController.GetInstance().IEditingModel != null)
                EditorWindowController.GetInstance().IEditingModel.SwitchWidgets(6);
        }

        private void AssetBrowserDocument_IsActiveChanged(object sender, EventArgs e)
        {
            if (!AssetBrowserDocument.IsActive)
                ThumbnailCache.Write(true);
        }

        private void MenuItem_Wireframe_Click(object sender, RoutedEventArgs e)
        {
            MenuItem_Wireframe.IsChecked = WorldFrame.Instance.MapManager.ToggleWireframe();
        }

        private void MenuItem_Terrain_Click(object sender, RoutedEventArgs e)
        {
            WorldFrame.Instance.MapManager.HideTerrain = !WorldFrame.Instance.MapManager.HideTerrain;
            MenuItem_Terrain.IsChecked = !WorldFrame.Instance.MapManager.HideTerrain;
        }

        private void MenuItem_M2_Click(object sender, RoutedEventArgs e)
        {
            WorldFrame.Instance.HideM2 = !WorldFrame.Instance.HideM2;
            MenuItem_M2.IsChecked = !WorldFrame.Instance.HideM2;
        }

        private void MenuItem_WMO_Click(object sender, RoutedEventArgs e)
        {
            WorldFrame.Instance.HideWMO = !WorldFrame.Instance.HideWMO;
            MenuItem_WMO.IsChecked = !WorldFrame.Instance.HideWMO;
        }
    }
}
