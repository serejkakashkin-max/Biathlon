# ADR-0045 — Owner shooting controls, voluntary tuck and stadium presentation

Date: 2026-10-10. Status: Accepted for the M13 owner review candidate.

## Context

The owner requests manual bolt operation before the first shot and between shots,
readable breath/heart-rate feedback, distinct ordinary pace and attack, voluntary
tuck, and stadium dressing based on real biathlon. M13 acceptance remains the
owner's decision after testing; this does not amend the Master Plan or start M14.

## Decision

Add optional `CycleBolt` / `ManualBoltCycle` commands to the deterministic input
stream. A manually operated session starts unchambered, consumes a bolt action
before each shot, and exits after shot five without another cycle. The existing
automatic policy remains compatible with older tapes and simulation callers.
Record the selected policy in the shooting context and preserve both commands
through single-athlete and pack command tapes. No hidden hit/miss RNG is added.

The existing heartbeat, breathing and breath-debt systems remain authoritative.
Expose their actual state in the snapshot and shooting HUD. Space holds breath,
RMB cycles the bolt, F requests fine aim. Display forced release and recovery.
Increase optical target size through camera FOV, preserving 50 m distance and
115/45 mm standing/prone hit geometry. Replace the original front post with a
pointed project-authored insert and animate the rifle during the actual bolt phase.

Select versioned gameplay resources for keyboard pace and tuck. Ordinary keyboard
effort saturates at 800; Shift requests attack at 1000, Ctrl requests recovery.
The existing reserve/fatigue model supplies their cost. Requested tuck stays low
on any terrain, including while steering, until released. Its existing low effort
ceiling, drag reduction and steering cost remain. Locked historical resources are
preserved. AI still chooses posture through its controller and uses the selected
physical model; no AI speed multiplier is introduced.

Make the existing 40 m start approach straight and add an original start gantry.
Delete the visual painted grid without deleting simulation start-slot metadata.
Use low, alternating T-post lane dividers, separate real-wind cloth, a low V-board
penalty fence with an entry opening, and low blue finish pickets. Route guidance
selects the penalty entrance only when penalty loops are owed. These presentation
meshes have no authoritative collision or race identity role.

Prune geometrically impossible clearance candidates using conservative bounding
boxes and stop minimum-cover queries when they cannot improve the known maximum.
Bound groups of the unchanged fine-grid disk centres by tube distance and, when
the complete centre group stays inside it, simple-polygon boundary distance.
Skip a group only when its conservative lower bound cannot improve the minimum.
Cache polygon edge geometry once per local ribbon. Evaluate the cheap collinear
certificate before the tube certificate. Preserve full oriented bodies, course
domains, admission thresholds, clock atomicity and rejection tests. Do not ship
the experimental contact-normal shortcuts or reduced sweep-depth trials.

## Consequences and evidence

The new manual bolt policy and selected pace/tuck definitions change commands,
shooting timing and race output; old tapes retain their prior policy. The straight
start changes initial world headings and track geometry. Record/replay checks and
course edge rejection checks are required. Meshes, view FOV and HUD do not change
geometric shot acceptance. Uphill animation changes presentation only.

Focused checks and native screenshots are in
`docs/quality/m13/owner-controls-20261010/`. CPU timings are diagnostic evidence,
not portable FPS guarantees. Any remaining timing spikes stay explicitly open.

Presentation references: [IBU 2026 Event and Competition Rules, chapter 3](https://assets.ctfassets.net/cz0vl36hcq0x/7mkQn1kQTB7VzlUZHAcuec/c8897efcf14ccded0f82af3513c6e5ef/ECR_Rules_2026_cap03.pdf)
(articles 3.3.11, 3.4.3.1, 3.4.6 and 3.5.1), and
[Cross Country Ski Areas Association / PSIA education](https://xcski.org/education/)
for uphill V1 versus flatter-terrain V2 technique. This candidate is not a claim
of full IBU venue certification. No external production assets were imported.
