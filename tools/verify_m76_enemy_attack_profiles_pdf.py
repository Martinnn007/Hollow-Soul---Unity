#!/usr/bin/env python3
from pathlib import Path
import sys

from pypdf import PdfReader


ROOT = Path(__file__).resolve().parents[1]
PDF_PATH = ROOT / "output" / "pdf" / "Hollow_M76_Enemy_Attack_Profiles.pdf"
REQUIRED = [
    "Attack Profiles",
    "Physical",
    "Projectile",
    "knockback",
    "stability",
    "Normal Chaser",
    "Bone Turret",
    "Stone Warden",
    "Hollow Star Larva",
]


def main() -> int:
    if not PDF_PATH.exists():
        print(f"missing {PDF_PATH}", file=sys.stderr)
        return 1

    reader = PdfReader(str(PDF_PATH))
    text = "\n".join(page.extract_text() or "" for page in reader.pages)
    folded = text.lower()
    missing = [item for item in REQUIRED if item.lower() not in folded]
    if missing:
        print(f"missing required text: {', '.join(missing)}", file=sys.stderr)
        return 1

    print(f"ok pages={len(reader.pages)} chars={len(text)}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
