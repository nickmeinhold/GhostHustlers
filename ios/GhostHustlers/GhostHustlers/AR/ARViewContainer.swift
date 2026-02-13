import SwiftUI
import ARKit
import RealityKit

struct ARViewContainer: UIViewRepresentable {
    @Binding var statusMessage: String
    @Binding var ghostPlaced: Bool

    func makeCoordinator() -> Coordinator {
        Coordinator(statusMessage: $statusMessage, ghostPlaced: $ghostPlaced)
    }

    func makeUIView(context: Context) -> ARView {
        let arView = ARView(frame: .zero)

        // Configure AR session for horizontal plane detection
        let config = ARWorldTrackingConfiguration()
        config.planeDetection = [.horizontal]
        config.environmentTexturing = .automatic
        arView.session.run(config)
        arView.session.delegate = context.coordinator
        context.coordinator.arView = arView

        // Add tap gesture for manual ghost placement
        let tapGesture = UITapGestureRecognizer(
            target: context.coordinator,
            action: #selector(Coordinator.handleTap(_:))
        )
        arView.addGestureRecognizer(tapGesture)

        return arView
    }

    func updateUIView(_ uiView: ARView, context: Context) {}

    class Coordinator: NSObject, ARSessionDelegate {
        var arView: ARView?
        var statusMessage: Binding<String>
        var ghostPlaced: Binding<Bool>
        private var ghost: GhostEntity?
        private var hasPlacedGhost = false

        init(statusMessage: Binding<String>, ghostPlaced: Binding<Bool>) {
            self.statusMessage = statusMessage
            self.ghostPlaced = ghostPlaced
        }

        // Auto-place ghost when a suitable horizontal plane is detected
        func session(_ session: ARSession, didAdd anchors: [ARAnchor]) {
            guard !hasPlacedGhost else { return }

            for anchor in anchors {
                guard let planeAnchor = anchor as? ARPlaneAnchor,
                      planeAnchor.alignment == .horizontal,
                      planeAnchor.extent.x > 0.3 || planeAnchor.extent.z > 0.3 else {
                    continue
                }

                // Place ghost at the center of the detected plane
                let position = SIMD3<Float>(
                    planeAnchor.center.x,
                    0,
                    planeAnchor.center.z
                )
                placeGhost(at: anchor, offset: position)
                break
            }
        }

        @objc func handleTap(_ gesture: UITapGestureRecognizer) {
            guard let arView = arView else { return }
            let location = gesture.location(in: arView)

            // Raycast to find a horizontal surface at tap point
            guard let result = arView.raycast(
                from: location,
                allowing: .estimatedPlane,
                alignment: .horizontal
            ).first else { return }

            // If ghost already placed, move it. Otherwise, place it.
            if hasPlacedGhost {
                moveGhost(to: result.worldTransform)
            } else {
                // Create an anchor at the raycast hit point
                let anchor = ARAnchor(name: "GhostAnchor", transform: result.worldTransform)
                arView.session.add(anchor: anchor)
                placeGhost(at: anchor, offset: .zero)
            }
        }

        private func placeGhost(at anchor: ARAnchor, offset: SIMD3<Float>) {
            guard !hasPlacedGhost, let arView = arView else { return }
            hasPlacedGhost = true

            do {
                let ghostEntity = try GhostEntity()
                self.ghost = ghostEntity

                // Create an anchor entity and attach the ghost
                let anchorEntity = AnchorEntity(anchor: anchor)
                ghostEntity.entity.position = offset
                anchorEntity.addChild(ghostEntity.entity)
                arView.scene.addAnchor(anchorEntity)

                ghostEntity.startHoverAnimation()

                DispatchQueue.main.async {
                    self.statusMessage.wrappedValue = "Ghost appeared!"
                    self.ghostPlaced.wrappedValue = true
                }
            } catch {
                hasPlacedGhost = false
                DispatchQueue.main.async {
                    self.statusMessage.wrappedValue = "Failed to load ghost: \(error.localizedDescription)"
                }
            }
        }

        private func moveGhost(to transform: simd_float4x4) {
            guard let ghost = ghost else { return }
            let position = SIMD3<Float>(
                transform.columns.3.x,
                transform.columns.3.y,
                transform.columns.3.z
            )
            ghost.entity.setPosition(position, relativeTo: nil)
        }
    }
}
