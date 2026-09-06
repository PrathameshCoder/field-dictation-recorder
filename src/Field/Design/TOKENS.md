> Superseded palette: see OVERLAY.md and the current Tokens.xaml for the user-requested orb revision.

# FIELD / 01 — approved design system

Source: `Tokens.xaml`. All dimensions are device-independent pixels. This file records the approved design before further visual refinement.

| Category | Tokens |
|---|---|
| Surfaces | body #B8B5AD; panel #D5D1C7; control #292A28; instrument #E9E1CC; input #F0EDE5 |
| Text | primary #222320; secondary #55564F; inverse #F0EDE5 |
| Edges | seam #77786F; highlight #F5F1E8 |
| Signals | record #A52C27; amber #886018 |
| Fonts | Segoe UI / Arial; Consolas / Courier New for instruments |
| Type | label 11/16 semibold uppercase; caption 12/16; body 14/20; heading 20/28 semibold; counter 28/32 mono |
| Tracking | label 0.5; other 0 |
| Spacing | 4, 8, 12, 16, 24, 32, 48 |
| Radius | panel 0; control/input 2; dialog/card 4 |
| Border | standard 1; focus/instrument 2 |
| Controls | standard height 36; transport 48; icon 16 / 24 |
| Window | 1120×760; minimum 800×600; Settings 560×480 resizable |
| Depth | raised offset 0,2 blur 0 black 24%; floating offset 0,4 blur 12 black 20% |
| Pressed | reversed raised edges; 1px downward travel; no shadow |
| Material | warm aluminum; optional 2% horizontal grain; matte plastic, no gloss |
| Motion | press 70ms; transition 140ms; needle attack 80ms / release 300ms; easing (0.2,0,0,1) |

Red is the sole accent. Amber belongs to instrumentation and warnings. Use text or symbols alongside color. Native Windows title bars, menus and file dialogs retain platform behavior.

Implementation extensions: meter height 160, editor height 72, meter padding 16, needle width 2, meter stroke 1. Instrument drawing uses normalized geometry to scale with the available panel. No decorative gradients, glow or neon.

Current controls change physical state immediately. Optional aluminum grain is omitted. The meter uses attack/release smoothing. Text tracking and specialized bevel rendering remain polish work; the shared tokens reserve their approved values.
