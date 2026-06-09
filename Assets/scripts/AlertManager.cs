using UnityEngine;
using TMPro;
using System.Collections;

public class AlertManager : MonoBehaviour
{
    public static AlertManager Instance;

    [Header("UI")]
    public TextMeshProUGUI alertText;
    private RectTransform rect;

    [Header("Settings")]
    public float displayDuration = 2.5f;
    public float typeSpeed = 0.02f;

    private Coroutine routine;
    private Vector3 basePos;

    // 🎨 Couleurs soft (pas agressives)
    private Color redSoft = new Color32(255, 76, 76, 255);
    private Color grayText = new Color32(220, 220, 220, 255);

    private string[] messages = {
        "Access terminal to stop attack",
        "Secure system immediately",
        "Virus detected",
        "Protect your data"
    };

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        rect = alertText.GetComponent<RectTransform>();
        basePos = rect.localPosition;

        // ✅ STYLE SIMPLE & PRO
        alertText.alignment = TextAlignmentOptions.Center;
        alertText.fontSize = 30; // 👈 taille correcte
        alertText.enableWordWrapping = false;

        // Glow léger
        alertText.outlineWidth = 0.15f;
        alertText.outlineColor = new Color32(255, 76, 76, 120);

        alertText.alpha = 0.95f;

        alertText.gameObject.SetActive(false);
    }

    public void ShowAlert()
    {
        string msg = messages[Random.Range(0, messages.Length)];

        // ✅ TEXTE COURT (important)
        string fullText =
        "<b><color=#FF4C4C>[ ERROR ]</color></b>\n" +
        "<color=#E0E0E0>> " + msg + "</color>";

        if (routine != null) StopCoroutine(routine);
        routine = StartCoroutine(PlayAlert(fullText));
    }

    IEnumerator PlayAlert(string text)
    {
        alertText.gameObject.SetActive(true);
        alertText.text = "";

        string display = "";
        bool isTag = false;

        // ✍️ Typewriter simple
        for (int i = 0; i < text.Length; i++)
        {
            if (text[i] == '<') isTag = true;

            if (isTag)
            {
                display += text[i];
                if (text[i] == '>') isTag = false;
            }
            else
            {
                display += text[i];
                alertText.text = display;
                yield return new WaitForSeconds(typeSpeed);
            }
        }

        // ⏳ Affichage + petit clignotement
        float t = 0f;

        while (t < displayDuration)
        {
            string cursor = (Time.time % 0.6f > 0.3f) ? "_" : " ";
            alertText.text = display + cursor;

            t += 0.1f;
            yield return new WaitForSeconds(0.1f);
        }

        alertText.gameObject.SetActive(false);
        rect.localPosition = basePos;
    }
}