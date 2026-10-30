# M16.2 — player shooting crowd reactions

2026-10-30. Owner audio follow-up implemented; M16.2 still awaits owner acceptance.
Run `Builds/M16_2_Live_Shooting_Review/START_TEST.cmd` for the updated fullscreen
candidate. [Build receipt](build-summary.json), [final binary hashes](build-fingerprint.json).

## Behavior

Every freshly accepted player shot receives one short human crowd reaction:
approving cheering on a hit, disappointed groaning on a miss. Two variants per
result alternate without runtime RNG, pitch manipulation or synthesis. A0.24s
configured delay lets the rifle/target transient read first. These reactions use
the actual `Host.PlayerIndex`, even if a replay/spectator camera follows a bot.
Bot hits/misses never request them. The former clean-fifth-shot extra cheer is
removed to avoid a doubled fifth reaction. Ordinary nearby rifle/target sounds
and the player's finish cheer remain.

The existing accepted snapshot feed is the only source: one shot-count delta and
an accepted closed-target delta, including final-shot/mat release in one state.
No hit probability, geometry, shot result, control, AI, race timer, save or graphics
quality changed. Repeated snapshots, nonadjacent ticks, rewind, suspended/fast-forward
paths do not replay historical reactions. Existing pool priorities protect player
feedback from the dense field without extra sources or a backlog.

Master and Ambience sliders control these cues. Quiet mode softens them and
unfocused mute still applies. The background ambience ducks during shooting;
these short feedback cues remain intelligible rather than receiving that duck.

## Real-world reference and licensed recordings

IBU describes the shooting-range tension and release in its
[fan atmosphere overview](https://www.biathlonworld.com/discover-biathlon/why-we-love-it/fan-tv-favourite).
This is a reference for the reaction pattern, not a source of extracted broadcast
audio. The bank uses short edited excerpts from public high-quality previews of:

* [AF Crowd Cheer Nat End .wav — mglennsound](https://freesound.org/people/mglennsound/sounds/678546/),
  a recorded cheering crowd.
* [Crowd Groans At Bad Joke01 — ShangusBurger](https://freesound.org/people/ShangusBurger/sounds/764035/),
  a recorded disappointed group reaction.

Both primary pages declare CC0. Authors, source URLs, source hashes, edits and
prepared WAV hashes are retained in
`Assets/Resources/Biathlon/Audio/crowd_reactions_sources.json`, original previews
in `tools/audio/sources/m16_2`, and build `THIRD_PARTY_CREDITS.txt`. Preparation:
`tools/audio/prepare_m162_crowd.py`. The existing24original clips are unchanged.

Four1.15s mono44.1kHz PCM clips add811440float-PCM bytes. Total20905200bytes remains
below the unchanged20MiB test ceiling. Source budget is still16transient+6loop=22.
Samples preload/decompress before play; no download or decoding during a shot.

## Native qualification

* [15EditMode single-shot transition cases](shot-transitions-native.json): all five
  hit/miss positions, reverse physical targets, final-shot release hit/miss,
  invalid/duplicate/bout/identity changes and zero warmed managed allocations.
* [12PlayMode audio cases](audio-playmode-native.json), including the existing
  audio suite updated in place: original rifle/bolt/target/heart/mute/spatial/UI
  behavior; own hits/misses; a spectated bot shooting while player never fires;
  actual production career MassStart with30athletes,145bot shots but only the
  player's five reactions; duplicate/rewind/gap and pause suppression.
* [4Editor import/provenance cases](audio-import-native.json): native mixer buses,
  PCM/preload importer settings, unchanged original24hashes and four CC0 excerpt
  hashes. All31unique native cases PASS; no failing assertion disabled.
* Separate [15/15CoreCLR cases](crowd-transitions-coreclr.trx) PASS, not15extra
  unique native cases. The initial inline allocation fixture measured24bytes on
  CoreCLR and zero on Mono. Isolating the measured loop from NUnit constraints in
  a non-inlined method gives zero on both; the exact zero-byte assertion remains.
  Initial red receipt remains alongside final green. An earlier small synthetic
  fixture did not cover30entrants; it now uses the real career pipeline. Its red
  receipt is retained.

## Compiled-player audition

Both clips below are native AudioListener mixer captures from the actual
30-athlete career pipeline. Simulation uses20Hz accepted ticks; each rendered
recording step waits at least50ms, with rendering overhead, so this is not an FPS
or exact realtime benchmark. Audio selection deliberately follows bot1 while
crowd reactions remain tied to the player. No edited outcomes or postmixed voices.

* [Five accepted hits — native WAV](player-hit/native-player-reactions.wav): five
  hit reactions, zero miss reactions,118other accepted shots without extra crowd
  cues. [Protocol](player-hit/review.json). Peak0.200/RMS0.0144, peak20voices.
* [Five accepted misses — native WAV](player-miss/native-player-reactions.wav): five
  miss reactions, zero hit reactions,145other accepted shots without extra crowd
  cues. Ordinary off-target player aim/trigger/bolt input, same geometric solver.
  [Protocol](player-miss/review.json). Peak0.177/RMS0.0117, peak19voices.

Both probes exited0with no runtime Error/Exception/Assert, exactly five crowd
playbacks and22allocated sources. The WAVs include ordinary rifles, mechanisms,
body and ambience so the mix can be judged in context. Subjective comfort remains
part of the owner's gameplay test; numeric checks do not establish aesthetics.

[Protected source/history check](authority-source-guard.json): among the655prior
protected files only the intended presentation audio cue JSON changed. All core
authority and other protected JSON remain exact. Original approved board videos
remain valid silent UI evidence from the earlier candidate; they are not presented
as demonstrations of this new audio. Updated ordinary fullscreen/startup acceptance:
[receipt](fullscreen/review.json). [Native Console check](console-final.json).

Build succeeded with0errors. Its520warnings comprise24existing API deprecations
and496existing inference-package shader variant warnings on recompilation:
[classification](build-warning-summary.json), [messages](build-messages.txt).
No package, shader variant, graphics-quality or dependency workaround was added.
Existing URP offscreen-target shutdown warning remains in the explicit QA logs.

Next: owner listens in a race and decides whether to accept M16.2. M17 not started.
