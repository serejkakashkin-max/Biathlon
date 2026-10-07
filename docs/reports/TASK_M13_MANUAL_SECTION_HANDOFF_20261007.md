# M13 manual world roster section handoff

Date: 2026-10-07. Intermediate manual-steering integration, not a playable
host change.

`ManualWorldRosterLedger.TryHandoffSections` now rebinds accepted athletes
at an exact selected-route endpoint to the ordered successor section. It
uses the existing ADR-0028/0037 endpoint proof and validates every requested
athlete before changing any accepted cursor. A handoff preserves world pose,
velocity, physical odometer and fixed tick, and awards no route distance.
It never chooses a branch or turns skis. An invalid second actor rejects a
multi-actor handoff without moving the first actor's cursor. Duplicate
indices and stale endpoints are rejected.

Release .NET `ManualWorldRosterLedgerTests` 8/8 PASS
([TRX](../quality/m13/manual-section-handoff-20261007/dotnet.trx)); broader
route/session/joint regression 26/26 PASS
([TRX](../quality/m13/manual-section-handoff-20261007/regression.trx)). Unity
6 Editor MCP executed the exact-endpoint EditMode test 1/1 PASS after the
long career run was cancelled through Unity's Test Runner API. No C#
compilation diagnostic appeared in the Console query. A subsequent full
native class request exceeded the MCP HTTP timeout and has no completion
receipt; it is not counted as passing.

This changes no production race result: the world ledger is not connected
to the shared Pack field, AI, replay or playable host. Endpoint handoff
still needs explicit traffic/seam admission, simultaneous race-state
publication and complete start/range/penalty ownership. M13 remains OPEN;
there is no new Windows build.
