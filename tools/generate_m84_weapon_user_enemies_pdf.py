#!/usr/bin/env python3
from __future__ import annotations

import shutil
import subprocess
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
DOCS_PATH = ROOT / "Docs/Hollow_M84_Weapon_User_Enemies.md"
PDF_PATH = ROOT / "output/pdf/Hollow_M84_Weapon_User_Enemies.pdf"
PREVIEW_DIR = ROOT / "output/pdf/previews/m84"

ROSTER = [
    ("Skeleton Sword", "4 HP", "1.55m/s", "Basic predator", "1.15-1.85m", "rusty_slash -> backhand_slash"),
    ("Skeleton Spear", "4 HP", "1.45m/s", "Basic sentinel", "1.75-2.75m", "spear_thrust, spear_sweep"),
    ("Knight", "8 HP", "1.15m/s", "Trained sentinel", "1.35-2.35m", "medium shield, slash/thrust/bash"),
    ("Giant", "14 HP", "0.75m/s", "Basic mindless", "1.85-3.10m", "club_sweep, overhead_slam, stomp"),
]

MOVES = [
    ("Skeleton Sword", "rusty_slash", "WeaponMelee", "1", "Light", "120deg", "0.28/0.14/0.24s", "0.35m", "backhand_slash"),
    ("Skeleton Sword", "backhand_slash", "WeaponMelee", "1", "Light", "140deg", "0.18/0.14/0.34s", "0.30m", "-"),
    ("Skeleton Spear", "spear_thrust", "WeaponMelee", "1", "Medium", "55deg", "0.34/0.12/0.34s", "0.45m", "-"),
    ("Skeleton Spear", "spear_sweep", "WeaponMelee", "1", "Light", "160deg", "0.30/0.16/0.38s", "0.35m", "-"),
    ("Knight", "shield_guard", "Defense", "0", "Medium", "150deg", "0.12/0.65/0.28s", "0.00m", "-"),
    ("Knight", "knight_slash", "WeaponMelee", "1", "Medium", "120deg", "0.36/0.16/0.36s", "0.50m", "shield_bash"),
    ("Knight", "knight_thrust", "WeaponMelee", "1", "Medium", "65deg", "0.34/0.13/0.38s", "0.45m", "-"),
    ("Knight", "shield_bash", "WeaponMelee", "1", "Medium", "90deg", "0.28/0.14/0.50s", "0.65m", "-"),
    ("Giant", "club_sweep", "WeaponMelee", "2", "Heavy", "190deg", "0.65/0.22/0.75s", "0.90m", "-"),
    ("Giant", "overhead_slam", "Area", "2", "Heavy", "360deg", "0.78/0.20/0.90s", "1.10m", "-"),
    ("Giant", "stomp", "Area", "1", "Heavy", "360deg", "0.50/0.18/0.60s", "0.80m", "-"),
]


def require_reportlab():
    try:
        from reportlab.lib import colors  # noqa: F401
        from reportlab.lib.pagesizes import letter  # noqa: F401
        from reportlab.lib.styles import getSampleStyleSheet  # noqa: F401
        from reportlab.platypus import SimpleDocTemplate  # noqa: F401
    except Exception as exc:  # pragma: no cover
        raise SystemExit(f"reportlab unavailable: {exc}")


def build_pdf() -> None:
    require_reportlab()
    from reportlab.lib import colors
    from reportlab.lib.enums import TA_CENTER
    from reportlab.lib.pagesizes import letter
    from reportlab.lib.styles import ParagraphStyle, getSampleStyleSheet
    from reportlab.lib.units import inch
    from reportlab.platypus import PageBreak, Paragraph, SimpleDocTemplate, Spacer, Table, TableStyle

    PDF_PATH.parent.mkdir(parents=True, exist_ok=True)
    styles = getSampleStyleSheet()
    title = ParagraphStyle("M84Title", parent=styles["Title"], alignment=TA_CENTER, fontSize=22, leading=26, textColor=colors.HexColor("#2a3038"))
    h2 = ParagraphStyle("M84H2", parent=styles["Heading2"], fontSize=13, leading=16, textColor=colors.HexColor("#5a3028"))
    body = ParagraphStyle("M84Body", parent=styles["BodyText"], fontSize=8.4, leading=11)
    small = ParagraphStyle("M84Small", parent=styles["BodyText"], fontSize=7.1, leading=8.5)

    doc = SimpleDocTemplate(str(PDF_PATH), pagesize=letter, rightMargin=0.45 * inch, leftMargin=0.45 * inch, topMargin=0.42 * inch, bottomMargin=0.42 * inch)
    story = [
        Paragraph("M84: Weapon-User Enemies", title),
        Paragraph("Souls-lite commitment for Skeleton Sword, Skeleton Spear, Knight, and Giant. Ordinary contact remains harmless; damage comes from explicit weapon arcs, area impacts, projectiles, hazards, or active boss windows.", body),
        Spacer(1, 0.14 * inch),
        Paragraph("Roster Cards", h2),
    ]

    roster_data = [["Enemy", "Stats", "Identity", "Range", "Primary Actions"]]
    for row in ROSTER:
        roster_data.append([Paragraph(row[0], small), row[1] + "<br/>" + row[2], row[3], row[4], Paragraph(row[5], small)])
    roster_table = Table(roster_data, colWidths=[1.2 * inch, 1.0 * inch, 1.2 * inch, 1.05 * inch, 2.5 * inch])
    roster_table.setStyle(TableStyle([
        ("BACKGROUND", (0, 0), (-1, 0), colors.HexColor("#303844")),
        ("TEXTCOLOR", (0, 0), (-1, 0), colors.white),
        ("BACKGROUND", (0, 1), (-1, -1), colors.HexColor("#f2eee8")),
        ("GRID", (0, 0), (-1, -1), 0.35, colors.HexColor("#b6aaa1")),
        ("VALIGN", (0, 0), (-1, -1), "TOP"),
        ("FONTNAME", (0, 0), (-1, 0), "Helvetica-Bold"),
        ("FONTSIZE", (0, 0), (-1, -1), 7.2),
    ]))
    story += [roster_table, Spacer(1, 0.18 * inch), Paragraph("Shield Tier Table", h2)]

    shield_data = [
        ["Tier", "Arc", "Light/Medium", "Heavy", "Massive", "Break"],
        ["Small shield", "135deg", "50%", "25%", "0%", "Heavy+"],
        ["Medium shield", "150deg", "75%", "50%", "25%", "Heavy+"],
        ["Heavy shield", "170deg", "100%", "80%", "55%", "Massive+"],
    ]
    shield_table = Table(shield_data, colWidths=[1.25 * inch, 0.75 * inch, 1.2 * inch, 0.85 * inch, 0.85 * inch, 0.9 * inch])
    shield_table.setStyle(TableStyle([
        ("BACKGROUND", (0, 0), (-1, 0), colors.HexColor("#472f2a")),
        ("TEXTCOLOR", (0, 0), (-1, 0), colors.white),
        ("GRID", (0, 0), (-1, -1), 0.35, colors.HexColor("#b6aaa1")),
        ("FONTSIZE", (0, 0), (-1, -1), 7.4),
    ]))
    story += [shield_table, PageBreak(), Paragraph("Movesets And Impact", h2)]

    move_data = [["Enemy", "Attack", "Runtime", "Dmg", "Force", "Arc", "Timing", "KB", "Combo"]]
    for row in MOVES:
        move_data.append(list(row))
    move_table = Table(move_data, repeatRows=1, colWidths=[1.0 * inch, 1.05 * inch, 0.8 * inch, 0.35 * inch, 0.55 * inch, 0.55 * inch, 1.0 * inch, 0.45 * inch, 0.9 * inch])
    move_table.setStyle(TableStyle([
        ("BACKGROUND", (0, 0), (-1, 0), colors.HexColor("#303844")),
        ("TEXTCOLOR", (0, 0), (-1, 0), colors.white),
        ("BACKGROUND", (0, 1), (-1, -1), colors.HexColor("#faf8f4")),
        ("GRID", (0, 0), (-1, -1), 0.3, colors.HexColor("#c5bbb3")),
        ("FONTSIZE", (0, 0), (-1, -1), 6.5),
        ("VALIGN", (0, 0), (-1, -1), "TOP"),
    ]))
    story += [move_table, Spacer(1, 0.18 * inch), Paragraph("Runtime Contract", h2)]
    for text in [
        "WeaponMelee attacks apply damage only during active frames, inside range and hit arc, once per activation.",
        "Behavior trees choose only the opener from idle; runtime may start one follow-up if alive, engaged, in range, and budgeted.",
        "Knight medium shield reduces frontal physical hits; flank/back hits bypass guard and Heavy+ hits can break guard into recovery.",
        "No pathfinding, obstacle LOS, squad tactics, or boss runtime changes are included in M84.",
        "Encounter notes: skeleton patrol, spear lane, knight shield line, giant pressure room, and mixed weapon battlefield.",
    ]:
        story.append(Paragraph("• " + text, body))

    doc.build(story)


def render_previews() -> None:
    pdftoppm = shutil.which("pdftoppm")
    if not pdftoppm:
        print("poppler preview skipped: pdftoppm not found")
        return
    PREVIEW_DIR.mkdir(parents=True, exist_ok=True)
    subprocess.run([pdftoppm, "-png", "-f", "1", "-l", "2", str(PDF_PATH), str(PREVIEW_DIR / "page")], check=True)


if __name__ == "__main__":
    if not DOCS_PATH.exists():
        raise SystemExit(f"missing markdown source: {DOCS_PATH}")
    build_pdf()
    render_previews()
    print(f"generated {PDF_PATH}")
