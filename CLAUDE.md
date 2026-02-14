# Ghost Hustlers

Cross-platform AR ghost-catching game (Ghostbusters-style proton beam mechanic).

## Project structure

- `ios/GhostHustlers/` — Xcode project (Swift, RealityKit, ARKit)
- `android/GhostHustlers/` — Gradle project (Kotlin, Jetpack Compose, SceneView/ARCore)
- `models/` — Shared source assets (ghost.glb, ghost.usdz)

## iOS (Swift/RealityKit)

### Build
- Open `ios/GhostHustlers/GhostHustlers.xcodeproj` in Xcode
- Deployment target: iOS 18.0 (needed for `MeshResource.generateCylinder`)
- Must run on physical device (AR requires camera)

### Key files
- `AR/ARViewContainer.swift` — AR session, gesture handling, per-frame beam update, respawn logic
- `AR/GhostEntity.swift` — Ghost model, health, shake, health bar, capture animation
- `AR/BeamEntity.swift` — Beam cylinder mesh, positioning, pulse animation
- `ContentView.swift` — SwiftUI overlay (status, crosshair, respawn button)

### Architecture
- `UIViewRepresentable` wrapping `ARView` with a `Coordinator` as `ARSessionDelegate`
- `UILongPressGestureRecognizer` (minimumPressDuration=0) for instant touch-down beam firing
- Per-frame update via `session(_:didUpdate frame:)` for beam positioning and hit detection
- Angular threshold check (not raycasting) for beam-to-ghost hit detection

### Status
- Full gameplay loop: place ghost → fire beam → drain health → capture → respawn
- Known issue: beam reliability after respawn needs more testing

## Android (Kotlin/SceneView)

### Build
```bash
cd android/GhostHustlers
ANDROID_HOME=~/Library/Android/sdk ./gradlew installDebug
```

### Key files
- `ui/ARScreen.kt` — Compose AR screen, plane detection, ghost placement
- `ar/GhostNode.kt` — Ghost model loading, hover animation

### Dependencies
- AGP 8.9.1, Kotlin 2.2.21, Gradle 8.11.1, compileSdk 36
- arsceneview 2.3.3 (SceneView AR library)

### Status
- Base AR working: ghost auto-places on surface with hover + rotation
- No beam/health/capture gameplay yet (iOS-only for now)

## Gameplay design

1. Ghost auto-places on first detected horizontal surface
2. Hold screen to fire beam from camera toward screen center
3. Beam hits ghost (angular check): ghost shakes, health bar drains over ~4s
4. Health reaches 0: ghost shrinks+fades, "Ghost captured!" shown
5. "Spawn New Ghost" button resets and places a new ghost
