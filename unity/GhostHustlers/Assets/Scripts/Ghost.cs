using UnityEngine;
using System;

/// <summary>
/// Ghost entity with health system, hover/rotation animations, damage shake,
/// health bar display, and capture animation.
/// Ported from iOS GhostEntity.swift.
/// </summary>
public class Ghost : MonoBehaviour
{
    [Header("Health")]
    public float health = 1f;
    public float captureTime = 4f; // seconds of beam contact to capture

    [Header("Hover Animation")]
    public float hoverAmplitude = 0.05f; // ±5cm
    public float hoverPeriod = 1.5f;

    [Header("Rotation")]
    public float rotationPeriod = 8f; // 360° every 8s

    [Header("Shake")]
    public float shakeIntensity = 0.01f; // ±1cm jitter

    [Header("Ghost Material")]
    public Color ghostColor = new Color(0.3f, 0.7f, 1f, 1f);

    [Header("Health Bar")]
    public float healthBarWidth = 0.15f;
    public float healthBarHeight = 0.01f;
    public float healthBarDepth = 0.02f;
    public float healthBarYOffset = 0.3f;

    // State
    [NonSerialized] public bool isShaking;
    private float elapsed;
    private Vector3 baseLocalPosition;
    private bool isAnimating = true;
    private bool isCaptured;

    // Health bar objects
    private GameObject healthBarBg;
    private GameObject healthBarFill;
    private Renderer healthBarFillRenderer;
    private bool healthBarVisible;

    // Capture animation
    private bool capturePlaying;
    private float captureElapsed;
    private const float captureDuration = 0.5f;
    private Vector3 captureStartScale;
    private Action captureCallback;

    // Material
    private Renderer ghostRenderer;
    private Material ghostMaterial;

    void Awake()
    {
        baseLocalPosition = transform.localPosition;
        SetupMaterial();
        SetupHealthBar();
    }

    void SetupMaterial()
    {
        ghostRenderer = GetComponentInChildren<Renderer>();
        if (ghostRenderer == null)
        {
            Debug.LogError("[Ghost] No Renderer found on ghost or children!");
            return;
        }

        Shader shader = ShaderUtils.FindURPShader();
        if (shader == null) { Debug.LogError("[Ghost] No shader available!"); return; }
        ghostMaterial = new Material(shader);

        // Opaque base color — visible against any background
        ghostMaterial.SetColor("_BaseColor", new Color(0.3f, 0.7f, 1f, 1f));
        ghostMaterial.SetFloat("_Metallic", 0f);
        ghostMaterial.SetFloat("_Smoothness", 0.5f);
        // Leave as opaque (Surface=0) — transparent variants get stripped on device
        ghostMaterial.SetFloat("_Surface", 0);
        ghostMaterial.renderQueue = -1; // use shader default

        ghostRenderer.material = ghostMaterial;
        Debug.Log("[Ghost] Material setup complete, shader: " + shader.name +
            " color: " + ghostMaterial.GetColor("_BaseColor") +
            " renderQueue: " + ghostMaterial.renderQueue);
    }

    static void ConfigureTransparentMaterial(Material mat, Color color, int renderQueue)
    {
        mat.SetFloat("_Surface", 1); // Transparent
        mat.SetFloat("_Blend", 0); // Alpha
        mat.SetFloat("_AlphaClip", 0);
        mat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetFloat("_ZWrite", 0);
        mat.SetColor("_BaseColor", color);
        mat.renderQueue = renderQueue;
        mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
    }

    void SetupHealthBar()
    {
        // Background bar (dark)
        healthBarBg = GameObject.CreatePrimitive(PrimitiveType.Cube);
        healthBarBg.name = "HealthBarBg";
        healthBarBg.transform.SetParent(transform);
        healthBarBg.transform.localPosition = new Vector3(0, healthBarYOffset, 0);
        healthBarBg.transform.localScale = new Vector3(healthBarWidth, healthBarHeight, healthBarDepth);
        Destroy(healthBarBg.GetComponent<Collider>());

        // NOTE: Transparent shader variants may be stripped on device if no Material
        // asset in the project references URP/Lit in transparent mode. Health bars will
        // still render but as opaque. Acceptable for now — see CLAUDE.md "Runtime material creation".
        Shader shader = ShaderUtils.FindURPShader();
        if (shader == null) return;
        var bgMat = new Material(shader);
        ConfigureTransparentMaterial(bgMat, new Color(0.2f, 0.2f, 0.2f, 0.8f), 3001);
        healthBarBg.GetComponent<Renderer>().material = bgMat;

        // Fill bar (green → red)
        healthBarFill = GameObject.CreatePrimitive(PrimitiveType.Cube);
        healthBarFill.name = "HealthBarFill";
        healthBarFill.transform.SetParent(transform);
        healthBarFill.transform.localPosition = new Vector3(0, healthBarYOffset, 0);
        healthBarFill.transform.localScale = new Vector3(healthBarWidth, healthBarHeight * 1.2f, healthBarDepth * 1.1f);
        Destroy(healthBarFill.GetComponent<Collider>());

        var fillMat = new Material(shader);
        ConfigureTransparentMaterial(fillMat, Color.green, 3002);
        healthBarFillRenderer = healthBarFill.GetComponent<Renderer>();
        healthBarFillRenderer.material = fillMat;

        // Initially hidden
        healthBarBg.SetActive(false);
        healthBarFill.SetActive(false);
    }

    void Update()
    {
        if (capturePlaying)
        {
            UpdateCaptureAnimation();
            return;
        }

        if (!isAnimating || isCaptured) return;

        elapsed += Time.deltaTime;

        // Sine wave hover: ±5cm, 1.5s period
        float hoverY = hoverAmplitude * Mathf.Sin(elapsed * 2f * Mathf.PI / hoverPeriod);

        // Slow rotation: 360°/8s
        float rotationAngle = elapsed * 360f / rotationPeriod;

        Vector3 pos = baseLocalPosition;
        pos.y += hoverY;

        // Shake jitter when being hit
        if (isShaking)
        {
            pos.x += UnityEngine.Random.Range(-shakeIntensity, shakeIntensity);
            pos.z += UnityEngine.Random.Range(-shakeIntensity, shakeIntensity);
        }

        transform.localPosition = pos;
        transform.localRotation = Quaternion.Euler(0, rotationAngle, 0);

        // Billboard the health bar (counter-rotate so it faces camera)
        if (healthBarVisible && Camera.main != null)
        {
            Vector3 camForward = Camera.main.transform.forward;
            camForward.y = 0;
            if (camForward.sqrMagnitude > 0.001f)
            {
                Quaternion lookRot = Quaternion.LookRotation(camForward);
                healthBarBg.transform.rotation = lookRot;
                healthBarFill.transform.rotation = lookRot;
            }
        }
    }

    public void ShowHealthBar()
    {
        if (!healthBarVisible)
        {
            healthBarBg.SetActive(true);
            healthBarFill.SetActive(true);
            healthBarVisible = true;
        }
    }

    public void HideHealthBar()
    {
        if (healthBarVisible)
        {
            healthBarBg.SetActive(false);
            healthBarFill.SetActive(false);
            healthBarVisible = false;
        }
    }

    public void TakeDamage(float deltaTime)
    {
        health -= deltaTime / captureTime;
        health = Mathf.Max(0, health);
        UpdateHealthBar();
    }

    void UpdateHealthBar()
    {
        float h = Mathf.Clamp01(health);

        // Scale fill bar by health, anchored to left
        Vector3 fillScale = healthBarFill.transform.localScale;
        fillScale.x = healthBarWidth * h;
        healthBarFill.transform.localScale = fillScale;

        Vector3 fillPos = healthBarFill.transform.localPosition;
        fillPos.x = -healthBarWidth * (1f - h) / 2f;
        healthBarFill.transform.localPosition = fillPos;

        // Color gradient: green → yellow → red
        Color barColor;
        if (h > 0.5f)
        {
            float t = (h - 0.5f) * 2f; // 1 at full, 0 at half
            barColor = new Color(1f - t, 0.5f + 0.5f * t, 0, 0.9f);
        }
        else
        {
            float t = h * 2f; // 1 at half, 0 at empty
            barColor = new Color(1f, t * 0.8f, 0, 0.9f);
        }
        healthBarFillRenderer.material.SetColor("_BaseColor", barColor);
    }

    /// <summary>
    /// Play the capture animation (shrink + fade over 0.5s), then invoke callback.
    /// </summary>
    public void PlayCaptureAnimation(Action onComplete)
    {
        isAnimating = false;
        isShaking = false;
        isCaptured = true;
        capturePlaying = true;
        captureElapsed = 0;
        captureStartScale = transform.localScale;
        captureCallback = onComplete;
    }

    void UpdateCaptureAnimation()
    {
        captureElapsed += Time.deltaTime;
        float t = Mathf.Clamp01(captureElapsed / captureDuration);

        // Shrink
        transform.localScale = captureStartScale * (1f - t * 0.99f);

        // Fade
        if (ghostMaterial != null)
        {
            Color c = ghostColor;
            c.a = ghostColor.a * (1f - t);
            ghostMaterial.SetColor("_BaseColor", c);
        }

        if (t >= 1f)
        {
            capturePlaying = false;
            captureCallback?.Invoke();
            Destroy(gameObject);
        }
    }

    public void ResetGhost()
    {
        health = 1f;
        isShaking = false;
        isCaptured = false;
        capturePlaying = false;
        elapsed = 0;
        isAnimating = true;
        transform.localScale = Vector3.one;
        HideHealthBar();
        UpdateHealthBar();
        if (ghostMaterial != null)
            ghostMaterial.SetColor("_BaseColor", ghostColor);
    }
}
