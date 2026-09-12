# TASK-133 — Shooting device input

WP133 complete. ShootingDeviceInput maps accumulated mouse displacement or held raw right-stick axes into existing fixed-tick WeaponAimCommand values. Left mouse/RT latches a single trigger edge, Space/left shoulder holds breath, right mouse/LT selects fine control. Device choice is explicit. Focus loss, missing device and disable clear pending motion/actions. Initialization accepts an authoritative initial tick; invalid tick requests do not consume pending input.

Sensitivity/deadzone/exponent are versioned in shooting_device_v1. Mouse sensitivity is converted with the configured simulation aim rate and tick frequency. Excess motion drains across bounded ticks instead of violating aim speed. Sampling must occur once per Input System frame. The host consumes one command per global tick; shooting lifecycle remains authoritative and may reject triggers during handling.

Validation: PlayMode virtual devices3/3 (18 discovered): packet accumulation, bounded catch-up, trigger edge, focus release, gamepad fine/breath and identical aim replay. .NET aim/session10/10; Unity EditMode shooting/session5/5 including pinned CSV/event trace. Console0 errors/1 warning. No hidden RNG, hit test, balance or shooting schema changes. No cursor locking/rebinding UI yet; playable host integrates context switching in WP136. Next: WP134 penalty presentation.
