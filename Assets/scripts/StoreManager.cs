using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class StoreManager : MonoBehaviour
{
    public static StoreManager Instance;

    [Header("Panels")]
    public GameObject storePanel;
    public Button openStoreButton;
    public Button closeStoreButton;

    [Header("Item 1 — Time Booster")]
    public Button buyTimeBoosterButton;
    public int    timeBoosterCost   = 10;
    public float  timeBoosterAmount = 20f;

    [Header("Item 2 — Health Patch")]
    public Button buyHealthPatchButton;
    public int    healthPatchCost   = 15;
    public float  healthPatchAmount = 25f;

    [Header("Item 3 — Antivirus Pack")]
    public Button buyAntivirusPackButton;
    public int    antivirusPackCost   = 20;
    public int    antivirusPackAmount = 5;

    [Header("Feedback")]
    public TextMeshProUGUI feedbackText;
    public float feedbackDuration = 1.5f;

    [Header("References")]
    public TimerManager timerManager;
    public SystemHealth systemHealth;

    private Coroutine feedbackCoroutine;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        if (storePanel != null) storePanel.SetActive(false);

        if (openStoreButton  != null) openStoreButton .onClick.AddListener(OpenStore);
        if (closeStoreButton != null) closeStoreButton.onClick.AddListener(CloseStore);

        if (buyTimeBoosterButton  != null) buyTimeBoosterButton .onClick.AddListener(BuyTimeBooster);
        if (buyHealthPatchButton  != null) buyHealthPatchButton .onClick.AddListener(BuyHealthPatch);
        if (buyAntivirusPackButton != null) buyAntivirusPackButton.onClick.AddListener(BuyAntivirusPack);

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

        if (timerManager != null)
            timerManager.AddTime(timeBoosterAmount);

        // ── QuestManager — Quest 3 ──
        if (QuestManager.Instance != null)
            QuestManager.Instance.OnTimerBoosterBought();

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
        if (buyTimeBoosterButton   != null) buyTimeBoosterButton  .interactable = GameManager.score >= timeBoosterCost;
        if (buyHealthPatchButton   != null) buyHealthPatchButton  .interactable = GameManager.score >= healthPatchCost;
        if (buyAntivirusPackButton != null) buyAntivirusPackButton.interactable = GameManager.score >= antivirusPackCost;
    }

    void ShowFeedback(string msg)
    {
        if (feedbackText == null) return;
        if (feedbackCoroutine != null) StopCoroutine(feedbackCoroutine);
        feedbackCoroutine = StartCoroutine(FeedbackRoutine(msg));
    }

    System.Collections.IEnumerator FeedbackRoutine(string msg)
    {
        feedbackText.text = msg;
        feedbackText.gameObject.SetActive(true);
        yield return new WaitForSeconds(feedbackDuration);
        feedbackText.gameObject.SetActive(false);
    }
}