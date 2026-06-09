using UnityEngine;
using UnityEngine.SceneManagement;

public class UIManager : MonoBehaviour
{
    [Header("── Panels ──")]
    public GameObject welcomePanel;
    public GameObject loginPanel;
    public GameObject createAccountPanel;
    public GameObject characterPanel;
    public GameObject menuPanel;
    public GameObject settingsPanel;
    public GameObject forgotPasswordPanel;
    public GameObject verifyEmailPanel;

    [Header("── Help Panels ──")]
    public GameObject helpPanel1;
    public GameObject helpPanel2;

    void Start()
    {
        if (PlayerPrefs.GetInt("OpenCharacterPanel", 0) == 1)
        {
            PlayerPrefs.DeleteKey("OpenCharacterPanel");
            PlayerPrefs.Save();
            ShowCharacterSelection();
            return;
        }

        if (PlayerPrefs.GetInt("ShowMenuPanel", 0) == 1)
        {
            PlayerPrefs.DeleteKey("ShowMenuPanel");
            PlayerPrefs.Save();
            ShowMenuFromDB();
            return;
        }

        if (PlayerPrefs.GetInt("ShowConvertPanel", 0) == 1)
        {
            PlayerPrefs.DeleteKey("ShowConvertPanel");
            PlayerPrefs.Save();
            ShowCreateAccount();
            return;
        }

        ShowWelcome();
    }

    // ══════════════════════════════════════════
    //  HIDE ALL
    // ══════════════════════════════════════════
    void HideAll()
    {
        if (welcomePanel)        welcomePanel       .SetActive(false);
        if (loginPanel)          loginPanel         .SetActive(false);
        if (createAccountPanel)  createAccountPanel .SetActive(false);
        if (characterPanel)      characterPanel     .SetActive(false);
        if (menuPanel)           menuPanel          .SetActive(false);
        if (settingsPanel)       settingsPanel      .SetActive(false);
        if (forgotPasswordPanel) forgotPasswordPanel.SetActive(false);
        if (verifyEmailPanel)    verifyEmailPanel   .SetActive(false);
        if (helpPanel1)          helpPanel1         .SetActive(false);
        if (helpPanel2)          helpPanel2         .SetActive(false);
    }

    // ══════════════════════════════════════════
    //  NAVIGATION
    // ══════════════════════════════════════════
    public void ShowWelcome()
    {
        HideAll();
        if (welcomePanel) welcomePanel.SetActive(true);
    }

    public void ShowLogin()
    {
        HideAll();
        if (loginPanel) loginPanel.SetActive(true);
    }

    public void ShowCreateAccount()
    {
        HideAll();
        if (createAccountPanel) createAccountPanel.SetActive(true);
    }

    public void ShowCharacterSelection()
    {
        HideAll();
        if (characterPanel) characterPanel.SetActive(true);
    }

    public void ShowMenu(RealtimeDBManager.UserData userData)
    {
        HideAll();
        if (menuPanel == null)
        {
            Debug.LogError("❌ menuPanel non assigné dans UIManager !");
            return;
        }
        menuPanel.SetActive(true);

        MenuManager menu = menuPanel.GetComponent<MenuManager>();
        if (menu != null)
            menu.ShowCorrectPanel(userData);
        else
            Debug.LogWarning("⚠️ MenuManager introuvable sur menuPanel !");
    }

    public void ShowMenuFromDB()
    {
        HideAll();
        if (menuPanel == null)
        {
            Debug.LogError("❌ menuPanel non assigné dans UIManager !");
            return;
        }
        menuPanel.SetActive(true);

        MenuManager menu = menuPanel.GetComponent<MenuManager>();
        if (menu != null)
            menu.ShowCorrectPanelFromDB();
        else
            Debug.LogWarning("⚠️ MenuManager introuvable sur menuPanel !");
    }

    public void ShowSettings()
    {
        HideAll();
        if (settingsPanel) settingsPanel.SetActive(true);
    }

    public void ShowForgotPassword()
    {
        HideAll();
        if (forgotPasswordPanel) forgotPasswordPanel.SetActive(true);
    }

    public void ShowVerifyEmail()
    {
        HideAll();
        if (verifyEmailPanel) verifyEmailPanel.SetActive(true);
    }

    // ══════════════════════════════════════════
    //  HELP NAVIGATION
    // ══════════════════════════════════════════

    /// <summary>Ouvre le Help Panel 1 (bouton Help du menu)</summary>
    public void ShowHelpPanel1()
    {
        HideAll();
        if (helpPanel1 != null)
            helpPanel1.SetActive(true);
        else
            Debug.LogError("❌ helpPanel1 non assigné dans UIManager !");
    }

    /// <summary>Bouton Next de Help Panel 1 → ouvre Help Panel 2</summary>
    public void HelpNext()
    {
        if (helpPanel1) helpPanel1.SetActive(false);

        if (helpPanel2 != null)
            helpPanel2.SetActive(true);
        else
            Debug.LogError("❌ helpPanel2 non assigné dans UIManager !");
    }

    /// <summary>Bouton Quit des Help Panels → retour au menu principal</summary>
    public void HelpQuit() => ShowMenuFromDB();

    // ══════════════════════════════════════════
    //  BACK / SCENE / QUIT
    // ══════════════════════════════════════════
    public void BackToMenu() => ShowMenuFromDB();

    public void MoveToScene(int sceneID) => SceneManager.LoadScene(sceneID);

    public void QuitGame()
    {
        Debug.Log("Quit Game");
        Application.Quit();
    }
}