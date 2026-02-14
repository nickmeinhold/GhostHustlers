# Ghost Hustlers

Cross-platform AR ghost-catching game (Ghostbusters-style proton beam mechanic).

## Platform decision (Feb 2025)

Originally built as two separate native codebases (iOS: Swift/RealityKit, Android: Kotlin/SceneView). Researched cross-platform options for the multiplayer/Cloud Anchors phase:

- **Flutter + AR plugins**: Not viable. AR plugin ecosystem is community-maintained, inconsistent, sub-30 FPS. Flutter is a UI framework, not a game engine. No one ships AR games on Flutter.
- **Kotlin Multiplatform (KMP)**: Shares business logic only, not rendering/physics/AR. You'd still write the AR layer twice. Kotlin Slack community advises against it for AR.
- **Godot**: AR support is immature, no major AR game shipped on it. Mobile AR plugins are basic.
- **Unreal Engine**: Can work (The Machines used UE4) but builds are heavier, mobile AR optimization is harder, ecosystem is Unity-centric.
- **Unity + AR Foundation**: Every major AR multiplayer game (Pokemon GO, Monster Hunter Now, Pikmin Bloom, Ghostbusters World) is built on Unity. Google officially recommends AR Foundation for ARCore. Provides physics, particles, spatial audio, animation, multiplayer networking out of the box. 10-20% performance overhead vs native is acceptable.

**Decision: Convert to Unity 6 + AR Foundation 6.0.** Port iOS gameplay (the complete implementation) into a single cross-platform codebase.

## Project structure

- `unity/GhostHustlers/` — Unity 6 project (C#, AR Foundation, URP)
- `legacy/ios/` — Original iOS implementation (Swift/RealityKit) — reference only
- `legacy/android/` — Original Android implementation (Kotlin/SceneView) — reference only
- `models/` — Shared source assets (ghost.glb)

## Unity project

### Setup
- Unity 6 LTS, Universal Render Pipeline (URP)
- AR Foundation 6.0 + ARCore XR Plugin + ARKit XR Plugin
- ARCore Extensions (for Cloud Anchors)
- Build targets: iOS 14+, Android API 24+

### Key files
- `Assets/Scripts/GameManager.cs` — Game flow, input, AR session management
- `Assets/Scripts/Ghost.cs` — Ghost behavior, health, animations, capture
- `Assets/Scripts/ProtonBeam.cs` — Beam rendering, positioning, pulse animation
- `Assets/Scripts/UIManager.cs` — HUD overlay (status, crosshair, respawn)
- `Assets/Scripts/ARPlaneController.cs` — Plane detection, ghost placement

### Build
- Open `unity/GhostHustlers/` in Unity 6
- Switch platform to iOS or Android in Build Settings
- Build and run on physical device (AR requires camera)

## Gameplay design

1. Ghost auto-places on first detected horizontal surface (extent > 0.3m)
2. Hold screen to fire beam from camera toward screen center
3. Beam hits ghost (angular check: angle < atan2(ghostRadius, distance) && distance < 10m): ghost shakes, health bar drains over ~4s
4. Health reaches 0: ghost shrinks+fades over 0.5s, "Ghost captured!" shown
5. "Spawn New Ghost" button resets and places a new ghost

### Animation specs
- Hover: sine wave Y offset, +-5cm amplitude, 1.5s period
- Rotation: 360deg/8s around Y axis
- Ghost material: semi-transparent blue (alpha 0.6)
- Beam material: yellow/gold (RGBA: 1.0, 0.85, 0.2, 0.7)
- Health bar: green->yellow->red gradient, positioned Y+0.3m above ghost

## Future (multiplayer)

- Cloud Anchors via ARCore Extensions for AR Foundation (cross-platform spatial alignment)
- Real-time state sync via Photon Fusion or Unity Netcode for GameObjects
- Niantic Lightship VPS as alternative/complement for location-based colocalization
- Game logic: who's damaging which ghost, scores, player positions
- Lobby/matchmaking/discovery
