# TASK-141 — Humanoid presentation architecture

Status: PASS. Dependencies: WP125–140 verified; owner G1 approved 2026-09-13.

Objective: replace the capsule consumer with a reusable, replaceable humanoid rig and detached animation adapter. Keep existing camera and simulation contracts.

Scope: typed bone/socket binding, original unbranded development athlete asset and prefab generator, stable-ID lifecycle, animation consumer, observational snapshot additions (TASK141A), PlayMode tests and fixed Game View captures.

Constraints: no simulation root motion, Unity dependencies in core, numerical shooting changes or control redesign. Reference images are guidance only. The original development rig is not a final visual quality claim. External production assets require source/license/status.

Acceptance: 30 stable identities follow snapshots; a rig can be replaced without race changes; all required equipment/camera sockets resolve without name lookup; animation and render calls cannot change authority; serialized prefab survives instantiation; relevant tests pass, Console has no errors, humanoid/equipment silhouette visible in Game View. Locomotion and later transitions remain WP142 onward.

Validation: focused .NET snapshot tests; Unity EditMode snapshot tests; PlayMode binding/swap/lifecycle tests plus existing proxy/camera/host checks; MCP runtime capture.

Risk/rollback: generated development asset is reversible and independent of tuning; retain public proxy lookup used by chase camera. Revert only this task's explicit files, preserving owner local settings and package changes.
