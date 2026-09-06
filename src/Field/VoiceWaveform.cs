using System.Windows;
using System.Windows.Media;
namespace Field;
public sealed class VoiceWaveform : FrameworkElement
{
 readonly double[] samples = new double[17]; bool thinking, result; double smooth;
 public void SetState(bool processing, bool completed) { thinking = processing; result = completed; Array.Clear(samples); smooth = 0; InvalidateVisual(); }
 public void Level(double input)
 {
  // This is a recent input-level envelope, not a fabricated frequency spectrum.
  smooth += (Math.Clamp(input, 0, 1) - smooth) * (input > smooth ? .65 : .25);
  Array.Copy(samples, 1, samples, 0, samples.Length - 1); samples[^1] = smooth;
  InvalidateVisual();
 }
 protected override void OnRender(DrawingContext dc)
 {
  var brush = (Brush)FindResource("WaveColor"); var stroke = (double)FindResource("WaveStroke"); var minimum = (double)FindResource("WaveMinimum");
  var step = ActualWidth / samples.Length;
  for (var i = 0; i < samples.Length; i++)
  {
   var value = thinking ? .18 : result ? .08 : samples[i];
   var height = Math.Max(minimum, value * ActualHeight);
   dc.DrawRoundedRectangle(brush, null, new Rect(i * step + (step - stroke) / 2, (ActualHeight - height) / 2, stroke, height), stroke / 2, stroke / 2);
  }
 }
}
