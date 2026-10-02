# TASK169X — explicit environment preview cleanup

2026-10-02. The actual T mesh diagnostic exposed an Editor lifecycle defect: generated backstop colliders used deferred `Destroy`, which logs an error in EditMode and leaves the collider attached. An added native Editor test reproduced this failure. Switching to the existing mode-aware owned-object cleanup removed it, then the same test exposed a second problem: disabling the weather component did not invoke its runtime release callback in EditMode, leaving the preview sky applied.

`ProductionVenueEnvironment.RemoveColliders` now uses its existing `DestroyOwned` helper. `WeatherPresentation.Shutdown` explicitly restores the weather lease, disables callbacks, releases owned mesh/material using the appropriate Editor/runtime API and clears its references. Environment shutdown calls this method before removing generated objects; weather destruction calls the same idempotent cleanup. Shared source materials/prefabs remain owned by Resources. No fog values, palette, lighting recipe, track, simulation, AI or shooting changes are part of X.

The new Editor regression constructs an actual small environment with two banks/backstop and cloned native art. It verifies immediate collider removal, unchanged source prefab colliders, owned terrain/weather material release, exact sky/fog restoration and a second complete construction/shutdown with a fresh lease. Both original negative results are retained. Final Editor1/1 PASS; existing PlayMode weather4/4, production fog7/7 and environment7/7 PASS (18 related tests,204 discovered). These are focused results, not the full PlayMode suite. Unity compilation is clean and these successful runs captured no Error logs.

S source hashes belong to its preceding checkpoint; X deliberately changes only cleanup in those two presentation files. T geometry/full-suite/capture and Y directed passage verification remain separate work. No new Windows build or WP169/M13 completion is claimed.

[Bounded proof](../quality/m13/wp169x-editor-environment-lifecycle.json) binds source/result hashes and both retained failures. Evidence lives under `artifacts/wp169x`.
