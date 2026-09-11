# M7 — Pursuit and Mass Start

PASS — 2026-09-11. WP080–090 complete. Unity MCP championship2/2 matches all three CLI result hashes;90 finishers and1500 accepted shots. Console0 errors,4 retained MCP warnings.

Gate: all three launch race formats complete headlessly with AI.

WP080–084 provide profile-bound Pursuit qualification, exact start gaps/global finish ranking,
shared pack race execution and regression. WP085–087 add a 30-athlete physical start grid,
continuous approach-to-route movement, PPSS/five-lap orchestration and committed range lane
assignments. WP088–089 verify repeated flat/rolling/narrow fields, collision clearance, bounded
stalls, exact result ranking and cross-runtime deterministic output. WP090 integrates Sprint →
Pursuit qualification with a separately constructed Mass Start through one portable factory.

Evidence and hashes: TASK-088-089-REPORT.md, TASK-090-REPORT.md and docs/quality/wp088-089,
docs/quality/wp090. Full .NET regression484/484 preceded the factory; its two new focused tests
bring discovery to486. This is not a claim that a single full486-test suite was rerun. Prior
PlayMode7/7 remains baseline; these workloads require headless EditMode validation, not new visuals.

M8 owns calibration. Synthetic tracks,30-athlete factory capacity and complete-finisher result
snapshots are explicit boundaries. No new hidden hit RNG, AI power multiplier, physics tuning
or UnityEngine dependency in the simulation core was introduced.
