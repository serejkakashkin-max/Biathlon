# TASK-024 — WP-024 positional occupancy

Dependencies WP023 PASS. Shared ski placement contract, data-driven body extents/clearance, track
boundary checks and continuous swept-space query against a validated roster. Candidate motion must
not tunnel through another skier despite nonoverlapping endpoints. Stable earliest blocker IDs;
no Unity collision dependency. Tests boundaries, side-by-side, corridor isolation, crossing/tunnelling,
duplicate/default roster rejection and zero allocations. WP025/026 consume queries for resolution.
