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
PDF_PATH = ROOT / "output" / "pdf" / "Hollow_M74_Movement_Intent_V2.pdf"
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
        title="Hollow M74 Movement Intent V2",
    )

    story = [
        Paragraph("Hollow M74: Movement Intent V2", styles["Title"]),
        Paragraph(
            "Design-contract catalogue for authored preferred range bands, soft separation, "
            "contact-buffer smoothing, and capped retreat bursts. Movement remains local: "
            "no pathfinding, no line of sight, no squad tactics, no home leash system, and no boss behavior changes.",
            styles["BodyText"],
        ),
        Spacer(1, 0.12 * inch),
        Paragraph("Runtime Contract", styles["Section"]),
    ]
    contract_rows = [
        ["Rule", "Contract"],
        ["preferred range", "Applied only during ordinary chase, wander, and hold movement."],
        ["separation", "Soft nudge away from nearby living non-boss enemies."],
        ["player buffer", "Stops constant shoving while still allowing hits and brief overlaps."],
        ["retreat", "Short 0.75s bursts, then reassess."],
        ["unchanged", "Windups, charges, stun, death, entry grace, attacks, contact damage, and bosses."],
    ]
    story.append(make_table(contract_rows, [1.45 * inch, 5.6 * inch]))
    story.extend([
        Spacer(1, 0.12 * inch),
        Paragraph("Current Roster Range Table", styles["Section"]),
    ])
    enemy_rows = [
        ["Enemy", "Preferred Min", "Preferred Max", "Notes"],
        ["Normal Chaser", "1.05m", "1.75m", "Loose direct pressure."],
        ["Flying Chaser", "2.75m", "4.25m", "Prey retreat and wander band."],
        ["Fast Chaser", "0.90m", "1.45m", "Close fast pressure."],
        ["Heavy Chaser", "1.35m", "2.15m", "Mindless pressure with more body room."],
        ["Ash Charger", "0.80m", "1.35m", "Instinctive predator; charge behavior unchanged."],
        ["Bone Turret", "5.25m", "7.50m", "Stationary data envelope only."],
        ["Husk Splitter", "1.25m", "2.00m", "Basic predator spacing."],
        ["Stone Warden Spawn", "4.50m", "6.50m", "Data completeness only; boss unchanged."],
    ]
    story.append(make_table(enemy_rows, [1.55 * inch, 1.0 * inch, 1.0 * inch, 3.5 * inch]))
    story.extend([
        Spacer(1, 0.12 * inch),
        Paragraph("Intelligence Precision", styles["Section"]),
        Paragraph(
            "Instinctive enemies use bands mainly for prey behavior and anti-shove smoothing. "
            "Simple enemies are loose. Basic, Trained, Tactical, and Cunning enemies respect the authored band more cleanly.",
            styles["BodyText"],
        ),
        Paragraph("Deferred Work", styles["Section"]),
        Paragraph(
            "Home leash behavior, obstacle steering, pathfinding, line of sight, and squad coordination are deferred to future milestones.",
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
    prefix = PREVIEW_DIR / "Hollow_M74_Movement_Intent_V2"
    subprocess.run([pdftoppm, "-png", str(PDF_PATH), str(prefix)], check=True)
    print(f"generated {PDF_PATH}; previews written to {PREVIEW_DIR}")


def main() -> None:
    build_pdf()
    render_previews_if_available()


if __name__ == "__main__":
    main()
