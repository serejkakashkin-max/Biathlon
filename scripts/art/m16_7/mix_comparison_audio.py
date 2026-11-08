"""Mix original project Foley under a commentary-friendly comparison master.

Run with numpy and imageio-ffmpeg. No external recordings are used.
"""

import argparse
import json
import subprocess
import wave
from pathlib import Path

import imageio_ffmpeg
import numpy as np


RATE = 48_000
AUDIO = Path(__file__).resolve().parents[3] / "Assets/Resources/Biathlon/Audio"
TRAVEL = {"skate", "skate_front", "acceleration", "acceleration_front", "uphill", "uphill_front", "entry"}


def load(cue):
    with wave.open(str(AUDIO / f"{cue}.wav"), "rb") as source:
        assert (source.getnchannels(), source.getsampwidth(), source.getframerate()) == (1, 2, RATE), cue
        return np.frombuffer(source.readframes(source.getnframes()), dtype="<i2").astype(np.float32) / 32768


def smooth_fade(clip, attack, release):
    clip = clip.copy()
    start = min(round(attack * RATE), len(clip) // 2)
    end = min(round(release * RATE), len(clip) // 2)
    if start:
        clip[:start] *= np.sin(np.linspace(0, np.pi / 2, start)) ** 2
    if end:
        clip[-end:] *= np.cos(np.linspace(0, np.pi / 2, end)) ** 2
    return clip


def loop(clip, count, offset=0):
    """Crossfade the ends so wind and glide have no audible wrap."""
    overlap = min(RATE // 4, len(clip) // 8)
    rise = np.sin(np.linspace(0, np.pi / 2, overlap)) ** 2
    period = clip[:-overlap].copy()
    period[:overlap] = clip[-overlap:] * (1 - rise) + clip[:overlap] * rise
    return period[(np.arange(count) + offset) % len(period)]


def compose(chapters):
    duration = chapters["duration_seconds"]
    count = round(duration * RATE)
    mix = np.zeros((count, 2), dtype=np.float32)
    cues = {name: load(name) for name in (
        "menu_air", "wind", "ski_packed", "ski_soft", "ski_ice", "crowd", "bolt", "pole_0", "pole_1", "pole_2", "rifle_0"
    )}

    def add(samples, when, level, pan=0):
        start = round(when * RATE)
        stop = min(start + len(samples), count)
        angle = (pan + 1) * np.pi / 4
        mix[start:stop, 0] += samples[:stop - start] * level * np.cos(angle)
        mix[start:stop, 1] += samples[:stop - start] * level * np.sin(angle)

    # The existing airy tonal menu bed fills the orbit/detail chapters. Retain
    # only a trace outdoors, where the Foley should carry the comparison.
    air = loop(cues["menu_air"], count)
    first_snow = next(chapter["start_seconds"] for chapter in chapters["chapters"] if chapter["id"] == "skate")
    air *= np.interp(np.arange(count) / RATE, [0, first_snow - .4, first_snow + .6, duration], [.52, .52, .13, .13])
    add(smooth_fade(air, .75, 1.1), 0, 1)

    for index, chapter in enumerate(chapters["chapters"]):
        scene = chapter["id"]
        start = chapter["start_seconds"]
        length = chapter["duration_seconds"]
        size = round(length * RATE)
        if start < first_snow:
            continue
        add(smooth_fade(loop(cues["wind"], size, index * 17777), .3, .4), start, .26, -.24)
        if scene in TRAVEL:
            glide = "ski_soft" if scene.startswith("uphill") else "ski_ice" if scene == "entry" else "ski_packed"
            snow = loop(cues[glide], size, index * 13111)
            t = np.linspace(0, 1, size)
            if scene.startswith("acceleration"):
                snow *= .40 + .60 * t
            elif scene == "entry":
                snow *= np.maximum(0, 1 - t / .73) ** .7
            add(smooth_fade(snow, .18, .35), start, .54, .12)
            for plant, at in enumerate(chapter.get("pole_plant_seconds", [])):
                add(cues[f"pole_{plant % 3}"], start + at, .35, .25)
                add(cues[f"pole_{(plant + 1) % 3}"], start + at + .022, .21, -.20)
            if scene == "entry":
                add(cues["pole_1"], start + length * .43, .23, .17)
        if scene in {"entry", "standing", "prone"}:
            add(smooth_fade(loop(cues["crowd"], size, index * 21111), .34, .48), start, .19, -.28)
        if scene in {"standing", "prone"}:
            add(cues["bolt"], start + length * .72, .25, .13)
            # A quiet shot from an adjacent lane, outside the comparison image.
            distant = cues["rifle_0"].copy()
            distant = np.convolve(distant, np.ones(23) / 23, mode="same")
            add(distant, start + .36, .10, -.54)

    for side in range(2):
        mix[:, side] = smooth_fade(mix[:, side], .14, .58)
    peak = np.max(np.abs(mix))
    if peak > .48:
        mix *= .48 / peak
    return mix


def mux(source, manifest, target):
    chapter_data = json.loads(manifest.read_text(encoding="utf-8"))
    samples = compose(chapter_data)
    ffmpeg = imageio_ffmpeg.get_ffmpeg_exe()
    subprocess.run([
        ffmpeg, "-hide_banner", "-loglevel", "error", "-y", "-i", str(source),
        "-f", "f32le", "-ar", str(RATE), "-ac", "2", "-i", "-",
        "-map", "0:v:0", "-map", "1:a:0", "-c:v", "copy", "-c:a", "aac", "-b:a", "192k",
        "-af", "loudnorm=I=-25:TP=-6:LRA=9", "-ar", str(RATE), "-movflags", "+faststart", str(target),
    ], input=samples.astype("<f4", copy=False).tobytes(), check=True)


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("video", type=Path)
    parser.add_argument("chapters", type=Path)
    parser.add_argument("output", type=Path)
    args = parser.parse_args()
    mux(args.video, args.chapters, args.output)
