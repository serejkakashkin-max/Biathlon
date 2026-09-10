# TASK-085 — Mass Start grid and launch

Status: IN PROGRESS — 2026-09-10.

TASK-085A: versioned physical grid, common start clock and pack-kernel launch contract. Thirty
stable slots, body/edge validation, real rear-row offsets, repeat events and first-minute swept
occupancy checks. Verified in .NET/Unity; see TASK-085A-REPORT.

TASK-085B NEXT: integrate the start-area approach with authoritative athlete physiology and the
race route. Preserve common StartTick and actual rear-row travel; do not teleport rear rows to
route zero or count grid-origin translation as completed distance. Existing RacePathMotion rejects
negative route coordinates; choose an explicit approach adapter and validate its boundary/time
handoff. Do not mark WP085 complete before the actual race launch composition is verified.

TASK-085B1 DONE: explicit physiological approach, exact fractional route handoff and projection; full .NET475/475 and focused Unity10/10. See TASK-085B1-REPORT. TASK-085B2 NEXT: shared30-body field and lateral-safe approach seam.

WP085 DONE: TASK085B2 verifies30-body physical launch and exact common-clock integration; see report/ADR0016.
