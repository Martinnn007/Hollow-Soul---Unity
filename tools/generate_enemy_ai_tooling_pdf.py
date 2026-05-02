#!/usr/bin/env python3
from pathlib import Path
import shutil
import subprocess

from reportlab.lib import colors
from reportlab.lib.enums import TA_CENTER
from reportlab.lib.pagesizes import letter
from reportlab.lib.styles import ParagraphStyle, getSampleStyleSheet
from reportlab.lib.units import inch
from reportlab.platypus import (
    HRFlowable,
    KeepTogether,
    ListFlowable,
    ListItem,
    LongTable,
    PageBreak,
    Paragraph,
    SimpleDocTemplate,
    Spacer,
    Table,
    TableStyle,
)


ROOT = Path(__file__).resolve().parents[1]
PDF_PATH = ROOT / "output" / "pdf" / "Hollow_Enemy_AI_Tooling_Recommendations.pdf"
PREVIEW_DIR = ROOT / "output" / "pdf" / "previews" / "enemy_ai_tooling"


TOOLS = [
    {
        "rank": "1",
        "name": "A* Pathfinding Project Pro",
        "snapshot": "Asset Store fallback: v5.4.6, Jan 22 2026, $140, original Unity 2021.3.45.",
        "why": "Best long-term pathfinding upgrade: grid/recast graphs, graph updates, AIPath-style movement, and Pro local avoidance using ORCA/RVO.",
        "fit": "Excellent. Solves Hollow's real missing layer: obstacle-aware paths, room graph reuse, dynamic updates, and local avoidance without replacing combat data.",
        "codex": "User purchases/imports. Codex can build RoomNavigationBuilder, an enemy navigation adapter, tests, and one shared prefab/spawn integration.",
        "risk": "Paid dependency and import/API validation. Wrap behind a Hollow interface so Unity AI Navigation/current movement can remain fallback.",
        "verdict": "Best paid pick. If we buy one tool, buy this first.",
    },
    {
        "rank": "2",
        "name": "Unity AI Navigation",
        "snapshot": "Already installed locally as com.unity.ai.navigation 2.0.11.",
        "why": "No new purchase, official Unity support, runtime/edit-time NavMesh building, dynamic obstacles, and links.",
        "fit": "Good baseline for X/Z room spaces with generated floor and obstacle geometry.",
        "codex": "Codex can implement runtime NavMesh building, an agent adapter, and shared enemy movement without per-enemy setup.",
        "risk": "Less grid-native than Hollow room data. Runtime surface filters and generated-room baking need care.",
        "verdict": "Best no-cost prototype if we are unsure about buying A* Pro.",
    },
    {
        "rank": "3",
        "name": "Behavior Designer Pro",
        "snapshot": "Asset Store fallback: v2.1.12, Jan 28 2026, $145, original Unity 2022.3.20.",
        "why": "Strong visual behavior-tree authoring with DOTS-backed traversal, shared variables, subtrees, debugging, and custom tasks.",
        "fit": "Strong after pathfinding for readable sentinel/prey/predator/charger/turret/boss decisions.",
        "codex": "User purchases/imports. Codex can create reusable GameObject tasks bound to EnemyDefinition and Hollow attack budgets.",
        "risk": "Adds Entities/Burst dependency and Opsive documents no WebGL support while Entities/Burst lack it. Does not solve navigation alone.",
        "verdict": "Best visual behavior-tree option for long-term power, but phase 3 rather than phase 1.",
    },
    {
        "rank": "4",
        "name": "NodeCanvas",
        "snapshot": "Marketplace/fallback: v3.4.1, Feb 20 2026, list $120 with sale pricing sometimes visible.",
        "why": "Mature BT/FSM/Dialog authoring, full source, blackboards, subgraphs, runtime debugging, and A* integration signal.",
        "fit": "Very good if we want designer-facing logic without DOTS assumptions; especially attractive for bosses and hybrid state behavior.",
        "codex": "User purchases/imports. Codex can write reusable tasks and map variables from EnemyDefinition.",
        "risk": "Less future-looking for massive agent counts. Graph freedom can invite one-off per-enemy authoring unless constrained.",
        "verdict": "Best practical alternative to Behavior Designer Pro, especially if DOTS/WebGL risk matters.",
    },
    {
        "rank": "5",
        "name": "Unity Behavior",
        "snapshot": "Unity docs: com.unity.behavior 1.0.13 released for Unity Editor 6000.0.",
        "why": "Official graph-based behavior trees with reusable subgraphs, C# integration, prebuilt nodes, and play-mode debugging.",
        "fit": "Interesting no-purchase behavior layer after navigation is solved.",
        "codex": "Codex can add the package if requested, create custom nodes, and bind shared graphs to current enemy data.",
        "risk": "Younger ecosystem than Behavior Designer Pro or NodeCanvas; likely needs more Hollow-specific nodes.",
        "verdict": "Good official experiment. Not the top production recommendation yet.",
    },
    {
        "rank": "6",
        "name": "GOAP v3 by CrashKonijn",
        "snapshot": "Asset Store fallback: free, v3.1.1, Dec 12 2025; docs show package tag 3.1.2.",
        "why": "Powerful multi-threaded goal/action planning with debugging for systemic AI.",
        "fit": "Situational. Useful for future bosses, companions, or NPC planners, not first for obstacle-aware chasers.",
        "codex": "Codex can integrate and author code-based goals/actions, but the goal model is design-heavy.",
        "risk": "Adds planning complexity where Hollow mostly needs movement and readable tactical variants.",
        "verdict": "Keep in reserve for systemic AI.",
    },
    {
        "rank": "7",
        "name": "Utility Intelligence GO v3",
        "snapshot": "Asset Store fallback: v3.1.3, Apr 18 2026, $125, original Unity 6000.0.71.",
        "why": "Utility scoring fits target choice, hold range, retreat, pressure, charge use, and boss phase selection.",
        "fit": "Useful later as a high-level decision layer for Tactical/Cunning enemies; not navigation.",
        "codex": "User purchases/imports. Codex can bind scoring inputs to EnemyDefinition, room state, distance, damage memory, and attack budget.",
        "risk": "Newer/smaller market signal. Scoring curves need playtest tuning.",
        "verdict": "Interesting later. Do not buy before pathfinding.",
    },
    {
        "rank": "8",
        "name": "Emerald AI 2025",
        "snapshot": "Marketplace: v1.3.3, Apr 8 2026, list $60 with $30 sale snapshot, Unity 6000.0.21f1 compatible.",
        "why": "Fast generic RPG/NPC/animal/faction/patrol/combat setup.",
        "fit": "Poor as Hollow's core layer because it overlaps with bespoke room combat, readability, budgets, bosses, spawning, and definitions.",
        "codex": "User purchases/imports. Codex could adapter-wrap it, but much of the value is inspector-driven per-enemy setup.",
        "risk": "Duplicate combat logic, integration overhead, and likely manual tuning per enemy.",
        "verdict": "Do not use as the core Hollow enemy AI solution.",
    },
]


SOURCES = [
    ("A* Pathfinding Project Pro Asset Store", "https://assetstore-fallback.unity.com/packages/tools/behavior-ai/a-pathfinding-project-pro-87744"),
    ("A* graph types", "https://arongranberg.com/astar/documentation/stable/graphtypes.html"),
    ("A* local avoidance", "https://arongranberg.com/astar/documentation/stable/localavoidance.html"),
    ("Unity AI Navigation manual", "https://docs.unity3d.com/ja/current/Manual/com.unity.ai.navigation.html"),
    ("Unity Behavior manual", "https://docs.unity3d.com/kr/current/Manual/com.unity.behavior.html"),
    ("Behavior Designer Pro Asset Store", "https://assetstore-fallback.unity.com/packages/tools/visual-scripting/behavior-designer-pro-dots-powered-behavior-trees-298743"),
    ("Behavior Designer Pro docs", "https://opsive.com/support/documentation/behavior-designer-pro/"),
    ("Behavior Designer Pro requirements", "https://opsive.com/support/documentation/behavior-designer-pro/requirements/"),
    ("NodeCanvas Asset Store", "https://marketplace.unity.com/packages/tools/visual-scripting/nodecanvas-14914"),
    ("NodeCanvas official site", "https://nodecanvas.paradoxnotion.com/"),
    ("GOAP v3 Asset Store", "https://assetstore-fallback.unity.com/packages/tools/behavior-ai/goap-v3-302434"),
    ("GOAP v3 docs", "https://goap.crashkonijn.com/readme/tutorial/gettingstarted"),
    ("Utility Intelligence GO v3 Asset Store", "https://assetstore-fallback.unity.com/packages/tools/behavior-ai/utility-intelligence-go-v3-utility-ai-framework-308338"),
    ("Emerald AI 2025 Asset Store", "https://marketplace.unity.com/packages/tools/behavior-ai/emerald-ai-2025-268519"),
]


def p(text: str, style: ParagraphStyle) -> Paragraph:
    return Paragraph(text.replace("&", "&amp;"), style)


def bullet_list(items, styles):
    return ListFlowable(
        [ListItem(p(item, styles["Body"]), leftIndent=10) for item in items],
        bulletType="bullet",
        start="circle",
        leftIndent=14,
        bulletFontSize=6,
    )


def make_tool_card(tool, styles):
    header = [[p(f"{tool['rank']}. {tool['name']}", styles["CardHeader"])]]
    header_table = Table(header, colWidths=[7.1 * inch])
    header_table.setStyle(TableStyle([
        ("BACKGROUND", (0, 0), (-1, -1), colors.HexColor("#20242b")),
        ("TEXTCOLOR", (0, 0), (-1, -1), colors.white),
        ("LEFTPADDING", (0, 0), (-1, -1), 7),
        ("RIGHTPADDING", (0, 0), (-1, -1), 7),
        ("TOPPADDING", (0, 0), (-1, -1), 5),
        ("BOTTOMPADDING", (0, 0), (-1, -1), 5),
    ]))

    rows = [
        ["Snapshot", tool["snapshot"]],
        ["Why", tool["why"]],
        ["Hollow fit", tool["fit"]],
        ["Codex/setup", tool["codex"]],
        ["Risks", tool["risk"]],
        ["Verdict", tool["verdict"]],
    ]
    table_rows = [[p(label, styles["Label"]), p(value, styles["SmallBody"])] for label, value in rows]
    body_table = Table(table_rows, colWidths=[1.05 * inch, 6.05 * inch])
    body_table.setStyle(TableStyle([
        ("GRID", (0, 0), (-1, -1), 0.35, colors.HexColor("#cfd5df")),
        ("BACKGROUND", (0, 0), (0, -1), colors.HexColor("#eef2f6")),
        ("BACKGROUND", (1, 0), (1, -1), colors.HexColor("#fbfcfd")),
        ("VALIGN", (0, 0), (-1, -1), "TOP"),
        ("LEFTPADDING", (0, 0), (-1, -1), 5),
        ("RIGHTPADDING", (0, 0), (-1, -1), 5),
        ("TOPPADDING", (0, 0), (-1, -1), 4),
        ("BOTTOMPADDING", (0, 0), (-1, -1), 4),
    ]))
    return [header_table, body_table, Spacer(1, 0.10 * inch)]


def build_pdf() -> None:
    PDF_PATH.parent.mkdir(parents=True, exist_ok=True)
    styles = getSampleStyleSheet()
    styles.add(ParagraphStyle(
        name="Subtitle",
        parent=styles["BodyText"],
        alignment=TA_CENTER,
        fontSize=9,
        leading=11,
        textColor=colors.HexColor("#4e5663"),
        spaceAfter=8,
    ))
    styles.add(ParagraphStyle(
        name="Body",
        parent=styles["BodyText"],
        fontSize=9,
        leading=12,
        textColor=colors.HexColor("#252a33"),
        spaceAfter=5,
    ))
    styles.add(ParagraphStyle(
        name="SmallBody",
        parent=styles["Body"],
        fontSize=8,
        leading=10,
        spaceAfter=0,
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
    styles.add(ParagraphStyle(
        name="CardHeader",
        parent=styles["Heading3"],
        fontSize=10,
        leading=12,
        textColor=colors.white,
        spaceBefore=0,
        spaceAfter=0,
    ))
    styles.add(ParagraphStyle(
        name="Label",
        parent=styles["SmallBody"],
        fontName="Helvetica-Bold",
        textColor=colors.HexColor("#20242b"),
    ))
    styles.add(ParagraphStyle(
        name="TableHead",
        parent=styles["SmallBody"],
        fontName="Helvetica-Bold",
        textColor=colors.white,
    ))
    styles.add(ParagraphStyle(
        name="Source",
        parent=styles["SmallBody"],
        fontSize=7,
        leading=8,
        textColor=colors.HexColor("#303844"),
    ))

    doc = SimpleDocTemplate(
        str(PDF_PATH),
        pagesize=letter,
        rightMargin=0.55 * inch,
        leftMargin=0.55 * inch,
        topMargin=0.55 * inch,
        bottomMargin=0.55 * inch,
        title="Hollow Enemy AI Tooling Recommendations",
        author="OpenAI Codex",
    )

    story = [
        p("Hollow Enemy AI Tooling Recommendations", styles["Title"]),
        p("Decision memo - snapshot May 2, 2026 - optimized for long-term power with minimal per-enemy setup", styles["Subtitle"]),
        HRFlowable(width="100%", color=colors.HexColor("#cfd5df"), thickness=0.6),
        Spacer(1, 0.08 * inch),
        p("Decision", styles["Section"]),
        p(
            "Buy or adopt pathfinding before buying a behavior authoring suite. Hollow already has a good data spine through EnemyDefinition, EnemyBehaviorId, intelligence, disposition, attack budgets, and room-local collision. The missing production multiplier is route planning around authored room obstacles, holes, and future perception spaces.",
            styles["Body"],
        ),
        bullet_list([
            "Best paid first purchase: A* Pathfinding Project Pro.",
            "Best no-new-purchase first pass: Unity AI Navigation, already installed locally.",
            "Best visual behavior-tree layer later: Behavior Designer Pro for long-term power, or NodeCanvas if we prefer mature GameObject-first authoring and full source.",
            "Avoid Emerald AI as the core Hollow enemy system; it is better for generic NPC projects than bespoke room combat.",
        ], styles),
        p("Current Hollow Baseline", styles["Section"]),
        bullet_list([
            "Unity 6000.4.1f1 with com.unity.ai.navigation 2.0.11 already in Packages/manifest.json.",
            "Enemies currently use direct vector chase/retreat/hold in EnemyRuntimeController and RoomLocalCollision.",
            "EnemyDefinition already carries archetype, behavior, movement mode, intelligence, disposition, range, attack, charge, body, and split data.",
            "Existing M72 docs explicitly defer pathfinding, line of sight, squad coordination, and richer boss behavior.",
        ], styles),
        p("Recommendation Shape", styles["Section"]),
    ]

    reco_rows = [
        ["Phase", "Action", "Why"],
        ["1 - Navigation", "Use A* Pro if buying; otherwise prototype with Unity AI Navigation.", "Solves obstacle-aware movement first and keeps combat behavior stable."],
        ["2 - Behavior abstraction", "Extract movement decisions into data-driven movement intents and add perception hooks.", "Keeps tests deterministic while preparing for richer AI."],
        ["3 - Visual authoring", "Add Behavior Designer Pro or NodeCanvas only if shared graphs will accelerate enemy/boss design.", "Avoids per-enemy graph work while allowing designer-facing logic."],
    ]
    reco_table = LongTable(
        [[p(cell, styles["TableHead"] if idx == 0 else styles["SmallBody"]) for cell in row] for idx, row in enumerate(reco_rows)],
        colWidths=[1.25 * inch, 3.15 * inch, 2.7 * inch],
        repeatRows=1,
    )
    reco_table.setStyle(TableStyle([
        ("BACKGROUND", (0, 0), (-1, 0), colors.HexColor("#20242b")),
        ("TEXTCOLOR", (0, 0), (-1, 0), colors.white),
        ("GRID", (0, 0), (-1, -1), 0.35, colors.HexColor("#cfd5df")),
        ("BACKGROUND", (0, 1), (-1, -1), colors.HexColor("#fbfcfd")),
        ("VALIGN", (0, 0), (-1, -1), "TOP"),
        ("LEFTPADDING", (0, 0), (-1, -1), 5),
        ("RIGHTPADDING", (0, 0), (-1, -1), 5),
        ("TOPPADDING", (0, 0), (-1, -1), 5),
        ("BOTTOMPADDING", (0, 0), (-1, -1), 5),
    ]))
    story.extend([reco_table, PageBreak(), p("Ranked Tools", styles["Section"])])

    for index, tool in enumerate(TOOLS):
        story.extend(make_tool_card(tool, styles))
        if index in {2, 5}:
            story.append(PageBreak())

    story.extend([
        p("Manual Setup Answer", styles["Section"]),
        p(
            "Codex can take care of most integration work once assets exist in the project: shared components, adapters, custom graph tasks/nodes, prefab/spawn glue, ScriptableObject bindings, migration helpers, and tests.",
            styles["Body"],
        ),
        p(
            "The user still needs to purchase/import paid Asset Store packages. For visual graph tools, the user may also want to inspect or tune shared behavior graphs in Unity. The recommended architecture avoids per-enemy manual configuration by using existing EnemyDefinition data and shared prefabs/subgraphs.",
            styles["Body"],
        ),
        p("Sources", styles["Section"]),
    ])
    source_rows = [["Source", "URL"]] + [[name, url] for name, url in SOURCES]
    source_table = LongTable(
        [[p(cell, styles["TableHead"] if idx == 0 else styles["Source"]) for cell in row] for idx, row in enumerate(source_rows)],
        colWidths=[2.25 * inch, 4.85 * inch],
        repeatRows=1,
    )
    source_table.setStyle(TableStyle([
        ("BACKGROUND", (0, 0), (-1, 0), colors.HexColor("#20242b")),
        ("TEXTCOLOR", (0, 0), (-1, 0), colors.white),
        ("GRID", (0, 0), (-1, -1), 0.35, colors.HexColor("#d6dbe3")),
        ("BACKGROUND", (0, 1), (-1, -1), colors.HexColor("#fbfcfd")),
        ("VALIGN", (0, 0), (-1, -1), "TOP"),
        ("LEFTPADDING", (0, 0), (-1, -1), 4),
        ("RIGHTPADDING", (0, 0), (-1, -1), 4),
        ("TOPPADDING", (0, 0), (-1, -1), 3),
        ("BOTTOMPADDING", (0, 0), (-1, -1), 3),
    ]))
    story.append(source_table)
    story.append(Spacer(1, 0.08 * inch))
    story.append(p("Asset Store prices and versions are checkout snapshots from May 2, 2026 and should be verified before purchase.", styles["SmallBody"]))

    doc.build(story, onFirstPage=draw_footer, onLaterPages=draw_footer)


def draw_footer(canvas, doc):
    canvas.saveState()
    canvas.setFont("Helvetica", 7)
    canvas.setFillColor(colors.HexColor("#6b7280"))
    canvas.drawString(doc.leftMargin, 0.30 * inch, "Hollow Enemy AI Tooling Recommendations")
    canvas.drawRightString(letter[0] - doc.rightMargin, 0.30 * inch, f"Page {doc.page}")
    canvas.restoreState()


def render_previews() -> None:
    PREVIEW_DIR.mkdir(parents=True, exist_ok=True)
    pdftoppm = shutil.which("pdftoppm")
    if pdftoppm:
        prefix = PREVIEW_DIR / "Hollow_Enemy_AI_Tooling_Recommendations"
        subprocess.run([pdftoppm, "-png", str(PDF_PATH), str(prefix)], check=True)
        print(f"previews written to {PREVIEW_DIR}")
        return

    try:
        import pypdfium2 as pdfium
    except Exception as exception:
        print(f"generated {PDF_PATH}; preview skipped because pdftoppm and pypdfium2 are unavailable: {exception}")
        return

    pdf = pdfium.PdfDocument(str(PDF_PATH))
    for index, page in enumerate(pdf):
        bitmap = page.render(scale=1.5).to_pil()
        bitmap.save(PREVIEW_DIR / f"Hollow_Enemy_AI_Tooling_Recommendations-{index + 1}.png")
    print(f"generated {PDF_PATH}; previews written to {PREVIEW_DIR}")


def main() -> None:
    build_pdf()
    render_previews()


if __name__ == "__main__":
    main()
