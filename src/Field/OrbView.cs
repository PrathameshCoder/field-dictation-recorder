using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
namespace Field;

public sealed class OrbView : Grid, IDisposable
{
 readonly WebView2CompositionControl web = new() { Focusable = false, IsHitTestVisible = false, DefaultBackgroundColor = System.Drawing.Color.Transparent };
 readonly System.Windows.Shapes.Ellipse fallback = new();
 bool initialized, disposed, active, sending; string state = "idle"; double level; long revision;
 public bool IsShaderReady { get; private set; }
 public string? RenderError { get; private set; }
 public string RenderStage { get; private set; } = "Waiting for layout";
 public OrbView()
 {
  IsHitTestVisible = false;
  fallback.Fill = (Brush)FindResource("OrbFallback"); fallback.Margin = new Thickness((double)FindResource("PillOrbSize") * .15);
  Children.Add(fallback); Children.Add(web); Loaded += Initialize;
 }
 async void Initialize(object sender, RoutedEventArgs e)
 {
  if (initialized || disposed) return; initialized = true; RenderStage = "Loading local page";
  try
  {
   using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Field.Design.Orb.html")!;
   using var reader = new StreamReader(stream);
   var page = System.Text.Encoding.UTF8.GetBytes(await reader.ReadToEndAsync());
   RenderStage = "Starting WebView2";
   var environment = await CoreWebView2Environment.CreateAsync(null, Path.Combine(Storage.Folder, "WebView2"));
   if (disposed) return;
   RenderStage = "Creating compositor"; await web.EnsureCoreWebView2Async(environment);
   if (disposed) return;
   web.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
   web.CoreWebView2.Settings.AreDevToolsEnabled = false;
   web.CoreWebView2.Settings.IsStatusBarEnabled = false;
   web.CoreWebView2.Settings.IsZoomControlEnabled = false;
   // Serve the embedded document directly: no filesystem mapping or network lookup.
   web.CoreWebView2.AddWebResourceRequestedFilter("https://field.invalid/*", CoreWebView2WebResourceContext.All);
   web.CoreWebView2.WebResourceRequested += (_, args) => args.Response = environment.CreateWebResourceResponse(new MemoryStream(page, false), 200, "OK", "Content-Type: text/html; charset=utf-8\r\nCache-Control: no-store");
   ulong pageNavigation = 0;
   web.CoreWebView2.NavigationStarting += (_, args) => { if (args.Uri == "https://field.invalid/orb.html") { pageNavigation = args.NavigationId; RenderStage = "Loading orb document"; } else if (args.Uri != "about:blank") args.Cancel = true; };
   web.CoreWebView2.NewWindowRequested += (_, args) => args.Handled = true;
   web.CoreWebView2.PermissionRequested += (_, args) => args.State = CoreWebView2PermissionState.Deny;
   web.CoreWebView2.WebMessageReceived += (_, args) =>
   {
    if (args.TryGetWebMessageAsString() == "orb-ready") { IsShaderReady = true; fallback.Visibility = Visibility.Hidden; _ = Update(); }
    else { RenderError = "WebGPU unavailable"; web.Visibility = Visibility.Hidden; fallback.Visibility = Visibility.Visible; }
   };
   web.CoreWebView2.NavigationCompleted += (_, args) => { if (args.NavigationId != pageNavigation || pageNavigation == 0) return; RenderStage = args.IsSuccess ? "Compiling orb shader" : "Navigation failed"; if (!args.IsSuccess) RenderError = args.WebErrorStatus.ToString(); };
   RenderStage = "Opening local page"; web.CoreWebView2.Navigate("https://field.invalid/orb.html");
  }
  catch (Exception ex) { RenderError = ex.Message; web.Visibility = Visibility.Hidden; }
 }
 public void SetState(string next, bool visible, double inputLevel = 0) { state = next; active = visible; level = inputLevel; revision++; _ = Update(); }
 async Task Update()
 {
  if (!IsShaderReady || disposed || sending) return;
  sending = true;
  var sentRevision = revision;
  try { await web.ExecuteScriptAsync($"window.fieldUpdate({JsonSerializer.Serialize(state)}, {(active ? "true" : "false")}, {Math.Clamp(level, 0, 1).ToString(System.Globalization.CultureInfo.InvariantCulture)})"); }
  catch (Exception ex) { RenderError = ex.Message; }
  finally { sending = false; if (!disposed && sentRevision != revision) _ = Update(); }
 }
 public async Task<string> ReadStateAsync() => await web.ExecuteScriptAsync("JSON.stringify({state:liquidOrb.getState(),active:fieldActive,ready:fieldOrbReady})");
 public async Task CaptureAsync(string path) { using var file = File.Create(path); await web.CoreWebView2.CapturePreviewAsync(CoreWebView2CapturePreviewImageFormat.Png, file); }
 public void Dispose() { disposed = true; web.Dispose(); }
}
