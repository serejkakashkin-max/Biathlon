# M13 — range rendering and grounded snow

2026-10-10. Owner test candidate. M13 OPEN; M14 not started.

Build: `Builds/WoodlandM13PerfGround-20261010/START_WOODLAND_REVIEW.cmd`.

- Stationary venue/range decoration now uses spatial material batches. High
  first-range draw submissions decrease by approximately35% in the captured
  preparation/aiming views. Moving flags, target state and athlete animation
  remain independent. High visual settings and shooting geometry are retained.
- Presentation targets60 FPS and restores the previous target on release.
- Snow is fitted around the shortened960m road. The authoring pass changes12697
  tile samples; exact road clearance then makes2698 small corrections, at most
  0.420m.984 trees receive updated heights. Standing crowd feet use the exact
  runtime terrain; their former cliff-like banks are filled and softened.
- No race physics, AI decisions, input semantics or replay policy changed.

Validation: native batching2, quality3, environment7 PASS; focused .NET3 PASS.
Native first-range CPU probe: mean6.13ms, max11.03ms including publication.
Final frozen-view frame windows: High preparation max3.918ms, aiming max3.253ms;
GPU means about2.00ms /1.13ms. These exclude active simulation and do not certify
an entire visible race or reproduce the owner's reported long hitch.

Visual evidence:
[forest shoulder](../quality/m13/perf-ground-20261010/snow-shoulder-160.png),
[roadside crowd](../quality/m13/perf-ground-20261010/snow-shoulder-435.png).
Both reviewed in native Game View. Initial frame CSV limitations and exact
decisions are recorded in [ADR-0047](../adr/ADR-0047-m13-scenery-batching-and-snow-ground.md).

Windows build Succeeded, Unity6000.6.0f1,0 errors/0 warnings,355704002 bytes.
Hidden Mass30/Sprint30 startup,400-tick controls and scene transition smoke
PASS, exit0. Rendering was suspended in that hidden check; no FPS claim.
Build/smoke receipts are retained in the evidence folder. Owner checks
visible preparation, post-range travel and road edges before M13 closure.
