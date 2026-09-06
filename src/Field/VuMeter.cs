using System.Globalization;
using System.Windows;
using System.Windows.Media;
namespace Field;
public sealed class VuMeter : FrameworkElement
{
 double level; public double Level { get => level; set { level = Math.Clamp(value, 0, 1); InvalidateVisual(); } }
 Brush B(string name) => (Brush)FindResource(name);
 double D(string name) => (double)FindResource(name);
 void Text(DrawingContext dc, string text, double x, double y, string brush = "Ink") => dc.DrawText(new FormattedText(text, CultureInfo.InvariantCulture, FlowDirection.LeftToRight, new Typeface((FontFamily)FindResource("MeterFont"), FontStyles.Normal, FontWeights.Normal, FontStretches.Normal), D("CaptionSize"), B(brush), VisualTreeHelper.GetDpi(this).PixelsPerDip), new Point(x, y));
 protected override void OnRender(DrawingContext dc)
 {
  base.OnRender(dc); double w = ActualWidth, h = ActualHeight, pad = D("MeterPadding");
  dc.DrawRectangle(B("Instrument"), new Pen(B("Seam"), D("NeedleWidth")), new Rect(0, 0, w, h));
  var center = new Point(w / 2, h - pad); double radius = Math.Min(w * .42, h * .7);
  Point P(double angle, double r) => new(center.X + Math.Sin(angle) * r, center.Y - Math.Cos(angle) * r);
  for (int i = 0; i <= 10; i++) { double a = -1.1 + i * .22; dc.DrawLine(new Pen(B(i >= 9 ? "Record" : "Amber"), D("MeterStroke")), P(a, radius * .84), P(a, radius)); }
  string[] labels = ["−20", "−10", "−5", "0", "+3"];
  for (int i = 0; i < labels.Length; i++) { var p = P(-1.1 + i * .55, radius * 1.16); Text(dc, labels[i], p.X - pad / 2, p.Y - pad / 2); }
  Text(dc, "VU / INPUT", pad, pad); Text(dc, "FIELD", w - pad * 4, h - pad * 2);
  dc.DrawLine(new Pen(B("Ink"), D("NeedleWidth")), center, P(-1.1 + level * 2.2, radius * .92));
  dc.DrawEllipse(B("Ink"), null, center, D("NeedleWidth") * 2, D("NeedleWidth") * 2);
 }
}
