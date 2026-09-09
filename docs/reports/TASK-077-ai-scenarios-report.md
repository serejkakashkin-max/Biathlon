# TASK-077 — Integrated AI scenarios

AiRaceScenarioTests runs six complete, fixed-seed, identical-capability fixtures through the actual
PackAiRaceRunner and PackSprintFieldSession: flat duel2, rolling pack6, climb attack2, flat draft
train8, flat final sprint1 and range-approach recovery2. No scripted lane changes or synthetic hit
outcomes stand in for AI decisions. Climb acceptance counts Attack commands on positive grades;
draft acceptance requires at least two simultaneous physical wakes and chosen draft intent; final
sprint requires both intent and physical activation events; recovery requires observed requests
and chosen pre-range recovery intent.

Every run checks finish, two bouts, exact 3000m plus 150m per actual penalty, monotone travel,
independent endpoint body clearance (including loop wrap), no precision fallback, <=120s individual
skiing stall and no chain of eight lateral reversals separated by <=1s. Shooting/staging waits are
excluded from the skiing stall metric. Shared coordinator continuously validates swept trajectories;
the additional independent audit samples endpoints and does not claim a separate continuous proof.

Focused initial six cases passed. Stronger behavior checks also passed in the preflight regression
after FIFO admission; final .NET/Unity verification is recorded in docs/quality/wp078 and WP078 artifacts. These are
synthetic acceptance fixtures. Distributional realism and venue authoring remain WP079/later work.
Next WP078 repeats complete fields of30.

