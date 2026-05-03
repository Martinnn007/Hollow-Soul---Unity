#!/usr/bin/env python3
from pathlib import Path

from reportlab.lib import colors
from reportlab.lib.pagesizes import letter
from reportlab.lib.styles import getSampleStyleSheet, ParagraphStyle
from reportlab.lib.units import inch
from reportlab.platypus import Paragraph, SimpleDocTemplate, Spacer, Table, TableStyle


ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / "output/pdf/Hollow_M87_Magic_Ghost_Soul_Enemies.pdf"
DOCS = ROOT / "Docs/Hollow_M87_Magic_Ghost_Soul_Enemies.md"

ROSTER = [
    ("Hollow Acolyte", "caster", "4", "Trained", "sentinel", "slow_soul_orb, rune_burst, veil_step"),
    ("Wraith", "ghost", "3", "Tactical", "predator", "phase_shift, wraith_bolt, curse_touch"),
    ("Soul Eater", "drain predator", "7", "Trained", "predator", "soul_drain, soul_burst, eater_phase_step"),
    ("Curse Binder", "curse caster", "5", "Tactical", "territorial", "binding_bolt, curse_field, sigil_fan"),
    ("Grave Lantern", "stationary pattern", "6", "Basic", "sentinel", "lantern_soul_ring, lantern_curse_fan, grave_orb"),
]

ATTACKS = [
    ("slow_soul_orb", "Projectile", "Soul", "1", "readable slow orb"),
    ("rune_burst", "RadialProjectile", "Soul", "1", "six-lane radial pressure"),
    ("veil_step", "PhaseMove", "Soul", "0", "local non-damaging retreat"),
    ("phase_shift", "PhaseMove", "Soul", "0", "ghost reposition"),
    ("wraith_bolt", "Projectile", "Soul", "1", "quick ghost bolt"),
    ("curse_touch", "MeleeLunge", "Cursed", "1", "short active touch"),
    ("soul_drain", "Beam", "Soul", "1", "narrow committed lane drain"),
    ("soul_burst", "RadialProjectile", "Soul", "1", "eight-lane soul ring"),
    ("curse_field", "Area", "Cursed", "1", "leave-the-sigil area pressure"),
    ("sigil_fan", "FanProjectile", "Cursed", "1", "five-mark fan"),
    ("lantern_soul_ring", "RadialProjectile", "Soul", "1", "ten-shot stationary ring"),
    ("lantern_curse_fan", "FanProjectile", "Cursed", "1", "stationary curse fan"),
]


def para(text, style):
    return Paragraph(text.replace("&", "&amp;"), style)


def build():
    OUT.parent.mkdir(parents=True, exist_ok=True)
    styles = getSampleStyleSheet()
    styles.add(ParagraphStyle(name="Small", parent=styles["BodyText"], fontSize=8, leading=10))
    styles.add(ParagraphStyle(name="CardTitle", parent=styles["Heading3"], fontSize=11, leading=13, spaceAfter=3))
    doc = SimpleDocTemplate(
        str(OUT),
        pagesize=letter,
        rightMargin=0.55 * inch,
        leftMargin=0.55 * inch,
        topMargin=0.55 * inch,
        bottomMargin=0.55 * inch,
        title="Hollow M87 Magic/Ghost/Soul Enemies",
    )
    story = []
    story.append(Paragraph("M87: Magic/Ghost/Soul Enemies V1", styles["Title"]))
    story.append(Paragraph("Casters, ghosts, soul eaters, phase movement, drain, curse, and area pressure.", styles["BodyText"]))
    story.append(Spacer(1, 0.14 * inch))

    story.append(Paragraph("Roster", styles["Heading2"]))
    roster_table = [["Enemy", "Role", "HP", "Intelligence", "Disposition", "Actions"]]
    roster_table += [[para(cell, styles["Small"]) for cell in row] for row in ROSTER]
    table = Table(roster_table, colWidths=[1.05 * inch, 1.0 * inch, 0.35 * inch, 0.78 * inch, 0.78 * inch, 2.45 * inch])
    table.setStyle(TableStyle([
        ("BACKGROUND", (0, 0), (-1, 0), colors.HexColor("#243447")),
        ("TEXTCOLOR", (0, 0), (-1, 0), colors.white),
        ("FONTNAME", (0, 0), (-1, 0), "Helvetica-Bold"),
        ("FONTSIZE", (0, 0), (-1, -1), 8),
        ("VALIGN", (0, 0), (-1, -1), "TOP"),
        ("GRID", (0, 0), (-1, -1), 0.25, colors.HexColor("#9aa5b1")),
        ("ROWBACKGROUNDS", (0, 1), (-1, -1), [colors.white, colors.HexColor("#edf3f7")]),
    ]))
    story.append(table)
    story.append(Spacer(1, 0.18 * inch))

    story.append(Paragraph("Attack And Action Cards", styles["Heading2"]))
    for attack_id, runtime, element, damage, note in ATTACKS:
        display_name = attack_id.replace("_", " ").title()
        data = [[
            para(f"<b>{display_name}</b><br/>{attack_id}", styles["Small"]),
            para(f"Runtime: {runtime}<br/>Element: {element}<br/>Damage: {damage}", styles["Small"]),
            para(note, styles["Small"]),
        ]]
        card = Table(data, colWidths=[1.7 * inch, 1.55 * inch, 3.0 * inch])
        card.setStyle(TableStyle([
            ("BOX", (0, 0), (-1, -1), 0.5, colors.HexColor("#5d7188")),
            ("BACKGROUND", (0, 0), (0, 0), colors.HexColor("#dfeaf3")),
            ("VALIGN", (0, 0), (-1, -1), "TOP"),
            ("LEFTPADDING", (0, 0), (-1, -1), 6),
            ("RIGHTPADDING", (0, 0), (-1, -1), 6),
            ("TOPPADDING", (0, 0), (-1, -1), 5),
            ("BOTTOMPADDING", (0, 0), (-1, -1), 5),
        ]))
        story.append(card)
        story.append(Spacer(1, 0.06 * inch))

    story.append(Paragraph("Runtime Contract", styles["Heading2"]))
    bullets = [
        "Beam deals elemental lane damage only at the active point and uses profile range, arc, knockback, and guard recoil.",
        "PhaseMove is non-damaging local movement with windup, active, and recovery; it is not pathfinding.",
        "Soul and Cursed elements are metadata for future resistance/status systems; M87 does not add statuses.",
        "Magic projectiles remain budgeted. Curse field uses area timing and does not restore passive contact damage.",
        "M88 should add navigation behind an adapter so these actions can later choose better destinations and lanes.",
    ]
    for item in bullets:
        story.append(Paragraph(f"- {item}", styles["BodyText"]))

    doc.build(story)
    print(f"Generated {OUT.relative_to(ROOT)}")


if __name__ == "__main__":
    build()
