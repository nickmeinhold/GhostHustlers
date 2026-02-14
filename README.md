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
│   │       └── SceneSetup.cs  # Editor utility for scene setup
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
- Android: ARCore-compatible device, API 24+

### Build & Run
1. Open `unity/GhostHustlers/` in Unity 6
2. Wait for package import (AR Foundation, URP, etc.)
3. Run **Ghost Hustlers → Setup Scene** from the menu bar
4. Run **Ghost Hustlers → Create Ghost Prefab**
5. Configure XR settings (see `UNITY_SETUP.md` for details)
6. Connect a physical device and Build & Run

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

