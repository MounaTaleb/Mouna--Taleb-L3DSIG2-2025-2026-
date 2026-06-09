using UnityEngine;
using TMPro;
using System.Collections;

public class FloatingTextSpawner : MonoBehaviour
{
    public static FloatingTextSpawner Instance;
    public Canvas canvas;
    public TextMeshProUGUI scoreText;

    void Awake()
    {
        // ✅ Singleton sécurisé
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    public void ShowScorePopup(string message, Color color)
    {
        if (canvas == null || scoreText == null)
        {
            Debug.LogError("FloatingTextSpawner: canvas ou scoreText est NULL !");
            return;
        }
        StartCoroutine(SpawnPopup(message, color));
    }

    IEnumerator SpawnPopup(string message, Color color)
    {
        GameObject go = new GameObject("FloatingText");
        go.transform.SetParent(canvas.transform, false);

        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = message;
        tmp.color = color;
        tmp.fontSize = 40;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.raycastTarget = false;

        RectTransform rect = go.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(200f, 80f);

        RectTransform scoreRect = scoreText.GetComponent<RectTransform>();
        Vector3 spawnPos = scoreRect.position;
        spawnPos.x += 160f;
        rect.position = spawnPos;

        float duration = 1.5f;
        float elapsed = 0f;
        Vector3 startPos = rect.position;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            rect.position = startPos + new Vector3(0f, 100f * t, 0f);
            float alpha = t < 0.4f ? 1f : 1f - ((t - 0.4f) / 0.6f);
            Color c = color;
            c.a = alpha;
            tmp.color = c;

            yield return null;
        }

        Destroy(go);
    }
}