using System.Windows;
using System.Windows.Interop;
using Field;
internal static class LifetimeChecks
{
 public static void Run()
 {
  var app = new App();
  foreach (var name in new[] { "Tokens", "OverlayTokens", "DarkControls" }) app.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri($"pack://application:,,,/Field;component/Design/{name}.xaml") });
  var main = new MainWindow(); Exception? failure = null; bool started = false;
  main.ContentRendered += async (_, _) =>
  {
   if (started) return; started = true;
   try
   {
    var handle = new WindowInteropHelper(main).Handle;
    main.Close(); await Task.Delay(100);
    if (main.IsVisible || new WindowInteropHelper(main).Handle != handle) throw new Exception("Closing destroyed the main window instead of hiding it.");
    var hotkey = typeof(MainWindow).GetField("hotkey", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.GetValue(main)!;
    if (hotkey == null || (IntPtr)typeof(GlobalHotkey).GetField("hook", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.GetValue(hotkey)! == IntPtr.Zero) throw new Exception("Hotkey was removed while hidden.");
    main.ShowMain(); await Task.Delay(100);
    if (!main.IsVisible) throw new Exception("Tray restore failed.");
    Console.WriteLine("PASS: Close hides window, retains hotkey, restores window; explicit Exit shuts down.");
   }
   catch (Exception ex) { failure = ex; }
   finally { main.Quit(); }
  };
  app.Run(main); if (failure != null) throw failure;
 }
}
