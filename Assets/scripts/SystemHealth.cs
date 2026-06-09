using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SystemHealth : MonoBehaviour
{
    public static SystemHealth Instance;

    [Header("Santé du système")]
    public float maxHP = 100f;
    public float currentHP;

    [Header("UI")]
    public Slider healthSlider;
    public TMP_Text healthText;

    void Awake()
    {
        Instance = this;
        currentHP = maxHP;
    }

    void Start()
    {
        UpdateUI();
    }

    // ══════════════════════════════════════════
    //  DÉGÂTS
    // ══════════════════════════════════════════
    public void TakeDamage(float amount)
    {
        currentHP = Mathf.Max(0f, currentHP - amount);
        UpdateUI();
        Debug.Log($"[SystemHealth] -{amount} HP → {currentHP}/{maxHP}");

        if (currentHP <= 0f)
            OnSystemDead();
    }

    // ══════════════════════════════════════════
    //  STORE — Health Patch
    // ══════════════════════════════════════════
    public void Heal(float amount)
    {
        currentHP = Mathf.Min(maxHP, currentHP + amount);
        UpdateUI();
        Debug.Log($"[SystemHealth] +{amount} HP → {currentHP}/{maxHP}");
    }

    // ══════════════════════════════════════════
    //  UI
    // ══════════════════════════════════════════
    void UpdateUI()
    {
        if (healthSlider != null)
            healthSlider.value = currentHP / maxHP;

        if (healthText != null)
            healthText.text = $"{Mathf.CeilToInt(currentHP)}/{Mathf.RoundToInt(maxHP)}";
    }

    // ══════════════════════════════════════════
    //  GAME OVER SANTÉ
    // ══════════════════════════════════════════
    void OnSystemDead()
    {
        Debug.Log("[SystemHealth] Système totalement infecté !");
        // Appelle ton GameOver ici si besoin
        // ex: TimerManager.instance?.OnTimerEnd();
    }
}