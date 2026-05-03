#!/usr/bin/env python3
from pathlib import Path
import sys

from pypdf import PdfReader


ROOT = Path(__file__).resolve().parents[1]
PDF_PATH = ROOT / "output/pdf/Hollow_M86_Ranged_Firearm_Enemies.pdf"
REQUIRED = [
    "Ranged + Firearm Enemies",
    "Hollow Archer",
    "Powder Gunner",
    "Knife Thrower",
    "Repeater Turret",
    "Clockwork Sentry",
    "Projectile",
    "FanProjectile",
    "RadialProjectile",
    "active-window",
]


def main():
    if not PDF_PATH.exists():
        print(f"missing pdf: {PDF_PATH}", file=sys.stderr)
        return 1

    reader = PdfReader(str(PDF_PATH))
    text = "\n".join(page.extract_text() or "" for page in reader.pages)
    missing = [token for token in REQUIRED if token.lower() not in text.lower()]
    if missing:
        print(f"missing required text: {missing}", file=sys.stderr)
        return 1

    print(f"ok pages={len(reader.pages)} chars={len(text)}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
