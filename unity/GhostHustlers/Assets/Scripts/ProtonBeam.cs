using UnityEngine;

/// <summary>
/// Proton beam rendered as a dynamically-sized cylinder between camera and target.
/// Yellow/gold material with pulse animation.
/// Ported from iOS BeamEntity.swift.
/// </summary>
public class ProtonBeam : MonoBehaviour
{
    [Header("Beam Properties")]
    public Color beamColor = new Color(1f, 0.85f, 0.2f, 0.7f);
    public float baseRadius = 0.02f;
    public float pulseFrequency = 6f; // Hz
    public float pulseAmplitude = 0.005f;
    public float metallic = 0.8f;
    public float smoothness = 0.9f;

    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;
    private Material beamMaterial;
    private float pulseElapsed;
    private bool isPulsing;

    void Awake()
    {
        // Create a cylinder primitive as a child
        GameObject cylinder = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        cylinder.transform.SetParent(transform, false);
        cylinder.name = "BeamCylinder";

        // Remove collider — beam is visual only
        Destroy(cylinder.GetComponent<Collider>());

        meshFilter = cylinder.GetComponent<MeshFilter>();
        meshRenderer = cylinder.GetComponent<MeshRenderer>();

        // Create URP transparent material
        beamMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        beamMaterial.SetFloat("_Surface", 1); // Transparent
        beamMaterial.SetFloat("_Blend", 0);
        beamMaterial.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        beamMaterial.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        beamMaterial.SetFloat("_ZWrite", 0);
        beamMaterial.SetFloat("_Metallic", metallic);
        beamMaterial.SetFloat("_Smoothness", smoothness);
        beamMaterial.SetColor("_BaseColor", beamColor);
        beamMaterial.renderQueue = 3000;
        beamMaterial.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        meshRenderer.material = beamMaterial;
    }

    void Update()
    {
        if (isPulsing)
            pulseElapsed += Time.deltaTime;
    }

    /// <summary>
    /// Update beam geometry to stretch from start to end position in world space.
    /// </summary>
    public void UpdateBeam(Vector3 start, Vector3 end)
    {
        Vector3 direction = end - start;
        float distance = direction.magnitude;
        if (distance < 0.001f) return;

        // Position at midpoint
        transform.position = (start + end) / 2f;

        // Rotate cylinder (default Y-up) to align with beam direction
        transform.rotation = Quaternion.FromToRotation(Vector3.up, direction.normalized);

        // Scale: X/Z for radius (with pulse), Y for half-length
        // Unity cylinder is 2 units tall by default, 1 unit diameter
        float radius = baseRadius + pulseAmplitude * Mathf.Sin(pulseElapsed * 2f * Mathf.PI * pulseFrequency);
        float diameter = radius * 2f;
        transform.localScale = new Vector3(diameter, distance / 2f, diameter);
    }

    public void StartPulse()
    {
        pulseElapsed = 0;
        isPulsing = true;
        gameObject.SetActive(true);
    }

    public void StopPulse()
    {
        isPulsing = false;
        gameObject.SetActive(false);
    }

    /// <summary>
    /// Create a ProtonBeam instance in the scene.
    /// </summary>
    public static ProtonBeam Create()
    {
        GameObject go = new GameObject("ProtonBeam");
        ProtonBeam beam = go.AddComponent<ProtonBeam>();
        go.SetActive(false);
        return beam;
    }
}
