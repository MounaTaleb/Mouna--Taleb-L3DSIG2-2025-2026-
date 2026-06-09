using UnityEngine;
using UnityEngine.UI;

public class MenuManager : MonoBehaviour
{
    [Header("── Panels ──")]
    public GameObject firstTimePanel;
    public GameObject progressPanel;

    [Header("── Boutons First Time Panel ──")]
    public Button startButton;
    public Button settingsButtonFirst;
    public Button helpButtonFirst;

    [Header("── Boutons Progress Panel ──")]
    public Button continueButton;
    public Button newGameButton;
    public Button settingsButtonProgress;
    public Button helpButtonProgress;

    [Header("── References ──")]
    public UIManager uiManager;

    private const string LEVEL_KEY = "CurrentLevel";

    // ══════════════════════════════════════════
    private void Start()
    {
        SetupButtonListeners();
        ShowCorrectPanel();
    }

    // ══════════════════════════════════════════
    //  PUBLIC — appelé par UIManager.ShowMenu()
    // ══════════════════════════════════════════
    public void ShowCorrectPanel()
    {
        if (HasProgress())
        {
            firstTimePanel.SetActive(false);
            progressPanel.SetActive(true);
            Debug.Log("🔄 Progression trouvée → ProgressPanel");
        }
        else
        {
            firstTimePanel.SetActive(true);
            progressPanel.SetActive(false);
            Debug.Log("🆕 Première fois → FirstTimePanel");
        }
    }

    // ══════════════════════════════════════════
    //  WIRING BOUTONS
    // ══════════════════════════════════════════
    private void SetupButtonListeners()
    {
        // ── First Time Panel ──
        if (startButton != null)
            startButton.onClick.AddListener(StartGame);

        if (settingsButtonFirst != null)
            settingsButtonFirst.onClick.AddListener(GoToSettings);

        if (helpButtonFirst != null)
            helpButtonFirst.onClick.AddListener(GoToHelp);

        // ── Progress Panel ──
        if (continueButton != null)
            continueButton.onClick.AddListener(ContinueGame);

        if (newGameButton != null)
            newGameButton.onClick.AddListener(NewGame); // direct, sans popup

        if (settingsButtonProgress != null)
            settingsButtonProgress.onClick.AddListener(GoToSettings);

        if (helpButtonProgress != null)
            helpButtonProgress.onClick.AddListener(GoToHelp);
    }

    // ══════════════════════════════════════════
    //  ACTIONS
    // ══════════════════════════════════════════
    private void StartGame()
    {
        Debug.Log("▶ START — Première partie !");
        PlayerPrefs.SetInt(LEVEL_KEY, 1);
        PlayerPrefs.Save();

        EnsureUIManager();
        uiManager?.ShowLevel();
    }

    private void ContinueGame()
    {
        int savedLevel = PlayerPrefs.GetInt(LEVEL_KEY, 1);
        Debug.Log($"▶ CONTINUE — Niveau {savedLevel}");

        EnsureUIManager();
        uiManager?.ShowLevel();
    }

    // Direct sans popup
    private void NewGame()
    {
        Debug.Log("🔄 NEW GAME — Progression effacée !");
        PlayerPrefs.DeleteKey(LEVEL_KEY);
        PlayerPrefs.SetInt(LEVEL_KEY, 1);
        PlayerPrefs.Save();

        EnsureUIManager();
        uiManager?.ShowLevel();
    }

    // ══════════════════════════════════════════
    //  NAVIGATION
    // ══════════════════════════════════════════
    private void GoToSettings()
    {
        EnsureUIManager();
        uiManager?.ShowSettings();
    }

    private void GoToHelp()
    {
        Debug.Log("❓ Help");
    }

    // ══════════════════════════════════════════
    //  UTILS
    // ══════════════════════════════════════════
    private bool HasProgress()
    {
        return PlayerPrefs.HasKey(LEVEL_KEY);
    }

    public static bool PlayerHasProgress()
    {
        return PlayerPrefs.HasKey("CurrentLevel");
    }

    public static int GetSavedLevel()
    {
        return PlayerPrefs.GetInt("CurrentLevel", 1);
    }

    private void EnsureUIManager()
    {
        if (uiManager == null)
            uiManager = FindObjectOfType<UIManager>();
    }

    // ══════════════════════════════════════════
    //  DEBUG
    // ══════════════════════════════════════════
    [ContextMenu("Debug → Simuler 1ère fois")]
    private void DebugFirstTime()
    {
        PlayerPrefs.DeleteKey(LEVEL_KEY);
        PlayerPrefs.Save();
        ShowCorrectPanel();
    }

    [ContextMenu("Debug → Simuler progression Level 3")]
    private void DebugHasProgress()
    {
        PlayerPrefs.SetInt(LEVEL_KEY, 3);
        PlayerPrefs.Save();
        ShowCorrectPanel();
    }
}