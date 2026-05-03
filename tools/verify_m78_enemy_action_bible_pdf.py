#!/usr/bin/env python3
from pathlib import Path
from pypdf import PdfReader


ROOT = Path(__file__).resolve().parents[1]
DOC_PATH = ROOT / "Docs/Hollow_M78_Enemy_Action_Bible.md"
PDF_PATH = ROOT / "output/pdf/Hollow_M78_Enemy_Action_Bible.pdf"
REQUIRED = [
    "Enemy Action Bible",
    "Bite",
    "Overhead Slash",
    "Arrow Volley",
    "Beam",
    "Teleport",
    "Soul Drain",
    "contact",
    "hazard",
    "behavior tree",
]
COVERAGE = [
    "body-only",
    "weapon-user",
    "ranged",
    "magic",
    "ghost/soul",
    "mechanical",
    "boss-scale",
]


def main():
    if not DOC_PATH.exists():
        raise SystemExit(f"missing markdown: {DOC_PATH}")
    if not PDF_PATH.exists():
        raise SystemExit(f"missing pdf: {PDF_PATH}")

    markdown = DOC_PATH.read_text(encoding="utf-8")
    card_count = sum(1 for line in markdown.splitlines() if line.startswith("### "))
    if not 120 <= card_count <= 180:
        raise SystemExit(f"expected 120-180 action cards, found {card_count}")

    missing_markdown = [term for term in REQUIRED + COVERAGE if term.lower() not in markdown.lower()]
    if missing_markdown:
        raise SystemExit(f"missing markdown text: {', '.join(missing_markdown)}")

    reader = PdfReader(str(PDF_PATH))
    text = "\n".join(page.extract_text() or "" for page in reader.pages)
    missing_pdf = [term for term in REQUIRED if term.lower() not in text.lower()]
    if missing_pdf:
        raise SystemExit(f"missing extracted pdf text: {', '.join(missing_pdf)}")

    print(f"ok pages={len(reader.pages)} cards={card_count} chars={len(text)}")


if __name__ == "__main__":
    main()
