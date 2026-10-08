# M13 — backward downhill presentation and allocation isolation

2026-10-08. Continuation of TASK_M13_MANUAL_ACTUAL_WORLD_20261007.
M13/WP-175/177/178 remain OPEN; no intermediate Windows build.

The existing backward Pack integration now also exercises actual Woodland
station80 m with a skier facing downhill against increasing route station and
holding tuck. Five jointly proven fixed ticks retain high-water race credit,
advance physical odometer/physiology/clocks, and publish actual negative slope,
zero yaw curvature and active tuck in the detached presentation snapshot.
The station10 m flat case remains. .NET2/2 PASS; native Unity2/2 PASS before the
intentional interruption. The recorded receipt distinguishes execution count2
from discovery1291. On resume Unity is stopped, compiled and Error Console empty.
This verifies snapshot publication, not live manual rig/host integration.

## Allocation triage (assertions and production code unchanged)

[Evidence/source](../quality/m13/manual-actual-world-20261007/).
The reproducer copies reside in allocation-probe-program.cs.txt and
allocation-probe-project.csproj.txt. Restore these into artifacts/m13-alloc-probe/
and run `dotnet run --project artifacts/m13-alloc-probe/Probe.csproj -c Release --
<mode> <receipt.json>` from repository root. These diagnostics call the actual
public NUnit methods or the exact shared manual force API; they do not replace
required NUnit/native tests.

- `direct`:30 motion +30 ledger test method invocations on the main thread,
  60/60 PASS, original assertions unchanged.
- `dedicated`:same public methods on new threads,57/60 PASS. Three motion
  allocation assertions saw5288/3680/7752 bytes. Ledger30/30 PASS.
- `calls`:30 new-thread batches of10,000 actual accepted-velocity previews,
  measured per call;0 batches with allocations.
- `bulk`:60 new-thread batches with the same10,000-call force loop and stored
  bulk counter;0 batches with allocations.
- `counter`:256 allocation-free arithmetic windows under concurrent forced GC;
  every counter delta0.
- Explicit `DOTNET_TieredCompilation=0` broader manual selection:183/183 PASS.
  This single pass does not establish causality; the project runtime config
  already requests tiered compilation off.

Earlier183-test runs with7792/5072-byte failures remain preserved. Isolation
narrows the discrepancy to complete test invocation/context versus the sampled
core loop, but does not prove a particular NUnit, JIT or GC defect. Root cause
and runtime allocation qualification remain OPEN. No weakened assertion,
retired failing test, tolerance increase or performance closure was introduced.

Next: manual start/range/penalty physical scopes and complete world tick/phase
continuation, followed by live host/replay and final M13 qualification/build.
