using System.IO;
using System.Runtime.InteropServices;
namespace Field;
// Windows multimedia capture; PCM16 mono at 16 kHz, directly usable by whisper.cpp.
public sealed class AudioCapture : IDisposable
{
 [StructLayout(LayoutKind.Sequential)] struct Format { public ushort tag, channels; public uint rate, bytes; public ushort align, bits, size; }
 [StructLayout(LayoutKind.Sequential)] struct Header { public IntPtr data; public uint length, recorded; public IntPtr user; public uint flags, loops; public IntPtr next, reserved; }
 delegate void Callback(IntPtr handle, uint message, IntPtr instance, IntPtr param1, IntPtr param2);
 [DllImport("winmm.dll")] static extern uint waveInOpen(out IntPtr handle, uint device, ref Format format, Callback callback, IntPtr instance, uint flags);
 [DllImport("winmm.dll")] static extern uint waveInPrepareHeader(IntPtr handle, IntPtr header, uint size);
 [DllImport("winmm.dll")] static extern uint waveInUnprepareHeader(IntPtr handle, IntPtr header, uint size);
 [DllImport("winmm.dll")] static extern uint waveInAddBuffer(IntPtr handle, IntPtr header, uint size);
 [DllImport("winmm.dll")] static extern uint waveInStart(IntPtr handle);
 [DllImport("winmm.dll")] static extern uint waveInReset(IntPtr handle);
 [DllImport("winmm.dll")] static extern uint waveInClose(IntPtr handle);
 readonly Callback callback; readonly object gate = new(); readonly MemoryStream audio = new(); readonly List<(IntPtr Header, IntPtr Data)> buffers = [];
 IntPtr handle; volatile bool running; public double Level { get; private set; } public bool HasSpeech { get; private set; }
 public AudioCapture() { callback = Receive; }
 static void Check(uint result) { if (result != 0) throw new InvalidOperationException("Microphone error " + result + ". Check Windows microphone permissions and your default input device."); }
 public void Start()
 {
  var format = new Format { tag = 1, channels = 1, rate = 16000, bytes = 32000, align = 2, bits = 16 };
  Check(waveInOpen(out handle, uint.MaxValue, ref format, callback, IntPtr.Zero, 0x30000));
  running = true;
  for (var i = 0; i < 4; i++)
  {
   var data = Marshal.AllocHGlobal(3200); var pointer = Marshal.AllocHGlobal(Marshal.SizeOf<Header>());
   Marshal.StructureToPtr(new Header { data = data, length = 3200 }, pointer, false);
   buffers.Add((pointer, data)); Check(waveInPrepareHeader(handle, pointer, (uint)Marshal.SizeOf<Header>())); Check(waveInAddBuffer(handle, pointer, (uint)Marshal.SizeOf<Header>()));
  }
  Check(waveInStart(handle));
 }
 void Receive(IntPtr device, uint message, IntPtr instance, IntPtr pointer, IntPtr extra)
 {
  if (message != 0x3C0) return;
  lock (gate)
  {
   var header = Marshal.PtrToStructure<Header>(pointer);
   if (header.recorded > 0)
   {
    var bytes = new byte[header.recorded]; Marshal.Copy(header.data, bytes, 0, bytes.Length); audio.Write(bytes);
    double sum = 0;
    for (var i = 0; i + 1 < bytes.Length; i += 2) { var sample = BitConverter.ToInt16(bytes, i) / 32768.0; sum += sample * sample; }
    Level = Math.Sqrt(sum / Math.Max(1, bytes.Length / 2));
    if (Level > 0.009) HasSpeech = true;
   }
   if (running) waveInAddBuffer(device, pointer, (uint)Marshal.SizeOf<Header>());
  }
 }
 public void Stop(string? wav)
 {
  running = false;
  if (handle != IntPtr.Zero) waveInReset(handle);
  lock (gate)
  {
   if (wav != null)
   {
    using var writer = new BinaryWriter(File.Create(wav)); var bytes = audio.ToArray();
    writer.Write("RIFF"u8); writer.Write(36 + bytes.Length); writer.Write("WAVEfmt "u8); writer.Write(16); writer.Write((short)1); writer.Write((short)1); writer.Write(16000); writer.Write(32000); writer.Write((short)2); writer.Write((short)16); writer.Write("data"u8); writer.Write(bytes.Length); writer.Write(bytes);
   }
   foreach (var buffer in buffers) { waveInUnprepareHeader(handle, buffer.Header, (uint)Marshal.SizeOf<Header>()); Marshal.FreeHGlobal(buffer.Header); Marshal.FreeHGlobal(buffer.Data); }
   buffers.Clear(); if (handle != IntPtr.Zero) { waveInClose(handle); handle = IntPtr.Zero; }
  }
 }
 public void Dispose() { Stop(null); audio.Dispose(); }
}
