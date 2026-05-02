#!/usr/bin/env python3
from pathlib import Path
import sys

from pypdf import PdfReader


ROOT = Path(__file__).resolve().parents[1]
PDF_PATH = ROOT / "output" / "pdf" / "Hollow_Enemy_AI_Tooling_Recommendations.pdf"
PREVIEW_DIR = ROOT / "output" / "pdf" / "previews" / "enemy_ai_tooling"
REQUIRED = [
    "Hollow Enemy AI Tooling Recommendations",
    "A* Pathfinding Project Pro",
    "Unity AI Navigation",
    "Behavior Designer Pro",
    "NodeCanvas",
    "Unity Behavior",
    "GOAP v3",
    "Utility Intelligence GO v3",
    "Emerald AI 2025",
    "Manual Setup Answer",
    "Asset Store prices and versions are checkout snapshots",
]


def main() -> int:
    if not PDF_PATH.exists():
        print(f"missing {PDF_PATH}", file=sys.stderr)
        return 1

    reader = PdfReader(str(PDF_PATH))
    text = "\n".join(page.extract_text() or "" for page in reader.pages)
    missing = [item for item in REQUIRED if item not in text]
    if missing:
        print(f"missing required text: {', '.join(missing)}", file=sys.stderr)
        return 1

    previews = sorted(PREVIEW_DIR.glob("*.png"))
    if len(previews) != len(reader.pages):
        print(f"preview/page count mismatch: previews={len(previews)} pages={len(reader.pages)}", file=sys.stderr)
        return 1

    print(f"ok pages={len(reader.pages)} chars={len(text)} previews={len(previews)}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())

