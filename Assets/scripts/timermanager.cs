using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class TimerManager : MonoBehaviour
{
    [Header("Timer Settings")]
    public float totalTime = 120f;
    public bool countDown = true;

    [Header("UI")]
    public TextMeshProUGUI timerText;

    [Header("Colors")]
    public Color normalColor = Color.white;
    public Color warningColor = Color.yellow;
    public Color dangerColor = Color.red;

    [Header("Game Over Panel")]
    public GameObject gameOverPanel;

    [Header("Music")]
    public MusicManager musicManager;

    private float currentTime;
    private bool isRunning = false;

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
                isRunning = false;
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

    public void StartTimer() => isRunning = true;

    public void StopTimer() => isRunning = false;

    public void ResetTimer()
    {
        currentTime = countDown ? totalTime : 0f;
        isRunning = false;
        UpdateUI();
    }

    public float GetCurrentTime() => currentTime;

    public void AddTime(float seconds)
    {
        currentTime += seconds;

        if (countDown && currentTime > totalTime)
            currentTime = totalTime;

        UpdateUI();
        Debug.Log($"[TimerManager] +{seconds}s → {currentTime}s restantes");
    }

    // ══════════════════════════════════════════
    //  TIMER = 0
    // ══════════════════════════════════════════
    void OnTimerEnd()
    {
        Debug.Log("⏰ Temps écoulé !");

        // Gestion des quêtes
        if (QuestManager.Instance != null)
        {
            if (QuestManager.Instance.AreAllQuestsComplete())
                QuestManager.Instance.TriggerWin();
            else
                QuestManager.Instance.TriggerGameOver();

            return;
        }

        // Fallback
        if (musicManager != null)
            musicManager.ShowGameOver();
        else if (gameOverPanel != null)
            gameOverPanel.SetActive(true);
    }

    // ══════════════════════════════════════════
    //  BOUTON REJOUER
    // ══════════════════════════════════════════
    public void OnRejouer()
    {
        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);

        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    // ══════════════════════════════════════════
    //  BOUTON MENU
    // ══════════════════════════════════════════
    public void OnGoToMenu()
    {
        PlayerPrefs.SetInt("ShowMenuPanel", 1);
        PlayerPrefs.Save();
        SceneManager.LoadScene(0);
    }

    // ══════════════════════════════════════════
    //  BOUTON SCÈNE 3
    // ══════════════════════════════════════════
    public void GoToScene3()
    {
        Debug.Log("Loading Scene 3...");
        SceneManager.LoadScene(3);
    }

    // ══════════════════════════════════════════
    //  BOUTON QUITTER
    // ══════════════════════════════════════════
    public void OnQuit()
    {
        Debug.Log("Quit");
        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}