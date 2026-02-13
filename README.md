# Ghost Hustlers - AR Proof of Concept

Render a 3D ghost floating on a detected horizontal surface in AR, on both iOS and Android.

## Project Structure

```
ghost_hustlers/
├── ios/GhostHustlers/       # Swift + ARKit/RealityKit (iOS 17+)
├── android/GhostHustlers/   # Kotlin + ARCore + SceneView (Min SDK 24)
└── models/                  # Canonical 3D assets (GLB + USDZ)
```

## iOS

**Requirements**: Xcode 15+, physical iPhone/iPad with ARKit support (A9+ chip), iOS 17+

**Build & Run**:
1. Open `ios/GhostHustlers/GhostHustlers.xcodeproj` in Xcode
2. Set your development team in Signing & Capabilities
3. Connect a physical iOS device (AR doesn't work in Simulator)
4. Build and run (Cmd+R)

**What to expect**: Point the camera at a flat surface (table, floor). The app will auto-place a ghost on the first horizontal plane detected (>0.3m). You can also tap to place the ghost manually. The ghost hovers with a sine-wave animation and slowly rotates.

## Android

**Requirements**: Android Studio Hedgehog+, physical ARCore-supported Android device, Min SDK 24

**Build & Run**:
1. Open `android/GhostHustlers/` in Android Studio
2. Wait for Gradle sync to complete (downloads SceneView/ARCore dependencies)
3. Connect a physical Android device with ARCore support
4. Build and run

**What to expect**: Same as iOS - point at a flat surface, ghost auto-appears on the first detected horizontal plane. Tap to place additional ghosts.

## 3D Models

The `models/` directory contains placeholder ghost models:
- `ghost.glb` - Universal format (used by Android)
- `ghost.usdz` - Apple format (used by iOS)

These are minimal cone shapes. Replace with a proper ghost model (<10k triangles, ~40cm tall).
See `models/README_MODELS.md` for conversion instructions.

## Architecture

### iOS (ARKit + RealityKit)
- **ARViewContainer**: `UIViewRepresentable` wrapping `ARView` with horizontal plane detection
- **GhostEntity**: Loads USDZ model, applies transparent material (alpha 0.6), hover animation via Timer

### Android (ARCore + SceneView)
- **ARScreen**: Jetpack Compose screen using `ARScene` composable with horizontal plane finding
- **GhostNode**: Loads GLB model via SceneView's `ModelLoader`, transparent Filament material, coroutine-driven animation

## Not In Scope (Milestone 1)

Backend, map view, GPS/location spawning, catching mechanic, multiplayer, auth, sound, CI/CD, app store distribution.
