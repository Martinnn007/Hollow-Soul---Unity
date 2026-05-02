#!/usr/bin/env python3
from pathlib import Path
import sys

from pypdf import PdfReader


ROOT = Path(__file__).resolve().parents[1]
PDF_PATH = ROOT / "output" / "pdf" / "Hollow_M75_Enemy_Attacks_And_Senses.pdf"
REQUIRED = [
    "Enemy Attacks",
    "Senses",
    "sight",
    "hearing",
    "lunge",
    "Flying Chaser",
    "Bone Turret",
]


def main() -> int:
    if not PDF_PATH.exists():
        print(f"missing {PDF_PATH}", file=sys.stderr)
        return 1

    reader = PdfReader(str(PDF_PATH))
    text = "\n".join(page.extract_text() or "" for page in reader.pages)
    folded_text = text.lower()
    missing = [item for item in REQUIRED if item.lower() not in folded_text]
    if missing:
        print(f"missing required text: {', '.join(missing)}", file=sys.stderr)
        return 1

    print(f"ok pages={len(reader.pages)} chars={len(text)}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
