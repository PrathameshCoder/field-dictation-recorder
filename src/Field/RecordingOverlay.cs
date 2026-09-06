using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
namespace Field;

public sealed class RecordingOverlay : Window
{
 [DllImport("user32.dll", EntryPoint="GetWindowLongPtrW")] static extern IntPtr GetStyle(IntPtr window, int index);
 [DllImport("user32.dll", EntryPoint="SetWindowLongPtrW")] static extern IntPtr SetStyle(IntPtr window, int index, IntPtr value);
 [DllImport("user32.dll")] static extern bool SetWindowPos(IntPtr window, IntPtr after, int x, int y, int width, int height, uint flags);
 [StructLayout(LayoutKind.Sequential)] struct Rect { public int Left, Top, Right, Bottom; }
 [DllImport("user32.dll")] static extern bool GetWindowRect(IntPtr window, out Rect rect);
 public OrbView Orb { get; } = new();
 readonly VoiceWaveform waveform = new(); readonly DispatcherTimer dismiss = new(); string orbState = "idle";
 public RecordingOverlay()
 {
  Width = (double)FindResource("PillWidth"); Height = (double)FindResource("PillHeight");
  WindowStyle = WindowStyle.None; ResizeMode = ResizeMode.NoResize; AllowsTransparency = true; Background = Brushes.Transparent;
  ShowInTaskbar = false; ShowActivated = false; Topmost = true; Focusable = false; IsHitTestVisible = false;
  var row = new Grid(); row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); row.ColumnDefinitions.Add(new ColumnDefinition());
  Orb.Width = Orb.Height = (double)FindResource("PillOrbSize"); Orb.VerticalAlignment = VerticalAlignment.Center; row.Children.Add(Orb);
  waveform.Height = (double)FindResource("WaveHeight"); waveform.VerticalAlignment = VerticalAlignment.Center; Grid.SetColumn(waveform, 1); row.Children.Add(waveform);
  Content = new Border { Background = (Brush)FindResource("PillSurface"), BorderBrush = (Brush)FindResource("PillBorder"), BorderThickness = (Thickness)FindResource("PillStroke"), CornerRadius = (CornerRadius)FindResource("PillRadius"), Padding = (Thickness)FindResource("PillPadding"), Child = row };
  SourceInitialized += (_, _) => { var source = (HwndSource)PresentationSource.FromVisual(this); var handle = source.Handle; SetStyle(handle, -20, new IntPtr(GetStyle(handle, -20).ToInt64() | 0x08000000L | 0x80L | 0x20L)); source.AddHook((IntPtr h, int message, IntPtr w, IntPtr l, ref bool handled) => { if (message == 0x21) { handled = true; return new IntPtr(3); } if (message == 0x84) { handled = true; return new IntPtr(-1); } return IntPtr.Zero; }); };
  dismiss.Interval = TimeSpan.FromSeconds((double)FindResource("PillResultSeconds")); dismiss.Tick += (_, _) => Dismiss();
  Closed += (_, _) => { dismiss.Stop(); Orb.Dispose(); };
 }
 public void Present(string text, bool thinking, IntPtr target, bool temporary = false)
 {
  dismiss.Stop(); System.Windows.Automation.AutomationProperties.SetName(this, text); orbState = thinking ? "thinking" : "idle"; waveform.SetState(thinking, temporary);
  var foreground = target == IntPtr.Zero ? TextDelivery.GetForegroundWindow() : target;
  Show();
  Position(foreground);
  // Allow WPF to apply a monitor DPI change, then center using the actual native size.
  Dispatcher.BeginInvoke(DispatcherPriority.Loaded, () => { if (IsVisible) Position(foreground); });
  Orb.SetState(orbState, true);
  if (temporary) dismiss.Start();
 }
 void Position(IntPtr target)
 {
  var area = System.Windows.Forms.Screen.FromHandle(target).WorkingArea; var handle = new WindowInteropHelper(this).Handle;
  if (!GetWindowRect(handle, out var rect)) return;
  var width = rect.Right - rect.Left; var height = rect.Bottom - rect.Top;
  var scale = width / Width;
  // Never resize a WPF window using another process's DPI: it clips the WPF contents.
  SetWindowPos(handle, new IntPtr(-1), area.Left + (area.Width - width) / 2, area.Bottom - height - (int)((double)FindResource("PillBottomOffset") * scale), 0, 0, 0x1 | 0x10 | 0x40);
 }
 public void Level(double level) { if (IsVisible) { Orb.SetState(orbState, true, level); waveform.Level(level); } }
 public void Dismiss() { dismiss.Stop(); Orb.SetState("idle", false); Hide(); }
}
