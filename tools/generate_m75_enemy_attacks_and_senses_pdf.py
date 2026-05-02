#!/usr/bin/env python3
from pathlib import Path
import shutil
import subprocess

from reportlab.lib import colors
from reportlab.lib.pagesizes import letter
from reportlab.lib.styles import ParagraphStyle, getSampleStyleSheet
from reportlab.lib.units import inch
from reportlab.platypus import Paragraph, SimpleDocTemplate, Spacer, Table, TableStyle


ROOT = Path(__file__).resolve().parents[1]
PDF_PATH = ROOT / "output" / "pdf" / "Hollow_M75_Enemy_Attacks_And_Senses.pdf"
PREVIEW_DIR = ROOT / "output" / "pdf" / "previews"


def build_pdf() -> None:
    PDF_PATH.parent.mkdir(parents=True, exist_ok=True)
    styles = getSampleStyleSheet()
    styles.add(ParagraphStyle(
        name="Small",
        parent=styles["BodyText"],
        fontSize=8,
        leading=10,
        textColor=colors.HexColor("#333333"),
    ))
    styles.add(ParagraphStyle(
        name="Section",
        parent=styles["Heading2"],
        fontSize=13,
        leading=16,
        spaceBefore=10,
        spaceAfter=6,
        textColor=colors.HexColor("#20242b"),
    ))

    doc = SimpleDocTemplate(
        str(PDF_PATH),
        pagesize=letter,
        rightMargin=0.55 * inch,
        leftMargin=0.55 * inch,
        topMargin=0.55 * inch,
        bottomMargin=0.55 * inch,
        title="Hollow M75 Enemy Attacks And Senses",
    )

    story = [
        Paragraph("Hollow M75: Enemy Attacks + Senses V1", styles["Title"]),
        Paragraph(
            "Design-contract catalogue for contact lunges, local Senses, awareness states, "
            "and separate melee attack pressure. The runtime remains local: no pathfinding, "
            "no obstacle line of sight, no alert sharing, no saved awareness state, and no boss behavior changes.",
            styles["BodyText"],
        ),
        Spacer(1, 0.12 * inch),
        Paragraph("Runtime Contract", styles["Section"]),
    ]
    contract_rows = [
        ["Rule", "Contract"],
        ["Enemy Attacks", "Contact-capable enemies can lunge from preferred-band edge."],
        ["Senses", "Sight is radius plus cone angle; hearing is radius stimulus response."],
        ["Awareness", "Unaware, Suspicious, Alerted, Engaged; Engaged persists until reset."],
        ["lunge", "0.22s windup, 0.18s active, 0.75m distance, 1.15s cooldown."],
        ["budget", "Melee lunges use a separate 0.30s room budget."],
        ["unchanged", "Ash Charger charge, Bone Turret ranged role, and bosses stay behavior-stable."],
    ]
    story.append(make_table(contract_rows, [1.45 * inch, 5.6 * inch]))
    story.extend([
        Spacer(1, 0.12 * inch),
        Paragraph("Current Roster Sense And Lunge Table", styles["Section"]),
    ])
    enemy_rows = [
        ["Enemy", "Sight", "Cone", "Hearing", "Lunge"],
        ["Normal Chaser", "6.5m", "150deg", "4.5m", "yes, 1.40m"],
        ["Flying Chaser", "7.5m", "240deg", "6.5m", "yes, 1.35m, endangered or Engaged"],
        ["Fast Chaser", "7.0m", "170deg", "5.0m", "yes, 1.25m"],
        ["Heavy Chaser", "5.0m", "110deg", "3.5m", "yes, 1.70m"],
        ["Ash Charger", "7.0m", "120deg", "5.0m", "charge attack only"],
        ["Bone Turret", "9.5m", "70deg", "2.5m", "ranged-only sentinel"],
        ["Husk Splitter", "6.5m", "160deg", "5.0m", "yes, 1.60m"],
        ["Stone Warden Spawn", "8.0m", "160deg", "4.5m", "data only"],
    ]
    story.append(make_table(enemy_rows, [1.45 * inch, 0.75 * inch, 0.75 * inch, 0.85 * inch, 3.25 * inch]))
    story.extend([
        Spacer(1, 0.12 * inch),
        Paragraph("Current Boss Sense Metadata", styles["Section"]),
    ])
    boss_rows = [
        ["Boss", "Sight", "Cone", "Hearing", "Policy"],
        ["Stone Warden", "8.0m", "140deg", "5.0m", "metadata only"],
        ["Splinter Saint", "8.0m", "180deg", "5.5m", "metadata only"],
        ["Gravel Maw", "6.5m", "110deg", "6.0m", "metadata only"],
        ["Cartouche Widow", "10.0m", "220deg", "6.5m", "metadata only"],
        ["Iron Reliquary", "8.5m", "120deg", "4.0m", "metadata only"],
        ["Mirror Husk", "9.0m", "220deg", "6.0m", "metadata only"],
        ["Ash Comet", "9.0m", "160deg", "7.0m", "metadata only"],
        ["Choir of Teeth", "10.0m", "300deg", "7.0m", "metadata only"],
        ["Rust Bishop", "9.5m", "180deg", "5.5m", "metadata only"],
        ["Hollow Star Larva", "0.0m", "0deg", "9.5m", "blind metadata profile"],
    ]
    story.append(make_table(boss_rows, [1.55 * inch, 0.75 * inch, 0.75 * inch, 0.85 * inch, 3.15 * inch]))
    story.extend([
        Spacer(1, 0.12 * inch),
        Paragraph("Deferred Work", styles["Section"]),
        Paragraph(
            "Obstacle line of sight, stealth UI, pathfinding, squad tactics, alert sharing, "
            "saved awareness state, and boss behavior changes are deliberately deferred.",
            styles["BodyText"],
        ),
    ])
    doc.build(story)


def make_table(rows, widths):
    table = Table(rows, colWidths=widths, repeatRows=1)
    table.setStyle(TableStyle([
        ("BACKGROUND", (0, 0), (-1, 0), colors.HexColor("#252a33")),
        ("TEXTCOLOR", (0, 0), (-1, 0), colors.white),
        ("FONTNAME", (0, 0), (-1, 0), "Helvetica-Bold"),
        ("FONTSIZE", (0, 0), (-1, -1), 8),
        ("LEADING", (0, 0), (-1, -1), 10),
        ("GRID", (0, 0), (-1, -1), 0.35, colors.HexColor("#c9ced8")),
        ("BACKGROUND", (0, 1), (-1, -1), colors.HexColor("#f7f8fa")),
        ("VALIGN", (0, 0), (-1, -1), "TOP"),
        ("LEFTPADDING", (0, 0), (-1, -1), 5),
        ("RIGHTPADDING", (0, 0), (-1, -1), 5),
        ("TOPPADDING", (0, 0), (-1, -1), 4),
        ("BOTTOMPADDING", (0, 0), (-1, -1), 4),
    ]))
    return table


def render_previews_if_available() -> None:
    pdftoppm = shutil.which("pdftoppm")
    if not pdftoppm:
        print(f"generated {PDF_PATH}; poppler preview skipped")
        return

    PREVIEW_DIR.mkdir(parents=True, exist_ok=True)
    prefix = PREVIEW_DIR / "Hollow_M75_Enemy_Attacks_And_Senses"
    subprocess.run([pdftoppm, "-png", str(PDF_PATH), str(prefix)], check=True)
    print(f"generated {PDF_PATH}; previews written to {PREVIEW_DIR}")


def main() -> None:
    build_pdf()
    render_previews_if_available()


if __name__ == "__main__":
    main()
