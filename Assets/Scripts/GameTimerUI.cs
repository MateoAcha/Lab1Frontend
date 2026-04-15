using UnityEngine;
using UnityEngine.UI;

public class GameTimerUI : MonoBehaviour
{
    private Text timerText;

    private void Awake()
    {
        BuildUI();
    }

    private void Update()
    {
        if (timerText == null)
        {
            return;
        }

        float elapsed = Mathf.Max(0f, EnemySpawner.ElapsedTime);
        int minutes = Mathf.FloorToInt(elapsed / 60f);
        int seconds = Mathf.FloorToInt(elapsed % 60f);
        timerText.text = $"{minutes:00}:{seconds:00}";
    }

    private void BuildUI()
    {
        const string canvasName = "RuntimeGameHUD";
        const string timerName = "GameTimerText";

        Canvas canvas = FindHUDCanvas(canvasName);
        if (canvas == null)
        {
            return;
        }

        Transform existing = canvas.transform.Find(timerName);
        GameObject timerObj = existing != null ? existing.gameObject : new GameObject(timerName);
        if (existing == null)
        {
            timerObj.transform.SetParent(canvas.transform, false);
        }

        RectTransform rect = timerObj.GetComponent<RectTransform>();
        if (rect == null)
        {
            rect = timerObj.AddComponent<RectTransform>();
        }

        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.sizeDelta = new Vector2(500f, 100f);
        rect.anchoredPosition = new Vector2(0f, -16f);

        timerText = timerObj.GetComponent<Text>();
        if (timerText == null)
        {
            timerText = timerObj.AddComponent<Text>();
        }

        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (font == null)
        {
            font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        }

        timerText.font = font;
        timerText.fontSize = 28;
        timerText.alignment = TextAnchor.MiddleCenter;
        timerText.color = Color.white;
        timerText.raycastTarget = false;
        timerText.text = "00:00";
    }

    private Canvas FindHUDCanvas(string canvasName)
    {
        GameObject canvasObj = GameObject.Find(canvasName);
        if (canvasObj == null)
        {
            canvasObj = new GameObject(canvasName);
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();
            return canvas;
        }

        Canvas existing = canvasObj.GetComponent<Canvas>();
        if (existing == null)
        {
            existing = canvasObj.AddComponent<Canvas>();
            existing.renderMode = RenderMode.ScreenSpaceOverlay;
        }

        if (canvasObj.GetComponent<CanvasScaler>() == null)
        {
            canvasObj.AddComponent<CanvasScaler>();
        }

        if (canvasObj.GetComponent<GraphicRaycaster>() == null)
        {
            canvasObj.AddComponent<GraphicRaycaster>();
        }

        return existing;
    }
}
