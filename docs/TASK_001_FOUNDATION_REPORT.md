# TASK 001 — Foundation verification report
Date: 2026-09-06. Project: C:/game/Biathlon/Biathlon.

## Outcome
TASK 001 acceptance criteria pass. Codex performed actual MCP JSON-RPC initialize, tools/list and tools/call requests against the running Unity Editor. Foundation is ready for TASK 002; no skiing, physiology, shooting or career gameplay was added.

## MCP diagnosis and fixes
1. At the first inspection Unity Editor was not running. The reproduced CLI status showed localhost:29577 connection refused and ai-game.dev/mcp HTTP 401.
2. UserSettings selected Cloud, while storing a legacy local host localhost:22315. The active Codex config pointed at the cloud project URL. codex mcp list reported Not logged in. The local token is not a cloud login.
3. Pinned Custom/local streamableHttp via unity-mcp-cli bootstrap-local, configured Codex for http://localhost:29577, and launched Unity 6000.6.0f1. Cloud OAuth is unnecessary for this local workflow; cloud access was not repaired or claimed.
4. Verified MCP server gamedev-mcp-server 9.2.5.0, initial active scene SampleScene, root hierarchy, Editor state, Console, and Scene View PNG.
5. Diagnosed a second real failure in script-execute: Common Roslyn 4.8 was mixed with CSharp 3.11 from com.unity.pipeline. MCP's name-only dependency resolver repeatedly disabled the NuGet CSharp 4.8 importer.
6. Removed only the optional experimental com.unity.pipeline 0.6.0-exp.1 direct dependency; UPM updated packages-lock.json. Restored CSharp 4.8 Editor compatibility and restarted Unity. script-execute now succeeds; both Roslyn DLLs load as 4.8. Unity AI Assistant remains installed.
7. Generated 77 skills in .agents/skills; the current task's refreshed skill catalog includes them. No additional Unity extensions installed.
8. Enabled required Editor state, screenshot and reflection tools in MCP discovery. Final tools/list exposes 44 tools; unity-tool-list records the broader registered tool set.
9. Added a real MCP client script for this session and repeatable local verification. Calls were executed through this PowerShell JSON-RPC client, not merely inferred from configuration. codex mcp get confirms enabled streamable_http, the correct URL, startup timeout 30 s and tool timeout 300 s.

## Successful tool calls
- editor-application-get-state
- editor-application-set-state
- scene-get-data (scene + hierarchy)
- scene-list-opened
- console-get-logs
- screenshot-scene-view
- screenshot-game-view
- assets-refresh
- tests-run (EditMode and PlayMode)
- script-execute (scene setup, deterministic hash, live tick observation, Console reset/inspection)
- reflection-method-find
- reflection-method-call
- unity-tool-list
- tool-set-enabled-state

The initial editor-state spelling was rejected; the documented editor-application-get-state succeeded. console-clear-logs was tested and failed with a Windows file-sharing exception; it is not counted as successful.

## Assemblies
| Assembly | Dependencies / role |
|---|---|
| Biathlon.Domain | BCL only; immutable tick, ID, state, input, event, RNG contracts |
| Biathlon.Simulation | Domain; clock, config, world, SplitMix64 |
| Biathlon.Replay | Domain + Simulation; ordered event log and canonical fingerprint |
| Biathlon.AI | Domain; reserved boundary marker |
| Biathlon.Career | Domain; reserved boundary marker |
| Biathlon.Unity.Presentation | Domain + Simulation + Replay + Unity; bootstrap and debug label |
| Biathlon.Tests | Five core assemblies + NUnit; shared pure tests |
| Biathlon.PlayModeTests | Presentation + core + Unity test framework |
| Biathlon.Unity.Editor | UnityEditor only; verified diagnostic inspection |

All five core asmdefs set noEngineReferences=true and overrideReferences=true. The same source builds as five netstandard2.1 projects without Unity DLL references. A runtime assembly-reference test verifies the boundary in both .NET and Unity.

## Tests and results
| Runner | Passed | Failed | Skipped |
|---|---:|---:|---:|
| Standalone .NET 8 / shared NUnit suite | 14 | 0 | 0 |
| Unity EditMode / Biathlon.Tests | 14 | 0 | 0 |
| Unity PlayMode / Biathlon.PlayModeTests | 2 | 0 | 0 |

The 14 core cases cover:
- exact clock at 20 Hz / 10,000 ticks, 30 Hz / 10,000 ticks, and 7 Hz / 31 ticks;
- identical state and ordered-event hash for two 10,000-tick runs, with a pinned regression hash;
- different-seed isolation of nonrandom systems;
- SplitMix64 known vectors and 10,000-draw restored-state reproducibility;
- independent worlds and defensive copying of initial athlete data;
- rejection of invalid input batches before mutation;
- same-tick input ordering and suppression of no-op events;
- replay from recorded inputs;
- hash sensitivity to event order and payload;
- no Unity assembly dependency;
- zero managed allocation in a warmed 10,000-step loop with preallocated event storage;
- invalid config, IDs, effort, duplicate IDs, negative ticks and overflow.

PlayMode verifies JSON config loading, actual tick progression, and pausing when Unity scaled time is paused.
No test was removed, disabled or skipped.

Commands from the project root:
~~~powershell
./scripts/Test-Foundation.ps1
./scripts/Invoke-UnityMcp.ps1 -Tool tests-run -Arguments @{testMode='EditMode';testAssembly='Biathlon.Tests';includePassingTests=$true}
./scripts/Invoke-UnityMcp.ps1 -Tool tests-run -Arguments @{testMode='PlayMode';testAssembly='Biathlon.PlayModeTests';includePassingTests=$true}
./scripts/New-FoundationScene.ps1
~~~
After PlayMode tests, wait until editor-application-get-state reports IsPlaying=false and IsPlayingOrWillChangePlaymode=false before opening/running Foundation. The test tool can return the result before Unity finishes restoring the scene.

A local .NET SDK 8.0.424 was installed at %LOCALAPPDATA%/Biathlon/dotnet because the machine initially had only .NET runtimes. Test-Foundation.ps1 automatically finds this SDK; it also supports an explicit -Dotnet path. The source itself targets netstandard2.1.

## Determinism evidence
Seed 12345; fixture config foundation_test; two athletes; 10,000 ticks; 20,000 ordered effort-change events; final requests 991 and 931.
Both .NET and Unity returned:
~~~
aab9e912c3ed27c93706397a6667ae7a71dbd6f97685a2a5519feb7380a3360e
~~~
The hash includes schema/config ID, frequency, tick, seed/RNG state, ordered athlete states and ordered events. It never uses runtime GetHashCode.
The nonrandom comparison intentionally excludes seed/RNG metadata and matches across seeds 12345 and 98765.

## Unity acceptance
- Saved scene: Assets/Scenes/Foundation.unity; bootstrap + debug camera, no manual Inspector setup.
- Real PlayMode observations: tick 284 / 14.2 s, then tick 292 / 14.6 s, frequency 20 Hz.
- Game View inspected: tick 290, 14.500 s, 20 Hz label visible.
- Final scene Foundation remains open and saved; Editor stopped, not compiling/updating.
- Native Console: errors 0, warnings 1; scriptCompilationFailed=false.
- Warning: Unity-MCP BufferedFileLogStorage, "Flush called but already disposed, ignored" during PlayMode exit. This is a plugin lifecycle warning, not a C# compiler warning. MCP log cache has 0 entries because it is recreated across that transition.
- During initial startup the existing Unity AI Assistant also emitted an Account API timeout warning; it is recorded in the initial capture.

## Files created
Plus Unity-generated .meta files:
- Assets/Scripts/Domain: AthleteId.cs, AthleteState.cs, SimulationTick.cs, SimulationInput.cs, RaceEvent.cs, IRandomSource.cs, Biathlon.Domain.asmdef.
- Assets/Scripts/Simulation: SimulationConfig.cs, SimulationClock.cs, SimulationWorld.cs, SeededRandomSource.cs, Biathlon.Simulation.asmdef.
- Assets/Scripts/Replay: RaceEventLog.cs, SimulationFingerprint.cs, Biathlon.Replay.asmdef.
- Assets/Scripts/AI and Career: AssemblyMarker.cs and their asmdefs.
- Assets/Scripts/Unity/Presentation: FoundationBootstrap.cs and asmdef.
- Assets/Scripts/Unity/Editor: FoundationDiagnostics.cs and asmdef.
- Assets/Scripts/Tools/BalanceLab/README.md.
- Assets/Resources/Biathlon/simulation_foundation.json.
- Assets/Scenes/Foundation.unity.
- Assets/Tests/EditMode: FoundationTests.cs, Biathlon.Tests.asmdef.
- Assets/Tests/PlayMode: FoundationBootstrapTests.cs, Biathlon.PlayModeTests.asmdef.
- dotnet: six csproj files for the five core assemblies and shared tests.
- scripts: Invoke-UnityMcp.ps1, Test-Foundation.ps1, New-FoundationScene.ps1, Reset-UnityConsole.ps1.
- docs/adr/ADR-0001-deterministic-simulation.md, docs/README.md, docs/BIATHLON_GDD_TDD_v0.1.md (forwarding link), this report.
- tests/README.md.
- .agents/skills: 77 generated SKILL.md files.

Existing files/settings changed:
- .codex/config.toml: cloud → local HTTP.
- UserSettings/AI-Game-Developer-Config.json: local connection and tool visibility (ignored local settings).
- Assets/Plugins/NuGet/Microsoft.CodeAnalysis.CSharp.dll.meta: Editor enabled. The plugin also maintains its NuGet importer settings automatically.
- Packages/manifest.json and packages-lock.json: optional Pipeline removal/resolution.
- .gitignore: retain portable dotnet csproj; ignore build/test artifacts.

## Git review and scope
git diff --check passes. Existing user deletions under Assets/Editor, URP icon change and ProjectSettings changes were present before this task and were preserved. The repository-wide diff includes that pre-existing work; it must not be attributed wholesale to Foundation.
Our addition is 16 implementation/diagnostic C# files, 2 test source files, 9 asmdefs, 6 portable project files, one config, one scene, scripts/docs and 77 skills (plus Unity metas). No commits or remote operations were performed. Original AGENTS, GDD and TASK 001 were not edited.

## Limitations / technical debt
- Upstream console-clear-logs in MCP 0.90.0 cannot delete the open Windows log file. Reset-UnityConsole.ps1 provides a tested local alternative: archive the Console, dispose/flush collector, truncate with sharing, recreate collector. No vendor cache patches.
- Upstream plugin emits the one nonblocking disposal warning above.
- Optional Unity Pipeline was removed; reintroduce only with a verified compatible Roslyn dependency configuration.
- Cloud authorization remains unconfigured because local MCP fulfills this task.
- No production player build, full save format, full replay file format, race rules or balance system was requested or claimed. Those are future systems.
- The bootstrap catches up all elapsed scaled-time ticks. Future heavy simulation needs an explicit catch-up/performance policy.
- The event log grows if not preallocated. Fingerprinting allocates outside the tick loop.
- Foundation uses only integer authoritative state. Future floating physiology/physics requires its own determinism analysis.

## Evidence files
All machine captures are under artifacts/foundation (ignored generated output):
core-tests.trx, unity-editmode.json, unity-playmode.json, unity-hash.json, mcp-tools.json, unity-tool-list.json, editor-initial.json, editor-final.json, scene-initial.json, scene-final.json, console-initial.json, console-final.json, console-counts-final.json, native-console-messages.json, play-observation-1.json, play-observation-2.json, scene-initial.png, foundation-play.png.
Historical diagnostic failures are kept in earlier captures; final results are reported separately.

## Next task
Ready for TASK 002 once its specification is supplied. The GDD's next logical subsystem is HeartRateSystem + AnaerobicSystem + FatigueSystem with data-driven parameters and deterministic workload tests. Do not start ski movement before physiology acceptance.

## Documentation consulted
- Installed Unity-MCP 0.90.0 source, generated skills and CLI help (version-matched).
- [Unity-MCP CLI documentation](https://github.com/ivanmurzak/unity-mcp/blob/main/cli/README.md).
- [Official Codex MCP documentation](https://learn.chatgpt.com/docs/extend/mcp?surface=cli).
