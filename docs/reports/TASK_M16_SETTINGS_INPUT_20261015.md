# M16 — settings and persistent controls

2026-10-15. WP-210 / WP-211 implementation report. Native/build acceptance is recorded by the M16 delivery report.

## Product behavior

The approved FROSTLINE menu now separates Display, Audio, Controls, Accessibility and Saves & support. Each page has its own focused content; bindings use skiing/shooting/interface groups and five rows per page (three in a compact view or at 130% text). Optional aiming preferences expand separately, so binding pagination and cancellation remain visible. Existing native UI Toolkit keyboard/gamepad navigation remains available. The product retains the accepted audio mixer, visual quality tiers and fullscreen startup.

Controls can be assigned independently for keyboard/mouse and gamepad. Selecting a binding enters capture mode; Esc or Start cancels without changing preferences. Conflicts within the active context, including global HUD shortcuts, are rejected with the conflicting action's title. Default controls can be restored without resetting graphics, sound, accessibility or aiming preferences. Menu navigation/cancel controls remain standard to avoid trapping the player in a custom mapping.

The finite, versioned `input_bindings_v1.json` catalog contains stable action IDs and allowed typed input paths. Defaults retain W/S pace and braking, A/D steering, Shift attack, Ctrl recovery, Space tuck/breath, RMB bolt and LMB fire. Gamepad defaults also retain their existing command semantics. Analog controls are read without processors and continue through the existing command builder's deadzone/effort mapping. The adapter does not change race physics or athlete capability.

The career race receives a detached preference snapshot. The skiing/shooting adapters, HUD shortcuts and range overview use that snapshot. Input control references are resolved when devices change; ordinary sampling creates no InputActions or control lists. Pre-race instructions, help, pace/burst guidance and breath key labels reflect the selected bindings. Sensitivity and vertical inversion affect input intent only; shot geometry remains authoritative and unchanged.

## Persistence and recovery

`Settings/settings_v1.json` is separate from career schema v3. It stores locale, text size, contrast, reduced motion, graphics tier, six audio volumes, quiet/background audio choices, selected input device, sensitivity, inversion and explicit binding overrides. Values, action IDs, device/kind compatibility and binding conflicts are validated before publication.

On first use, valid legacy PlayerPrefs choices are migrated; compatibility keys remain mirrored for existing presentation adapters. Isolated review menus (`PersistPreferences=false`) do not write owner preferences. Invalid/future settings records are retained and are not automatically overwritten during loading.

Writes use a unique temporary file, flush, exact deserialize/validate verification, process lock and atomic replacement of the published file with a previous-good backup. Loading ignores incomplete temporary files and can use the verified backup. A damaged primary is quarantined on an explicit subsequent save rather than copied over the good backup. Failure leaves a readable UI message and the last good published settings.

Final review reproduced a publication mismatch by holding the actual settings lock: the device dropdown displayed Gamepad while the persisted/input record still selected keyboard. The added native regression was run against the old callbacks and failed exactly at the dropdown assertion (`Expected 0; But was 1`). All callbacks now stage detached candidates; one successful publication applies the validated settings, language/text/accessibility fields and audio mix only after the file write succeeds. A failed write rebuilds the controls from the last good record. The regression also checks all six volume controls, audio runtime, locale/text/contrast/motion, graphics, sensitivity/inversion, bindings/reset and an unlocked retry, using an isolated temporary settings directory.

The shared Unity-free `JsonDocumentSyntax.Validate` guard rejects truncated roots, trailing extra roots and duplicate/escaped-duplicate object fields before framework deserialization. The established career syntax boundary now delegates to the same implementation; no career serializer or schema format changes are introduced by this extraction.

## Verification

- `.NET Release build`: zero errors, zero warnings.
- `PlayerSettingsTests`: 9/9 PASS. Coverage includes approved defaults, both-device persistence, context/global conflicts, reserved controls, type validation, reset isolation, corrupt-primary backup recovery, preservation of invalid/future records, incomplete temporary files, locked publication, invalid settings and strict JSON.
- `PlayerSettingsTests | CareerProfile*`: 38/38 PASS; `artifacts/m16/settings/settings-career-json.trx`.
- With `CareerSaveMigrationFixtureTests` included: 53/53 PASS; `artifacts/m16/settings/settings-career-migrations.trx`. This is an overlapping broader run, not 53 additional unique tests.
- Initial root native qualification passed `PlayerSettingsInputTests` 6/6, `PlayerDeviceInputTests` 8/8, `ShootingDeviceInputTests` 4/4 and `M15AudioTests` 10/10. Coverage includes real control path resolution, actual remapped skiing and shooting commands, sensitivity/inversion, UI conflict/cancel/reset and actual keyboard/gamepad capture, including B/circle without accidentally navigating Back. These are actual native results reported by the root agent; they are not inferred from the .NET run. The final failed-publication regression expands the settings fixture to seven tests and requires requalification of it plus audio after the final callback correction; root delivery evidence records that final result.
- The visual fixture captures seven rendered page views, waits for the actual FROSTLINE opacity transition, then asserts visible body/page/control bounds before capturing RU 1600×900 and EN 1024×768 at 130% text. Initial inspection identified clipped binding pagination in the compact view; the final layout adds a collapsed aiming section and three compact rows. The expanded assertion now requires reset/cancel controls to fit the viewport, and the root confirmed the corrected compact view fits. Unsupported PlayStation symbol glyphs use readable ASCII names (Square, Circle, Triangle, Cross).

## Source manifest

`docs/quality/m16/settings/source-manifest.json` lists all 23 source/resource/test files with UTF-8/LF SHA-256 and associated Unity metadata paths. `FrontEndMenu.cs` also contains the root's reliability hooks; it is intentionally shared integration rather than an independently owned replacement. The native PNGs and this report are additional evidence files, not gameplay definitions.

## Boundaries

Race/career save bytes, RNG, ratings, training, AI decisions and shooting geometry are not settings. Rebinding changes the user's command source, not recorded commands or historical replay authority. Existing replay/spectator transport and standard menu navigation remain their accepted controls. Display quality changes apply to the next race; this work does not lower Ultra textures or shadows. The preferences record supports the current RU/EN product locales and existing text sizes.

## Final root qualification

Final native settings7/7 PASS, including the actual locked-file publication regression (RED before correction, GREEN after it); audio10/10, skiing8/8 and shooting4/4 PASS. Final nine pure settings cases also PASS natively. Recovery/support UI4/4 and existing front-end interaction10/10 PASS, including actual career race launch and pause/abandon. Compact/large-text PNGs were inspected; pagination/reset/cancel fit. Fullscreen Windows candidate acceptance also retained the binding through the real settings file and applied its detached snapshot to the production host. No owner career was changed by this isolated probe.
