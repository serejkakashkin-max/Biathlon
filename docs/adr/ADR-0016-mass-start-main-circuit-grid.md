# ADR-0016 — Mass Start grid on the main circuit

2026-09-10. Extends ADR0012/0014/0015 without changing their default policies.

The explicit PhysicalStartApproachMap represents a grid occupying the terminal portion of the
main circuit before the start line. It is one physical corridor, not an exclusive branch junction.
Rear-row physical coordinates are mainLength minus remaining approach. Approach receipts still
carry zero route credit; after crossing, ordinary main-loop mapping continues without displacement.
Existing periodic continuous body sweeps therefore see front and rear rows on both sides of the
line, including during the fractional crossing tick. No new ghost exemption or collision bypass.

This opt-in map requires an open single-segment approach exactly matching the constant terminal
main-loop terrain and all movement metadata. It rejects mismatched width/snow/grade/curvature,
foreign track/route snapshots and unsupported geometry. The synthetic track generator copies the
terminal segment. A venue with a separate branch approach needs a separately validated adapter.
Initial lateral fit uses the actual start corridor width. Thereafter the existing paid, bounded
steering approaches its conservative race-wide target limits; initial coordinates never jump.

All grid bodies exist physically before the gun. One scheduler emits all official start events;
movement/lateral motion begins after the same start tick. Front-row references and rear offsets
are immutable. Field grid/body/profile/roster bindings validate before events. AI preview ignores
only the nonphysical approach alias when validating physical-loop radius; actual loop checks remain.

MassStartResultSnapshot retains exact grid context and global finish order, with exact ties sharing
ranks and stable ID sorting only for canonical tied display. Reload requires caller-owned grid and
clock. Complete finisher snapshots only; no fabricated qualifying Sprint or elapsed ranking.

TASK085B2 verifies30 physiological bodies over1200 ticks and six complete AI races, with .NET/Unity
agreement. This closes launch WP085. Broader PPSS, lane/order policy and30-AI stress remain WP086–089.
