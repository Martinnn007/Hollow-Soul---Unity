# Milestone 39: Story, World Identity, And Run Framing V1

M39 adds the first lightweight story/run-framing layer without changing combat, branch generation, rewards, saves, room data, or ArtPass authority.

## What Changed

- Added `RunFramingDefinition` and `RunFramingCatalogDefinition` under `Assets/_Hollow/Data/Worlds/M39/`.
- Added three prototype world identities: The Hollow Threshold, The Ashen Toyworks, and The Quiet Reliquary.
- Added `RunFramingService` to resolve current world, phase, boss-threshold state, and seed text into a compact runtime snapshot.
- Added `RunFramingHudController` on `PlatformShellCanvas`, outside `WorldPresentationRoot`, so the framing text remains unscaled on Windows and Vision Pro routes.
- Added M39 generation, validation, tests, and a report at `output/reports/m39_story_world_identity_run_framing.md`.

## Runtime Behavior

The HUD shows:

- Current world name.
- Current phase: prologue branch, hub branch, inter-branch hub, boss threshold, or extraction.
- Run seed and branch seed for debugging/replay notes.
- One short world-specific line that explains the current context.

The system is intentionally read-only. It does not mutate snapshots or influence branch generation.

## Unity Commands

```text
Hollow/Generation/Generate Milestone 39 Assets
Hollow/Validation/Run Milestone 39 Validation
```

## Non-Goals

- No new story-choice UI.
- No permanent lore/progression state.
- No changes to combat, rewards, shops, branch topology, room designer, save/load, or ArtPass gameplay authority.
