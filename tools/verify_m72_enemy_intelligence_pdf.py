#!/usr/bin/env python3
from pathlib import Path
import sys

from pypdf import PdfReader


ROOT = Path(__file__).resolve().parents[1]
PDF_PATH = ROOT / "output/pdf/Hollow_M72_Enemy_Intelligence_Catalogue.pdf"
REQUIRED_TEXT = [
    "Instinctive",
    "Cunning",
    "prey",
    "predator",
    "Current Base Enemy Table",
    "Current Boss Metadata Table",
    "Normal Chaser",
    "Flying Chaser",
    "Stone Warden",
    "Hollow Star Larva",
]


def main() -> int:
    if not PDF_PATH.exists():
        print(f"missing PDF: {PDF_PATH}", file=sys.stderr)
        return 1

    reader = PdfReader(str(PDF_PATH))
    text = "\n".join(page.extract_text() or "" for page in reader.pages)
    missing = [token for token in REQUIRED_TEXT if token not in text]
    if missing:
        print(f"missing PDF text: {', '.join(missing)}", file=sys.stderr)
        return 1

    print(f"ok pages={len(reader.pages)} chars={len(text)}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
