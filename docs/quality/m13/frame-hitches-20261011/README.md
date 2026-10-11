# Diagnostic evidence

`rifle-red.json` / `rifle-final.json`: real prone setup sub-tick regression.
`pacing-red.json` / `pacing-green.json`: real production host catch-up regression.
`forest-before.txt` / `forest-after.txt`: same 768-tree native CPU probe.
`vegetation-tests.json` / `forest-visibility-tests.json`: invalidation, zero
allocations, frustum, LOD, shadow and canopy preservation tests.
`presentation-core.trx`: 9 relevant .NET cases.

The `*.cs.txt` files are diagnostic scripts, not game scripts and not build
content. Invoke individual safe probes through scripts/Invoke-UnityMcp.ps1,
script-execute, isMethodBody=false, className/methodName matching the source.
The live probe requires a fresh Play Mode host with AutoStart=false. It drives
all AI at one simulation tick per rendered frame, so it is a bounded comparative
CPU trace, not the player's normal real-time input/scheduler path. The latter is
covered separately by PlayerRangeOwnerFeedbackTests.

`frames-before.csv` / `samples-before.csv` contain editor startup/tool overhead;
do not interpret their totals as gameplay cost. Allocation stack capture did
not resolve methods. The attempted deep-profile capture has no valid result and
must not be rerun automatically. Its failure receipt is retained for provenance.
