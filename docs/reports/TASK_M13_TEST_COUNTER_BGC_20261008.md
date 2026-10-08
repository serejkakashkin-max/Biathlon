# M13 — CoreCLR allocation measurement diagnosis, 2026-10-08

PASS for the diagnosed test-host measurement problem and the selected manual/
replay regressions. Production performance and M13 remain OPEN.

## Reproduction and diagnosis

The previous [common-event report](TASK_M13_MANUAL_COMMON_EVENTS_20261008.md)
preserves the default final 258/260 failure with 7,440/3,144 B in existing exact
zero-allocation assertions. Earlier reports also retain their original failures.

The ranked hypotheses were background-GC counter accounting, lazy runtime/JIT
initialization and actual allocations in simulation. A bounded package-free
reproducer uses no game code, reflection, NUnit or instrumentation callbacks in
its measurement. It allocates a real thread-context anchor outside the measured
method, then measures only two `GetAllocatedBytesForCurrentThread` calls around
`Thread.SpinWait`. A separate bounded worker retains live roots and requests gen2
collections. Explicit 64-byte allocations are independent positive controls.

SDK 8.0.425; CoreCLR 8.0.31; Windows x64; workstation GC. All comparisons run the
same compiled diagnostic DLL, with only the runtime configuration changing.

| Configuration/run | Empty controls | Real 64 B controls | Empty intervals during collection pressure | Gen2 requests/collections |
|---|---:|---:|---:|---:|
| Default concurrent, first | 0/32 nonzero | 32/32 correct | 7/256 nonzero | 117/119 |
| Nonconcurrent, first | 0/32 nonzero | 32/32 correct | 0/256 nonzero | 117/119 |
| Default concurrent, confirmation | 0/32 nonzero | 32/32 correct | 10/256 nonzero | 118/120 |
| Nonconcurrent, confirmation | 0/32 nonzero | 32/32 correct | 0/256 nonzero | 78/80 |
| Exact new Biathlon test runtime config | 0/32 nonzero | 32/32 correct | 0/256 nonzero | 123/125 |

Default false positives are six 8,136 B plus one 1,448 B in the first run and ten
8,136 B in confirmation. These empty-loop failures establish a local counter
problem independently of gameplay and NUnit. They match the proposed background
allocation-context accounting mechanism; individual historical test intervals
were not separately traced and are not rewritten as if they had passed.

Primary context: [runtime issue 134724](https://github.com/dotnet/runtime/issues/134724)
and [draft fix 134855](https://github.com/dotnet/runtime/pull/134855), checked
2026-10-08. The proposed fix retires unused allocation-context bytes before BGC
clears those contexts. The published original case uses macOS ARM64; our Windows
receipts establish applicability here. No third-party or draft runtime was installed.

## Change and verification

Only `dotnet/Biathlon.Tests/Biathlon.Tests.csproj` sets
`ConcurrentGarbageCollection=false`. The generated test runtime configuration
contains `System.GC.Concurrent=false`; existing tiered-compilation setting is
unchanged. No zero assertion, test, expected race result or tolerance changes.

- Original expanded common-event selection: 260/260 PASS after the setting.
- Broader all-manual + Sprint/Pack/Player replay selection: 416/416 PASS.
  This includes the earlier motion/ledger/roster/force allocation test families.
- Running the diagnostic under the **exact generated test runtime config** still
  detects real heap allocations (32/32) while empty pressured intervals stay zero.
- Native 21+5+19 passes and empty compile/Error/Exception Console receipts from
  the common-event task apply to unchanged Unity/core source. This .NET test-only
  project setting requires no repeated Unity import or gameplay window.

Unity and BalanceLab collectors remain at their production defaults. BalanceLab's
current runtime config contains no `System.GC.Concurrent` override. This correction
makes the .NET assertions measurable; it is not a game-memory optimization or
evidence of 40-athlete production frame/work budgets.

## Evidence and reproduction

[New receipts](../quality/m13/test-counter-bgc-20261008/) and
[standalone source/first A-B receipts](../quality/m13/manual-common-events-20261008/).
Restore `counter-probe-program.cs.txt` as `artifacts/m13-counter-bgc/Program.cs`
and `counter-probe-project.csproj.txt` as its `Probe.csproj`. From repository root:

```powershell
dotnet run --project artifacts/m13-counter-bgc/Probe.csproj -c Release -- artifacts/m13-counter-bgc/default.json concurrent
dotnet exec --runtimeconfig docs/quality/m13/manual-common-events-20261008/counter-off.runtimeconfig.json artifacts/m13-counter-bgc/bin/Release/net8.0/Probe.dll artifacts/m13-counter-bgc/off.json nonconcurrent
dotnet exec --runtimeconfig dotnet/Biathlon.Tests/bin/Release/net8.0/Biathlon.Tests.runtimeconfig.json artifacts/m13-counter-bgc/bin/Release/net8.0/Probe.dll artifacts/m13-counter-bgc/test-host.json test-host
```

Default red command exit 1 means a nonzero empty interval; exit 2 means a failed
zero/positive control. Startup/worker/deadline errors throw and cannot pass. The pressure workload/start/shutdown are
bounded. No retry-until-green selection was used. All controls run outside any
game tuning or authoritative tick; diagnostics remain in the explicit artifacts/
evidence location, not production source.

Next: compose signed controlled slip/yaw and common physical phases into the
operational full-field runner, qualify course seams/mats/finish/replay/host, then
WP-175/177/178 and the combined final M13 owner-test build. WP-176 stays PASS.
