# TASK-139 — Screenshot/playtest evidence harness

WP139 complete. Editor PlaytestDiagnostics creates an isolated disabled two-athlete Sprint host, advances scripted commands to bounded checkpoints and exports versioned JSON observations. scripts/Capture-Playtest.ps1 captures MCP Game View images with state before/after, rejects changed state/existing evidence paths, and records screenshot SHA256, simulation-context SHA256, Git revision and dirty-tree flag. The host remains frozen for inspection; leaving Play Mode removes it.

Usage: open GreyboxVenue, enter Play Mode without a playable host, then run scripts/Capture-Playtest.ps1 -Ticks 0,400,2500 -OutputDirectory <fresh-directory>. This is an explicitly scripted diagnostic, not a recorded human input replay. Outputs for this run: docs/quality/m10/wp139. Tick2500 shows penalty phase,32.357858372m and five remaining loops; the image and JSON match. All three image checksums verified.

Validation: PlayMode evidence1/1 (24 discovered), stable repeated state and backward-tick rejection without mutation. Actual MCP capture at0/400/2500 completed. Initial reflection lookup omitted method metadata; corrected before successful capture. Final Console0 errors/1 warning, no compilation errors. No gameplay changes.

Known visual finding: tick2500 exposes coplanar overlap/z-fighting between schematic main/range/penalty ribbons. Preserve evidence; fix presentation layering in a separate visual task before treating visuals as polished. The harness records scalar race state and context hash, not a complete resumable save. Next: WP140 Windows development build and launch/log smoke, followed by owner G1.
