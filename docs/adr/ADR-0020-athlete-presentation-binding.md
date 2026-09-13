# ADR-0020 — Athlete rig and animation boundary

Accepted for WP141 following owner G1 approval (2026-09-13).

The fixed-tick host publishes detached observations. Unity maps these observations to animation inputs; the mapping never owns a race session. `AthleteProxyPresentation` retains its existing stable-ID lookup and camera pivot contract, now containing a replaceable `AthleteRigBinding` and `AthleteAnimationPresentation`.

The placement pivot remains one metre above the sampled course for M10 camera compatibility. The child visual model has a ground-level origin. Typed bones/sockets are explicit serialized bindings, not object-name searches or athlete IDs. +Y is up, +Z is forward, units are metres. Asset-specific correction transforms may adapt another skeleton to these axes without rewriting gameplay.

Animation only affects bones under the model root. Animator root motion is disabled; pose consumers receive value observations only. Rendering restores placement/model-root transforms after procedural pose application. CameraTarget is a stable composition anchor; body sway must not become camera recoil or authoritative aim.

Required sockets: left/right hand grip, left/right ski binding, rifle back mount, rifle aim axis and camera target. They are independently mapped; missing, duplicate and foreign bindings fail validation. Material and LOD data belong to assets/presentation, never to gameplay tuning.

WP141 supplies an original unbranded development rig for pipeline validation. It does not approve final character quality. WP142–149 add state-specific motion; WP150 measures crowd/LOD behavior; WP151 records repeatable motion evidence. A licensed asset decision is raised only if actual quality evidence makes it necessary.
