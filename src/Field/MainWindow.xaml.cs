using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Threading;
namespace Field;
public partial class MainWindow : Window
{
 Settings settings = new(); List<DictionaryEntry> dictionary = []; List<Transcript> history = [];
 readonly ISpeechEngine engine = new WhisperCliEngine();
 GlobalHotkey? hotkey; System.Windows.Forms.NotifyIcon? tray; FileSystemWatcher? watcher;
 RecordingOverlay? overlay;
 bool exitRequested;
 readonly DispatcherTimer timer = new() { Interval = TimeSpan.FromMilliseconds(40) };
 AudioCapture? capture; CancellationTokenSource? cancellation; DateTime started; bool busy, hotkeySession, ready, closing; IntPtr target;
 Guid? selected; DateTime lastDictionaryWrite; bool dictionaryInvalid, historyInvalid;
 public MainWindow()
 {
  InitializeComponent();
  Icon = System.Windows.Media.Imaging.BitmapFrame.Create(new Uri("pack://application:,,,/Field;component/Design/Field.ico"));
  try { settings = Storage.Read("settings", () => new Settings()); } catch (Exception ex) { Status.Text = "Settings file error: " + ex.Message; }
  try { history = Storage.Read("history", () => new List<Transcript>()); if (history.Any(t => t == null || t.Text == null || t.Raw == null || t.Corrections == null)) throw new InvalidDataException("Invalid history entry."); } catch (Exception ex) { history = []; historyInvalid = true; Status.Text = "History file error; existing file will be preserved. " + ex.Message; }
  try { ReloadDictionary(); } catch (Exception ex) { dictionaryInvalid = true; Status.Text = "Dictionary file error: " + ex.Message; }
  ready = true; RefreshHistory(); RefreshDictionary(); UpdateShortcutHint();
  timer.Tick += Tick; timer.Start();
  PreviewKeyDown += (_, e) => { if (e.Key == Key.OemComma && Keyboard.Modifiers == ModifierKeys.Control) { OpenSettings(this, new()); e.Handled = true; } else if (e.Key == Key.Escape) CancelRecording(this, new()); };
  SourceInitialized += (_, _) => SetupIntegration();
  Activated += (_, _) => CheckDictionary();
 }
 void SetupIntegration()
 {
  try { ConnectHotkey(); }
  catch (Exception ex) { Status.Text = ex.Message; }
  tray = new System.Windows.Forms.NotifyIcon { Icon = System.Drawing.Icon.ExtractAssociatedIcon(Environment.ProcessPath!), Text = "FIELD / 01 — Ready", Visible = true };
  var menu = new System.Windows.Forms.ContextMenuStrip(); menu.Items.Add("Open FIELD", null, (_, _) => Dispatcher.Invoke(ShowMain)); menu.Items.Add("Settings", null, (_, _) => Dispatcher.Invoke(() => OpenSettings(this, new()))); menu.Items.Add("Exit", null, (_, _) => Dispatcher.Invoke(Quit)); tray.ContextMenuStrip = menu; tray.DoubleClick += (_, _) => Dispatcher.Invoke(ShowMain);
  watcher = new FileSystemWatcher(Storage.Folder, "dictionary.json") { NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName, EnableRaisingEvents = true };
  watcher.Changed += (_, _) => Dispatcher.BeginInvoke(CheckDictionary); watcher.Created += (_, _) => Dispatcher.BeginInvoke(CheckDictionary); watcher.Renamed += (_, _) => Dispatcher.BeginInvoke(CheckDictionary);
 }
 void ConnectHotkey()
 {
  hotkey = new GlobalHotkey(settings.Hotkey);
  hotkey.Pressed += () => Dispatcher.BeginInvoke(() => HandleShortcut(true));
  hotkey.Released += () => Dispatcher.BeginInvoke(() => HandleShortcut(false));
 }
 async void HandleShortcut(bool pressed)
 {
  if (closing) return;
  switch (RecordingShortcut.Decide(pressed, settings.HoldToTalk, capture != null, hotkeySession, busy))
  {
   case ShortcutAction.Start: BeginRecording(true); break;
   case ShortcutAction.Stop: await FinishRecording(); break;
  }
 }
 string ShortcutHelp => settings.HoldToTalk ? "Hold " + settings.Hotkey + " to record; release to stop." : "Tap " + settings.Hotkey + " to record; tap again to stop.";
 void UpdateShortcutHint() => ShortcutHint.Text = ShortcutHelp;
 void Notify(string title, string message) => tray?.ShowBalloonTip(3000, title, message, System.Windows.Forms.ToolTipIcon.Info);
 public void ShowMain() { Show(); if (WindowState == WindowState.Minimized) WindowState = WindowState.Normal; Activate(); }
 void ReloadDictionary()
 {
  var loaded = Storage.Read("dictionary", () => new List<DictionaryEntry>());
  if (loaded.Any(e => e == null || string.IsNullOrWhiteSpace(e.Word) || e.Word.Length > 120 || e.Heard == null || e.Heard.Length > 120) || loaded.Count > 1000) throw new InvalidDataException("Dictionary must contain at most 1,000 entries with Word and Heard strings of at most 120 characters.");
  dictionary = loaded; dictionaryInvalid = false;
  if (!File.Exists(Storage.PathFor("dictionary"))) Storage.Save("dictionary", dictionary);
  lastDictionaryWrite = File.GetLastWriteTimeUtc(Storage.PathFor("dictionary"));
 }
 void CheckDictionary()
 {
  if (!ready || closing || File.GetLastWriteTimeUtc(Storage.PathFor("dictionary")) == lastDictionaryWrite) return;
  try { ReloadDictionary(); RefreshDictionary(); Status.Text = "Dictionary reloaded from file."; } catch (Exception ex) { dictionaryInvalid = true; Status.Text = "Dictionary file error; keeping last valid entries. " + ex.Message; }
 }
 void RefreshHistory()
 {
  if (!ready) return;
  var q = HistorySearch.Text.Trim(); var matches = history.Where(t => t.Text.Contains(q, StringComparison.OrdinalIgnoreCase) || t.Raw.Contains(q, StringComparison.OrdinalIgnoreCase)).OrderByDescending(t => t.Created).ToList();
  HistoryList.ItemsSource = matches; HistoryEmpty.Visibility = matches.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
  HistoryEmpty.Text = history.Count == 0 ? "No recordings yet. Press RECORD, or use the shortcut in another app. " + ShortcutHelp : "No transcriptions match your search.";
 }
 void RefreshDictionary() { if (ready) DictionaryList.ItemsSource = dictionary.Where(e => e.Display.Contains(DictionarySearch.Text.Trim(), StringComparison.OrdinalIgnoreCase)).ToList(); }
 void SearchHistory(object sender, TextChangedEventArgs e) => RefreshHistory();
 void SearchDictionary(object sender, TextChangedEventArgs e) => RefreshDictionary();
 void SelectEntry(object sender, SelectionChangedEventArgs e) { if (DictionaryList.SelectedItem is DictionaryEntry entry) { selected = entry.Id; TargetWord.Text = entry.Word; HeardWord.Text = entry.Heard; } }
 void EntryChanged(object sender, TextChangedEventArgs e) { if (DictionaryWarning != null && HeardWord != null) DictionaryWarning.Text = DictionaryRules.Warning(new() { Word = TargetWord.Text, Heard = HeardWord.Text }); }
 void SaveEntry(object sender, RoutedEventArgs e)
 {
  CheckDictionary();
  if (dictionaryInvalid) { Status.Text = "Fix the dictionary file before saving, so your external edits are not overwritten."; return; }
  var word = TargetWord.Text.Trim(); var heard = HeardWord.Text.Trim();
  if (word.Length == 0 || word.Length > 120 || heard.Length > 120 || dictionary.Count >= 1000 && selected == null) { DictionaryWarning.Text = "Enter a word or phrase (maximum 120 characters; 1,000 entries)."; return; }
  if (dictionary.Any(d => d.Id != selected && (string.IsNullOrWhiteSpace(d.Heard) ? d.Word : d.Heard).Equals(heard.Length == 0 ? word : heard, StringComparison.OrdinalIgnoreCase))) { DictionaryWarning.Text = "That source phrase already exists. Edit its existing entry."; return; }
  var entry = dictionary.FirstOrDefault(d => d.Id == selected); if (entry == null) { entry = new(); dictionary.Add(entry); }
  entry.Word = word; entry.Heard = heard;
  try { Storage.Save("dictionary", dictionary); lastDictionaryWrite = File.GetLastWriteTimeUtc(Storage.PathFor("dictionary")); RefreshDictionary(); NewEntry(this, new()); Status.Text = "Dictionary entry saved."; } catch (Exception ex) { Status.Text = ex.Message; }
 }
 void NewEntry(object sender, RoutedEventArgs e) { selected = null; DictionaryList.SelectedItem = null; TargetWord.Clear(); HeardWord.Clear(); }
 void DeleteEntry(object sender, RoutedEventArgs e) { CheckDictionary(); if (selected == null || dictionaryInvalid) return; dictionary.RemoveAll(d => d.Id == selected); try { Storage.Save("dictionary", dictionary); RefreshDictionary(); NewEntry(this, new()); } catch (Exception ex) { Status.Text = ex.Message; } }
 void OpenDictionaryFile(object sender, RoutedEventArgs e) { try { Process.Start(new ProcessStartInfo("notepad.exe") { ArgumentList = { Storage.PathFor("dictionary") }, UseShellExecute = false }); } catch (Exception ex) { Status.Text = ex.Message; } }
 void CopyTranscript(object sender, RoutedEventArgs e) { if ((sender as Button)?.Tag is Transcript t) try { System.Windows.Clipboard.SetText(t.Text); Status.Text = "Transcript copied."; } catch (Exception ex) { Status.Text = "Clipboard unavailable: " + ex.Message; } }
 void OpenSettings(object sender, RoutedEventArgs e)
 {
  if (capture != null || busy) { Status.Text = "Stop or cancel the recording before changing Settings."; return; }
  var dialog = new SettingsWindow(settings) { Owner = this };
  if (dialog.ShowDialog() == true && dialog.Result != null) try { Storage.Save("settings", dialog.Result); settings = dialog.Result; if (hotkey == null) ConnectHotkey(); else hotkey.Configure(settings.Hotkey); UpdateShortcutHint(); RefreshHistory(); Status.Text = "Settings saved — " + ShortcutHelp; } catch (Exception ex) { Status.Text = ex.Message; }
 }
 void BeginRecording(bool fromHotkey)
 {
  if (capture != null || busy || closing || OwnedWindows.Count > 0) return;
  CheckDictionary(); target = fromHotkey ? TextDelivery.GetForegroundWindow() : IntPtr.Zero;
  if (target == new WindowInteropHelper(this).Handle) target = IntPtr.Zero;
  hotkeySession = fromHotkey;
  try { capture = new AudioCapture(); capture.Start(); started = DateTime.UtcNow; StartButton.IsEnabled = false; StopButton.IsEnabled = true; var stopHelp = fromHotkey ? (settings.HoldToTalk ? "release the hotkey to transcribe" : "tap " + settings.Hotkey + " again to transcribe") : "press STOP to transcribe"; Status.Text = "● RECORDING — " + stopHelp; if (tray != null) tray.Text = "FIELD / 01 — Recording"; (overlay ??= new RecordingOverlay()).Present("Listening…", false, target); }
  catch (Exception ex) { capture?.Dispose(); capture = null; hotkeySession = false; Status.Text = ex.Message; if (fromHotkey) Notify("FIELD — Cannot record", ex.Message); }
 }
 void Tick(object? sender, EventArgs e)
 {
  var raw = capture?.Level ?? 0; var desired = raw <= 0 ? 0 : Math.Clamp((20 * Math.Log10(raw) + 48) / 48, 0, 1);
  var ms = (double)FindResource(desired > Meter.Level ? "NeedleAttackMs" : "NeedleReleaseMs"); Meter.Level += (desired - Meter.Level) * (1 - Math.Exp(-timer.Interval.TotalMilliseconds / ms));
  overlay?.Level(desired);
  if (capture != null) { var elapsed = DateTime.UtcNow - started; Counter.Text = elapsed.ToString(@"mm\:ss\.f"); if (elapsed.TotalMinutes >= 10) _ = FinishRecording(); }
 }
 async Task FinishRecording()
 {
  if (capture == null || busy) return;
  busy = true; hotkeySession = false; StopButton.IsEnabled = false; var recording = capture; capture = null;
  var folder = Path.Combine(Path.GetTempPath(), "Field", Guid.NewGuid().ToString("N"));
  cancellation = new CancellationTokenSource(TimeSpan.FromMinutes(5));
  var resultLabel = "Saved";
  try
  {
   Directory.CreateDirectory(folder); var wav = Path.Combine(folder, "recording.wav"); recording.Stop(wav);
   if (!recording.HasSpeech) { resultLabel = "No speech"; Status.Text = "No audible speech detected. Nothing was transcribed."; return; }
   Status.Text = "TRANSCRIBING — Escape cancels"; if (tray != null) tray.Text = "FIELD / 01 — Transcribing";
   overlay?.Present("Transcribing…", true, target);
   var raw = await engine.TranscribeAsync(wav, DictionaryRules.Context(dictionary), settings, cancellation.Token);
   if (string.IsNullOrWhiteSpace(raw)) { resultLabel = "No speech"; Status.Text = "No speech returned by the engine."; return; }
   var transcript = DictionaryRules.Apply(raw, dictionary); history.Add(transcript); RefreshHistory();
   if (historyInvalid) { resultLabel = "Check history"; Status.Text = "Transcript available to COPY, but not saved: repair history.json and restart first."; return; }
   Storage.Save("history", history);
   Status.Text = target == IntPtr.Zero ? "Transcript saved." : await TextDelivery.Paste(transcript.Text, target);
   resultLabel = target == IntPtr.Zero ? "Saved" : Status.Text.Contains("paste sent") ? "Done" : "Ready to copy";
  }
  catch (OperationCanceledException) { resultLabel = "Cancelled"; Status.Text = "Transcription cancelled or timed out."; }
  catch (Exception ex) { resultLabel = "Check FIELD"; Status.Text = ex.Message; }
  finally { recording.Dispose(); cancellation.Dispose(); cancellation = null; busy = false; StartButton.IsEnabled = true; if (tray != null) tray.Text = "FIELD / 01 — Ready"; if (!closing) overlay?.Present(resultLabel, false, target, true); try { if (Directory.Exists(folder)) Directory.Delete(folder, true); } catch (IOException) { } }
 }
 void StartRecording(object sender, RoutedEventArgs e) => BeginRecording(false);
 async void StopRecording(object sender, RoutedEventArgs e) => await FinishRecording();
 async void ToggleRecording(object sender, RoutedEventArgs e) { if (capture != null) await FinishRecording(); else BeginRecording(false); }
 void CancelRecording(object sender, RoutedEventArgs e) { cancellation?.Cancel(); capture?.Dispose(); capture = null; hotkeySession = false; StartButton.IsEnabled = !busy; StopButton.IsEnabled = false; Status.Text = "Cancelled."; overlay?.Dismiss(); if (tray != null) tray.Text = "FIELD / 01 — Ready"; }
 public void Quit() { exitRequested = true; Close(); System.Windows.Application.Current.Shutdown(); }
 void ExitApp(object sender, RoutedEventArgs e) => Quit();
 void OnClosing(object? sender, CancelEventArgs e)
 {
  if (!exitRequested && !App.IsEnding) { e.Cancel = true; Hide(); return; }
  closing = true; timer.Stop(); cancellation?.Cancel(); capture?.Dispose(); hotkey?.Dispose(); watcher?.Dispose(); tray?.Dispose(); overlay?.Close();
 }
}
