using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class CharacterSelectionManager : MonoBehaviour
{
    [Header("Checkboxes (Toggles)")]
    public Toggle femaleCheckbox;
    public Toggle maleCheckbox;

    [Header("Buttons")]
    public Button confirmButton;
    public Button returnButton;
    public Button customizeButton;

    [Header("References")]
    public UIManager uiManager;

    [Header("Scenes")]
    public int gameSceneIndex      = 1;
    public int customizeSceneIndex = 4;

    private const string CHARACTER_KEY = "SelectedCharacter";

    private void Start()
    {
        ToggleGroup group = gameObject.AddComponent<ToggleGroup>();
        group.allowSwitchOff = true;

        femaleCheckbox.group = group;
        maleCheckbox.group   = group;
        femaleCheckbox.isOn  = false;
        maleCheckbox.isOn    = false;

        femaleCheckbox.onValueChanged.AddListener(_ => UpdateConfirmButton());
        maleCheckbox.onValueChanged.AddListener(_   => UpdateConfirmButton());

        confirmButton.onClick.AddListener(ConfirmSelection);
        returnButton.onClick.AddListener(GoBack);
        customizeButton.onClick.AddListener(GoCustomize);

        confirmButton.interactable   = false;
        customizeButton.interactable = true;
    }

    private void UpdateConfirmButton()
    {
        confirmButton.interactable = femaleCheckbox.isOn || maleCheckbox.isOn;
    }

    // ── CONFIRM → efface la customisation, sauvegarde le genre, lance le jeu ──
    private void ConfirmSelection()
    {
        // Supprime la clé de customisation pour que CharacterSpawner
        // n'affiche pas le custom character par erreur
        PlayerPrefs.DeleteKey(CustomizationData.PREFS_KEY);
        PlayerPrefs.Save();

        SaveCharacter();
        SceneManager.LoadScene(gameSceneIndex);
    }

    // ── CUSTOMIZE → efface le genre explicite, puis va en customisation ──
    private void GoCustomize()
    {
        // ✅ FIX PRINCIPAL : on supprime CHARACTER_KEY pour que CharacterSpawner
        // lise "none" (valeur par défaut) au lieu de "male" ou "female".
        // Ainsi hasExplicitGender = false → le custom armature s'affiche correctement.
        PlayerPrefs.DeleteKey(CHARACTER_KEY);
        PlayerPrefs.Save();

        // On NE supprime PAS PREFS_KEY ici — la customisation doit persister
        SceneManager.LoadScene(customizeSceneIndex);
    }

    private void SaveCharacter()
    {
        string selected = femaleCheckbox.isOn ? "female" : "male";
        PlayerPrefs.SetString(CHARACTER_KEY, selected);
        PlayerPrefs.Save();
        RealtimeDBManager.Instance?.UpdateCharacter(selected);
        Debug.Log($"Personnage sauvegardé : {selected}");
    }

    private void GoBack()
    {
        if (uiManager == null)
            uiManager = FindObjectOfType<UIManager>();
        uiManager?.ShowMenuFromDB();
    }

    public static bool   HasSelectedCharacter()   => PlayerPrefs.HasKey(CHARACTER_KEY);
    public static string GetSelectedCharacter()   => PlayerPrefs.GetString(CHARACTER_KEY, "");
    public static void   ResetCharacterSelection()
    {
        PlayerPrefs.DeleteKey(CHARACTER_KEY);
        PlayerPrefs.Save();
    }
}