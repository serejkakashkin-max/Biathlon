# TASK-041 — Heartbeat impulse

Status: PASS, 2026-09-07. Added HeartbeatDefinition/JSON and HeartbeatSystem/Frame. Cadence
integrates integer nanobpm into a frequency-scaled beat period; no per-tick rounded phase
drift. HR changes preserve accumulated phase. Data controls rapid linear attack, smooth
cubic decay, quiet fraction and HR-dependent angular amplitude. Beat count/start, phase,
envelope and vertical angle are observable; no random hit outcome.

226/226 .NET and Unity EditMode pass; zero Console errors, four upstream warnings.
Evidence: artifacts/physiology/wp041-standalone.trx and wp041-{editmode,console}.json.
Exact minute counts at 20/30/144 Hz and 60/123/180 bpm, variable HR, peak/decay/quiet windows,
replay, invalid-input atomicity and active zero allocations pass. Earlier regressions pass.

Impulse shape is provisional game tuning; sampling constrains what peaks are observable.
Constructor rejects cadence above supported sampling frequency. Stance composition follows
WP043; no ski balance changes or commit. Next: WP042 muscular sway.
