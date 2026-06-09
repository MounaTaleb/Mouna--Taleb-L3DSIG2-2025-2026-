using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class RoundManager2 : MonoBehaviour
{
    public static RoundManager2 Instance;

    [Header("Round Root GameObjects")]
    public GameObject round1Root;
    public GameObject round2Root;

    // ─────────────────────────────────────────────────────────────
    //  UI EXCLUSIFS PAR ROUND
    //  Glisse ici tous les GameObjects à afficher/cacher par round
    // ─────────────────────────────────────────────────────────────
    [Header("UI — Round 1 uniquement (slider R1, terminal, objectifs R1…)")]
    public GameObject[] uiRound1Only;   // ex: SliderR1, TerminalPanel, QuestPanelR1

    [Header("UI — Round 2 uniquement (slider R2, objectifs R2…)")]
    public GameObject[] uiRound2Only;   // ex: SliderR2, QuestPanelR2

    [Header("Timer Round 2")]
    public float round2TotalTime = 180f;

    [Header("Round Label (optionnel)")]
    public TextMeshProUGUI roundLabel;

    const string KEY_ROUND  = "CurrentRound";
    const string KEY_SCORE  = "SavedScore";
    const string KEY_ANTI   = "SavedAnti";
    const string KEY_SPYBOT = "SavedSpybot";
    const string KEY_TIMER  = "SavedTimer";

    private int  currentRound  = 1;
    private bool transitioning = false;

    private static bool isReloading = false;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
    }

    void Start()
    {
        if (!isReloading)
        {
            PlayerPrefs.DeleteKey(KEY_ROUND);
            PlayerPrefs.DeleteKey(KEY_SCORE);
            PlayerPrefs.DeleteKey(KEY_ANTI);
            PlayerPrefs.DeleteKey(KEY_SPYBOT);
            PlayerPrefs.DeleteKey(KEY_TIMER);
            PlayerPrefs.Save();
            Debug.Log("[RoundManager2] Démarrage frais → Round 1 forcé");
        }

        currentRound = PlayerPrefs.GetInt(KEY_ROUND, 1);

        if (currentRound == 1)
        {
            if (round1Root != null) round1Root.SetActive(true);
            if (round2Root != null) round2Root.SetActive(false);

            // ✅ Affiche UI Round 1, cache UI Round 2
            SetRoundUI(showRound1: true);
        }
        else
        {
            if (round1Root != null) round1Root.SetActive(false);
            if (round2Root != null) round2Root.SetActive(true);

            // ✅ Cache UI Round 1, affiche UI Round 2
            SetRoundUI(showRound1: false);

            RestoreSavedData();
            StartCoroutine(StartRound2AfterReload());
        }

        UpdateRoundLabel();

        CharacterSpawner spawner = FindObjectOfType<CharacterSpawner>();
        if (spawner != null) spawner.LockInitialPosition();

        Debug.Log($"[RoundManager2] Round {currentRound} démarré");
    }

    // ─────────────────────────────────────────────────────────────
    //  GESTION UI PAR ROUND
    // ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Active les GameObjects du round en cours, désactive l'autre groupe.
    /// </summary>
    void SetRoundUI(bool showRound1)
    {
        // UI Round 1
        if (uiRound1Only != null)
            foreach (GameObject go in uiRound1Only)
                if (go != null) go.SetActive(showRound1);

        // UI Round 2
        if (uiRound2Only != null)
            foreach (GameObject go in uiRound2Only)
                if (go != null) go.SetActive(!showRound1);
    }

    // ─────────────────────────────────────────────────────────────

    IEnumerator StartRound2AfterReload()
    {
        yield return new WaitForSeconds(0.3f);

        if (CyberAlertRound2.Instance != null)
            yield return StartCoroutine(CyberAlertRound2.Instance.PlayRound2Intro());

        TimerManager2 timer = FindObjectOfType<TimerManager2>();
        float savedTime = PlayerPrefs.GetFloat(KEY_TIMER, round2TotalTime);
        if (timer != null)
        {
            timer.SetTotalTime(savedTime);
            timer.ResetTimer();
            timer.StartTimer();
        }

        if (QuestManager2_Round2.Instance != null)
            QuestManager2_Round2.Instance.StartRound();

        Debug.Log("[RoundManager2] Round 2 prêt après reload");
    }

    public void OnRound1Complete()
    {
        if (transitioning || currentRound != 1) return;
        transitioning = true;
        StartCoroutine(SaveAndReloadForRound2());
    }

    IEnumerator SaveAndReloadForRound2()
    {
        TimerManager2 timer = FindObjectOfType<TimerManager2>();
        if (timer != null) timer.StopTimer();

        GameObject player = GetActivePlayer();
        SetPlayerControls(player, false);

        SaveGameData();
        PlayerPrefs.SetInt(KEY_ROUND, 2);
        PlayerPrefs.Save();

        if (CyberAlertSystem.Instance != null)
            CyberAlertSystem.Instance.DisableUI();

        // ✅ Cache immédiatement les UI Round 1 avant le reload
        SetRoundUI(showRound1: false);

        isReloading = true;

        yield return new WaitForSeconds(1f);

        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    void SaveGameData()
    {
        PlayerPrefs.SetInt(KEY_SCORE,  GameManager.score);
        PlayerPrefs.SetInt(KEY_ANTI,   GameManager.anti);
        PlayerPrefs.SetInt(KEY_SPYBOT, GameManager.spybot);
        PlayerPrefs.SetFloat(KEY_TIMER, round2TotalTime);
        PlayerPrefs.Save();
        Debug.Log($"[RoundManager2] Sauvegarde → score:{GameManager.score} anti:{GameManager.anti} spybot:{GameManager.spybot}");
    }

    void RestoreSavedData()
    {
        GameManager.score  = PlayerPrefs.GetInt(KEY_SCORE,  0);
        GameManager.anti   = PlayerPrefs.GetInt(KEY_ANTI,   0);
        GameManager.spybot = PlayerPrefs.GetInt(KEY_SPYBOT, 0);

        if (GameManager.Instance != null)
            GameManager.Instance.ForceUpdateDisplays();

        PlayerPrefs.DeleteKey(KEY_SCORE);
        PlayerPrefs.DeleteKey(KEY_ANTI);
        PlayerPrefs.DeleteKey(KEY_SPYBOT);
        PlayerPrefs.DeleteKey(KEY_TIMER);

        Debug.Log($"[RoundManager2] Restauration → score:{GameManager.score} anti:{GameManager.anti} spybot:{GameManager.spybot}");
    }

    public void OnRound2Complete()
    {
        isReloading = false;
        PlayerPrefs.DeleteKey(KEY_ROUND);
        PlayerPrefs.Save();
        Debug.Log("[RoundManager2] WIN FINAL");
        if (QuestManager2_Round2.Instance != null)
            QuestManager2_Round2.Instance.TriggerFinalWin();
    }

    GameObject GetActivePlayer()
    {
        CharacterSpawner spawner = FindObjectOfType<CharacterSpawner>();
        if (spawner != null && spawner.ActiveCharacter != null) return spawner.ActiveCharacter;
        return GameObject.FindWithTag("Player");
    }

    void SetPlayerControls(GameObject player, bool enabled)
    {
        if (player == null) return;
        foreach (MonoBehaviour s in player.GetComponents<MonoBehaviour>())
        {
            string t = s.GetType().Name;
            if (t == "ThirdPersonController" || t == "FirstPersonController" ||
                t == "StarterAssetsInputs"   || t == "PlayerController")
                s.enabled = enabled;
        }
    }

    void UpdateRoundLabel()
    {
        if (roundLabel != null) roundLabel.text = $"Round {currentRound} / 2";
    }

    public int GetCurrentRound() => currentRound;
}