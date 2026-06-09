using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// ============================================================
//  QuestManager2_Round2.cs  —  ROUND 2 : RANSOMWARE (3 OBJECTIFS)
//
//  Q1 — Deactivate WiFi (hack 1 WifiHackNode)       goal = 1
//  Q2 — Collect 20 Backups of 3 types               goal = 20 total (min 1 per type)
//  Q3 — Quiz (1 Quiz WiFi + 1 Refus de rançon)      goal = 2
//
//  ✅ WIN       : tous objectifs complétés + timer > 0 + slider HP > 0
//  ❌ GAME OVER : timer expire OU slider HP tombe à 0
// ============================================================
public class QuestManager2_Round2 : MonoBehaviour
{
    public static QuestManager2_Round2 Instance;

    [Header("Panel")]
    public GameObject questPanel;
    public Button     openQuestButton;
    public Button     closeQuestButton;
    public Button     collectButton;

    [Header("Result Panels (partagés avec Round 1)")]
    public GameObject winPanel;
    public GameObject gameOverPanel;

    [Header("Objectives Progress Texts (TMP)")]
    public TextMeshProUGUI quest1ProgressText;   // Deactivate WiFi
    public TextMeshProUGUI quest2ProgressText;   // Collect 20 Backups
    public TextMeshProUGUI quest3ProgressText;   // Quiz & Ransom (2/2)

    [Header("Reward Per Quest")]
    public int rewardPerQuest = 10;

    [Header("Feedback (optional)")]
    public TextMeshProUGUI feedbackText;
    public float           feedbackDuration = 2f;

    [Header("Win Delay (seconds)")]
    public float winDelay = 1.5f;

    // ── Q1 — Deactivate WiFi ─────────────────
    private int  q1Current = 0;
    private readonly int q1Goal = 1;
    private bool q1Complete = false;
    private bool q1Claimed  = false;

    // ── Q2 — Collect 20 Backups ───────────────────
    private int  q2TotalCurrent = 0;
    private int[] backupCounts  = new int[3];
    private readonly int q2TotalGoal   = 20;
    private readonly int q2TypesNeeded = 3;
    private bool q2Complete = false;
    private bool q2Claimed  = false;

    // ── Q3 — Quiz & Rançon (2/2) ─────────────────
    private int  q3Current = 0;
    private readonly int q3Goal = 2; // 1 = Quiz Wifi | 1 = Refuser Rançon
    private bool q3Complete = false;
    private bool q3Claimed  = false;

    private Coroutine feedbackCoroutine;
    private bool      winTriggered   = false;
    private bool      gameOverTriggered = false;   // ✅ garde-fou Game Over
    private bool      roundStarted   = false;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
    }

    void Start()
    {
        if (questPanel   != null) questPanel  .SetActive(false);
        if (feedbackText != null) feedbackText.gameObject.SetActive(false);

        if (openQuestButton  != null) openQuestButton .onClick.AddListener(OpenPanel);
        if (closeQuestButton != null) closeQuestButton.onClick.AddListener(ClosePanel);
        if (collectButton    != null) collectButton   .onClick.AddListener(CollectRewards);

        RefreshUI();
    }

    // ══════════════════════════════════════════
    //  DÉMARRAGE DU ROUND
    // ══════════════════════════════════════════
    public void StartRound()
    {
        if (roundStarted) return;
        roundStarted = true;

        q1Current      = 0;
        q2TotalCurrent = 0;
        q3Current      = 0;
        backupCounts   = new int[3];

        q1Complete = q2Complete = q3Complete = false;
        q1Claimed  = q2Claimed  = q3Claimed  = false;

        winTriggered      = false;
        gameOverTriggered = false;

        RefreshUI();
    }

    // ══════════════════════════════════════════
    //  PANEL QUÊTES
    // ══════════════════════════════════════════
    public void OpenPanel()
    {
        if (!roundStarted) return;
        if (questPanel != null) questPanel.SetActive(true);
        RefreshUI();
    }

    public void ClosePanel()
    {
        if (questPanel != null) questPanel.SetActive(false);
    }

    // ══════════════════════════════════════════
    //  ÉVÉNEMENTS DE JEU
    // ══════════════════════════════════════════
    public void OnWifiDeactivated()
    {
        if (!roundStarted || q1Complete) return;
        q1Current = Mathf.Min(q1Current + 1, q1Goal);
        if (q1Current >= q1Goal) q1Complete = true;
        RefreshUI();
        CheckWinCondition();
    }

    public void OnBackupCollected(int backupType = 0, int amount = 1)
    {
        if (!roundStarted || q2Complete) return;

        backupType = Mathf.Clamp(backupType, 0, 2);
        backupCounts[backupType] += amount;
        q2TotalCurrent = Mathf.Min(q2TotalCurrent + amount, q2TotalGoal);

        int typesUnlocked = 0;
        foreach (int count in backupCounts)
            if (count > 0) typesUnlocked++;

        if (q2TotalCurrent >= q2TotalGoal && typesUnlocked >= q2TypesNeeded)
            q2Complete = true;

        RefreshUI();
        CheckWinCondition();
    }

    // Appelé par le système de Quiz (ex: Quiz Wifi)
    public void OnQuizAnsweredCorrectly()
    {
        if (!roundStarted || q3Complete) return;
        q3Current = Mathf.Min(q3Current + 1, q3Goal);
        if (q3Current >= q3Goal) q3Complete = true;
        RefreshUI();
        CheckWinCondition();
    }

    // Appelé par le RansomPanelManager quand le joueur refuse la rançon
    public void OnRansomDeclined()
    {
        if (!roundStarted || q3Complete) return;
        q3Current = Mathf.Min(q3Current + 1, q3Goal);
        if (q3Current >= q3Goal) q3Complete = true;
        RefreshUI();
        CheckWinCondition();
    }

    // ══════════════════════════════════════════
    //  VÉRIFICATION WIN / GAME OVER
    // ══════════════════════════════════════════
    public bool AreAllQuestsComplete() => q1Complete && q2Complete && q3Complete;

    void CheckWinCondition()
    {
        // Déjà résolu → rien à faire
        if (winTriggered || gameOverTriggered) return;

        // Pas encore tous complétés → attendre
        if (!AreAllQuestsComplete()) return;

        // ── Vérification timer ───────────────────────────
        TimerManager2 timer = FindObjectOfType<TimerManager2>();
        bool timerOk  = timer != null && timer.GetCurrentTime() > 0f;

        // ── Vérification slider HP ───────────────────────
        bool healthOk = SystemHealth1.Instance != null && SystemHealth1.Instance.IsAlive;

        if (timerOk && healthOk)
        {
            // ✅ Tous les objectifs + timer ok + HP ok → WIN
            winTriggered = true;
            StartCoroutine(WinDelayed(winDelay));
        }
        else
        {
            // ❌ Timer expiré OU slider à 0 → GAME OVER
            Debug.Log($"[QuestManager2] Objectifs complétés mais : timerOk={timerOk} healthOk={healthOk} → GAME OVER");
            TriggerGameOver();
        }
    }

    IEnumerator WinDelayed(float delay)
    {
        yield return new WaitForSeconds(delay);

        // Double-vérification au moment de l'affichage
        TimerManager2 timer = FindObjectOfType<TimerManager2>();
        bool timerOk  = timer != null && timer.GetCurrentTime() > 0f;
        bool healthOk = SystemHealth1.Instance != null && SystemHealth1.Instance.IsAlive;

        if (timerOk && healthOk)
        {
            Debug.Log("[QuestManager2] ✅ WIN FINAL !");
            RoundManager2.Instance?.OnRound2Complete();
        }
        else
        {
            // La situation a changé pendant le délai → GAME OVER
            winTriggered = false;
            Debug.Log("[QuestManager2] Situation changée pendant le délai → GAME OVER");
            TriggerGameOver();
        }
    }

    // ══════════════════════════════════════════
    //  AFFICHAGE PANELS RÉSULTAT
    // ══════════════════════════════════════════
    public void TriggerFinalWin()
    {
        if (gameOverTriggered) return;   // priorité Game Over

        TimerManager2 timer = FindObjectOfType<TimerManager2>();
        if (timer != null) timer.StopTimer();

        if (questPanel    != null) questPanel   .SetActive(false);
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (winPanel      != null) winPanel     .SetActive(true);

        Debug.Log("[QuestManager2] 🏆 WIN PANEL affiché");
    }

    public void TriggerGameOver()
    {
        if (gameOverTriggered) return;   // n'affiche qu'une seule fois
        gameOverTriggered = true;
        winTriggered      = false;

        // Arrête le timer
        TimerManager2 timer = FindObjectOfType<TimerManager2>();
        if (timer != null) timer.StopTimer();

        if (questPanel != null) questPanel .SetActive(false);
        if (winPanel   != null) winPanel   .SetActive(false);
        if (gameOverPanel != null) gameOverPanel.SetActive(true);

        Debug.Log("[QuestManager2] 💀 GAME OVER PANEL affiché");
    }

    // ══════════════════════════════════════════
    //  RÉCOMPENSES
    // ══════════════════════════════════════════
    void CollectRewards()
    {
        bool any = false;
        if (q1Complete && !q1Claimed) { q1Claimed = true; GrantReward("Deactivate WiFi");              any = true; }
        if (q2Complete && !q2Claimed) { q2Claimed = true; GrantReward("Collect 20 Backups (3 types)"); any = true; }
        if (q3Complete && !q3Claimed) { q3Claimed = true; GrantReward("Quiz & Ransom Declined");       any = true; }

        if (!any) ShowFeedback("No quest completed yet!");
        RefreshUI();
    }

    void GrantReward(string questName)
    {
        // Décommenter si GameManager existe dans votre projet :
        // if (GameManager.Instance != null)
        //     GameManager.Instance.AddCoin(rewardPerQuest);

        ShowFeedback($"[OK] {questName} — +{rewardPerQuest} coins!");
    }

    // ══════════════════════════════════════════
    //  UI
    // ══════════════════════════════════════════
    void RefreshUI()
    {
        SetProgressText(quest1ProgressText, q1Current,      q1Goal,      q1Claimed);
        SetProgressText(quest2ProgressText, q2TotalCurrent, q2TotalGoal, q2Claimed);
        SetProgressText(quest3ProgressText, q3Current,      q3Goal,      q3Claimed);

        if (collectButton != null)
            collectButton.interactable =
                (q1Complete && !q1Claimed) ||
                (q2Complete && !q2Claimed) ||
                (q3Complete && !q3Claimed);
    }

    void SetProgressText(TextMeshProUGUI tmp, int current, int goal, bool claimed)
    {
        if (tmp == null) return;
        tmp.text = claimed ? $"✅ {goal}/{goal}" : $"{current}/{goal}";
    }

    void ShowFeedback(string msg)
    {
        if (feedbackText == null || !gameObject.activeInHierarchy) return;
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

    // ══════════════════════════════════════════
    //  NAVIGATION
    // ══════════════════════════════════════════
    public void GoToNextLevel()
    {
        // PlayerPrefs.SetInt("ScoreFromLevel2", GameManager.score);
        // PlayerPrefs.Save();
        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex + 1);
    }
}