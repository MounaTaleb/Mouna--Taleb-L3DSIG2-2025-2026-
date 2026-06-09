using UnityEngine;
using UnityEngine.SceneManagement;
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

    // ══════════════════════════════════════════
    //  STATE INTERNE
    // ══════════════════════════════════════════
    private RealtimeDBManager.UserData _pendingUserData;
    private bool _panelShown = false;

    // ══════════════════════════════════════════
    //  INIT
    // ══════════════════════════════════════════
    private void Awake()
    {
        if (firstTimePanel != null) firstTimePanel.SetActive(false);
        if (progressPanel  != null) progressPanel .SetActive(false);
    }

    private void Start()
    {
        SetupButtonListeners();
    }

    private void OnEnable()
    {
        if (!_panelShown)
            ApplyPanel(_pendingUserData);
    }

    // ══════════════════════════════════════════
    //  PUBLIC
    // ══════════════════════════════════════════
    public void ShowCorrectPanel(RealtimeDBManager.UserData userData)
    {
        _pendingUserData = userData;
        _panelShown      = false;
        ApplyPanel(userData);
    }

    public void ShowCorrectPanelFromDB()
    {
        if (RealtimeDBManager.Instance == null)
        {
            Debug.LogWarning("⚠️ RealtimeDBManager introuvable → FirstTimePanel par défaut");
            ApplyPanel(null);
            return;
        }

        RealtimeDBManager.Instance.LoadUserData(userData =>
        {
            MainThreadDispatcher.Run(() => ShowCorrectPanel(userData));
        });
    }

    // ══════════════════════════════════════════
    //  LOGIQUE CENTRALE D'AFFICHAGE
    // ══════════════════════════════════════════
    /// <summary>
    /// ✅ RÈGLE FINALE :
    ///   level == 0  → jamais joué             → FirstTimePanel
    ///   level >= 1  → a joué (win/lose/quit)  → ProgressPanel
    ///
    ///   LevelTracker écrit level = 1 dès que la scène 1 démarre.
    ///   Donc même un joueur qui perd immédiatement → level 1 → ProgressPanel.
    /// </summary>
    private void ApplyPanel(RealtimeDBManager.UserData userData)
    {
        _panelShown = true;

        int level = 0;
        if (userData?.profile != null)
            level = userData.profile.level;

        // ✅ FIX : >= 1 au lieu de >= 2
        bool hasProgress = level >= 1;

        if (firstTimePanel != null) firstTimePanel.SetActive(!hasProgress);
        if (progressPanel  != null) progressPanel .SetActive( hasProgress);

        Debug.Log(hasProgress
            ? $"🔄 ProgressPanel (level = {level})"
            : $"🆕 FirstTimePanel (level = {level})");
    }

    // ══════════════════════════════════════════
    //  WIRING BOUTONS
    // ══════════════════════════════════════════
    private void SetupButtonListeners()
    {
        if (startButton            != null) startButton           .onClick.AddListener(StartGame);
        if (settingsButtonFirst    != null) settingsButtonFirst   .onClick.AddListener(GoToSettings);
        if (helpButtonFirst        != null) helpButtonFirst       .onClick.AddListener(GoToHelp);
        if (continueButton         != null) continueButton        .onClick.AddListener(ContinueGame);
        if (newGameButton          != null) newGameButton         .onClick.AddListener(NewGame);
        if (settingsButtonProgress != null) settingsButtonProgress.onClick.AddListener(GoToSettings);
        if (helpButtonProgress     != null) helpButtonProgress    .onClick.AddListener(GoToHelp);
    }

    // ══════════════════════════════════════════
    //  ACTIONS
    // ══════════════════════════════════════════
    private void StartGame()
    {
        Debug.Log("▶ START → Sélection du personnage");
        CharacterSelectionManager.ResetCharacterSelection();
        EnsureUIManager();
        uiManager?.ShowCharacterSelection();
    }

    private void ContinueGame()
    {
        if (RealtimeDBManager.Instance == null)
        {
            Debug.LogWarning("⚠️ DB introuvable → scène 1");
            SceneManager.LoadScene(1);
            return;
        }

        RealtimeDBManager.Instance.LoadUserData(userData =>
        {
            int level = (userData?.profile != null && userData.profile.level >= 1)
                ? userData.profile.level
                : 1;

            Debug.Log($"▶ CONTINUE → scène {level}");
            SceneManager.LoadScene(level);
        });
    }

    private void NewGame()
    {
        Debug.Log("🔄 NEW GAME → reset level 0 → Sélection du personnage");

        // Reset à 0 → retour FirstTimePanel au prochain lancement
        RealtimeDBManager.Instance?.UpdateLevel(0);

        CharacterSelectionManager.ResetCharacterSelection();
        EnsureUIManager();
        uiManager?.ShowCharacterSelection();
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
    private void EnsureUIManager()
    {
        if (uiManager == null)
            uiManager = FindObjectOfType<UIManager>();
    }

    // ══════════════════════════════════════════
    //  DEBUG (Context Menu)
    // ══════════════════════════════════════════
    [ContextMenu("Debug → 1ère fois (level 0)")]
    private void DebugFirstTime()
    {
        RealtimeDBManager.Instance?.UpdateLevel(0);
        ShowCorrectPanelFromDB();
    }

    [ContextMenu("Debug → A joué (level 1)")]
    private void DebugLevel1()
    {
        RealtimeDBManager.Instance?.UpdateLevel(1);
        ShowCorrectPanelFromDB();
    }

    [ContextMenu("Debug → Niveau 1 complété (level 2)")]
    private void DebugLevel2()
    {
        RealtimeDBManager.Instance?.UpdateLevel(2);
        ShowCorrectPanelFromDB();
    }

    [ContextMenu("Debug → Progression avancée (level 3+)")]
    private void DebugLevel3()
    {
        RealtimeDBManager.Instance?.UpdateLevel(3);
        ShowCorrectPanelFromDB();
    }
}