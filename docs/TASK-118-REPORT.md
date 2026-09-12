# TASK-118 — Equipment inventory and progression

WP118 complete. Athletes can select only owned, catalog-valid skis. Comparison exposes condition-adjusted glide, grip, stability, combined cornering and purchase cost directly from the existing catalog. Glide and soft-snow options have explicit tradeoffs; ownership does not stack bonuses or introduce rarity rolls.

Validation: .NET28/28 (career24 plus existing ski equipment4); Unity inventory1/1, 534 discovered; Console0 errors/4 retained warnings. Tests cover soft/icy comparison differences, unowned selection atomicity, repeat selection, switching back and save/load persistence. Existing ski physics tests remain green; no race tuning changed.

Comparison is a headless DTO for later UI. Career-selected equipment is wired into race entry in WP121. Next: WP119 rivalry/history.
