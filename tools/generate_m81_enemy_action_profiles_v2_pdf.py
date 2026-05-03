#!/usr/bin/env python3
from __future__ import annotations

from dataclasses import dataclass
from pathlib import Path
import shutil
import subprocess

from reportlab.lib import colors
from reportlab.lib.pagesizes import landscape, letter
from reportlab.lib.styles import ParagraphStyle, getSampleStyleSheet
from reportlab.lib.units import inch
from reportlab.platypus import (
    PageBreak,
    Paragraph,
    SimpleDocTemplate,
    Spacer,
    Table,
    TableStyle,
)


ROOT = Path(__file__).resolve().parents[1]
DOC_PATH = ROOT / "Docs/Hollow_M81_Enemy_Action_Profiles_V2.md"
REPORT_PATH = ROOT / "output/reports/m81_enemy_action_profiles_v2.md"
PDF_PATH = ROOT / "output/pdf/Hollow_M81_Enemy_Action_Profiles_V2.pdf"
PREVIEW_DIR = ROOT / "output/pdf/previews/m81"


@dataclass(frozen=True)
class ActionRow:
    owner: str
    action: str
    category: str
    intent: str
    shape: str
    usage: str
    linked_attack: str
    counterplay: str


CATEGORIES = [
    ("Body", "Body-only and creature pressure actions such as Bite, Pounce, Slam, and Shove."),
    ("Weapon", "Weapon-user actions for skeletons, knights, giants, duelists, and future humanoids."),
    ("Ranged", "Aimed weapon fire such as Arrow Shot, Musket Shot, and Cannon Shot."),
    ("Projectile", "Pattern pressure such as Spread Shot, Fan Shot, Radial Burst, and Falling Mark."),
    ("Magic", "Cast actions such as Beam, Curse Field, Ground Eruption, and Magic Counter."),
    ("Movement", "Repositioning such as Sidestep, Backstep, Teleport, Burrow, and Fly Strafe."),
    ("Defense", "Guard, Brace, Parry, Counter Stance, and other punishable defensive choices."),
    ("Summon", "Spawn, split, and add-management actions with room pressure budgets."),
    ("Hazard", "Area setup such as Acid Puddle, Fire Patch, Mine, and Falling Debris."),
    ("GhostSoul", "Ghost/soul behavior such as Phase, Possess, Soul Drain, Curse, Fear Pulse, and re-form."),
    ("BossScale", "Large arena-readable attacks such as Shockwave, Arena Hazard, and Desperation Burst."),
]


ENEMY_ROWS = [
    ActionRow("Normal Chaser", "Claw Lunge", "Body", "Damage", "ForwardArc", "CurrentRuntime", "claw_lunge", "Dodge or block the active arc, then punish recovery."),
    ActionRow("Normal Chaser", "Desperate Bite", "Body", "Damage", "ForwardArc", "CurrentRuntime", "desperate_bite", "Stay outside close bite range."),
    ActionRow("Normal Chaser", "Claw Combo", "Body", "Pressure", "ForwardArc", "FutureCandidate", "-", "Future combo must expose a clear final recovery."),
    ActionRow("Flying Chaser", "Panic Peck", "Body", "Damage", "ForwardArc", "CurrentRuntime", "panic_peck", "Endangered prey commits only briefly."),
    ActionRow("Flying Chaser", "Dive Scratch", "Body", "Damage", "ForwardArc", "CurrentRuntime", "dive_scratch", "Sidestep the dive line."),
    ActionRow("Flying Chaser", "Panic Retreat", "Movement", "Escape", "Self", "FutureCandidate", "-", "Read the retreat burst before re-engaging."),
    ActionRow("Fast Chaser", "Quick Pounce", "Body", "Damage", "ForwardArc", "CurrentRuntime", "quick_pounce", "Fast but light; punish whiffs quickly."),
    ActionRow("Fast Chaser", "Needle Rush", "Body", "Damage", "ForwardArc", "CurrentRuntime", "needle_rush", "Avoid being baited into the rush lane."),
    ActionRow("Fast Chaser", "Evasive Skitter", "Movement", "Reposition", "Self", "FutureCandidate", "-", "Track landing rather than swinging early."),
    ActionRow("Heavy Chaser", "Body Slam", "Body", "Damage", "ForwardArc", "CurrentRuntime", "body_slam", "High guard pressure, long recovery."),
    ActionRow("Heavy Chaser", "Maul Lunge", "Body", "Damage", "ForwardArc", "CurrentRuntime", "maul_lunge", "Dodge late and punish commitment."),
    ActionRow("Heavy Chaser", "Stomp", "Body", "Pressure", "CircleArea", "FutureCandidate", "-", "Future stomp needs a visible foot lift."),
    ActionRow("Ash Charger", "Ash Charge", "Body", "Damage", "Lane", "CurrentRuntime", "ash_charge", "Move off the charge lane."),
    ActionRow("Ash Charger", "Ember Clash", "Body", "Damage", "ForwardArc", "CurrentRuntime", "ember_clash", "Short close control hit."),
    ActionRow("Ash Charger", "Fire Trail Charge", "Hazard", "HazardSetup", "Lane", "FutureCandidate", "-", "Do not chase through the future fire lane."),
    ActionRow("Bone Turret", "Bone Dart", "Projectile", "Damage", "Projectile", "CurrentRuntime", "bone_dart", "Strafe the aimed shot."),
    ActionRow("Bone Turret", "Rattle Volley", "Projectile", "Pressure", "Projectile", "CurrentRuntime", "rattle_volley", "Move through gaps and respect ranged budget."),
    ActionRow("Bone Turret", "Aimed Bone Shot", "Ranged", "Damage", "Projectile", "FutureCandidate", "-", "Future ranged option stays stationary."),
    ActionRow("Husk Splitter", "Husk Cleave", "Body", "Damage", "ForwardArc", "CurrentRuntime", "husk_cleave", "Dodge the cleave arc."),
    ActionRow("Husk Splitter", "Death Split", "Summon", "Summon", "CircleArea", "CurrentRuntime", "death_split", "Room clear must account for split children."),
    ActionRow("Husk Splitter", "Splinter Burst", "Hazard", "HazardSetup", "Radial", "FutureCandidate", "-", "Read radial gaps before committing."),
    ActionRow("Spitting Pod", "Spit Lob", "Projectile", "Damage", "Projectile", "CurrentRuntime", "spit_lob", "Move before the ballistic landing point."),
    ActionRow("Spitting Pod", "Seed Burst", "Projectile", "Pressure", "Radial", "FutureCandidate", "-", "Budgeted pod pressure with gaps."),
    ActionRow("Rat", "Rat Bite", "Body", "Damage", "ForwardArc", "CurrentRuntime", "rat_bite", "Wait for territorial warning, then punish bite recovery."),
    ActionRow("Rat", "Warning Squeal", "Body", "Feint", "Cone", "FutureCandidate", "-", "Non-damaging warning before attack selection."),
    ActionRow("Rat", "Skitter Retreat", "Movement", "Escape", "Self", "FutureCandidate", "-", "Damage should make rats retreat readily."),
    ActionRow("Spider", "Startle Hop", "Body", "Damage", "ForwardArc", "CurrentRuntime", "startle_hop", "Side-step the hop and punish recovery."),
    ActionRow("Spider", "Close Bite", "Body", "Damage", "ForwardArc", "CurrentRuntime", "close_bite", "Do not stand inside bite range."),
    ActionRow("Spider", "Panic Flee", "Movement", "Escape", "Self", "FutureCandidate", "-", "Fight-or-flight stays readable and capped."),
]


BOSS_ROWS = [
    ActionRow("Stone Warden", "Stone Charge", "BossScale", "Damage", "Lane", "CurrentRuntime", "stone_charge", "Roll out of lane; punish recovery."),
    ActionRow("Stone Warden", "Stone Shockwave", "BossScale", "Pressure", "Radial", "FutureCandidate", "-", "Radial wave needs safe ring timing."),
    ActionRow("Splinter Saint", "Side-Hop Radial", "BossScale", "Pressure", "Radial", "CurrentRuntime", "splinter_side_hop_radial", "Move through radial gaps."),
    ActionRow("Splinter Saint", "Splinter Dash Feint", "BossScale", "Feint", "Lane", "FutureCandidate", "-", "False dash should not deal damage."),
    ActionRow("Gravel Maw", "Burrow Summon", "BossScale", "Summon", "CircleArea", "CurrentRuntime", "gravel_burrow_summon", "Pressure summon windows without losing room clear."),
    ActionRow("Gravel Maw", "Gravel Emerge Bite", "BossScale", "Damage", "ForwardArc", "FutureCandidate", "-", "Ground tell before emerge attack."),
    ActionRow("Cartouche Widow", "Falling Marks", "BossScale", "HazardSetup", "Fan", "CurrentRuntime", "cartouche_falling_marks", "Keep moving through target marks."),
    ActionRow("Cartouche Widow", "Cartouche Mark Delay", "BossScale", "HazardSetup", "TargetPoint", "FutureCandidate", "-", "Delayed marks create dodge timing."),
    ActionRow("Iron Reliquary", "Peek Shot", "BossScale", "Pressure", "Fan", "CurrentRuntime", "iron_peek_shot", "Punish reload/cover reset."),
    ActionRow("Iron Reliquary", "Iron Bash Recover", "BossScale", "Interrupt", "ForwardArc", "FutureCandidate", "-", "Close bash punishes greedy pressure."),
    ActionRow("Mirror Husk", "Mirror Chase Contact", "BossScale", "Damage", "ForwardArc", "CurrentRuntime", "mirror_chase_contact", "M79 keeps ordinary chase overlap harmless unless active."),
    ActionRow("Mirror Husk", "Mirror Decoy", "BossScale", "Feint", "Self", "FutureCandidate", "-", "Misdirection needs strong readability."),
    ActionRow("Ash Comet", "Comet Dash", "BossScale", "Damage", "Lane", "CurrentRuntime", "ash_comet_dash", "Move off dash line; fire identity remains data."),
    ActionRow("Ash Comet", "Ash Fire Trail", "BossScale", "HazardSetup", "Lane", "FutureCandidate", "-", "Temporary hazard lane should leave safe routes."),
    ActionRow("Choir of Teeth", "Rotating Hymn", "BossScale", "Pressure", "Radial", "CurrentRuntime", "choir_rotating_hymn", "Move with the rotating gap."),
    ActionRow("Choir of Teeth", "Choir Silence Pulse", "BossScale", "Interrupt", "Radial", "FutureCandidate", "-", "Audio drop before pulse."),
    ActionRow("Rust Bishop", "Rust Beam", "BossScale", "Pressure", "Lane", "CurrentRuntime", "rust_beam", "Strafe perpendicular to beam line."),
    ActionRow("Rust Bishop", "Rust Hazard Minefield", "BossScale", "HazardSetup", "HazardZone", "FutureCandidate", "-", "Mines need arming tells."),
    ActionRow("Hollow Star Larva", "Starfall", "BossScale", "Pressure", "Fan", "CurrentRuntime", "larva_starfall", "Read cosmic projectile fan."),
    ActionRow("Hollow Star Larva", "Larva Void Pulse", "BossScale", "Pressure", "Radial", "FutureCandidate", "-", "Void pulse needs clear safe radius."),
]


TEMPLATE_NAMES = {
    "Body": ["Bite", "Claw", "Peck", "Pounce", "Tail Swipe", "Body Slam"],
    "Weapon": ["Light Slash", "Heavy Slash", "Thrust", "Overhead Slash", "Sweep", "Shield Bash"],
    "Ranged": ["Arrow Shot", "Arrow Volley", "Aimed Shot", "Thrown Knife", "Pistol Shot", "Cannon Shot"],
    "Projectile": ["Slow Orb", "Fast Bolt", "Spread Shot", "Radial Burst", "Homing Shot", "Falling Mark"],
    "Magic": ["Beam", "Fire Trail", "Curse Field", "Ground Eruption", "Summoned Orb", "Magic Counter"],
    "Movement": ["Sidestep", "Backstep", "Roll", "Circle", "Teleport", "Burrow"],
    "Defense": ["Guard", "Brace", "Parry", "Evade", "Shield Wall", "Counter Stance"],
    "Summon": ["Summon Minion", "Summon Wave", "Raise Skeleton", "Spawn Trap", "Call Swarm", "Clone Split"],
    "Hazard": ["Spike Trap", "Acid Puddle", "Fire Patch", "Mine", "Falling Debris", "Closing Wall"],
    "GhostSoul": ["Phase", "Possess", "Soul Drain", "Curse", "Fear Pulse", "Re-form"],
    "BossScale": ["Shockwave", "Arena Hazard", "Multi-Stage Combo", "Desperation Burst", "Rotating Pattern", "Boss Grab"],
}


def markdown_table(rows: list[ActionRow]) -> str:
    lines = [
        "| Owner | Action | Category | Intent | Shape | Usage | Linked attack | Counterplay |",
        "| --- | --- | --- | --- | --- | --- | --- | --- |",
    ]
    for row in rows:
        lines.append(f"| {row.owner} | {row.action} | {row.category} | {row.intent} | {row.shape} | {row.usage} | {row.linked_attack} | {row.counterplay} |")
    return "\n".join(lines)


def write_markdown() -> None:
    DOC_PATH.parent.mkdir(parents=True, exist_ok=True)
    lines: list[str] = []
    lines.append("# M81: Enemy Action Profiles V2")
    lines.append("")
    lines.append("M81 adds a data-only action-profile layer above M76 attack profiles. Existing attack profiles remain the source of damage, timing, poise, knockback, guard recoil, and impact classification. Runtime AI does not change in M81.")
    lines.append("")
    lines.append("## Action Taxonomy")
    lines.append("")
    for name, note in CATEGORIES:
        lines.append(f"- **{name}**: {note}")
    lines.append("")
    lines.append("## Current Roster Action Profiles")
    lines.append("")
    lines.append(markdown_table(ENEMY_ROWS))
    lines.append("")
    lines.append("## Boss Action Profiles")
    lines.append("")
    lines.append(markdown_table(BOSS_ROWS))
    lines.append("")
    lines.append("## Counterplay And Scoring Contract")
    lines.append("")
    lines.append("- Actions carry AI scoring metadata: min range, ideal range, max range, weight, pressure cost, cooldown group, minimum intelligence, allowed dispositions, minimum awareness, and facing requirements.")
    lines.append("- Actions carry Dark Souls-style counterplay metadata: telegraph note, punishability rating, guard pressure rating, poise break note, parryable, blockable, dodgeable, and recovery punish note.")
    lines.append("- Linked actions reference M76 attacks. Unlinked future actions and templates are explicitly non-damaging until an attack profile or behavior implementation exists.")
    lines.append("- M82 can select from this layer without changing M81 runtime behavior.")
    lines.append("")
    lines.append("## Reusable Future Action Templates")
    lines.append("")
    for category, names in TEMPLATE_NAMES.items():
        lines.append(f"### {category}")
        lines.append("")
        for name in names:
            users = best_users_for(category)
            lines.append(f"- **{name}**: Category `{category}`, counterplay uses readable windup, active window, poise/counterplay notes, and recovery punish timing. Best users: {users}.")
        lines.append("")
    lines.append("## M82 Readiness")
    lines.append("")
    lines.append("M81 deliberately stops at data, catalogue, validation, and behavior-tree readiness. M82 can layer selector/sequence logic over awareness, intelligence, disposition, cooldowns, range bands, and pressure budgets.")
    DOC_PATH.write_text("\n".join(lines) + "\n", encoding="utf-8")


def best_users_for(category: str) -> str:
    return {
        "Body": "rats, spiders, beasts, undead crows",
        "Weapon": "skeletons, knights, giants",
        "Ranged": "archers, gunslingers, machines",
        "Projectile": "turrets, pods, wizards, bosses",
        "Magic": "wizards, cultists, soul eaters",
        "Movement": "creatures, knights, ghosts",
        "Defense": "knights, machines, bosses",
        "Summon": "necromancers, pods, bosses",
        "Hazard": "pods, machines, casters, bosses",
        "GhostSoul": "ghosts, soul eaters, mirror enemies",
        "BossScale": "bosses and giant enemies",
    }[category]


def make_pdf() -> None:
    PDF_PATH.parent.mkdir(parents=True, exist_ok=True)
    styles = getSampleStyleSheet()
    title = ParagraphStyle("TitleM81", parent=styles["Title"], fontName="Helvetica-Bold", fontSize=20, leading=24, textColor=colors.HexColor("#20242b"))
    h2 = ParagraphStyle("H2M81", parent=styles["Heading2"], fontName="Helvetica-Bold", fontSize=12, leading=14, textColor=colors.HexColor("#29384a"), spaceBefore=8, spaceAfter=4)
    body = ParagraphStyle("BodyM81", parent=styles["BodyText"], fontName="Helvetica", fontSize=7.5, leading=9.2)
    small = ParagraphStyle("SmallM81", parent=styles["BodyText"], fontName="Helvetica", fontSize=6.6, leading=8.1)

    doc = SimpleDocTemplate(
        str(PDF_PATH),
        pagesize=landscape(letter),
        leftMargin=0.38 * inch,
        rightMargin=0.38 * inch,
        topMargin=0.35 * inch,
        bottomMargin=0.35 * inch,
    )
    story = [
        Paragraph("M81: Enemy Action Profiles V2", title),
        Paragraph("Data-only action-profile layer above M76 attack profiles. Runtime enemy behavior does not change in M81; this catalogue is the M82-ready AI and counterplay contract.", body),
        Spacer(1, 0.1 * inch),
        Paragraph("Action Taxonomy", h2),
    ]

    taxonomy_data = [["Category", "Contract"]]
    taxonomy_data.extend([[name, note] for name, note in CATEGORIES])
    story.append(table(taxonomy_data, [1.05 * inch, 8.9 * inch], small))
    story.append(PageBreak())

    story.append(Paragraph("Current Roster Action Profiles", h2))
    story.append(action_table(ENEMY_ROWS, small))
    story.append(PageBreak())

    story.append(Paragraph("Boss Action Profiles", h2))
    story.append(action_table(BOSS_ROWS, small))
    story.append(Spacer(1, 0.08 * inch))
    story.append(Paragraph("Counterplay metadata includes telegraph/readability note, punishability rating, guard pressure rating, poise break note, parryable, blockable, dodgeable, and recovery punish note.", body))
    story.append(PageBreak())

    story.append(Paragraph("Reusable Future Action Templates", h2))
    template_rows = [["Category", "Templates", "Best Users / Notes"]]
    for category, names in TEMPLATE_NAMES.items():
        template_rows.append([category, ", ".join(names), best_users_for(category)])
    story.append(table(template_rows, [1.05 * inch, 5.9 * inch, 3 * inch], small))
    story.append(Spacer(1, 0.08 * inch))
    story.append(Paragraph("Template count: 66. Coverage includes Body, Weapon, Ranged, Projectile, Magic, Movement, Defense, Summon, Hazard, GhostSoul, and BossScale. Key examples: Bite, Overhead Slash, Arrow Volley, Beam, Soul Drain, Guard, Teleport, Shockwave.", body))

    doc.build(story, onFirstPage=footer, onLaterPages=footer)


def table(data: list[list[str]], widths: list[float], style: ParagraphStyle) -> Table:
    wrapped = [[Paragraph(str(cell), style) for cell in row] for row in data]
    output = Table(wrapped, colWidths=widths, repeatRows=1)
    output.setStyle(TableStyle([
        ("BACKGROUND", (0, 0), (-1, 0), colors.HexColor("#20242b")),
        ("TEXTCOLOR", (0, 0), (-1, 0), colors.white),
        ("FONTNAME", (0, 0), (-1, 0), "Helvetica-Bold"),
        ("GRID", (0, 0), (-1, -1), 0.25, colors.HexColor("#c8ccd2")),
        ("VALIGN", (0, 0), (-1, -1), "TOP"),
        ("ROWBACKGROUNDS", (0, 1), (-1, -1), [colors.white, colors.HexColor("#f5f7fa")]),
        ("LEFTPADDING", (0, 0), (-1, -1), 4),
        ("RIGHTPADDING", (0, 0), (-1, -1), 4),
        ("TOPPADDING", (0, 0), (-1, -1), 3),
        ("BOTTOMPADDING", (0, 0), (-1, -1), 3),
    ]))
    return output


def action_table(rows: list[ActionRow], style: ParagraphStyle) -> Table:
    data = [["Owner", "Action", "Cat", "Intent", "Shape", "Usage", "Link", "Counterplay"]]
    data.extend([[r.owner, r.action, r.category, r.intent, r.shape, r.usage, r.linked_attack, r.counterplay] for r in rows])
    return table(data, [1.1 * inch, 1.25 * inch, 0.72 * inch, 0.78 * inch, 0.86 * inch, 0.95 * inch, 1.05 * inch, 3.2 * inch], style)


def footer(canvas, doc) -> None:
    canvas.saveState()
    canvas.setFont("Helvetica", 7)
    canvas.setFillColor(colors.HexColor("#60666f"))
    canvas.drawRightString(10.6 * inch, 0.2 * inch, f"M81 Enemy Action Profiles V2 - page {doc.page}")
    canvas.restoreState()


def write_report() -> None:
    REPORT_PATH.parent.mkdir(parents=True, exist_ok=True)
    template_count = sum(len(names) for names in TEMPLATE_NAMES.values())
    REPORT_PATH.write_text(
        "# M81 Enemy Action Profiles V2 Report\n\n"
        f"- Catalogue Markdown: `{DOC_PATH.relative_to(ROOT)}`.\n"
        f"- Catalogue PDF: `{PDF_PATH.relative_to(ROOT)}`.\n"
        f"- Enemy rows documented: {len(ENEMY_ROWS)}.\n"
        f"- Boss rows documented: {len(BOSS_ROWS)}.\n"
        f"- Reusable future templates: {template_count}.\n"
        "- Runtime policy: data/catalogue/validation only; M76/M80 attack execution remains unchanged.\n",
        encoding="utf-8",
    )


def render_previews() -> None:
    pdftoppm = shutil.which("pdftoppm")
    if not pdftoppm:
        return
    PREVIEW_DIR.mkdir(parents=True, exist_ok=True)
    prefix = PREVIEW_DIR / "page"
    subprocess.run([pdftoppm, "-png", "-f", "1", "-l", "3", str(PDF_PATH), str(prefix)], check=True)


def main() -> None:
    write_markdown()
    make_pdf()
    write_report()
    render_previews()
    print(f"generated {PDF_PATH.relative_to(ROOT)}")


if __name__ == "__main__":
    main()
