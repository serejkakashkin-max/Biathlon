# ADR-0002 — Physiology numerical and data contracts

Status: accepted technical policy for WP-002; provisional tuning, not final game feel.

## Context
GDD §6 uses powers and exponential HR response. Foundation's integer guarantee remains intact.
System.Math transcendental results are not guaranteed bit-identical on every CPU/runtime.

## Decision
- Use IEEE binary64 double for physiological quantities. Time comes from integer tick/frequency,
  never accumulated render delta. Round persisted physiological state to nine decimal places,
  ties-to-even, after each tick. This resolution is encoding policy, not gameplay tuning.
- Reject NaN/infinity and out-of-contract external data; do not silently repair invalid definitions.
  Internal resource saturation is explicit: reserve/fatigue [0,1], HR [rest,safety ceiling],
  high-load time [0, configured drift saturation seconds]. Physical state uses suffixes for units.
- Exact replay guarantee: same simulation/tuning version, tick frequency, input/state and runtime/build.
  Cross-runtime qualification requires shared scripted checkpoints within 1e-6 absolute per quantity
  and identical discrete events. Quantization alone does NOT guarantee cross-runtime equality.
  Qualification applies to tested traces only; new branches/events must test threshold-near cases.
  Unsupported runtime qualification is not advertised as deterministic lockstep.
- Keep Foundation's canonical hash/version unchanged. Physiology gets its own snapshots and trace hashes.
- Immutable data contracts use required snake_case DataMember fields and schema_version/id.
  BCL DataContractJsonSerializer runs only at setup/export, never inside ticks. Unknown fields are ignored
  for additive compatibility; missing required fields and unsupported schema versions are errors.
- Finite scalar encoding domain is [-1e6,1e6]; positive rates/times/capacities must be >=1e-9.
  These are numerical validity bounds, not claimed athlete physiological limits. More restrictive
  relationships are checked per field. New systems must validate intermediate calculations.

## Alternatives
Fixed-point with custom transcendental functions would increase complexity before a demonstrated need.
Decimal has no built-in powers/exp. Float loses unnecessary precision. Unqualified double lockstep
would overpromise reproducibility. If checkpoint/event qualification fails, fix the numerical algorithm
or adopt deterministic math before extending production simulation; do not loosen tests silently.

## Consequences
No Unity dependency or hot-loop allocation is required. Runtime qualification evidence is collected
as systems are introduced. JSON is the sole provisional tuning authority; C# contains no fallback tuning.
AthleteState.RequestedEffort remains control thousandths, distinct from physiological relative effort.

## WP-008 composition
PhysiologySession wraps a private single-athlete Foundation world. Per-tick inputs include form and
terrain demand so replay does not lose external workload changes. Validate and calculate in locals
before kernel mutation. Tick order: power from start state → reserve using start fatigue → muscular
fatigue → bounded exposure/drift using new fatigue → HR target/response → end-tick state/telemetry.
The canonical kernel remains unchanged. Resource boundary event IDs append after EffortChanged=1;
effort events precede resource events. Callbacks retain the nonthrow/nonreentrant recording contract.
Telemetry power is interval input capability; physiological state is end-of-interval.
