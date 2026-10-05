# AGENTS.md — BIATHLON

## Mission
Build a modern, deterministic, skill-based biathlon simulation and career game in Unity 6. Codex is expected to implement, test, debug and iteratively improve the project with minimal manual Unity Editor work.

## Source of truth
Read before making architectural or gameplay changes:

1. `docs/BIATHLON_GDD_TDD_v0.1.md`
2. relevant ADRs under `docs/adr/`
3. tests for the subsystem being changed

If code conflicts with the GDD, do not silently redefine the design. Either make the code conform or create a clearly documented proposal/ADR.

## Architecture rules

- `Biathlon.Domain`, `Biathlon.Simulation`, `Biathlon.AI`, `Biathlon.Career`, and `Biathlon.Replay` must not reference `UnityEngine`.
- Unity MonoBehaviours are adapters/presentation, not authoritative gameplay logic.
- Use fixed-tick deterministic simulation.
- All gameplay tuning values must be data-driven; avoid magic constants in system code.
- Do not use Unity Random in simulation core. Use the project seeded `IRandomSource`.
- Do not use object names in a scene as gameplay identifiers.
- Save games use stable IDs and versioned schemas, not Unity object references.

## Gameplay invariants

- Never determine a player shooting hit/miss with hidden RNG.
- AI obeys the same race physics and shooting geometry as the player.
- Difficulty may alter AI decision quality, reaction time, planning horizon and control precision, but never hidden speed/power multipliers.
- Athlete stats affect control difficulty and physical capability; they must not silently override a geometrically valid player shot.
- Every major high-performance action should have a cost/trade-off.

## Test policy

For every system change:

1. add/update unit tests;
2. add/update integration tests when cross-system behavior changes;
3. run the relevant test assembly;
4. run deterministic regression tests if race output can change;
5. inspect Unity Console for Unity-facing work;
6. never delete/disable a failing test merely to make the suite pass.

Statistical tests must use fixed seeds, sufficiently large samples and non-flaky tolerances.

## Observability

New race-affecting systems must expose useful state to debug/telemetry.
Prefer structured machine-readable logs for automated Codex analysis.

Important race events must go through the replay/event-log system.

## Performance

- Avoid managed allocations in critical per-tick loops.
- AI tactical planning does not need to execute every simulation tick.
- Headless simulation must be able to run faster than realtime.
- Favor simple deterministic structures over expensive Unity physics for authoritative race logic.

## Unity workflow

When a task touches Unity:

1. let Unity compile;
2. read Console;
3. fix all compile errors;
4. run relevant EditMode/PlayMode tests;
5. enter Play Mode if behavior needs runtime validation;
6. inspect Game View / screenshots when visual behavior is part of acceptance criteria.

Do not rely on a human to configure Inspector fields manually if setup can be generated or validated by code.

## Data

Every versioned definition should include:

- `schema_version`
- stable `id`

Do not duplicate tuning constants across JSON, ScriptableObjects and code. Prefer one authoritative data source with adapters.

## Git

Make small coherent changes. Do not mix architecture rewrites, gameplay tuning and visual polish in one task unless explicitly required.

Use conventional-style commit messages where practical:

- `feat(sim): ...`
- `feat(ai): ...`
- `fix(shooting): ...`
- `test(sim): ...`
- `docs(gdd): ...`

## Before declaring a task complete

Report:

- files/systems changed;
- tests run and results;
- deterministic/balance impact if applicable;
- known limitations;
- next logical task.

A task is not done while the project has compile errors or required tests are failing.

## Production state history

Owner directive 2026-10-05: `docs/PROJECT_STATE.md` is append-only. Never delete,
compact, summarize away, or replace historical entries. Append dated progress,
corrections, evidence and the latest operational state at the end of the file.
Historical statuses keep their original dates; the latest appended record controls
current work. Preserve the history from Foundation onward.
