using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// HUD overlay manager. Creates and controls status text, crosshair,
/// respawn button, and hint text via a Screen Space Overlay canvas.
/// Ported from iOS ContentView.swift.
/// </summary>
public class UIManager : MonoBehaviour
{
    [Header("References (auto-created if null)")]
    public Canvas canvas;
    public TextMeshProUGUI statusText;
    public TextMeshProUGUI hintText;
    public Button respawnButton;
    public GameObject crosshair;

    [Header("Game Manager")]
    public GameManager gameManager;

    void Awake()
    {
        if (canvas == null)
            CreateUI();
    }

    void CreateUI()
    {
        // -- Canvas --
        GameObject canvasGo = new GameObject("HUDCanvas");
        canvasGo.transform.SetParent(transform);
        canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.matchWidthOrHeight = 0.5f;

        canvasGo.AddComponent<GraphicRaycaster>();

        // -- Status Text (top center) --
        GameObject statusGo = CreateTextElement(canvasGo.transform, "StatusText",
            "Scanning for surfaces...", 32, TextAlignmentOptions.Center);
        RectTransform statusRect = statusGo.GetComponent<RectTransform>();
        statusRect.anchorMin = new Vector2(0.5f, 1);
        statusRect.anchorMax = new Vector2(0.5f, 1);
        statusRect.pivot = new Vector2(0.5f, 1);
        statusRect.anchoredPosition = new Vector2(0, -100);
        statusRect.sizeDelta = new Vector2(800, 80);

        // Add background panel
        Image statusBg = statusGo.AddComponent<Image>();
        statusBg.color = new Color(0, 0, 0, 0.5f);
        statusBg.raycastTarget = false;
        // Move text on top of background
        statusText = statusGo.GetComponentInChildren<TextMeshProUGUI>();
        if (statusText == null)
        {
            // Text is on the same object — create a child text instead
            Destroy(statusGo.GetComponent<TextMeshProUGUI>());
            GameObject statusTextGo = CreateTextElement(statusGo.transform, "StatusLabel",
                "Scanning for surfaces...", 32, TextAlignmentOptions.Center);
            RectTransform stRect = statusTextGo.GetComponent<RectTransform>();
            stRect.anchorMin = Vector2.zero;
            stRect.anchorMax = Vector2.one;
            stRect.offsetMin = new Vector2(16, 8);
            stRect.offsetMax = new Vector2(-16, -8);
            statusText = statusTextGo.GetComponent<TextMeshProUGUI>();
        }

        // -- Crosshair (center) --
        crosshair = CreateCrosshair(canvasGo.transform);

        // -- Hint Text (bottom center) --
        GameObject hintGo = CreateTextElement(canvasGo.transform, "HintText",
            "Point your camera at a flat surface", 26, TextAlignmentOptions.Center);
        RectTransform hintRect = hintGo.GetComponent<RectTransform>();
        hintRect.anchorMin = new Vector2(0.5f, 0);
        hintRect.anchorMax = new Vector2(0.5f, 0);
        hintRect.pivot = new Vector2(0.5f, 0);
        hintRect.anchoredPosition = new Vector2(0, 80);
        hintRect.sizeDelta = new Vector2(700, 60);
        hintText = hintGo.GetComponent<TextMeshProUGUI>();
        hintText.alpha = 0.7f;

        // -- Respawn Button (bottom center) --
        GameObject buttonGo = new GameObject("RespawnButton");
        buttonGo.transform.SetParent(canvasGo.transform, false);
        RectTransform buttonRect = buttonGo.AddComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0.5f, 0);
        buttonRect.anchorMax = new Vector2(0.5f, 0);
        buttonRect.pivot = new Vector2(0.5f, 0);
        buttonRect.anchoredPosition = new Vector2(0, 60);
        buttonRect.sizeDelta = new Vector2(350, 80);

        Image buttonImage = buttonGo.AddComponent<Image>();
        buttonImage.color = new Color(0.2f, 0.75f, 0.3f, 1f); // Green

        respawnButton = buttonGo.AddComponent<Button>();
        respawnButton.targetGraphic = buttonImage;
        respawnButton.onClick.AddListener(OnRespawnClicked);

        // Button label
        GameObject buttonLabel = CreateTextElement(buttonGo.transform, "ButtonLabel",
            "Spawn New Ghost", 30, TextAlignmentOptions.Center);
        RectTransform labelRect = buttonLabel.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        // Initially hidden
        buttonGo.SetActive(false);
        crosshair.SetActive(false);
    }

    GameObject CreateTextElement(Transform parent, string name, string text,
        int fontSize, TextAlignmentOptions alignment)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>();

        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.alignment = alignment;
        tmp.color = Color.white;
        tmp.raycastTarget = false;

        return go;
    }

    GameObject CreateCrosshair(Transform parent)
    {
        GameObject container = new GameObject("Crosshair");
        container.transform.SetParent(parent, false);
        RectTransform containerRect = container.AddComponent<RectTransform>();
        containerRect.anchorMin = new Vector2(0.5f, 0.5f);
        containerRect.anchorMax = new Vector2(0.5f, 0.5f);
        containerRect.sizeDelta = new Vector2(30, 30);

        Color crossColor = new Color(1, 1, 1, 0.8f);

        // Circle (ring)
        GameObject ring = new GameObject("Ring");
        ring.transform.SetParent(container.transform, false);
        RectTransform ringRect = ring.AddComponent<RectTransform>();
        ringRect.anchorMin = Vector2.zero;
        ringRect.anchorMax = Vector2.one;
        ringRect.offsetMin = Vector2.zero;
        ringRect.offsetMax = Vector2.zero;

        Image ringImage = ring.AddComponent<Image>();
        ringImage.color = crossColor;
        ringImage.raycastTarget = false;
        // We'll use a simple circle sprite if available, otherwise outline approach
        ringImage.type = Image.Type.Simple;

        // Vertical line
        GameObject vLine = new GameObject("VLine");
        vLine.transform.SetParent(container.transform, false);
        RectTransform vRect = vLine.AddComponent<RectTransform>();
        vRect.anchorMin = new Vector2(0.5f, 0.5f);
        vRect.anchorMax = new Vector2(0.5f, 0.5f);
        vRect.sizeDelta = new Vector2(2, 14);
        Image vImage = vLine.AddComponent<Image>();
        vImage.color = crossColor;
        vImage.raycastTarget = false;

        // Horizontal line
        GameObject hLine = new GameObject("HLine");
        hLine.transform.SetParent(container.transform, false);
        RectTransform hRect = hLine.AddComponent<RectTransform>();
        hRect.anchorMin = new Vector2(0.5f, 0.5f);
        hRect.anchorMax = new Vector2(0.5f, 0.5f);
        hRect.sizeDelta = new Vector2(14, 2);
        Image hImage = hLine.AddComponent<Image>();
        hImage.color = crossColor;
        hImage.raycastTarget = false;

        // Make the ring look like a circle outline by making it small and using
        // a generated circle texture
        ringImage.sprite = CreateCircleSprite();
        ringImage.type = Image.Type.Simple;
        ringImage.preserveAspect = true;
        // Make ring slightly transparent to look like an outline
        Color ringOutline = crossColor;
        ringOutline.a = 0.6f;
        ringImage.color = ringOutline;

        return container;
    }

    Sprite CreateCircleSprite()
    {
        int size = 64;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        float center = size / 2f;
        float outerRadius = center - 1;
        float innerRadius = outerRadius - 3;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                if (dist <= outerRadius && dist >= innerRadius)
                    tex.SetPixel(x, y, Color.white);
                else
                    tex.SetPixel(x, y, Color.clear);
            }
        }

        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
    }

    // -- Public API --

    public void SetStatus(string message)
    {
        if (statusText != null)
            statusText.text = message;
    }

    public void ShowHint(string message)
    {
        if (hintText != null)
        {
            hintText.text = message;
            hintText.gameObject.SetActive(true);
        }
    }

    public void HideHint()
    {
        if (hintText != null)
            hintText.gameObject.SetActive(false);
    }

    public void SetCrosshairVisible(bool visible)
    {
        if (crosshair != null)
            crosshair.SetActive(visible);
    }

    public void SetRespawnVisible(bool visible)
    {
        if (respawnButton != null)
            respawnButton.gameObject.SetActive(visible);
    }

    void OnRespawnClicked()
    {
        if (gameManager != null)
            gameManager.Respawn();
    }
}
