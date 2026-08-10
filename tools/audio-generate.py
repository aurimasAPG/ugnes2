#!/usr/bin/env python3
"""
Reproducible audio generation for Hidden Valley (ElevenLabs).

    tools/audio-generate.py narration      # voice the third-person stage directions
    tools/audio-generate.py sfx            # regenerate the one-shot effects
    tools/audio-generate.py narration --force   # re-render even if the file exists

Everything is idempotent: a clip whose file already exists is skipped, so a re-run
after adding one narration line costs one request, not seventy-two. Jobs live in
tools/jobs/*.json so what was asked for is in the repo even though the audio is
regenerated, not hand-edited.

Quality: mp3_44100_192 is the highest format the Creator tier exposes (PCM needs
Pro). That is the "high resolution" ceiling for this account, and it is what both
endpoints are asked for.

The API key is read from ELEVENLABS_API_KEY, falling back to the export in
~/.zshrc. It is never printed.
"""

import argparse
import hashlib
import json
import os
import re
import sys
import urllib.error
import urllib.request
from pathlib import Path

REPO = Path(__file__).resolve().parent.parent
VOICE_DIR = REPO / "Assets/HiddenValley/Resources/Audio/Voice"
AUDIO_DIR = REPO / "Assets/HiddenValley/Resources/Audio"
CONTENT_DIR = REPO / "Assets/StreamingAssets/Content"
VOICE_MAP = REPO / "Assets/StreamingAssets/Audio/voice_map.json"
JOBS = REPO / "tools/jobs"

API = "https://api.elevenlabs.io/v1"
OUTPUT_FORMAT = "mp3_44100_192"


def api_key() -> str:
    key = os.environ.get("ELEVENLABS_API_KEY", "").strip()
    if key:
        return key
    zshrc = Path.home() / ".zshrc"
    if zshrc.exists():
        for line in zshrc.read_text(errors="ignore").splitlines():
            if "ELEVENLABS_API_KEY=" in line:
                key = line.split("ELEVENLABS_API_KEY=", 1)[1]
                return key.strip().strip('"').strip("'")
    sys.exit("No ELEVENLABS_API_KEY in the environment or ~/.zshrc.")


def post(path: str, payload: dict, key: str) -> bytes:
    request = urllib.request.Request(
        f"{API}/{path}?output_format={OUTPUT_FORMAT}",
        data=json.dumps(payload).encode(),
        headers={"xi-api-key": key, "Content-Type": "application/json"},
        method="POST",
    )
    try:
        with urllib.request.urlopen(request, timeout=180) as response:
            return response.read()
    except urllib.error.HTTPError as e:
        body = e.read().decode(errors="ignore")[:300]
        raise SystemExit(f"ElevenLabs {e.code} on {path}: {body}")


# ---- the shipped naming scheme ------------------------------------------------
# GameAudio looks a line up by "<speaker>|<text>", and also by "<speaker>|<quoted
# speech only>". Clip ids are sha1 of the cleaned key, truncated to 12 — matched
# against the existing 72 clips so new files sit in the same convention.


def clean_line(text: str) -> str:
    quoted = re.findall(r'"([^"]+)"', text)
    return " ".join(quoted) if quoted else text.strip()


def clip_id(prefix: str, speaker: str, text: str) -> str:
    digest = hashlib.sha1(f"{speaker}|{clean_line(text)}".encode()).hexdigest()[:12]
    return f"vo_{prefix}_{digest}"


def narration_lines():
    """Every dialogue line with no quoted speech — the stage directions. Derived from
    content rather than listed in the job file, so a new narration line is picked up
    by re-running this tool instead of by remembering to edit a list."""
    found = []
    for path in sorted(CONTENT_DIR.glob("*.json")):
        data = json.loads(path.read_text())
        for dialogue in data.get("dialogue", []):
            for node in dialogue.get("nodes", []):
                speaker = node.get("speaker") or dialogue.get("speaker") or dialogue.get("npc")
                for line in node.get("lines", []):
                    text = line if isinstance(line, str) else line.get("text", "")
                    who = speaker if isinstance(line, str) else (line.get("speaker") or speaker)
                    if text and not re.search(r'"[^"]+"', text):
                        found.append((who, text.strip()))
    # Dedupe, keeping order: the same direction can appear in two nodes.
    seen, unique = set(), []
    for who, text in found:
        if (who, text) not in seen:
            seen.add((who, text))
            unique.append((who, text))
    return unique


def run_narration(job: dict, key: str, force: bool) -> None:
    voice = job["voice_id"]
    model = job.get("model_id", "eleven_multilingual_v2")
    settings = job.get("voice_settings", {})

    lines = narration_lines()
    if not lines:
        sys.exit("No narration lines found in content — nothing to voice.")

    mapping = json.loads(VOICE_MAP.read_text()) if VOICE_MAP.exists() else {}
    VOICE_DIR.mkdir(parents=True, exist_ok=True)

    made = skipped = 0
    for speaker, text in lines:
        if not speaker:
            print(f"  ! no speaker for: {text[:60]!r} — skipped")
            continue
        name = clip_id("narrator", speaker, text)
        target = VOICE_DIR / f"{name}.mp3"

        if target.exists() and not force:
            skipped += 1
        else:
            audio = post(
                f"text-to-speech/{voice}",
                {"text": text, "model_id": model, "voice_settings": settings},
                key,
            )
            target.write_bytes(audio)
            made += 1
            print(f"  + {name}.mp3  {len(audio)/1024:6.1f} KB  {text[:52]}")

        # Both lookup keys, matching how GameAudio resolves. For narration the raw
        # and cleaned forms are identical, so this collapses to one entry.
        mapping[f"{speaker}|{text}"] = name
        mapping[f"{speaker}|{clean_line(text)}"] = name

    VOICE_MAP.write_text(json.dumps(mapping, indent=1, ensure_ascii=False) + "\n")
    print(f"\nnarration: {made} rendered, {skipped} already present, map now {len(mapping)} keys")


def conform_one_shot(mp3: Path, headroom: float = 0.89, floor: float = 0.06) -> tuple:
    """Trim the lead-in, peak-normalise, and write 44.1 kHz WAV beside the source.

    Measured on the first batch: onsets up to 210 ms and peaks from 0.00 (silent)
    to 0.97. A footstep that fires every 0.38 s cannot carry a fifth of a second of
    leading silence, and one-shots at wildly different levels make the mix sound
    broken rather than varied. WAV, not re-encoded MP3, because macOS has no MP3
    encoder and a second lossy pass is the wrong direction for "high resolution".

    Returns (seconds, onset_seconds, peak) of the source, so the caller can report
    a generation that came back empty instead of shipping silence.
    """
    import struct
    import subprocess
    import tempfile
    import wave

    scratch = Path(tempfile.mkdtemp()) / "decoded.wav"
    subprocess.run(
        ["afconvert", "-f", "WAVE", "-d", "LEI16@44100", "-c", "1", str(mp3), str(scratch)],
        capture_output=True, check=True,
    )
    with wave.open(str(scratch)) as w:
        rate, frames = w.getframerate(), w.getnframes()
        samples = list(struct.unpack(f"<{frames}h", w.readframes(frames)))
    scratch.unlink()

    peak = max((abs(s) for s in samples), default=0)
    if peak == 0:
        return frames / rate, 0.0, 0.0

    # Attack start is found by walking *back* from the loudest sample, not forward
    # from zero: a soft-attacked one-shot (a fingertip on paper) creeps over any
    # forward threshold slowly, which left 73 ms of dead air in front of the UI
    # click — audible as lag on every tap.
    loudest = max(range(len(samples)), key=lambda i: abs(samples[i]))
    attack = peak * 0.15
    start = loudest
    while start > 0 and abs(samples[start]) > attack:
        start -= 1

    threshold = peak * floor
    end = next((i for i in range(len(samples) - 1, -1, -1) if abs(samples[i]) > threshold), len(samples) - 1)

    # Keep a hair before the transient so the attack is not clipped, and a short
    # decay after the last audible sample so the tail is not chopped square.
    start = max(0, start - int(0.004 * rate))
    end = min(len(samples) - 1, end + int(0.030 * rate))
    trimmed = samples[start:end + 1]

    gain = (headroom * 32767.0) / peak
    conformed = [max(-32768, min(32767, int(s * gain))) for s in trimmed]

    out = mp3.with_suffix(".wav")
    with wave.open(str(out), "wb") as w:
        w.setnchannels(1)
        w.setsampwidth(2)
        w.setframerate(rate)
        w.writeframes(struct.pack(f"<{len(conformed)}h", *conformed))

    mp3.unlink()  # one asset per Resources path, or Unity picks arbitrarily
    return frames / rate, start / rate, peak / 32768.0


def run_sfx(job: dict, key: str, force: bool) -> None:
    AUDIO_DIR.mkdir(parents=True, exist_ok=True)
    made = skipped = 0
    silent = []
    for effect in job["effects"]:
        final = AUDIO_DIR / f"{effect['file']}.wav"
        if final.exists() and not force:
            skipped += 1
            continue

        raw = AUDIO_DIR / f"{effect['file']}.mp3"
        audio = post(
            "sound-generation",
            {
                "text": effect["prompt"],
                "duration_seconds": effect["seconds"],
                "prompt_influence": job.get("prompt_influence", 0.6),
            },
            key,
        )
        raw.write_bytes(audio)
        seconds, onset, peak = conform_one_shot(raw)
        made += 1

        if peak == 0.0:
            silent.append(effect["file"])
        print(f"  + {effect['file']}.wav  was {seconds:.2f}s onset {onset*1000:4.0f}ms peak {peak:.2f}"
              f"  {effect['prompt'][:34]}")

    print(f"\nsfx: {made} rendered, {skipped} already present")
    if silent:
        print("SILENT — regenerate with a more concrete prompt: " + ", ".join(silent))


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("job", choices=["narration", "sfx"])
    parser.add_argument("--force", action="store_true", help="re-render existing files")
    args = parser.parse_args()

    job = json.loads((JOBS / f"{args.job}.json").read_text())
    key = api_key()
    print(f"{args.job}: {OUTPUT_FORMAT}\n")

    (run_narration if args.job == "narration" else run_sfx)(job, key, args.force)


if __name__ == "__main__":
    main()
