using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

[Serializable]
public class GameUiTheme
{
    public Color panelBackground = new Color(0.06f, 0.08f, 0.12f, 0.97f);
    public Color mainMenuBackground = new Color(0.06f, 0.08f, 0.12f, 0.97f);
    public Color authBackground = new Color(0.07f, 0.10f, 0.14f, 0.97f);
    public Color profileBackground = new Color(0.07f, 0.10f, 0.14f, 0.97f);
    public Color multiplayerBackground = new Color(0.06f, 0.08f, 0.12f, 0.97f);
    public Color mapSelectBackground = new Color(0.06f, 0.08f, 0.12f, 0.97f);
    public Color inventoryBackground = new Color(0.07f, 0.10f, 0.14f, 0.96f);
    public Color shopBackground = new Color(0.07f, 0.10f, 0.14f, 0.97f);
    public Color statsBackground = new Color(0.07f, 0.10f, 0.14f, 0.97f);
    public Color skillTreeBackground = new Color(0.06f, 0.09f, 0.13f, 0.98f);
    public Color pauseBackground = new Color(0.08f, 0.11f, 0.16f, 0.98f);
    public Color resultBackground = new Color(0.08f, 0.10f, 0.14f, 0.97f);
    public Color surface = new Color(0.11f, 0.14f, 0.19f, 1f);
    public Color primaryButton = new Color(0.12f, 0.40f, 0.52f, 1f);
    public Color secondaryButton = new Color(0.18f, 0.23f, 0.30f, 1f);
    public Color successButton = new Color(0.14f, 0.42f, 0.22f, 1f);
    public Color dangerButton = new Color(0.34f, 0.15f, 0.16f, 1f);
    public Color text = new Color(0.95f, 0.97f, 1f, 1f);
    public Color border = new Color(0.78f, 0.88f, 1f, 0.45f);
    [Range(0f, 8f)] public float borderThickness = 2f;

    public Color MutedText(float alpha = 0.82f)
    {
        return new Color(text.r, text.g, text.b, alpha);
    }

    public Color Disabled(Color color, float alpha = 0.75f)
    {
        return new Color(color.r * 0.55f, color.g * 0.55f, color.b * 0.55f, alpha);
    }

    public Color Hover(Color color)
    {
        return Color.Lerp(color, Color.white, 0.12f);
    }

    public Color Pressed(Color color)
    {
        return Color.Lerp(color, Color.black, 0.12f);
    }
}

public static class GameUiThemeRuntime
{
    private static GameUiTheme _theme;

    public static GameUiTheme Current
    {
        get
        {
            if (_theme == null)
                _theme = new GameUiTheme();
            return _theme;
        }
    }

    public static void SetTheme(GameUiTheme theme)
    {
        _theme = theme ?? new GameUiTheme();
    }

    public static void StylePanel(GameObject obj)
    {
        StylePanel(obj, Current.panelBackground, true);
    }

    public static void StyleSurface(GameObject obj)
    {
        StylePanel(obj, Current.surface, true);
    }

    public static void StylePanel(GameObject obj, Color color, bool withBorder)
    {
        if (obj == null) return;
        Image image = obj.GetComponent<Image>();
        if (image == null)
            image = obj.AddComponent<Image>();
        image.color = color;

        if (withBorder)
            ApplyBorder(obj);
    }

    public static void StyleButton(Button button, Image image, Color color)
    {
        if (button == null) return;
        if (image == null)
            image = button.GetComponent<Image>();

        if (image != null)
        {
            image.color = color;
            button.targetGraphic = image;
            ApplyBorder(image.gameObject);
        }

        GameUiTheme theme = Current;
        ColorBlock colors = button.colors;
        colors.normalColor = color;
        colors.highlightedColor = theme.Hover(color);
        colors.pressedColor = theme.Pressed(color);
        colors.selectedColor = theme.Hover(color);
        colors.disabledColor = theme.Disabled(color);
        button.colors = colors;

        TextMeshProUGUI text = button.GetComponentInChildren<TextMeshProUGUI>(true);
        if (text != null)
        {
            text.color = theme.text;
            text.raycastTarget = false;
        }
    }

    public static void ApplyBorder(GameObject obj)
    {
        if (obj == null) return;
        GameUiTheme theme = Current;
        Outline outline = obj.GetComponent<Outline>();
        if (outline == null)
            outline = obj.AddComponent<Outline>();
        outline.effectColor = theme.border;
        outline.effectDistance = new Vector2(theme.borderThickness, -theme.borderThickness);
        outline.useGraphicAlpha = true;
    }
}
