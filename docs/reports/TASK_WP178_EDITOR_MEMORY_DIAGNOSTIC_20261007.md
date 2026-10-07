# WP-178 — post-cancel Editor memory diagnostic

Date: 2026-10-07. This is a diagnostic receipt, not a venue performance pass.
WP-178 and M13 remain open.

The already-open Unity Editor ran a long Woodland career EditMode test through
Unity MCP. The HTTP request timed out while Unity's test runner continued. The
test was cancelled through Unity's Test Runner API, and the plugin subsequently
reported no active runner job before its orphaned request lease was cleared.
Neither the partial test output nor the cancellation is a PASS result.

The [hidden Editor snapshot](../quality/m13/wp178-memory-20261007/editor-after-cancel.json)
was taken after that cancellation. The process working set was 11,121 MB and
private bytes 30,772 MB. Unity's profiler reported a Mono heap capacity of
27,505 MB but only 166 MB of Mono-used memory at that instant; total reserved
memory was 2,892 MB and allocated memory 1,005 MB. The large heap *capacity*
does not prove that live gameplay objects occupy 27 GB. It does establish that
running long career races inside this Editor session is a poor substitute for
an isolated player or headless performance measurement.

This snapshot contains no GPU frame timing, sustained FPS, draw-call matrix,
broadcast shot, or worst-case 30-athlete Mass Start result. It was deliberately
not taken from a visible game window. The earlier owner instruction permits
hidden checks only. Do not call WP-178 PASS or extrapolate an Editor-after-test
process footprint into the Windows player.

Next: use bounded exact native methods for compile/behavior qualification;
run a separate hidden player/benchmark process for a 30-athlete Mass Start and
broadcast shots when the final M13 gameplay batch is integrated; collect
frame-time, draw-call, GC and peak process-memory receipts per quality tier.

## Headless season throughput, 2026-10-07

The later isolated Release .NET WP-176 season test completed 16 real
30-athlete Woodland races, totaling 394,978 unique field ticks at the
configured 20 Hz. Its elapsed test duration was 13m45s, including weekly
career work, save/restore and explicit collection of discarded test-runner
graphs between races. This is about 479 field ticks/s, or roughly 24 times
realtime for the sequential headless test. The
[TRX](../quality/m13/wp176-woodland-career-20261007/wp176-full-production-season.trx)
contains per-event ticks and managed heap retained after each event
(7.1–7.7 MB). This is not a worst-case instantaneous tick latency, player
frame rate, GPU, broadcast camera or LOD result. The earlier Editor memory
snapshot and the separate testhost footprint must not be conflated.
