# ADR-0067 — bounded presentation audio

2026-10-13. M15 WP-198–205, following ADR-0023 and the accepted M14 presentation.

## Decision

Audio consumes the existing accepted RacePresentationFeed. Shot-count, closed-mask
and bolt-phase transitions map to rifle, target and mechanism cues, respectively.
These facts already originate in the authoritative event/replay path. There is no
new gameplay event, speculative trigger audio, hit RNG, or write to race authority.
The feed publishes once per accepted observation, without an additional snapshot.

One prewarmed persistent mixer owns 16 transient and six reserved loop sources.
Selected-athlete events precede distant-athlete events. At most 12 transient event
requests are admitted per rendered frame; no backlog is retained. More important
reports can replace less important voices. Spatial voices retain fixed world
positions while the listener follows the current race camera. Distant reports are
culled; no raycast or per-athlete audio component is introduced.

Master and Effects/Body/Ambience/Interface/Music settings control source gains routed
to matching native AudioMixer groups. Predecompressed mono clips eliminate runtime
decode spikes; loops crossfade in level. Quiet mix reduces report/cheer peaks and
shooting focus ducks ambience. Settings are a separately versioned preference record;
invalid preferences revert to defaults. No career-save schema changes.

Pauses, accelerated completion, pursuit skip, replay seeking and accelerated replay
suppress race voices and rebase observation state. Ordinary 1x replay observes the
same facts and spatial camera context. No burst of stale events on resumption.

All 24 source clips are original offline sound design, reproducible through
`tools/audio/generate_m15_audio.py`. Seeded sound-generation noise is not gameplay
randomness. `sources.json` records provenance, hashes, peak/RMS and duration. No
runtime synthesis, third-party recordings, downloaded music or license dependency.

## Consequences and limits

The sound bank is stylized Foley, not a claim of field-recorded realism. Human
listening/playtesting remains necessary for the final aesthetic mix. Six loop voices
include reserved menu air and speed wind. Only the selected athlete gets close
breath/poles/heartbeat, reducing fatigue and voice cost. Nearby rifles remain spatial.
At discontinuous tick jumps historical audio is intentionally dropped. Sound never
affects authoritative result/hash, input mapping, or visual quality.

Unity references: [AudioSource](https://docs.unity3d.com/6000.6/Documentation/ScriptReference/AudioSource.html),
[preloading](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/AudioClip.LoadAudioData.html).
