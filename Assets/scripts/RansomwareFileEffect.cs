using System.Collections;
using UnityEngine;

/// <summary>
/// RansomwareFileEffect v3.2 — Correction rotation prefab vertical.
/// 
/// Le prefab spawn maintenant avec rotation verticale (debout)
/// au lieu de horizontal (à plat).
/// </summary>
public class RansomwareFileEffect : MonoBehaviour
{
    // ── état ──
    private bool _isEncrypted = false;
    private bool _attackAuraOn = false;
    private bool _encryptionStarted = false;
    private bool _isCorrupting = false;

    // ── références ──
    private Renderer _renderer;
    private Material _originalMat;
    private Vector3 _originalScale;
    private Vector3 _originPos;

    // ── VFX roots ──
    private GameObject _attackRoot;
    private GameObject _corruptRoot;
    private GameObject _cryptRoot;

    // ── lumière ──
    private Light _redLight;
    private Light _strobeLight;

    // ── attaque ──
    private ParticleSystem _attackPS;
    private ParticleSystem _dataBurnPS;
    private GameObject _glitchFrame;
    private GameObject _warningSymbol;

    // ── corruption ──
    private Material _corruptMat;
    private float _corruptProgress = 0f;

    // ── crypté ──
    private LineRenderer _lockBody;
    private LineRenderer _lockShackle;
    private LineRenderer[] _encryptRings;
    private TextMesh _mainLabel;
    private TextMesh _subLabel;
    private GameObject _skullIcon;

    // ── couleurs ──
    private Color _red, _dark, _orange, _white;

    // ── prefab ──
    private GameObject _encryptedPrefab;
    private Vector3 _prefabOffset;

    // ── time ──
    private float _t = 0f;
    private const int RINGS = 3;

    // ── hex pool ──
    private static readonly string[] HEX_POOL = {
        "DEADBEEF", "AES-256", "RSA-2048",
        "FF00AA33", "C0FFEE42", ".LOCKED",
        "ENCRYPT", "###ERR", "4F3A2B1C",
        "CORRUPT", "VIRUS!!", "0xDEAD",
        "PWNED!!", "HAHAHA!", "NO_ESC"
    };

    // ══════════════════════════════════════════════════════
    void Awake()
    {
        _renderer = GetComponentInChildren<Renderer>();
        if (_renderer != null) _originalMat = _renderer.sharedMaterial;
        _originalScale = transform.localScale;
        _originPos = transform.position;
    }

    void Update()
    {
        _t += Time.deltaTime;
        if (_attackAuraOn) UpdateAttackVFX();
        if (_isCorrupting) UpdateCorruptionVFX();
        if (_isEncrypted) UpdateCryptedVFX();
    }

    // ══════════════════════════════════════════════════════
    //  API PUBLIQUE
    // ══════════════════════════════════════════════════════

    public void PlayAttackAura(Color red, Color orange)
    {
        _red = red; _orange = orange;
        if (_attackRoot != null) return;
        _attackAuraOn = true;
        BuildAttackVFX();
    }

    public void TriggerEncryption(Color red, Color dark, Color orange, Color white,
                                  GameObject encryptedPrefab, Vector3 prefabOffset)
    {
        if (_encryptionStarted) return;
        _encryptionStarted = true;
        _red = red; _dark = dark; _orange = orange; _white = white;
        _encryptedPrefab = encryptedPrefab;
        _prefabOffset = prefabOffset;
        StartCoroutine(EncryptSequence());
    }

    // ══════════════════════════════════════════════════════
    //  VFX ATTAQUE
    // ══════════════════════════════════════════════════════

    private void BuildAttackVFX()
    {
        _attackRoot = new GameObject("AttackVFX");
        _attackRoot.transform.SetParent(transform);
        _attackRoot.transform.localPosition = Vector3.zero;

        _glitchFrame = BuildGlitchFrame();
        _warningSymbol = BuildWarningSymbol();

        var psGo = new GameObject("AttackPS");
        psGo.transform.SetParent(_attackRoot.transform);
        psGo.transform.localPosition = Vector3.zero;
        _attackPS = psGo.AddComponent<ParticleSystem>();

        var main = _attackPS.main;
        main.loop = true;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.3f, 0.7f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.5f, 2.0f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.03f, 0.08f);
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(1f, 0f, 0f, 1f),
            new Color(1f, 0.3f, 0f, 0.8f));
        main.gravityModifier = -0.1f;
        main.maxParticles = 30;

        var emission = _attackPS.emission;
        emission.rateOverTime = 15f;

        var shape = _attackPS.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(0.6f, 0.1f, 0.6f);

        var ren = _attackPS.GetComponent<ParticleSystemRenderer>();
        ren.renderMode = ParticleSystemRenderMode.Billboard;
        ren.material = MakeMat(_red, 1f);
        _attackPS.Play();
    }

    private GameObject BuildGlitchFrame()
    {
        var root = new GameObject("GlitchFrame");
        root.transform.SetParent(_attackRoot.transform);
        root.transform.localPosition = Vector3.zero;

        Vector3[] corners = {
            new Vector3(-0.6f,  0.9f, 0f),
            new Vector3( 0.6f,  0.9f, 0f),
            new Vector3( 0.6f, -0.2f, 0f),
            new Vector3(-0.6f, -0.2f, 0f)
        };

        for (int i = 0; i < 4; i++)
        {
            var seg = new GameObject("FrameSeg_" + i);
            seg.transform.SetParent(root.transform);
            seg.transform.localPosition = Vector3.zero;

            LineRenderer lr = seg.AddComponent<LineRenderer>();
            lr.positionCount = 2;
            lr.useWorldSpace = false;
            lr.startWidth = 0.03f;
            lr.endWidth = 0.03f;
            lr.alignment = LineAlignment.View;
            lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lr.material = MakeMat(_red, 1f);
            lr.SetPosition(0, corners[i]);
            lr.SetPosition(1, corners[(i + 1) % 4]);
        }
        return root;
    }

    private GameObject BuildWarningSymbol()
    {
        var go = new GameObject("WarningSymbol");
        go.transform.SetParent(_attackRoot.transform);
        go.transform.localPosition = new Vector3(0f, 1.1f, 0f);

        var tm = go.AddComponent<TextMesh>();
        tm.text = "!";
        tm.fontSize = 72;
        tm.characterSize = 0.08f;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.alignment = TextAlignment.Center;
        tm.color = _orange;
        tm.fontStyle = FontStyle.Bold;

        return go;
    }

    private void UpdateAttackVFX()
    {
        if (_glitchFrame != null)
        {
            float blink = Mathf.PerlinNoise(_t * 8f, 0f) > 0.4f ? 1f : 0f;
            _glitchFrame.SetActive(blink > 0.5f);
            float jitter = 0.02f;
            _glitchFrame.transform.localPosition = new Vector3(
                Random.Range(-jitter, jitter),
                Random.Range(-jitter, jitter), 0f);
        }

        if (_warningSymbol != null)
        {
            float pulse = 1f + Mathf.Sin(_t * 6f) * 0.3f;
            _warningSymbol.transform.localScale = Vector3.one * pulse;
            var tm = _warningSymbol.GetComponent<TextMesh>();
            if (tm != null)
            {
                float alpha = 0.5f + Mathf.Sin(_t * 4f) * 0.5f;
                tm.color = new Color(_orange.r, _orange.g, _orange.b, alpha);
            }
            FaceCamera(_warningSymbol);
        }

        if (Random.value < 0.06f) SpawnHexText(small: true);
    }

    // ══════════════════════════════════════════════════════
    //  SÉQUENCE DE CRYPTAGE
    // ══════════════════════════════════════════════════════

    private IEnumerator EncryptSequence()
    {
        _attackAuraOn = false;
        if (_attackPS != null) _attackPS.Stop();
        if (_attackRoot != null) Destroy(_attackRoot, 0.2f);

        _isCorrupting = true;
        BuildCorruptionVFX();
        yield return StartCoroutine(CorruptionPhase());
        _isCorrupting = false;
        if (_corruptRoot != null) Destroy(_corruptRoot);

        GlitchExistingTexts();
        yield return StartCoroutine(ShakeAndFlashRed());

        BuildRedLight();
        BuildStrobeLight();

        BuildCryptedVFX();
        _isEncrypted = true;
        yield return StartCoroutine(LockSlamAnimation());

        for (int i = 0; i < 8; i++)
        {
            SpawnHexText(small: false);
            yield return new WaitForSeconds(0.04f);
        }

        // ═══ CORRECTION ROTATION : Spawn avec rotation verticale ═══
        Vector3 spawnPos = transform.position + _prefabOffset;

        // Rotation verticale : le prefab est debout (Y up)
        // Si ton prefab est modélisé à plat, on corrige ici
       Quaternion verticalRot = Quaternion.Euler(0f, transform.rotation.eulerAngles.y, 0f);

        GameObject newFile = null;

        if (_encryptedPrefab != null)
        {
            newFile = Instantiate(_encryptedPrefab, spawnPos, verticalRot);

            var vfx = newFile.GetComponent<EncryptedFileVFX>();
            if (vfx != null) vfx.enabled = false;

            var renderers = newFile.GetComponentsInChildren<Renderer>(true);
            foreach (var r in renderers) if (r != null) r.enabled = false;

            yield return StartCoroutine(SpawnFlashEffect(spawnPos));

            foreach (var r in renderers) if (r != null) r.enabled = true;
            if (vfx != null) vfx.enabled = true;
        }
        else
        {
            Debug.LogError("[RansomwareFileEffect] prefab non assigné — spawn fallback.");
            var fb = GameObject.CreatePrimitive(PrimitiveType.Cube);
            fb.transform.position = spawnPos;
            fb.transform.rotation = verticalRot;
            fb.transform.localScale = Vector3.one * 0.5f;
            var mr = fb.GetComponent<MeshRenderer>();
            if (mr != null) { mr.material = new Material(Shader.Find("Unlit/Color")); mr.material.color = _red; }
        }

        yield return StartCoroutine(FadeOutOriginal(duration: 0.3f));

        Destroy(gameObject);
    }

    // ── Flash d'apparition explosif ──
    private IEnumerator SpawnFlashEffect(Vector3 position)
    {
        var flashGo = new GameObject("SpawnFlash");
        flashGo.transform.position = position + Vector3.up * 0.5f;
        var flashLight = flashGo.AddComponent<Light>();
        flashLight.type = LightType.Point;
        flashLight.color = Color.white;
        flashLight.range = 6f;
        flashLight.intensity = 10f;

        var psGo = new GameObject("SpawnBurst");
        psGo.transform.position = position;
        var ps = psGo.AddComponent<ParticleSystem>();

        var main = ps.main;
        main.duration = 0.3f;
        main.loop = false;
        main.startLifetime = 0.5f;
        main.startSpeed = 4f;
        main.startSize = 0.1f;
        main.startColor = new ParticleSystem.MinMaxGradient(Color.white, _red);
        main.maxParticles = 30;

        var emission = ps.emission;
        emission.burstCount = 30;
        emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 30) });

        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.3f;

        var ren = ps.GetComponent<ParticleSystemRenderer>();
        ren.material = MakeMat(Color.white, 1f);
        ps.Play();

        float elapsed = 0f;
        while (elapsed < 0.3f)
        {
            elapsed += Time.deltaTime;
            flashLight.intensity = Mathf.Lerp(10f, 0f, elapsed / 0.3f);
            yield return null;
        }

        Destroy(flashGo);
        Destroy(psGo, 1f);
    }

    // ── PHASE CORRUPTION ──

    private void BuildCorruptionVFX()
    {
        _corruptRoot = new GameObject("CorruptionVFX");
        _corruptRoot.transform.SetParent(transform);
        _corruptRoot.transform.localPosition = Vector3.zero;

        _corruptMat = new Material(Shader.Find("Standard"));
        _corruptMat.color = _red;
        _corruptMat.SetFloat("_Mode", 2);
        _corruptMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        _corruptMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        _corruptMat.SetInt("_ZWrite", 0);
        _corruptMat.DisableKeyword("_ALPHATEST_ON");
        _corruptMat.EnableKeyword("_ALPHABLEND_ON");
        _corruptMat.renderQueue = 3000;

        if (_renderer != null)
            _renderer.material = _corruptMat;

        var psGo = new GameObject("DataBurnPS");
        psGo.transform.SetParent(_corruptRoot.transform);
        psGo.transform.localPosition = Vector3.zero;
        _dataBurnPS = psGo.AddComponent<ParticleSystem>();

        var main = _dataBurnPS.main;
        main.loop = true;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.4f, 1.0f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(1f, 3f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.02f, 0.06f);
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(1f, 0.8f, 0f, 1f),
            new Color(1f, 0f, 0f, 0.8f));
        main.gravityModifier = 0.2f;
        main.maxParticles = 50;

        var emission = _dataBurnPS.emission;
        emission.rateOverTime = 30f;

        var shape = _dataBurnPS.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(0.5f, 0.5f, 0.5f);

        var col = _dataBurnPS.colorOverLifetime;
        col.enabled = true;
        Gradient g = new Gradient();
        g.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(Color.yellow, 0f),
                new GradientColorKey(_orange, 0.3f),
                new GradientColorKey(_red, 0.7f),
                new GradientColorKey(_dark, 1f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(0.8f, 0.5f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        col.color = new ParticleSystem.MinMaxGradient(g);

        var ren = _dataBurnPS.GetComponent<ParticleSystemRenderer>();
        ren.renderMode = ParticleSystemRenderMode.Billboard;
        ren.material = MakeMat(_orange, 1f);
        _dataBurnPS.Play();
    }

    private void UpdateCorruptionVFX()
    {
        if (_corruptMat == null || _renderer == null) return;
        _corruptProgress += Time.deltaTime;
        float progress = Mathf.Clamp01(_corruptProgress / 0.8f);

        Color baseColor = _originalMat != null ? _originalMat.color : Color.white;
        Color corruptColor = Color.Lerp(baseColor, _red, progress);
        corruptColor = Color.Lerp(corruptColor, _dark, progress * 0.5f);
        _corruptMat.color = corruptColor;

        float shakeIntensity = Mathf.Lerp(0.02f, 0.15f, progress);
        transform.position = _originPos + new Vector3(
            Random.Range(-shakeIntensity, shakeIntensity),
            Random.Range(-shakeIntensity * 0.5f, shakeIntensity * 0.5f),
            Random.Range(-shakeIntensity, shakeIntensity));

        float scalePulse = 1f + Mathf.Sin(_t * 10f) * 0.1f * progress;
        transform.localScale = _originalScale * scalePulse;
    }

    private IEnumerator CorruptionPhase()
    {
        float elapsed = 0f;
        while (elapsed < 0.8f)
        {
            elapsed += Time.deltaTime;
            if (Random.value < 0.15f) SpawnCorruptText();
            yield return null;
        }
        transform.position = _originPos;
        transform.localScale = _originalScale;
    }

    private void SpawnCorruptText()
    {
        var go = new GameObject("CorruptText");
        go.transform.position = transform.position + new Vector3(
            Random.Range(-0.5f, 0.5f),
            Random.Range(0.3f, 0.8f),
            Random.Range(-0.5f, 0.5f));
        FaceCamera(go);

        TextMesh tm = go.AddComponent<TextMesh>();
        string[] corruptTexts = { "CORRUPT", "ERROR", "VIRUS", "DEAD", "LOCKED", "PWNED" };
        tm.text = corruptTexts[Random.Range(0, corruptTexts.Length)];
        tm.fontSize = 20 + Random.Range(0, 16);
        tm.characterSize = 0.08f;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.alignment = TextAlignment.Center;
        tm.fontStyle = FontStyle.Bold;
        tm.color = Random.value > 0.5f ? _red : _orange;

        StartCoroutine(FloatFadeFast(go, tm));
    }

    private IEnumerator FloatFadeFast(GameObject go, TextMesh tm)
    {
        float life = 0.4f;
        float t = 0f;
        Color col = tm.color;

        while (t < life)
        {
            if (go == null) yield break;
            t += Time.deltaTime;
            go.transform.position += Vector3.up * 2f * Time.deltaTime;
            FaceCamera(go);
            float jitter = 0.03f;
            go.transform.position += new Vector3(
                Random.Range(-jitter, jitter), 0f, Random.Range(-jitter, jitter));
            tm.color = new Color(col.r, col.g, col.b, Mathf.Lerp(1f, 0f, t / life));
            yield return null;
        }
        Destroy(go);
    }

    // ── Glitch textes existants ──

    private void GlitchExistingTexts()
    {
        foreach (var tm in GetComponentsInChildren<TextMesh>())
            if (tm.text.Contains("10101") || tm.text.Contains("1010") || tm.text.Contains("File"))
                StartCoroutine(AnimGlitchText(tm));
    }

    private IEnumerator AnimGlitchText(TextMesh tm)
    {
        if (tm == null) yield break;
        Vector3 basePos = tm.transform.localPosition;
        Vector3 baseScale = tm.transform.localScale;
        float elapsed = 0f;

        while (elapsed < 0.6f)
        {
            elapsed += Time.deltaTime;
            tm.transform.localPosition = basePos + new Vector3(
                Random.Range(-0.06f, 0.06f),
                Random.Range(-0.03f, 0.03f), 0f);

            float r = Random.value;
            tm.color = r < 0.3f ? Color.white :
                       r < 0.6f ? _red :
                       r < 0.8f ? _orange : Color.cyan;

            if (Random.value < 0.2f)
                tm.text = HEX_POOL[Random.Range(0, HEX_POOL.Length)];

            FaceCamera(tm.gameObject);
            yield return null;
        }

        tm.text = "ENCRYPTED";
        tm.color = _red;
        tm.fontStyle = FontStyle.Bold;
        tm.fontSize = Mathf.Max(tm.fontSize, 32);
        tm.transform.localPosition = basePos;

        float pulse = 0f;
        while (pulse < 0.3f)
        {
            pulse += Time.deltaTime;
            float s = 1f + Mathf.Sin(pulse / 0.3f * Mathf.PI) * 0.5f;
            tm.transform.localScale = baseScale * s;
            yield return null;
        }
        tm.transform.localScale = baseScale;
    }

    // ── Shake + flash ROUGE ──

    private IEnumerator ShakeAndFlashRed()
    {
        if (_renderer == null) yield break;

        Material flashMat = new Material(Shader.Find("Unlit/Color"));
        flashMat.color = _red;
        _renderer.material = flashMat;

        float elapsed = 0f;
        while (elapsed < 0.3f)
        {
            elapsed += Time.deltaTime;
            float shake = Mathf.Lerp(0.2f, 0f, elapsed / 0.3f);
            transform.position = _originPos + new Vector3(
                Random.Range(-shake, shake),
                Random.Range(-shake * 0.5f, shake * 0.5f),
                Random.Range(-shake, shake));
            float s = 1f + Mathf.Sin(elapsed / 0.3f * Mathf.PI) * 0.25f;
            transform.localScale = _originalScale * s;

            if (Mathf.Sin(elapsed * 30f) > 0f)
                flashMat.color = _red;
            else
                flashMat.color = Color.white;

            yield return null;
        }

        transform.position = _originPos;
        transform.localScale = _originalScale;
    }

    // ── Fond progressif de l'original ──

    private IEnumerator FadeOutOriginal(float duration)
    {
        if (_renderer == null) yield break;

        Material fadeMat = new Material(Shader.Find("Standard"));
        fadeMat.color = _corruptMat != null ? _corruptMat.color : (_originalMat != null ? _originalMat.color : Color.white);
        fadeMat.SetFloat("_Mode", 2);
        fadeMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        fadeMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        fadeMat.SetInt("_ZWrite", 0);
        fadeMat.EnableKeyword("_ALPHABLEND_ON");
        fadeMat.renderQueue = 3000;

        _renderer.material = fadeMat;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, elapsed / duration);
            Color c = fadeMat.color;
            c.a = alpha;
            fadeMat.color = c;

            float scale = Mathf.Lerp(1f, 0.5f, elapsed / duration);
            transform.localScale = _originalScale * scale;

            yield return null;
        }
    }

    // ── Lumières ──

    private void BuildRedLight()
    {
        var go = new GameObject("RedLight");
        go.transform.SetParent(transform);
        go.transform.localPosition = new Vector3(0f, 1.5f, 0f);

        _redLight = go.AddComponent<Light>();
        _redLight.type = LightType.Point;
        _redLight.color = _red;
        _redLight.intensity = 6f;
        _redLight.range = 5f;
        _redLight.shadows = LightShadows.None;
    }

    private void BuildStrobeLight()
    {
        var go = new GameObject("StrobeLight");
        go.transform.SetParent(transform);
        go.transform.localPosition = new Vector3(0f, 0.5f, 0f);

        _strobeLight = go.AddComponent<Light>();
        _strobeLight.type = LightType.Point;
        _strobeLight.color = Color.white;
        _strobeLight.intensity = 0f;
        _strobeLight.range = 3f;
        _strobeLight.shadows = LightShadows.None;

        StartCoroutine(StrobeCoroutine());
    }

    private IEnumerator StrobeCoroutine()
    {
        float elapsed = 0f;
        while (elapsed < 1.5f)
        {
            elapsed += Time.deltaTime;
            _strobeLight.intensity = Mathf.Sin(elapsed * 20f) > 0f ? 8f : 0f;
            yield return null;
        }
        if (_strobeLight != null) Destroy(_strobeLight.gameObject);
    }

    // ── Cadenas ──

    private void BuildCryptedVFX()
    {
        _cryptRoot = new GameObject("CryptedVFX");
        _cryptRoot.transform.SetParent(transform);
        _cryptRoot.transform.localPosition = Vector3.zero;

        float topY = GetTopY();

        var bodyGo = new GameObject("LockBody");
        bodyGo.transform.SetParent(_cryptRoot.transform);
        bodyGo.transform.localPosition = new Vector3(0f, topY + 0.05f, 0f);

        _lockBody = bodyGo.AddComponent<LineRenderer>();
        _lockBody.positionCount = 5;
        _lockBody.useWorldSpace = false;
        _lockBody.loop = false;
        _lockBody.alignment = LineAlignment.View;
        _lockBody.startWidth = 0.08f;
        _lockBody.endWidth = 0.08f;
        _lockBody.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        _lockBody.material = MakeMat(_red, 1f);

        float bW = 0.25f, bH = 0.2f;
        _lockBody.SetPosition(0, new Vector3(-bW, -bH, 0f));
        _lockBody.SetPosition(1, new Vector3( bW, -bH, 0f));
        _lockBody.SetPosition(2, new Vector3( bW,  bH, 0f));
        _lockBody.SetPosition(3, new Vector3(-bW,  bH, 0f));
        _lockBody.SetPosition(4, new Vector3(-bW, -bH, 0f));
        SetGrad(_lockBody, _red, Color.white);

        var shGo = new GameObject("LockShackle");
        shGo.transform.SetParent(_cryptRoot.transform);
        shGo.transform.localPosition = new Vector3(0f, topY + 0.05f, 0f);

        _lockShackle = shGo.AddComponent<LineRenderer>();
        _lockShackle.positionCount = 21;
        _lockShackle.useWorldSpace = false;
        _lockShackle.alignment = LineAlignment.View;
        _lockShackle.startWidth = 0.08f;
        _lockShackle.endWidth = 0.08f;
        _lockShackle.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        _lockShackle.material = MakeMat(Color.white, 1f);

        float sr = 0.18f;
        for (int i = 0; i <= 20; i++)
        {
            float ang = Mathf.PI + (float)i / 20f * Mathf.PI;
            _lockShackle.SetPosition(i,
                new Vector3(Mathf.Cos(ang) * sr, 0.22f + Mathf.Sin(ang) * sr, 0f));
        }
        SetGrad(_lockShackle, Color.white, _red);

        _encryptRings = new LineRenderer[RINGS];
        for (int i = 0; i < RINGS; i++)
        {
            var go = new GameObject("Ring_" + i);
            go.transform.SetParent(_cryptRoot.transform);
            go.transform.localPosition = Vector3.zero;

            LineRenderer lr = go.AddComponent<LineRenderer>();
            lr.positionCount = 33;
            lr.useWorldSpace = false;
            lr.loop = true;
            lr.alignment = LineAlignment.View;
            lr.startWidth = 0.04f;
            lr.endWidth = 0.04f;
            lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lr.material = MakeMat(_red, 0.8f);
            SetRingPositions(lr, 0.6f + i * 0.4f);
            _encryptRings[i] = lr;
        }

        BuildCleanLabels(topY);
        BuildSkullIcon(topY);
    }

    private void BuildCleanLabels(float topY)
    {
        var goMain = new GameObject("MainLabel");
        goMain.transform.SetParent(_cryptRoot.transform);
        goMain.transform.localPosition = new Vector3(0f, topY + 0.7f, 0f);

        _mainLabel = goMain.AddComponent<TextMesh>();
        _mainLabel.text = ".LOCKED";
        _mainLabel.fontSize = 56;
        _mainLabel.characterSize = 0.06f;
        _mainLabel.anchor = TextAnchor.MiddleCenter;
        _mainLabel.alignment = TextAlignment.Center;
        _mainLabel.color = Color.white;
        _mainLabel.fontStyle = FontStyle.Bold;

        var goSub = new GameObject("SubLabel");
        goSub.transform.SetParent(_cryptRoot.transform);
        goSub.transform.localPosition = new Vector3(0f, topY + 0.48f, 0f);

        _subLabel = goSub.AddComponent<TextMesh>();
        _subLabel.text = "AES-256  ENCRYPTED";
        _subLabel.fontSize = 28;
        _subLabel.characterSize = 0.05f;
        _subLabel.anchor = TextAnchor.MiddleCenter;
        _subLabel.alignment = TextAlignment.Center;
        _subLabel.color = _red;
        _subLabel.fontStyle = FontStyle.Bold;
    }

    private void BuildSkullIcon(float topY)
    {
        _skullIcon = new GameObject("SkullIcon");
        _skullIcon.transform.SetParent(_cryptRoot.transform);
        _skullIcon.transform.localPosition = new Vector3(0f, topY + 0.95f, 0f);

        var tm = _skullIcon.AddComponent<TextMesh>();
        tm.text = "X";
        tm.fontSize = 48;
        tm.characterSize = 0.1f;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.alignment = TextAlignment.Center;
        tm.color = _red;
        tm.fontStyle = FontStyle.Bold;
    }

    // ── Lock slam animation ──

    private IEnumerator LockSlamAnimation()
    {
        if (_lockBody != null) _lockBody.widthMultiplier = 0f;
        if (_lockShackle != null) _lockShackle.widthMultiplier = 0f;
        foreach (var r in _encryptRings) if (r != null) r.widthMultiplier = 0f;

        float elapsed = 0f;
        while (elapsed < 0.15f)
        {
            elapsed += Time.deltaTime;
            float frac = elapsed / 0.15f;
            frac = frac * frac * (3f - 2f * frac);

            if (_lockBody != null) _lockBody.widthMultiplier = frac;
            if (_lockShackle != null) _lockShackle.widthMultiplier = frac;
            yield return null;
        }

        for (int i = 0; i < RINGS; i++)
        {
            elapsed = 0f;
            while (elapsed < 0.2f)
            {
                elapsed += Time.deltaTime;
                float frac = elapsed / 0.2f;
                if (_encryptRings[i] != null) _encryptRings[i].widthMultiplier = frac;
                yield return null;
            }
            if (_strobeLight != null) _strobeLight.intensity = 5f;
        }

        float shakeT = 0f;
        while (shakeT < 0.2f)
        {
            shakeT += Time.deltaTime;
            float shake = Mathf.Lerp(0.1f, 0f, shakeT / 0.2f);
            transform.position = _originPos + new Vector3(
                Random.Range(-shake, shake),
                Random.Range(-shake, shake),
                Random.Range(-shake, shake));
            yield return null;
        }
        transform.position = _originPos;
    }

    // ══════════════════════════════════════════════════════
    //  UPDATE VFX IDLE (crypté)
    // ══════════════════════════════════════════════════════

    private void UpdateCryptedVFX()
    {
        for (int i = 0; i < RINGS; i++)
        {
            if (_encryptRings == null || _encryptRings[i] == null) continue;
            float phase = _t * 2.5f + i * (Mathf.PI * 2f / RINGS);
            float pulse = 0.85f + Mathf.Sin(phase) * 0.15f;
            float alpha = 0.25f + Mathf.Sin(phase) * 0.55f;
            SetRingPositions(_encryptRings[i], (0.6f + i * 0.4f) * pulse);
            SetRingAlpha(_encryptRings[i], Mathf.Clamp01(alpha));
        }

        if (_redLight != null)
            _redLight.intensity = 4f + Mathf.Sin(_t * 4f) * 2f;

        if (_lockBody != null)
        {
            float f = 0.5f + Mathf.Sin(_t * 6f) * 0.5f;
            Color c = Color.Lerp(_red, Color.white, f);
            _lockBody.startColor = c; _lockBody.endColor = c;
        }

        if (_mainLabel != null) FaceCamera(_mainLabel.gameObject);
        if (_subLabel != null) FaceCamera(_subLabel.gameObject);
        if (_skullIcon != null)
        {
            FaceCamera(_skullIcon);
            float skullPulse = 1f + Mathf.Sin(_t * 3f) * 0.2f;
            _skullIcon.transform.localScale = Vector3.one * skullPulse;
        }

        if (_mainLabel != null)
        {
            float g = 0.5f + Mathf.Sin(_t * 5f) * 0.5f;
            _mainLabel.color = Color.Lerp(Color.white, _red, g);
            _mainLabel.color = new Color(
                _mainLabel.color.r, _mainLabel.color.g, _mainLabel.color.b, 1f);
        }

        if (Random.value < 0.015f) SpawnHexText(small: false);
    }

    // ══════════════════════════════════════════════════════
    //  TEXTES HEX FLOTTANTS
    // ══════════════════════════════════════════════════════

    private void SpawnHexText(bool small)
    {
        var go = new GameObject("HexFloat");
        go.transform.position = transform.position + new Vector3(
            Random.Range(-0.5f, 0.5f),
            Random.Range(0.2f, 0.8f),
            Random.Range(-0.5f, 0.5f));
        FaceCamera(go);

        TextMesh tm = go.AddComponent<TextMesh>();
        tm.text = HEX_POOL[Random.Range(0, HEX_POOL.Length)];
        tm.fontSize = small ? 18 : 26;
        tm.characterSize = small ? 0.09f : 0.12f;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.alignment = TextAlignment.Center;
        tm.fontStyle = FontStyle.Bold;
        tm.color = Random.value > 0.4f ? Color.white : _red;

        StartCoroutine(FloatFade(go, tm));
    }

    private IEnumerator FloatFade(GameObject go, TextMesh tm)
    {
        float life = Random.Range(0.6f, 1.2f);
        float t = 0f;
        Color col = tm.color;

        while (t < life)
        {
            if (go == null) yield break;
            t += Time.deltaTime;
            go.transform.position += Vector3.up * 1.2f * Time.deltaTime;
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
        if (col != null) return col.bounds.extents.y + 0.4f;
        var ren = GetComponentInChildren<Renderer>();
        if (ren != null) return ren.bounds.extents.y + 0.4f;
        return 1.0f;
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
                new GradientColorKey(_red, 0f),
                new GradientColorKey(Color.white, 0.5f),
                new GradientColorKey(_red, 1f)
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