# ADR-0087 — Training planner presentation

2026-11-01. M16.6.4 implemented and qualified in docs/quality/m16-6/training; training authority unchanged.

Keep the six existing CareerTrainingKind values, definition IDs, weekly slots,
adaptation, fatigue, form, health/risk, quality and taper calculations unchanged.
Render existing sport tasks with six original code-native illustrations:
endurance striding, interval sprint, corner technique, prone rifle, standing
rifle and recovery stretching. No emoji, fake category or fictitious benefit.

The week plan remains a detached list of existing sessions. A visible slot strip,
ordered plan with remove/clear controls, and actual TrainingPreview output show
what the player has selected. Show fatigue, form, health/risk and individual
ability deltas in consistent units; explain small/rounded/no guaranteed growth.
Each card uses authored slot/cost/benefit data and disables additions that exceed
the calendar limit. Never apply training on hover, page navigation or preview.

Reuse FROSTLINE fonts/colors, the existing localization/preference/accessibility
system and logical keyboard/gamepad controls. All six cards remain available in
the page scroll; test RU/EN, normal/large text,16:9/4:3, no overflow, real addition,
removal and preview/source equality. Native captures and a new standalone build
are required before claiming delivery.
