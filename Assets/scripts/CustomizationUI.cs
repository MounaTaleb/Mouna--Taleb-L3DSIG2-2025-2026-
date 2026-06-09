using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using GanzSe;

public class CustomizationUI : MonoBehaviour
{
    [Header("Category Buttons (Left Panel)")]
    public Button hairButton;
    public Button faceHairsButton;
    public Button eyesButton;
    public Button chestsButton;
    public Button legsButton;
    public Button feetButton;

    [Header("Panels")]
    public GameObject hairPanel;
    public GameObject faceHairsPanel;
    public GameObject eyesPanel;
    public GameObject chestsPanel;
    public GameObject legsPanel;
    public GameObject feetPanel;

    [Header("Item Buttons — HAIRS (5)")]
    public Button[] hairItemButtons;

    [Header("Item Buttons — FACE HAIRS (4)")]
    public Button[] faceHairItemButtons;

    [Header("Item Buttons — EYES (4)")]
    public Button[] eyesItemButtons;

    [Header("Item Buttons — CHESTS (3)")]
    public Button[] chestsItemButtons;

    [Header("Item Buttons — LEGS (4)")]
    public Button[] legsItemButtons;

    [Header("Item Buttons — FEET (3)")]
    public Button[] feetItemButtons;

    [Header("Confirm / Randomize / Back")]
    public Button confirmButton;
    public Button randomizeButton;
    public Button backButton;

    [Header("Scenes")]
    public int gameSceneIndex            = 1;
    public int characterSelectSceneIndex = 0;

    [Header("Modular Hero")]
    public ModularHeroController heroController;

    private const string CAT_HAIR     = "HAIRS";
    private const string CAT_FACEHAIR = "FACE HAIRS";
    private const string CAT_EYES     = "EYES";
    private const string CAT_CHESTS   = "CHESTS";
    private const string CAT_LEGS     = "LEGS";
    private const string CAT_FEET     = "FEET";

    private GameObject        currentOpenPanel;
    private CustomizationData currentData = new CustomizationData();

    private void Start()
    {
        // ── Category buttons ──────────────────────────────
        hairButton.onClick.AddListener(()      => OpenPanel(hairPanel));
        faceHairsButton.onClick.AddListener(() => OpenPanel(faceHairsPanel));
        eyesButton.onClick.AddListener(()      => OpenPanel(eyesPanel));
        chestsButton.onClick.AddListener(()    => OpenPanel(chestsPanel));
        legsButton.onClick.AddListener(()      => OpenPanel(legsPanel));
        feetButton.onClick.AddListener(()      => OpenPanel(feetPanel));

        // ── Item buttons ──────────────────────────────────
        RegisterItemButtons(hairItemButtons,     CAT_HAIR);
        RegisterItemButtons(faceHairItemButtons, CAT_FACEHAIR);
        RegisterItemButtons(eyesItemButtons,     CAT_EYES);
        RegisterItemButtons(chestsItemButtons,   CAT_CHESTS);
        RegisterItemButtons(legsItemButtons,     CAT_LEGS);
        RegisterItemButtons(feetItemButtons,     CAT_FEET);

        // ── Action buttons ────────────────────────────────
        confirmButton.onClick.AddListener(OnConfirm);
        randomizeButton.onClick.AddListener(OnRandomize);
        backButton.onClick.AddListener(OnBack);

        // ── Charge customisation existante si elle existe ─
        LoadExistingCustomization();

        // ── Ouvre Hair par défaut ─────────────────────────
        CloseAllPanels();
        hairPanel.SetActive(true);
        currentOpenPanel = hairPanel;
    }

    // ─────────────────────────────────────────────────────
    //  Charge et applique une customisation déjà sauvegardée
    // ─────────────────────────────────────────────────────
    private void LoadExistingCustomization()
    {
        if (!PlayerPrefs.HasKey(CustomizationData.PREFS_KEY)) return;

        string json  = PlayerPrefs.GetString(CustomizationData.PREFS_KEY);
        currentData  = JsonUtility.FromJson<CustomizationData>(json);

        heroController.SelectPart(CAT_HAIR,     currentData.hair);
        heroController.SelectPart(CAT_FACEHAIR, currentData.facehair);
        heroController.SelectPart(CAT_EYES,     currentData.eyes);
        heroController.SelectPart(CAT_CHESTS,   currentData.chests);
        heroController.SelectPart(CAT_LEGS,     currentData.legs);
        heroController.SelectPart(CAT_FEET,     currentData.feet);

        Debug.Log($"Customisation chargée : {json}");
    }

    // ─────────────────────────────────────────────────────
    //  Enregistre les boutons + track les choix
    // ─────────────────────────────────────────────────────
    private void RegisterItemButtons(Button[] buttons, string categoryName)
    {
        for (int i = 0; i < buttons.Length; i++)
        {
            int index = i;
            buttons[i].onClick.AddListener(() =>
            {
                heroController.SelectPart(categoryName, index);
                TrackSelection(categoryName, index);
                Debug.Log($"Part sélectionnée : {categoryName} [{index}]");
            });
        }
    }

    private void TrackSelection(string category, int index)
    {
        switch (category)
        {
            case CAT_HAIR:     currentData.hair     = index; break;
            case CAT_FACEHAIR: currentData.facehair = index; break;
            case CAT_EYES:     currentData.eyes     = index; break;
            case CAT_CHESTS:   currentData.chests   = index; break;
            case CAT_LEGS:     currentData.legs     = index; break;
            case CAT_FEET:     currentData.feet     = index; break;
        }
    }

    // ─────────────────────────────────────────────────────
    //  Gestion des panels
    // ─────────────────────────────────────────────────────
    public void OpenPanel(GameObject panelToOpen)
    {
        if (currentOpenPanel == panelToOpen)
        {
            currentOpenPanel.SetActive(false);
            currentOpenPanel = null;
            return;
        }
        CloseAllPanels();
        panelToOpen.SetActive(true);
        currentOpenPanel = panelToOpen;
    }

    public void CloseAllPanels()
    {
        hairPanel.SetActive(false);
        faceHairsPanel.SetActive(false);
        eyesPanel.SetActive(false);
        chestsPanel.SetActive(false);
        legsPanel.SetActive(false);
        feetPanel.SetActive(false);
        currentOpenPanel = null;
    }

    // ─────────────────────────────────────────────────────
    //  CONFIRM → sauvegarde + lance le jeu
    // ─────────────────────────────────────────────────────
    private void OnConfirm()
    {
        CloseAllPanels();

        string json = JsonUtility.ToJson(currentData);
        PlayerPrefs.SetString(CustomizationData.PREFS_KEY, json);
        PlayerPrefs.Save();

        // Firebase si besoin
        // RealtimeDBManager.Instance?.UpdateCustomization(json);

        Debug.Log($"Customisation sauvegardée : {json}");
        SceneManager.LoadScene(gameSceneIndex);
    }

    // ─────────────────────────────────────────────────────
    //  RANDOMIZE
    // ─────────────────────────────────────────────────────
    private void OnRandomize()
    {
        if (heroController != null)
            heroController.RandomizeAll();
        else
            Debug.LogWarning("heroController non assigné !");
    }

    // ─────────────────────────────────────────────────────
    //  BACK → retour sélection personnage
    // ─────────────────────────────────────────────────────
    private void OnBack()
    {
        PlayerPrefs.SetInt("OpenCharacterPanel", 1);
        PlayerPrefs.Save();
        SceneManager.LoadScene(characterSelectSceneIndex);
    }
}