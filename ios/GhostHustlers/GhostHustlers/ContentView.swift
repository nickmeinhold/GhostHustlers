import SwiftUI

struct ContentView: View {
    @State private var statusMessage = "Scanning for surfaces..."
    @State private var ghostPlaced = false

    var body: some View {
        ZStack {
            ARViewContainer(
                statusMessage: $statusMessage,
                ghostPlaced: $ghostPlaced
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

                if !ghostPlaced {
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
        }
    }
}
