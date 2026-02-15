using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using System;
using System.Collections.Generic;

/// <summary>
/// Handles AR plane detection and provides placement APIs.
/// Filters for horizontal planes with sufficient extent (>0.3m).
/// </summary>
public class ARPlaneController : MonoBehaviour
{
    [Header("Placement Settings")]
    public float minPlaneExtent = 0.3f;

    [Header("AR References")]
    public ARPlaneManager planeManager;
    public ARRaycastManager raycastManager;

    /// <summary>
    /// Fired when a suitable horizontal plane is first detected.
    /// Provides the plane's center position and rotation.
    /// </summary>
    public event Action<Pose> OnSuitablePlaneFound;

    private bool hasNotifiedPlane;
    private static readonly List<ARRaycastHit> raycastHits = new List<ARRaycastHit>();

    void OnEnable()
    {
        if (planeManager != null)
            planeManager.trackablesChanged.AddListener(OnTrackablesChanged);
    }

    void OnDisable()
    {
        if (planeManager != null)
            planeManager.trackablesChanged.RemoveListener(OnTrackablesChanged);
    }

    void OnTrackablesChanged(ARTrackablesChangedEventArgs<ARPlane> args)
    {
        if (hasNotifiedPlane) return;

        Debug.Log("[ARPlane] Trackables changed: added=" + args.added.Count +
            " updated=" + args.updated.Count);

        // Check newly added planes
        foreach (var plane in args.added)
        {
            Debug.Log("[ARPlane] New plane: alignment=" + plane.alignment +
                " extents=" + plane.extents + " center=" + plane.center);
            if (IsSuitablePlane(plane))
            {
                hasNotifiedPlane = true;
                Pose pose = new Pose(plane.center, plane.transform.rotation);
                Debug.Log("[ARPlane] Suitable plane found, invoking callback");
                OnSuitablePlaneFound?.Invoke(pose);
                return;
            }
        }

        // Also check updated planes (may have grown large enough)
        foreach (var plane in args.updated)
        {
            if (IsSuitablePlane(plane))
            {
                hasNotifiedPlane = true;
                Pose pose = new Pose(plane.center, plane.transform.rotation);
                OnSuitablePlaneFound?.Invoke(pose);
                return;
            }
        }
    }

    bool IsSuitablePlane(ARPlane plane)
    {
        if (plane.alignment != PlaneAlignment.HorizontalUp)
            return false;

        Vector2 extent = plane.extents;
        return extent.x > minPlaneExtent || extent.y > minPlaneExtent;
    }

    /// <summary>
    /// Raycast from a screen point to find a placement position on a detected plane.
    /// Returns true if a valid hit was found.
    /// </summary>
    public bool TryGetPlacementPose(Vector2 screenPoint, out Pose pose)
    {
        pose = Pose.identity;
        if (raycastManager == null) return false;

        if (raycastManager.Raycast(screenPoint, raycastHits, TrackableType.PlaneWithinPolygon))
        {
            pose = raycastHits[0].pose;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Try to find a placement position on any currently tracked horizontal plane.
    /// Used for respawning when planes are already known.
    /// </summary>
    public bool TryGetAnyTrackedPlanePose(out Pose pose)
    {
        pose = Pose.identity;
        if (planeManager == null) return false;

        foreach (var plane in planeManager.trackables)
        {
            if (plane.alignment == PlaneAlignment.HorizontalUp &&
                plane.trackingState == TrackingState.Tracking)
            {
                pose = new Pose(plane.center, plane.transform.rotation);
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Reset state to allow detecting a new plane (used on respawn).
    /// </summary>
    public void ResetDetection()
    {
        hasNotifiedPlane = false;
    }

    /// <summary>
    /// Hide plane visualizations (called after ghost is placed).
    /// </summary>
    public void HidePlaneVisuals()
    {
        if (planeManager != null)
        {
            foreach (var plane in planeManager.trackables)
            {
                plane.gameObject.SetActive(false);
            }
            planeManager.planePrefab = null;
        }
    }

    /// <summary>
    /// Show plane visualizations (called when scanning for new placement).
    /// </summary>
    public void ShowPlaneVisuals()
    {
        if (planeManager != null)
        {
            foreach (var plane in planeManager.trackables)
            {
                plane.gameObject.SetActive(true);
            }
        }
    }
}
