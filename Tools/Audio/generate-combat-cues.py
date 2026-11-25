"""Synthesize the short combat cues used by CombatSandbox."""

import math
import random
import struct
import wave
from pathlib import Path

RATE = 22050
OUTPUT = Path(__file__).resolve().parents[2] / "Assets/_Game/Audio"


def write(name, duration, sample):
    with wave.open(str(OUTPUT / f"{name}.wav"), "wb") as stream:
        stream.setparams((1, 2, RATE, 0, "NONE", "not compressed"))
        frames = (int(max(-1, min(1, sample(i / RATE))) * 32767) for i in range(round(RATE * duration)))
        stream.writeframes(b"".join(struct.pack("<h", frame) for frame in frames))


def main():
    OUTPUT.mkdir(parents=True, exist_ok=True)
    noise = random.Random(204)
    write("Swing", 0.20, lambda t: (0.55 * noise.uniform(-1, 1) +
          0.25 * math.sin(math.tau * (700 * t - 1500 * t * t)))
          * math.sin(math.pi * t / 0.20) ** 2)
    write("Hit", 0.18, lambda t: (0.62 * math.sin(math.tau * (125 * t - 170 * t * t))
          + 0.32 * noise.uniform(-1, 1)) * min(t * 450, 1) * math.exp(-29 * t))


if __name__ == "__main__":
    main()
