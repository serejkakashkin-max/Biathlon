# ADR-0001: Foundation simulation contract
Status: accepted for TASK 001.

## Context
The GDD requires pure C# boundaries, fixed ticks, reproducible inputs/events and no gameplay in MonoBehaviours. TASK 001 intentionally excludes skiing, physiology and shooting.

## Decision
- Core source lives under Assets/Scripts, shared by Unity asmdefs and standalone .NET projects. All five core asmdefs use noEngineReferences=true and overrideReferences=true to exclude auto-referenced plugin binaries.
- Domain contains immutable tick, ID, state, input and event contracts. Simulation depends on Domain. Replay depends on Domain and Simulation for verification. AI and Career are boundary markers, with no gameplay implementation.
- Time is exactly the rational pair (completed 64-bit tick, positive integer frequency). Floating seconds are derived for display. Overflow throws.
- Initial athlete order is preserved and IDs are ordinal, case-sensitive stable strings. World copies caller state and exposes value snapshots.
- Inputs target the next tick and apply in supplied order. Validate the entire batch before mutation. Multiple inputs to one athlete are legal. Only actual effort changes emit events, ordered exactly as the inputs, at the destination tick. Effort uses integer thousandths of requested control 0..1; this is an encoding, not relative physiological effort.
- The kernel does not fabricate race-start/finish events before race rules exist.
- SplitMix64 is the versioned integer random algorithm. Constants are algorithm parameters, not gameplay tuning. The empty kernel never consumes it. RNG state is exposed and can be restored by constructing a source from that state.
- SHA-256 verification uses explicit binary fields in little-endian order, UTF-8 length-prefixed strings, ordered athlete states and events, plus seed/current RNG state. No runtime GetHashCode is persisted.
- The bootstrap reads one versioned JSON definition. Update translates elapsed Unity scaled wall time into a target integer tick and only calls World.Step. Rendering reads state.
- Event sinks must not throw or re-enter World. Recording storage may grow; preallocate for bounded headless runs. No allocations in the kernel step itself.

## Alternatives
Accumulating float time, System.Random, Unity physics as authority, and Unity-dependent core were rejected because they weaken reproducibility or isolation.

## Consequences
Foundation determinism is integer-only and can be compared between Unity Mono and .NET. Future floating physics needs a separate determinism decision. Replay persistence/migration, race lifecycle and Balance Lab race commands remain future tasks. Fingerprinting allocates outside the tick loop. The adapter catches up all elapsed ticks; a future performance policy must preserve authoritative steps.
