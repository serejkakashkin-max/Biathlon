# TASK-011 / WP-011 — PASS

Added streaming full-tick input/state/telemetry hashing, two versioned regression fixtures,
long extreme-state tests and reproducible benchmark. CLI summary now includes full trace SHA.
Serializer-only DTO constructors are private; unsupported numerical combinations fail before ticks.

.NET Release and Unity MCP EditMode: 71/71 each. All eight full trace hashes at nine decimal places
AND final state/event hashes exactly match the pinned baseline in both runtimes. This qualifies
these Windows .NET 8 / Unity 6000.6 Mono workloads; it is not an all-platform guarantee.
Nine initial-state/stat/form combinations ran 100,000 ticks each (83m20s simulated each), with bounded
resources, recovery and no NaN/Inf. Foundation fingerprint unchanged.

100,000 warmed integrated ticks, 5,000 simulated seconds: .NET 16.8942 ms / 0 allocated bytes;
Unity Mono MCP 45.4376 ms / 0 allocated bytes. These establish local observations, not hardware-independent
timing assertions; future >10% regression requires repeated like-for-like measurement and explanation.
Export/hash formatting cost excluded from simulation benchmark. Evidence artifacts/physiology/wp011-*.

Unity Console 0 errors / 4 upstream reload warnings, compile successful; git diff --check passed.
No gameplay retuning or scenes changed. Existing uncommitted baseline retained, no commit/remote operation.
Next WP-012 M1 report/plots and acceptance decision from measured evidence.
