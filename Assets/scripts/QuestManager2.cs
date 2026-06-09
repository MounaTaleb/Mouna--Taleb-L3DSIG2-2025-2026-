using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// ============================================================
//  QuestManager2.cs  —  OBJECTIVES LVL 2  —  ROUND 1 : SPYWARE
//
//  Q1 — Collect 5 Spybot           goal = 5
//  Q2 — Activate Firewall Defense  goal = 1
//  Q3 — Stop Spyware with a spell  goal = 1
//  Q4 — Eliminate via CMD          goal = 5
//
//  ✅ MODIF : quand toutes les quêtes sont complètes,
//     on appelle RoundManager2.OnRound1Complete()
//     au lieu d'afficher le WinPanel directement.
// ============================================================
public class QuestManager2 : MonoBehaviour
{
    public static QuestManager2 Instance;

    [Header("Panel")]
    public GameObject questPanel;
    public Button     openQuestButton;
    public Button     closeQuestButton;
    public Button     collectButton;

    [Header("Result Panels")]
    public GameObject winPanel;
    public GameObject gameOverPanel;

    [Header("Objectives Progress Texts (TMP)")]
    public TextMeshProUGUI quest1ProgressText;
    public TextMeshProUGUI quest2ProgressText;
    public TextMeshProUGUI quest3ProgressText;
    public TextMeshProUGUI quest4ProgressText;

    [Header("Reward Per Quest")]
    public int rewardPerQuest = 10;

    [Header("Feedback (optional)")]
    public TextMeshProUGUI feedbackText;
    public float           feedbackDuration = 2f;

    [Header("Win Delay (seconds)")]
    public float winDelay = 1.5f;

    // ── Q1 — Collect 5 Spybot ──────────────────────────────────
    private int  q1Current = 0;
    private readonly int q1Goal = 5;
    private bool q1Complete = false;
    private bool q1Claimed  = false;

    // ── Q2 — Activate Firewall Defense ─────────────────────────
    private int  q2Current = 0;
    private readonly int q2Goal = 1;
    private bool q2Complete = false;
    private bool q2Claimed  = false;

    // ── Q3 — Stop Spyware with a spell ─────────────────────────
    private int  q3Current = 0;
    private readonly int q3Goal = 1;
    private bool q3Complete = false;
    private bool q3Claimed  = false;

    // ── Q4 — Eliminate via CMD terminal ────────────────────────
    private int  q4Current = 0;
    private readonly int q4Goal = 5;
    private bool q4Complete = false;
    private bool q4Claimed  = false;

    private Coroutine feedbackCoroutine;
    private bool      winTriggered = false;

    // ════════════════════════════════════════════════════════════
    //  LIFECYCLE
    // ════════════════════════════════════════════════════════════

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        if (questPanel    != null) questPanel   .SetActive(false);
        if (winPanel      != null) winPanel     .SetActive(false);
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (feedbackText  != null) feedbackText .gameObject.SetActive(false);

        if (openQuestButton  != null) openQuestButton .onClick.AddListener(OpenPanel);
        if (closeQuestButton != null) closeQuestButton.onClick.AddListener(ClosePanel);
        if (collectButton    != null) collectButton   .onClick.AddListener(CollectRewards);

        RefreshUI();
    }

    // ════════════════════════════════════════════════════════════
    //  PANEL OPEN / CLOSE
    // ════════════════════════════════════════════════════════════

    public void OpenPanel()
    {
        if (questPanel != null) questPanel.SetActive(true);
        RefreshUI();
    }

    public void ClosePanel()
    {
        if (questPanel != null) questPanel.SetActive(false);
    }

    // ════════════════════════════════════════════════════════════
    //  Q1 — COLLECT SPYBOT
    // ════════════════════════════════════════════════════════════

    public void OnSpybotCollected(int amount = 1)
    {
        if (q1Complete) return;
        q1Current = Mathf.Min(GameManager.spybot, q1Goal);
        if (q1Current >= q1Goal) q1Complete = true;
        Debug.Log($"[QuestManager2] Q1 Spybot : {q1Current}/{q1Goal}");
        RefreshUI();
        CheckWinCondition();
    }

    // ════════════════════════════════════════════════════════════
    //  Q2 — ACTIVATE FIREWALL
    // ════════════════════════════════════════════════════════════

    public void OnFirewallActivated()
    {
        if (q2Complete) return;
        q2Current = Mathf.Min(q2Current + 1, q2Goal);
        if (q2Current >= q2Goal) q2Complete = true;
        Debug.Log($"[QuestManager2] Q2 Firewall : {q2Current}/{q2Goal}");
        RefreshUI();
        CheckWinCondition();
    }

    // ════════════════════════════════════════════════════════════
    //  Q3 — STOP SPYWARE WITH SPELL
    // ════════════════════════════════════════════════════════════

    public void OnSpywareFrozen()
    {
        if (q3Complete) return;
        q3Current = Mathf.Min(q3Current + 1, q3Goal);
        if (q3Current >= q3Goal) q3Complete = true;
        Debug.Log($"[QuestManager2] Q3 Freeze : {q3Current}/{q3Goal}");
        RefreshUI();
        CheckWinCondition();
    }

    // ════════════════════════════════════════════════════════════
    //  Q4 — ELIMINATE VIA CMD
    // ════════════════════════════════════════════════════════════

    public void OnSpywareEliminatedViaCMD()
    {
        if (q4Complete) return;
        q4Current = Mathf.Min(q4Current + 1, q4Goal);
        if (q4Current >= q4Goal) q4Complete = true;
        Debug.Log($"[QuestManager2] Q4 CMD : {q4Current}/{q4Goal}");
        RefreshUI();
        CheckWinCondition();
    }

    // ════════════════════════════════════════════════════════════
    //  WIN CONDITION  ✅ MODIFIÉ
    //  → Ne montre plus le WinPanel directement.
    //  → Délègue à RoundManager2 pour lancer le Round 2.
    // ════════════════════════════════════════════════════════════

    public bool AreAllQuestsComplete()
        => q1Complete && q2Complete && q3Complete && q4Complete;

    void CheckWinCondition()
    {
        if (!AreAllQuestsComplete() || winTriggered) return;

        TimerManager2 timer = FindObjectOfType<TimerManager2>();
        if (timer != null && timer.GetCurrentTime() > 0f)
        {
            winTriggered = true;

            if (!gameObject.activeInHierarchy)
                NotifyRoundComplete();
            else
                StartCoroutine(RoundCompleteDelayed(winDelay));
        }
    }

    IEnumerator RoundCompleteDelayed(float delay)
    {
        yield return new WaitForSeconds(delay);
        TimerManager2 timer = FindObjectOfType<TimerManager2>();
        if (timer != null && timer.GetCurrentTime() > 0f)
            NotifyRoundComplete();
        else
            winTriggered = false;
    }

    /// <summary>
    /// Round 1 terminé : on informe RoundManager2.
    /// Le WinPanel ne s'affiche PAS ici.
    /// </summary>
    void NotifyRoundComplete()
    {
        // Ferme le quest panel du Round 1
        if (questPanel != null) questPanel.SetActive(false);

        if (RoundManager2.Instance != null)
        {
            // RoundManager2 gère la transition et le WinPanel final
            RoundManager2.Instance.OnRound1Complete();
        }
        else
        {
            // Fallback : si pas de RoundManager2, affiche quand même le WinPanel
            Debug.LogWarning("[QuestManager2] RoundManager2 introuvable — fallback WinPanel");
            TriggerWin();
        }
    }

    // ════════════════════════════════════════════════════════════
    //  TRIGGER WIN / GAME OVER  (fallback uniquement)
    // ════════════════════════════════════════════════════════════

    public void TriggerWin()
    {
        TimerManager2 timer = FindObjectOfType<TimerManager2>();
        if (timer != null) timer.StopTimer();

        if (questPanel    != null) questPanel   .SetActive(false);
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (winPanel      != null) winPanel     .SetActive(true);

        Debug.Log("[QuestManager2] 🏆 WIN (fallback) !");
    }

    public void TriggerGameOver()
    {
        if (questPanel    != null) questPanel   .SetActive(false);
        if (winPanel      != null) winPanel     .SetActive(false);
        if (gameOverPanel != null) gameOverPanel.SetActive(true);

        Debug.Log("[QuestManager2] 💀 GAME OVER.");
    }

    // ════════════════════════════════════════════════════════════
    //  COLLECT REWARDS
    // ════════════════════════════════════════════════════════════

    void CollectRewards()
    {
        bool any = false;
        if (q1Complete && !q1Claimed) { q1Claimed = true; GrantReward("Collect Spybot");             any = true; }
        if (q2Complete && !q2Claimed) { q2Claimed = true; GrantReward("Activate Firewall Defense");  any = true; }
        if (q3Complete && !q3Claimed) { q3Claimed = true; GrantReward("Stop Spyware with a spell");  any = true; }
        if (q4Complete && !q4Claimed) { q4Claimed = true; GrantReward("Eliminate via CMD terminal"); any = true; }
        if (!any) ShowFeedback("No quest completed yet!");
        RefreshUI();
    }

    void GrantReward(string questName)
    {
        if (GameManager.Instance != null)
            GameManager.Instance.AddCoin(rewardPerQuest);
        ShowFeedback($"[OK] {questName} — +{rewardPerQuest} coins!");
    }

    // ════════════════════════════════════════════════════════════
    //  REFRESH UI
    // ════════════════════════════════════════════════════════════

    void RefreshUI()
    {
        SetProgressText(quest1ProgressText, q1Current, q1Goal, q1Claimed);
        SetProgressText(quest2ProgressText, q2Current, q2Goal, q2Claimed);
        SetProgressText(quest3ProgressText, q3Current, q3Goal, q3Claimed);
        SetProgressText(quest4ProgressText, q4Current, q4Goal, q4Claimed);

        if (collectButton != null)
            collectButton.interactable =
                (q1Complete && !q1Claimed) ||
                (q2Complete && !q2Claimed) ||
                (q3Complete && !q3Claimed) ||
                (q4Complete && !q4Claimed);
    }

    void SetProgressText(TextMeshProUGUI tmp, int current, int goal, bool claimed)
    {
        if (tmp == null) return;
        tmp.text = claimed ? $"OK {goal}/{goal}" : $"{current}/{goal}";
    }

    // ════════════════════════════════════════════════════════════
    //  FEEDBACK
    // ════════════════════════════════════════════════════════════

    void ShowFeedback(string msg)
    {
        if (feedbackText == null) return;
        if (!gameObject.activeInHierarchy) return;
        if (feedbackCoroutine != null) StopCoroutine(feedbackCoroutine);
        feedbackCoroutine = StartCoroutine(FeedbackRoutine(msg));
    }

    IEnumerator FeedbackRoutine(string msg)
    {
        feedbackText.text = msg;
        feedbackText.gameObject.SetActive(true);
        yield return new WaitForSeconds(feedbackDuration);
        feedbackText.gameObject.SetActive(false);
    }

    // ════════════════════════════════════════════════════════════
    //  NEXT LEVEL
    // ════════════════════════════════════════════════════════════

    public void GoToNextLevel()
    {
        PlayerPrefs.SetInt("ScoreFromLevel2", GameManager.score);
        PlayerPrefs.Save();
        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex + 1);
    }
}