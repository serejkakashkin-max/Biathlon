# TASK-047 — Shooting handling

Status: PASS, 2026-09-07. ShootingHandlingDefinition, ShootingEquipmentDefinition and their
JSON data produce a bounded skill/equipment profile. Real lifecycle setup/bolt durations
consume it. ShootingHandlingSystem exposes deterministic post-shot recoil and exponential
settling: discharge uses the existing aim, then recoil affects subsequent shots. Equipment
must fit the shared envelope; geometry receives no skill-based hit adjustment.

247/247 .NET and Unity EditMode pass; zero Console errors, four upstream warnings.
Evidence: artifacts/physiology/wp047-standalone.trx and wp047-{editmode,console}.json.
Actual second-shot timing differs by skill while valid rays still close targets. Recoil
onset, analytic decay, expert settling, saturation, equipment rejection, replay, invalid tick
and active zero-allocation tests pass. Prior deterministic regressions remain passing.

Neutral rifle is the only authored inventory item; career/equipment catalogue follows later.
Full aim+sway+recoil composition follows WP050. Handling tuning is provisional. No ski balance
change or commit. Next: WP048 pressure/composure.
