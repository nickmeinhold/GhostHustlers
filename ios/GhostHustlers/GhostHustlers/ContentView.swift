import SwiftUI

struct ContentView: View {
    @State private var statusMessage = "Scanning for surfaces..."
    @State private var ghostPlaced = false
    @State private var ghostCaptured = false

    var body: some View {
        ZStack {
            ARViewContainer(
                statusMessage: $statusMessage,
                ghostPlaced: $ghostPlaced,
                ghostCaptured: $ghostCaptured
            )
            .edgesIgnoringSafeArea(.all)

            VStack {
                Text(statusMessage)
                    .font(.headline)
                    .padding(.horizontal, 16)
                    .padding(.vertical, 8)
                    .background(.ultraThinMaterial)
                    .clipShape(Capsule())
                    .padding(.top, 60)

                Spacer()

                if ghostCaptured {
                    Button(action: {
                        // Trigger respawn — reset state so auto-place picks up next plane
                        ghostCaptured = false
                        ghostPlaced = false
                        statusMessage = "Scanning for surfaces..."
                    }) {
                        Text("Spawn New Ghost")
                            .font(.headline)
                            .foregroundColor(.white)
                            .padding(.horizontal, 24)
                            .padding(.vertical, 12)
                            .background(Color.green)
                            .clipShape(Capsule())
                    }
                    .padding(.bottom, 40)
                } else if !ghostPlaced {
                    Text("Point your camera at a flat surface")
                        .font(.subheadline)
                        .foregroundStyle(.secondary)
                        .padding(.horizontal, 16)
                        .padding(.vertical, 8)
                        .background(.ultraThinMaterial)
                        .clipShape(Capsule())
                        .padding(.bottom, 40)
                }
            }

            // Crosshair overlay — visible when ghost is placed and not captured
            if ghostPlaced && !ghostCaptured {
                Circle()
                    .stroke(Color.white.opacity(0.8), lineWidth: 2)
                    .frame(width: 30, height: 30)
                    .overlay(
                        Group {
                            Rectangle()
                                .fill(Color.white.opacity(0.8))
                                .frame(width: 2, height: 14)
                            Rectangle()
                                .fill(Color.white.opacity(0.8))
                                .frame(width: 14, height: 2)
                        }
                    )
            }
        }
    }
}
