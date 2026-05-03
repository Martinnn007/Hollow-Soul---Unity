#!/usr/bin/env python3
from pathlib import Path

from reportlab.lib import colors
from reportlab.lib.pagesizes import letter
from reportlab.lib.styles import ParagraphStyle, getSampleStyleSheet
from reportlab.lib.units import inch
from reportlab.platypus import Paragraph, SimpleDocTemplate, Spacer, Table, TableStyle


ROOT = Path(__file__).resolve().parents[1]
DOCS_PATH = ROOT / "Docs/Hollow_M86_Ranged_Firearm_Enemies.md"
PDF_PATH = ROOT / "output/pdf/Hollow_M86_Ranged_Firearm_Enemies.pdf"

ROSTER = [
    ("Hollow Archer", "spawnEnemyHollowArcher", "HP4", "1.35m/s", "Basic", "sentinel", "4.00-7.25m", "Bow draw, single shot, retreating shot, volley."),
    ("Powder Gunner", "spawnEnemyPowderGunner", "HP5", "1.05m/s", "Trained", "sentinel", "4.75-8.50m", "Slow firearm aim, heavy musket shot, scatter shot."),
    ("Knife Thrower", "spawnEnemyKnifeThrower", "HP4", "1.75m/s", "Basic", "territorial", "2.70-5.25m", "Quick thrown knife, fan throw, evasive spacing."),
    ("Repeater Turret", "spawnEnemyRepeaterTurret", "HP6", "0.00m/s", "Trained", "sentinel", "6.00-9.25m", "Stationary burst and suppressing fan patterns."),
    ("Clockwork Sentry", "spawnEnemyClockworkSentry", "HP8", "0.65m/s", "Tactical", "sentinel", "4.80-7.80m", "Slow machine with radial and rotating projectile patterns."),
]

ATTACKS = [
    ("Hollow Archer", "arrow_shot", "Projectile", "1", "Light", "7.50m", "1", "0.38/0.08/0.38s", "Bow shot with narrow aim read."),
    ("Hollow Archer", "retreating_arrow", "Projectile", "1", "Light", "6.70m", "1", "0.34/0.08/0.42s", "Spacing shot after pressure."),
    ("Hollow Archer", "arrow_volley", "FanProjectile", "1", "Light", "7.25m", "3", "0.48/0.08/0.46s", "Three arrows with gaps."),
    ("Hollow Archer", "archer_backstep", "CreatureMove", "0", "Light", "2.50m", "0", "0.08/0.20/0.20s", "Harmless ranged reset."),
    ("Powder Gunner", "aimed_musket_shot", "Projectile", "2", "Heavy", "8.80m", "1", "0.72/0.06/0.75s", "Long aim, fast heavy projectile."),
    ("Powder Gunner", "scatter_shot", "FanProjectile", "1", "Medium", "5.40m", "5", "0.58/0.08/0.68s", "Close firearm fan."),
    ("Powder Gunner", "gunner_backstep", "CreatureMove", "0", "Light", "2.50m", "0", "0.12/0.20/0.26s", "Harmless reload-space hop."),
    ("Knife Thrower", "throwing_knife", "Projectile", "1", "Light", "5.80m", "1", "0.22/0.06/0.24s", "Fast skirmisher throw."),
    ("Knife Thrower", "knife_fan", "FanProjectile", "1", "Light", "4.80m", "3", "0.34/0.08/0.32s", "Three-knife pressure fan."),
    ("Knife Thrower", "thrower_backstep", "CreatureMove", "0", "Light", "2.10m", "0", "0.06/0.18/0.16s", "Harmless evasive burst."),
    ("Repeater Turret", "repeater_burst", "FanProjectile", "1", "Light", "8.50m", "3", "0.36/0.08/0.35s", "Narrow three-shot burst."),
    ("Repeater Turret", "suppressing_arc", "FanProjectile", "1", "Light", "8.00m", "5", "0.52/0.08/0.50s", "Wide stationary fan."),
    ("Repeater Turret", "lock_on_dart", "Projectile", "1", "Light", "9.25m", "1", "0.42/0.06/0.32s", "Precise lock-on shot."),
    ("Clockwork Sentry", "clockwork_radial", "RadialProjectile", "1", "Medium", "6.50m", "8", "0.55/0.10/0.55s", "Radial gap-finding pattern."),
    ("Clockwork Sentry", "rotating_fan", "FanProjectile", "1", "Light", "7.00m", "5", "0.38/0.08/0.42s", "Rotating fan volley."),
    ("Clockwork Sentry", "gear_shot", "Projectile", "1", "Light", "7.80m", "1", "0.30/0.06/0.30s", "Simple gear projectile."),
]


def para(text, style):
    return Paragraph(str(text).replace("&", "&amp;"), style)


def table(data, widths, style):
    wrapped = [[para(cell, style) for cell in row] for row in data]
    table_obj = Table(wrapped, colWidths=widths, repeatRows=1)
    table_obj.setStyle(TableStyle([
        ("BACKGROUND", (0, 0), (-1, 0), colors.HexColor("#20252b")),
        ("TEXTCOLOR", (0, 0), (-1, 0), colors.white),
        ("GRID", (0, 0), (-1, -1), 0.25, colors.HexColor("#aeb7c2")),
        ("VALIGN", (0, 0), (-1, -1), "TOP"),
        ("ROWBACKGROUNDS", (0, 1), (-1, -1), [colors.HexColor("#f6f8fa"), colors.white]),
        ("LEFTPADDING", (0, 0), (-1, -1), 4),
        ("RIGHTPADDING", (0, 0), (-1, -1), 4),
        ("TOPPADDING", (0, 0), (-1, -1), 4),
        ("BOTTOMPADDING", (0, 0), (-1, -1), 4),
    ]))
    return table_obj


def main():
    PDF_PATH.parent.mkdir(parents=True, exist_ok=True)
    styles = getSampleStyleSheet()
    title = ParagraphStyle("Title", parent=styles["Title"], fontName="Helvetica-Bold", fontSize=20, leading=24, textColor=colors.HexColor("#161a1d"))
    h2 = ParagraphStyle("H2", parent=styles["Heading2"], fontName="Helvetica-Bold", fontSize=12, leading=15, spaceBefore=10)
    body = ParagraphStyle("Body", parent=styles["BodyText"], fontName="Helvetica", fontSize=8.2, leading=10.5)
    small = ParagraphStyle("Small", parent=body, fontSize=7.2, leading=9)

    story = [
        para("M86: Ranged + Firearm Enemies V1", title),
        para("Dark Souls-inspired ranged commitment for archers, gunners, throwers, turrets, machines, and projectile pattern enemies. Damage remains physical and active-window-only; ordinary body contact remains harmless.", body),
        Spacer(1, 0.12 * inch),
        para("Roster", h2),
        table(
            [("Enemy", "Spawn", "HP", "Speed", "AI", "Disposition", "Range", "Identity")] + ROSTER,
            [0.9 * inch, 1.25 * inch, 0.35 * inch, 0.55 * inch, 0.5 * inch, 0.65 * inch, 0.75 * inch, 1.8 * inch],
            small,
        ),
        Spacer(1, 0.12 * inch),
        para("Attack Profiles", h2),
        table(
            [("Enemy", "Attack", "Runtime", "Dmg", "Force", "Range", "Count", "Timing", "Notes")] + ATTACKS,
            [0.78 * inch, 0.98 * inch, 0.72 * inch, 0.32 * inch, 0.45 * inch, 0.52 * inch, 0.42 * inch, 0.82 * inch, 1.5 * inch],
            small,
        ),
        Spacer(1, 0.12 * inch),
        para("Runtime Contract", h2),
        para("Profile-specific StartRangedAction choices check range before commitment. Projectile, FanProjectile, and RadialProjectile patterns spawn only at the active transition, then enter recovery. Ranged/charge budgets remain authoritative, and Tactical/Cunning intelligence only wins tie-breaks without increasing total pressure.", body),
        para("M87 Bridge", h2),
        para("The same profile-driven projectile path is ready for magic, ghost, soul, curse, and area-pressure enemies later, without adding a separate caster projectile system in M86.", body),
    ]

    doc = SimpleDocTemplate(str(PDF_PATH), pagesize=letter, rightMargin=0.42 * inch, leftMargin=0.42 * inch, topMargin=0.44 * inch, bottomMargin=0.44 * inch)
    doc.build(story)
    print(f"Generated {PDF_PATH}")


if __name__ == "__main__":
    main()
