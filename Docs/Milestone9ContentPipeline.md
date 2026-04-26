# Milestone 9: Content Pipeline, Materials, VFX, Audio, Addressables

Milestone 9 adds the first real content pipeline layer for Hollow Soul without changing gameplay behavior. The game still uses prototype primitives, but visual roles, cue definitions, and import validation are now centralized so future art, audio, VFX, and model drops can replace graybox content safely.

## Presentation Content

- `MaterialPaletteDefinition` maps stable `MaterialRole` values to prototype materials.
- `PresentationContentCatalog` is loaded from `Resources/Hollow/Presentation/PresentationContentCatalog` and references the active material palette, VFX cues, and audio cues.
- `MaterialResolver` is the runtime boundary for room, combat, reward, door, spawn, projectile, enemy, and room-designer materials.
- `VfxPresenter` and `AudioPresenter` provide safe cue playback. Missing audio clips no-op; generated VFX cues currently use debug primitives.
- `AddressableAssetLoader` is the future asset-loading boundary for Addressables content.

## Generated Prototype Content

Run `Hollow/Generation/Generate Milestone 9 Assets` to regenerate M1-M8 assets first, then create:

- prototype materials in `Assets/_Hollow/Art/Materials/Prototype`
- presentation data in `Assets/_Hollow/Data/Presentation`
- runtime-loaded catalog in `Assets/_Hollow/Resources/Hollow/Presentation`
- local Addressables group `Hollow Local Content`
- labels: `hollow.core`, `hollow.rooms`, `hollow.enemies`, `hollow.player`, `hollow.ui`, `hollow.audio`, `hollow.vfx`, `hollow.designer`, and `hollow.data`

## Validation

`ContentImportValidator` checks required palette/catalog assets, all material roles, all cue IDs, Addressables labels, key addressable entries, prefab renderer materials, and naming conventions.

Run `Hollow/Validation/Run Milestone 9 Validation` after generation. The EditMode suite includes material resolution, room-builder presentation, cue presenter safety, Addressables loader boundary, and content import validation tests.

## Scope

M9 is pipeline-only. It does not add final production art, real SFX/music, remote catalogs, procedural content, new combat behavior, or platform-specific polish. Those remain later work.
