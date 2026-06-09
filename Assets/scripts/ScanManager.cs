using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ScanManager : MonoBehaviour
{
    public static ScanManager Instance;

    [Header("── UI ──")]
    public Button            scanButton;
    public TextMeshProUGUI   scanButtonText;
    public int               spybotRequired = 5;

    [Header("── Effets Scan ──")]
    public AudioClip         scanSound;
    private AudioSource      audioSource;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;

        if (scanButton != null)
            scanButton.onClick.AddListener(TryScan);

        UpdateButtonState();
    }

    void Update()
    {
        UpdateButtonState();
    }

    // ── Active/désactive le bouton selon le stock de spybots ──
    void UpdateButtonState()
    {
        if (scanButton == null) return;

        bool canScan = GameManager.spybot >= spybotRequired;
        scanButton.interactable = canScan;

        if (scanButtonText != null)
        {
            // ✅ Pas d'emojis : TMP ne les trouve pas dans LiberationSans SDF
            scanButtonText.text = canScan
                ? $"[SCAN] {GameManager.spybot}/{spybotRequired}"
                : $"[LOCK] {GameManager.spybot}/{spybotRequired}";
        }
    }

    // ── Bouton cliqué ──
    public void TryScan()
    {
        if (GameManager.spybot < spybotRequired)
        {
            Debug.Log("Pas assez de spybots pour scanner !");
            return;
        }

        // Dépenser les spybots
        for (int i = 0; i < spybotRequired; i++)
            GameManager.Instance.UseSpybot();

        // ✅ Révéler TOUS les spywares invisibles – de façon permanente
        // Reveal() pose isVisible=true et réactive les renderers ;
        // SpywareAI n'a aucun chemin de code qui repasserait en invisible ensuite.
        SpywareAI[] allSpywares = FindObjectsOfType<SpywareAI>();
        int count = 0;
        foreach (var spy in allSpywares)
        {
            spy.Reveal();   // interne : if (isVisible) return; donc idempotent
            count++;
        }

        // Son
        if (scanSound != null) audioSource.PlayOneShot(scanSound);

        Debug.Log($"[ScanManager] {count} spyware(s) révélé(s) définitivement !");
    }
}