using UnityEngine;
using UnityEngine.UI;

public class MenuController : MonoBehaviour
{
    [Header("Menu Buttons")]
    public Button[] menuButtons;

    [Header("Menu Panels")]
    public GameObject[] menuPanels;

    [Header("Close Buttons (bouton X de chaque panel)")]
    public Button[] closePanelButtons; // Le bouton X de chaque panel

    [Header("Colors")]
    public Color normalColor = new Color(1f, 1f, 1f, 1f);
    public Color activeColor = new Color(0.55f, 0.1f, 0.75f, 0.85f);

    private int currentActiveIndex = -1;
    private Image[] buttonImages;

    void Start()
    {
        buttonImages = new Image[menuButtons.Length];

        for (int i = 0; i < menuButtons.Length; i++)
        {
            buttonImages[i] = menuButtons[i].GetComponent<Image>();
            buttonImages[i].color = normalColor;

            int index = i;
            menuButtons[i].onClick.AddListener(() => SelectButton(index));
        }

        // ✅ CORRECTIF : Brancher chaque bouton X sur ClosePanel avec son index
        if (closePanelButtons != null)
        {
            for (int i = 0; i < closePanelButtons.Length; i++)
            {
                if (closePanelButtons[i] == null) continue;
                int index = i;
                closePanelButtons[i].onClick.AddListener(() => ClosePanel(index));
            }
        }

        // Ferme tous les panels au départ
        foreach (var panel in menuPanels)
            if (panel != null) panel.SetActive(false);
    }

    public void SelectButton(int buttonIndex)
    {
        // Toggle : reclique = ferme
        if (currentActiveIndex == buttonIndex)
        {
            ClosePanel(buttonIndex);
            return;
        }

        // Ferme l'ancien panel actif
        if (currentActiveIndex >= 0)
            ClosePanel(currentActiveIndex);

        // Ouvre le nouveau
        OpenPanel(buttonIndex);
    }

    private void OpenPanel(int index)
    {
        buttonImages[index].color = activeColor;

        if (menuPanels != null && index < menuPanels.Length && menuPanels[index] != null)
            menuPanels[index].SetActive(true);

        currentActiveIndex = index;
    }

    // ✅ Appelé par le bouton X du panel ET par le toggle
    public void ClosePanel(int index)
    {
        if (index < 0 || index >= buttonImages.Length) return;

        // Remet le bouton à sa couleur normale
        buttonImages[index].color = normalColor;

        // Ferme le panel
        if (menuPanels != null && index < menuPanels.Length && menuPanels[index] != null)
            menuPanels[index].SetActive(false);

        // ✅ Remet l'index à -1 pour éviter les conflits
        if (currentActiveIndex == index)
            currentActiveIndex = -1;
    }

    public void CloseAll()
    {
        if (currentActiveIndex >= 0)
            ClosePanel(currentActiveIndex);
    }
}