# Milestone 1 - Menu, Profiles, And Platform Routing

Milestone 1 proves the shell flow before gameplay work begins:

`Boot -> MainMenu -> Profile Select -> Slot Main -> Platform Shell`

## Shared Runtime Logic

- `Hollow.Persistence` owns JSON profile slot storage.
- `Hollow.UI` owns menu state, profile cards, and launch commands.
- `Hollow.Platform` maps platform kind to route.
- `Hollow.Core` still owns app route state and scene loading.

The same menu/profile/routing logic is used for Windows, Vision Pro bounded tabletop, and Vision Pro immersive mode.

## Platform Differences

- Windows uses screen-space menu and shell UI.
- Vision Pro bounded and immersive routes use separate scene shells.
- Vision Pro UI can be switched to world-space presentation through `MainMenuPlatformPresenter` / shell scene configuration.
- Gameplay logic must not fork by platform. Only presentation, camera, scale, and input affordances should differ.

## Generated Assets

Run this from Unity or batchmode:

```bash
"/Applications/Unity/Hub/Editor/6000.4.1f1/Unity.app/Contents/MacOS/Unity" \
  -batchmode \
  -projectPath "/Users/martinjedrzejewski/Documents/GitHub/Unity/Hollow Soul - Unity" \
  -quit \
  -logFile /tmp/hollow-soul-unity-m1-generate.log \
  -executeMethod Hollow.Editor.Generation.Milestone1AssetGenerator.Generate
```

Then validate:

```bash
"/Applications/Unity/Hub/Editor/6000.4.1f1/Unity.app/Contents/MacOS/Unity" \
  -batchmode \
  -projectPath "/Users/martinjedrzejewski/Documents/GitHub/Unity/Hollow Soul - Unity" \
  -quit \
  -logFile /tmp/hollow-soul-unity-m1.log \
  -executeMethod Hollow.Editor.Validation.Milestone1Validator.Validate
```

