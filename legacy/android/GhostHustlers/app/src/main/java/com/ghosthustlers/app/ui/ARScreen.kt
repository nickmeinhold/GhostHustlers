package com.ghosthustlers.app.ui

import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.systemBarsPadding
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material3.Surface
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.rememberCoroutineScope
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.google.ar.core.Config
import com.google.ar.core.Frame
import com.google.ar.core.Plane
import com.google.ar.core.TrackingFailureReason
import com.ghosthustlers.app.ar.createGhostModelNode
import com.ghosthustlers.app.ar.startHoverAnimation
import io.github.sceneview.ar.ARScene
import io.github.sceneview.ar.arcore.createAnchorOrNull
import io.github.sceneview.ar.arcore.getUpdatedPlanes
import io.github.sceneview.ar.arcore.isValid
import io.github.sceneview.ar.node.AnchorNode
import io.github.sceneview.ar.rememberARCameraNode
import io.github.sceneview.rememberCollisionSystem
import io.github.sceneview.rememberEngine
import io.github.sceneview.rememberModelLoader
import io.github.sceneview.rememberNodes
import io.github.sceneview.rememberOnGestureListener
import io.github.sceneview.rememberView

@Composable
fun ARScreen() {
    Box(modifier = Modifier.fillMaxSize()) {
        val engine = rememberEngine()
        val modelLoader = rememberModelLoader(engine)
        val cameraNode = rememberARCameraNode(engine)
        val childNodes = rememberNodes()
        val view = rememberView(engine)
        val collisionSystem = rememberCollisionSystem(view)
        val scope = rememberCoroutineScope()

        var ghostPlaced by remember { mutableStateOf(false) }
        var trackingFailure by remember { mutableStateOf<TrackingFailureReason?>(null) }
        var frame by remember { mutableStateOf<Frame?>(null) }

        ARScene(
            modifier = Modifier.fillMaxSize(),
            childNodes = childNodes,
            engine = engine,
            view = view,
            modelLoader = modelLoader,
            collisionSystem = collisionSystem,
            sessionConfiguration = { session, config ->
                config.planeFindingMode = Config.PlaneFindingMode.HORIZONTAL
                config.lightEstimationMode = Config.LightEstimationMode.ENVIRONMENTAL_HDR
            },
            cameraNode = cameraNode,
            planeRenderer = !ghostPlaced,
            onTrackingFailureChanged = { trackingFailure = it },
            onSessionUpdated = { _, updatedFrame ->
                frame = updatedFrame

                // Auto-place ghost on the first detected horizontal plane
                if (!ghostPlaced) {
                    updatedFrame.getUpdatedPlanes()
                        .firstOrNull { it.type == Plane.Type.HORIZONTAL_UPWARD_FACING }
                        ?.let { plane ->
                            // Only place if plane is large enough (>0.3m in either dimension)
                            val extent = plane.extentX.coerceAtLeast(plane.extentZ)
                            if (extent > 0.3f) {
                                plane.createAnchorOrNull(plane.centerPose)
                            } else null
                        }?.let { anchor ->
                            val anchorNode = AnchorNode(engine = engine, anchor = anchor)
                            val ghostModel = createGhostModelNode(modelLoader)
                            anchorNode.addChildNode(ghostModel)
                            childNodes += anchorNode
                            ghostPlaced = true
                            startHoverAnimation(scope, ghostModel)
                        }
                }
            },
            onGestureListener = rememberOnGestureListener(
                onSingleTapConfirmed = { motionEvent, node ->
                    // Tap to place additional ghosts or reposition
                    if (node == null) {
                        val hitResults = frame?.hitTest(motionEvent.x, motionEvent.y)
                        hitResults?.firstOrNull {
                            it.isValid(depthPoint = false, point = false)
                        }?.createAnchorOrNull()?.let { anchor ->
                            val anchorNode = AnchorNode(engine = engine, anchor = anchor)
                            val ghostModel = createGhostModelNode(modelLoader)
                            anchorNode.addChildNode(ghostModel)
                            childNodes += anchorNode
                            ghostPlaced = true
                            startHoverAnimation(scope, ghostModel)
                        }
                    }
                }
            )
        )

        // Status overlay
        Surface(
            modifier = Modifier
                .systemBarsPadding()
                .align(Alignment.TopCenter)
                .padding(top = 16.dp),
            shape = RoundedCornerShape(24.dp),
            color = Color.Black.copy(alpha = 0.6f),
        ) {
            Text(
                modifier = Modifier.padding(horizontal = 20.dp, vertical = 10.dp),
                textAlign = TextAlign.Center,
                fontSize = 16.sp,
                color = Color.White,
                text = when {
                    trackingFailure != null -> "Tracking lost - move your device slowly"
                    ghostPlaced -> "Ghost appeared!"
                    else -> "Scanning for surfaces..."
                }
            )
        }

        // Bottom hint
        if (!ghostPlaced) {
            Surface(
                modifier = Modifier
                    .align(Alignment.BottomCenter)
                    .padding(bottom = 48.dp),
                shape = RoundedCornerShape(24.dp),
                color = Color.Black.copy(alpha = 0.4f),
            ) {
                Text(
                    modifier = Modifier.padding(horizontal = 16.dp, vertical = 8.dp),
                    textAlign = TextAlign.Center,
                    fontSize = 14.sp,
                    color = Color.White.copy(alpha = 0.8f),
                    text = "Point your camera at a flat surface"
                )
            }
        }
    }
}
