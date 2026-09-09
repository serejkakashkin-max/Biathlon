# TASK-067 — Pace intent controller

Map the eight intents to data-driven effort targets, with bounded response rate and seeded
control error held per accepted decision. Publish standard SimulationInput plus inspectable
nominal target/error/applied target. No state/physics multipliers. Validate tick/actor/expiry
before consuming random state. Decisions must be newly issued at the current control tick;
hold existing decisions to their exclusive deadline. Cross-system test feeds controller output
through complete shared Sprint and verifies identical scripted replay. Target tracking, terrain,
pack and range policy remain WP068–073.
