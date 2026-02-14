using UnityEngine;
using UnityEditor;
using UnityEngine.XR.ARFoundation;
using UnityEngine.InputSystem.XR;

/// <summary>
/// Editor utility to set up the Ghost Hustlers AR scene with all required
/// GameObjects and components. Run from menu: Ghost Hustlers > Setup Scene.
/// </summary>
public class SceneSetup
{
    [MenuItem("Ghost Hustlers/Setup Scene")]
    public static void SetupARScene()
    {
        // -- AR Session --
        GameObject arSession = new GameObject("AR Session");
        arSession.AddComponent<ARSession>();
        arSession.AddComponent<ARInputManager>();

        // -- XR Origin (AR Session Origin) --
        GameObject xrOrigin = new GameObject("XR Origin");
        var origin = xrOrigin.AddComponent<Unity.XR.CoreUtils.XROrigin>();

        // Camera Offset
        GameObject cameraOffset = new GameObject("Camera Offset");
        cameraOffset.transform.SetParent(xrOrigin.transform);
        origin.CameraFloorOffsetObject = cameraOffset;

        // AR Camera
        GameObject arCameraGo = new GameObject("AR Camera");
        arCameraGo.transform.SetParent(cameraOffset.transform);
        arCameraGo.tag = "MainCamera";

        Camera arCamera = arCameraGo.AddComponent<Camera>();
        arCamera.clearFlags = CameraClearFlags.SolidColor;
        arCamera.backgroundColor = Color.black;
        arCamera.nearClipPlane = 0.1f;
        arCamera.farClipPlane = 20f;

        arCameraGo.AddComponent<ARCameraManager>();
        arCameraGo.AddComponent<ARCameraBackground>();
        arCameraGo.AddComponent<TrackedPoseDriver>();

        origin.Camera = arCamera;

        // -- AR Plane Manager --
        var planeManager = xrOrigin.AddComponent<ARPlaneManager>();

        // -- AR Raycast Manager --
        var raycastManager = xrOrigin.AddComponent<ARRaycastManager>();

        // -- Game Manager --
        GameObject gameManagerGo = new GameObject("GameManager");
        GameManager gm = gameManagerGo.AddComponent<GameManager>();
        gm.arCamera = arCamera;

        // -- AR Plane Controller --
        ARPlaneController planeController = gameManagerGo.AddComponent<ARPlaneController>();
        planeController.planeManager = planeManager;
        planeController.raycastManager = raycastManager;
        gm.planeController = planeController;

        // -- UI Manager --
        GameObject uiManagerGo = new GameObject("UIManager");
        UIManager uiMgr = uiManagerGo.AddComponent<UIManager>();
        uiMgr.gameManager = gm;
        gm.uiManager = uiMgr;

        Debug.Log("Ghost Hustlers AR scene setup complete! " +
            "Assign your ghost prefab to GameManager.ghostPrefab, " +
            "then configure XR settings in Project Settings > XR Plug-in Management.");
    }

    [MenuItem("Ghost Hustlers/Create Ghost Prefab")]
    public static void CreateGhostPrefab()
    {
        // Check if ghost.glb model exists in the Models folder
        string modelPath = "Assets/Models/ghost.glb";
        GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);

        GameObject ghostGo;
        if (model != null)
        {
            ghostGo = (GameObject)PrefabUtility.InstantiatePrefab(model);
            ghostGo.name = "Ghost";
        }
        else
        {
            // Fallback: procedural sphere
            ghostGo = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            ghostGo.name = "Ghost";
            ghostGo.transform.localScale = new Vector3(0.4f, 0.4f, 0.4f);
            Debug.LogWarning("ghost.glb not found at " + modelPath +
                ". Created procedural sphere. Import your GLB model to Assets/Models/.");
        }

        // Add Ghost component
        if (ghostGo.GetComponent<Ghost>() == null)
            ghostGo.AddComponent<Ghost>();

        // Save as prefab
        string prefabPath = "Assets/Prefabs/Ghost.prefab";
        if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
            AssetDatabase.CreateFolder("Assets", "Prefabs");

        PrefabUtility.SaveAsPrefabAsset(ghostGo, prefabPath);
        Object.DestroyImmediate(ghostGo);

        // Auto-assign to GameManager if one exists
        GameManager gm = Object.FindFirstObjectByType<GameManager>();
        if (gm != null)
        {
            gm.ghostPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            EditorUtility.SetDirty(gm);
        }

        Debug.Log("Ghost prefab created at " + prefabPath);
    }
}
