#!/usr/bin/env python3
from pathlib import Path
import shutil
import subprocess

from reportlab.lib import colors
from reportlab.lib.pagesizes import landscape, letter
from reportlab.lib.styles import ParagraphStyle, getSampleStyleSheet
from reportlab.lib.units import inch
from reportlab.platypus import SimpleDocTemplate, Paragraph, Spacer, Table, TableStyle


ROOT = Path(__file__).resolve().parents[1]
PDF_PATH = ROOT / "output/pdf/Hollow_M77_Critter_Roster_And_Ballistic_Behaviors.pdf"
PREVIEW_DIR = ROOT / "output/pdf/previews/m77"


ENEMIES = [
    ["Spitting Pod", "spawnEnemySpittingPod", "SpittingPod", "10", "0.00", "Simple", "sentinel", "0m/0deg", "9.0m", "5.5-8.0m", "spit_lob"],
    ["Rat", "spawnEnemyRat", "Rat", "3", "2.65", "Basic", "territorial", "8.0m/260deg", "7.5m", "1.2-2.2m", "rat_bite"],
    ["Spider", "spawnEnemySpider", "Spider", "2", "2.90", "Simple", "prey", "8.5m/300deg", "8.0m", "1.0-1.9m", "startle_hop, close_bite"],
]

ATTACKS = [
    ["Spitting Pod", "Spit Lob", "Physical/Projectile", "Light", "1", "0.35m", "1.00s", "Visible ballistic arc; small splash landing."],
    ["Rat", "Rat Bite", "Physical/Melee", "Light", "1", "0.22m", "0.90s", "Short territorial close bite."],
    ["Spider", "Startle Hop", "Physical/Melee", "Light", "1", "0.30m", "0.85s", "Fast fight-or-flight hop attack."],
    ["Spider", "Close Bite", "Physical/Melee", "Light", "1", "0.22m", "0.75s", "Very close panic bite."],
]

ENCOUNTERS = [
    ["m77_pod_warning", "Spitting Pod + Normal Chaser", "Early ballistic awareness check."],
    ["m77_rat_scramble", "2x Rat + Normal Chaser", "Chaotic territorial pressure."],
    ["m77_spider_scuttle", "2x Spider + Flying Chaser", "Skittish fight-or-flight movement."],
    ["m77_critter_mix", "Pod + Rat + Spider + Fast Chaser", "Mixed M77 roster sampler."],
]

ROOMS = [
    ["m77_spider_brood_den_wide", "Wide 2x1", "8 Spiders", "Three spatial brood groups: 3, 3, and 2."],
    ["m77_rat_warren_single", "Single 1x1", "5 Rats", "Two warren groups: 2 and 3 rats."],
    ["m77_rocky_spider_pod_wide", "Wide 2x1", "1 Pod + 6 Spiders", "Rocky cover field with a central ballistic pod."],
    ["m77_rocky_rat_pod_wide", "Wide 2x1", "1 Pod + 5 Rats", "Rocky cover field with territorial rats and a central pod."],
]


def para(text, style):
    return Paragraph(text, style)


def styled_table(rows, widths):
    table = Table(rows, colWidths=widths, repeatRows=1)
    table.setStyle(TableStyle([
        ("BACKGROUND", (0, 0), (-1, 0), colors.HexColor("#263238")),
        ("TEXTCOLOR", (0, 0), (-1, 0), colors.white),
        ("FONTNAME", (0, 0), (-1, 0), "Helvetica-Bold"),
        ("FONTSIZE", (0, 0), (-1, -1), 7),
        ("GRID", (0, 0), (-1, -1), 0.25, colors.HexColor("#B0BEC5")),
        ("ROWBACKGROUNDS", (0, 1), (-1, -1), [colors.white, colors.HexColor("#F5F7F8")]),
        ("VALIGN", (0, 0), (-1, -1), "TOP"),
        ("LEFTPADDING", (0, 0), (-1, -1), 4),
        ("RIGHTPADDING", (0, 0), (-1, -1), 4),
    ]))
    return table


def build_pdf():
    PDF_PATH.parent.mkdir(parents=True, exist_ok=True)
    styles = getSampleStyleSheet()
    title = ParagraphStyle("Title", parent=styles["Title"], fontSize=18, leading=22, spaceAfter=10)
    h2 = ParagraphStyle("Heading2", parent=styles["Heading2"], fontSize=11, leading=14, spaceBefore=8, spaceAfter=4)
    body = ParagraphStyle("Body", parent=styles["BodyText"], fontSize=8.5, leading=11)

    doc = SimpleDocTemplate(
        str(PDF_PATH),
        pagesize=landscape(letter),
        rightMargin=0.35 * inch,
        leftMargin=0.35 * inch,
        topMargin=0.35 * inch,
        bottomMargin=0.35 * inch,
    )

    story = [
        para("M77: Critter Roster + Ballistic Creature Behaviors V1", title),
        para("Spitting Pod, Rat, and Spider join the enemy roster with intelligence, senses, movement identity, Physical natural attacks, and early encounter coverage. The Pod uses a visible ballistic projectile arc with a small splash landing; Rat and Spider use readable chaotic critter behavior.", body),
        Spacer(1, 8),
        para("Enemy Stat Cards", h2),
        styled_table(
            [["Enemy", "Spawn Kind", "Behavior", "HP", "Speed", "Intel", "Disposition", "Sight", "Hearing", "Range", "Attacks"]] + ENEMIES,
            [0.85 * inch, 1.35 * inch, 0.78 * inch, 0.32 * inch, 0.45 * inch, 0.5 * inch, 0.65 * inch, 0.68 * inch, 0.55 * inch, 0.65 * inch, 1.35 * inch],
        ),
        para("Attack Profiles", h2),
        styled_table(
            [["Owner", "Attack", "Classification", "Force", "Damage", "Knockback", "Cooldown", "Notes"]] + ATTACKS,
            [0.9 * inch, 0.95 * inch, 1.15 * inch, 0.55 * inch, 0.45 * inch, 0.62 * inch, 0.62 * inch, 2.45 * inch],
        ),
        para("Behavior Contract", h2),
        para("Spitting Pod is stationary, blind, hearing-driven, and budgeted as a ranged attacker. Rat is territorial: it roams, warns, delays its first bite, and retreats after damage. Spider is skittish: it startles, then chooses readable fight-or-flight decisions with fast hop or bite attacks.", body),
        para("Encounter And Showcase Coverage", h2),
        styled_table(
            [["Encounter", "Composition", "Purpose"]] + ENCOUNTERS,
            [1.45 * inch, 2.0 * inch, 4.0 * inch],
        ),
        para("Bespoke Room Templates", h2),
        styled_table(
            [["Room", "Footprint", "Composition", "Design Note"]] + ROOMS,
            [1.7 * inch, 0.85 * inch, 1.4 * inch, 3.5 * inch],
        ),
        para("Compatibility", h2),
        para("No poison, acid, elemental resistance, pathfinding, obstacle line of sight, squad tactics, stealth UI, save schema changes, or boss behavior changes are added in M77.", body),
    ]
    doc.build(story)


def render_previews():
    pdftoppm = shutil.which("pdftoppm")
    if not pdftoppm:
        print("poppler preview skipped: pdftoppm not found")
        return
    PREVIEW_DIR.mkdir(parents=True, exist_ok=True)
    subprocess.run([pdftoppm, "-png", str(PDF_PATH), str(PREVIEW_DIR / "page")], check=True)
    print(f"rendered previews to {PREVIEW_DIR}")


if __name__ == "__main__":
    build_pdf()
    render_previews()
    print(f"generated {PDF_PATH}")
