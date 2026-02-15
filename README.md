# Ghost Hustlers - AR Ghost-Catching Game

Cross-platform AR ghost-catching game built with Unity 6 + AR Foundation. Hold the screen to fire a proton beam at ghosts hovering in your physical space.

## Project Structure

```
ghost_hustlers/
├── unity/GhostHustlers/       # Unity 6 project (C#, AR Foundation, URP)
│   ├── Assets/Scripts/        # Game logic
│   │   ├── GameManager.cs     # Game flow, input, beam hit detection
│   │   ├── Ghost.cs           # Ghost entity (health, animations, capture)
│   │   ├── ProtonBeam.cs      # Beam rendering with pulse animation
│   │   ├── UIManager.cs       # HUD overlay (status, crosshair, respawn)
│   │   ├── ARPlaneController.cs # Plane detection, ghost placement
│   │   └── Editor/
│   │       ├── BuildScript.cs        # CLI build (iOS + Android)
│   │       ├── ProjectConfigurator.cs # URP, XR, player settings
│   │       └── SceneSetup.cs         # Editor scene setup utility
│   ├── Assets/Models/         # 3D assets (ghost.glb)
│   ├── Assets/Prefabs/        # Ghost prefab (created in editor)
│   ├── Assets/Scenes/         # AR scene (created in editor)
│   └── UNITY_SETUP.md         # Step-by-step editor setup guide
└── models/                    # Canonical 3D source assets
    ├── ghost.glb              # Universal GLB format
    └── ghost.usdz             # Apple USDZ format
```

## Quick Start

### Requirements
- Unity 6 LTS (6000.0+) via Unity Hub
- iOS: iPhone with A9+ chip, iOS 14+
- Android: ARCore-compatible device, API 29+ (Android 10+)

### Build & Run (Editor)
1. Open `unity/GhostHustlers/` in Unity 6
2. Wait for package import (AR Foundation, URP, etc.)
3. Run **Ghost Hustlers → Setup Scene** from the menu bar
4. Run **Ghost Hustlers → Create Ghost Prefab**
5. Configure XR settings (see `UNITY_SETUP.md` for details)
6. Connect a physical device and Build & Run

### Build & Run (CLI)
```bash
# Android
Unity -batchmode -projectPath unity/GhostHustlers -buildTarget Android \
  -executeMethod BuildScript.BuildAndroid -quit
adb install -r unity/GhostHustlers/Builds/GhostHustlers.apk

# iOS (requires configure step on first build)
Unity -batchmode -projectPath unity/GhostHustlers -buildTarget iOS \
  -executeMethod ProjectConfigurator.ConfigureProject -quit
Unity -batchmode -projectPath unity/GhostHustlers -buildTarget iOS \
  -executeMethod BuildScript.BuildiOS -quit
```

### Gameplay
1. Point your camera at a flat surface (table, floor)
2. Ghost auto-places on the first detected horizontal plane (>0.3m)
3. Hold the screen to fire the proton beam
4. Aim the crosshair at the ghost — it shakes, health bar drains (~4 seconds)
5. Ghost captured! Shrinks and fades away
6. Tap "Spawn New Ghost" to play again

## 3D Models

The `models/` directory contains placeholder ghost models:
- `ghost.glb` — Universal GLB format (imported by Unity)
- `ghost.usdz` — Apple USDZ format (legacy iOS only)

These are minimal cone shapes (~40cm tall). Replace with higher-fidelity models (<10k triangles).

## Architecture

### Core Scripts

| Script | Lines | Purpose |
|---|---|---|
| `GameManager.cs` | ~240 | State machine (Scanning→GhostPlaced→Capturing→Captured), input, beam hit detection, respawn |
| `Ghost.cs` | ~220 | Health (1.0→0.0 over 4s), hover animation (±5cm sine, 1.5s), rotation (360°/8s), shake, health bar (green→red), capture (shrink+fade 0.5s) |
| `ProtonBeam.cs` | ~90 | Dynamic cylinder from camera to target, gold material (RGBA 1.0/0.85/0.2/0.7), 6Hz pulse |
| `UIManager.cs` | ~210 | Screen-space canvas: status text, crosshair (circle + cross), respawn button, hint text |
| `ARPlaneController.cs` | ~130 | AR Foundation plane events, filtering, raycast placement, respawn re-detection |

### Hit Detection
Beam hits if `aimAngle < atan2(ghostRadius, distance) && distance < 10m`, matching the original iOS implementation. The ghost radius is 0.25m, providing a forgiving but not trivial target.

## Platform Notes

### Android
- Uses **OpenGLES3** (not Vulkan) — ARCore camera background rendering with Vulkan requires an additional `ARCommandBufferSupportRendererFeature` in the URP renderer. GLES3 works with the standard `ARBackgroundRendererFeature` and is battle-tested across major AR titles.
- Min SDK **API 29** (Android 10+).
- Materials created at runtime via `Shader.Find("Universal Render Pipeline/Lit")` in opaque mode. Transparent URP shader variants get stripped from the build unless a Material asset explicitly references them.
- Tested on Pixel 4 (Android 13).

### iOS
- Uses ARKit XR Plugin with batch mode workarounds (see `CLAUDE.md` for details on the `UNITY_XR_ARKIT_LOADER_ENABLED` scripting define issue).
- Requires `iOSPostBuildProcessor` for Swift runtime linking.

