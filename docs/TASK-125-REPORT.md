# TASK-125 — Presentation state adapters

WP125 complete. RacePresentationSnapshot captures detached completed-tick values: stable identity, physical loop/distance/lateral position, speed, physiology, race phase/laps/bouts/penalties, range lane and shooting targets. No mutable session/map reference is exposed. RacePresentationFeed accepts increasing snapshots and explicitly resets between races.

Adjacent compatible snapshots interpolate scalar positions for frame-rate presentation. Branch/phase changes, skipped ticks and loop wrap snap to current position, avoiding invented geometric shortcuts. Geometry and seamless curve sampling belong to WP126; presentation never writes interpolated values back to authority. Captures allocate detached arrays outside the critical simulation tick loop.

Validation: focused .NET1/1 and Unity EditMode1/1 (542 discovered). Twin100-tick races with/without capture retain identical motion and event hash; historical snapshots remain unchanged. PlayMode7/7 includes a complete shared Sprint publishing through the MonoBehaviour feed, rejecting duplicate ticks and resetting. Console0 errors/1 retained warning. Race tuning and authority are unchanged. Next: WP126 track geometry authoring.
