using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

// ============================================================
//  SpybotFirewallQuizManager.cs
//  FIX : CompleteObjective + ShowFirewallActivatedAlert supprimés
//        → remplacés par ShowDataExfilAlert + QuestManager2 direct
// ============================================================
public class SpybotFirewallQuizManager : MonoBehaviour
{
    public static SpybotFirewallQuizManager Instance;

    // ═══════════════════════════════════════════
    //  PANEL 1 — SPYBOT QUIZ
    // ═══════════════════════════════════════════
    [Header("Panel 1 — Spybot Quiz")]
    public GameObject      quizPanel1;
    public TextMeshProUGUI questionText1;
    public Button[]        answerButtons1 = new Button[4];

    // ═══════════════════════════════════════════
    //  PANEL 2 — FIREWALL QUIZ
    // ═══════════════════════════════════════════
    [Header("Panel 2 — Firewall Quiz")]
    public GameObject      quizPanel2;
    public TextMeshProUGUI questionText2;
    public Button[]        answerButtons2 = new Button[4];

    // ═══════════════════════════════════════════
    //  PAREFEU
    // ═══════════════════════════════════════════
    [Header("Parefeu — murs à activer")]
    public GameObject parefeuParent;
    public float      firewallDuration = 15f;

    // ═══════════════════════════════════════════
    //  AUDIO
    // ═══════════════════════════════════════════
    [Header("Audio")]
    public AudioClip  correctClip;
    public AudioClip  wrongClip;
    private AudioSource _audio;

    // ═══════════════════════════════════════════
    //  COULEURS DE FEEDBACK
    // ═══════════════════════════════════════════
    [Header("Couleurs feedback")]
    public Color correctColor = new Color(0f,  0.85f, 0.3f);
    public Color wrongColor   = new Color(0.9f, 0.1f, 0.1f);
    public Color defaultColor = new Color(0f,  0.75f, 0.85f);

    // ═══════════════════════════════════════════
    //  QUESTIONS
    // ═══════════════════════════════════════════

    // ── Quiz 1 — Spybot ──
    private readonly string   _q1Text = "What does spyware do in a system?";
    private readonly string[] _q1Answers = {
        "A:  Protects files",
        "B:  Monitors user activity secretly",
        "C:  Speeds up the computer",
        "D:  Deletes viruses"
    };
    private const int _q1Correct = 1;

    // ── Quiz 2 — Firewall ──
    private readonly string   _q2Text = "What is the purpose of a firewall?";
    private readonly string[] _q2Answers = {
        "A:  Clean files",
        "B:  Block unauthorized network access",
        "C:  Charge battery",
        "D:  Update software"
    };
    private const int _q2Correct = 1;

    // ═══════════════════════════════════════════
    //  ÉTAT INTERNE
    // ═══════════════════════════════════════════
    private int  _refundCost;
    private bool _waitingForPanel2;

    // ═══════════════════════════════════════════
    //  LIFECYCLE
    // ═══════════════════════════════════════════

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        _audio = gameObject.AddComponent<AudioSource>();

        if (quizPanel1 != null) quizPanel1.SetActive(false);
        if (quizPanel2 != null) quizPanel2.SetActive(false);

        for (int i = 0; i < answerButtons1.Length; i++)
        {
            int idx = i;
            if (answerButtons1[i] != null)
                answerButtons1[i].onClick.AddListener(() => OnAnswer1(idx));
        }

        for (int i = 0; i < answerButtons2.Length; i++)
        {
            int idx = i;
            if (answerButtons2[i] != null)
                answerButtons2[i].onClick.AddListener(() => OnAnswer2(idx));
        }

        if (parefeuParent != null) parefeuParent.SetActive(false);
    }

    // ═══════════════════════════════════════════
    //  POINT D'ENTRÉE
    // ═══════════════════════════════════════════

    public void ActivateFirewall(float duration, int refundCost = 35)
    {
        firewallDuration  = duration;
        _refundCost       = refundCost;
        _waitingForPanel2 = false;
        OpenPanel1();
    }

    // ═══════════════════════════════════════════
    //  PANEL 1 — SPYBOT
    // ═══════════════════════════════════════════

    void OpenPanel1()
    {
        if (questionText1 != null) questionText1.text = _q1Text;
        PopulateButtons(answerButtons1, _q1Answers);
        ResetButtonColors(answerButtons1);
        SetButtonsInteractable(answerButtons1, true);
        if (quizPanel1 != null) quizPanel1.SetActive(true);
    }

    void OnAnswer1(int selected)
    {
        SetButtonsInteractable(answerButtons1, false);

        if (selected == _q1Correct)
        {
            FlashButton(answerButtons1[selected], correctColor);
            PlaySound(correctClip);
            // Spybot collecté → notifier QuestManager2
            QuestManager2.Instance?.OnSpybotCollected(1);
            StartCoroutine(TransitionToPanel2());
        }
        else
        {
            FlashButton(answerButtons1[selected], wrongColor);
            FlashButton(answerButtons1[_q1Correct], correctColor);
            PlaySound(wrongClip);
            GameManager.Instance?.AddCoin(_refundCost);
            StartCoroutine(ClosePanel1AfterDelay(1.8f));
        }
    }

    IEnumerator TransitionToPanel2()
    {
        yield return new WaitForSeconds(1.2f);
        if (quizPanel1 != null) quizPanel1.SetActive(false);
        OpenPanel2();
    }

    IEnumerator ClosePanel1AfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (quizPanel1 != null) quizPanel1.SetActive(false);
    }

    // ═══════════════════════════════════════════
    //  PANEL 2 — FIREWALL
    // ═══════════════════════════════════════════

    void OpenPanel2()
    {
        if (questionText2 != null) questionText2.text = _q2Text;
        PopulateButtons(answerButtons2, _q2Answers);
        ResetButtonColors(answerButtons2);
        SetButtonsInteractable(answerButtons2, true);
        if (quizPanel2 != null) quizPanel2.SetActive(true);
    }

    void OnAnswer2(int selected)
    {
        SetButtonsInteractable(answerButtons2, false);

        if (selected == _q2Correct)
        {
            FlashButton(answerButtons2[selected], correctColor);
            PlaySound(correctClip);
            StartCoroutine(ActivateFirewallAfterDelay(1.2f));
        }
        else
        {
            FlashButton(answerButtons2[selected], wrongColor);
            FlashButton(answerButtons2[_q2Correct], correctColor);
            PlaySound(wrongClip);
            GameManager.Instance?.AddCoin(_refundCost);
            StartCoroutine(ClosePanel2AfterDelay(1.8f));
        }
    }

    IEnumerator ActivateFirewallAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (quizPanel2 != null) quizPanel2.SetActive(false);
        EnableFirewall();
    }

    IEnumerator ClosePanel2AfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (quizPanel2 != null) quizPanel2.SetActive(false);
    }

    // ═══════════════════════════════════════════
    //  PAREFEU
    // ═══════════════════════════════════════════

    void EnableFirewall()
    {
        SetParefeuActive(true);
        Debug.Log("[SpybotFirewallQuizManager] Parefeu activé DÉFINITIVEMENT.");

        // ── Alerte data exfil (remplace ShowFirewallActivatedAlert) ──
        CyberAlertSystem.Instance?.ShowDataExfilAlert();

        // ── Objectif Q2 Firewall complété ──
        QuestManager2.Instance?.OnFirewallActivated();
    }

    void SetParefeuActive(bool active)
    {
        if (parefeuParent == null)
        {
            Debug.LogWarning("[SpybotFirewallQuizManager] parefeuParent non assigné !");
            return;
        }

        if (active)
        {
            parefeuParent.SetActive(true);
            foreach (Transform child in parefeuParent.transform)
                child.gameObject.SetActive(true);
        }
        else
        {
            foreach (Transform child in parefeuParent.transform)
                child.gameObject.SetActive(false);
            parefeuParent.SetActive(false);
        }

        Debug.Log($"[SpybotFirewallQuizManager] Parefeu → {(active ? "ACTIVÉ ✅" : "désactivé")}");
    }

    public bool IsFirewallActive => parefeuParent != null && parefeuParent.activeSelf;

    // ═══════════════════════════════════════════
    //  HELPERS UI
    // ═══════════════════════════════════════════

    static void PopulateButtons(Button[] buttons, string[] labels)
    {
        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i] == null) continue;
            var tmp = buttons[i].GetComponentInChildren<TextMeshProUGUI>();
            if (tmp != null && i < labels.Length) tmp.text = labels[i];
        }
    }

    void ResetButtonColors(Button[] buttons)
    {
        foreach (var btn in buttons)
        {
            if (btn == null) continue;
            SetButtonColor(btn, defaultColor);
        }
    }

    static void SetButtonsInteractable(Button[] buttons, bool state)
    {
        foreach (var btn in buttons)
            if (btn != null) btn.interactable = state;
    }

    void FlashButton(Button btn, Color color)
    {
        if (btn == null) return;
        SetButtonColor(btn, color);
    }

    static void SetButtonColor(Button btn, Color color)
    {
        if (btn == null) return;
        var cb = btn.colors;
        cb.normalColor   = color;
        cb.selectedColor = color;
        cb.disabledColor = color * 0.7f;
        btn.colors       = cb;
    }

    void PlaySound(AudioClip clip)
    {
        if (_audio != null && clip != null)
            _audio.PlayOneShot(clip);
    }
}