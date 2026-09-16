# TASK-153 — PBR snow materials

Implement WP153 after the verified WP152 rendering foundation. Original repository-authored URP snow shader and versioned cold/wet material presets; expose slope and roughness variation at world scale. No imported third-party artwork, no changes to authoritative surface friction, shooting or race tuning.

Generate assets and a repeatable close/mid-distance material fixture. Validate data bounds, material/shader parameters, shader compilation, representative flat/sloped geometry under directional lighting and preset contrast. Inspect Unity Console and screenshots. Integrate the material with the benchmark workflow; multi-surface terrain blending is WP157, tracks/VFX WP154–155. Carry M11 actions into the M12 sector and stop for G2 only after WP166.
