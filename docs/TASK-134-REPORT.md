# TASK-134 — Penalty loop presentation

WP134 complete. PenaltyLoopPresentation draws the authored physical penalty route while the selected stable athlete ID occupies that loop. Existing proxy interpolation follows the same geometry. The panel displays authoritative remaining loop obligations, current-loop metres/progress and the global race clock computed from snapshot tick/frequency. Rejoin and race reset clear the guide/panel. No visual crossing grants penalty credit.

Validation: scripted geometric misses drove five required loops and rejoin in PlayMode1/1 (19 discovered), checking remaining obligations and exact tick-derived clock. .NET and Unity EditMode traversal10/10 each. Game View screenshot inspected: docs/quality/m10/wp134-penalty.png (50m of150m, five remaining,127.10s). Final Console0 errors/1 warning, no compilation errors.

Verification tooling: a diagnostic script initially used the wrong namespace for TimeTrialInput; corrected before the screenshot. Subsequent Test Runner mode switching returned zero tests; those runs were not accepted. RequestScriptCompilation reset discovery and the final PlayMode rerun passed1/1; XML retained in artifacts/balance/wp134-playmode.xml. No gameplay/balance changes. Route remains schematic geometry; this panel shows global race time, not a separately inferred penalty duration. Next: WP135 basic race HUD.
