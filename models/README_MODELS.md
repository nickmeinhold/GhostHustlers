# Ghost 3D Models

## ghost.glb
Placeholder low-poly cone ghost shape. Replace with a proper ghost model from
Sketchfab or Blender (<10k triangles, ~40cm tall).

## ghost.usdz
Generate from ghost.glb using Apple's Reality Converter:
1. Download Reality Converter from https://developer.apple.com/augmented-reality/tools/
2. Open ghost.glb
3. Export as ghost.usdz

Or use the command-line tool:
```
xcrun usdzconvert ghost.glb ghost.usdz
```

The iOS app will load ghost.usdz from the Xcode bundle.
The Android app loads ghost.glb from assets/models/.
