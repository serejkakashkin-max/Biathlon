# M13 — explicit manual start, range and penalty geometry

2026-10-08. G1/G2 APPROVED; M13/WP-175/177/178 OPEN; WP-176 PASS.
No intermediate Windows build. Fully manual playable steering remains pending.

Added `ManualRacePathCatalog` in pure Simulation. Its exact format/mat binding
covers the optional Mass Start approach, every counted route interval, physical
range ingress, firing position, exit and each possible penalty ordinal on each
shooting lap. It splits the assigned physical mat path exactly at the firing
anchor. Start/penalty arc is distinct from base distance; repeated coordinates
cannot select a different lap/circuit. Shared exits retain one physical identity.
Actual surface metadata comes from the observed physical loop, not route credit.

Reused `ManualRouteProjectionMap` through a scoped geometry factory and added
read-only slice/boundary metadata in `PhysicalRangePathMap`. Scoped geometry
cannot bind the old base-route world ledger. The ordinary base projector's
context/query policy and legacy race integration remain intact. No shooting,
ability, course or race-balance tuning changed; no authoritative live output
changes until this catalog is connected to the race owner.

The added exact endpoint test went red at occurrence2/end98 m. A second attempt
proved projection repaired but target sampling still failed. Scoped binary seek
and endpoint observation now use the existing two-nanometre encoding reserve;
all scoped endpoints project/sample in an exact zero-width station window.
World pose and physical clearance are unchanged. Failed receipts are retained.

[ADR](../adr/ADR-0038-manual-race-physical-occurrences.md).
[Evidence](../quality/m13/manual-physical-paths-20261008/).

- First focused .NET:45/45 PASS, before the new exact endpoint assertion.
- Final focused .NET:69/69 PASS, including16 new catalog cases and legacy
  Sprint/Pack/player replay. Unity:16/16 executed PASS (discovery1307),
  17.2246519 s. Compile settled; Error Console empty.
- Broad manual/replay:229/230 PASS; unchanged motion allocation test saw192 B.
  Another focused run:68/69 PASS; unchanged forward-target allocation test saw
  4984 B. These remain evidence of an OPEN allocation risk; no test or assertion
  was removed/relaxed, and the later69/69 pass does not prove a fix.
- Additional diagnostic-only probes:300/300 empty arithmetic windows with inline
  NUnit assertion;300/300 with a stored counter;60/60 unchanged public test calls
  on new threads, and60/60 with JIT disassembly. The disassembly places the final
  allocation read before `Is.Zero`; assertion reordering is not established as
  the cause. These probes do not replace required tests or qualify player GC.

The reproducer source/project and disassembly are stored with this task's
evidence. The .NET8.0.31 counter implementation was inspected in the official
[runtime source](https://github.com/dotnet/runtime/blob/v8.0.31/src/coreclr/vm/comutilnative.cpp).
It does not by itself identify the cause of the measured deltas. Root cause and
final Windows runtime qualification remain OPEN.

Limitations: catalog queries do not admit any body/branch, assign mats, determine
penalty debt, commit phases/events or change the playable host. The old host still
uses lateral route-following. Next is phase-owned progress and world handoffs,
then complete session/contact/replay/host integration and final M13 checks/build.
