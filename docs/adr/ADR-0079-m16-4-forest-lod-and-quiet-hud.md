# ADR-0079 — M16.4 forest transitions and quiet race HUD

2026-10-31. Owner reports forest becoming suddenly fuller on approach after WP-G,
and requests removal of obvious labels, persistent key instructions and development
explanations from the game interface.

## Reproduction and decision

The native single-tree camera test reproduces a37.56% change in canopy pixels
after moving2mm across the24m LOD boundary. WP-G already retains every visible
tree; the remaining discontinuity is the immediate replacement of imported LOD
meshes. The imported far fir is substantially simpler than the original near
model, so merely retaining instances does not solve this separate symptom.

Keep the original meshes, textures, foliage alpha coverage, material lighting,
placement, wind and fog. The optional complete-visibility definition now binds
12m/20m transition bands. Full original near detail remains through24m, blends
to the middle model over24–36m, then blends from the middle to the far model
over55–75m. Existing projection-aware zoom scales those distances as before.
Omitted bands retain historical hard LOD behavior for benchmark adapters.

Only two adjacent meshes can submit in a transition. An opaque alpha-tested
shader assigns complementary, stable screen samples to adjacent LODs. The
forward, depth and shadow passes share the same camera-relative fade. There
is no translucent sorting, new camera, fade texture, per-tree GameObject,
timer, state history or per-frame allocation. CPU preparation retains unique
visibility counts and exposes actual mesh submissions/transition counts and
raw triangle costs separately. Low/Balanced trees previously pinned to far
silhouettes remain pinned, without accidentally clipping them out at close
range. Instanced batches remain capped at500 and existing full-canopy/wind
frustum/fog bounds remain unchanged.

[Unity documents LOD dithering](https://docs.unity.com/en-us/engine/6000.6/manual/render-pipelines/universal-render-pipeline/urp-reference/universalrp-asset)
and the [instanced submission/bounds limits](https://docs.unity.com/en-us/engine/6000.6/script-reference/unityengine/graphics/rendermeshinstanced).
This adapter draws its own explicit-camera instances, so it applies its own
bounded fade rather than relying on a GameObject LODGroup.

## Interface

Remove the map title, rival count and legend; retain the actual route, distinct
player arrow, competitors and landmarks. Remove persistent pace/burst/key/help
captions and their reserved space, including menu navigation/pre-race footers.
Keep meaningful speed, pace, burst capacity, HR, energy, fatigue and shot evidence.
Shooting/breathing/approach text describes the accepted state, without key names
or implementation terms. Readiness expresses the effect on aim without control
instructions. Split history keeps its heading, result rows, selection mode and
arrows; existing keyboard/controller navigation is unchanged. Controls remain
available deliberately through Settings and F1. Remove unsolicited explanations
about OVR bonuses and fictional background results from career cards/roster.
Save-recovery and error messages remain useful product actions.

No Domain/Simulation/AI/Career/Replay or race tuning change; no scoring, movement,
input binding, save schema or deterministic event-log change. Qualification and
measurement limits: `docs/quality/m16-4/forest-ui/README.md`.
