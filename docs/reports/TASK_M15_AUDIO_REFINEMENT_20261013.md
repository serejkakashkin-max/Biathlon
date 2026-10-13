# M15 owner correction — music, pole Foley and visible Mass Start preparation

2026-10-13. All three requested corrections implemented. WP-199/204/205 follow-up
and the WP-182 personal-start regression are complete; listening/playtest follows.
Build: `Builds/M15_Audio_Refinement_Review/START_M15_TEST.cmd`.

## Diagnosis and changes

- Menu: the old 15.5-second sustained drone was not a musical arrangement. It is
  replaced with **Snowlight**, an original 32-second, eight-bar, 60 BPM instrumental
  composition: soft keys, melody, warm pad and bass, without percussion. Existing
  music preference is retained; the UI now calls it "Музыка меню / Menu music".
- Skiing: an accepted-state ski-only probe emits pole cues but zero rifle, bolt or
  target cues. The pole source itself had its strongest 12 ms block right at onset,
  resembling a shot crack. All three sources now have a broad snow-compression
  envelope and less treble; peak lowered from 0.60 to 0.32, cue gain 0.32 to 0.23.
  First-12-ms RMS is now only 2.8–3.3% of the strongest block, previously 100%.
  Rifle/target/other waveforms are byte-identical to the prior version.
- Mass Start: a reproducible eight-second elapsed loading frame consumed the
  entire local countdown (expected 3, actual 0). The host now discards oversized
  frames during the local preparation hold, using the existing authored 0.2-second
  frame backlog limit. Loading/rendering stalls cannot consume preparation time.
  With ordinary frames the sequence remains exactly three seconds. Authority and
  input clocks remain at zero; no catch-up tick burst occurs at the gun.

Files: soundbank/generator and waveform guard, FrontEndAudioSettings,
ProductionVenueHost, M15AudioReview, build script, native audio/start tests and
additive explanations in ADR-0065/0067. No balance or graphical-quality changes.

## Verification

30 distinct selected tests pass: M15AudioTests 10, PlayerStartWaitTests 4,
RaceStartPresentationTests 4, M15AudioImportTests 3, .NET preparation/countdown 9.
The initial RaceStartPresentation run passed three and failed its old UI fixture,
which still advanced two one-second frames. That fixture was changed to sixteen
ordinary 125 ms frames, preserving every visibility and first-tick assertion; its
focused rerun passes. Initial and final receipts are both retained. No test disabled.

The new Mass Start regression first failed before the fix (3 -> 0 after one 8s
frame), then passed the complete 3–2–1 sequence including additional render stalls.
Native pursuit ordinary/skip journals remain identical. Race equations, scheduled
starts and gaps, accepted command serialization, and saves are unchanged. No full
race balance rerun is claimed for these presentation/audio-only changes.

Waveform guard ran red on the old pole envelopes/menu duration and green on the
new sources. Bank remains 24 preloaded mono PCM clips and 22 sources; memory is
20,093,760 bytes (19.16 MiB), under the existing 20 MiB guard. No runtime synthesis.

Windows build succeeds with 0 errors and 0 warnings. Ordinary startup exits 0.
Native mixer audition now includes eight seconds of menu music before ski/body/
rifle/target/crowd cues: exit 0, 2,228,224 samples at 48 kHz stereo, peak 0.144392,
RMS 0.008996, zero collected errors, zero dropped events. This is a bounded audition,
not proof of every maximum-volume dense volley. Final Console check has no game
errors/exceptions. An unsupported combined test-class filter logged a tool error;
the classes were subsequently run separately.

RU/EN settings captures refreshed; 1024×768 at 130% text was visually inspected.
Evidence: `docs/quality/m15-refinement/`. Source/build identity is recorded there.

## Next check

Owner should listen to the menu and a skiing segment, then launch a Mass Start
and verify the visible preparation. Comfort and musical taste remain subjective;
the checks above do not claim human listening approval. Prior test build and
unrelated working-tree edits are preserved. State history is append-only.
