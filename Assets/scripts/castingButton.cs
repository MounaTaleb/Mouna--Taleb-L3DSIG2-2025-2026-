using UnityEngine;
using UnityEngine.UI;
using StarterAssets;
using System.Collections;

/// <summary>
/// CastingButton v3.4
/// ✅ anti > 0  → anim + effet (StartCasting normal)
/// ✅ anti == 0 → anim SEULEMENT, pas d'effet (StartCastingAnimOnly)
/// ✅ UpdateUI() chaque frame → toujours synchronisé
/// </summary>
public class CastingButton : MonoBehaviour
{
    [Header("── Références ──")]
    public UICanvasControllerInput uiCanvas;

    [Header("── UI ──")]
    public Image glowRingImage;

    [Header("── Couleurs ──")]
    public Color normalColor   = new Color(0.35f, 0.10f, 0.70f, 1f);
    public Color activeColor   = new Color(0.75f, 0.20f, 1.00f, 1f);
    public Color disabledColor = new Color(0.18f, 0.08f, 0.25f, 0.55f);
    public Color glowColor     = new Color(0.85f, 0.30f, 1.00f, 0.65f);

    [Header("── Animation bouton ──")]
    [Range(2f, 20f)]      public float colorLerpSpeed = 10f;
    [Range(0.01f, 0.15f)] public float pulseAmount    = 0.07f;
    [Range(1f, 8f)]       public float pulseSpeed     = 4f;
    [Range(0.85f, 0.99f)] public float pressScale     = 0.90f;

    [Header("── Durée effet (secondes) ──")]
    public float effectDuration = 3f;

    // ── Composants ──
    private Button        _button;
    private Image         _buttonImage;
    private RectTransform _rectTransform;
    private Text          _buttonText;

    // ── Couleur smooth ──
    private Color _displayColor;
    private Color _targetColor;

    // ── Scale ──
    private Vector3 _originalScale;
    private float   _pulsePhase;
    private bool    _pressAnimPlaying;

    // ── État ──
    private bool  _isCasting   = false;
    private float _effectTimer = 0f;
    private float _shimmerPhase;

    // ════════════════════════════════════════════════════════
    //  HELPER
    // ════════════════════════════════════════════════════════

    private ThirdPersonController TPC =>
        uiCanvas != null ? uiCanvas.thirdPersonController : null;

    private bool AntiAvailable =>
        GameManager.Instance != null && GameManager.anti > 0;

    // ════════════════════════════════════════════════════════
    //  INIT
    // ════════════════════════════════════════════════════════

    void Start()
    {
        _button        = GetComponent<Button>();
        _buttonImage   = GetComponent<Image>();
        _rectTransform = GetComponent<RectTransform>();
        _buttonText    = GetComponentInChildren<Text>();
        _originalScale = _rectTransform != null ? _rectTransform.localScale : Vector3.one;

        if (_button != null)
            _button.onClick.AddListener(OnCastButtonClicked);

        if (uiCanvas == null)
            Debug.LogWarning("[CastingButton] uiCanvas non assigné !");

        _displayColor = AntiAvailable ? normalColor : disabledColor;
        _targetColor  = _displayColor;
        if (_buttonImage  != null) _buttonImage.color  = _displayColor;
        if (glowRingImage != null) glowRingImage.color = new Color(glowColor.r, glowColor.g, glowColor.b, 0f);

        UpdateUI();
    }

    // ════════════════════════════════════════════════════════
    //  UPDATE
    // ════════════════════════════════════════════════════════

    void Update()
    {
        UpdateUI();

        if (_isCasting)
        {
            _effectTimer -= Time.deltaTime;
            if (_effectTimer <= 0f)
            {
                Debug.Log("[ONDE] Auto-stop (timer expiré)");
                StopWave();
                return;
            }
        }

        _displayColor = Color.Lerp(_displayColor, _targetColor, Time.deltaTime * colorLerpSpeed);
        if (_buttonImage != null) _buttonImage.color = _displayColor;

        if (_isCasting && _rectTransform != null)
        {
            _pulsePhase += Time.deltaTime * pulseSpeed;
            _rectTransform.localScale = _originalScale * (1f + Mathf.Sin(_pulsePhase) * pulseAmount);
        }
        else if (!_pressAnimPlaying && _rectTransform != null)
        {
            _rectTransform.localScale = Vector3.Lerp(
                _rectTransform.localScale, _originalScale, Time.deltaTime * colorLerpSpeed);
        }

        UpdateGlowRing();
        UpdateShimmer();
    }

    private void UpdateGlowRing()
    {
        if (glowRingImage == null) return;
        float targetAlpha = _isCasting
            ? 0.45f + Mathf.Sin(_shimmerPhase * 1.3f) * 0.30f
            : 0f;
        Color c = glowColor;
        c.a = Mathf.Lerp(glowRingImage.color.a, targetAlpha, Time.deltaTime * 8f);
        glowRingImage.color = c;
        if (_isCasting && glowRingImage.rectTransform != null)
            glowRingImage.rectTransform.Rotate(0, 0, Time.deltaTime * 45f);
    }

    private void UpdateShimmer()
    {
        if (_buttonText == null) return;
        if (_isCasting)
        {
            _shimmerPhase += Time.deltaTime * 5f;
            float b = 0.85f + Mathf.Abs(Mathf.Sin(_shimmerPhase)) * 0.15f;
            _buttonText.color = new Color(b, b * 0.9f, 1f, 1f);
        }
        else
        {
            _buttonText.color = Color.Lerp(_buttonText.color, Color.white, Time.deltaTime * 5f);
        }
    }

    // ════════════════════════════════════════════════════════
    //  BOUTON PRESSÉ
    // ════════════════════════════════════════════════════════

    void OnCastButtonClicked()
    {
        StartCoroutine(PressScaleAnim());

        if (_isCasting)
        {
            StopWave();
            return;
        }

        if (TPC == null)
        {
            Debug.LogError("[ONDE] ThirdPersonController introuvable !");
            return;
        }

        if (AntiAvailable)
        {
            // ── Cas normal : anti > 0 → anim + effet ──
            uiCanvas.VirtualCastingInput(true);

            if (!TPC.IsCasting)
            {
                Debug.LogWarning("[ONDE] TPC n'a pas démarré (anti épuisé ?)");
                StartCoroutine(FlashPulse());
                UpdateUI();
                return;
            }

            Debug.Log($"[ONDE] ▶ Cast COMPLET lancé ! Anti restants={GameManager.anti}");
        }
        else
        {
            // ── Cas anti == 0 : anim SEULEMENT, pas d'effet ──
            TPC.StartCastingAnimOnly();

            if (!TPC.IsCasting)
            {
                Debug.LogWarning("[ONDE] TPC n'a pas démarré (StartCastingAnimOnly a échoué ?)");
                UpdateUI();
                return;
            }

            Debug.Log("[ONDE] ▶ Cast ANIM ONLY lancé (anti=0, pas d'effet)");
        }

        // ── Commun aux deux cas ──
        _isCasting   = true;
        _effectTimer = effectDuration;
        _pulsePhase  = 0f;
        _targetColor = activeColor;

        UpdateUI();
    }

    private IEnumerator PressScaleAnim()
    {
        if (_rectTransform == null) yield break;
        _pressAnimPlaying = true;
        float t = 0f;
        while (t < 0.08f)
        {
            t += Time.deltaTime;
            _rectTransform.localScale = _originalScale * Mathf.Lerp(1f, pressScale, t / 0.08f);
            yield return null;
        }
        _pressAnimPlaying = false;
    }

    // ════════════════════════════════════════════════════════
    //  STOP
    // ════════════════════════════════════════════════════════

    void StopWave()
    {
        if (!_isCasting) return;
        _isCasting   = false;
        _effectTimer = 0f;
        if (uiCanvas != null) uiCanvas.VirtualCastingInput(false);
        // Arrêt complet via TPC (StopCasting gère aussi le cas animOnly)
        if (TPC != null) TPC.StopCasting();
        UpdateUI();
        Debug.Log("[ONDE] ■ Cast arrêté");
    }

    // ════════════════════════════════════════════════════════
    //  UI
    // ════════════════════════════════════════════════════════

    void UpdateUI()
    {
        // Le bouton est TOUJOURS interactable (même sans anti, on peut lancer l'anim)
        if (_button != null)
            _button.interactable = true;

        if (!_isCasting)
            _targetColor = AntiAvailable ? normalColor : disabledColor;
    }

    // ════════════════════════════════════════════════════════
    //  FLASH — feedback visuel optionnel
    // ════════════════════════════════════════════════════════

    IEnumerator FlashPulse()
    {
        if (_rectTransform == null) yield break;
        for (int i = 0; i < 3; i++)
        {
            _targetColor = new Color(0.9f, 0.1f, 0.4f, 1f);
            float t = 0f;
            while (t < 0.12f) { t += Time.deltaTime; yield return null; }
            _targetColor = disabledColor;
            t = 0f;
            while (t < 0.10f) { t += Time.deltaTime; yield return null; }
        }
        UpdateUI();
    }
}