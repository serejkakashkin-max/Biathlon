# TASK-148 — Ski and pole contact

PASS after WP147; full PlayMode42/42, Console0 errors. See TASK-148-contact-REPORT.md. Derive local presentation slope from the rendered course; align ski targets/normals and use bounded two-bone corrections. Place pole tips on a plausible local ground plane during a push and lift them during recovery, preserving shaft length. Staged range poles must also meet the surface. Reuse observations and prebound props; no authoritative physics/raycast dependency or per-tick gameplay changes.

Verify flat/up/down slope, ski clearance and orientation, pole length/contact/recovery, transition continuity and unchanged placement root. Fixed slope/state captures and real chase evidence required. Local-plane approximation and procedural contact limits must be explicit; performance qualification for30 athletes follows WP150.
