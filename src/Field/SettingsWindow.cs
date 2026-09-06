using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
namespace Field;
public sealed class SettingsWindow : Window
{
 readonly TextBox hotkey = new(), engine = new(), model = new(); readonly TextBlock error = new();
 readonly ComboBox mode = new();
 public Settings? Result { get; private set; }
 public SettingsWindow(Settings settings)
 {
  Style = (Style)FindResource("AppWindow"); Title = "FIELD / 01 — Settings"; Width = (double)FindResource("SettingsWidth"); Height = (double)FindResource("SettingsHeight"); MinWidth = Width; MinHeight = Height; WindowStartupLocation = WindowStartupLocation.CenterOwner;
  hotkey.Text = settings.Hotkey; engine.Text = settings.EnginePath; model.Text = settings.ModelPath;
  var body = new StackPanel { Margin = (Thickness)FindResource("Space6") }; Content = new ScrollViewer { Content = body, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
  Label(body, "SETTINGS", true); Label(body, "GLOBAL SHORTCUT"); body.Children.Add(hotkey);
  Label(body, "RECORDING MODE"); mode.Items.Add("Tap to start / tap again to stop"); mode.Items.Add("Hold to record / release to stop"); mode.SelectedIndex = settings.HoldToTalk ? 1 : 0; body.Children.Add(mode);
  Label(body, "WHISPER.CPP EXECUTABLE"); Picker(body, engine, "Executable (*.exe)|*.exe");
  Label(body, "MODEL"); Picker(body, model, "Whisper model (*.bin;*.gguf)|*.bin;*.gguf|All files|*.*");
  error.Foreground = (System.Windows.Media.Brush)FindResource("Amber"); error.Margin = (Thickness)FindResource("Space2"); body.Children.Add(error);
  var buttons = new StackPanel { Orientation = Orientation.Horizontal }; body.Children.Add(buttons);
  var save = new Button { Content = "SAVE", IsDefault = true }; save.Click += (_, _) => { try { GlobalHotkey.Validate(hotkey.Text); if (engine.Text.Length > 0 && !System.IO.File.Exists(engine.Text)) throw new ArgumentException("The engine executable does not exist."); if (model.Text.Length > 0 && !System.IO.File.Exists(model.Text)) throw new ArgumentException("The model file does not exist."); Result = new Settings { Hotkey = hotkey.Text.Trim(), EnginePath = engine.Text.Trim(), ModelPath = model.Text.Trim(), HoldToTalk = mode.SelectedIndex == 1 }; DialogResult = true; } catch (Exception ex) { error.Text = ex.Message; } }; buttons.Children.Add(save); buttons.Children.Add(new Button { Content = "CANCEL", IsCancel = true });
 }
 void Label(Panel panel, string text, bool heading = false) => panel.Children.Add(new TextBlock { Text = text, Style = (Style)FindResource(heading ? "Heading" : "Label") });
 void Picker(Panel panel, TextBox box, string filter) { var row = new DockPanel(); var browse = new Button { Content = "BROWSE" }; DockPanel.SetDock(browse, Dock.Right); row.Children.Add(browse); row.Children.Add(box); panel.Children.Add(row); browse.Click += (_, _) => { var dialog = new OpenFileDialog { Filter = filter }; if (dialog.ShowDialog(this) == true) box.Text = dialog.FileName; }; }
}
