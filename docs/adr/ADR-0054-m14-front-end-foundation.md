# ADR-0054 — M14 front end and career profile boundary

2026-10-11. Implemented first slice of WP-179/WP-180. M14 is in progress.

The master plan starts M14 with UI architecture/design tokens, navigation,
localization and the main menu/profile flow. A new Unity-free
`Biathlon.Presentation` assembly owns front-end transitions and the validated
career boundary. Unity UI Toolkit presents that state through a retained
`UIDocument`; it does not advance simulation ticks, career weeks or race results.

The first scene provides New Career, Continue, Settings and Quit; editable name
and nation; a small career overview and upcoming event; Russian/English text and
100/115/130 percent text sizes. Localization and theme tokens are versioned
resources with stable IDs. The interface uses snow/ink/teal colors and five
target circles, with visible focus, unavailable-action state and error messages.

New profiles call `CareerDefinitions.ForNewProductionCareer` and `CareerFactory`.
Continuation calls `ForSave` and restores the saved calendar and RNG state;
legacy careers are never silently migrated. An injected profile store writes
validated UTF-8 to a unique temporary file, flushes it, then publishes it with a
non-overwriting move. The UI publishes the new career only after this succeeds.
Independent profiles live under `Application.persistentDataPath/Profiles`.
Continue selects the latest career file; a corrupt file is preserved and shown
as a readable error rather than reset. Profile selection is a later extension.

Unity 6000.6 native InputForUI handles mouse, keyboard and gamepad through the
project UI action map. `PanelInputConfiguration.Never` keeps this screen on the
native UI Toolkit route. Up/down navigation skips disabled actions and preserves
the caller's focus on Back; native submit/cancel events own actions. No gameplay
device polling or global Input System setting change is added to the product.
Synthetic tests explicitly pump Unity's provider and dispatcher in an unfocused
batch Editor; that exception exists only in the test harness.

`FrontEndAuthoring.Generate` imports and validates the scene in a preview, so it
does not replace open scenes or edit build settings. A menu camera is created
only if the scene has none. UI preferences use PlayerPrefs; career state uses
the existing versioned career JSON and remains independent.

Acceptance covers validated save/restore, failed-write publication, legacy
calendar preservation, independent profiles, localization, actual menu flow,
native gamepad and keyboard events, focus and two rendered aspect/text sizes.
This is not the full M14 gate. WP-181 pre-race, race/result/next-event routing,
expanded settings/dashboard, replay and broadcast UI remain next work. No new
player build or frame-rate claim is part of this slice.
