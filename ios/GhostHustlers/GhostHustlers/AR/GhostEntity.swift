import RealityKit
import UIKit

class GhostEntity {
    let entity: ModelEntity
    private var hoverTimer: Timer?
    private var elapsed: Float = 0

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
    }

    func startHoverAnimation() {
        elapsed = 0
        hoverTimer = Timer.scheduledTimer(withTimeInterval: 1.0 / 60.0, repeats: true) { [weak self] _ in
            guard let self = self else { return }
            self.elapsed += 1.0 / 60.0

            // Sine wave hover: ±5cm amplitude, 1.5s period
            let hoverY = 0.05 * sin(self.elapsed * 2 * .pi / 1.5)

            // Slow rotation: full turn every 8 seconds
            let rotationAngle = self.elapsed * 2 * .pi / 8.0

            self.entity.transform.translation.y = Float(hoverY)
            self.entity.transform.rotation = simd_quatf(
                angle: Float(rotationAngle),
                axis: SIMD3<Float>(0, 1, 0)
            )
        }
    }

    func stopHoverAnimation() {
        hoverTimer?.invalidate()
        hoverTimer = nil
    }
}
