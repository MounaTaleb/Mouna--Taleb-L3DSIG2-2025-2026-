using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// StoreLevel2_1 — compatible avec l'UI Store 8 slots
/// Slots actifs  : 1-Time Booster | 2-Health Patch | 3-Antivirus Pack
///                 4-Spybot Radar | 5-Firewall
/// Slots verrouillés : 6 | 7 | 8  → "Coming Soon"
/// </summary>
public class StoreLevel2_1 : MonoBehaviour
{
    public static StoreLevel2_1 Instance;

    [Header("Panels")]
    public GameObject storePanel;
    public Button     openStoreButton;
    public Button     closeStoreButton;

    [Header("Slot 1 — Time Booster")]
    public Button buyTimeBoosterButton;
    public int    timeBoosterCost   = 10;
    public float  timeBoosterAmount = 20f;

    [Header("Slot 2 — Health Patch")]
    public Button buyHealthPatchButton;
    public int    healthPatchCost   = 15;
    public float  healthPatchAmount = 25f;

    [Header("Slot 3 — Antivirus Pack")]
    public Button buyAntivirusPackButton;
    public int    antivirusPackCost   = 20;
    public int    antivirusPackAmount = 5;

    [Header("Slot 4 — Spybot Radar")]
    public Button buySpybotRadarButton;
    public int    spybotRadarCost   = 20;
    public int    spybotRadarAmount = 5;

    [Header("Slot 5 — Firewall")]
    public Button buyFirewallButton;
    public int    firewallCost     = 35;
    public float  firewallDuration = 15f;

    [Header("Slots 6-7-8 — Coming Soon (locked)")]
    public Button comingSoonSlot6Button;
    public Button comingSoonSlot7Button;
    public Button comingSoonSlot8Button;

    [Header("Feedback")]
    public TextMeshProUGUI feedbackText;
    public float           feedbackDuration = 1.5f;

    [Header("References")]
    public TimerManager    timerManager;
    public SystemHealth    systemHealth;
    public SpybotFirewallQuizManager firewallManager;

    private Coroutine _feedbackCoroutine;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        if (storePanel != null) storePanel.SetActive(false);

        openStoreButton ?.onClick.AddListener(OpenStore);
        closeStoreButton?.onClick.AddListener(CloseStore);

        buyTimeBoosterButton  ?.onClick.AddListener(BuyTimeBooster);
        buyHealthPatchButton  ?.onClick.AddListener(BuyHealthPatch);
        buyAntivirusPackButton?.onClick.AddListener(BuyAntivirusPack);
        buySpybotRadarButton  ?.onClick.AddListener(BuySpybotRadar);
        buyFirewallButton     ?.onClick.AddListener(BuyFirewall);

        LockComingSoonSlot(comingSoonSlot6Button);
        LockComingSoonSlot(comingSoonSlot7Button);
        LockComingSoonSlot(comingSoonSlot8Button);

        if (feedbackText != null) feedbackText.gameObject.SetActive(false);
    }

    public void OpenStore()
    {
        if (storePanel != null) storePanel.SetActive(true);
        RefreshButtons();
    }

    public void CloseStore()
    {
        if (storePanel != null) storePanel.SetActive(false);
    }

    public void BuyTimeBooster()
    {
        if (!TrySpend(timeBoosterCost)) return;
        timerManager?.AddTime(timeBoosterAmount);
        QuestManager.Instance?.OnTimerBoosterBought();
        ShowFeedback($"+{timeBoosterAmount}s !");
        RefreshButtons();
    }

    public void BuyHealthPatch()
    {
        if (!TrySpend(healthPatchCost)) return;
        if (systemHealth != null)
        {
            float heal = systemHealth.maxHP * (healthPatchAmount / 100f);
            systemHealth.Heal(heal);
        }
        ShowFeedback($"+{healthPatchAmount}% HP !");
        RefreshButtons();
    }

    public void BuyAntivirusPack()
    {
        if (!TrySpend(antivirusPackCost)) return;
        GameManager.Instance.AddAntivirus(antivirusPackAmount);
        ShowFeedback($"+{antivirusPackAmount} Antivirus !");
        RefreshButtons();
    }

    public void BuySpybotRadar()
    {
        if (!TrySpend(spybotRadarCost)) return;
        GameManager.Instance?.AddSpybot(spybotRadarAmount);
        ShowFeedback($"+{spybotRadarAmount} Spybots !");
        RefreshButtons();
    }

    public void BuyFirewall()
    {
        if (!TrySpend(firewallCost)) return;
        CloseStore();
        if (firewallManager != null)
            firewallManager.ActivateFirewall(firewallDuration, firewallCost);
        else
            Debug.LogWarning("[StoreLevel2_1] FirewallManager non assigné !");
    }

    bool TrySpend(int cost)
    {
        if (GameManager.score < cost)
        {
            ShowFeedback("Pas assez de coins !");
            return false;
        }
        GameManager.Instance.AddCoin(-cost);
        return true;
    }

    void RefreshButtons()
    {
        SetInteractable(buyTimeBoosterButton,   GameManager.score >= timeBoosterCost);
        SetInteractable(buyHealthPatchButton,   GameManager.score >= healthPatchCost);
        SetInteractable(buyAntivirusPackButton, GameManager.score >= antivirusPackCost);
        SetInteractable(buySpybotRadarButton,   GameManager.score >= spybotRadarCost);
        SetInteractable(buyFirewallButton,      GameManager.score >= firewallCost);
    }

    static void SetInteractable(Button btn, bool state)
    {
        if (btn != null) btn.interactable = state;
    }

    static void LockComingSoonSlot(Button btn)
    {
        if (btn == null) return;
        btn.interactable = false;
        var label = btn.GetComponentInChildren<TextMeshProUGUI>();
        if (label != null) label.text = "COMING SOON";
    }

    void ShowFeedback(string msg)
    {
        if (feedbackText == null) return;
        if (_feedbackCoroutine != null) StopCoroutine(_feedbackCoroutine);
        _feedbackCoroutine = StartCoroutine(FeedbackRoutine(msg));
    }

    System.Collections.IEnumerator FeedbackRoutine(string msg)
    {
        feedbackText.text = msg;
        feedbackText.gameObject.SetActive(true);
        yield return new WaitForSeconds(feedbackDuration);
        feedbackText.gameObject.SetActive(false);
    }
}