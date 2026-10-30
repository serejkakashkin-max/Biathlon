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

## 2026-10-13 — owner listening refinement

The sustained menu bed was replaced with the original 32-second instrumental
composition Snowlight (soft keys, pad, bass, 60 BPM). The stable resource ID remains
menu_air for compatibility. Pole sources now use a broad snow-compression envelope
rather than an exponential crack, with lower source/mix gain. Other effect samples
are byte-identical. A ski-only accepted-state probe confirms that no rifle, bolt or
target cue is emitted without shooting. Subjective comfort still needs listening.
PCM usage is now 20,093,760 bytes, below the existing 20 MiB test budget; source count
remains 22. No runtime synthesis or additional per-athlete audio work was added.

## 2026-10-30 — M16.2 player shooting crowd reactions

Owner requests a short approving crowd reaction for each accepted player hit and
a disappointed reaction for each accepted miss. Use actual `Host.PlayerIndex`,
independent of the selected spectator/replay athlete. Existing nearby bot rifles
and mechanical target sounds remain; bot hits/misses never request these crowd
reactions. Replace the old fifth-clean-shot-only cheer with per-shot reactions,
so a clean fifth shot does not stack two cheering clips.

The observer admits one result only across adjacent accepted ticks with matching
track/profile/clock/roster. `AcceptedShotTransition` reads shot-count/closed-mask
deltas, including the last-completed mask when shot5 and mat release coincide.
No geometry, RNG, shot outcome, simulation event or authoritative state is changed.
Duplicates, rewind, skipped ticks, suspended playback and accelerated race paths
rebase without playing historical reactions. Existing replay suppression applies.

Add CrowdHit/CrowdMiss to the end of the cue enum; existing indices are stable.
Each uses two short human-recording excerpts, predecompressed with the existing
bank. Schedule after the accepted shot with a configurable0.24s DSP delay. These
transients have player priority within the existing16-transient/six-loop pool.
They obey Master/Ambience/quiet/unfocused settings. The background ambience still
ducks during shooting; these short feedback reactions are exempt from that duck
so they remain intelligible. No extra ordinary-game AudioSource or MonoBehaviour.

Four1.15s mono44.1kHz PCM clips add811440float-PCM bytes; total20905200bytes remains
below20MiB. Existing24original clips are byte-identical. New recordings are CC0
Freesound public high-quality previews by mglennsound (678546) and ShangusBurger
(764035), trimmed/equalized/faded offline. Source URLs, authors, licenses, original
and prepared hashes are retained in crowd_reactions_sources.json and project
credits. No copyrighted broadcast audio is extracted. The original24-clip
provenance above remains historical and correct for that bank.

Reference for the real spectator pattern:
[IBU fan atmosphere](https://www.biathlonworld.com/discover-biathlon/why-we-love-it/fan-tv-favourite).
Source/verification and native mixer audition: docs/quality/m16-2/audio/README.md.
