import SwiftUI
import ARKit
import RealityKit

struct ARViewContainer: UIViewRepresentable {
    @Binding var statusMessage: String
    @Binding var ghostPlaced: Bool
    @Binding var ghostCaptured: Bool

    func makeCoordinator() -> Coordinator {
        Coordinator(
            statusMessage: $statusMessage,
            ghostPlaced: $ghostPlaced,
            ghostCaptured: $ghostCaptured
        )
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

        // Long-press gesture with 0 delay — fires on touch-down immediately
        let longPress = UILongPressGestureRecognizer(
            target: context.coordinator,
            action: #selector(Coordinator.handleLongPress(_:))
        )
        longPress.minimumPressDuration = 0
        arView.addGestureRecognizer(longPress)

        return arView
    }

    func updateUIView(_ uiView: ARView, context: Context) {
        context.coordinator.ghostCaptured = $ghostCaptured

        // Detect respawn: ghostCaptured and ghostPlaced both reset to false by button
        if !ghostPlaced && !ghostCaptured && context.coordinator.needsRespawn {
            context.coordinator.respawnGhost()
        }
    }

    class Coordinator: NSObject, ARSessionDelegate {
        var arView: ARView?
        var statusMessage: Binding<String>
        var ghostPlaced: Binding<Bool>
        var ghostCaptured: Binding<Bool>
        /// True after a ghost has been captured, cleared on respawn
        var needsRespawn = false

        private var ghost: GhostEntity?
        private var ghostAnchorEntity: AnchorEntity?
        private var hasPlacedGhost = false

        // Beam state
        private var beam: BeamEntity?
        private var beamAnchor: AnchorEntity?
        private var isFiring = false
        private var touchBeganTime: TimeInterval = 0
        private var lastFrameTime: TimeInterval = 0

        init(statusMessage: Binding<String>, ghostPlaced: Binding<Bool>, ghostCaptured: Binding<Bool>) {
            self.statusMessage = statusMessage
            self.ghostPlaced = ghostPlaced
            self.ghostCaptured = ghostCaptured
        }

        // MARK: - Gesture handling

        @objc func handleLongPress(_ gesture: UILongPressGestureRecognizer) {
            switch gesture.state {
            case .began:
                touchBeganTime = CACurrentMediaTime()
                guard hasPlacedGhost, ghost != nil, ghost!.health > 0 else {
                    return
                }
                startBeam()

            case .ended, .cancelled:
                let touchDuration = CACurrentMediaTime() - touchBeganTime
                stopBeam()

                // Short tap (< 0.2s) treated as a tap for placement
                if touchDuration < 0.2 {
                    handleTapPlacement(gesture)
                }

            default:
                break
            }
        }

        private func handleTapPlacement(_ gesture: UILongPressGestureRecognizer) {
            guard !hasPlacedGhost, let arView = arView else { return }
            let location = gesture.location(in: arView)

            guard let result = arView.raycast(
                from: location,
                allowing: .estimatedPlane,
                alignment: .horizontal
            ).first else { return }

            let anchor = ARAnchor(name: "GhostAnchor", transform: result.worldTransform)
            arView.session.add(anchor: anchor)
            placeGhost(at: anchor, offset: .zero)
        }

        private func startBeam() {
            guard let arView = arView else { return }
            isFiring = true
            let beamEntity = BeamEntity()
            beam = beamEntity
            beamEntity.startPulse()

            // Add beam to scene root so it's not parented to ghost
            let anchor = AnchorEntity(world: .zero)
            anchor.addChild(beamEntity.entity)
            arView.scene.addAnchor(anchor)
            beamAnchor = anchor

            ghost?.showHealthBar()
        }

        private func stopBeam() {
            isFiring = false
            beam?.stopPulse()
            if let anchor = beamAnchor {
                arView?.scene.removeAnchor(anchor)
            }
            beam = nil
            beamAnchor = nil

            ghost?.isShaking = false
            if let ghost = ghost, ghost.health > 0 {
                ghost.hideHealthBar()
            }
        }

        // MARK: - Per-frame update

        func session(_ session: ARSession, didUpdate frame: ARFrame) {
            let currentTime = CACurrentMediaTime()
            let deltaTime = lastFrameTime > 0 ? Float(currentTime - lastFrameTime) : Float(1.0 / 60.0)
            lastFrameTime = currentTime

            guard isFiring, let beam = beam, let ghost = ghost, ghost.health > 0 else {
                return
            }

            let cameraTransform = frame.camera.transform

            // Camera position
            let cameraPos = SIMD3<Float>(
                cameraTransform.columns.3.x,
                cameraTransform.columns.3.y,
                cameraTransform.columns.3.z
            )

            // Camera forward direction (negative Z in camera space)
            let cameraForward = -SIMD3<Float>(
                cameraTransform.columns.2.x,
                cameraTransform.columns.2.y,
                cameraTransform.columns.2.z
            )

            // Beam origin: 0.3m in front of camera (past AR near clip plane)
            let beamOrigin = cameraPos + 0.3 * cameraForward

            // Ghost world position
            let ghostWorldPos = ghost.entity.position(relativeTo: nil)

            // Check if aiming at ghost: simple angular check
            let toGhost = ghostWorldPos - cameraPos
            let distToGhost = simd_length(toGhost)
            let toGhostDir = simd_normalize(toGhost)
            let dotProduct = simd_dot(cameraForward, toGhostDir)

            // The ghost has ~0.2m radius; compute angular threshold
            let ghostRadius: Float = 0.25
            let angularThreshold = atan2(ghostRadius, max(distToGhost, 0.1))
            let aimAngle = acos(min(max(dotProduct, -1.0), 1.0))

            let beamEnd: SIMD3<Float>
            let isHittingGhost = aimAngle < angularThreshold && distToGhost < 10.0

            if isHittingGhost {
                beamEnd = ghostWorldPos
                ghost.isShaking = true
                ghost.showHealthBar()
                ghost.takeDamage(deltaTime)

                if ghost.health <= 0 {
                    // Ghost captured!
                    stopBeam()
                    ghost.playCaptureAnimation { [weak self] in
                        DispatchQueue.main.async {
                            self?.needsRespawn = true
                            self?.statusMessage.wrappedValue = "Ghost captured!"
                            self?.ghostCaptured.wrappedValue = true
                        }
                    }
                    return
                }
            } else {
                // Beam fires 2m into empty space
                beamEnd = beamOrigin + 2.0 * cameraForward
                ghost.isShaking = false
            }

            beam.update(from: beamOrigin, to: beamEnd)
        }

        // MARK: - Auto-place ghost on detected plane

        func session(_ session: ARSession, didAdd anchors: [ARAnchor]) {
            guard !hasPlacedGhost else { return }

            for anchor in anchors {
                guard let planeAnchor = anchor as? ARPlaneAnchor,
                      planeAnchor.alignment == .horizontal,
                      planeAnchor.extent.x > 0.3 || planeAnchor.extent.z > 0.3 else {
                    continue
                }

                let position = SIMD3<Float>(
                    planeAnchor.center.x,
                    0,
                    planeAnchor.center.z
                )
                placeGhost(at: anchor, offset: position)
                break
            }
        }

        // MARK: - Ghost placement

        private func placeGhost(at anchor: ARAnchor, offset: SIMD3<Float>) {
            guard !hasPlacedGhost, let arView = arView else { return }
            hasPlacedGhost = true

            do {
                let ghostEntity = try GhostEntity()
                self.ghost = ghostEntity

                let anchorEntity = AnchorEntity(anchor: anchor)
                ghostEntity.entity.position = offset
                anchorEntity.addChild(ghostEntity.entity)
                arView.scene.addAnchor(anchorEntity)
                self.ghostAnchorEntity = anchorEntity

                ghostEntity.startHoverAnimation()

                DispatchQueue.main.async {
                    self.statusMessage.wrappedValue = "Ghost appeared! Hold screen to fire beam!"
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

        // MARK: - Respawn

        func respawnGhost() {
            // Clean up old ghost
            ghost?.stopHoverAnimation()
            ghostAnchorEntity?.removeFromParent()
            ghost = nil
            ghostAnchorEntity = nil
            hasPlacedGhost = false
            needsRespawn = false
            isFiring = false
            beam?.stopPulse()
            if let anchor = beamAnchor {
                arView?.scene.removeAnchor(anchor)
            }
            beam = nil
            beamAnchor = nil
            lastFrameTime = 0

            // Always reset UI state first
            DispatchQueue.main.async {
                self.ghostCaptured.wrappedValue = false
                self.ghostPlaced.wrappedValue = false
                self.statusMessage.wrappedValue = "Scanning for surfaces..."
            }

            // Place immediately on an already-tracked plane
            if let arView = arView, let frame = arView.session.currentFrame {
                for anchor in frame.anchors {
                    if let planeAnchor = anchor as? ARPlaneAnchor,
                       planeAnchor.alignment == .horizontal {
                        let position = SIMD3<Float>(
                            planeAnchor.center.x, 0, planeAnchor.center.z
                        )
                        placeGhost(at: planeAnchor, offset: position)
                        break
                    }
                }
            }
        }
    }
}
