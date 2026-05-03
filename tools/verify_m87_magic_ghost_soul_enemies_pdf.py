#!/usr/bin/env python3
from pathlib import Path
from pypdf import PdfReader


ROOT = Path(__file__).resolve().parents[1]
PDF = ROOT / "output/pdf/Hollow_M87_Magic_Ghost_Soul_Enemies.pdf"
REQUIRED = [
    "Magic/Ghost/Soul Enemies",
    "Hollow Acolyte",
    "Wraith",
    "Soul Eater",
    "Curse Binder",
    "Grave Lantern",
    "Beam",
    "PhaseMove",
    "Soul Drain",
    "curse",
    "M88",
]


def main():
    if not PDF.exists():
        raise SystemExit(f"missing PDF: {PDF}")
    reader = PdfReader(str(PDF))
    text = "\n".join(page.extract_text() or "" for page in reader.pages)
    missing = [item for item in REQUIRED if item.lower() not in text.lower()]
    if missing:
        raise SystemExit(f"missing extracted text: {missing}")
    print(f"ok pages={len(reader.pages)} chars={len(text)}")


if __name__ == "__main__":
    main()
