using UnityEngine;
using UnityEngine.XR.ARFoundation;

/// <summary>
/// Central game flow controller. Manages state machine, input, beam firing,
/// hit detection, and ghost lifecycle.
/// Ported from iOS ARViewContainer.swift Coordinator.
/// </summary>
public class GameManager : MonoBehaviour
{
    public enum GameState
    {
        Scanning,
        GhostPlaced,
        Capturing,
        Captured
    }

    [Header("AR References")]
    public ARPlaneController planeController;
    public Camera arCamera;

    [Header("Ghost")]
    public GameObject ghostPrefab;
    public float ghostScale = 1f; // GLB is already ~40cm

    [Header("Beam")]
    public float beamMissLength = 2f; // beam length when not hitting ghost
    public float beamStartOffset = 0.3f; // start beam 0.3m in front of camera

    [Header("Hit Detection")]
    public float ghostHitRadius = 0.25f;
    public float maxHitDistance = 10f;

    [Header("UI")]
    public UIManager uiManager;

    // State
    public GameState CurrentState { get; private set; } = GameState.Scanning;

    private Ghost currentGhost;
    private GameObject ghostObject;
    private ProtonBeam beam;
    private bool isFiring;
    private float touchBeganTime;

    void Start()
    {
        // Create beam (starts inactive)
        beam = ProtonBeam.Create();

        // Subscribe to plane detection
        if (planeController != null)
            planeController.OnSuitablePlaneFound += OnSuitablePlaneFound;

        SetState(GameState.Scanning);
    }

    void OnDestroy()
    {
        if (planeController != null)
            planeController.OnSuitablePlaneFound -= OnSuitablePlaneFound;

        if (beam != null)
            Destroy(beam.gameObject);
    }

    void Update()
    {
        HandleInput();
        UpdateBeam();
    }

    // -- State Management --

    void SetState(GameState newState)
    {
        CurrentState = newState;

        switch (newState)
        {
            case GameState.Scanning:
                uiManager?.SetStatus("Scanning for surfaces...");
                uiManager?.ShowHint("Point your camera at a flat surface");
                uiManager?.SetCrosshairVisible(false);
                uiManager?.SetRespawnVisible(false);
                break;

            case GameState.GhostPlaced:
                uiManager?.SetStatus("Ghost appeared! Hold screen to fire beam!");
                uiManager?.HideHint();
                uiManager?.SetCrosshairVisible(true);
                uiManager?.SetRespawnVisible(false);
                break;

            case GameState.Capturing:
                // Sub-state of GhostPlaced, beam is active
                break;

            case GameState.Captured:
                uiManager?.SetStatus("Ghost captured!");
                uiManager?.SetCrosshairVisible(false);
                uiManager?.SetRespawnVisible(true);
                break;
        }
    }

    // -- Plane Detection --

    void OnSuitablePlaneFound(Pose pose)
    {
        if (CurrentState != GameState.Scanning) return;
        PlaceGhost(pose.position, pose.rotation);
    }

    // -- Ghost Placement --

    void PlaceGhost(Vector3 position, Quaternion rotation)
    {
        if (ghostPrefab == null)
        {
            // Fallback: create a procedural ghost (sphere) if no prefab assigned
            ghostObject = CreateProceduralGhost();
        }
        else
        {
            ghostObject = Instantiate(ghostPrefab, position, Quaternion.identity);
        }

        ghostObject.transform.position = position;
        ghostObject.transform.localScale = Vector3.one * ghostScale;

        currentGhost = ghostObject.GetComponent<Ghost>();
        if (currentGhost == null)
            currentGhost = ghostObject.AddComponent<Ghost>();

        SetState(GameState.GhostPlaced);
        planeController?.HidePlaneVisuals();
    }

    GameObject CreateProceduralGhost()
    {
        // Fallback procedural ghost: sphere (matches iOS fallback)
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = "Ghost";
        go.transform.localScale = new Vector3(0.4f, 0.4f, 0.4f);
        // Ghost component will handle material setup
        return go;
    }

    // -- Input Handling --

    void HandleInput()
    {
        if (CurrentState == GameState.Captured || CurrentState == GameState.Scanning)
            return;

        // Touch input (mobile)
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);

            switch (touch.phase)
            {
                case TouchPhase.Began:
                    touchBeganTime = Time.time;
                    if (currentGhost != null && currentGhost.health > 0)
                        StartBeam();
                    break;

                case TouchPhase.Ended:
                case TouchPhase.Canceled:
                    float touchDuration = Time.time - touchBeganTime;
                    StopBeam();

                    // Short tap (<0.2s) for manual placement
                    if (touchDuration < 0.2f && CurrentState == GameState.Scanning)
                        HandleTapPlacement(touch.position);
                    break;
            }
        }

        // Mouse input (editor testing)
        if (Input.GetMouseButtonDown(0))
        {
            touchBeganTime = Time.time;
            if (currentGhost != null && currentGhost.health > 0)
                StartBeam();
        }
        if (Input.GetMouseButtonUp(0))
        {
            StopBeam();
        }
    }

    void HandleTapPlacement(Vector2 screenPos)
    {
        if (planeController != null && planeController.TryGetPlacementPose(screenPos, out Pose pose))
        {
            PlaceGhost(pose.position, pose.rotation);
        }
    }

    // -- Beam --

    void StartBeam()
    {
        if (isFiring || currentGhost == null) return;
        isFiring = true;
        beam.StartPulse();
        currentGhost.ShowHealthBar();
        SetState(GameState.Capturing);
    }

    void StopBeam()
    {
        if (!isFiring) return;
        isFiring = false;
        beam.StopPulse();

        if (currentGhost != null)
        {
            currentGhost.isShaking = false;
            if (currentGhost.health > 0)
                currentGhost.HideHealthBar();
        }

        if (CurrentState == GameState.Capturing)
            SetState(GameState.GhostPlaced);
    }

    void UpdateBeam()
    {
        if (!isFiring || beam == null || currentGhost == null || currentGhost.health <= 0)
            return;

        Transform camTransform = arCamera != null ? arCamera.transform : Camera.main.transform;
        Vector3 cameraPos = camTransform.position;
        Vector3 cameraForward = camTransform.forward;

        // Beam origin: offset in front of camera (past near clip)
        Vector3 beamOrigin = cameraPos + beamStartOffset * cameraForward;

        // Ghost world position
        Vector3 ghostPos = currentGhost.transform.position;

        // Hit detection: angular threshold check (matching iOS logic)
        Vector3 toGhost = ghostPos - cameraPos;
        float distToGhost = toGhost.magnitude;
        Vector3 toGhostDir = toGhost.normalized;
        float dotProduct = Vector3.Dot(cameraForward, toGhostDir);

        float angularThreshold = Mathf.Atan2(ghostHitRadius, Mathf.Max(distToGhost, 0.1f));
        float aimAngle = Mathf.Acos(Mathf.Clamp(dotProduct, -1f, 1f));

        bool isHitting = aimAngle < angularThreshold && distToGhost < maxHitDistance;

        Vector3 beamEnd;
        if (isHitting)
        {
            beamEnd = ghostPos;
            currentGhost.isShaking = true;
            currentGhost.ShowHealthBar();
            currentGhost.TakeDamage(Time.deltaTime);

            if (currentGhost.health <= 0)
            {
                // Ghost captured!
                StopBeam();
                currentGhost.PlayCaptureAnimation(() =>
                {
                    currentGhost = null;
                    ghostObject = null;
                    SetState(GameState.Captured);
                });
                return;
            }
        }
        else
        {
            // Beam fires into empty space
            beamEnd = beamOrigin + beamMissLength * cameraForward;
            currentGhost.isShaking = false;
        }

        beam.UpdateBeam(beamOrigin, beamEnd);
    }

    // -- Respawn (called from UI button) --

    public void Respawn()
    {
        // Clean up old ghost
        if (ghostObject != null)
            Destroy(ghostObject);
        currentGhost = null;
        ghostObject = null;

        // Stop any active beam
        isFiring = false;
        beam?.StopPulse();

        // Reset plane detection
        planeController?.ResetDetection();
        planeController?.ShowPlaneVisuals();

        SetState(GameState.Scanning);

        // Try to place immediately on an already-tracked plane
        if (planeController != null && planeController.TryGetAnyTrackedPlanePose(out Pose pose))
        {
            PlaceGhost(pose.position, pose.rotation);
        }
    }
}
