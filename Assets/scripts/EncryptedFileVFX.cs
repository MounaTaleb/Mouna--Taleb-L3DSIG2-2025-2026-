using UnityEngine;
using System.Collections;

/// <summary>
/// EncryptedFileVFX — Effets visuels permanents sur un fichier crypté.
/// 
/// À attacher sur le prefab "chiffré" (remplace le cube rouge basique).
/// Affiche : cadenas 3D, texte .LOCKED, anneaux de corruption,
/// lumière rouge pulsante, particules de données mortes.
/// </summary>
public class EncryptedFileVFX : MonoBehaviour
{
    [Header("Couleurs")]
    [ColorUsage(true, true)] public Color lockRed = new Color(1f, 0.05f, 0.05f, 1f);
    [ColorUsage(true, true)] public Color darkRed = new Color(0.4f, 0f, 0f, 1f);
    [ColorUsage(true, true)] public Color neonOrange = new Color(1f, 0.35f, 0f, 1f);
    [ColorUsage(true, true)] public Color corpseWhite = new Color(0.8f, 0.8f, 0.85f, 1f);

    [Header("Paramètres")]
    public float floatAmplitude = 0.05f;
    public float floatSpeed = 1.5f;
    public int ringCount = 3;

    // ── refs ──
    private LineRenderer _lockBody;
    private LineRenderer _lockShackle;
    private LineRenderer[] _rings;
    private TextMesh _mainLabel;
    private TextMesh _subLabel;
    private TextMesh _skullLabel;
    private Light _redLight;
    private ParticleSystem _deadDataPS;

    private float _t = 0f;
    private Vector3 _basePos;

    // ══════════════════════════════════════════════════════
    void Start()
    {
        _basePos = transform.position;
        BuildLock();
        BuildRings();
        BuildLabels();
        BuildLight();
        BuildDeadDataParticles();
        BuildCorruptionOverlay();
    }

    void Update()
    {
        _t += Time.deltaTime;

        // Flottement sinueux
        transform.position = _basePos + Vector3.up * Mathf.Sin(_t * floatSpeed) * floatAmplitude;

        UpdateRings();
        UpdateLock();
        UpdateLight();
        UpdateLabels();

        if (Random.value < 0.008f) SpawnGlitchText();
    }

    // ══════════════════════════════════════════════════════
    //  CADENAS 3D
    // ══════════════════════════════════════════════════════
    private void BuildLock()
    {
        float topY = GetTopY() + 0.1f;

        // Corps
        var bodyGo = new GameObject("LockBody");
        bodyGo.transform.SetParent(transform);
        bodyGo.transform.localPosition = new Vector3(0f, topY, 0f);

        _lockBody = bodyGo.AddComponent<LineRenderer>();
        _lockBody.positionCount = 5;
        _lockBody.useWorldSpace = false;
        _lockBody.loop = false;
        _lockBody.alignment = LineAlignment.View;
        _lockBody.startWidth = 0.1f;
        _lockBody.endWidth = 0.1f;
        _lockBody.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        _lockBody.material = MakeMat(lockRed, 1f);

        float bw = 0.3f, bh = 0.25f;
        _lockBody.SetPosition(0, new Vector3(-bw, -bh, 0f));
        _lockBody.SetPosition(1, new Vector3( bw, -bh, 0f));
        _lockBody.SetPosition(2, new Vector3( bw,  bh, 0f));
        _lockBody.SetPosition(3, new Vector3(-bw,  bh, 0f));
        _lockBody.SetPosition(4, new Vector3(-bw, -bh, 0f));
        SetGrad(_lockBody, lockRed, Color.white);

        // Arche
        var shackGo = new GameObject("LockShackle");
        shackGo.transform.SetParent(transform);
        shackGo.transform.localPosition = new Vector3(0f, topY, 0f);

        _lockShackle = shackGo.AddComponent<LineRenderer>();
        _lockShackle.positionCount = 25;
        _lockShackle.useWorldSpace = false;
        _lockShackle.alignment = LineAlignment.View;
        _lockShackle.startWidth = 0.08f;
        _lockShackle.endWidth = 0.08f;
        _lockShackle.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        _lockShackle.material = MakeMat(Color.white, 1f);

        float sr = 0.22f;
        for (int i = 0; i <= 24; i++)
        {
            float ang = Mathf.PI + (float)i / 24f * Mathf.PI;
            _lockShackle.SetPosition(i,
                new Vector3(Mathf.Cos(ang) * sr, 0.28f + Mathf.Sin(ang) * sr, 0f));
        }
        SetGrad(_lockShackle, Color.white, lockRed);
    }

    private void UpdateLock()
    {
        if (_lockBody == null) return;
        float pulse = 0.5f + Mathf.Sin(_t * 5f) * 0.5f;
        Color c = Color.Lerp(lockRed, Color.white, pulse);
        _lockBody.startColor = c;
        _lockBody.endColor = c;
    }

    // ══════════════════════════════════════════════════════
    //  ANNEAUX DE CORRUPTION
    // ══════════════════════════════════════════════════════
    private void BuildRings()
    {
        _rings = new LineRenderer[ringCount];
        for (int i = 0; i < ringCount; i++)
        {
            var go = new GameObject("CorruptRing_" + i);
            go.transform.SetParent(transform);
            go.transform.localPosition = Vector3.zero;

            LineRenderer lr = go.AddComponent<LineRenderer>();
            lr.positionCount = 33;
            lr.useWorldSpace = false;
            lr.loop = true;
            lr.alignment = LineAlignment.View;
            lr.startWidth = 0.04f;
            lr.endWidth = 0.04f;
            lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lr.material = MakeMat(lockRed, 0.7f);
            SetRingPositions(lr, 0.7f + i * 0.45f);
            _rings[i] = lr;
        }
    }

    private void UpdateRings()
    {
        for (int i = 0; i < ringCount; i++)
        {
            if (_rings == null || _rings[i] == null) continue;
            float phase = _t * 2f + i * (Mathf.PI * 2f / ringCount);
            float pulse = 0.85f + Mathf.Sin(phase) * 0.15f;
            float alpha = 0.25f + Mathf.Sin(phase) * 0.55f;
            SetRingPositions(_rings[i], (0.7f + i * 0.45f) * pulse);
            SetRingAlpha(_rings[i], Mathf.Clamp01(alpha));
        }
    }

    // ══════════════════════════════════════════════════════
    //  LABELS LISIBLES
    // ══════════════════════════════════════════════════════
    private void BuildLabels()
    {
        float topY = GetTopY() + 0.85f;

        // .LOCKED — grand, blanc, gras
        var goMain = new GameObject("LockedLabel");
        goMain.transform.SetParent(transform);
        goMain.transform.localPosition = new Vector3(0f, topY, 0f);

        _mainLabel = goMain.AddComponent<TextMesh>();
        _mainLabel.text = "🔒 .LOCKED";
        _mainLabel.fontSize = 52;
        _mainLabel.characterSize = 0.055f;
        _mainLabel.anchor = TextAnchor.MiddleCenter;
        _mainLabel.alignment = TextAlignment.Center;
        _mainLabel.color = corpseWhite;
        _mainLabel.fontStyle = FontStyle.Bold;

        // AES-256
        var goSub = new GameObject("AESLabel");
        goSub.transform.SetParent(transform);
        goSub.transform.localPosition = new Vector3(0f, topY - 0.22f, 0f);

        _subLabel = goSub.AddComponent<TextMesh>();
        _subLabel.text = "AES-256  ENCRYPTED";
        _subLabel.fontSize = 24;
        _subLabel.characterSize = 0.045f;
        _subLabel.anchor = TextAnchor.MiddleCenter;
        _subLabel.alignment = TextAlignment.Center;
        _subLabel.color = lockRed;
        _subLabel.fontStyle = FontStyle.Bold;

        // 💀 icône
        var goSkull = new GameObject("SkullLabel");
        goSkull.transform.SetParent(transform);
        goSkull.transform.localPosition = new Vector3(0f, topY + 0.35f, 0f);

        _skullLabel = goSkull.AddComponent<TextMesh>();
        _skullLabel.text = "💀";
        _skullLabel.fontSize = 40;
        _skullLabel.characterSize = 0.1f;
        _skullLabel.anchor = TextAnchor.MiddleCenter;
        _skullLabel.alignment = TextAlignment.Center;
        _skullLabel.color = lockRed;
    }

    private void UpdateLabels()
    {
        if (_mainLabel != null)
        {
            FaceCamera(_mainLabel.gameObject);
            float g = 0.6f + Mathf.Sin(_t * 3f) * 0.4f;
            _mainLabel.color = Color.Lerp(corpseWhite, lockRed, g);
        }
        if (_subLabel != null) FaceCamera(_subLabel.gameObject);
        if (_skullLabel != null)
        {
            FaceCamera(_skullLabel.gameObject);
            float pulse = 1f + Mathf.Sin(_t * 2.5f) * 0.15f;
            _skullLabel.transform.localScale = Vector3.one * pulse;
        }
    }

    // ══════════════════════════════════════════════════════
    //  LUMIÈRE ROUGE PULSANTE
    // ══════════════════════════════════════════════════════
    private void BuildLight()
    {
        var go = new GameObject("RedLight");
        go.transform.SetParent(transform);
        go.transform.localPosition = new Vector3(0f, 1f, 0f);

        _redLight = go.AddComponent<Light>();
        _redLight.type = LightType.Point;
        _redLight.color = lockRed;
        _redLight.intensity = 3f;
        _redLight.range = 4f;
        _redLight.shadows = LightShadows.None;
    }

    private void UpdateLight()
    {
        if (_redLight != null)
            _redLight.intensity = 2.5f + Mathf.Sin(_t * 3.5f) * 1.5f;
    }

    // ══════════════════════════════════════════════════════
    //  PARTICULES "DONNÉES MORTES"
    // ══════════════════════════════════════════════════════
    private void BuildDeadDataParticles()
    {
        var psGo = new GameObject("DeadDataPS");
        psGo.transform.SetParent(transform);
        psGo.transform.localPosition = Vector3.zero;

        _deadDataPS = psGo.AddComponent<ParticleSystem>();
        var main = _deadDataPS.main;
        main.loop = true;
        main.playOnAwake = true;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.6f, 1.5f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.2f, 1f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.02f, 0.06f);
        main.startColor = new ParticleSystem.MinMaxGradient(lockRed, darkRed);
        main.gravityModifier = 0.15f;
        main.maxParticles = 40;

        var emission = _deadDataPS.emission;
        emission.rateOverTime = 12f;

        var shape = _deadDataPS.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(0.6f, 0.1f, 0.6f);

        var col = _deadDataPS.colorOverLifetime;
        col.enabled = true;
        Gradient g = new Gradient();
        g.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(neonOrange, 0f),
                new GradientColorKey(lockRed, 0.4f),
                new GradientColorKey(darkRed, 1f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(0.8f, 0f),
                new GradientAlphaKey(0.5f, 0.6f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        col.color = new ParticleSystem.MinMaxGradient(g);

        var ren = _deadDataPS.GetComponent<ParticleSystemRenderer>();
        ren.renderMode = ParticleSystemRenderMode.Billboard;
        ren.material = MakeMat(lockRed, 0.8f);
        _deadDataPS.Play();
    }

    // ══════════════════════════════════════════════════════
    //  OVERLAY DE CORRUPTION SUR LE MESH
    // ══════════════════════════════════════════════════════
    private void BuildCorruptionOverlay()
    {
        var rend = GetComponentInChildren<Renderer>();
        if (rend == null) return;

        // Clone le matériau pour le corrompre
        Material corruptMat = new Material(rend.material);
        Color baseColor = corruptMat.color;

        // Teinte rouge/noire
        Color corruptColor = Color.Lerp(baseColor, darkRed, 0.6f);
        corruptColor = Color.Lerp(corruptColor, lockRed, 0.2f);
        corruptMat.color = corruptColor;

        // Augmente le metallic pour effet "cyber-corrompu"
        corruptMat.SetFloat("_Metallic", 0.7f);
        corruptMat.SetFloat("_Glossiness", 0.3f);

        rend.material = corruptMat;
    }

    // ══════════════════════════════════════════════════════
    //  TEXTES GLITCH OCCASIONNELS
    // ══════════════════════════════════════════════════════
    private void SpawnGlitchText()
    {
        string[] texts = { "0xDEAD", "CORRUPT", "###ERR", "NO_KEY", "PAY$$$" };

        var go = new GameObject("GlitchText");
        go.transform.position = transform.position + new Vector3(
            Random.Range(-0.5f, 0.5f),
            Random.Range(0.3f, 0.9f),
            Random.Range(-0.5f, 0.5f));
        FaceCamera(go);

        TextMesh tm = go.AddComponent<TextMesh>();
        tm.text = texts[Random.Range(0, texts.Length)];
        tm.fontSize = 20;
        tm.characterSize = 0.08f;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.alignment = TextAlignment.Center;
        tm.fontStyle = FontStyle.Bold;
        tm.color = Random.value > 0.5f ? lockRed : neonOrange;

        StartCoroutine(FloatFade(go, tm));
    }

    private IEnumerator FloatFade(GameObject go, TextMesh tm)
    {
        float life = 0.8f;
        float t = 0f;
        Color col = tm.color;

        while (t < life)
        {
            if (go == null) yield break;
            t += Time.deltaTime;
            go.transform.position += Vector3.up * 0.8f * Time.deltaTime;
            FaceCamera(go);
            tm.color = new Color(col.r, col.g, col.b, Mathf.Lerp(1f, 0f, t / life));
            yield return null;
        }
        Destroy(go);
    }

    // ══════════════════════════════════════════════════════
    //  UTILS
    // ══════════════════════════════════════════════════════
    private float GetTopY()
    {
        var col = GetComponent<Collider>();
        if (col != null) return col.bounds.extents.y;
        var ren = GetComponentInChildren<Renderer>();
        if (ren != null) return ren.bounds.extents.y;
        return 0.5f;
    }

    private void FaceCamera(GameObject go)
    {
        if (go == null || Camera.main == null) return;
        go.transform.rotation = Camera.main.transform.rotation;
    }

    private void SetRingPositions(LineRenderer lr, float radius)
    {
        int n = lr.positionCount;
        for (int s = 0; s < n; s++)
        {
            float a = (float)s / (n - 1) * Mathf.PI * 2f;
            lr.SetPosition(s, new Vector3(Mathf.Cos(a) * radius, 0f, Mathf.Sin(a) * radius));
        }
    }

    private void SetRingAlpha(LineRenderer lr, float alpha)
    {
        Gradient g = new Gradient();
        g.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(lockRed, 0f),
                new GradientColorKey(Color.white, 0.5f),
                new GradientColorKey(lockRed, 1f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(alpha, 0f),
                new GradientAlphaKey(alpha * 0.7f, 0.8f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        lr.colorGradient = g;
    }

    private void SetGrad(LineRenderer lr, Color a, Color b)
    {
        Gradient g = new Gradient();
        g.SetKeys(
            new GradientColorKey[] { new GradientColorKey(a, 0f), new GradientColorKey(b, 1f) },
            new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) }
        );
        lr.colorGradient = g;
    }

    private Material MakeMat(Color col, float alpha = 1f)
    {
        Shader sh = Shader.Find("Legacy Shaders/Particles/Additive") ?? Shader.Find("Unlit/Color");
        Material m = new Material(sh);
        Color c = col; c.a = alpha; m.color = c;
        return m;
    }
}