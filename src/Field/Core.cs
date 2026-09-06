using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
namespace Field;

public sealed class DictionaryEntry
{
 public Guid Id { get; set; } = Guid.NewGuid();
 public string Word { get; set; } = "";
 public string Heard { get; set; } = "";
 public string Display => string.IsNullOrWhiteSpace(Heard) ? Word + "   · vocabulary" : Heard + "  →  " + Word;
}
public sealed record Correction(string Before, string After);
public sealed class Transcript
{
 public DateTimeOffset Created { get; set; } = DateTimeOffset.Now;
 public string Raw { get; set; } = "";
 public string Text { get; set; } = "";
 public List<Correction> Corrections { get; set; } = [];
 public string Stamp => Created.ToLocalTime().ToString("dd MMM yyyy  /  HH:mm:ss");
 public string CorrectionSummary => Corrections.Count == 0 ? "No dictionary corrections" : "DICTIONARY  ·  " + string.Join("  ·  ", Corrections.Select(c => $"“{c.Before}” → “{c.After}”"));
}
public sealed class Settings
{
 public string EnginePath { get; set; } = "";
 public string ModelPath { get; set; } = "";
 public string Hotkey { get; set; } = "Ctrl+Alt+Space";
 public bool HoldToTalk { get; set; } = false;
}
public enum ShortcutAction { None, Start, Stop }
public static class RecordingShortcut
{
 public static ShortcutAction Decide(bool pressed, bool holdToTalk, bool recording, bool hotkeySession, bool busy)
 {
  if (busy) return ShortcutAction.None;
  if (pressed) return recording ? (!holdToTalk && hotkeySession ? ShortcutAction.Stop : ShortcutAction.None) : ShortcutAction.Start;
  return holdToTalk && recording && hotkeySession ? ShortcutAction.Stop : ShortcutAction.None;
 }
}
public static class Storage
{
 public static readonly string Folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Field");
 public static string PathFor(string name) => Path.Combine(Folder, name + ".json");
 static readonly JsonSerializerOptions Options = new() { WriteIndented = true, PropertyNameCaseInsensitive = true };
 public static T Read<T>(string name, Func<T> fallback)
 {
  Directory.CreateDirectory(Folder);
  return File.Exists(PathFor(name)) ? JsonSerializer.Deserialize<T>(File.ReadAllText(PathFor(name)), Options) ?? throw new InvalidDataException(name + " is empty.") : fallback();
 }
 public static void Save<T>(string name, T data)
 {
  Directory.CreateDirectory(Folder);
  var temp = PathFor(name) + ".tmp";
  File.WriteAllText(temp, JsonSerializer.Serialize(data, Options), new UTF8Encoding(false));
  File.Move(temp, PathFor(name), true);
 }
}
public static class DictionaryRules
{
 static readonly HashSet<string> Common = new("a an and are as at be been but by can cloud code do for from go had has have he her here him his how i if in is it its like may me my no not of on one or our out say she so some that the their them then there these they this time to up us use was we were what when which who will with word would you your".Split(' '), StringComparer.OrdinalIgnoreCase);
 public static string Warning(DictionaryEntry entry)
 {
  var source = string.IsNullOrWhiteSpace(entry.Heard) ? entry.Word : entry.Heard;
  if (Common.Contains(source.Trim()) || source.Trim().Length < 3) return "CAUTION — This matches a common or short word. Every whole-word occurrence will be replaced. Use a fuller phrase if possible.";
  return "Full phrases only. Spaces and hyphens between parts are optional; surrounding words are protected.";
 }
 static string Pattern(string text)
 {
  text = Regex.Replace(text.Trim(), @"(?<=\p{Ll})(?=\p{Lu})", " ");
  var parts = Regex.Split(text, @"[\s\-\u2010-\u2015]+").Where(p => p.Length > 0).Select(Regex.Escape);
  return @"(?<![\p{L}\p{N}_])" + string.Join(@"[\s\-\u2010-\u2015]*", parts) + @"(?![\p{L}\p{N}_])";
 }
 public static Transcript Apply(string raw, IEnumerable<DictionaryEntry> entries)
 {
  var rules = new List<(string Source, string Target)>();
  foreach (var entry in entries.Where(e => !string.IsNullOrWhiteSpace(e.Word)))
  {
   rules.Add((string.IsNullOrWhiteSpace(entry.Heard) ? entry.Word : entry.Heard, entry.Word.Trim()));
   // A narrow, explicit homophone alias; never matches “cloud” by itself.
   if (string.IsNullOrWhiteSpace(entry.Heard) && entry.Word.Trim().Equals("Claude Code", StringComparison.OrdinalIgnoreCase)) rules.Add(("cloud code", entry.Word.Trim()));
  }
  var sorted = rules.OrderByDescending(r => r.Source.Length).ToArray();
  var result = new Transcript { Raw = raw, Text = raw };
  if (sorted.Length == 0) return result;
  var expression = string.Join("|", sorted.Select((r, i) => $"(?<r{i}>{Pattern(r.Source)})"));
  // One pass over the original text prevents corrections cascading into other rules.
  result.Text = Regex.Replace(raw, expression, match =>
  {
   for (var i = 0; i < sorted.Length; i++) if (match.Groups["r" + i].Success)
   {
    var replacement = sorted[i].Target;
    if (match.Value != replacement) result.Corrections.Add(new(match.Value, replacement));
    return replacement;
   }
   return match.Value;
  }, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1));
  return result;
 }
 public static string Context(IEnumerable<DictionaryEntry> entries)
 {
  // A bounded vocabulary prompt, not a transcript or instructions. Never include heard forms.
  var words = new List<string>(); var length = 0;
  foreach (var word in entries.Select(e => e.Word.Trim()).Where(w => w.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase))
  {
   if (words.Count == 24) break;
   if (length + word.Length + 2 > 240) continue;
   words.Add(word); length += word.Length + 2;
  }
  return string.Join(", ", words);
 }
}
