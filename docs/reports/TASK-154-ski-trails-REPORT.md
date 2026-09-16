# TASK-154 — Ski and pole contact marks

2026-09-16. WP154 PASS. Final full PlayMode57/57; four new buffer/mesh/integration tests pass, including warmed0 B managed allocation.

Added a presentation-only contact history and one dynamic transparent mesh, fed from typed ski bindings and resolved pole tips after athlete pose/contact evaluation. Ski strips stop when lifted, shooting, too far away, stationary, teleported or missing updates. Pole impressions are stamped on contact onset. Rewind and host clearing remove history. No simulation physics, friction, timing, aim or authoritative state changed.

The versioned budget is4096 marks/256 emitters,0.1s sampling,18s lifetime,28–45m distance fade,4.5cm width. The fixed ring buffer replaces oldest marks when full. Full-buffer synthetic30-athlete/two-ski test:4096 visible marks,4904 overwritten,300 rebuilds after60 warmups, mean1.27345ms and0 B on the current Editor machine (Ryzen9 9950X3D/RTX5080). This is CPU mesh work, not a GPU or whole-race performance claim.

Actual Sprint tick1200 capture retains275 recent marks,92 inside the chase-camera distance budget. Overhead view shows narrow skating traces; pole impressions are subtle at chase distance. The capture harness initially sampled every6 ticks at20Hz, accidentally triggering the intended large-step protection; corrected to2 ticks. A1µs comparison tolerance handles float-config0.1s against the double presentation clock. Replay state remains unchanged when traces render; integration tests verify this directly.

Tests cover contact gaps, teleport, stale samples, pole contact onset, expiry/rewind, bounded emitter and mark budgets, distant culling, allocation and a real race host. Evidence in quality/m12/wp154. Unity MCP briefly stopped responding while the Editor was minimized; restoring its existing window recovered the connection without restarting or discarding work. A compilation-request RPC later failed during reload; the subsequent final full test run completed57/57 and its JSON/XML are retained.

Limits: these are subtle surface marks, not physical/displaced snow. Contact uses the current athlete-local surface plane; sharp crests, varied terrain contact and seam handling must be checked with the representative sector at WP164 (M11-VIS-003). The screenshot temporarily uses WP153 snow on greybox trail/ground; WP157 owns production blending. Old marks can be evicted early in a dense pack. GPU/whole-scene qualification remains WP166. Next WP155 snow interaction VFX; OWNER-FEEL-001 remains OPEN.

Console after final regression:0 errors,1 warning, compilation_failed=false. The warning is MCP BufferedFileLogStorage 'Flush called but already disposed'; Unity AI also emitted informational NoSubscription messages. Native Console text and counts are retained. No product compile/runtime error is present; no package/account changes were made.
