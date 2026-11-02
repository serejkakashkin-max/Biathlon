# M16.6.8 — Timing readability and BIATHLON: PULSE, 2026-11-02

Owner request: first stadium split digits are almost hidden by the header; the shared gantry must be blank at launch. Public game titles: BIATHLON: PULSE / Биатлон: Пульс гонки.

## Diagnosis and result

Initial regression: 2/2failed on the reported symptoms (native-reproduction.json). The shared gantry showed a future lap-end distance at route0. Label centre4.3m/character-size0.18placed the lower glyph bounds around3.6565m, below the actual header4.15–5.05m.

Authored geometry now puts the text at4.6m, character-size0.075, face offset0.46m. It fits inside the actual panel and in front of the opaque layers. The shared stadium sign is blank until its first actual split occurrence, including replay seek back to launch. Other course signs and subsequent cumulative labels retain their established behavior/20mhold. Binding resets the label cache explicitly so a reused view clears correctly.

Native camera renders use the exact six production gantry meshes and the production depth-tested font material, isolated from other venue objects. portal-blank/distance-25m/12m.png show the blank and populated states. The pixel regression compares both states and requires visible bright text; geometric checks enforce panel containment. These are isolated native portal renders, not full-venue screenshots.

## Game title

The localized title replaces the old menu brand and the visible FROSTLINE style label. It fits narrow/large-text navigation; FROSTLINE remains the internal visual style name. Windows captions use the official localized title through the native window API. The executable, product/storage identifier and existing directories remain stable so profiles/settings/replays stay discoverable. Build launcher/readme use the new public title.

A first standalone probe exposed that Process.MainWindowHandle excludes hidden review windows. The implementation now finds the process-owned UnityWndClass window directly, including hidden windows. The rebuilt standalone probe passes both localized caption applications. The first failed probe is not acceptance evidence.

## Qualification

- 11nativePlay portal casesPASS: old placement/corridor tests, all3race-format cumulative labels/seek/noalloc,3new blank/containment/render regressions (native-portals.json).
- 19nativePlay menu casesPASS, including real race launch/pause/abandon and localized title/persistence identity (native-menu.json). Final2affected title/results cases rerunPASS (native-menu-final.json), subset of these19.
- 27.NETRelease distance/session/localization testsPASS (pulse-regression.trx).
- 1nativeEdit panel testPASS. Total58unique focused tests.
- RU/EN1024x768at130%title screenshots and1600x900career pages inspected. Console0Error/Exception/Assert after resolved compile errors were cleared; editor not compiling/updating.
- Standalone2560x1440FullScreenWindowPASS/exit0;6actualmenu captures,18artresources bound,bothofficialtitlesapplied,legacyproductBiathlon,ownerprofilefingerprintunchanged/errors[]. This probe is menu-only; nativePlay separately verifies race startup.
- FinalBuildSucceeded0errors/520warnings,477666024bytes,18.8549751s. Source/binary hashes in delivery.json. All25ProjectSettings assets match pre-build bytes; temporary URP binding restored from exact backup after each build.

Review: Builds/M16_6_8_Pulse_Review/START_TEST.cmd.
No simulation, career/economy, timing authority, official distances, race duration, RNG, save schema or replay command changes. M16.6remainsOPEN for ownerreview;M17notstarted. The complete race suite was not repeated for this presentation-only task.
