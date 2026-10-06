# M13 manual base-route force input — 2026-10-06

The race session can now stage a manual base-route tick without first
calculating legacy centre-line movement. It supplies the accepted world pose,
directional grade and the same physiology, technique, snow, equipment and tuck
values used by ordinary skiing to the manual force solver. An unbound preview
cannot commit; only a full-clear world-roster proposal may produce a race
commit. This is a prerequisite for manual control, not its activation.

Changed: `RacePathMotion`, `SprintAthleteSession`, `TrackSample` and the
base-route integration test. ADR-0037 records the boundary. The test verifies
the force inputs against the authoritative technique/equipment definitions,
actual yaw response and stale-world rejection.

Verification: Release .NET focused regression 19/19 PASS
(`docs/quality/m13/manual-force-bridge-regression.trx`). Hidden Unity 6
EditMode batch run 2/2 PASS
(`docs/quality/m13/manual-force-unity-noquit.xml`), process exit 0. The
batch log reports the test run completed and has no C# compilation error.
The Unity MCP hub was unavailable, so an Editor Console snapshot was not
captured. The first batch invocation included `-quit` and exited before
running tests; its log is diagnostic only and is not acceptance evidence.

No v1 race output, AI balance or shooting authority is changed. The playable
host still uses the legacy route path. Full-roster race commit, start/range/
penalty paths, contact replan, AI direction, v2 replay and owner hands-on
validation remain open. Per the owner batch directive, no intermediate
Windows build was produced.
