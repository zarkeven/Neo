﻿using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Threading;
using WoWEditor6.Graphics;
using WoWEditor6.Scene;
using WoWEditor6.UI;

namespace WoWEditor6
{
    class Program
    {
        [STAThread]
        static void Main()
        {
            AppDomain.CurrentDomain.UnhandledException += (args, e) => Log.Debug(e.ExceptionObject.ToString());

            Settings.KeyBindings.Initialize();

            var window = new EditorWindow();
            var context = new GxContext(window.DrawTarget);
            context.InitContext();

            WorldFrame.Instance.Initialize(window.DrawTarget, context);
            WorldFrame.Instance.OnResize((int) window.RenderSize.Width, (int) window.RenderSize.Height);

            var app = new Application();
            var timer = new DispatcherTimer(TimeSpan.FromMilliseconds(10), DispatcherPriority.Send,
                (sender, args) =>
                {
                    var watch = Stopwatch.StartNew();
                    context.BeginFrame();
                    WorldFrame.Instance.OnFrame();
                    context.EndFrame();
                    Log.Debug(watch.ElapsedMilliseconds.ToString());
                }, app.Dispatcher);

            app.Run(window);

            WorldFrame.Instance.Shutdown();
        }
    }
}
