# M16.6.1 — Stable rival appearance and no goggles

2026-11-01. Appearance implementation qualified; M16.6 remains OPEN.
Branch: `codex/m16-6-equipment-weather-training`.
Build: `Builds/M16_6_1_Appearance_Review/START_TEST.cmd` (fullscreen).

## Delivered

- Third goggles choice: absent. Actual lens, frame and strap are clipped in
  forward, depth and shadow passes in every LOD; raised morph is zero.
- Existing cap/headband/no-headwear and worn/raised goggles remain. Eyes/hair
  have their original separate tags; no extra renderer or per-frame mesh.
- New rivals receive full saved AppearanceProfile details, chosen from the
  existing creator/face data by stable identity and cosmetic-property name.
  New catalog cohorts, off-field peers and future prospects/replacements are
  covered. Player choice and saved explicit/legacy looks are preserved.
- Cosmetics do not receive the career RNG. Tests compare all abilities, money,
  nationality, player appearance and the exact RNG checkpoint; no simulation
  equation, scoring or difficulty multiplier changes.

![No goggles, with headband](creator-headband-absent.png)

![Raised goggles](creator-headband-raised.png)

![Rival](rival-0.png)

Six actual rival portraits plus all three goggles choices and no headwear are
native G2 studio captures using the production High URP renderer. Existing
creator UI tests also complete own/existing creation, save/load and actual race
binding. These captures qualify existing assets and supported choices; no new
haircut or face asset is advertised.

## Tests and evidence

| Receipt | Result |
| --- | --- |
| `appearance-core.trx` | 26/26 .NET appearance, catalog, legacy migration, world and generation cases |
| `appearance-replay.trx` | 3/3 complete Sprint frame/event/result, context and allocation regressions |
| `native-edit.json` | 12/12 native Unity EditMode; overlaps the pure cases above |
| `native-play.json` | 28/28 native PlayMode, including all nine headwear/goggles pairs, three LOD masks, independent materials, preview/race parity, dimensions, national kits and full creator flows |

The first absent-choice test is red against the original policy. The previous
catalog fixture incorrectly assumed roster position was the Sprint start slot;
an exact source export of baseline `1717c530` reproduces expected13/actual29
(`catalog-exact-baseline.trx`). The fixture now checks exact accepted seeded draw
slots and retains its count/identity assertions. No start-order code changes.

Initial native receipts are retained. The first generator call occurred before
the new Editor assembly had settled; rerunning after compilation changed the
actual LOD0 goggles mask from0 to3157 vertices, and all-LOD tests now pass.
New colour assertions use component float tolerance, matching the existing
material tests rather than requiring impossible exact native colour equality.

The old studio test ran without URP under the pre-existing Editor settings and
captured only pink equipment. Fixtures now acquire/release the production High
pipeline, preserving their original visibility thresholds and settings lease.
Invalid diagnostic captures remain under ignored artifacts, not this gallery.
Two edit-mode probe cleanup errors were archived separately; final PlayMode,
build and standalone qualification have no runtime errors. Final Editor Console
was inspected and cleared; its receipt is `console-final.json`.

Unity 6000.6.0f1 development build: Succeeded, **0 errors**, 520 retained warning
instances (24 existing API deprecations and496 inference shader variants),
430,864,852 bytes,29.010seconds. The common builder's historical receipt ID is
retained; `milestone.json` identifies M16.6.1. Standalone startup uses isolated
review save/settings/replay folders and checks owner-profile hashes.

Recheck the exact source/build, native receipts and append-only state prefix:

```text
python scripts/quality/report_m166_appearance.py
```

Next: M16.6.2 equipment, economy and shop; this milestone is not closed by
appearance delivery. Current equipment/stat/weather/training balance is unchanged
by this stage. See [ADR-0084](../../../adr/ADR-0084-m16-6-persistent-rival-appearance-and-no-goggles.md).
