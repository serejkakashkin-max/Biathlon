# TASK-071B5C8 — Physical receipt projection

Added PhysicalMotionProjection/PhysicalMotionReceipt, an internal section-boundary check in
PhysicalTrackMap, PhysicalMotionProjectionTests and expanded full-Sprint timeline integration.
Projection binds an explicit physical origin to one timed receipt. Main origins must match
race progress and remain in one route section (end boundary allowed). Penalty origins must be
on the penalty loop and are supplied by traversal state; race progress cannot infer them.

Projected receipts retain explicit travel, incident endpoint coordinates and fractional time
bounds. Slice computes differences of cumulative interpolated nanometres, preserving total
distance across adjacent slices and closed-loop seams. Complete circuits are not mistaken for
zero motion. Foreign/default receipts, invalid intervals, mismatched origins, multi-section
main movement and oversized loop travel fail. Original receipt validation is reused.

Tests cover complete-circle and main-lap seams, nanometre conservation, invalid origins and
crossings, foreign maps/receipts and warmed .NET allocation. The full real Sprint now projects
every main/penalty receipt and sums its physical slices to the exact original travel. Evidence
uses artifacts/physiology/wp071b5c8-*. No old physics, tuning, shooting or replay outputs changed.
405/405 standalone and Unity MCP EditMode PASS; Console zero errors/four upstream warnings.
Prior PlayMode 6/6 at B5C4 retained; no new visual behavior. git diff --check passes with existing
CRLF conversion warnings. .NET provides allocation evidence; Unity's counter limitation remains.

Limitations: penalty local origin is trusted traversal input after map validation, not inferred
from race progress; field integration must supply it consistently. Branch changes require
separate authored seam admission. This projection is not force resampling, automatic follower
replanning, full physical race orchestration or a coordinated Sprint transaction. WP071 remains
IN PROGRESS and no owner gate applies.
