#!/usr/bin/env python3
from __future__ import annotations

import shutil
import subprocess
from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
DOCS_PATH = ROOT / "Docs/Hollow_M85_Creature_Action_Expansion.md"
PDF_PATH = ROOT / "output/pdf/Hollow_M85_Creature_Action_Expansion.pdf"
PREVIEW_DIR = ROOT / "output/pdf/previews/m85"

ROSTER = [
    ("Hollow Bird", "3 HP", "2.25m/s", "Light / Simple predator", "1.80-3.60m", "swoop_peck, claw_dive, wing_retreat, caw_signal"),
    ("Hollow Beast", "5 HP", "1.90m/s", "Medium / Basic predator", "1.15-2.10m", "leap_bite, body_check, leap_back, howl_signal"),
]

ACTIONS = [
    ("Normal Chaser", "short_backstep", "CreatureMove", "0", "Light", "2.00m", "0.08/0.22/0.16s", "0.72m", "Reset hop only."),
    ("Normal Chaser", "warning_feint", "CreatureSignal", "0", "Light", "2.40m", "0.16/0.12/0.20s", "-", "Readable shoulder feint."),
    ("Flying Chaser", "fly_strafe", "CreatureMove", "0", "Light", "4.50m", "0.06/0.28/0.12s", "0.95m", "Flying reposition."),
    ("Flying Chaser", "dive_feint", "CreatureSignal", "0", "Light", "3.00m", "0.14/0.12/0.18s", "-", "False dive tell."),
    ("Fast Chaser", "evasive_skitter", "CreatureMove", "0", "Light", "2.20m", "0.06/0.22/0.12s", "0.85m", "Diagonal skitter."),
    ("Fast Chaser", "snap_combo", "MeleeLunge", "1", "Light", "1.15m", "0.14/0.12/0.20s", "0.45m", "Body-only snap hit."),
    ("Heavy Chaser", "guarded_shove", "MeleeLunge", "1", "Medium", "1.55m", "0.30/0.16/0.34s", "0.28m", "Braced shove."),
    ("Heavy Chaser", "slow_overhead_slam", "Area", "2", "Heavy", "1.45m", "0.52/0.20/0.55s", "-", "Long punish recovery."),
    ("Ash Charger", "short_recover_hop", "CreatureMove", "0", "Light", "1.80m", "0.08/0.22/0.18s", "0.78m", "Whiff reset."),
    ("Ash Charger", "shoulder_check", "MeleeLunge", "1", "Medium", "1.25m", "0.24/0.15/0.32s", "0.38m", "Close physical check."),
    ("Husk Splitter", "splitter_backstep", "CreatureMove", "0", "Light", "2.00m", "0.08/0.22/0.16s", "0.80m", "Frames next cleave."),
    ("Husk Splitter", "cleave_feint", "CreatureSignal", "0", "Light", "2.20m", "0.18/0.12/0.22s", "-", "Non-damaging tell."),
    ("Rat", "skitter_retreat", "CreatureMove", "0", "Light", "2.20m", "0.06/0.22/0.12s", "0.90m", "Threat retreat."),
    ("Rat", "panic_pounce", "MeleeLunge", "1", "Light", "1.20m", "0.16/0.14/0.18s", "0.70m", "Territorial panic leap."),
    ("Rat", "alarm_squeal", "CreatureSignal", "0", "Light", "5.00m", "0.18/0.12/0.28s", "-", "Rats only."),
    ("Spider", "panic_flee", "CreatureMove", "0", "Light", "2.00m", "0.05/0.22/0.10s", "1.00m", "Erratic flee."),
    ("Spider", "web_feint", "CreatureSignal", "0", "Light", "2.00m", "0.12/0.12/0.16s", "-", "No web slow."),
    ("Hollow Bird", "swoop_peck", "MeleeLunge", "1", "Light", "1.35m", "0.18/0.15/0.22s", "1.00m", "Committed swoop."),
    ("Hollow Bird", "claw_dive", "MeleeLunge", "1", "Medium", "1.55m", "0.24/0.17/0.28s", "1.15m", "Heavier dive."),
    ("Hollow Bird", "wing_retreat", "CreatureMove", "0", "Light", "3.80m", "0.08/0.25/0.15s", "1.25m", "Flying retreat."),
    ("Hollow Bird", "caw_signal", "CreatureSignal", "0", "Light", "5.20m", "0.22/0.12/0.25s", "-", "Birds only."),
    ("Hollow Beast", "leap_bite", "MeleeLunge", "1", "Light", "1.45m", "0.22/0.16/0.28s", "0.90m", "Punish bite."),
    ("Hollow Beast", "body_check", "MeleeLunge", "1", "Medium", "1.65m", "0.30/0.18/0.38s", "0.75m", "Shoulder check."),
    ("Hollow Beast", "leap_back", "CreatureMove", "0", "Light", "2.20m", "0.10/0.24/0.20s", "1.00m", "Local retreat."),
    ("Hollow Beast", "howl_signal", "CreatureSignal", "0", "Light", "5.50m", "0.28/0.14/0.35s", "-", "Beasts only."),
]


def require_reportlab() -> None:
    try:
        from reportlab.lib import colors  # noqa: F401
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
    title = ParagraphStyle("M85Title", parent=styles["Title"], alignment=TA_CENTER, fontSize=21, leading=25, textColor=colors.HexColor("#25313a"))
    h2 = ParagraphStyle("M85H2", parent=styles["Heading2"], fontSize=13, leading=16, textColor=colors.HexColor("#315336"))
    body = ParagraphStyle("M85Body", parent=styles["BodyText"], fontSize=8.4, leading=11)
    small = ParagraphStyle("M85Small", parent=styles["BodyText"], fontSize=6.7, leading=8.2)

    doc = SimpleDocTemplate(str(PDF_PATH), pagesize=letter, rightMargin=0.42 * inch, leftMargin=0.42 * inch, topMargin=0.4 * inch, bottomMargin=0.4 * inch)
    story = [
        Paragraph("M85: Creature Action Expansion", title),
        Paragraph("Body-only creatures move toward Souls-lite readable commitment. Damage remains physical-only and active-window-only; ordinary body overlap stays harmless from M79.", body),
        Spacer(1, 0.13 * inch),
        Paragraph("New Creature Roster", h2),
    ]

    roster_data = [["Enemy", "Stats", "Identity", "Range", "Action Set"]]
    for row in ROSTER:
        roster_data.append([Paragraph(row[0], small), row[1] + "<br/>" + row[2], row[3], row[4], Paragraph(row[5], small)])
    roster_table = Table(roster_data, colWidths=[1.15 * inch, 1.0 * inch, 1.35 * inch, 1.0 * inch, 2.65 * inch])
    roster_table.setStyle(TableStyle([
        ("BACKGROUND", (0, 0), (-1, 0), colors.HexColor("#2f3d35")),
        ("TEXTCOLOR", (0, 0), (-1, 0), colors.white),
        ("BACKGROUND", (0, 1), (-1, -1), colors.HexColor("#eef4ee")),
        ("GRID", (0, 0), (-1, -1), 0.32, colors.HexColor("#a9b8a8")),
        ("FONTNAME", (0, 0), (-1, 0), "Helvetica-Bold"),
        ("FONTSIZE", (0, 0), (-1, -1), 7.0),
        ("VALIGN", (0, 0), (-1, -1), "TOP"),
    ]))
    story += [roster_table, Spacer(1, 0.16 * inch), Paragraph("Signal And Burst Rules", h2)]
    for text in [
        "Signals are same-family local stimuli only: alarm_squeal affects rats, caw_signal affects Hollow Birds, and howl_signal affects Hollow Beasts.",
        "Movement bursts such as swoop, strafe, skitter, leap-back, circle, and hop-back do not deal damage.",
        "Melee/area creature damage still uses windup, active window, and recovery. Recovery is intentionally punishable.",
        "No pathfinding, obstacle LOS, passive contact damage, status effects, save migration, squad navigation, or boss runtime changes are included.",
    ]:
        story.append(Paragraph("• " + text, body))

    story += [PageBreak(), Paragraph("Creature Action Cards", h2)]
    action_data = [["Owner", "Action", "Runtime", "Dmg", "Force", "Range", "Timing", "Move", "Notes"]]
    for row in ACTIONS:
        action_data.append([Paragraph(row[0], small), Paragraph(row[1], small), *row[2:]])
    action_table = Table(action_data, repeatRows=1, colWidths=[0.82 * inch, 1.02 * inch, 0.8 * inch, 0.32 * inch, 0.48 * inch, 0.48 * inch, 0.84 * inch, 0.46 * inch, 1.62 * inch])
    action_table.setStyle(TableStyle([
        ("BACKGROUND", (0, 0), (-1, 0), colors.HexColor("#2f3d35")),
        ("TEXTCOLOR", (0, 0), (-1, 0), colors.white),
        ("BACKGROUND", (0, 1), (-1, -1), colors.HexColor("#fbfaf6")),
        ("GRID", (0, 0), (-1, -1), 0.25, colors.HexColor("#c1c8bd")),
        ("FONTSIZE", (0, 0), (-1, -1), 5.95),
        ("VALIGN", (0, 0), (-1, -1), "TOP"),
    ]))
    story += [action_table, Spacer(1, 0.16 * inch), Paragraph("Curated Rooms", h2)]
    story.append(Paragraph("Hollow Bird perch room, Hollow Beast den, Rat/spider signal room, and mixed body-creature scramble are generated as designer-approved runtime rooms.", body))

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
