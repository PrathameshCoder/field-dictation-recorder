using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Field;
internal static class Program
{
 static int count;
 static void Equal(string expected, string actual) { if (expected != actual) throw new Exception($"Expected [{expected}], got [{actual}]"); count++; }
 [STAThread] static int Main(string[] args)
 {
  try
  {
   List<DictionaryEntry> pair = [new() { Heard = "cloud code", Word = "Claude Code" }];
   Equal("Claude Code Claude Code Claude Code", DictionaryRules.Apply("CloudCode cloud-code CLOUD CODE", pair).Text);
   Equal("cloud Cloudflare cloud coder xCloudCode CloudCodeX _cloudcode cloudcode2", DictionaryRules.Apply("cloud Cloudflare cloud coder xCloudCode CloudCodeX _cloudcode cloudcode2", pair).Text);
   Equal("(Claude Code), Claude Code!", DictionaryRules.Apply("(cloud code), CLOUD-CODE!", pair).Text);
   Equal("Claude Code", DictionaryRules.Apply("CloudCode", [new() { Word = "Claude Code" }]).Text);
   Equal("Vercel Supabase", DictionaryRules.Apply("vercel supabase", [new() { Word = "Vercel" }, new() { Word = "Supabase" }]).Text);
   Equal("LONG SHORT", DictionaryRules.Apply("alpha beta alpha", [new() { Heard = "alpha", Word = "SHORT" }, new() { Heard = "alpha beta", Word = "LONG" }]).Text);
   Equal("beta", DictionaryRules.Apply("alpha", [new() { Heard = "alpha", Word = "beta" }, new() { Heard = "beta", Word = "gamma" }]).Text);
   Equal("C++", DictionaryRules.Apply("c++", [new() { Word = "C++" }]).Text);
   Equal("écloudcode cloudcodeé", DictionaryRules.Apply("écloudcode cloudcodeé", pair).Text);
   Equal("2", DictionaryRules.Apply("CloudCode cloud-code", pair).Corrections.Count.ToString());
   Equal("CloudCode", DictionaryRules.Apply("CloudCode", pair).Corrections[0].Before);
   Equal("True", DictionaryRules.Warning(new() { Word = "Claude", Heard = "cloud" }).StartsWith("CAUTION").ToString());
   var context = DictionaryRules.Context(Enumerable.Range(0, 100).Select(i => new DictionaryEntry { Word = "Product" + i }));
   Equal("True", (context.Length <= 240 && context.Split(',').Length <= 24).ToString());
   Equal("Claude Code", DictionaryRules.Context(pair));
   GlobalHotkey.Validate("Ctrl+Alt+Space"); count++;
   try { GlobalHotkey.Validate("Space"); throw new Exception("Invalid hotkey accepted"); } catch (ArgumentException) { count++; }
   Equal("Start", RecordingShortcut.Decide(true, false, false, false, false).ToString());
   Equal("None", RecordingShortcut.Decide(false, false, true, true, false).ToString());
   Equal("Stop", RecordingShortcut.Decide(true, false, true, true, false).ToString());
   Equal("None", RecordingShortcut.Decide(false, false, false, false, true).ToString());
   Equal("None", RecordingShortcut.Decide(true, false, false, false, true).ToString());
   Equal("Start", RecordingShortcut.Decide(true, true, false, false, false).ToString());
   Equal("Stop", RecordingShortcut.Decide(false, true, true, true, false).ToString());
   Equal("None", RecordingShortcut.Decide(true, false, true, false, false).ToString());
   Equal("None", RecordingShortcut.Decide(false, true, true, false, false).ToString());
   var migrated = System.Text.Json.JsonSerializer.Deserialize<Settings>("{\"EnginePath\":\"engine.exe\",\"ModelPath\":\"model.bin\",\"Hotkey\":\"Ctrl+Alt+Space\"}")!;
   Equal("False", migrated.HoldToTalk.ToString()); Equal("engine.exe", migrated.EnginePath); Equal("model.bin", migrated.ModelPath);
   var heldSettings = new Settings { HoldToTalk = true };
   Equal("True", System.Text.Json.JsonSerializer.Deserialize<Settings>(System.Text.Json.JsonSerializer.Serialize(heldSettings))!.HoldToTalk.ToString());
   var scoped = System.Text.Json.JsonSerializer.Deserialize<List<DictionaryEntry>>(File.ReadAllText(TestPaths.Source("Design/RecommendedDictionary.json")))!;
   Equal("The dictation recorder app. So to use the shortcut and to have it using.", DictionaryRules.Apply("The declaration recorder app. So to use the shotgun and to have it using.", scoped).Text);
   Equal("A declaration about a shotgun.", DictionaryRules.Apply("A declaration about a shotgun.", scoped).Text);
   Equal("2", DictionaryRules.Apply("declaration recorder; use the shotgun and", scoped).Corrections.Count.ToString());
   Console.WriteLine($"PASS: {count} dictionary, shortcut-mode and settings checks.");
   if (args.Contains("--overlay")) OverlayChecks.Run();
   if (args.Contains("--lifetime")) LifetimeChecks.Run();
   if (args.Contains("--render"))
   {
    var app = new App(); app.InitializeComponent();
    var main = new MainWindow(); if (args.Contains("--render-live")) main.Show(); Render(main, "main.png");
    main.Width = 800; main.Height = 600; Render(main, "main-small.png");
    var tabs = Find<System.Windows.Controls.TabControl>(main); if (tabs != null) tabs.SelectedIndex = 1; Render(main, "dictionary-small.png");
    var settings = new SettingsWindow(new Settings()); if (args.Contains("--render-live")) settings.Show(); Render(settings, "settings.png");
    main.Close(); settings.Close();
    Console.WriteLine("PASS: Main and Settings views constructed and rendered.");
   }
   return 0;
  }
  catch (Exception ex) { Console.Error.WriteLine(ex); return 1; }
 }
 static void Render(Window window, string file)
 {
  var content = (FrameworkElement)window.Content;
  content.Measure(new Size(window.Width, window.Height)); content.Arrange(new Rect(0, 0, window.Width, window.Height)); content.UpdateLayout();
  var visual = new DrawingVisual(); using (var drawing = visual.RenderOpen()) { drawing.DrawRectangle(window.Background, null, new Rect(0, 0, window.Width, window.Height)); drawing.DrawRectangle(new VisualBrush(content) { ViewboxUnits = BrushMappingMode.Absolute, Viewbox = new Rect(0, 0, window.Width, window.Height), Stretch = Stretch.None }, null, new Rect(0, 0, window.Width, window.Height)); }
  var bitmap = new RenderTargetBitmap((int)window.Width, (int)window.Height, 96, 96, PixelFormats.Pbgra32); bitmap.Render(visual);
  var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap)); Directory.CreateDirectory(TestPaths.Artifacts); using var stream = File.Create(TestPaths.Artifact(file)); encoder.Save(stream);
 }
 static T? Find<T>(DependencyObject parent) where T : DependencyObject { for (var i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++) { var child = VisualTreeHelper.GetChild(parent, i); if (child is T found) return found; var nested = Find<T>(child); if (nested != null) return nested; } return null; }
}
