# ADR-0050 — Progressive keyboard ski deflection and restored High shadows

2026-10-11. Implemented for owner review; M13 remains OPEN.

The owner reports practically no remaining hitches, requests restoring athlete
shadow quality and investigating why the player reaches the first Mass Start
range last. Production creates identical ski/shooting athletes for every entrant.
An equal-navigation diagnostic through the actual production field, device W
command builder and effort controller reaches the first mat at tick2707, place1/30.
It isolates effort capability; it does not reproduce a human's manual steering or
establish a universal race balance verdict. W's ceiling and attack/recovery costs
remain unchanged. Expert AI, personalities, stats and physical force laws remain.

A short A/D press previously requested1000 steering units immediately. In the
shared high-speed force fixture at14m/s this requests300N of corner braking,
before much heading rotation can develop. The AI can request smaller deflections.
This is an input ergonomics problem: a small keyboard correction should be
possible without every tap becoming a maximum deflection.

`device_control_gameplay_v3` adds an optional keyboard steering rate,2000units/s.
At20Hz the first held tick requests100units; continued holding reaches1000 in
0.5s. Release requests zero immediately; reversal starts a fresh ramp toward the
new sign. No route tangent/centre enters the command builder. Existing heading
release behaviour remains; later bends still require manual input. Analog gamepad
and historical definitions keep their prior behaviour. The shared force law still
charges a hard turn for every athlete. The14m/s tap fixture now requests0N extra
corner braking and exits its first50ms at13.952625833m/s versus13.752625833 with
an immediate full deflection. This is not a power or speed bonus.

Commands are filtered at fixed ticks, serialized after filtering, and replay
unchanged. Red/green checks use the real device-to-force seam. Frame sampling,
rejected tick atomicity, release/reversal, analog controls and zero allocation
remain covered. This production input revision does not revise the historical
core balance tuning lock or career stat coefficients.

High restores its former shadow settings:4096,4cascades,100m,soft shadows.
Render scale/textures/meshes and previous batching/caching remain. Only those
three serialized High asset fields are changed; existing owner asset edits remain.
The real player rig retains body and equipment shadow casters. The inspected
native gameplay capture confirms the resulting silhouette.

CheckpointTimingBoard previously scanned the roster on every leader/gap query.
Splits are immutable, so it now updates a per-ordinal leader only on accepted
arrival. Clock-specific ordering and stable athlete-ID tie breaking remain exact.
Differential30-athlete arrival tests cover both global/elapsed clocks. A bounded
300000-query CPU probe falls421.6912→47.5947ms with identical checksum and zero
allocation. This measures timing-query work, not full-frame FPS or hitch removal.

Further closure remains the owner's gameplay review. The earlier unrelated
Pursuit directed-passage/start-continuation construction failure is still recorded.
