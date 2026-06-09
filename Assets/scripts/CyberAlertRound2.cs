using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CyberAlertRound2 : MonoBehaviour
{
    [Header("UI")]
    public TextMeshProUGUI alertText;
    public Image           alertBackground;
    public TextMeshProUGUI roundText;

    [Header("Audio")]
    public AudioClip typeSound;
    public AudioClip round2Sound;
    public AudioClip glitchSound;
    private AudioSource audioSource;

    [Header("Colors Round 2")]
    public Color cyberMagenta = new Color(1f, 0f, 0.8f);
    public Color cyberRed     = new Color(1f, 0.2f, 0.2f);
    public Color cyberOrange  = new Color(1f, 0.5f, 0f);
    public Color cyberYellow  = new Color(1f, 0.9f, 0f);
    public Color cyberCyan    = new Color(0f, 1f, 0.95f);

    [Header("Timing")]
    public float typeSpeed    = 0.03f;
    public float fadeDuration = 0.6f;

    public static CyberAlertRound2 Instance;

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
        if (alertText != null)       alertText.gameObject.SetActive(false);
        if (alertBackground != null) alertBackground.gameObject.SetActive(false);
        if (roundText != null)       roundText.gameObject.SetActive(false);

        StartCoroutine(ProcessQueue());
    }

    // ════════════════════════════════════════════════════════════
    //  POINT D'ENTRÉE — appelé par RoundManager2
    // ════════════════════════════════════════════════════════════

    public IEnumerator PlayRound2Intro()
    {
        if (alertText != null)       alertText.gameObject.SetActive(true);
        if (alertBackground != null) alertBackground.gameObject.SetActive(true);
        if (roundText != null)       roundText.gameObject.SetActive(true);

        SetAlpha(alertText, 0);
        SetAlpha(roundText, 0);
        if (alertBackground) SetImageAlpha(alertBackground, 0);

        queue.Clear();

        yield return StartCoroutine(PlayRound2Animation());

        StartCoroutine(Round2AlertSequence());
    }

    // ════════════════════════════════════════════════════════════
    //  ANIMATION ROUND 2
    // ════════════════════════════════════════════════════════════

    IEnumerator PlayRound2Animation()
    {
        string title      = "ROUND 2";
        Color  roundColor = cyberMagenta;
        Color  pulseTo    = cyberRed;

        roundText.transform.localPosition = Vector3.zero;
        roundText.text = "";
        SetAlpha(roundText, 0);

        // 1. GLITCH BUILD
        float buildTime = 0.8f;
        float t = 0f;
        roundText.color = roundColor;
        SetAlpha(roundText, 1);

        while (t < buildTime)
        {
            t += Time.deltaTime;
            float p        = t / buildTime;
            int   revealed = Mathf.FloorToInt(p * title.Length);

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < revealed; i++)            sb.Append(title[i]);
            for (int i = revealed; i < title.Length; i++) sb.Append(RandomChar());

            roundText.text = sb.ToString();

            if (glitchSound && Random.value > 0.85f)
                audioSource.PlayOneShot(glitchSound, 0.03f);

            yield return new WaitForSeconds(0.016f);
        }

        // 2. LOCK IN — ROUND 2 seulement
        roundText.text  = title;
        roundText.color = roundColor;
        if (round2Sound != null) audioSource.PlayOneShot(round2Sound);

        yield return StartCoroutine(MicroShake(0.25f));

        // 3. PULSE HOLD
        Vector3 origin = roundText.transform.localPosition;
        t = 0f;
        while (t < 1.5f)
        {
            t += Time.deltaTime;
            SetAlpha(roundText, 0.88f + Mathf.Sin(t * 6f) * 0.12f);
            roundText.color = Color.Lerp(roundColor, pulseTo, Mathf.Sin(t * 2f) * 0.08f);
            roundText.transform.localPosition = (Random.value > 0.95f)
                ? origin + new Vector3(Random.Range(-2f, 2f), 0, 0)
                : origin;
            yield return null;
        }

        roundText.transform.localPosition = origin;

        // 4. FADE OUT
        t = 0f;
        while (t < 0.4f)
        {
            t += Time.deltaTime;
            SetAlpha(roundText, 1f - (t / 0.4f));
            yield return null;
        }

        SetAlpha(roundText, 0);
        roundText.text = "";
    }

    // ════════════════════════════════════════════════════════════
    //  ALERTES SÉQUENCE ROUND 2
    // ════════════════════════════════════════════════════════════

    IEnumerator Round2AlertSequence()
    {
        yield return new WaitForSeconds(0.3f);

        Enqueue("[CRITICAL] RANSOMWARE DETECTED",         cyberRed,     4f);
        Enqueue("[SYS] FILES ENCRYPTION STARTED",         cyberMagenta, 4f);
        Enqueue("[WARNING] BACKUP ACCESS BLOCKED",        cyberOrange,  3f);
        Enqueue("[NET] RANSOM SERVER CONTACTED",          cyberRed,     4f);
        Enqueue("[CRITICAL] SYSTEM COMPROMISE IMMINENT",  cyberMagenta, 4f);
    }

    // ════════════════════════════════════════════════════════════
    //  ALERT TRIGGERS Round 2
    // ════════════════════════════════════════════════════════════

    public void ShowFileEncryptedAlert()
    {
        Enqueue("[RANSOMWARE] FILE ENCRYPTED",  cyberRed,     3f);
        Enqueue("[SYS] RECOVERY KEY DELETED",   cyberMagenta, 3f);
    }

    public void ShowFileDecryptedAlert()
    {
        Enqueue("[OK] FILE DECRYPTION SUCCESSFUL", cyberCyan, 3f);
    }

    public void ShowEncryptionBlockedAlert()
    {
        Enqueue("[DEFENSE] ENCRYPTION PROCESS KILLED", cyberCyan,   3f);
        Enqueue("[SYS] FIREWALL RULE APPLIED",         cyberYellow, 3f);
    }

    public void ShowRansomwareNeutralizedAlert()
    {
        Enqueue("[NEUTRALIZED] RANSOMWARE PROCESS TERMINATED", cyberCyan, 4f);
    }

    public void ShowSystemRestoredAlert()
    {
        Enqueue("[CMD] SYSTEM RESTORE POINT APPLIED", cyberCyan,   3f);
        Enqueue("[SYS] INTEGRITY CHECK PASSED",       cyberYellow, 3f);
    }

    // ════════════════════════════════════════════════════════════
    //  QUEUE
    // ════════════════════════════════════════════════════════════

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
            if (Random.value > 0.85f)
                SetAlpha(alertText, Random.Range(0.4f, 1f));
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

    // ════════════════════════════════════════════════════════════
    //  HELPERS
    // ════════════════════════════════════════════════════════════

    IEnumerator MicroShake(float d)
    {
        Vector3 origin = roundText.transform.localPosition;
        float t = 0f;
        while (t < d)
        {
            t += Time.deltaTime;
            float s = Mathf.Lerp(2f, 0f, t / d);
            roundText.transform.localPosition =
                origin + new Vector3(Random.Range(-s, s), Random.Range(-s, s), 0);
            yield return null;
        }
        roundText.transform.localPosition = origin;
    }

    char RandomChar()
    {
        string chars = "#@%&!?XZ01";
        return chars[Random.Range(0, chars.Length)];
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