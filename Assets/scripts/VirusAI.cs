using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class VirusAI : MonoBehaviour
{
    [Header("Animation")]
    public string walkParamName   = "isWalking"; // Bool : marche normale
    public string bloqueParamName = "bloque";    // Bool : animation gelé

    [Header("Déplacement")]
    public float detectionRadius = 20f;
    public float attackRange     = 1.2f;

    [Header("Attaque")]
    public float timeBetweenHits = 0.5f;
    public float infectDelay     = 0.3f;

    [Header("Tag des fichiers")]
    public string fileTag = "File";

    [Header("══ Freeze Effect ══")]
    public float freezeDuration = 15f;
    [ColorUsage(true, true)]
    public Color frozenColor = new Color(0.0f, 0.6f, 0.2f, 1f);

    [Header("══ Freeze VFX Colors ══")]
    [ColorUsage(true, true)]
    public Color iceColor     = new Color(0.0f, 1.0f, 0.4f, 1f);
    [ColorUsage(true, true)]
    public Color iceDarkColor = new Color(0.0f, 0.4f, 0.15f, 1f);
    [ColorUsage(true, true)]
    public Color glowColor    = new Color(0.8f, 1.0f, 0.0f, 1f);

    private NavMeshAgent     agent;
    private Animator         anim;
    private List<FileTarget> allFiles = new List<FileTarget>();
    private FileTarget       target   = null;
    private bool             isBusy   = false;

    private bool      _isFrozen    = false;
    private Renderer  _renderer;
    private Color     _originalColor;
    private Vector3   _originalScale;

    private GameObject     _freezeRoot;
    private ParticleSystem _psIce;
    private LineRenderer[] _pulseRings;
    private GameObject     _freezeLabel;
    private LineRenderer[] _crystals;

    private float      _pulseTime = 0f;
    private const int  RING_COUNT    = 3;
    private const int  CRYSTAL_COUNT = 6;

    // ─────────────────────────────────────────────────────────
    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.stoppingDistance = attackRange * 0.8f;

        // Récupération de l'Animator (sur le même objet ou sur un enfant)
        anim = GetComponent<Animator>();
        if (anim == null) anim = GetComponentInChildren<Animator>();

        _renderer = GetComponentInChildren<Renderer>();
        if (_renderer != null) _originalColor = _renderer.material.color;
        _originalScale = transform.localScale;

        foreach (var go in GameObject.FindGameObjectsWithTag(fileTag))
        {
            var ft = go.GetComponent<FileTarget>();
            if (ft) allFiles.Add(ft);
        }

        BuildFreezeVFX();
    }

    void Update()
    {
        allFiles.RemoveAll(ft => ft == null);

        if (_isFrozen) { UpdateFreezeVFX(); return; }
        if (isBusy)    return;

        if (target == null) target = GetClosest();
        if (target == null)
        {
            agent.isStopped = true;
            SetAnim(false, false);
            return;
        }

        agent.isStopped = false;
        agent.SetDestination(target.transform.position);

        bool isMoving = agent.velocity.sqrMagnitude > 0.01f && !agent.isStopped;
        SetAnim(isMoving, false);

        float dist = DistXZ(transform.position, target.transform.position);
        if (dist <= attackRange && !agent.pathPending)
            StartCoroutine(AttackLoop());
    }

    IEnumerator AttackLoop()
    {
        isBusy          = true;
        agent.isStopped = true;
        agent.velocity  = Vector3.zero;
        SetAnim(false, false);

        while (target != null && !target.isInfected)
        {
            FaceTarget();
            bool done = target.ReceiveHit();
            if (done) break;
            yield return new WaitForSeconds(timeBetweenHits);
        }

        yield return new WaitForSeconds(infectDelay);
        target          = null;
        agent.isStopped = false;
        isBusy          = false;
    }

    // ═══════════════════════════════════════════════════════
    //  FREEZE PUBLIC API
    // ═══════════════════════════════════════════════════════
    public void Freeze(float duration)
    {
        if (_isFrozen) return;
        StartCoroutine(FreezeRoutine(duration));
    }

    private IEnumerator FreezeRoutine(float duration)
    {
        _isFrozen       = true;
        isBusy          = true;
        agent.isStopped = true;
        agent.velocity  = Vector3.zero;

        // ── Déclenche l'animation "bloque", arrête "isWalking" ──
        SetAnim(false, true);

        // ── QuestManager — Quest 2 ──
        if (QuestManager.Instance != null)
            QuestManager.Instance.OnVirusFrozen();

        StartCoroutine(FreezeAppear());

        // Phase entrée : teinte + gonflement
        float elapsed   = 0f;
        float enterTime = 0.5f;
        while (elapsed < enterTime)
        {
            elapsed += Time.deltaTime;
            float t  = elapsed / enterTime;
            if (_renderer != null) _renderer.material.color = Color.Lerp(_originalColor, frozenColor, t);
            transform.localScale = Vector3.Lerp(_originalScale, _originalScale * 1.08f, t);
            yield return null;
        }

        // Attente principale
        yield return new WaitForSeconds(duration - enterTime - 0.5f);

        StartCoroutine(FreezeDisappear());

        // Phase sortie : retour couleur + taille
        elapsed = 0f;
        while (elapsed < 0.5f)
        {
            elapsed += Time.deltaTime;
            float t  = elapsed / 0.5f;
            if (_renderer != null) _renderer.material.color = Color.Lerp(frozenColor, _originalColor, t);
            transform.localScale = Vector3.Lerp(_originalScale * 1.08f, _originalScale, t);
            yield return null;
        }

        if (_renderer != null) _renderer.material.color = _originalColor;
        transform.localScale = _originalScale;

        // ── Fin du gel : désactive "bloque", reprend la marche ──
        SetAnim(false, false);

        agent.isStopped = false;
        isBusy          = false;
        _isFrozen       = false;
        target          = null;
    }

    // ═══════════════════════════════════════════════════════
    //  HELPER ANIMATION CENTRALE
    // ═══════════════════════════════════════════════════════

    /// <summary>
    /// Met à jour les deux paramètres Animator en un seul appel.
    /// walking = true  → animation de marche
    /// bloque  = true  → animation gelé  (prend le dessus)
    /// </summary>
    private void SetAnim(bool walking, bool bloque)
    {
        if (anim == null) return;
        anim.SetBool(walkParamName,   walking);
        anim.SetBool(bloqueParamName, bloque);
    }

    // ═══════════════════════════════════════════════════════
    //  FREEZE VFX
    // ═══════════════════════════════════════════════════════
    private void BuildFreezeVFX()
    {
        _freezeRoot = new GameObject("FreezeVFX");
        _freezeRoot.transform.SetParent(transform);
        _freezeRoot.transform.localPosition = Vector3.zero;
        _freezeRoot.SetActive(false);

        _psIce       = BuildIceParticles();
        _pulseRings  = BuildPulseRings();
        _crystals    = BuildCrystals();
        _freezeLabel = BuildFreezeLabel();
    }

    private ParticleSystem BuildIceParticles()
    {
        GameObject go = new GameObject("IcePS");
        go.transform.SetParent(_freezeRoot.transform);
        go.transform.localPosition = Vector3.zero;

        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.loop            = true;
        main.playOnAwake     = false;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startLifetime   = new ParticleSystem.MinMaxCurve(0.6f, 1.4f);
        main.startSpeed      = new ParticleSystem.MinMaxCurve(0.5f, 2.5f);
        main.startSize       = new ParticleSystem.MinMaxCurve(0.03f, 0.12f);
        main.startColor      = new ParticleSystem.MinMaxGradient(iceColor, iceDarkColor);
        main.gravityModifier = -0.05f;
        main.maxParticles    = 60;

        var emission = ps.emission;
        emission.rateOverTime = 30f;

        var shape = ps.shape;
        shape.enabled   = true;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius    = 0.5f;

        var col = ps.colorOverLifetime;
        col.enabled = true;
        Gradient g  = new Gradient();
        g.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(iceColor,     0f),
                new GradientColorKey(iceDarkColor, 0.5f),
                new GradientColorKey(glowColor,    1f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(1f,   0f),
                new GradientAlphaKey(0.6f, 0.6f),
                new GradientAlphaKey(0f,   1f)
            }
        );
        col.color = new ParticleSystem.MinMaxGradient(g);

        var ren = ps.GetComponent<ParticleSystemRenderer>();
        ren.renderMode = ParticleSystemRenderMode.Billboard;
        ren.material   = MakeMat(iceColor, 0.9f);

        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        return ps;
    }

    private LineRenderer[] BuildPulseRings()
    {
        LineRenderer[] rings = new LineRenderer[RING_COUNT];
        for (int i = 0; i < RING_COUNT; i++)
        {
            GameObject go = new GameObject("PulseRing_" + i);
            go.transform.SetParent(_freezeRoot.transform);
            go.transform.localPosition = new Vector3(0f, 0.05f, 0f);

            LineRenderer lr      = go.AddComponent<LineRenderer>();
            lr.positionCount     = 33;
            lr.useWorldSpace     = false;
            lr.loop              = true;
            lr.alignment         = LineAlignment.View;
            lr.startWidth        = 0.05f;
            lr.endWidth          = 0.05f;
            lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lr.receiveShadows    = false;
            lr.material          = MakeMat(iceColor, 0.8f);

            SetRingPositions(lr, 0.8f + i * 0.35f);
            rings[i] = lr;
        }
        return rings;
    }

    private void SetRingPositions(LineRenderer lr, float radius)
    {
        int count = lr.positionCount;
        for (int s = 0; s < count; s++)
        {
            float angle = (float)s / (count - 1) * Mathf.PI * 2f;
            lr.SetPosition(s, new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius));
        }
    }

    private LineRenderer[] BuildCrystals()
    {
        LineRenderer[] crystals = new LineRenderer[CRYSTAL_COUNT];
        for (int i = 0; i < CRYSTAL_COUNT; i++)
        {
            GameObject go = new GameObject("Crystal_" + i);
            go.transform.SetParent(_freezeRoot.transform);
            go.transform.localPosition = Vector3.zero;

            LineRenderer lr   = go.AddComponent<LineRenderer>();
            lr.positionCount  = 2;
            lr.useWorldSpace  = false;
            lr.startWidth     = 0.06f;
            lr.endWidth       = 0.015f;
            lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lr.receiveShadows = false;
            lr.material       = MakeMat(iceColor, 1f);

            float angle = (float)i / CRYSTAL_COUNT * Mathf.PI * 2f;
            float h     = Random.Range(0.3f, 1.2f);
            float r     = Random.Range(0.4f, 0.9f);
            Vector3 dir = new Vector3(Mathf.Cos(angle) * r, h, Mathf.Sin(angle) * r);

            lr.SetPosition(0, Vector3.zero);
            lr.SetPosition(1, dir);

            Gradient g = new Gradient();
            g.SetKeys(
                new GradientColorKey[] {
                    new GradientColorKey(iceColor,    0f),
                    new GradientColorKey(glowColor,   0.5f),
                    new GradientColorKey(Color.white, 1f)
                },
                new GradientAlphaKey[] {
                    new GradientAlphaKey(1f,   0f),
                    new GradientAlphaKey(0.9f, 0.4f),
                    new GradientAlphaKey(0f,   1f)
                }
            );
            lr.colorGradient = g;
            crystals[i] = lr;
        }
        return crystals;
    }

    private GameObject BuildFreezeLabel()
    {
        GameObject go = new GameObject("FreezeLabel");
        go.transform.SetParent(_freezeRoot.transform);
        go.transform.localPosition = new Vector3(0f, 2.5f, 0f);

        TextMesh tm      = go.AddComponent<TextMesh>();
        tm.text          = ">> BLOQUE <<\nANTIVIRUS ACTIF";
        tm.fontSize      = 32;
        tm.characterSize = 0.09f;
        tm.anchor        = TextAnchor.MiddleCenter;
        tm.alignment     = TextAlignment.Center;
        tm.color         = Color.white;
        tm.fontStyle     = FontStyle.Bold;

        return go;
    }

    private void UpdateFreezeVFX()
    {
        _pulseTime += Time.deltaTime;

        for (int i = 0; i < RING_COUNT; i++)
        {
            if (_pulseRings[i] == null) continue;
            float phase = _pulseTime * 2.5f + i * (Mathf.PI * 2f / RING_COUNT);
            float pulse = 0.85f + Mathf.Sin(phase) * 0.15f;
            float alpha = 0.4f  + Mathf.Sin(phase) * 0.4f;

            SetRingPositions(_pulseRings[i], (0.8f + i * 0.35f) * pulse);

            Gradient g = new Gradient();
            g.SetKeys(
                new GradientColorKey[] {
                    new GradientColorKey(glowColor, 0f),
                    new GradientColorKey(iceColor,  0.5f),
                    new GradientColorKey(glowColor, 1f)
                },
                new GradientAlphaKey[] {
                    new GradientAlphaKey(alpha, 0f),
                    new GradientAlphaKey(alpha, 0.9f),
                    new GradientAlphaKey(0f,    1f)
                }
            );
            _pulseRings[i].colorGradient = g;
        }

        for (int i = 0; i < CRYSTAL_COUNT; i++)
        {
            if (_crystals[i] == null) continue;
            float phase   = _pulseTime * 3f + i * 1.05f;
            float flicker = 0.7f + Mathf.Sin(phase) * 0.3f;
            Color c = iceColor * flicker; c.a = flicker;
            _crystals[i].startColor = c;
        }

        if (_freezeLabel != null)
        {
            float bobY = Mathf.Sin(_pulseTime * 2f) * 0.08f;
            _freezeLabel.transform.localPosition = new Vector3(0f, 2.5f + bobY, 0f);

            Camera cam = Camera.main;
            if (cam != null)
            {
                Vector3 dir = _freezeLabel.transform.position - cam.transform.position;
                _freezeLabel.transform.rotation = Quaternion.LookRotation(dir);
            }

            TextMesh tm = _freezeLabel.GetComponent<TextMesh>();
            if (tm != null)
            {
                float glow = 0.7f + Mathf.Sin(_pulseTime * 4f) * 0.3f;
                Color tc = Color.Lerp(Color.white, iceColor, 0.4f) * glow; tc.a = 1f; tm.color = tc;
            }
        }
    }

    private IEnumerator FreezeAppear()
    {
        _freezeRoot.SetActive(true);
        if (_psIce != null) _psIce.Play();

        float elapsed = 0f;
        while (elapsed < 0.5f)
        {
            elapsed += Time.deltaTime;
            float t  = elapsed / 0.5f;
            if (_crystals != null)
                foreach (var c in _crystals)
                    if (c != null) c.widthMultiplier = Mathf.Lerp(0f, 1f, t);
            yield return null;
        }
    }

    private IEnumerator FreezeDisappear()
    {
        float elapsed = 0f;
        while (elapsed < 0.5f)
        {
            elapsed += Time.deltaTime;
            float t  = elapsed / 0.5f;
            if (_crystals != null)
                foreach (var c in _crystals)
                    if (c != null) c.widthMultiplier = Mathf.Lerp(1f, 0f, t);
            yield return null;
        }

        if (_psIce != null) _psIce.Stop();
        _freezeRoot.SetActive(false);
    }

    // ── UTILS ──
    private Material MakeMat(Color col, float alpha = 1f)
    {
        Shader sh = Shader.Find("Legacy Shaders/Particles/Additive");
        if (sh == null) sh = Shader.Find("Unlit/Color");
        Material m = new Material(sh);
        Color c    = col; c.a = alpha; m.color = c;
        return m;
    }

    float DistXZ(Vector3 a, Vector3 b)
    {
        float dx = a.x - b.x; float dz = a.z - b.z;
        return Mathf.Sqrt(dx * dx + dz * dz);
    }

    void FaceTarget()
    {
        if (!target) return;
        Vector3 d = target.transform.position - transform.position; d.y = 0f;
        if (d.sqrMagnitude > 0.001f) transform.rotation = Quaternion.LookRotation(d);
    }

    FileTarget GetClosest()
    {
        FileTarget best = null; float minD = Mathf.Infinity;
        foreach (var ft in allFiles)
        {
            if (ft == null || ft.isInfected) continue;
            float d = DistXZ(transform.position, ft.transform.position);
            if (d < detectionRadius && d < minD) { minD = d; best = ft; }
        }
        return best;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow; Gizmos.DrawWireSphere(transform.position, detectionRadius);
        Gizmos.color = Color.red;    Gizmos.DrawWireSphere(transform.position, attackRange);
        if (target) { Gizmos.color = Color.green; Gizmos.DrawLine(transform.position, target.transform.position); }
    }
}