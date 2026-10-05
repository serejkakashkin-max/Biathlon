# TASK M13 — bounded large replay context encoding

2026-10-05. CLI tooling task PASS; no venue or race acceptance follows from it.

The new production Mass30 field has a 176,996,464-character exact context. The .NET JSON writer rejects a single string token above its implementation limit before tick one. A property-scoped converter preserves ordinary legacy string encoding and writes larger field contexts as bounded 4Mi-character string chunks, preserving surrogate pairs. Reading reassembles the identical string. The outer compressed-context stream still enforces the requested 128–512MiB expanded-byte limit; malformed chunks, truncated arrays and excessive expansion fail closed. This changes evidence-file encoding only, without altering simulation, input commands or context identity.

Changed: `dotnet/Biathlon.VenueLab/FieldContextJsonConverter.cs`, its binding in `ProductionReplayHarness.cs`, and `dotnet/Biathlon.VenueLab.Tests/FieldContextEncodingTests.cs`.

Validation: the complete VenueLab.Tests assembly has 28 PASS / 0 FAIL / 0 skipped, including the actual oversized token boundary, Unicode chunk boundary, exact legacy encoding, capacity guards and corrupt replay input. Isolated Release build has 0 errors / 0 warnings. Evidence: `artifacts/owner-short-course-20261005/test-results/venue-tool-tests.trx` and frozen `race-tools-v2`. Failed pre-fix context capture is retained. Recording subsequently reaches real race ticks, where separate compact-range traffic defects remain under diagnosis; prefix tapes are not completed-race evidence.

Next: resolve and qualify shortened-course races, persist complete tapes, then deliver the Windows review build. Large files still require explicit adequate recorder capacity; changing a recorder bound never substitutes for race completion or gameplay validation.
