#!/usr/bin/env python3
from pathlib import Path
from pypdf import PdfReader


ROOT = Path(__file__).resolve().parents[1]
DOC_PATH = ROOT / "Docs/Hollow_M81_Enemy_Action_Profiles_V2.md"
PDF_PATH = ROOT / "output/pdf/Hollow_M81_Enemy_Action_Profiles_V2.pdf"
REQUIRED = [
    "Enemy Action Profiles V2",
    "Body",
    "Weapon",
    "Magic",
    "Defense",
    "Hazard",
    "poise",
    "counterplay",
    "Rat",
    "Spider",
    "Boss Action Profiles",
]
CATEGORIES = [
    "Body",
    "Weapon",
    "Ranged",
    "Projectile",
    "Magic",
    "Movement",
    "Defense",
    "Summon",
    "Hazard",
    "GhostSoul",
    "BossScale",
]


def main() -> None:
    if not DOC_PATH.exists():
        raise SystemExit(f"missing markdown: {DOC_PATH}")
    if not PDF_PATH.exists():
        raise SystemExit(f"missing pdf: {PDF_PATH}")

    markdown = DOC_PATH.read_text(encoding="utf-8")
    template_count = sum(1 for line in markdown.splitlines() if line.startswith("- **"))
    if template_count < 60:
        raise SystemExit(f"expected at least 60 template/action bullets, found {template_count}")

    missing_markdown = [term for term in REQUIRED + CATEGORIES if term.lower() not in markdown.lower()]
    if missing_markdown:
        raise SystemExit(f"missing markdown text: {', '.join(missing_markdown)}")

    reader = PdfReader(str(PDF_PATH))
    text = "\n".join(page.extract_text() or "" for page in reader.pages)
    missing_pdf = [term for term in REQUIRED if term.lower() not in text.lower()]
    if missing_pdf:
        raise SystemExit(f"missing extracted pdf text: {', '.join(missing_pdf)}")

    print(f"ok pages={len(reader.pages)} templates={template_count} chars={len(text)}")


if __name__ == "__main__":
    main()
