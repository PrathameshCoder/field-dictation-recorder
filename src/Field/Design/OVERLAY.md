# FIELD — orb overlay revision

The user's supplied pill screenshot and `the original supplied orb.txt` supersede the earlier industrial palette for this revision. Shared native tokens remain in Tokens.xaml; the pill's tokens are in OverlayTokens.xaml. DarkControls.xaml applies those shared colors to selectors and tabs.

Pill: 180 × 64 DIP, radius 32, #111214 surface, #303136 seam. Centered orb canvas: 50 DIP. Visible status text is replaced by a 24 DIP high, 17-bar voice envelope (#91DCE8, 2 DIP stroke). Bottom offset: 48 DIP above the active monitor's working-area edge. Results remain for 2.5 seconds. The orb's internal shader colors and 220/650ms transitions come directly from the supplied HTML.

Orb.html contains the original WGSL shader and both original state presets. Integration changes only the transparent page background, the host state bridge, a subtle input-level radius response, a 30fps cap, and pause/resume when hidden. There are no external scripts or assets. The local page is served directly from embedded bytes through WebView2's HTTPS request handler; the source orb.txt is unchanged.

Listening uses the supplied idle state; transcription uses thinking. Saved, Done, No speech, Ready to copy and failure states return to idle and automatically hide. Windows styles prevent activation, pointer capture and taskbar appearance. Normal desktop apps and borderless fullscreen are supported; Windows secure desktop and exclusive fullscreen can obscure overlays.

Rendering requires Microsoft's WebView2 Runtime with WebGPU support. If unavailable, the status pill remains usable with a static fallback orb. Browser data lives under %LOCALAPPDATA%/Field/WebView2, never beside the executable. The renderer pauses when hidden and is disposed on app exit.


DPI fix: the overlay uses its actual native window dimensions for positioning and never resizes itself using another process''s DPI. Per-monitor V2 awareness is explicit. Voice bars show recent measured input levels, not fabricated spectrum data; processing uses a quiet fixed baseline while the orb changes state. Textual status remains in the main window and accessibility name.
