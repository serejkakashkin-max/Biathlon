# Current qualification -2026-11-02

M16.6 implementation and automated legacy-regression repair complete. Owner
review remains OPEN; M17 not started. Review START_TEST.cmd under
Builds/M16_6_5_Regression_Review (unchanged qualified fullscreen player).

| Current gate | Actual result |
| --- | --- |
| Complete .NET8 Release assembly | 2188PASS /0FAIL; completed72m4s |
| All existing Explicit diagnostics, selected separately | 5/5PASS;2193unique cases qualified in total |
| Recent native regression groups | 52EditMode /5PlayModePASS; prior groups overlap and are not summed |
| Original third-bout held-exit case | PASS128.543s; original600s guard/physical assertions unchanged |
| Final Unity state/Console | idle, compiled,0errors/exceptions/asserts |
| Fullscreen standalone review | PASS/exit0; owner profiles unchanged; build0errors |

Raw complete release-final-assembly.trx, release-acceptance.json and assembly
identity retain the actual completed result. Default-suite Explicit skips are
listed separately and all five have actual separate passing receipts. CPU timings
are observations, not portable performance/FPS promises. Build identity verifies
all eight existing player binaries unchanged. Only test/docs/launcher work followed
that player build. publication.json records runtime/fixture/launcher revisions.

Use Release for the full core suite. scripts/Test-Foundation.ps1 keeps Release and
exposes -HangTimeout (outer70m default exceeds the existing60m season test). Its
argv/default/invalid-duration/nonzero-exit contract is separately verified. For
visible per-case progress, add a console logger with verbosity=normal to a direct
dotnet test command; retain the TRX logger. The actual final run used an isolated
output folder with no diagnostic source injection.

The following dated progress/audits remain historical evidence. Their pending,
timeout and incomplete Debug statuses are superseded by the completed Release
qualification above; their original failures are retained.

---

# M16.6 — owner-authorized legacy regression repair

2026-11-01. Work in progress after published training21429280. User explicitly
selected continuing old failures in this same task. ADR-0089 explains the input
and assertion corrections. No failing test, invariant or threshold was disabled.

Completed groups (overlap; not a unique aggregate):

| Check | Result |
| --- | --- |
| .NET models, steering, full range cycles and1536-row pacing calibration | 63/63 PASS |
| .NET scoped sessions, penalty transitions, complete finish/repeat | 50/50 PASS |
| Native captured steering/transaction/identity/publication/holds | 78/78 PASS |
| Native actual first-range departure, before-penalty seams/current context | 8/8 PASS |
| Native full1536-row pacing calibration and career lock | 2/2 PASS |
| Native versioned start/snow/metadata/venue controls,2026-11-02 | 35/35 PASS |
| Native current Mass Start and original start-policy1 backward seek | 2/2 PASS |

The78-case native tool response exceeded300s. Actual completed testResults proves
Passed78; all rows are retained in two pages plus the status aggregation. No
timeout is treated as a passing response. Further traffic/native checks and the
complete snow-intent fix remain in progress. Build4 remains the last published
review candidate; a new build is required after the gameplay repair is qualified.

Experimental baseline receipts retain failing controls and successive minimal
probes. In particular, the finish physical probe still fails the original arc
assertion while exposing the positive2.859939mm suffix. Complete snow-domain probe
passes8/8 and retains zero allocation. It motivates the explicitly versioned
policy in90686970, with legacy context preserved.

The historical full assembly was launched on the equipment-stage binary before
these repairs. It remains a historical failure audit, not current acceptance.
M16.6 OPEN; M17 not started.


2026-11-02 delivery: all current native groups PASS (35policy/venue/metadata,
5original-v25Edit,2currentMass/start1Play,2weather1/legacy-savePlay,1replayUIPlay).
Build5 Succeeded0errors/520existing warnings,430943436bytes,37.216s. Post-build
mapped settings cleanup first returned IOException; guarded retry restored the
known temporary timing/preload changes and the existing successful player was
packaged without rebuilding. Owner settings snapshots stay local/ignored.
Actual fullscreen PASS/exit0,2560x1440,bib10,start5401/tick5405,3-2-1-0,owner
profiles unchanged,errors empty.09-race.png inspected. No FPS/optional navigation
claim. Full current assembly remains running; M16.6 OPEN, no globally green claim.

2026-11-02 follow-up: full assembly found one historical-weather and two exact-mat fixture failures. Corrected input-only reruns: .NET1+9PASS, Unity10/10PASS, Console0errors. Original first-pass failures remain visible. Player unchanged; full assembly still pending. See ADR-0089 and historical-yaw-fixed-weather.trx, mat-exact-boundary.trx, native-historical-mat.json.

Current full native Mass Start/Pursuit repeats:2/2PASS (each race twice,270.492s/267.484s) with explicit600s test budgets; initial180s timeout receipt retained. Full.NET assembly additionally exposes the third-bout held-exit600s timeout; diagnosis continues, no global green claim.

Configuration correction: prior current full audit was unoptimized Debug and stopped incomplete after1699PASS/4FAIL/1703executed. Unchanged scout selects player21/tick10393 in Debug294.222s and Release51.763s. Original600s third-exit timeout restored in full. Canonical core configuration Release now runs the complete current assembly in an isolated folder, no diagnostic source injection. Acceptance pending; Debug audit is not a qualified suite.

All five existing Explicit diagnostics selected directly in Release:1penalty-edge +4pace/CPU PASS. Full default-suite skips are separately qualified by these receipts. Launcher keeps Release; validated optional -HangTimeout defaults70m outside existing per-test limits up to60m. AST/argv/invalid-duration/failing-exit contract PASS; no test assertions or individual timeouts changed. Complete real Release assembly still pending.
