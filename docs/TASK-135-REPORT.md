# TASK-135 — Basic race HUD

WP135 complete. RaceHud displays selected athlete phase, lap, next range, completed-bout hits and timing. Detached RaceHudState compares arrivals at the selected athlete's latest checkpoint and labels the rank as a split with the number of recorded competitors. Sprint uses elapsed timing; Pursuit/Mass Start use global timing. Final snapshots use the format-specific authoritative result rank; Sprint gap uses registered units. Added read-only completed-bout mask access so shooting results survive range exit.

Validation: .NET HUD/full Sprint and detached snapshot twin-race2/2; Unity EditMode full Sprint HUD1/1 (544 discovered); PlayMode display/reset1/1 (20 discovered). Final Sprint ranks/gaps match registered result rows, intermediate gaps match the timing board and event/movement parity is unchanged. Game View final HUD inspected (docs/quality/m10/wp135-hud.png). Console0 errors/1 warning, compilation_failed=false.

No gameplay or balance changes. Provisional split rank is not current physical order; final results are shown once the whole field is complete. HUD capture allocates strings on presentation publication; this is outside simulation stepping. Pursuit/Mass Start display branches await format-specific playable acceptance in WP137/138. Next: WP136 playable Sprint host and scene composition.
