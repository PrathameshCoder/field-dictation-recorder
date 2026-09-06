<div align="center">

# FIELD
**Your voice. Your words. On your machine.**

A Windows dictation app with a liquid orb, a live waveform, and local speech recognition.

**Windows 11 · C# / WPF · .NET 10 · whisper.cpp**

</div>

---

### Speak anywhere

Click a text field, tap **Ctrl + Alt + Space**, speak, then tap again. FIELD transcribes locally and pastes into the original app. A compact, non-activating pill shows recording and processing without stealing focus.

- **A real desktop app** — searchable history, copy controls, Settings on `Ctrl+,`, and a secondary tray icon.
- **A personal dictionary** — vocabulary hints plus explicit phrase corrections, with a record of every replacement.
- **Designed to stay out of the way** — close the window to keep dictating; use tray → Exit to quit.
- **Local by design** — no account, API key, cloud transcription, or telemetry code.

### Build & run

Requires Windows, the [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0), and [Microsoft Edge WebView2 Runtime](https://developer.microsoft.com/microsoft-edge/webview2/). The animated orb needs WebGPU; a static fallback is available.

```powershell
git clone https://github.com/PrathameshCoder/field-dictation-recorder.git
cd field-dictation-recorder
.\Build.ps1
```

Open `dist\Field.exe`. In Settings, select an extracted [whisper.cpp](https://github.com/ggml-org/whisper.cpp/releases) `whisper-cli.exe` and a compatible [Whisper GGML model](https://huggingface.co/ggerganov/whisper.cpp). Keep the engine's DLLs beside its executable. NVIDIA builds can use CUDA. Hold-to-talk is also available in Settings.

### How it works

`Shortcut → microphone → whisper.cpp → dictionary corrections → history + paste`

WPF handles the desktop app and Windows integration. WebView2 renders the supplied liquid-orb shader. Settings, history, and the editable dictionary live in `%LOCALAPPDATA%\Field`; temporary recording files are removed after processing. Stored text is unencrypted. Downloads, models, recordings, local state, and build outputs are excluded from this repository.

If focus changes, FIELD preserves the transcript for manual copying. Elevated apps may reject automatic insertion. Recognition can still make mistakes; dictionary biasing and wider beam search are not guarantees of accuracy.

### Verification

```powershell
dotnet run --project tests/Field.Tests -c Release
```

The suite covers dictionary boundaries, correction tracking, shortcut modes, and settings compatibility. Optional `-- --overlay` and `-- --lifetime` checks open test windows to exercise rendering, focus preservation, and tray lifecycle. Design specifications live in [`src/Field/Design`](src/Field/Design).

### Built by a human, with AI assistance

Built by **[Prathamesh](https://github.com/PrathameshCoder)** with implementation and debugging assistance from **OpenAI Codex**. I defined the product, supplied the visual direction, tested it in everyday use, and drove the iterations. This project documents how I use AI to turn an idea into working software—with explicit requirements, local data handling, scoped corrections, and verification before shipping.

Dependencies and supplied visual assets retain their respective rights. This repository does not bundle whisper.cpp, model weights, or Microsoft's runtime.
