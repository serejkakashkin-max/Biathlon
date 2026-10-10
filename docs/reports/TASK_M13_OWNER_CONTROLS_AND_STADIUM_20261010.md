# M13 — owner controls and stadium review candidate

Date: 2026-10-10. Owner acceptance OPEN; M14 has not started.

## Delivered changes

- RMB manually cycles the bolt before the first shot and between shots; the
  fifth shot exits without another cycle. LMB fires, Space holds breath, F is
  fine aim. Reload is an input edge, not repeated every frame while held.
- The HUD shows actual heart rate, breath debt, forced release/recovery and
  chamber status. Existing heartbeat/breath physiology controls sway; a long
  hold eventually increases tremor and requires recovery. Geometric shot
  acceptance stays unchanged, with no hidden hit RNG.
- A sharper triangular front post and narrower 3.5°/2° camera view make the
  physical targets larger on screen. Rifle motion follows the bolt phase.
- W/S ordinary pace is capped at 80%; Shift attack requests 100%, Ctrl recovery
  requests 35%. Existing reserve and fatigue are visible and affect capability.
- Space holds the low posture on any terrain until released; steering does not
  stand the athlete up. Low effort and reduced steering remain its trade-offs.
  Uphill presentation uses shorter/wider steep strides and an offset arm push.
- The 40 m start approach is straight, with a separate original start gantry.
  Painted start boxes and their crossing-grid visual artifact are removed.
- Low, alternating T-post dividers are more frequent. Thirty separate wind
  cloths observe actual wind; calm conditions do not show a fabricated gust.
  A low white V-board fence borders the penalty loop with an entry opening.
  Yellow penalty paint is removed; cyan guidance selects the penalty entrance
  when misses require it. The separate finish uses low blue pickets.

## Performance correction

The pathological work was in authoritative snow-boundary queries, including
AI actors on the penalty loop, rather than solely graphics. A captured actor
required 78,526 disk queries / 318 clearance samples in one tick. Simple
per-edge pruning alone still measured a 472.214 ms worst tick.

The final algorithm bounds groups of the existing fine-grid disk centres using
the same conservative tubes and simple polygon, then visits only groups that
can improve the minimum. Its exhaustive-grid comparison includes inside,
folded and outside poses. Oriented body dimensions, grid leaf radii, course
thresholds, collision response and clock atomicity remain intact. Old failing
course fixtures now derive edge positions from current venue width, retaining
both stationary and moving outside rejection cases.

Experimental smaller sweep budgets and alternative contact-normal fields were
discarded. No strict whole-race gate was introduced. Visible perceived FPS,
final setup, off-course response and post-range hitches remain owner checks.

Final optimized **Release .NET CPU diagnostic** (`cpu-shipping-release.trx`):
12,000 ticks, mean2.348 ms/tick, setup mean2.774 ms/tick. The largest tick was
the cold first tick,134.203 ms. Every >100 ms tick is logged; none occurred
after tick1. The actual player had completed one bout and was skiing;29 of30
AI first bouts completed within this bounded horizon. This is not a full-race
completion check or a Windows GPU/FPS measurement. Debug and Release timings
are different configurations and must not be presented as a direct speedup.
Visible final-setup/off-course performance still needs the owner's build test.

## Owner build

`Builds/WoodlandM13OwnerControls-20261010/START_WOODLAND_REVIEW.cmd`.
Russian controls and review notes: `READ_ME_RU.txt` beside the executable.
Unity6000.6.0f1 Windows build: **Succeeded**,0 errors,496 existing inference
package shader warnings;20.13 seconds,359,354,418 reported build bytes.
Hidden Mass30/Sprint30 startup,400 paid ticks per scene, quality selection and
actual scene transition: **PASS**,EXIT0. Hidden rendering was suspended, so this
is content/control evidence and does not establish sustained visible FPS.
Smoke artifacts: `artifacts/production-player-smoke/20261010-135301-585/`.
Executable SHA256: `718444f6a718fa668da96eb0a58445b009673310e6129152a63dbf7a92b8de00`.

The failed console-cache clear was a Unity-MCP log-file sharing error. Read-only
recent Console inspection after compilation and the final native tests returned
no errors; post-build exception inspection was also empty. No cache files were
deleted to bypass the lock.

## Validation

- `shipping-core-verified.trx`: 68/68 focused .NET cases PASS before the final
  cover optimization, including manual shooting, breath/HR, input, tuck,
  course rejection and deterministic replay.
- `hierarchical-cover-shipping-core.trx`: 27/27 focused cases PASS after the final optimization,
  including an exhaustive disk-grid oracle, unchanged edge rejection, replay
  and zero-allocation warm cover checks.
- Native Unity: shooting input 4/4, technique 7/7, actual mat approach/manual
  reload/departure plus rejected-tick clock behavior 2/2, Mass30
  stadium/environment 1/1 PASS. These are executed
  counts, not the complete editor test inventory.
- Four native 1600×900 captures inspected: `rifle-prone.png`,
  `start-straight.png`, `penalty-fence.png`, `finish-markers.png`.

Evidence directory: `docs/quality/m13/owner-controls-20261010/`.
Decision: [ADR-0045](../adr/ADR-0045-owner-shooting-controls-and-stadium.md).

## References and scope

[IBU 2026 Event and Competition Rules, chapter 3](https://assets.ctfassets.net/cz0vl36hcq0x/7mkQn1kQTB7VzlUZHAcuec/c8897efcf14ccded0f82af3513c6e5ef/ECR_Rules_2026_cap03.pdf)
informs T-posts, windcloth placement, penalty V-boards and low finish markers
(articles 3.4.3.1, 3.4.6, 3.3.11, 3.5.1). The candidate is not a fully certified
IBU venue. [PSIA-linked cross-country education](https://xcski.org/education/)
informs distinct steep uphill technique.

No new third-party production assets were imported. Existing credits travel
with the build. One Woodland venue, maximum30 racers and High default remain.
The Master Plan and historical state are preserved. The selected input/tuck
definitions and initial straight geometry can change race output; the rifle,
flags, fences and animation are presentation only.

Next: owner tests this candidate and decides M13 closure. No automatic M14
transition or owner acceptance claim.
