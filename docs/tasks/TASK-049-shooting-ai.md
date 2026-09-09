# TASK-049 — Shooting AI controller primitive

Pure AI produces normalized WeaponAimCommand and trigger/hold intent from delayed observations.
Versioned reaction time, tracking error, stability window and max wait govern control quality;
no hit-percent or geometry bypass. Seeded smooth tracking bias only. Integrate with existing
WeaponAimSystem and ShotResolutionSystem in tests; verify full bout, command replay, reaction
holding, bounds and zero allocations. AI may reference pure Simulation, never UnityEngine.
