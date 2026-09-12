# TASK-129 — Chase camera

WP129 complete. AthleteChaseCamera follows a selected proxy with exponential position smoothing and forward look-ahead. Versioned chase_camera_v1 settings own distance, response and clearance. Ground probing and sphere-cast arm shortening avoid visual obstacles without feeding Unity physics into authoritative race state.

Validation: Unity MCP PlayMode camera test passed 1/1 (11 discovered), covering following, target immutability, obstruction clearance and invalid delta time. Game View preview inspected: docs/quality/m10/wp129-camera.png. Console: 0 errors, 1 retained warning. A test initially required camera elevation above a block even when the arm correctly shortened in front of it; the final assertion verifies geometric clearance directly.

No simulation or balance changes. Obstacle detection requires colliders on the selected environment mask; current schematic venue falls back to athlete-relative minimum height where no terrain collider exists. Camera feel awaits G1 hands-on review. Next: WP130 fixed-tick input integration.
