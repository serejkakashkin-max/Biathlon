# TASK-056 — Range and penalty traversal

PASS, 2026-09-08. RangePenaltyTraversal binds a bout to authored route anchors, clamps at
the firing line, accepts a completed shared shooting result and requires one actual circuit
per unclosed target. Integer distance accounting retains the ordinary entry/rejoin segment;
penalty terrain can be sampled separately. Phase/loop events and consumed/unconsumed motion
are exposed. ADR-0009 records topology, stop and physics-substep responsibilities.

286/286 standalone and Unity EditMode pass; Console zero errors, four upstream warnings.
Evidence: artifacts/physiology/wp056-{standalone.trx,editmode.json,console.json}.
Actual shooting resolver tested with 0–5 geometric misses: exactly 100 + 150×misses metres
from range entry to rejoin, including unchanged 70 m after firing. Constant one-metre tick
input demonstrates the corresponding extra time. One-nanometre boundary, duplicate/early
completion, second-lap offsets, multi-circuit crossing, ambiguous topology, replay and
100k active allocation-free updates pass. Initial test used the wrong fixture segment ID;
corrected against authored penalty_arc. No gameplay tuning or baseline hash changes.

This is the distance adapter, not the complete race/physics orchestrator. WP059 must activate
it at range entry, split physics at surfaces, reset speed at the shooting stop, and separate
range arrival from WP054's intermediate combined lap notification. No Unity visuals required.
Next: WP057 checkpoint timing and gaps. No owner gate or commit.
