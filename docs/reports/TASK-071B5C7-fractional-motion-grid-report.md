# TASK-071B5C7 — Fractional motion grid

Added FractionalMotionTimeline: finite duration/range validation, round-to-even billionth-tick
quantization, common roster boundary merging and quantized receipt sampling. Shared boundaries
are deduplicated in original time first. Distinct original times that map to one fraction fail
before output is written; movement, stationary gaps and suffixes must not disappear. Caller
double scratch may change on failure, while integer output remains untouched.

Sampling validates the full chain, then linearly interpolates nanometres between quantized
boundaries using decimal arithmetic. Original segment endpoints remain exact, including frozen
main progress on penalties. This is explicit pack retiming, not a change to old solo trajectories.
Interior timing shifts by boundary rounding; new pack context must identify this policy.

Three new tests cover a full real Sprint through penalties/stops, every endpoint and final
position, quantization/invalid output, zero warmed .NET allocation, and deliberate collisions
of movement/gap/suffix/roster boundaries. Evidence uses artifacts/physiology/wp071b5c7-*.
403/403 standalone and Unity MCP EditMode PASS; Console zero errors/four upstream warnings.
Prior PlayMode 6/6 at B5C4 retained; no new visual behavior. git diff --check passes with existing
CRLF warnings. .NET supplies allocation evidence; Unity's known counter limitation remains.

Limitations: rejected precision collisions need upstream replanning or a revised versioned
precision policy; this utility does not invent separation or skip events. Force resampling,
automatic follower replanning, physical trajectory projection and coordinated Sprint commit
remain pending. It must not be treated as a completed pack race. WP071 remains IN PROGRESS.
