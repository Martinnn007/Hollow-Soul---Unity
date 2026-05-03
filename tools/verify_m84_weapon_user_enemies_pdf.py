#!/usr/bin/env python3
from pathlib import Path

from pypdf import PdfReader


ROOT = Path(__file__).resolve().parents[1]
DOC_PATH = ROOT / "Docs/Hollow_M84_Weapon_User_Enemies.md"
PDF_PATH = ROOT / "output/pdf/Hollow_M84_Weapon_User_Enemies.pdf"
REQUIRED = [
    "Weapon-User Enemies",
    "Skeleton",
    "Knight",
    "Giant",
    "shield",
    "combo",
    "recovery",
    "rusty_slash",
    "spear_thrust",
    "club_sweep",
]


def main() -> None:
    if not DOC_PATH.exists():
        raise SystemExit(f"missing markdown: {DOC_PATH}")
    if not PDF_PATH.exists():
        raise SystemExit(f"missing pdf: {PDF_PATH}")

    markdown = DOC_PATH.read_text(encoding="utf-8")
    missing_markdown = [term for term in REQUIRED if term.lower() not in markdown.lower()]
    if missing_markdown:
        raise SystemExit(f"missing markdown text: {', '.join(missing_markdown)}")

    reader = PdfReader(str(PDF_PATH))
    text = "\n".join(page.extract_text() or "" for page in reader.pages)
    missing_pdf = [term for term in REQUIRED if term.lower() not in text.lower()]
    if missing_pdf:
        raise SystemExit(f"missing extracted pdf text: {', '.join(missing_pdf)}")

    print(f"ok pages={len(reader.pages)} chars={len(text)}")


if __name__ == "__main__":
    main()
