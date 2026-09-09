# TASK-068B — Terrain plan into shared pace

Accept bounded planned effort adjustment only with a new tactical decision, hold it until the
next decision and expose it in PaceControllerFrame. Apply before ordinary effort saturation/
response. Test complete hilly Sprint with source lookahead, 3 Hz planning and 20 Hz physics,
exact replay, actual positive adjustments and invalid-update random-state preservation.
