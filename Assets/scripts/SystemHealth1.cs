using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SystemHealth1 : MonoBehaviour
{
    public static SystemHealth1 Instance;

    [Header("Santé du système")]
    public float maxHP     = 100f;
    public float currentHP;

    [Header("UI")]
    public Slider   healthSlider;
    public TMP_Text healthText;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance  = this;
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
        if (amount <= 0f) return;

        currentHP = Mathf.Max(0f, currentHP - amount);
        UpdateUI();
        Debug.Log($"[SystemHealth] -{amount} HP → {currentHP}/{maxHP}");

        if (currentHP <= 0f)
            OnSystemDead();
    }

    // ══════════════════════════════════════════
    //  SOIN — Health Patch
    // ══════════════════════════════════════════
    public void Heal(float amount)
    {
        if (amount <= 0f) return;

        currentHP = Mathf.Min(maxHP, currentHP + amount);
        UpdateUI();
        Debug.Log($"[SystemHealth] +{amount} HP → {currentHP}/{maxHP}");
    }

    // ══════════════════════════════════════════
    //  UI
    // ══════════════════════════════════════════
    private void UpdateUI()
    {
        float ratio = (maxHP > 0f) ? currentHP / maxHP : 0f;

        if (healthSlider != null)
            healthSlider.value = ratio;

        if (healthText != null)
            healthText.text = $"{Mathf.CeilToInt(currentHP)} / {Mathf.RoundToInt(maxHP)}";
    }

    // ══════════════════════════════════════════
    //  GAME OVER — slider tombe à 0
    // ══════════════════════════════════════════
    private void OnSystemDead()
    {
        Debug.Log("[SystemHealth] ⚠ Système totalement infecté → GAME OVER !");

        if (QuestManager2_Round2.Instance != null)
            QuestManager2_Round2.Instance.TriggerGameOver();
    }

    // ══════════════════════════════════════════
    //  ACCESSEURS UTILITAIRES
    // ══════════════════════════════════════════

    /// <summary>Retourne true si le système est encore en vie.</summary>
    public bool IsAlive => currentHP > 0f;

    /// <summary>Ratio HP entre 0 et 1.</summary>
    public float HealthRatio => (maxHP > 0f) ? currentHP / maxHP : 0f;
}