# Unity Project Setup Guide

## Prerequisites
- Unity 6 LTS (6000.0+) installed via Unity Hub
- iOS Build Support module (for iOS deployment)
- Android Build Support module (for Android deployment)

## Step 1: Open the Project

1. Open Unity Hub
2. Click "Add" → "Add project from disk"
3. Select `unity/GhostHustlers/`
4. Unity will import packages from `manifest.json` (AR Foundation, ARCore, ARKit, URP)
5. Wait for package resolution — this takes a few minutes on first open

## Step 2: Configure URP

1. If no URP pipeline asset exists:
   - Right-click in `Assets/` → Create → Rendering → URP Asset (with Universal Renderer)
   - Name it `URPAsset`
2. Go to Edit → Project Settings → Graphics
   - Set "Scriptable Render Pipeline Settings" to your URP Asset
3. Go to Edit → Project Settings → Quality
   - Set the default quality level's Render Pipeline Asset to the same URP Asset

## Step 3: Configure XR Plug-in Management

1. Go to Edit → Project Settings → XR Plug-in Management
2. Install XR Plug-in Management if prompted
3. **iOS tab**: Check "ARKit"
4. **Android tab**: Check "ARCore"

## Step 4: Set Up the Scene

### Option A: Automatic (recommended)

1. Create a new empty scene: File → New Scene → Empty
2. Save it to `Assets/Scenes/GhostHustlers.unity`
3. Go to menu: **Ghost Hustlers → Setup Scene**
   - This creates: AR Session, XR Origin with AR Camera, GameManager, UIManager, ARPlaneController
4. Go to menu: **Ghost Hustlers → Create Ghost Prefab**
   - This creates the ghost prefab from ghost.glb and assigns it to GameManager

### Option B: Manual

1. Create a new scene
2. Delete the default Main Camera and Directional Light
3. Add GameObjects:

**AR Session:**
- Create empty GameObject named "AR Session"
- Add components: `AR Session`, `AR Input Manager`

**XR Origin:**
- Create empty GameObject named "XR Origin"
- Add component: `XR Origin`
- Create child "Camera Offset" (empty)
- Create child of Camera Offset: "AR Camera"
  - Add: `Camera`, `AR Camera Manager`, `AR Camera Background`, `Tracked Pose Driver`
  - Tag as "MainCamera"
  - Set near clip: 0.1, far clip: 20
- Set XR Origin's Camera Floor Offset Object to Camera Offset
- Set XR Origin's Camera to AR Camera
- Add to XR Origin: `AR Plane Manager`, `AR Raycast Manager`

**GameManager:**
- Create empty "GameManager"
- Add component: `GameManager` (from Scripts/)
- Add component: `ARPlaneController` (from Scripts/)
- Wire references:
  - GameManager.arCamera → AR Camera
  - GameManager.planeController → self (ARPlaneController)
  - ARPlaneController.planeManager → XR Origin's AR Plane Manager
  - ARPlaneController.raycastManager → XR Origin's AR Raycast Manager

**UIManager:**
- Create empty "UIManager"
- Add component: `UIManager` (from Scripts/)
- Wire: UIManager.gameManager → GameManager
- Wire: GameManager.uiManager → UIManager

**Ghost Prefab:**
- Drag `Assets/Models/ghost.glb` into scene
- Add `Ghost` component to it
- Drag to `Assets/Prefabs/` to create prefab
- Delete from scene
- Assign prefab to `GameManager.ghostPrefab`

## Step 5: Configure Build Settings

### iOS
1. File → Build Settings → iOS
2. Click "Switch Platform"
3. Player Settings:
   - Other Settings → Target minimum iOS Version: `14.0`
   - Other Settings → Architecture: `ARM64`
   - Other Settings → Scripting Backend: `IL2CPP`
   - Other Settings → Camera Usage Description: "Required for AR"
   - Resolution → Requires ARKit support: checked

### Android
1. File → Build Settings → Android
2. Click "Switch Platform"
3. Player Settings:
   - Other Settings → Minimum API Level: `Android 7.0 (API level 24)`
   - Other Settings → Target API Level: `Automatic (highest installed)`
   - Other Settings → Scripting Backend: `IL2CPP`
   - Other Settings → Target Architectures: ARM64 checked, ARMv7 unchecked
   - Other Settings → Requires ARCore support: checked
   - XR Plug-in Management → ARCore: checked

## Step 6: Build and Run

1. Connect a physical device (AR requires a camera)
   - iOS: iPhone with A9+ chip, iOS 14+
   - Android: ARCore-compatible device, API 24+
2. File → Build and Run
3. Allow camera permission when prompted on device

## Troubleshooting

- **"No AR subsystem provider" error**: Ensure XR Plug-in Management has ARKit/ARCore checked for the target platform
- **Pink/magenta materials**: URP pipeline asset is not assigned. Check Edit → Project Settings → Graphics
- **Ghost not appearing**: Verify ghost prefab is assigned in GameManager inspector
- **TextMeshPro import prompt**: Click "Import TMP Essentials" when prompted
- **Camera feed not showing**: Ensure AR Camera has ARCameraBackground component and URP is configured
