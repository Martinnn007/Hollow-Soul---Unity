#!/usr/bin/env python3
from pathlib import Path
from pypdf import PdfReader


ROOT = Path(__file__).resolve().parents[1]
PDF_PATH = ROOT / "output/pdf/Hollow_M77_Critter_Roster_And_Ballistic_Behaviors.pdf"
DOC_PATH = ROOT / "Docs/Hollow_M77_Critter_Roster_And_Ballistic_Behaviors.md"
REQUIRED = [
    "Spitting Pod",
    "Rat",
    "Spider",
    "ballistic",
    "territorial",
    "sight",
    "hearing",
    "spit_lob",
    "rat_bite",
    "startle_hop",
]


def main():
    if not DOC_PATH.exists():
        raise SystemExit(f"missing markdown: {DOC_PATH}")
    if not PDF_PATH.exists():
        raise SystemExit(f"missing pdf: {PDF_PATH}")

    reader = PdfReader(str(PDF_PATH))
    text = "\n".join(page.extract_text() or "" for page in reader.pages)
    missing = [term for term in REQUIRED if term.lower() not in text.lower()]
    if missing:
        raise SystemExit(f"missing extracted text: {', '.join(missing)}")
    print(f"ok pages={len(reader.pages)} chars={len(text)}")


if __name__ == "__main__":
    main()
