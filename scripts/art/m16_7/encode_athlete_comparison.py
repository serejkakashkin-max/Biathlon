"""Compose the two Unity camera feeds into one chaptered H.264 comparison.

Requires Pillow and imageio-ffmpeg in the Python environment.
"""
import argparse
import json
import subprocess
from pathlib import Path

import imageio_ffmpeg
from PIL import Image, ImageDraw, ImageFont, ImageStat


CHAPTERS = {
    "orbit": ("Круговой обзор", "Один масштаб. Поворот камеры на 360° вокруг обеих моделей."),
    "face": ("Лицо и очки", "Форма лица, кожа, шапочка и посадка поднятых очков."),
    "rifle": ("Винтовка и ремни", "Приклад, ствол, крепления и отдельный стартовый номер поверх формы."),
    "skis": ("Лыжи и ботинки", "Гоночный профиль лыжи, крепление носка, подошва и карбоновая манжета."),
    "skate": ("Коньковый ход V2", "Woodland Basin. Одна траектория, скорость 6 м/с, общий ракурс."),
    "acceleration": ("Разгон", "Сравните рост частоты шага, работу рук и перенос веса."),
    "uphill": ("Подъём V1", "Участок подъёма Woodland Basin. Сравните опору лыжи и работу палок."),
    "entry": ("Заход на огневой рубеж", "Один поворот на дорожку № 15. Торможение, укладка палок и подъём очков."),
    "standing": ("Изготовка стоя", "Снятие винтовки с плеч, хват и выход на линию прицеливания."),
    "prone": ("Изготовка лёжа", "Переход на коврик, опора рук, положение ног и лыж."),
}


def compose(source: Path, destination: Path, font_dir: Path) -> None:
    destination.mkdir(parents=True, exist_ok=True)
    studio = json.loads((source / "studio.json").read_text(encoding="utf-8-sig"))
    woodland = json.loads((source / "woodland.json").read_text(encoding="utf-8-sig"))
    shots = studio["shots"] + woodland["shots"]
    fps = studio["fps"]
    assert fps == woodland["fps"] and [shot["id"] for shot in shots] == list(CHAPTERS)
    regular = font_dir / "segoeui.ttf"
    bold = font_dir / "seguisb.ttf"
    brand = ImageFont.truetype(bold, 19)
    title_font = ImageFont.truetype(bold, 38)
    body = ImageFont.truetype(regular, 20)
    tag = ImageFont.truetype(bold, 20)
    small = ImageFont.truetype(regular, 16)
    width, height = 1512, 900
    padding, panel, image_y = 24, 720, 108
    left, right = padding, width - padding - panel
    paper = (9, 17, 26)
    main = (231, 242, 245)
    muted = (163, 181, 194)
    accent = (123, 222, 224)
    total = sum(shot["frames"] for shot in shots)
    summary = []
    thumbnails = {}
    ffmpeg = imageio_ffmpeg.get_ffmpeg_exe()
    output = destination / "comparison.mp4"
    command = [ffmpeg, "-hide_banner", "-loglevel", "error", "-y", "-f", "rawvideo", "-pix_fmt", "rgb24",
               "-video_size", f"{width}x{height}", "-framerate", str(fps), "-i", "-", "-an", "-c:v", "libx264",
               "-preset", "fast", "-crf", "17", "-pix_fmt", "yuv420p", "-movflags", "+faststart", str(output)]
    writer = subprocess.Popen(command, stdin=subprocess.PIPE)
    written = 0
    try:
        for index, shot in enumerate(shots):
            name = shot["id"]
            label, caption = CHAPTERS[name]
            template = Image.new("RGB", (width, height), paper)
            draw = ImageDraw.Draw(template)
            draw.text((left, 9), "BIATHLON: PULSE   /   М16.7", font=brand, fill=accent)
            draw.text((left, 35), label, font=title_font, fill=main)
            draw.text((width-padding, 31), f"{index+1:02d}  /  {len(shots):02d}", font=body, fill=muted, anchor="rt")
            for x, text, color in ((left, "В ИГРЕ  ·  G2", muted), (right, "НОВЫЙ СПОРТСМЕН  ·  М16.7", accent)):
                draw.rounded_rectangle((x, 85, x+panel, 107), radius=4, fill=(22, 36, 47))
                draw.text((x+9, 95), text, font=tag, fill=color, anchor="lm")
            draw.text((left, 847), caption, font=body, fill=main)
            draw.text((width-padding, 847), "UNITY  ·  ОДИНАКОВАЯ КАМЕРА", font=small, fill=muted, anchor="rt")
            summary.append({"id": name, "title_ru": label, "start_frame": written, "start_seconds": written/fps,
                            "frames": shot["frames"], "duration_seconds": shot["frames"]/fps})
            for frame in range(shot["frames"]):
                composed = template.copy()
                for x, side in ((left, "game"), (right, "hero")):
                    path = source / name / f"{side}-{frame:04d}.png"
                    with Image.open(path) as picture:
                        assert picture.size == (panel, panel), path
                        assert max(ImageStat.Stat(picture.resize((12, 12))).mean) > 8, f"Blank capture {path}"
                        composed.paste(picture, (x, image_y))
                progress = ImageDraw.Draw(composed)
                progress.rectangle((left, 883, width-padding, 886), fill=(36, 55, 67))
                progress.rectangle((left, 883, left+(width-padding-left)*(written+1)/total, 886), fill=accent)
                showcase = int(shot["frames"] * (.83 if name in ("standing", "prone") else .5))
                if frame == showcase:
                    thumbnails[name] = composed.copy()
                writer.stdin.write(composed.tobytes())
                written += 1
            print(f"{name}: {shot['frames']} matched pairs", flush=True)
    finally:
        writer.stdin.close()
        status = writer.wait()
        if status: raise RuntimeError(f"FFmpeg exit {status}")
    assert written == total
    thumbnails["skate"].save(destination / "preview.jpg", quality=94)
    board = Image.new("RGB", (1512, 940), paper)
    draw = ImageDraw.Draw(board)
    draw.text((24, 17), "М16.7  /  ДЕТАЛИ СПОРТСМЕНА", font=title_font, fill=main)
    for row, names in enumerate((("face", "rifle"), ("skis", "prone"))):
        for column, name in enumerate(names):
            preview = thumbnails[name].resize((720, 428), Image.Resampling.LANCZOS)
            board.paste(preview, (24 + column*744, 62 + row*436))
    board.save(destination / "details.jpg", quality=93)
    evidence = {"schema_version": 1, "id": "m167_shipping_vs_natural_comparison", "fps": fps, "resolution": [width,height],
                "frames": written, "duration_seconds": written/fps, "capture_method": "paired native Unity renders",
                "original_asset": studio["original_asset"], "candidate_asset": studio["candidate_asset"], "chapters": summary}
    (destination / "chapters.json").write_bytes((json.dumps(evidence, ensure_ascii=False, indent=2)+"\n").encode())


if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument("--capture", type=Path, required=True)
    parser.add_argument("--output", type=Path, required=True)
    parser.add_argument("--font-dir", type=Path, default=Path("C:/Windows/Fonts"))
    args = parser.parse_args()
    compose(args.capture, args.output, args.font_dir)
