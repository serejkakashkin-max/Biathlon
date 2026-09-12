# ADR-0018 — Provisional tuning lock and bounded proposals

Accepted in M8, 2026-09-11.

Balance experiments use the same deterministic authority as races. A calibration controller can receive an authored imperfect wind estimate; it changes its aim command, never the shot outcome. Existing compositions retain their defaults when this observation is absent. Cohort calibration combines physical/control stats and controller latency; stat sensitivity experiments vary one stat separately.

The first playable tuning version is identified by `biathlon_first_playable_tuning_v1` in `docs/balance/tuning-lock-v1.json`. It locks the listed resource definitions, track fixtures and balance experiments by SHA-256 of UTF-8 text after BOM removal and CRLF→LF normalization. It does not hash itself, metadata, machine timings or owner-local package settings. Verification is read-only and fails on changed/missing listed inputs.

This is a provisional version, not final approval of game feel. New career data can be introduced after M8; changing a locked coefficient requires an explicit new tuning revision, measured comparison and updated regression evidence. Do not bless a failing fingerprint automatically. Preserve the old manifest as the historical version when creating its successor.

The bounded tuner emits proposals only. It may evaluate only declared coefficients within explicit bounds toward explicit target windows, with a finite budget and a full evaluation log. The M8 implementation allowlists reaction time and tracking error. No filesystem mutation occurs in the core optimizer, and neither the optimizer nor its CLI changes production definitions. Owner review of broad game feel remains recommended before visual polish, without becoming a new mandatory blocker gate.
