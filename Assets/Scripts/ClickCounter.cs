using UnityEngine;
using UnityEngine.UI;

public class ClickCounter : MonoBehaviour
{
    private int clickCount;
    private Text label;

    void Start()
    {
        CreateUI();
        UpdateLabel();
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            clickCount++;
            UpdateLabel();
        }
    }

    void CreateUI()
    {
        var canvasGo = new GameObject("ClickCounterCanvas");
        canvasGo.transform.SetParent(transform, false);

        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = short.MaxValue;

        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        canvasGo.AddComponent<GraphicRaycaster>();

        var textGo = new GameObject("ClickCounterText");
        textGo.transform.SetParent(canvasGo.transform, false);

        label = textGo.AddComponent<Text>();
        label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        label.fontSize = 32;
        label.fontStyle = FontStyle.Bold;
        label.color = Color.white;
        label.alignment = TextAnchor.LowerRight;
        label.horizontalOverflow = HorizontalWrapMode.Overflow;
        label.verticalOverflow = VerticalWrapMode.Overflow;

        var rt = label.rectTransform;
        rt.anchorMin = new Vector2(1f, 0f);
        rt.anchorMax = new Vector2(1f, 0f);
        rt.pivot = new Vector2(1f, 0f);
        rt.anchoredPosition = new Vector2(-20f, 20f);
        rt.sizeDelta = new Vector2(300f, 60f);
    }

    void UpdateLabel()
    {
        if (label != null)
        {
            label.text = "Clicks: " + clickCount;
        }
    }
}
