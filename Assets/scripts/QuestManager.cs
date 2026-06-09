using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class QuestManager : MonoBehaviour
{
    public static QuestManager Instance;

    [Header("Panel")]
    public GameObject questPanel;
    public Button     openQuestButton;
    public Button     closeQuestButton;
    public Button     collectButton;

    [Header("Normal User Panels")]
    public GameObject winPanel;
    public GameObject gameOverPanel;

    [Header("Guest Win Panel")]
    public GameObject guestWinPanel;
    public Button     guestWinCreateAccountButton;
    public Button     guestWinQuitButton;

    [Header("Guest Game Over Panel")]
    public GameObject guestGameOverPanel;
    public Button     guestGameOverCreateAccountButton;
    public Button     guestGameOverQuitButton;

    [Header("Quest Progress Texts (TMP)")]
    public TextMeshProUGUI quest1ProgressText;
    public TextMeshProUGUI quest2ProgressText;
    public TextMeshProUGUI quest3ProgressText;
    public TextMeshProUGUI quest4ProgressText;

    [Header("Reward Per Quest")]
    public int rewardPerQuest = 10;

    [Header("Feedback (optional)")]
    public TextMeshProUGUI feedbackText;
    public float feedbackDuration = 2f;

    [Header("Win Delay (seconds)")]
    public float winDelay = 1.5f;

    private int  q1Current = 0; private int q1Goal = 5; private bool q1Complete = false; private bool q1Claimed = false;
    private int  q2Current = 0; private int q2Goal = 2; private bool q2Complete = false; private bool q2Claimed = false;
    private int  q3Current = 0; private int q3Goal = 2; private bool q3Complete = false; private bool q3Claimed = false;
    private int  q4Current = 0; private int q4Goal = 7; // ← 7 étapes CMD
                                private bool q4Complete = false; private bool q4Claimed = false;

    private Coroutine feedbackCoroutine;
    private bool      winTriggered = false;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        if (questPanel         != null) questPanel        .SetActive(false);
        if (winPanel           != null) winPanel          .SetActive(false);
        if (gameOverPanel      != null) gameOverPanel     .SetActive(false);
        if (guestWinPanel      != null) guestWinPanel     .SetActive(false);
        if (guestGameOverPanel != null) guestGameOverPanel.SetActive(false);
        if (feedbackText       != null) feedbackText      .gameObject.SetActive(false);

        if (openQuestButton  != null) openQuestButton .onClick.AddListener(OpenPanel);
        if (closeQuestButton != null) closeQuestButton.onClick.AddListener(ClosePanel);
        if (collectButton    != null) collectButton   .onClick.AddListener(CollectRewards);

        if (guestWinCreateAccountButton != null)
            guestWinCreateAccountButton.onClick.AddListener(GoToCreateAccount);
        if (guestWinQuitButton != null)
            guestWinQuitButton.onClick.AddListener(QuitGame);

        if (guestGameOverCreateAccountButton != null)
            guestGameOverCreateAccountButton.onClick.AddListener(GoToCreateAccount);
        if (guestGameOverQuitButton != null)
            guestGameOverQuitButton.onClick.AddListener(QuitGame);

        RefreshUI();
    }

    public void OpenPanel()  { if (questPanel != null) questPanel.SetActive(true);  RefreshUI(); }
    public void ClosePanel() { if (questPanel != null) questPanel.SetActive(false); }

    // ══════════════════════════════════════════
    //  TRACKING
    // ══════════════════════════════════════════
    public void OnAntivirusCollected(int amount = 1)
    {
        if (q1Complete) return;
        q1Current = Mathf.Min(q1Current + amount, q1Goal);
        if (q1Current >= q1Goal) q1Complete = true;
        RefreshUI();
        CheckWinCondition();
    }

    public void OnVirusFrozen()
    {
        if (q2Complete) return;
        q2Current = Mathf.Min(q2Current + 1, q2Goal);
        if (q2Current >= q2Goal) q2Complete = true;
        RefreshUI();
        CheckWinCondition();
    }

    public void OnTimerBoosterBought()
    {
        if (q3Complete) return;
        q3Current = Mathf.Min(q3Current + 1, q3Goal);
        if (q3Current >= q3Goal) q3Complete = true;
        RefreshUI();
        CheckWinCondition();
    }

    public void OnVirusEliminatedViaCMD()
    {
        if (q4Complete) return;
        q4Current = Mathf.Min(q4Current + 1, q4Goal);
        if (q4Current >= q4Goal) q4Complete = true;
        RefreshUI();
        CheckWinCondition();
    }

    // ══════════════════════════════════════════
    //  WIN CONDITION
    // ══════════════════════════════════════════
    public bool AreAllQuestsComplete()
    {
        return q1Complete && q2Complete && q3Complete && q4Complete;
    }

    void CheckWinCondition()
    {
        if (!AreAllQuestsComplete()) return;
        if (winTriggered) return;

        TimerManager timer = FindObjectOfType<TimerManager>();
        if (timer != null && timer.GetCurrentTime() > 0f)
        {
            winTriggered = true;
            StartCoroutine(WinDelayed(winDelay));
        }
    }

    IEnumerator WinDelayed(float delay)
    {
        yield return new WaitForSeconds(delay);

        TimerManager timer = FindObjectOfType<TimerManager>();
        if (timer != null && timer.GetCurrentTime() > 0f)
            TriggerWin();
        else
            winTriggered = false;
    }

    // ══════════════════════════════════════════
    //  TRIGGER WIN
    // ══════════════════════════════════════════
    public void TriggerWin()
    {
        TimerManager timer = FindObjectOfType<TimerManager>();
        if (timer != null) timer.StopTimer();
        if (questPanel != null) questPanel.SetActive(false);
        HideAllResultPanels();

        if (AuthManager.IsGuestUser)
        {
            Debug.Log("👤 Guest WIN → GuestWinPanel");
            if (guestWinPanel != null) guestWinPanel.SetActive(true);
        }
        else
        {
            Debug.Log("🏆 WIN → WinPanel");
            if (winPanel != null) winPanel.SetActive(true);
        }
    }

    // ══════════════════════════════════════════
    //  BOUTON "CONTINUER" → LEVEL 2
    // ══════════════════════════════════════════
    public void GoToLevel2()
    {
        PlayerPrefs.SetInt("ScoreFromLevel1", GameManager.score);
        PlayerPrefs.Save();
        UnityEngine.SceneManagement.SceneManager.LoadScene(3);
    }

    // ══════════════════════════════════════════
    //  TRIGGER GAME OVER
    // ══════════════════════════════════════════
    public void TriggerGameOver()
    {
        if (questPanel != null) questPanel.SetActive(false);
        HideAllResultPanels();

        if (AuthManager.IsGuestUser)
        {
            Debug.Log("👤 Guest GAME OVER → GuestGameOverPanel");
            if (guestGameOverPanel != null) guestGameOverPanel.SetActive(true);
        }
        else
        {
            Debug.Log("💀 GAME OVER → GameOverPanel");
            if (gameOverPanel != null) gameOverPanel.SetActive(true);
        }
    }

    // ══════════════════════════════════════════
    //  HIDE ALL RESULT PANELS
    // ══════════════════════════════════════════
    void HideAllResultPanels()
    {
        if (winPanel           != null) winPanel          .SetActive(false);
        if (gameOverPanel      != null) gameOverPanel     .SetActive(false);
        if (guestWinPanel      != null) guestWinPanel     .SetActive(false);
        if (guestGameOverPanel != null) guestGameOverPanel.SetActive(false);
    }

    // ══════════════════════════════════════════
    //  GUEST BUTTONS
    // ══════════════════════════════════════════
    void GoToCreateAccount()
    {
        Debug.Log("👤 Guest → Conversion Panel");
        if (guestWinPanel      != null) guestWinPanel     .SetActive(false);
        if (guestGameOverPanel != null) guestGameOverPanel.SetActive(false);
        if (GuestConversionManager.Instance != null)
            GuestConversionManager.Instance.ShowPanel();
        else
            Debug.LogError("❌ GuestConversionManager introuvable !");
    }

    void QuitGame()
    {
        Debug.Log("👤 Guest → Quit");
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    // ══════════════════════════════════════════
    //  COLLECT REWARDS
    // ══════════════════════════════════════════
    void CollectRewards()
    {
        bool any = false;
        if (q1Complete && !q1Claimed) { q1Claimed = true; GrantReward("Antivirus shields");    any = true; }
        if (q2Complete && !q2Claimed) { q2Claimed = true; GrantReward("Stop virus with spell"); any = true; }
        if (q3Complete && !q3Claimed) { q3Claimed = true; GrantReward("Buy timer booster");     any = true; }
        if (q4Complete && !q4Claimed) { q4Claimed = true; GrantReward("Eliminate via CMD");     any = true; }
        if (!any) ShowFeedback("Aucune quête complétée !");
        RefreshUI();
    }

    void GrantReward(string questName)
    {
        if (GameManager.Instance != null) GameManager.Instance.AddCoin(rewardPerQuest);
        ShowFeedback($"[OK] {questName} — +{rewardPerQuest} coins !");
    }

    // ══════════════════════════════════════════
    //  REFRESH UI
    // ══════════════════════════════════════════
    void RefreshUI()
    {
        SetProgressText(quest1ProgressText, q1Current, q1Goal, q1Claimed);
        SetProgressText(quest2ProgressText, q2Current, q2Goal, q2Claimed);
        SetProgressText(quest3ProgressText, q3Current, q3Goal, q3Claimed);
        SetProgressText(quest4ProgressText, q4Current, q4Goal, q4Claimed);

        if (collectButton != null)
            collectButton.interactable = (q1Complete && !q1Claimed)
                                       || (q2Complete && !q2Claimed)
                                       || (q3Complete && !q3Claimed)
                                       || (q4Complete && !q4Claimed);
    }

    void SetProgressText(TextMeshProUGUI tmp, int current, int goal, bool claimed)
    {
        if (tmp == null) return;
        tmp.text = claimed ? $"OK {goal}/{goal}" : $"{current}/{goal}";
    }

    // ══════════════════════════════════════════
    //  FEEDBACK
    // ══════════════════════════════════════════
    void ShowFeedback(string msg)
    {
        if (feedbackText == null) return;
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
}