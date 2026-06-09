using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CyberAlertSystem : MonoBehaviour
{
    [Header("UI")]
    public TextMeshProUGUI alertText;
    public Image alertBackground;
    public TextMeshProUGUI roundText;

    [Header("Audio")]
    public AudioClip typeSound;
    public AudioClip round1Sound;
    public AudioClip glitchSound;
    private AudioSource audioSource;

    [Header("Colors")]
    public Color cyberCyan    = new Color(0f, 1f, 0.95f);
    public Color cyberMagenta = new Color(1f, 0f, 0.8f);
    public Color cyberRed     = new Color(1f, 0.2f, 0.2f);
    public Color cyberOrange  = new Color(1f, 0.5f, 0f);
    public Color cyberYellow  = new Color(1f, 0.9f, 0f);

    [Header("Timing")]
    public float typeSpeed    = 0.03f;
    public float fadeDuration = 0.6f;

    public static CyberAlertSystem Instance;

    Queue<AlertRequest> queue = new Queue<AlertRequest>();
    bool showing = false;

    struct AlertRequest
    {
        public string msg;
        public Color  color;
        public float  duration;
    }

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.volume = 0.35f;
    }

    void Start()
    {
        if (PlayerPrefs.GetInt("CurrentRound", 1) == 1)
        {
            if (alertText != null)       alertText.gameObject.SetActive(true);
            if (alertBackground != null) alertBackground.gameObject.SetActive(true);
            if (roundText != null)       roundText.gameObject.SetActive(true);

            SetAlpha(alertText, 0);
            SetAlpha(roundText, 0);
            if (alertBackground) SetImageAlpha(alertBackground, 0);

            StartCoroutine(ProcessQueue());
            StartCoroutine(StartRound1Sequence());
        }
        else
        {
            DisableUI();
            StartCoroutine(ProcessQueue());
        }
    }

    IEnumerator StartRound1Sequence()
    {
        yield return new WaitForSeconds(1.2f);
        yield return StartCoroutine(PlayRound1Animation());
        yield return new WaitForSeconds(0.5f);

        Enqueue("[SYS] CPU SPIKE DETECTED",          cyberRed,    4f);
        Enqueue("[ALERT] MALICIOUS PROCESS RUNNING", cyberOrange, 3f); // "Spyware" changé en "Malicious"
        Enqueue("[NET] SUSPICIOUS OUTBOUND TRAFFIC", cyberYellow, 3f);
    }

    IEnumerator PlayRound1Animation()
    {
        string title = "ROUND 1";
        Color roundColor = cyberCyan;

        roundText.transform.localPosition = Vector3.zero;
        roundText.transform.localScale = Vector3.one * 0.5f; // Commence petit pour l'animation
        roundText.text = title;
        roundText.color = roundColor;
        SetAlpha(roundText, 0);

        // 1. APPARITION AVEC ZOOM
        float animTime = 0.5f;
        float t = 0f;
        if (round1Sound != null) audioSource.PlayOneShot(round1Sound);

        while (t < animTime)
        {
            t += Time.deltaTime;
            float p = t / animTime;
            float curve = Mathf.SmoothStep(0, 1, p); // Animation fluide

            roundText.transform.localScale = Vector3.one * Mathf.Lerp(0.5f, 1.2f, curve);
            SetAlpha(roundText, curve);
            yield return null;
        }

        // Petit rebond vers la taille normale
        roundText.transform.localScale = Vector3.one;
        yield return StartCoroutine(MicroShake(0.2f));

        // 2. PULSE D'ATTENTE (Légère respiration)
        t = 0f;
        while (t < 1.5f)
        {
            t += Time.deltaTime;
            float pulse = 1f + Mathf.Sin(t * 4f) * 0.05f;
            roundText.transform.localScale = Vector3.one * pulse;
            SetAlpha(roundText, 0.8f + Mathf.Sin(t * 4f) * 0.2f);
            yield return null;
        }

        // 3. FADE OUT
        t = 0f;
        while (t < 0.4f)
        {
            t += Time.deltaTime;
            float a = 1f - (t / 0.4f);
            SetAlpha(roundText, a);
            roundText.transform.localScale = Vector3.one * (1f + t); // Zoom sortant
            yield return null;
        }

        SetAlpha(roundText, 0);
        roundText.text = "";
    }

    // --- LE RESTE DU CODE RESTE IDENTIQUE ---
    // (TriggerAttackAlert, ShowDataExfilAlert, DisableUI, Queue, etc.)

    public void TriggerAttackAlert(DataTarget.DataType type)
    {
        switch (type)
        {
            case DataTarget.DataType.Image:
                Enqueue("[ALERT] IMAGE EXFILTRATION",    cyberOrange,  3f); break;
            case DataTarget.DataType.PasswordFile:
                Enqueue("[CRITICAL] PASSWORD STOLEN",    cyberRed,     4f); break;
            case DataTarget.DataType.Location:
                Enqueue("[WARNING] LOCATION TRACKED",    cyberYellow,  3f); break;
            case DataTarget.DataType.BankCard:
                Enqueue("[CRITICAL] BANK DATA BREACHED", cyberMagenta, 4f); break;
        }
    }

    public void ShowDataExfilAlert()
    {
        Enqueue("[NET] DATA SENT TO EXTERNAL SERVER", cyberMagenta, 4f);
        Enqueue("[SYS] TRACE FAILED",                 cyberOrange,  3f);
    }

    public void DisableUI()
    {
        ClearQueue();
        if (alertText != null)       alertText.gameObject.SetActive(false);
        if (alertBackground != null) alertBackground.gameObject.SetActive(false);
        if (roundText != null)       roundText.gameObject.SetActive(false);
    }

    public void Enqueue(string msg, Color color, float duration)
    {
        queue.Enqueue(new AlertRequest { msg = msg, color = color, duration = duration });
    }

    public void ClearQueue() => queue.Clear();

    IEnumerator ProcessQueue()
    {
        while (true)
        {
            if (queue.Count > 0 && !showing)
            {
                var r = queue.Dequeue();
                yield return StartCoroutine(ShowAlert(r.msg, r.color, r.duration));
            }
            yield return null;
        }
    }

    IEnumerator ShowAlert(string msg, Color color, float duration)
    {
        showing = true;
        alertText.text  = "";
        alertText.color = color;

        if (alertBackground)
            yield return StartCoroutine(FadeImage(alertBackground, 0, 0.85f, 0.15f));

        foreach (char c in msg)
        {
            alertText.text += c;
            if (typeSound && c != ' ')
                audioSource.PlayOneShot(typeSound, 0.12f);
            yield return new WaitForSeconds(typeSpeed);
        }

        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            SetAlpha(alertText, 0.85f + Mathf.Sin(t * 8f) * 0.15f);
            yield return null;
        }

        t = 0f;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            float a = 1f - (t / fadeDuration);
            SetAlpha(alertText, a);
            if (alertBackground) SetImageAlpha(alertBackground, a * 0.85f);
            yield return null;
        }

        SetAlpha(alertText, 0);
        if (alertBackground) SetImageAlpha(alertBackground, 0);
        showing = false;
    }

    IEnumerator MicroShake(float d)
    {
        Vector3 origin = roundText.transform.localPosition;
        float t = 0f;
        while (t < d)
        {
            t += Time.deltaTime;
            float s = Mathf.Lerp(2f, 0f, t / d);
            roundText.transform.localPosition = origin + new Vector3(Random.Range(-s, s), Random.Range(-s, s), 0);
            yield return null;
        }
        roundText.transform.localPosition = origin;
    }

    void SetAlpha(TextMeshProUGUI t, float a)
    {
        if (!t) return;
        Color c = t.color; c.a = a; t.color = c;
    }

    void SetImageAlpha(Image i, float a)
    {
        if (!i) return;
        Color c = i.color; c.a = a; i.color = c;
    }

    IEnumerator FadeImage(Image img, float from, float to, float dur)
    {
        float t = 0f;
        while (t < dur)
        {
            t += Time.deltaTime;
            SetImageAlpha(img, Mathf.Lerp(from, to, t / dur));
            yield return null;
        }
    }
}