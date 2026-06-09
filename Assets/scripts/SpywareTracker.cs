// ════════════════════════════════════════════════════════
//  SpywareTracker.cs  —  Attacher sur un GameObject UI (Canvas)
//  Dépendances : UnityEngine.UI
// ════════════════════════════════════════════════════════
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SpywareTracker : MonoBehaviour
{
    [Header("── Références ──")]
    public Camera       mainCamera;          // Caméra principale
    public Canvas       hudCanvas;           // Canvas HUD (Screen Space - Overlay)

    [Header("── Flèche bord d'écran ──")]
    public Sprite       arrowSprite;         // Sprite flèche (simple triangle blanc suffit)
    public Color        arrowColor       = new Color(0.9f, 0.3f, 1f, 0.9f);
    public float        arrowSize        = 48f;
    public float        edgePadding      = 60f;   // distance du bord en pixels

    [Header("── Pulse monde 3D ──")]
    public float        pulseInterval    = 3f;    // secondes entre chaque pulse
    public float        pulseMaxRadius   = 5f;
    public float        pulseDuration    = 1.2f;
    public Color        pulseColor       = new Color(0.8f, 0.2f, 1f, 0.85f);
    public int          pulseSegments    = 48;

    [Header("── Distance texte ──")]
    public Font         distanceFont;            // laisser null = font par défaut
    public Color        distanceFontColor = new Color(1f, 0.7f, 1f, 1f);

    // ── État interne ──
    private List<SpywareAI>   spywares     = new List<SpywareAI>();
    private List<TrackerEntry> entries     = new List<TrackerEntry>();

    private class TrackerEntry
    {
        public SpywareAI    spyware;
        public Image        arrowImg;
        public TextMesh     pulseText;    // text 3D distance (world space)
        public LineRenderer pulseRing;
        public Coroutine    pulseRoutine;
        public Text         screenText;   // texte UI distance (screen space)
    }

    // ════════════════════════════════════════════════════════
    //  INIT
    // ════════════════════════════════════════════════════════

    void Start()
    {
        if (mainCamera == null) mainCamera = Camera.main;
        if (hudCanvas  == null) hudCanvas  = FindObjectOfType<Canvas>();

        // Trouver tous les SpywareAI de la scène
        SpywareAI[] found = FindObjectsOfType<SpywareAI>(true);
        foreach (var s in found)
            RegisterSpyware(s);

        StartCoroutine(WatchForNewSpywares());
    }

    // ── Enregistrer un spyware et créer ses éléments UI/3D ──
    public void RegisterSpyware(SpywareAI s)
    {
        if (s == null) return;
        foreach (var e in entries)
            if (e.spyware == s) return; // déjà enregistré

        TrackerEntry entry = new TrackerEntry();
        entry.spyware = s;

        // ── Flèche HUD ──
        if (hudCanvas != null)
        {
            GameObject arrowGO  = new GameObject("Arrow_" + s.gameObject.name);
            arrowGO.transform.SetParent(hudCanvas.transform, false);

            Image img           = arrowGO.AddComponent<Image>();
            img.sprite          = arrowSprite;   // peut être null, Unity met un carré blanc
            img.color           = arrowColor;
            img.rectTransform.sizeDelta = new Vector2(arrowSize, arrowSize);
            img.enabled         = false;
            entry.arrowImg      = img;

            // Texte distance sous la flèche
            GameObject txtGO    = new GameObject("Dist_" + s.gameObject.name);
            txtGO.transform.SetParent(arrowGO.transform, false);
            Text txt            = txtGO.AddComponent<Text>();
            txt.text            = "";
            txt.fontSize        = 18;
            txt.alignment       = TextAnchor.MiddleCenter;
            txt.color           = distanceFontColor;
            if (distanceFont != null) txt.font = distanceFont;
            RectTransform rt    = txtGO.GetComponent<RectTransform>();
            rt.sizeDelta        = new Vector2(80f, 24f);
            rt.anchoredPosition = new Vector2(0f, -arrowSize * 0.75f);
            entry.screenText    = txt;
        }

        // ── Anneau de pulse 3D ──
        GameObject ringGO      = new GameObject("PulseRing_" + s.gameObject.name);
        LineRenderer lr        = ringGO.AddComponent<LineRenderer>();
        lr.positionCount       = pulseSegments + 1;
        lr.useWorldSpace       = true;
        lr.loop                = false;
        lr.alignment           = LineAlignment.View;
        lr.startWidth          = 0.12f;
        lr.endWidth            = 0f;
        lr.material            = MakePulseMaterial();
        lr.enabled             = false;
        entry.pulseRing        = lr;

        entries.Add(entry);
        entry.pulseRoutine = StartCoroutine(PulseLoop(entry));
    }

    // ════════════════════════════════════════════════════════
    //  UPDATE — Flèche bord d'écran
    // ════════════════════════════════════════════════════════

    void Update()
    {
        if (mainCamera == null || hudCanvas == null) return;

        Rect screen = new Rect(0, 0, Screen.width, Screen.height);

        foreach (var entry in entries)
        {
            if (entry.spyware == null || entry.arrowImg == null) continue;

            // Ne montrer que si le spyware est visible (révélé)
            // On accède au champ via reflection ou propriété publique — ici on vérifie
            // si le GameObject est actif ; adapte selon ton accès à isVisible
            bool spywareRevealed = IsRevealed(entry.spyware);
            if (!spywareRevealed) { entry.arrowImg.enabled = false; continue; }

            Vector3 worldPos   = entry.spyware.transform.position + Vector3.up * 1f;
            Vector3 screenPos  = mainCamera.WorldToScreenPoint(worldPos);
            bool    inFrustum  = screenPos.z > 0
                                 && screen.Contains(new Vector2(screenPos.x, screenPos.y));

            if (inFrustum)
            {
                // Spyware visible à l'écran → cacher la flèche
                entry.arrowImg.enabled = false;
                if (entry.screenText != null) entry.screenText.text = "";
            }
            else
            {
                // Hors écran → afficher la flèche sur le bord
                entry.arrowImg.enabled = true;

                // Si derrière la caméra, inverser
                if (screenPos.z < 0) { screenPos.x = Screen.width  - screenPos.x;
                                       screenPos.y = Screen.height - screenPos.y; }

                // Calculer l'angle vers le centre de l'écran
                Vector2 center    = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
                Vector2 dir2D     = new Vector2(screenPos.x - center.x, screenPos.y - center.y).normalized;
                float   angle     = Mathf.Atan2(dir2D.y, dir2D.x) * Mathf.Rad2Deg;

                // Rotation de la flèche
                entry.arrowImg.rectTransform.rotation = Quaternion.Euler(0, 0, angle - 90f);

                // Position sur le bord de l'écran
                float halfW = Screen.width  * 0.5f - edgePadding;
                float halfH = Screen.height * 0.5f - edgePadding;
                float cosA  = Mathf.Cos(angle * Mathf.Deg2Rad);
                float sinA  = Mathf.Sin(angle * Mathf.Deg2Rad);
                float scale = Mathf.Min(
                    Mathf.Abs(halfW / (cosA != 0 ? cosA : 0.001f)),
                    Mathf.Abs(halfH / (sinA != 0 ? sinA : 0.001f)));
                Vector2 edgePos = new Vector2(center.x + cosA * scale, center.y + sinA * scale);
                entry.arrowImg.rectTransform.anchoredPosition =
                    edgePos - new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);

                // Distance
                float dist = Vector3.Distance(mainCamera.transform.position,
                                              entry.spyware.transform.position);
                if (entry.screenText != null)
                    entry.screenText.text = Mathf.RoundToInt(dist) + "m";

                // Faire pulser la couleur de la flèche
                float pulse = 0.65f + Mathf.Abs(Mathf.Sin(Time.time * 3f)) * 0.35f;
                entry.arrowImg.color = new Color(arrowColor.r, arrowColor.g, arrowColor.b,
                                                 arrowColor.a * pulse);
            }
        }
    }

    // ════════════════════════════════════════════════════════
    //  PULSE 3D — Anneau qui s'élargit périodiquement
    // ════════════════════════════════════════════════════════

    private IEnumerator PulseLoop(TrackerEntry entry)
    {
        while (true)
        {
            yield return new WaitForSeconds(pulseInterval);

            if (entry.spyware == null) yield break;
            if (!IsRevealed(entry.spyware)) continue;   // pas révélé → skip

            yield return StartCoroutine(PlayPulse(entry));
        }
    }

    private IEnumerator PlayPulse(TrackerEntry entry)
    {
        LineRenderer lr = entry.pulseRing;
        lr.enabled      = true;
        float t         = 0f;
        Vector3 center  = entry.spyware.transform.position + Vector3.up * 0.05f;

        while (t < pulseDuration)
        {
            t          += Time.deltaTime;
            float ratio  = t / pulseDuration;
            float radius = Mathf.Lerp(0.3f, pulseMaxRadius, ratio);
            float alpha  = Mathf.Lerp(1f, 0f, ratio * ratio);

            // Mettre à jour la couleur avec transparence
            Gradient g = new Gradient();
            g.SetKeys(
                new GradientColorKey[] {
                    new GradientColorKey(pulseColor, 0f),
                    new GradientColorKey(pulseColor, 1f) },
                new GradientAlphaKey[] {
                    new GradientAlphaKey(alpha, 0f),
                    new GradientAlphaKey(0f,    1f) });
            lr.colorGradient = g;
            lr.startWidth    = Mathf.Lerp(0.15f, 0.02f, ratio);

            // Dessiner le cercle horizontal
            for (int i = 0; i <= pulseSegments; i++)
            {
                float a = (float)i / pulseSegments * Mathf.PI * 2f;
                lr.SetPosition(i, center + new Vector3(
                    Mathf.Cos(a) * radius, 0f, Mathf.Sin(a) * radius));
            }
            yield return null;
        }
        lr.enabled = false;
    }

    // ════════════════════════════════════════════════════════
    //  UTILITAIRES
    // ════════════════════════════════════════════════════════

    // Vérifie si le spyware a été révélé — accède au champ privé via une propriété
    // ⚠️ Ajoute cette propriété publique dans SpywareAI : public bool IsVisible => isVisible;
    private bool IsRevealed(SpywareAI s)
    {
        return s.IsVisible; // propriété à ajouter dans SpywareAI (voir note ci-dessous)
    }

    private IEnumerator WatchForNewSpywares()
    {
        while (true)
        {
            yield return new WaitForSeconds(2f);
            SpywareAI[] found = FindObjectsOfType<SpywareAI>(true);
            foreach (var s in found) RegisterSpyware(s);
            // Nettoyer les entrées dont le spyware est détruit
            entries.RemoveAll(e => e.spyware == null);
        }
    }

    private Material MakePulseMaterial()
    {
        Shader sh = Shader.Find("Particles/Additive")
                 ?? Shader.Find("Legacy Shaders/Particles/Additive")
                 ?? Shader.Find("Standard");
        Material m = new Material(sh);
        Color c    = pulseColor;
        if (m.HasProperty("_Color"))     m.SetColor("_Color", c);
        if (m.HasProperty("_TintColor")) m.SetColor("_TintColor",
            new Color(c.r * 0.5f, c.g * 0.5f, c.b * 0.5f, 0.8f));
        m.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        m.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One);
        m.SetInt("_ZWrite", 0);
        m.renderQueue = 3000;
        return m;
    }
}