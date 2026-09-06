using System.Runtime.InteropServices;
using System.Windows.Input;
namespace Field;
public sealed class GlobalHotkey : IDisposable
{
 delegate IntPtr Hook(int code, IntPtr message, IntPtr data);
 [DllImport("user32.dll", SetLastError = true)] static extern IntPtr SetWindowsHookEx(int id, Hook callback, IntPtr module, uint thread);
 [DllImport("user32.dll")] static extern IntPtr CallNextHookEx(IntPtr hook, int code, IntPtr message, IntPtr data);
 [DllImport("user32.dll")] static extern bool UnhookWindowsHookEx(IntPtr hook);
 [DllImport("user32.dll")] static extern short GetAsyncKeyState(int key);
 [DllImport("kernel32.dll", CharSet = CharSet.Unicode)] static extern IntPtr GetModuleHandle(string? name);
 readonly Hook callback; IntPtr hook; int key; bool ctrl, alt, shift, win, held;
 public event Action? Pressed; public event Action? Released;
 public GlobalHotkey(string shortcut) { callback = Handle; Configure(shortcut); hook = SetWindowsHookEx(13, callback, GetModuleHandle(null), 0); if (hook == IntPtr.Zero) throw new InvalidOperationException("Windows could not register the global keyboard hook."); }
 public static void Validate(string shortcut) => Parse(shortcut);
 static (int Key, bool Ctrl, bool Alt, bool Shift, bool Win) Parse(string shortcut)
 {
  var parts = shortcut.Split('+', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
  if (parts.Length < 2) throw new ArgumentException("Use a modifier and a key, for example Ctrl+Alt+Space.");
  foreach (var part in parts[..^1]) if (!new[] { "Ctrl", "Alt", "Shift", "Win" }.Contains(part, StringComparer.OrdinalIgnoreCase)) throw new ArgumentException("Modifiers must be Ctrl, Alt, Shift or Win.");
  if (!Enum.TryParse<Key>(parts[^1], true, out var parsed) || parsed == Key.None || parsed is Key.LeftCtrl or Key.RightCtrl or Key.LeftAlt or Key.RightAlt or Key.LeftShift or Key.RightShift or Key.LWin or Key.RWin) throw new ArgumentException("The last part must be a key, for example Space or F9.");
  return (KeyInterop.VirtualKeyFromKey(parsed), parts.Contains("Ctrl", StringComparer.OrdinalIgnoreCase), parts.Contains("Alt", StringComparer.OrdinalIgnoreCase), parts.Contains("Shift", StringComparer.OrdinalIgnoreCase), parts.Contains("Win", StringComparer.OrdinalIgnoreCase));
 }
 public void Configure(string shortcut) { var p = Parse(shortcut); key = p.Key; ctrl = p.Ctrl; alt = p.Alt; shift = p.Shift; win = p.Win; }
 static bool Down(int key) => (GetAsyncKeyState(key) & 0x8000) != 0;
 IntPtr Handle(int code, IntPtr message, IntPtr data)
 {
  if (code >= 0)
  {
   var vk = Marshal.ReadInt32(data); var down = message.ToInt64() is 0x100 or 0x104; var up = message.ToInt64() is 0x101 or 0x105;
   if (vk == key && down && (held || (Down(0x11) == ctrl && Down(0x12) == alt && Down(0x10) == shift && (Down(0x5B) || Down(0x5C)) == win))) { if (!held) { held = true; Pressed?.Invoke(); } return 1; }
   if (held && up && (vk == key || vk is 0xA0 or 0xA1 or 0xA2 or 0xA3 or 0xA4 or 0xA5 or 0x5B or 0x5C)) { held = false; Released?.Invoke(); if (vk == key) return 1; }
  }
  return CallNextHookEx(hook, code, message, data);
 }
 public void Dispose() { if (hook != IntPtr.Zero) UnhookWindowsHookEx(hook); hook = IntPtr.Zero; }
}
public static class TextDelivery
{
 [DllImport("user32.dll")] public static extern IntPtr GetForegroundWindow();
 [DllImport("user32.dll")] static extern uint GetClipboardSequenceNumber();
 [DllImport("user32.dll")] static extern short GetAsyncKeyState(int key);
 [StructLayout(LayoutKind.Sequential)] struct Input { public uint type; public InputUnion value; }
 [StructLayout(LayoutKind.Explicit)] struct InputUnion { [FieldOffset(0)] public Keyboard keyboard; [FieldOffset(0)] public Mouse mouse; }
 [StructLayout(LayoutKind.Sequential)] struct Mouse { public int x, y; public uint data, flags, time; public IntPtr extra; }
 [StructLayout(LayoutKind.Sequential)] struct Keyboard { public ushort vk, scan; public uint flags, time; public IntPtr extra; }
 [DllImport("user32.dll", SetLastError = true)] static extern uint SendInput(uint count, Input[] inputs, int size);
 static Input Key(ushort key, bool up = false) => new() { type = 1, value = new() { keyboard = new() { vk = key, flags = up ? 2u : 0u } } };
 public static async Task<string> Paste(string text, IntPtr target)
 {
  for (var attempt = 0; attempt < 80 && new[] { 0x10, 0x11, 0x12, 0x5B, 0x5C }.Any(k => (GetAsyncKeyState(k) & 0x8000) != 0); attempt++) await Task.Delay(25);
  if (new[] { 0x10, 0x11, 0x12, 0x5B, 0x5C }.Any(k => (GetAsyncKeyState(k) & 0x8000) != 0)) return "Saved — release modifier keys and use COPY to insert the transcript.";
  if (target == IntPtr.Zero || GetForegroundWindow() != target) return "Saved — window changed. Use COPY to insert the transcript.";
  var old = System.Windows.Clipboard.GetDataObject();
  System.Windows.Clipboard.SetText(text); var sequence = GetClipboardSequenceNumber();
  try
  {
   if (GetForegroundWindow() != target) return "Saved — window changed. Use COPY to insert the transcript.";
   Input[] inputs = [Key(0x11), Key(0x56), Key(0x56, true), Key(0x11, true)];
   if (SendInput(4, inputs, Marshal.SizeOf<Input>()) != 4) return "Saved — Windows blocked insertion. Use COPY; elevated apps may reject input.";
   await Task.Delay(500);
   return "Saved — paste sent to the original app. COPY is available if the app did not accept it.";
  }
  finally { if (GetClipboardSequenceNumber() == sequence) { if (old != null) System.Windows.Clipboard.SetDataObject(old, true); else System.Windows.Clipboard.Clear(); } }
 }
}
