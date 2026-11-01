# M16.5 — Results, official distance and evening scenery

2026-11-01. All five work packages implemented and qualified; **M16.5 OPEN / OWNER
TEST PENDING**. The owner explicitly closes M16.4. No subsequent milestone work.

Launch `Builds/M16_5_Results_Distance_Review/START_TEST.cmd` for the fullscreen
Windows development build. Existing careers are supported. Source decisions and
IBU discipline-length reference: [ADR-0082](../../adr/ADR-0082-m16-5-results-official-distance-and-night-scenery.md).

## Delivered behaviour

- Results show ten athletes per page, a compact result/shooting summary, aligned
  time/gap/points, flags, podium ranks and the player's accented row. The first
  page follows the player's position; explicit previous/next navigation then
  takes priority. Personal splits and return to career remain available.
- Preparation uses existing official Men/Women discipline profiles. Men's Sprint
  displays 10 km, Pursuit 12.5 km and Mass Start 15 km. Each physical lap maps to
  its official share: men's Mass Start boundaries are 3/6/9/12/15 km. Approved
  compressed geometry, elapsed time, power, fatigue and race balance are retained.
- Every timing gantry, including the reused stadium frame, displays official
  cumulative kilometres for the current passage. It holds the just-passed value
  for 20 physical metres, keeping it readable from the follow camera, then uses
  the next passage. Rewind restores earlier values. HUD notices and TAB history
  use the same mapping. TIMING is removed; the separate START gate is retained.
- Both crowd shaders and static venue Lit materials sample the existing evening
  illumination field. Source colours, textures, shader passes and shadows remain
  intact. Shared source materials are untouched; clones are owned by the venue.
  No extra realtime lights, shadow passes, rendering cameras or quality reduction.

## Qualification

**30 unique tests pass: 9 pure EditMode cases through .NET and 21 native PlayMode
cases.** No test deleted/disabled and no failing threshold relaxed.

| Receipt | Passed | Coverage |
| --- | ---: | --- |
| `core-final.trx` | 9 | Three formats × both canonical categories; per-lap boundaries, formatting, validation, allocation-free conversion; existing result ordering/points/source immutability |
| `m165_native_final.json` | 10 | Actual gantries/grounding, ten-row results/navigation/viewport, three real format routes/cumulative labels/passage hold/rewind, evening crowd pixels and preserved advert material |
| `m165_regression.json` | 11 | TAB history, actual venue floodlights/target plate lighting, weather binding and all three graphics tiers |

Red evidence is retained: `m165_red_results.json` reproduces five rows instead of
ten; `m165_lap_red.json` catches whole-route scaling that mislabels official lap
boundaries. The implementation now maps individual laps. Original immutable
track identity is retained when constructing CheckpointLayout; reserialization
was corrected rather than weakening its guard. An initial fixture's Unity
fake-null access was also corrected before final qualification.

Final actual Unity Console was inspected: 0 errors and 75 existing build warning
entries, then cleared; final 0 errors/0 warnings, idle outside Play Mode. All three
modified/new shaders are supported with zero shader errors (`shaders.txt`).

## Visual evidence

Results at RU 1600×900 and EN large-text 1024×768 were inspected. All ten rows,
headers and the pager fit. Test result records are deterministic UI fixtures,
not a claim about simulated race balance.

![Russian results, ten rows](results-ru.png)

![English large-text results, 1024×768](results-en-large.png)

The same actual roadside crowd, lamps off/on; native pixel checks measure its
torso-region luminance increase while preserving clothing colours.

![Evening crowd illuminated](crowd-evening-lit.png)

The `*-checkpoint-labels.txt` files retain every actual occurrence for each
format. Mass Start labels increase through 1.16/2.30/3.00, 4.16/5.30/6.00,
7.16/8.30/9.00, 10.16/11.30/12.00 and 13.37/14.70 km. The shorter final
physical branch is normalized to the same official lap share and finishes at
15 km; there is no invented final checkpoint outside the accepted route.

## Compiled player and performance scope

Unity 6000.6.0f1 build succeeded: **0 errors, 520 existing warning instances,
430,852,435 bytes, 32.725 seconds** (`build.json`). The inherited build receipt
retains the original builder's identifier; the delivery's `milestone.json`
identifies M16.5.

`startup-player/review.json` passes and exits 0: ordinary 2560×1440
FullScreenWindow, randomized Sprint start ordinal/bib 20, personal 3–2–1–0,
successful career/settings/recovery/checkpoint checks and owner profiles
unchanged. Loading paints all 15 substeps across four captured stages while
holding the clock, keeping the countdown unarmed and hiding the gameplay HUD.
Optional advanced navigation probes are not enabled or claimed.

`evening-player/review.json` passes and exits 0: six compiled evening cases,
two cameras × three quality tiers, 90 warmup + 240 recorded frames each
(1,440 recorded frames). Actual range/forest screenshots retained. Thirty real
athletes are held at accepted tick 608; 63 masts and 6 target fills remain.
Median wall times (ms):

| Camera | High | Balanced | Low |
| --- | ---: | ---: | ---: |
| Range | 1.350 | 1.355 | 1.234 |
| Forest | 1.530 | 1.551 | 1.470 |

Hardware: RTX 5080 / Ryzen 9 9950X3D / Direct3D11. The probe renders 1920×1080
offscreen and excludes live ticking, HUD/menu and swapchain. It is a scoped
evening validation, not a before/after cost comparison or whole-game/minimum-spec
FPS guarantee. Dedicated GPU timing is unavailable and reported as such. Full
samples and derived medians are retained in `performance-summary.json`.

## Authority, history and limits

All 722 task-entry Domain/Simulation/AI/Career/Replay source/meta files are
unchanged. Their five compiled DLLs are byte-identical to the preceding M16.4
Weather Lights build (`authority.json`). Presentation-only changes do not alter
physics, AI, power, scoring, elapsed time, RNG, replay events or save schemas.

`docs/PROJECT_STATE.md` retains its exact 3,162,191 task-entry bytes as a prefix;
new dated entries are appended. Unrelated pre-existing owner/Unity files are
excluded from the scoped commit. Source/asset fingerprints and historical-prefix
guards can be rechecked with:

```
python scripts/quality/report_m165.py
```

Existing fixed-field lighting remains a diffuse approximation; this work does
not add calibrated lux, volumetric transport or per-mast dynamic shadows. Next:
owner tests result pages, cumulative gates and evening crowd/advertising in this
fullscreen build before deciding whether to close M16.5.
