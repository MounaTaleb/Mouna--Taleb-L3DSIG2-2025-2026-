using UnityEngine;
using UnityEngine.UI;
using System.Collections;

[RequireComponent(typeof(Toggle))]
public class SwitchToggleSimple : MonoBehaviour
{
    [Header("References")]
    [SerializeField] RectTransform handle;
    [SerializeField] Image background;

    [Header("Colors")]
    [SerializeField] Color backgroundOnColor = Color.green;
    [SerializeField] Color handleOnColor = Color.white;

    [Header("Animation")]
    [SerializeField] float animationDuration = 0.2f;
    
    [Header("Position Setup")]
    [Tooltip("Si coché, utilise la position actuelle du handle dans l'éditeur comme position OFF")]
    [SerializeField] bool useEditorPositionAsOff = true;

    [Header("🔐 Cyber Effects")]
    [SerializeField] bool enableCyberEffects = true;
    [SerializeField] Color cyberGlowColor = new Color(0f, 1f, 1f); // Cyan
    [SerializeField] float glowIntensity = 0.5f;
    [SerializeField] float particleDistance = 25f;

    Toggle toggle;
    Image handleImage;
    RawImage handleRawImage;

    // Positions
    float handleOffX;
    float handleOnX;
    float fixedY;

    Color backgroundOffColor;
    Color handleOffColor;

    Coroutine animationCoroutine;

    // ===== CYBER EFFECTS =====
    private Image handleGlow;
    private Image scanLine;
    private Image borderGlow;

    void Start()
    {
        toggle = GetComponent<Toggle>();
        toggle.transition = Selectable.Transition.None;

        if (handle == null || background == null)
        {
            Debug.LogError("SwitchToggleSimple: références manquantes");
            enabled = false;
            return;
        }

        handleImage = handle.GetComponent<Image>();
        handleRawImage = handle.GetComponent<RawImage>();

        if (handleImage == null && handleRawImage == null)
        {
            Debug.LogError("SwitchToggleSimple: Handle doit avoir Image ou RawImage");
            enabled = false;
            return;
        }

        // ===== INITIALISER LES EFFETS CYBER =====
        if (enableCyberEffects)
        {
            InitializeCyberEffects();
        }

        // Forcer le recalcul du layout
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(background.rectTransform);

        // 🔴 CAPTURER la position configurée dans l'éditeur
        Vector2 editorPosition = handle.anchoredPosition;
        fixedY = editorPosition.y;

        // Calcul des positions X basé sur la largeur du background
        RectTransform bgRect = background.rectTransform;
        float bgWidth = bgRect.rect.width;
        float handleWidth = handle.rect.width;

        // Padding horizontal (5 pixels de chaque côté)
        float padding = 5f;
        float maxOffset = (bgWidth - handleWidth) * 0.5f - padding;

        if (useEditorPositionAsOff)
        {
            // ✅ La position actuelle dans l'éditeur = position OFF (gauche)
            handleOffX = editorPosition.x;
            handleOnX = maxOffset;  // Position ON = à droite
        }
        else
        {
            // Alternative: calculer les positions symétriques
            handleOffX = -maxOffset;
            handleOnX = maxOffset;
        }

        Debug.Log($"[SwitchToggle] Initialized - OffX: {handleOffX}, OnX: {handleOnX}, " +
                  $"fixedY: {fixedY}, toggle.isOn: {toggle.isOn}");

        // Capturer les couleurs OFF depuis l'état actuel
        backgroundOffColor = background.color;
        handleOffColor = handleImage != null ? handleImage.color : handleRawImage.color;

        // S'abonner aux changements
        toggle.onValueChanged.AddListener(OnToggleValueChanged);

        // ✅ Appliquer l'état initial sans animation
        SetVisualImmediate(toggle.isOn);
    }

    // ===== INITIALISER LES EFFETS VISUELS =====
    void InitializeCyberEffects()
    {
        // 1. GLOW derrière le handle
        GameObject glowObj = new GameObject("HandleGlow");
        glowObj.transform.SetParent(handle.parent, false);
        glowObj.transform.SetSiblingIndex(handle.GetSiblingIndex()); // Derrière le handle
        
        handleGlow = glowObj.AddComponent<Image>();
        handleGlow.raycastTarget = false;
        
        RectTransform glowRect = handleGlow.rectTransform;
        glowRect.anchorMin = new Vector2(0.5f, 0.5f);
        glowRect.anchorMax = new Vector2(0.5f, 0.5f);
        glowRect.pivot = new Vector2(0.5f, 0.5f);
        glowRect.sizeDelta = handle.sizeDelta * 2f;
        glowRect.anchoredPosition = handle.anchoredPosition;
        
        handleGlow.color = new Color(cyberGlowColor.r, cyberGlowColor.g, cyberGlowColor.b, 0f);
        handleGlow.sprite = CreateCircleSprite(128);

        // 2. BORDER GLOW autour du background
        GameObject borderObj = new GameObject("BorderGlow");
        borderObj.transform.SetParent(background.transform.parent, false);
        borderObj.transform.SetSiblingIndex(background.transform.GetSiblingIndex()); // Derrière le background
        
        borderGlow = borderObj.AddComponent<Image>();
        borderGlow.raycastTarget = false;
        
        RectTransform borderRect = borderGlow.rectTransform;
        borderRect.anchorMin = background.rectTransform.anchorMin;
        borderRect.anchorMax = background.rectTransform.anchorMax;
        borderRect.pivot = background.rectTransform.pivot;
        borderRect.anchoredPosition = background.rectTransform.anchoredPosition;
        borderRect.sizeDelta = background.rectTransform.sizeDelta + new Vector2(6f, 6f);
        
        borderGlow.color = new Color(cyberGlowColor.r, cyberGlowColor.g, cyberGlowColor.b, 0f);
        borderGlow.sprite = CreateRoundedRectSprite(128);

        // 3. SCAN LINE
        GameObject scanObj = new GameObject("ScanLine");
        scanObj.transform.SetParent(background.transform, false);
        
        scanLine = scanObj.AddComponent<Image>();
        scanLine.raycastTarget = false;
        
        RectTransform scanRect = scanLine.rectTransform;
        scanRect.anchorMin = new Vector2(0f, 0.5f);
        scanRect.anchorMax = new Vector2(0f, 0.5f);
        scanRect.pivot = new Vector2(0.5f, 0.5f);
        scanRect.sizeDelta = new Vector2(2f, background.rectTransform.rect.height);
        scanRect.anchoredPosition = Vector2.zero;
        
        scanLine.color = new Color(cyberGlowColor.r, cyberGlowColor.g, cyberGlowColor.b, 0f);
    }

    void SetVisualImmediate(bool isOn)
    {
        float targetX = isOn ? handleOnX : handleOffX;
        handle.anchoredPosition = new Vector2(targetX, fixedY);

        background.color = isOn ? backgroundOnColor : backgroundOffColor;
        
        if (handleImage != null)
            handleImage.color = isOn ? handleOnColor : handleOffColor;
        else
            handleRawImage.color = isOn ? handleOnColor : handleOffColor;

        // Reset cyber effects
        if (enableCyberEffects && handleGlow != null)
        {
            handleGlow.rectTransform.anchoredPosition = handle.anchoredPosition;
            handleGlow.color = new Color(cyberGlowColor.r, cyberGlowColor.g, cyberGlowColor.b, 0f);
        }

        Debug.Log($"[SetVisualImmediate] isOn={isOn}, Position={handle.anchoredPosition}");
    }

    void OnToggleValueChanged(bool isOn)
    {
        Debug.Log($"[OnToggleValueChanged] isOn={isOn}");

        if (animationCoroutine != null)
            StopCoroutine(animationCoroutine);
        
        animationCoroutine = StartCoroutine(AnimateToState(isOn));
    }

    IEnumerator AnimateToState(bool targetIsOn)
    {
        Vector2 startPos = handle.anchoredPosition;
        float targetX = targetIsOn ? handleOnX : handleOffX;
        Vector2 targetPos = new Vector2(targetX, fixedY);

        Color startBgColor = background.color;
        Color startHandleColor = handleImage != null ? handleImage.color : handleRawImage.color;
        
        Color targetBgColor = targetIsOn ? backgroundOnColor : backgroundOffColor;
        Color targetHandleColor = targetIsOn ? handleOnColor : handleOffColor;

        Debug.Log($"[AnimateToState] From {startPos} to {targetPos}");

        // ===== DÉMARRER LES EFFETS CYBER SI ACTIVÉ =====
        if (enableCyberEffects && targetIsOn)
        {
            StartCoroutine(CyberGlowEffect());
            StartCoroutine(BorderGlowEffect());
            StartCoroutine(ScanLineEffect());
            StartCoroutine(SpawnCyberParticles(targetPos));
        }

        float elapsed = 0f;

        while (elapsed < animationDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / animationDuration);
            t = Mathf.SmoothStep(0f, 1f, t);

            // ✅ Interpoler uniquement X, garder Y fixe
            float currentX = Mathf.Lerp(startPos.x, targetPos.x, t);
            handle.anchoredPosition = new Vector2(currentX, fixedY);

            // Suivre le handle avec le glow
            if (enableCyberEffects && handleGlow != null)
            {
                handleGlow.rectTransform.anchoredPosition = handle.anchoredPosition;
            }

            background.color = Color.Lerp(startBgColor, targetBgColor, t);
            
            if (handleImage != null)
                handleImage.color = Color.Lerp(startHandleColor, targetHandleColor, t);
            else
                handleRawImage.color = Color.Lerp(startHandleColor, targetHandleColor, t);

            yield return null;
        }

        // ✅ Position finale précise
        handle.anchoredPosition = targetPos;
        background.color = targetBgColor;
        
        if (handleImage != null)
            handleImage.color = targetHandleColor;
        else
            handleRawImage.color = targetHandleColor;

        Debug.Log($"[AnimateToState] Complete - Final position: {handle.anchoredPosition}");
    }

    // ===== EFFETS CYBER =====

    IEnumerator CyberGlowEffect()
    {
        if (handleGlow == null) yield break;

        float duration = 0.4f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            
            // Pulse
            float alpha = Mathf.Sin(t * Mathf.PI) * glowIntensity;
            float scale = 1f + Mathf.Sin(t * Mathf.PI) * 0.3f;
            
            handleGlow.color = new Color(cyberGlowColor.r, cyberGlowColor.g, cyberGlowColor.b, alpha);
            handleGlow.rectTransform.localScale = Vector3.one * scale;
            
            yield return null;
        }
        
        handleGlow.color = new Color(cyberGlowColor.r, cyberGlowColor.g, cyberGlowColor.b, 0f);
        handleGlow.rectTransform.localScale = Vector3.one;
    }

    IEnumerator BorderGlowEffect()
    {
        if (borderGlow == null) yield break;

        float duration = 0.5f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            
            float alpha = Mathf.Sin(t * Mathf.PI) * 0.4f;
            borderGlow.color = new Color(cyberGlowColor.r, cyberGlowColor.g, cyberGlowColor.b, alpha);
            
            yield return null;
        }
        
        borderGlow.color = new Color(cyberGlowColor.r, cyberGlowColor.g, cyberGlowColor.b, 0f);
    }

    IEnumerator ScanLineEffect()
    {
        if (scanLine == null) yield break;

        RectTransform bgRect = background.rectTransform;
        float width = bgRect.rect.width;
        
        float startX = -width * 0.5f;
        float endX = width * 0.5f;
        
        float duration = 0.3f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            
            float x = Mathf.Lerp(startX, endX, t);
            scanLine.rectTransform.anchoredPosition = new Vector2(x, 0f);
            
            float alpha = Mathf.Sin(t * Mathf.PI) * 0.7f;
            scanLine.color = new Color(cyberGlowColor.r, cyberGlowColor.g, cyberGlowColor.b, alpha);
            
            yield return null;
        }
        
        scanLine.color = new Color(cyberGlowColor.r, cyberGlowColor.g, cyberGlowColor.b, 0f);
    }

    IEnumerator SpawnCyberParticles(Vector2 centerPos)
    {
        // Créer 6 particules
        for (int i = 0; i < 6; i++)
        {
            GameObject particleObj = new GameObject($"CyberParticle_{i}");
            particleObj.transform.SetParent(background.transform, false);
            
            Image particleImg = particleObj.AddComponent<Image>();
            particleImg.raycastTarget = false;
            particleImg.sprite = CreateCircleSprite(16);
            particleImg.color = cyberGlowColor;
            
            RectTransform particleRect = particleImg.rectTransform;
            particleRect.sizeDelta = new Vector2(3f, 3f);
            particleRect.anchoredPosition = centerPos;
            
            // Direction radiale
            float angle = i * 60f;
            Vector2 direction = new Vector2(
                Mathf.Cos(angle * Mathf.Deg2Rad), 
                Mathf.Sin(angle * Mathf.Deg2Rad)
            );
            
            StartCoroutine(AnimateCyberParticle(particleRect, particleImg, direction, centerPos));
            
            yield return new WaitForSeconds(0.02f);
        }
    }

    IEnumerator AnimateCyberParticle(RectTransform particleRect, Image particleImg, Vector2 direction, Vector2 origin)
    {
        float lifetime = 0.4f;
        float elapsed = 0f;
        
        while (elapsed < lifetime && particleRect != null)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / lifetime;
            
            Vector2 pos = origin + direction * particleDistance * t;
            particleRect.anchoredPosition = pos;
            
            float alpha = 1f - t;
            particleImg.color = new Color(cyberGlowColor.r, cyberGlowColor.g, cyberGlowColor.b, alpha);
            
            yield return null;
        }
        
        if (particleRect != null)
            Destroy(particleRect.gameObject);
    }

    // ===== UTILITAIRES =====

    Sprite CreateCircleSprite(int resolution)
    {
        Texture2D tex = new Texture2D(resolution, resolution);
        Color[] pixels = new Color[resolution * resolution];
        
        Vector2 center = new Vector2(resolution * 0.5f, resolution * 0.5f);
        float radius = resolution * 0.5f;
        
        for (int y = 0; y < resolution; y++)
        {
            for (int x = 0; x < resolution; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), center);
                float alpha = 1f - Mathf.Clamp01(dist / radius);
                alpha = Mathf.SmoothStep(0f, 1f, alpha);
                
                pixels[y * resolution + x] = new Color(1f, 1f, 1f, alpha);
            }
        }
        
        tex.SetPixels(pixels);
        tex.Apply();
        
        return Sprite.Create(tex, new Rect(0, 0, resolution, resolution), 
                           new Vector2(0.5f, 0.5f));
    }

    Sprite CreateRoundedRectSprite(int resolution)
    {
        Texture2D tex = new Texture2D(resolution, resolution);
        Color[] pixels = new Color[resolution * resolution];
        
        for (int y = 0; y < resolution; y++)
        {
            for (int x = 0; x < resolution; x++)
            {
                float distFromEdge = Mathf.Min(
                    Mathf.Min(x, resolution - x),
                    Mathf.Min(y, resolution - y)
                );
                
                float alpha = Mathf.Clamp01(distFromEdge / 8f);
                alpha = Mathf.SmoothStep(0f, 1f, alpha);
                
                pixels[y * resolution + x] = new Color(1f, 1f, 1f, alpha);
            }
        }
        
        tex.SetPixels(pixels);
        tex.Apply();
        
        return Sprite.Create(tex, new Rect(0, 0, resolution, resolution), 
                           new Vector2(0.5f, 0.5f));
    }

    void OnDestroy()
    {
        if (toggle != null)
            toggle.onValueChanged.RemoveListener(OnToggleValueChanged);
    }

#if UNITY_EDITOR
    // 🔧 Helper pour débug dans l'éditeur
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.P))
        {
            Debug.Log($"[DEBUG] Position actuelle: {handle.anchoredPosition}\n" +
                      $"OffX: {handleOffX}, OnX: {handleOnX}\n" +
                      $"Toggle isOn: {toggle.isOn}");
        }
    }

    // 📐 Visualiser les positions dans l'éditeur
    void OnDrawGizmosSelected()
    {
        if (handle == null || background == null) return;

        // Convertir les positions locales en world space pour les gizmos
        Vector3[] corners = new Vector3[4];
        background.rectTransform.GetWorldCorners(corners);
        
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(corners[0], corners[1]);
        Gizmos.DrawLine(corners[1], corners[2]);
        Gizmos.DrawLine(corners[2], corners[3]);
        Gizmos.DrawLine(corners[3], corners[0]);
    }
#endif
}