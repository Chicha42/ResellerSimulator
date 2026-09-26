using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Фабрика UI-элементов в стиле прототипа (UGUI + TextMeshPro).
// Используется для построения новых экранов (прокачка, торг) без ручной правки сцены.
public static class UIFactory
{
    public static readonly Color PanelBg   = new Color32(0x0F, 0x12, 0x1A, 0xF5);
    public static readonly Color CardBg    = new Color32(0x1B, 0x1F, 0x2B, 0xFF);
    public static readonly Color RowBg     = new Color32(0x23, 0x28, 0x36, 0xFF);
    public static readonly Color TextMain  = new Color32(0xEC, 0xEF, 0xF7, 0xFF);
    public static readonly Color TextMuted = new Color32(0x8A, 0x91, 0xA3, 0xFF);
    public static readonly Color Green     = new Color32(0x00, 0xB9, 0x54, 0xFF);
    public static readonly Color Red       = new Color32(0xFF, 0x5C, 0x5C, 0xFF);
    public static readonly Color Yellow    = new Color32(0xFF, 0xD2, 0x4D, 0xFF);
    public static readonly Color Blue      = new Color32(0x4C, 0x8D, 0xFF, 0xFF);
    public static readonly Color Overlay   = new Color32(0x00, 0x00, 0x00, 0xAA);

    // Общий спрайт для кнопок/панелей (берётся у существующей кнопки в сцене)
    public static Sprite ButtonSprite;

    public static GameObject NewUIObject(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go;
    }

    public static RectTransform Rect(GameObject go) => go.GetComponent<RectTransform>();

    public static Image Image(string name, Transform parent, Color color, Sprite sprite = null)
    {
        var go = NewUIObject(name, parent);
        var img = go.AddComponent<Image>();
        img.color = color;
        var s = sprite != null ? sprite : ButtonSprite;
        if (s != null)
        {
            img.sprite = s;
            img.type = UnityEngine.UI.Image.Type.Sliced;
        }
        return img;
    }

    public static TextMeshProUGUI Text(Transform parent, string name, string value, int size, Color color,
        TextAlignmentOptions align = TextAlignmentOptions.Center)
    {
        var go = NewUIObject(name, parent);
        var t = go.AddComponent<TextMeshProUGUI>();
        t.text = value;
        t.fontSize = size;
        t.color = color;
        t.alignment = align;
        return t;
    }

    public static Button Button(Transform parent, string name, string label, Color bg, Color textColor,
        Vector2 size, System.Action onClick, int fontSize = 22)
    {
        var go = NewUIObject(name, parent);
        var img = go.AddComponent<Image>();
        img.color = bg;
        if (ButtonSprite != null)
        {
            img.sprite = ButtonSprite;
            img.type = UnityEngine.UI.Image.Type.Sliced;
        }

        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        var colors = btn.colors;
        colors.highlightedColor = new Color(1f, 1f, 1f, 0.92f);
        colors.pressedColor = new Color(0.85f, 0.85f, 0.85f, 1f);
        colors.disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.6f);
        btn.colors = colors;

        var rt = Rect(go);
        rt.sizeDelta = size;

        var text = Text(go.transform, "Label", label, fontSize, textColor);
        var trt = text.rectTransform;
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.offsetMin = new Vector2(10f, 4f);
        trt.offsetMax = new Vector2(-10f, -4f);

        if (onClick != null) btn.onClick.AddListener(() => onClick());
        return btn;
    }

    // Растянуть на весь родителя
    public static void Stretch(RectTransform rt, float margin = 0f)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(margin, margin);
        rt.offsetMax = new Vector2(-margin, -margin);
    }

    // Обновить подпись кнопки, созданной через Button()
    public static void SetButtonLabel(Button btn, string label)
    {
        var t = btn.GetComponentInChildren<TextMeshProUGUI>();
        if (t != null) t.text = label;
    }
}
