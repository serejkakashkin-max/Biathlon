# M14 concept selection — FROSTLINE BROADCAST

2026-10-13. Three distinct options were implemented as native Unity UI Toolkit
design specimens, rendered at 1280×720 and inspected before production edits.
They are explicitly marked `DESIGN STUDY` and `SAMPLE DATA`, not presented as
finished production interfaces or an actual player's career.

| Direction | Composition | Strength | Main tradeoff |
| --- | --- | --- | --- |
| A · Frostline Broadcast | Editorial left home menu, biathlete hero, low race invitation; four compact HUD corners | Cold sport identity, clear numerals, existing hero remains visible | Dark instruments require explicit contrast on snow and target states |
| B · Alpine Studio | Light navigation sidebar, venue photograph and authored course dossier; white HUD plates | Clear preparation and equipment-room feeling | White plates compete with the snow; hero receives less emphasis |
| C · Night Pursuit | Full hero, framed invitation, centred bottom navigation; top/bottom instrument ribbons | Strong cinematic/controller silhouette | Bottom-centre telemetry can compete with the skier and crouch animation |

The selected direction is **A — Frostline Broadcast**. This is the implementation
team's choice within the owner's requested design work, not a claim of owner
approval. Its glacier timing edge, navy material and Oswald numerals tie the
menu and race together while keeping the gameplay centre open. It best preserves
the background the owner already liked and creates a recognisable sport identity
without making every scene into a dense dashboard.

Native specimens:

- [A menu](concepts/a-menu.png) · [A HUD](concepts/a-hud.png)
- [B menu](concepts/b-menu.png) · [B HUD](concepts/b-hud.png)
- [C menu](concepts/c-menu.png) · [C HUD](concepts/c-hud.png)

The original fixture is `Assets/Tests/PlayMode/FrostlineConceptTests.cs`.
The menu and venue images are existing project art. B's course silhouette reads
the actual authored `main` loop. The HUD specimens intentionally use illustrative
venue art beneath the UI; they are not a world-camera performance benchmark.

Production adapts A to real model bindings, existing keyboard/controller input,
localisation and the established HUD area limits. It is not a literal tracing of
sample values or a copy of another game's interface. Compare actual production
captures separately: decorative prototype data never enters saves or race state.

See [DESIGN_SYSTEM.md](DESIGN_SYSTEM.md) for implemented tokens, fonts, motion,
performance constraints and limitations. M14 remains open for owner acceptance.
