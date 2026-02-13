package com.ghosthustlers.app.ar

import com.google.android.filament.Engine
import io.github.sceneview.loaders.ModelLoader
import io.github.sceneview.node.ModelNode
import kotlinx.coroutines.CoroutineScope
import kotlinx.coroutines.Job
import kotlinx.coroutines.delay
import kotlinx.coroutines.isActive
import kotlinx.coroutines.launch
import kotlin.math.sin

private const val MODEL_FILE = "models/ghost.glb"

/**
 * Creates a ModelNode for the ghost, loading the GLB model and applying
 * semi-transparency. Call [startHoverAnimation] to begin the hover + rotation.
 */
fun createGhostModelNode(
    modelLoader: ModelLoader,
): ModelNode {
    val modelNode = ModelNode(
        modelInstance = modelLoader.createModelInstance(MODEL_FILE),
        scaleToUnits = 0.4f // ~40cm tall
    )
    // Apply semi-transparency via Filament material
    modelNode.modelInstance?.materialInstances?.forEach { material ->
        material.setParameter(
            "baseColorFactor",
            0.7f, 0.85f, 1.0f, 0.6f // RGBA with alpha for transparency
        )
    }
    return modelNode
}

/**
 * Starts a sine-wave hover + slow rotation animation on the given node.
 * Returns a Job that can be cancelled to stop the animation.
 */
fun startHoverAnimation(
    scope: CoroutineScope,
    node: ModelNode,
): Job = scope.launch {
    val startY = node.position.y
    var elapsed = 0f
    val frameInterval = 16L // ~60fps

    while (isActive) {
        elapsed += frameInterval / 1000f

        // Sine wave hover: ±5cm, 1.5s period
        val hoverY = startY + 0.05f * sin(elapsed * 2f * Math.PI.toFloat() / 1.5f)

        // Slow rotation: full turn every 8 seconds
        val rotationDeg = (elapsed * 360f / 8f) % 360f

        node.position = node.position.copy(y = hoverY)
        node.rotation = node.rotation.copy(y = rotationDeg)

        delay(frameInterval)
    }
}
