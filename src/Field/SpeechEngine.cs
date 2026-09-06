using System.Diagnostics;
using System.IO;
namespace Field;
public interface ISpeechEngine
{
 Task<string> TranscribeAsync(string wav, string context, Settings settings, CancellationToken cancellation);
}
public sealed class WhisperCliEngine : ISpeechEngine
{
 public async Task<string> TranscribeAsync(string wav, string context, Settings settings, CancellationToken cancellation)
 {
  if (!File.Exists(settings.EnginePath)) throw new InvalidOperationException("Choose whisper-cli.exe in Settings first.");
  if (!File.Exists(settings.ModelPath)) throw new InvalidOperationException("Choose a Whisper model in Settings first.");
  var output = Path.Combine(Path.GetDirectoryName(wav)!, "transcript");
  var info = new ProcessStartInfo(settings.EnginePath) { UseShellExecute = false, CreateNoWindow = true, RedirectStandardError = true, RedirectStandardOutput = true, WorkingDirectory = Path.GetDirectoryName(settings.EnginePath)! };
  foreach (var arg in new[] { "-m", settings.ModelPath, "-f", wav, "-otxt", "-of", output, "-nt" }) info.ArgumentList.Add(arg);
  // A wider beam considers more competing phrases; costs some decoding time.
  info.ArgumentList.Add("-bs"); info.ArgumentList.Add("8");
  if (context.Length > 0) { info.ArgumentList.Add("--prompt"); info.ArgumentList.Add(context); }
  using var process = Process.Start(info) ?? throw new InvalidOperationException("Could not start the speech engine.");
  using var cancel = cancellation.Register(() => { try { if (!process.HasExited) process.Kill(true); } catch (InvalidOperationException) { } });
  var stderr = process.StandardError.ReadToEndAsync(cancellation);
  var stdout = process.StandardOutput.ReadToEndAsync(cancellation);
  await process.WaitForExitAsync(cancellation);
  var error = await stderr; await stdout;
  if (process.ExitCode != 0) throw new InvalidOperationException("Speech engine failed: " + error[..Math.Min(600, error.Length)]);
  if (!File.Exists(output + ".txt")) throw new InvalidOperationException("The speech engine returned no transcript file.");
  return (await File.ReadAllTextAsync(output + ".txt", cancellation)).Trim();
 }
}
