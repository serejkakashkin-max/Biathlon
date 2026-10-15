# M15.6 evidence audit — 2026-10-15

This is a **pre-build audit checkpoint**, not a release approval. It records
saved evidence inspected at 12:57 UTC and the subsequent completed core run.
The three-season native UI fixture passed; final creator authoring and the
Windows build were still being qualified. M15.6 remains open
for owner review; M16 has not started.

## Counting method

Native counts use actual `Results[].Name/Status` rows, de-duplicated by complete
case name, including parameter values. The runner's `Summary.TotalTests` is the
assembly inventory and is **never** counted as executed tests. For the long
Course/clock run, the authoritative evidence is the completed NUnit XML: its
12 test-case rows passed in 339.775536 seconds, despite the MCP transport's
300-second timeout.

The foundation checkpoint in `quality/m15-6/verification.json` contains 94
unique native passes (57 EditMode, 37 PlayMode). Combining that checkpoint with
the selected later saved files below gives **158 unique executed cases: 154
passed, 4 failed, 0 skipped** — 107 EditMode passes and 47 PlayMode passes plus
4 PlayMode failures. This is cumulative evidence across checkpoints, **not** a
single final-source suite run. The case list and file provenance are retained in
`quality/m15-6/evidence-audit-checkpoint.json`.

The obsolete EditMode `WarmRatingProjectionAllocatesNoManagedMemory` case is
excluded: the Mono allocation API could return zero without measuring real
allocations. Its replacement PlayMode case uses actual `GC.Alloc` recording
with a positive control and is included in the foundation count.

## Selected native checkpoints

All paths in this table are relative to `docs/quality/m15-6/`. Repeated cases
are counted once in the aggregate above; the table intentionally shows each
fixture's actual execution count.

| Saved evidence | Passed | Failed | Interpretation |
|---|---:|---:|---|
| `verification.json` foundation cases | 94 | 0 | Earlier source checkpoint `cbd77cb8`; 57 EditMode and 37 PlayMode |
| `presets-final.json` | 9 | 0 | Seven earlier cases repeated; two additional allocation/geometry cases |
| `growth-final.json` | 11 | 0 | Individual ceilings, responses, ageing, training, migration, history |
| `world-native-tests.json` | 5 | 0 | Persistent world; includes 15 actual annual transitions |
| `generation-final.json` | 3 | 0 | Individual legacy-calendar retirement plus existing generation compatibility |
| `summary-final.json` | 6 | 0 | Actual abilities/readiness, observations, peak and recorded counters |
| `appearance-core-final.json` | 5 | 0 | Versioned cosmetic values and validation |
| `cards-final.json` | 2 | 0 | Native dossiers, navigation, filters, RU/EN, text scale and captures |
| `cards-three-seasons-native.xml` | 2 | 0 | Same two cases; extended fixture now seals three annual boundaries and captures four saved observations |
| `studio-final.json` | 4 | 0 | Reused rig/resources and preview/race appearance parity |
| `start-final.json` | 6 | 0 | Three starting levels and shared projected readiness |
| `creator-ui-final.json` | 2 | 0 | Own-athlete and existing-athlete creation paths |
| `appearance-second.json` | 5 | 0 | Intermediate expanded cosmetic authoring; later art still needs acceptance |
| `rifle-height-first-red.json` | 3 | 4 | Baseline height passed; short/tall skiing and tuck attachment failed |
| `legacy-growth-adaptation-tests.json` | 2 | 0 | Actual adaptation regression evidence archived from artifacts |
| `legacy-growth-ageing-tests.json` | 1 | 0 | Actual ageing regression evidence archived from artifacts |
| `legacy-growth-ai-training-tests.json` | 2 | 0 | Shared AI/player planning and recovery regression evidence |
| `legacy-growth-season-tests.json` | 1 | 0 | Weekly accounting/rollback regression evidence |
| `course-clock/native-tests.xml` | 12 | 0 | Completed native reproduction, edge sweep and full MassStart repeat |

The four rifle failures are still open in this audit. Their assertions require
stock/back surface gaps below 18 mm; recorded short/tall failures reach roughly
50–247 mm. WP-D cannot be marked complete from this evidence. A later passing
run must replace the selected status explicitly while preserving this red file.

## CoreCLR and race evidence

| Evidence | Actual result | Scope |
|---|---|---|
| `dotnet/final-44.trx` and `dotnet/catalog-and-pack.trx` | 44/44 and 9/9 passed | Foundation checkpoint; earlier source |
| `artifacts/m15-6/dotnet/final-career-core.trx` | 174 passed, 1 failed, 175 executed | Broad core run retained as red evidence; failure was the saved-career MassStart course continuation |
| `artifacts/m15-6/dotnet/m15-6-final-core.trx` | 142/142 passed, 0 skipped | Later current-source broad selection; includes the formerly failing saved-career MassStart case |
| `course-clock/course-clock-final-focused-regression.trx` | 67/67 passed | Relevant course/controller/clock regressions after the final continuation fix |
| `course-clock/owner-course-actual-and-edge-certified-cells.trx` | 13/13 passed | Includes the formerly failing MassStart case; two full 30-athlete races after save/load, 600 geometric shots per run |
| `authoritative-races/complete-presets.trx` | 9/9 passed | Final 24-point preset definitions and valid shooting geometry |
| `authoritative-races/final-authority-summary.json` | 22 cases passed | **19 full Sprint runs plus 3 MassStart opening samples**, 5700 shots, 548954 ticks, 8580 stat projections and exact repeated result |
| `authoritative-races/final-opening-observations.json` | 3 opening repeats passed | Additional 4273 diagnostic ticks; actual positive-grade stance observations |
| `authoritative-races/final-validation.json` | PASS, no errors | Final validator; maximum observed stationary interval 4.45 s |

The 175-case file must not be described as an all-green run. Its failed case has
later green evidence, including the final 142-case selection; that is different
from rerunning the complete 175-case selection. Likewise, 22 race-study cases must not be called 22 complete races.
The full MassStart repeat is separate from the three opening balance samples.

The race-study's three fixed seeds compare genuine stat allocations and shared
authority physics. They do not establish a universal player win rate, overall
preset ranking or suitability in every format. Scripted player shooting/control
is an upper-skill control, not a sample of human accuracy. The opening observer's
initial red validator counted downhill tuck inside a mixed-gradient distance
window as uphill tuck; final observation uses actual grade without changing
gameplay, and the red validator file is retained.

The Course/clock fix changes an uncertified curved-edge continuation. The
whole-world proof remains mandatory. This is a material deterministic context
change (`v26`): the published new full-race result hash differs from the old
physics result. It must not be described as preserving the former race hash or
as merely adding diagnostics.

## A–I coverage and remaining acceptance

| WP | Evidence-backed state at audit | Remaining acceptance / limitation |
|---|---|---|
| A | Audited design, architecture and migration contracts documented in ADR-0070/0071 | Keep final implementation/build provenance and append-only production history |
| B | Read-only OVR from 13 real abilities, grouped projections, native allocation positive control | OVR is a display projection, not an extra race-power multiplier |
| C | Three levels, six presets/origins, bounded budget, refunds, deterministic hidden profile and durable publication | Final presets now spend the same 24-point budget; final documentation must supersede older tuning checkpoint |
| D | Real reusable G2 studio, live face/body variation, saved preview/race appearance | Four short/tall rifle cases red; final eyes/hair/accessory visual review, authoring/resource tests and extended creator parity still pending |
| E | 90 persistent fictional athletes, exact authentic selection and race field mapping | A reviewed real-person pack contract exists; **no licensed real-athlete pack is shipped** |
| F | Individual grouped potential, style/age response, shared training, bounded history and peak | Long-term study is controlled cohort development with aggregate load, not authoritative races or a complete shared-world season simulation |
| G | Persistent bounded world, known rivals, league moves, annual replacements/retirements | Separate native world fixture covers 15 annual transitions; off-field load does not invent points, wins or race results |
| H | Native own/rival dossiers, 13 abilities, readiness, graph, actual milestones, filter/archive pages; extended two-case fixture passed with three real annual boundaries/four observations | Updated graph screenshot still needs final visual inspection |
| I | Growth cohorts and final race study qualified separately; Course/clock reproduction and regressions green | Final-source native qualification, Console checkpoint, fullscreen Windows build/receipt and built-player smoke still pending |

Long-term study scope: five controlled scenarios each contain 360 athlete
trajectories over up to 15 active seasons (1800 total), with a separate 360-person
control. Actors are frozen after their individual retirement. These are cohort
experiments, not 2160 full race simulations or 2160 athletes simultaneously in
one career. The six current copied summaries exclude retired frozen years from
stagnation/decline counts, report active observations and retain the final
creation-policy hash. Final copied summaries should be used instead of the initial slow-control
dataset or the earlier preset checkpoint.

## Visual and delivery checks

The evidence folder currently contains 23 native PNGs, exceeding the numerical
12-screen requirement, but count alone does not certify all visual acceptance.
Final uncovered-face/hair/equipment captures are still under review, and the
multi-season graph was regenerated by the passing extended fixture. Screenshots
must continue to show the actual Unity UI/model. No demo video was present at
this checkpoint; the brief requests one if technically possible.

The three-boundary UI fixture is explicitly accelerated: it uses real adaptation
and real annual history/ageing publication, with no invented race results or
history rows. It does not claim to play three full race calendars. Each annual
Continue reload uses the saved circuit definitions, and assertions verify saved
observation kind, season, age, count and JSON persistence before capturing.

The final build still needs its own fullscreen launch check, menu/new-career/race
smoke, credits/asset-provenance inclusion and final report. A prior compile/Console
checkpoint is not a substitute for checks after the final creator art and build.
Owner approval remains necessary to close M15.6 after testing the delivered build.

This audit edited only the permitted native UI fixture and new evidence/report
files. It did not run Unity, mutate gameplay source, use Git or rewrite
`PROJECT_STATE.md`.
