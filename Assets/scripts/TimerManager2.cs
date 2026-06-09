using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

// ============================================================
//  TimerManager2.cs  —  Timer dédié au niveau 2
//
//  - Quand timer = 0 :
//      → Round 1 actif : vérifie QuestManager2
//      → Round 2 actif : vérifie QuestManager2_Round2
//  - Si le joueur finit tous les objectifs avant timer = 0 :
//      → QuestManager2 / QuestManager2_Round2 appellent StopTimer()
// ============================================================
public class TimerManager2 : MonoBehaviour
{
    [Header("Timer Settings")]
    public float totalTime = 120f;
    public bool  countDown = true;

    [Header("UI")]
    public TextMeshProUGUI timerText;

    [Header("Colors")]
    public Color normalColor  = Color.white;
    public Color warningColor = Color.yellow;
    public Color dangerColor  = Color.red;

    [Header("Game Over Panel (fallback)")]
    public GameObject gameOverPanel;

    [Header("Music (optional)")]
    public MusicManager musicManager;

    private float currentTime;
    private bool  isRunning = false;

    // ════════════════════════════════════════════════════════════
    //  LIFECYCLE
    // ════════════════════════════════════════════════════════════

    void Start()
    {
        currentTime = countDown ? totalTime : 0f;

        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);

        UpdateUI();
        StartTimer();
    }

    void Update()
    {
        if (!isRunning) return;

        if (countDown)
        {
            currentTime -= Time.deltaTime;

            if (currentTime <= 0f)
            {
                currentTime = 0f;
                isRunning   = false;
                UpdateUI();
                OnTimerEnd();
                return;
            }
        }
        else
        {
            currentTime += Time.deltaTime;
        }

        UpdateUI();
    }

    // ════════════════════════════════════════════════════════════
    //  UI
    // ════════════════════════════════════════════════════════════

    void UpdateUI()
    {
        if (timerText == null) return;

        int minutes = Mathf.FloorToInt(currentTime / 60f);
        int seconds = Mathf.FloorToInt(currentTime % 60f);
        timerText.text = string.Format("{0}:{1:00}", minutes, seconds);

        if (countDown)
        {
            if (currentTime <= 10f)
                timerText.color = dangerColor;
            else if (currentTime <= 30f)
                timerText.color = warningColor;
            else
                timerText.color = normalColor;
        }
    }

    // ════════════════════════════════════════════════════════════
    //  CONTRÔLES
    // ════════════════════════════════════════════════════════════

    public void StartTimer() => isRunning = true;
    public void StopTimer()  => isRunning = false;

    public void ResetTimer()
    {
        currentTime = countDown ? totalTime : 0f;
        isRunning   = false;
        UpdateUI();
    }

    public float GetCurrentTime() => currentTime;

    /// <summary>Appelé par RoundManager2 pour changer le temps total avant ResetTimer()</summary>
    public void SetTotalTime(float newTotalTime)
    {
        totalTime = newTotalTime;
        Debug.Log($"[TimerManager2] ⏱ Nouveau temps total : {newTotalTime}s");
    }

    public void AddTime(float seconds)
    {
        currentTime += seconds;
        if (countDown && currentTime > totalTime)
            currentTime = totalTime;
        UpdateUI();
        Debug.Log($"[TimerManager2] +{seconds}s → {currentTime}s restantes");
    }

    // ════════════════════════════════════════════════════════════
    //  TIMER = 0  →  WIN ou GAME OVER  (Round 1 ou Round 2)
    // ════════════════════════════════════════════════════════════

    void OnTimerEnd()
    {
        Debug.Log("[TimerManager2] ⏰ Temps écoulé !");

        // ── Détecte le round actif via RoundManager2 ──────────
        int currentRound = (RoundManager2.Instance != null)
            ? RoundManager2.Instance.GetCurrentRound()
            : 1;

        if (currentRound == 2)
        {
            // Round 2 — vérifie QuestManager2_Round2
            if (QuestManager2_Round2.Instance != null)
            {
                if (QuestManager2_Round2.Instance.AreAllQuestsComplete())
                    QuestManager2_Round2.Instance.TriggerFinalWin();
                else
                    QuestManager2_Round2.Instance.TriggerGameOver();
                return;
            }
        }
        else
        {
            // Round 1 — vérifie QuestManager2
            if (QuestManager2.Instance != null)
            {
                if (QuestManager2.Instance.AreAllQuestsComplete())
                    QuestManager2.Instance.TriggerWin();   // → déclenche Round 2 via RoundManager2
                else
                    QuestManager2.Instance.TriggerGameOver();
                return;
            }
        }

        // Fallback si aucun QuestManager trouvé
        if (musicManager != null)
            musicManager.ShowGameOver();
        else if (gameOverPanel != null)
            gameOverPanel.SetActive(true);

        Debug.LogWarning("[TimerManager2] Aucun QuestManager trouvé !");
    }

    // ════════════════════════════════════════════════════════════
    //  BOUTONS UI
    // ════════════════════════════════════════════════════════════

    public void OnRejouer()
    {
        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void OnGoToMenu()
    {
        PlayerPrefs.SetInt("ShowMenuPanel", 1);
        PlayerPrefs.Save();
        SceneManager.LoadScene(0);
    }

    public void GoToScene3()
    {
        Debug.Log("[TimerManager2] Loading Scene 3...");
        SceneManager.LoadScene(3);
    }

    public void OnQuit()
    {
        Debug.Log("[TimerManager2] Quit");
        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}