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
- `models/` — Shared source assets (ghost.glb)

## Unity project

### Setup
- Unity 6 LTS, Universal Render Pipeline (URP)
- AR Foundation 6.0 + ARCore XR Plugin + ARKit XR Plugin
- ARCore Extensions (for Cloud Anchors)
- Build targets: iOS 14+, Android API 29+

### Key files
- `Assets/Scripts/GameManager.cs` — Game flow, input, AR session management
- `Assets/Scripts/Ghost.cs` — Ghost behavior, health, animations, capture
- `Assets/Scripts/ProtonBeam.cs` — Beam rendering, positioning, pulse animation
- `Assets/Scripts/UIManager.cs` — HUD overlay (status, crosshair, respawn)
- `Assets/Scripts/ARPlaneController.cs` — Plane detection, ghost placement
- `Assets/Scripts/Editor/ProjectConfigurator.cs` — Idempotent project setup (URP, XR, player settings, scripting defines)
- `Assets/Scripts/Editor/BuildScript.cs` — CLI build entry points with ARKit batch mode workarounds
- `Assets/Scripts/Editor/iOSPostBuildProcessor.cs` — Xcode project post-processing (Swift support)

### Build (CLI — batch mode)
```bash
# Step 1: Configure project (sets scripting defines, must run before first build)
Unity -batchmode -projectPath unity/GhostHustlers -buildTarget iOS \
  -executeMethod ProjectConfigurator.ConfigureProject -quit

# Step 2: Build iOS
Unity -batchmode -projectPath unity/GhostHustlers -buildTarget iOS \
  -executeMethod BuildScript.BuildiOS -quit

# Step 3: Xcode archive + deploy
xcodebuild -project Builds/iOS/Unity-iPhone.xcodeproj -scheme Unity-iPhone \
  -destination 'generic/platform=iOS' -configuration Release \
  DEVELOPMENT_TEAM=SPL85G447K archive -archivePath /tmp/GhostHustlers.xcarchive
xcrun devicectl device install app --device <DEVICE_ID> \
  /tmp/GhostHustlers.xcarchive/Products/Applications/GhostHustlers.app
```

```bash
# Build Android
Unity -batchmode -projectPath unity/GhostHustlers -buildTarget Android \
  -executeMethod BuildScript.BuildAndroid -quit

# Deploy to connected device
adb install -r unity/GhostHustlers/Builds/GhostHustlers.apk
```

Step 1 (iOS configure) only needs to run once (or after a clean checkout). It
persists `UNITY_XR_ARKIT_LOADER_ENABLED` to ProjectSettings.asset so subsequent
builds compile ARKit C# with real DllImport calls.

### ARKit batch mode build bug

`ARKitBuildProcessor.LoaderEnabledCheck` (in the ARKit XR Plugin package) has
`if (Application.isBatchMode) return;` in its static constructor, which skips
`UpdateARKitDefines()`. This causes three cascading failures when building from
the command line:

1. **`loaderEnabled` stays false** — `ShouldIncludeRuntimePluginsInBuild()` returns
   false for all native plugins, so `libUnityARKit.a` is excluded from the build.
   Fix: `BuildScript.ForceARKitLoaderEnabled()` uses reflection to set the field.

2. **`UNITY_XR_ARKIT_LOADER_ENABLED` define never added** — All ARKit C# compiles
   with stub implementations (`AtLeast11_0() => false`, no `DllImport` calls).
   `RegisterDescriptor()` exits early, no subsystem descriptors are registered,
   and `ARKitLoader.Initialize()` fails with "Failed to load session subsystem."
   Fix: `ProjectConfigurator.ConfigureScriptingDefines()` adds the define to
   PlayerSettings before scripts compile. `BuildScript.EnsureARKitScriptingDefine()`
   also adds it as a belt-and-suspenders measure.

3. **Swift linker failure** — `libUnityARKit.a` contains Swift code
   (`RoomCaptureSessionWrapper.o`) that references `swiftCompatibility*` symbols.
   Without `SWIFT_VERSION` set and a `.swift` source file in the target, Xcode
   won't invoke `swiftc` and the Swift runtime won't be linked.
   Fix: `iOSPostBuildProcessor` adds a dummy `SwiftBridge.swift` file to the
   UnityFramework target and sets `SWIFT_VERSION=5.0`.

### Android build settings

`BuildScript.BuildAndroid()` configures:

- **Graphics API: OpenGLES3** (not Vulkan) — Vulkan requires
  `ARCommandBufferSupportRendererFeature` in the URP renderer for ARCore camera
  background rendering. Without it, every frame throws
  `InvalidOperationException: ARCommandBufferSupportRendererFeature must be added`.
  OpenGLES3 works with just `ARBackgroundRendererFeature` and is what most
  shipping AR games use (Pokemon GO, etc.).
- **Min SDK: API 29** (Android 10) — required by ARCore when Vulkan is a
  potential graphics API. Even with GLES3 forced, the ARCore plugin validates
  this at build time.
- **Target architecture: ARM64** — all modern Android AR devices.

### AR-safe player settings

These settings are required/recommended for AR Foundation (applied by
`ProjectConfigurator`):

- **Color space: Linear** — required by URP
- **Multithreaded rendering: OFF** (iOS + Android) — causes black screen with AR Foundation
- **ARBackgroundRendererFeature** in URP renderer — required for camera feed render pass
- **Depth/Opaque texture: OFF** — known iOS AR black screen cause
- **MSAA: 1 (disabled)** — not needed for AR, saves GPU
- **Render scale: 1.0** — must match device resolution for AR
- **URP pipeline assigned to all quality levels** — prevents quality level mismatch

### Runtime material creation (Shader.Find pitfall)

Ghost, ProtonBeam, and health bar materials are created at runtime via
`Shader.Find("Universal Render Pipeline/Lit")`. This works but has caveats:

- **Transparent variants get stripped**: If no Material asset in the project
  explicitly uses URP/Lit in transparent mode, Unity's shader stripping removes
  those variants from the build. Setting `_Surface=1` and enabling
  `_SURFACE_TYPE_TRANSPARENT` at runtime has no effect if the compiled shader
  doesn't include the transparent pass. Ghost currently uses **opaque** mode to
  avoid this.
- **Always check for null**: `Shader.Find` returns null if the shader was
  stripped. Ghost.cs and ProtonBeam.cs include fallback chains
  (URP/Lit → Simple Lit → Unlit → Sprites/Default).
- **URP/Unlit is stripped** from the current build (no asset references it).
  Only URP/Lit and Sprites/Default are confirmed available at runtime.
- To restore transparency: create a Material asset in the editor with URP/Lit
  set to transparent mode and assign it to the ghost prefab. This forces Unity
  to include the transparent shader variants in the build.

## Bundle ID

`co.enspyr.ghosthustlers` — set by `BuildScript` for both iOS and Android at
build time via `PlayerSettings.SetApplicationIdentifier()`.

## Gameplay design

1. Ghost auto-places on first detected horizontal surface (extent > 0.1m)
2. Hold screen to fire beam from camera toward screen center
3. Beam hits ghost (angular check: angle < atan2(ghostRadius, distance) && distance < 10m): ghost shakes, health bar drains over ~4s
4. Health reaches 0: ghost shrinks+fades over 0.5s, "Ghost captured!" shown
5. "Spawn New Ghost" button resets, waits for plane detection, places a new ghost
6. No tap-to-place — ghost only appears via automatic plane detection

### Animation specs
- Hover: sine wave Y offset, +-5cm amplitude, 1.5s period
- Rotation: 360deg/8s around Y axis
- Ghost material: opaque blue (RGBA: 0.3, 0.7, 1.0, 1.0) — was semi-transparent but transparent URP shader variants get stripped in device builds (see "Runtime material creation" section)
- Beam material: yellow/gold (RGBA: 1.0, 0.85, 0.2, 0.7)
- Health bar: green->yellow->red gradient, positioned Y+0.3m above ghost

## Current status (Feb 2026)

### What's working
- Complete single-player gameplay loop on both platforms (scan → place → fire → capture → respawn)
- AR plane detection and ghost auto-placement (iOS ARKit + Android ARCore)
- Proton beam with pulse animation and angular hit detection
- Ghost hover/rotation animation, damage shake, health bar, capture shrink+fade
- Procedural UI (status text, crosshair, hint text, respawn button)
- CLI build pipeline for both platforms (batch mode, no editor required)
- Shader fallback chain with null guards (`ShaderUtils.FindURPShader()`)

### What's placeholder
- **Ghost model**: Low-poly cone shape (`models/ghost.glb`, ~40cm tall). Needs a proper ghost mesh (<10k triangles) from Blender/Sketchfab.
- **No Material assets**: All materials created at runtime via `Shader.Find`. Transparent URP shader variants are stripped — ghost is opaque, health bar transparency may silently fail. Fix: create Material assets in the editor to force variant inclusion.
- **No audio**: No sound effects or music.
- **No particles**: No beam impact particles, ghost spawn/capture VFX.
- **Company name**: Still `DefaultCompany` in PlayerSettings (cosmetic, doesn't affect builds since bundle ID is set in BuildScript).

### Known issues
- iOS bundle ID was previously `com.DefaultCompany.GhostHustlers` (old installs may conflict — uninstall first)
- Android had an old `com.ghosthustlers.app` install (same — uninstall old version)

## Roadmap: iOS

### Phase 1 — Polish (current)
- [x] AR plane detection and ghost placement
- [x] Beam firing with hit detection
- [x] Health system and capture animation
- [x] CLI build with ARKit batch mode workarounds
- [x] Xcode archive and device deployment
- [ ] Replace placeholder ghost model with proper 3D asset
- [ ] Add transparent Material assets in editor to restore ghost/beam/health bar transparency
- [ ] Add sound effects (beam fire, ghost hit, capture)
- [ ] Add particle effects (beam impact sparks, ghost capture burst)
- [ ] Haptic feedback on beam hit (iOS taptic engine)

### Phase 2 — Game feel
- [ ] Score counter and capture streak
- [ ] Ghost difficulty variants (faster movement, smaller hit radius, more health)
- [ ] Screen shake on capture
- [ ] Ghost spawn animation (fade in / materialize)
- [ ] Tutorial overlay for first-time players

### Phase 3 — Multiplayer
- [ ] Cloud Anchors via ARCore Extensions for AR Foundation (cross-platform spatial alignment)
- [ ] Real-time state sync via Photon Fusion or Unity Netcode for GameObjects
- [ ] Shared ghost state: who's damaging which ghost, damage attribution
- [ ] Player avatars / indicators visible in AR
- [ ] Score/leaderboard per session
- [ ] Lobby/matchmaking/discovery (nearby players)
- [ ] Niantic Lightship VPS as alternative for location-based colocalization

### Phase 4 — Distribution
- [ ] App Store submission (TestFlight first)
- [ ] App icon and launch screen
- [ ] Privacy manifest (camera usage, AR data)

## Roadmap: Android

### Phase 1 — Polish (current)
- [x] AR plane detection and ghost placement
- [x] Beam firing with hit detection
- [x] Health system and capture animation
- [x] OpenGLES3 (not Vulkan) for ARCore compatibility
- [x] CLI build and ADB deployment
- [x] Faster plane detection (0.1m threshold)
- [x] No tap-to-place (auto-placement only)
- [ ] Replace placeholder ghost model (same asset as iOS)
- [ ] Add transparent Material assets (same as iOS)
- [ ] Add sound effects
- [ ] Add particle effects
- [ ] Haptic feedback on beam hit (Android vibration API)

### Phase 2 — Game feel
- [ ] Same as iOS (shared codebase — all changes apply to both platforms)

### Phase 3 — Multiplayer
- [ ] Same as iOS (AR Foundation + Cloud Anchors work cross-platform)
- [ ] Cross-platform play: iOS and Android players in the same session

### Phase 4 — Distribution
- [ ] Google Play submission (internal testing track first)
- [ ] App icon and feature graphic
- [ ] Play Store listing (screenshots, description)
- [ ] Target SDK compliance (currently API 29 min, needs to meet Play Store requirements)
