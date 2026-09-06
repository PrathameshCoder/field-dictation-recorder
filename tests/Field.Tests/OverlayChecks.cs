using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Field;
internal static class OverlayChecks
{
 public static void Run()
 {
  var app = new App();
  app.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri("pack://application:,,,/Field;component/Design/Tokens.xaml") });
  app.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri("pack://application:,,,/Field;component/Design/OverlayTokens.xaml") });
  var input = new TextBox { Text = "Overlay focus test — no typing or microphone capture", Margin = new Thickness(24) };
  var target = new Window { Title = "FIELD overlay verification", Width = 640, Height = 240, Content = input };
  Exception? failure = null;
  target.ContentRendered += async (_, _) =>
  {
   RecordingOverlay? overlay = null;
   try
   {
    target.Activate(); input.Focus(); await Task.Delay(200);
    var handle = new WindowInteropHelper(target).Handle;
    if (TextDelivery.GetForegroundWindow() != handle) throw new Exception("Could not focus the isolated test window.");
    overlay = new RecordingOverlay(); overlay.Present("Listening…", false, handle);
    for (var i = 0; i < 600 && !overlay.Orb.IsShaderReady && overlay.Orb.RenderError == null; i++) { await Task.Delay(100); if (i == 200) Console.WriteLine("Orb initialization: " + overlay.Orb.RenderStage); }
    if (!overlay.Orb.IsShaderReady) throw new Exception("Orb shader did not initialize: " + overlay.Orb.RenderStage + ": " + overlay.Orb.RenderError);
    await Task.Delay(600);
    var orbBounds = overlay.Orb.TransformToAncestor(overlay).TransformBounds(new Rect(overlay.Orb.RenderSize));
    if (orbBounds.Top < 0 || orbBounds.Bottom > overlay.ActualHeight || orbBounds.Left < 0 || orbBounds.Right > overlay.ActualWidth) throw new Exception("Orb is clipped by the pill.");
    foreach (var level in new[] { .05, .1, .2, .4, .8, 1.0, .8, .6, .4, .7, .9, .5, .2, .15, .1, .2, .3 }) { overlay.Level(level); await Task.Delay(40); }
    if (TextDelivery.GetForegroundWindow() != handle || !input.IsKeyboardFocused) throw new Exception("Overlay stole focus.");
    if (overlay.ShowInTaskbar || overlay.ShowActivated || !overlay.Topmost) throw new Exception("Overlay window behavior is incorrect.");
    Directory.CreateDirectory(TestPaths.Artifacts);
    await overlay.Orb.CaptureAsync(TestPaths.Artifact("orb-live.png"));
    var bitmap = new RenderTargetBitmap((int)overlay.ActualWidth, (int)overlay.ActualHeight, 96, 96, PixelFormats.Pbgra32); bitmap.Render(overlay);
    var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap)); using (var file = File.Create(TestPaths.Artifact("overlay-live.png"))) encoder.Save(file);
    overlay.Present("Transcribing…", true, handle); await Task.Delay(400);
    if (!(await overlay.Orb.ReadStateAsync()).Contains("thinking")) throw new Exception("Thinking state was not applied.");
    if (TextDelivery.GetForegroundWindow() != handle) throw new Exception("State update stole focus.");
    overlay.Dismiss(); await Task.Delay(150);
    if (overlay.IsVisible || !(await overlay.Orb.ReadStateAsync()).Contains("false")) throw new Exception("Hidden orb did not pause.");
    overlay.Present("Done", false, handle, true); await Task.Delay(2900);
    if (overlay.IsVisible) throw new Exception("Result did not auto-hide.");
    Console.WriteLine("PASS: Live WebGPU shader, unclipped orb, waveform, non-activating overlay, thinking state, pause and auto-hide.");
   }
   catch (Exception ex) { failure = ex; }
   finally { overlay?.Close(); target.Close(); }
  };
  app.Run(target);
  if (failure != null) throw failure;
 }
}
