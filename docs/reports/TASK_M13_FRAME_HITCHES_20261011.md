# M13 — presentation hitches without graphics cuts

Owner request: investigate continuing stutter without lowering graphical quality.
M13 remains OPEN for owner review; this is not a claim of a hitch-free full race.

## Confirmed faults and changes

1. **Setup/exiting animation ignored render interpolation.** Body lowering,
   rifle transfer and the athlete's turn used integer phase elapsed ticks at
   20 Hz. The real-bout native regression sampled alpha .25 and .75 and failed:
   transfer remained .75 in both frames. Body, rifle and turn now share the
   fractional presentation time. Exact completed-tick endpoints remain intact;
   the shooting clock, hit geometry and controls are unchanged.
2. **Catch-up frames discarded intermediate snapshots.** A real host frame
   covering .175 seconds produced a previous/current gap of three ticks. The
   world-pose sampler deliberately refuses such a pair and snapped forward.
   The host now publishes each completed observation, retaining adjacent states,
   and supplies the actual scheduler remainder for render interpolation instead
   of restarting the interpolation clock at zero on each published frame.
   UI strings still refresh only once per render frame.
3. **Static forest geometry was rebuilt on the CPU every frame.** World tree
   matrices and conservative canopy bounds now cache until the grove transform
   changes. Camera frustum tests, LOD selection, wind, shadows and fog still
   update normally. No tree, mesh, texture, quality value or material is removed
   or reduced. A 768-tree, 1,500-iteration native preparation probe changed from
   220.2741 ms to 138.6849 ms (37% less time in this isolated operation), with
   zero managed allocations in both versions and identical visible count,
   triangle count (1,059,966) and combined bounds.

## Verification and interpretation

Evidence lives in `docs/quality/m13/frame-hitches-20261011/`.
The failing/passing tests exercise real race observations and the real host,
not a reimplementation of interpolation. Native rifle tests: 4 PASS. Native
host/automatic range/failed-tick tests: 3 PASS. Vegetation tests: 3 PASS.
Production forest visibility tests: 9 PASS. Core presentation/assistance tests:
9 PASS. No authoritative simulation or AI code/tuning changes in this task;
fixed ticks and their gameplay results are preserved. Scheduling and visual
presentation between accepted ticks change intentionally.

The initial profiler capture included editor startup and tool compilation,
so its large editor/GC totals are NOT attributed to gameplay. An attempted
deep-profile capture overloaded the editor, yielded no usable profile and was
discarded; the editor was restarted. The bounded all-AI native before trace
separately found simulation spikes up to 29.9 ms. This task does not claim those
are all eliminated by the presentation fixes. Forest microbenchmark savings
are not an overall FPS percentage. Owner testing remains necessary to judge
the remaining complete-race hitches on the standalone player.

Windows build: `Builds/WoodlandM13FramePacing-20261011/START_WOODLAND_REVIEW.cmd`.
Build succeeded with zero errors/warnings, 355,748,956 bytes. Hidden standalone
Mass30/Sprint30 startup, 400-tick controls and scene transition passed, exit 0;
rendering was suspended, so this supplies no FPS/GPU evidence. Russian controls
and suggested owner scenarios are in the build's READ_ME_RU.txt.

Owner feedback after playing: setup/prone transition hitches are gone; first
bend and active shooting still need investigation. Continued under
`corner-aim-hitches-20261011`; M13 remains open.
