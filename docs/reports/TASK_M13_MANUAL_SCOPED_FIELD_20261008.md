# M13 — scoped world publication through field timing and Pack state

2026-10-08. M13/WP-175/177/178 OPEN, WP-176 PASS. No Windows build.

`ManualScopedRosterSessionCommit` now prevalidates every session and the exact
world proof before publishing through either `SprintFieldSession` or
`PackSprintFieldSession`. Field timing, results, events and clock use the existing
prepared transaction. Pack caches actual scoped pose/velocity, physical station,
lateral line and draft; presentation snapshots read these detached accepted rows.
The world publishes last. Legacy movement cannot resume after scoped binding.
Initial context records immutable world policy and initial state once.

Reusable Sprint/Pursuit mat allocation is explicitly rejected until its resource
transaction is integrated. Finished actors are also outside this active-roster
bridge. Neither case silently falls back to legacy movement or a standing tick.

An independent course proof fixes safe straight-line rejection: the existing
whole-loop tube can conservatively reject a full body at lateral2 m on Woodland's
6 m straight because its reserve comes from a distant maximum bend. World v10
can instead prove all four planes of a strictly admitted source straight cell.
Body dimensions, clearance and contact tolerance are unchanged. The proof cannot
cross a cell or override a phase gate. Original outer start rows and bodies
straddling course seams still require composite course qualification.

Validation:

- Initial .NET9/10: fixture omitted authored start continuation; corrected setup.
- Expanded10/12 and diagnostic0/1 exposed the conservative Main tube rejection.
- Cell integration14/16 preserved the incoming outer start rejection. The cold
  four-row publication fixture subsequently uses paid legacy movement to lines
  +/-1.8 m and waits until the leading row is1.2 m into Main. This isolates field
  publication; it does not prove the original full-grid start seam. No production
  width/body/tuning or pose was changed to hide that remaining limitation.
- Fixture16/16, then final focused **94/94 .NET PASS**. Tests cover six phases,
  exact split timing, stale last-row rejection without earlier publication,
  foreign body/clock/resource rejection, detached presentation, and0 B warm
  independent/Pack prepare-publication loops.
- Native Unity **14/14** field cases and **4/4** cell cases PASS. Compile settled;
  Error Console empty. Discovery1435 is not the execution count.
- Broad default .NET **327/328**, one failure:
  `ManualScopedWorldRosterTests.RepeatedPreparedFullRosterQueriesAllocateNothing`
  measured440 B. This remains a failed qualification result; no test was removed,
  disabled or weakened, and no later focused PASS erases it.

[Exact evidence](../quality/m13/manual-scoped-field-20261008/).

This publishes admitted trajectories; it is not a live all-phase manual race.
Driven contact slip, persistent contacts, course/surface cuts, reusable mats,
finished actors, contact/seam replay journal, production host and broad allocation
qualification remain open. M13, playable queue behavior and final owner build
are not declared complete.
