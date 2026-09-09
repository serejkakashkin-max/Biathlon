# CODEX TASK 001 — Agent-ready foundation

## Objective
Create the initial Unity 6 + pure C# project structure for BIATHLON so future gameplay can be implemented and tested by Codex without putting simulation logic inside MonoBehaviours.

## Required repository layout

```text
/
  AGENTS.md
  docs/
    BIATHLON_GDD_TDD_v0.1.md
    adr/
  src/ or Assets/Scripts/
    Domain/
    Simulation/
    AI/
    Career/
    Replay/
    Unity/Presentation/
    Tools/BalanceLab/
  tests/
```

Exact physical layout may follow Unity conventions, but logical assembly boundaries are mandatory.

## Required assemblies

- Biathlon.Domain
- Biathlon.Simulation
- Biathlon.AI
- Biathlon.Career
- Biathlon.Replay
- Biathlon.Unity.Presentation
- Biathlon.Tests

Core assemblies must compile without UnityEngine references.

## Implement minimal kernel

Create:

```text
SimulationTick
SimulationClock
SimulationConfig
SimulationWorld
AthleteId
AthleteState (minimal placeholder)
SimulationInput
RaceEvent
IRaceEventSink
IRandomSource
SeededRandomSource
```

No skiing/HR/shooting logic yet.

## Determinism

Given the same:

- seed;
- initial world;
- input sequence;
- fixed tick count;

the simulation must produce identical final state and identical ordered event stream.

## Tests

### Fixed tick clock

After N ticks at configured frequency, elapsed simulation time is correct within deterministic representation.

### Same seed reproducibility

Run identical 10,000-tick simulation twice; final state/event hash is identical.

### Different seed isolation

Changing seed may change random-source output but does not change deterministic systems that do not consume RNG.

### Core has no UnityEngine dependency

Add an automated or build-level guard if practical.

## Unity adapter

Create one minimal Unity scene or bootstrap MonoBehaviour that:

- constructs SimulationWorld;
- advances it with fixed simulation ticks;
- displays current simulation tick/time in a simple debug UI or Console.

The MonoBehaviour must not own authoritative simulation state beyond the adapter reference.

## Validation

Before completion:

1. project compiles;
2. core/unit tests pass;
3. Unity Console has no compile errors;
4. Play Mode advances ticks;
5. two deterministic runs produce identical hashes.

## Deliverable report

Return:

- architecture summary;
- files created;
- test command(s);
- test results;
- Unity validation result;
- any deviation from this task and why.
