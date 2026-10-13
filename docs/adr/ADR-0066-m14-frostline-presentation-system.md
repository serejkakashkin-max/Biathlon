# ADR-0066 — M14 FROSTLINE presentation system

2026-10-13. Owner-requested premium UI/UX revision; M14 acceptance remains open.

## Context

M14's screens already expose career, race, timing and replay data, but the owner
finds their composition too textual and visually inconsistent. The request
permits a substantial visual redesign while preserving real game mechanics and
data. A theme recolour alone would not address hierarchy and information density.

## Decision

Render and inspect three native UI Toolkit alternatives before choosing a
production direction. Select Frostline Broadcast: deep navy instruments,
glacier timing edges, Oswald sport headings/numerals and Roboto utility copy.
Retain the approved biathlete hero and existing ski/venue art. Keep licensed TTF
fonts local with their SIL OFL notices; no runtime font network dependency.

`FrostlineStyle` and a small USS sheet provide shared presentation materials,
fonts and transitions. Existing versioned menu/HUD definition files retain their
metric role. Menus receive an editorial home, event-first dashboard, paged
collections and consistent keyboard focus. Hidden-scrollbar vertical containers
remain an overflow/focus fallback for constrained layouts.

Race UI stays on retained elements and reads accepted observations. Numerical
speed and unit are separate; clock/pulse use display numerals; pace, reserve and
burst rails have retained segment dividers. Shooting keeps actual target, bolt
and breath state. Contextual readiness moves away from centre aim. Tab/F2 use
roster-backed nation flags and stable timing columns. No simulation call is
introduced into UI rendering and no new minimap or invented opponent state is
substituted for real observations.

Reduce motion controls menu drift/transitions and HUD heartbeat/trace animation.
Existing text scaling, contrast, RU/EN, pause, personal countdown/skip and input
semantics remain. A hidden waiting-skip control cannot intercept gameplay input;
its picking/focus state follows actual visibility.

## Consequences and validation

Presentation changes do not alter physics, AI decisions, athlete capabilities,
career progression, result timing, save schema or replay commands. Menu `ink`
becomes light and `paper` dark; HUD foreground and target interiors must use
explicit contrast instead of the previous inverse-token assumption.

Native visual acceptance includes menu/HUD geometry, real bound observations,
RU/EN at 100/115/130%, normal/high contrast and supported aspect ratios. Existing
normal/enlarged HUD area assertions remain mandatory. Compare warmed UI refresh
cost separately from world rendering. Run original start/burst/menu regressions
and inspect Console before a test build; the task report records actual results.

Some secondary pages inherit the shared design rather than a new bespoke
composition. Existing text formatting can allocate; no zero-allocation claim is
made. Visual specimens remain clearly distinct from real production captures.
No graphics-quality reduction is an acceptable performance substitute here.

Detailed rationale and system documentation:
`docs/ui/m14-wow/CONCEPT_SELECTION.md` and `DESIGN_SYSTEM.md`.
