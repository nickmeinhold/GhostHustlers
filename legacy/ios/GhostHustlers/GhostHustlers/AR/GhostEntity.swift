import RealityKit
import UIKit

class GhostEntity {
    let entity: ModelEntity
    private var hoverTimer: Timer?
    private var elapsed: Float = 0

    // Health & combat state
    var health: Float = 1.0
    var isShaking: Bool = false
    private var healthBarFill: ModelEntity?
    private var healthBarBackground: ModelEntity?
    private let healthBarWidth: Float = 0.15

    init() throws {
        // Try loading the USDZ model from the bundle
        if let modelURL = Bundle.main.url(forResource: "ghost", withExtension: "usdz") {
            entity = try ModelEntity.loadModel(contentsOf: modelURL)
        } else {
            // Fallback: create a simple ghost shape procedurally (sphere)
            let bodyMesh = MeshResource.generateSphere(radius: 0.2)
            entity = ModelEntity(mesh: bodyMesh)
        }

        // Apply semi-transparent ghost material
        var material = SimpleMaterial()
        material.color = .init(
            tint: UIColor(red: 0.75, green: 0.88, blue: 1.0, alpha: 0.8)
        )
        material.metallic = .float(0.0)
        material.roughness = .float(0.8)
        entity.model?.materials = [material]

        // Scale to ~40cm tall
        entity.scale = SIMD3<Float>(repeating: 1.0)

        // Generate collision shape for tap interactions
        entity.generateCollisionShapes(recursive: true)

        // Create health bar
        setupHealthBar()
    }

    private func setupHealthBar() {
        // Background (dark bar)
        let bgMesh = MeshResource.generateBox(width: healthBarWidth, height: 0.01, depth: 0.02)
        var bgMaterial = SimpleMaterial()
        bgMaterial.color = .init(tint: UIColor(red: 0.2, green: 0.2, blue: 0.2, alpha: 0.8))
        bgMaterial.roughness = .float(0.9)
        let bg = ModelEntity(mesh: bgMesh, materials: [bgMaterial])
        bg.position = SIMD3<Float>(0, 0.3, 0)
        entity.addChild(bg)
        healthBarBackground = bg

        // Fill (green bar)
        let fillMesh = MeshResource.generateBox(width: healthBarWidth, height: 0.012, depth: 0.022)
        var fillMaterial = SimpleMaterial()
        fillMaterial.color = .init(tint: .green)
        fillMaterial.roughness = .float(0.9)
        let fill = ModelEntity(mesh: fillMesh, materials: [fillMaterial])
        fill.position = SIMD3<Float>(0, 0.3, 0)
        entity.addChild(fill)
        healthBarFill = fill

        // Initially hidden until ghost is being attacked
        bg.isEnabled = false
        fill.isEnabled = false
    }

    func showHealthBar() {
        healthBarBackground?.isEnabled = true
        healthBarFill?.isEnabled = true
    }

    func hideHealthBar() {
        healthBarBackground?.isEnabled = false
        healthBarFill?.isEnabled = false
    }

    func updateHealthBar() {
        guard let fill = healthBarFill else { return }

        // Scale X by health (anchored to left by adjusting position)
        let clampedHealth = max(0, min(1, health))
        fill.scale.x = clampedHealth
        fill.position.x = -healthBarWidth * (1.0 - clampedHealth) / 2.0

        // Color: green → yellow → red
        let color: UIColor
        if clampedHealth > 0.5 {
            let t = (clampedHealth - 0.5) * 2.0 // 1.0 at full, 0.0 at half
            color = UIColor(
                red: CGFloat(1.0 - t),
                green: CGFloat(0.5 + 0.5 * t),
                blue: 0,
                alpha: 0.9
            )
        } else {
            let t = clampedHealth * 2.0 // 1.0 at half, 0.0 at empty
            color = UIColor(
                red: 1.0,
                green: CGFloat(t * 0.8),
                blue: 0,
                alpha: 0.9
            )
        }
        var mat = SimpleMaterial()
        mat.color = .init(tint: color)
        mat.roughness = .float(0.9)
        fill.model?.materials = [mat]
    }

    func takeDamage(_ deltaTime: Float) {
        health -= deltaTime / 4.0 // 4 seconds to capture
        health = max(0, health)
        updateHealthBar()
    }

    func startHoverAnimation() {
        elapsed = 0
        hoverTimer = Timer.scheduledTimer(withTimeInterval: 1.0 / 60.0, repeats: true) { [weak self] _ in
            guard let self = self else { return }
            self.elapsed += 1.0 / 60.0

            // Sine wave hover: +/-5cm amplitude, 1.5s period
            let hoverY = 0.05 * sin(self.elapsed * 2 * .pi / 1.5)

            // Slow rotation: full turn every 8 seconds
            let rotationAngle = self.elapsed * 2 * .pi / 8.0

            self.entity.transform.translation.y = Float(hoverY)
            self.entity.transform.rotation = simd_quatf(
                angle: Float(rotationAngle),
                axis: SIMD3<Float>(0, 1, 0)
            )

            // Shake jitter when being hit by beam
            if self.isShaking {
                let jitterX = Float.random(in: -0.01...0.01)
                let jitterZ = Float.random(in: -0.01...0.01)
                self.entity.transform.translation.x = jitterX
                self.entity.transform.translation.z = jitterZ
            } else {
                self.entity.transform.translation.x = 0
                self.entity.transform.translation.z = 0
            }

            // Make health bar always face camera (billboard) — approximate via countering ghost rotation
            let counterRotation = simd_quatf(
                angle: -Float(rotationAngle),
                axis: SIMD3<Float>(0, 1, 0)
            )
            self.healthBarBackground?.orientation = counterRotation
            self.healthBarFill?.orientation = counterRotation
        }
    }

    func stopHoverAnimation() {
        hoverTimer?.invalidate()
        hoverTimer = nil
    }

    func playCaptureAnimation(completion: @escaping () -> Void) {
        stopHoverAnimation()
        isShaking = false

        // Animate shrink + fade over 0.5s
        var captureElapsed: Float = 0
        let duration: Float = 0.5
        let originalScale = entity.scale

        Timer.scheduledTimer(withTimeInterval: 1.0 / 60.0, repeats: true) { [weak self] timer in
            guard let self = self else {
                timer.invalidate()
                return
            }
            captureElapsed += 1.0 / 60.0
            let t = min(captureElapsed / duration, 1.0)

            // Shrink
            let scale = originalScale * (1.0 - t * 0.99)
            self.entity.scale = scale

            // Fade by adjusting material alpha
            var mat = SimpleMaterial()
            mat.color = .init(
                tint: UIColor(red: 0.75, green: 0.88, blue: 1.0, alpha: CGFloat(0.8 * (1.0 - t)))
            )
            mat.metallic = .float(0.0)
            mat.roughness = .float(0.8)
            self.entity.model?.materials = [mat]

            if t >= 1.0 {
                timer.invalidate()
                self.entity.removeFromParent()
                completion()
            }
        }
    }

    func reset() {
        health = 1.0
        isShaking = false
        entity.scale = SIMD3<Float>(repeating: 1.0)
        hideHealthBar()
        updateHealthBar()
    }
}
