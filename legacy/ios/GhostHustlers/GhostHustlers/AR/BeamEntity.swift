import RealityKit
import UIKit

class BeamEntity {
    let entity: ModelEntity
    private var pulseTimer: Timer?
    private var pulseElapsed: Float = 0
    private let baseRadius: Float = 0.02

    init() {
        let mesh = MeshResource.generateCylinder(height: 1.0, radius: baseRadius)
        var material = SimpleMaterial()
        material.color = .init(
            tint: UIColor(red: 1.0, green: 0.85, blue: 0.2, alpha: 0.7)
        )
        material.metallic = .float(0.8)
        material.roughness = .float(0.1)
        entity = ModelEntity(mesh: mesh, materials: [material])
    }

    func update(from start: SIMD3<Float>, to end: SIMD3<Float>) {
        let direction = end - start
        let distance = simd_length(direction)
        guard distance > 0.001 else { return }

        // Regenerate cylinder mesh to match beam length
        let radius = baseRadius + 0.005 * sin(pulseElapsed * 6.0)
        entity.model?.mesh = MeshResource.generateCylinder(height: distance, radius: radius)

        // Position at midpoint
        entity.position = (start + end) / 2.0

        // Rotate cylinder (Y-up by default) to align with beam direction
        let normalizedDir = simd_normalize(direction)
        let yAxis = SIMD3<Float>(0, 1, 0)
        let dot = simd_dot(yAxis, normalizedDir)

        if abs(dot + 1.0) < 0.001 {
            // Direction is directly downward — rotate 180 degrees around X
            entity.orientation = simd_quatf(angle: .pi, axis: SIMD3<Float>(1, 0, 0))
        } else if abs(dot - 1.0) < 0.001 {
            // Direction is directly upward — no rotation needed
            entity.orientation = simd_quatf(ix: 0, iy: 0, iz: 0, r: 1)
        } else {
            let cross = simd_cross(yAxis, normalizedDir)
            let crossLen = simd_length(cross)
            let angle = atan2(crossLen, dot)
            entity.orientation = simd_quatf(angle: angle, axis: simd_normalize(cross))
        }
    }

    func startPulse() {
        pulseElapsed = 0
        pulseTimer = Timer.scheduledTimer(withTimeInterval: 1.0 / 60.0, repeats: true) { [weak self] _ in
            self?.pulseElapsed += 1.0 / 60.0
        }
    }

    func stopPulse() {
        pulseTimer?.invalidate()
        pulseTimer = nil
    }
}
