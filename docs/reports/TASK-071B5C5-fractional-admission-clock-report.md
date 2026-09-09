# TASK-071B5C5 — Fractional admission clock

Added PrepareSubstep to JunctionAdmissionSession with integer start/end fractions (1e9 parts),
completion flag and the existing aligned connection rows. JunctionAdmissionPreview exposes
coverage. PendingFraction tracks committed coverage of Tick.Next(); Tick advances only after
explicit sealing at full coverage. Zero-time seams can occur between moving intervals or at
the endpoint before sealing, without consuming another tick. Every athlete must be stationary
in a zero-duration batch. Gaps, overlaps, early seals and out-of-range endpoints fail before
authority changes. Failed replacements continue to invalidate scratch handles.

Legacy Prepare/Step still represent a complete tick and preserve existing tests/event behavior.
The new exact-timing API requires seams to have zero duration. Three added tests verify movement
on both incident branches within one tick, event tick identity, invalid coverage and stale
handles, endpoint seam/seal, final position equivalence and zero warmed .NET allocation.

Evidence: 396/396 standalone and Unity MCP EditMode PASS, Console zero errors/four upstream
warnings, captures artifacts/physiology/wp071b5c5-*. Prior PlayMode 6/6 at B5C4 retained; no
visual/MonoBehaviour behavior changed. git diff --check passes with existing CRLF warnings.
No force/tuning or old race regression outputs changed.

Limitations: this enforces time coverage, not force-derived speed or joint race transactions.
Each committed substep is authoritative; a later substep failure does not roll back earlier
substeps. The field coordinator still needs whole-tick planning/replanning and coordinated
physiology/path/events commit. RaceEvent retains its integer tick and deterministic stream order;
it does not yet encode fractional timestamps. Boundary selection/rounding from motion times
requires a single explicit policy. No full shared-force pack Sprint is claimed. WP071 remains
IN PROGRESS and no owner gate applies.
