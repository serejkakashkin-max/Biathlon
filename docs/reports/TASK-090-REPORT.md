# TASK-090 — Three-format championship harness

PASS — 2026-09-11. WP090 DONE; M7 gate PASS.

The shared `ChampionshipRaceFactory` constructs synthetic 30-athlete Sprint, qualified Pursuit
and independently rostered Mass Start workloads from authoritative JSON definitions. The factory
has no Unity dependency. CLI and Unity regression use the same setup, seeded AI and shared physics.
Stable source athlete IDs preserve personality when Sprint results reorder the Pursuit roster.
Mass Start requires no Sprint qualification and uses its own grid and ordered range policy.

Run from the repository root (installed .NET 8 SDK):

```powershell
dotnet run --project dotnet/Biathlon.BalanceLab -c Release -- championship Assets/Resources/Biathlon tests/fixtures/tracks artifacts/championship
```

Each stage saves exact context, result JSON and replay event CSV. Root qualification JSON binds
Pursuit to the Sprint result. Summary starts as incomplete and becomes completed only after
start/shot/lap/bout/penalty/travel accounting and result deserialization succeed. Bad command
arguments return 2; missing data returns 1 and preserves incomplete status.

Two CLI runs matched all ten context/result/event/qualification files byte for byte. At 20 Hz,
seed 123: Sprint25844 ticks/300 shots/28 penalties; Pursuit16166/600/21;
Mass Start17730/600/37. All90 starters finished,1500 accepted shots,zero precision fallbacks.
Result and event hashes are recorded in docs/quality/wp090/verification.json.

Scope: synthetic headless complete-finisher workloads. This task does not claim rendered race
acceptance, career standings, withdrawal handling, production venues or final balance calibration.
Existing authoritative movement/shooting/tuning were not changed. Next roadmap work: WP091/M8.

Validation: new .NET2/2; Unity MCP2/2 (170.83 s),486 discovered. All three result hashes and tick counts match CLI/.NET/Unity exactly. Prior full484/484 remains regression evidence. Console0 errors,4 retained MCP warnings; compilation_failed=false.
