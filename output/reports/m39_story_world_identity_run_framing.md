# M39 Story, World Identity, And Run Framing V1

- Generated: 2026-04-28T02:36:13.3631000Z
- Catalog: `Assets/_Hollow/Data/Worlds/M39/RunFramingCatalog_M39.asset`
- Scope: data-driven world names, short branch/hub/boss/extraction lines, and a top-center run-framing HUD panel.
- Non-goals: no new story progression saves, no combat balance changes, no branch-generation changes, and no ArtPass authority changes.

## Worlds

- World 1: The Hollow Threshold - A room-made wound where lost souls first learn the rules.
- World 2: The Ashen Toyworks - A deeper floor of broken mechanisms, soot, and patient little traps.
- World 3: The Quiet Reliquary - A final prototype world for things the Hollow wanted to keep.

## Runtime Presentation

- `BranchSessionController.CreateRunFramingSnapshot()` resolves the current world/phase/seed text.
- `RunFramingHudController` renders the snapshot on `PlatformShellCanvas`, outside `WorldPresentationRoot`.
- The panel is intentionally compact so it adds context without stealing the minimap or combat HUD's job.
