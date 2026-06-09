using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuPanel : MonoBehaviour
{
    // ✅ Glisser le panel dans l'Inspector
    public GameObject panelComposants;

    void Start()
    {
        // ✅ Vérification
        if (panelComposants == null)
        {
            Debug.LogError("panelComposants non assigné !");
            return;
        }
        panelComposants.SetActive(false);
    }

    // ── Bouton AR ─────────────────────────────────────
    public void OuvrirPanel()
    {
        Debug.Log("Panel ouvert");
        panelComposants.SetActive(true);
    }

    // ── Bouton X ──────────────────────────────────────
    public void FermerPanel()
    {
        Debug.Log("Panel ferme");
        panelComposants.SetActive(false);
    }

    // ── 5 Boutons composants ──────────────────────────
    public void OuvrirBIOS() { Charger(0); }
    public void OuvrirRAM()  { Charger(1); }
    public void OuvrirCPU()  { Charger(2); }
    public void OuvrirGPU()  { Charger(3); }
    public void OuvrirSSD()  { Charger(4); }

    void Charger(int index)
    {
        Debug.Log("Chargement composant index : " + index);
        PlayerPrefs.SetInt("Composant", index);
        PlayerPrefs.Save();
        SceneManager.LoadScene(5);
    }
}