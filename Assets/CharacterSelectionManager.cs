using UnityEngine;
using UnityEngine.UI;

public class CharacterSelectionManager : MonoBehaviour
{
    [Header("Checkboxes (Toggles)")]
    public Toggle femaleCheckbox;
    public Toggle maleCheckbox;

    [Header("Buttons")]
    public Button confirmButton;
    public Button returnButton;

    [Header("References")]
    public UIManager uiManager;

    private const string CHARACTER_KEY = "SelectedCharacter";

    private void Start()
    {
        // ✅ Créer un ToggleGroup pour que les deux toggles
        //    se désactivent automatiquement l'un l'autre
        ToggleGroup group = gameObject.AddComponent<ToggleGroup>();
        group.allowSwitchOff = true;

        femaleCheckbox.group = group;
        maleCheckbox.group   = group;

        // Décocher les deux au départ
        femaleCheckbox.isOn = false;
        maleCheckbox.isOn   = false;

        // Écouter les changements
        femaleCheckbox.onValueChanged.AddListener(OnFemaleChanged);
        maleCheckbox.onValueChanged.AddListener(OnMaleChanged);

        // Boutons
        confirmButton.onClick.AddListener(ConfirmSelection);
        returnButton.onClick.AddListener(GoBack);

        // CONFIRM grisé au départ
        confirmButton.interactable = false;
    }

    // ── Appelé automatiquement quand le Toggle change ──
    private void OnFemaleChanged(bool isOn)
    {
        if (isOn) Debug.Log("🎮 Female sélectionnée");
        UpdateConfirmButton();
    }

    private void OnMaleChanged(bool isOn)
    {
        if (isOn) Debug.Log("🎮 Male sélectionné");
        UpdateConfirmButton();
    }

    // Active CONFIRM seulement si un des deux est coché
    private void UpdateConfirmButton()
    {
        confirmButton.interactable = femaleCheckbox.isOn || maleCheckbox.isOn;
    }

    // ── Confirmation ──
    private void ConfirmSelection()
    {
        string selected = femaleCheckbox.isOn ? "female" : "male";

        PlayerPrefs.SetString(CHARACTER_KEY, selected);
        PlayerPrefs.Save();
        Debug.Log($"💾 Personnage sauvegardé : {selected}");

        EnsureUIManager();
        uiManager?.ShowMenu();
    }

    // ── Retour ──
    private void GoBack()
    {
        EnsureUIManager();
        uiManager?.ShowWelcome();
    }

    private void EnsureUIManager()
    {
        if (uiManager == null)
            uiManager = FindObjectOfType<UIManager>();
    }

    // ── Utilitaires (pour AuthManager) ──
    public static bool HasSelectedCharacter()
    {
        return PlayerPrefs.HasKey(CHARACTER_KEY);
    }

    public static string GetSelectedCharacter()
    {
        return PlayerPrefs.GetString(CHARACTER_KEY, "");
    }

    public static void ResetCharacterSelection()
    {
        PlayerPrefs.DeleteKey(CHARACTER_KEY);
        PlayerPrefs.Save();
        Debug.Log("🔄 Sélection réinitialisée");
    }
}