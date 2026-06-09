using System.Collections;
using UnityEngine;

public class FileTarget : MonoBehaviour
{
    [Header("Coups necessaires pour infecter")]
    public int hitsRequired = 3;

    [Header("Prefab de remplacement (optionnel)")]
    public GameObject infectedPrefab;
    public Vector3    spawnOffset = Vector3.zero;

    [Header("Effet Glitch (a chaque coup)")]
    public float glitchDuration  = 0.4f;
    public float glitchIntensity = 0.12f;
    public float glitchSpeed     = 0.04f;

    [Header("Lumiere rouge")]
    public float lightIntensity = 4f;
    public float lightRange     = 4f;

    [Header("Texte binaire flottant")]
    public float textRiseSpeed  = 1.5f;
    public float textLifetime   = 1.0f;
    public float textSize       = 0.2f;

    [Header("Degats sur le systeme (a l'infection finale)")]
    public float damageOnInfected = 3f;

    [HideInInspector] public bool isInfected  = false;
    [HideInInspector] public int  currentHits = 0;

    private Renderer[] rends;
    private Vector3    originPos;
    private Light      redLight;
    private bool       isGlitching = false;

    private string[] binaryTexts = {
        "01001000", "10110101", "01010101",
        "11001010", "00101101", "010101",
        "1010", "0110101", "10101010"
    };

    void Awake()
    {
        rends     = GetComponentsInChildren<Renderer>(true);
        originPos = transform.position;

        if (rends.Length == 0)
            Debug.LogWarning($"[FileTarget] '{name}' : aucun Renderer trouve !");

        ApplyColor(Color.cyan);

        redLight           = gameObject.AddComponent<Light>();
        redLight.type      = LightType.Point;
        redLight.color     = Color.red;
        redLight.intensity = 0f;
        redLight.range     = lightRange;
    }

    public bool ReceiveHit()
    {
        if (isInfected) return true;

        currentHits++;
        Debug.Log($"[FileTarget] '{name}' : {currentHits}/{hitsRequired} coup(s)");

        // 🟢 LIGNE AJOUTÉE : Déclenche le texte rouge au premier coup reçu !
        if (currentHits == 1 && AlertManager.Instance != null)
        {
            AlertManager.Instance.ShowAlert();
        }

        float t = (float)currentHits / hitsRequired;
        ApplyColor(Color.Lerp(Color.cyan, Color.red, t));

        if (!isGlitching)
            StartCoroutine(GlitchEffect());

        SpawnBinaryText();

        if (currentHits >= hitsRequired)
        {
            StartCoroutine(InfectSequence());
            return true;
        }
        return false;
    }

    IEnumerator GlitchEffect()
    {
        isGlitching = true;

        float elapsed = 0f;
        while (elapsed < glitchDuration)
        {
            transform.position = originPos + new Vector3(
                Random.Range(-glitchIntensity, glitchIntensity),
                Random.Range(-glitchIntensity, glitchIntensity),
                Random.Range(-glitchIntensity, glitchIntensity)
            );

            ApplyColor(Random.value > 0.5f ? Color.red : Color.white);
            redLight.intensity = Random.Range(lightIntensity * 0.5f, lightIntensity * 1.5f);

            elapsed += glitchSpeed;
            yield return new WaitForSeconds(glitchSpeed);
        }

        transform.position = originPos;
        redLight.intensity = 0f;

        float t = (float)currentHits / hitsRequired;
        ApplyColor(Color.Lerp(Color.cyan, Color.red, t));

        isGlitching = false;
    }

    void SpawnBinaryText()
    {
        GameObject textGO = new GameObject("BinaryText");

        textGO.transform.position = transform.position + new Vector3(
            Random.Range(-0.4f, 0.4f),
            Random.Range( 0.3f, 0.7f),
            Random.Range(-0.4f, 0.4f)
        );

        if (Camera.main != null)
            textGO.transform.rotation = Camera.main.transform.rotation;

        TextMesh tm        = textGO.AddComponent<TextMesh>();
        tm.text            = binaryTexts[Random.Range(0, binaryTexts.Length)];
        tm.fontSize        = 24;
        tm.characterSize   = textSize;
        tm.color           = Color.red;
        tm.anchor          = TextAnchor.MiddleCenter;
        tm.alignment       = TextAlignment.Center;
        tm.fontStyle       = FontStyle.Bold;

        StartCoroutine(FloatAndFade(textGO, tm));
    }

    IEnumerator FloatAndFade(GameObject textGO, TextMesh tm)
    {
        float elapsed = 0f;
        Color baseColor = Color.red;

        while (elapsed < textLifetime)
        {
            if (textGO == null) yield break;

            textGO.transform.position += Vector3.up * textRiseSpeed * Time.deltaTime;

            if (Camera.main != null)
                textGO.transform.rotation = Camera.main.transform.rotation;

            float alpha = Mathf.Lerp(1f, 0f, elapsed / textLifetime);
            tm.color = new Color(baseColor.r, baseColor.g, baseColor.b, alpha);

            elapsed += Time.deltaTime;
            yield return null;
        }

        Destroy(textGO);
    }

    IEnumerator InfectSequence()
    {
        isInfected = true;

        yield return new WaitUntil(() => !isGlitching);

        float elapsed = 0f;
        float finalDuration = 0.6f;
        while (elapsed < finalDuration)
        {
            transform.position = originPos + new Vector3(
                Random.Range(-glitchIntensity * 2f, glitchIntensity * 2f),
                Random.Range(-glitchIntensity * 2f, glitchIntensity * 2f),
                Random.Range(-glitchIntensity * 2f, glitchIntensity * 2f)
            );

            ApplyColor(Random.value > 0.5f ? Color.red : Color.black);
            redLight.intensity = Random.Range(lightIntensity, lightIntensity * 2f);

            if (Random.value > 0.6f) SpawnBinaryText();

            elapsed += glitchSpeed;
            yield return new WaitForSeconds(glitchSpeed);
        }

        transform.position = originPos;
        redLight.intensity = 0f;

        if (SystemHealth.Instance != null)
            SystemHealth.Instance.TakeDamage(damageOnInfected);
        else
            Debug.LogWarning("[FileTarget] SystemHealth.Instance est null !");

        if (infectedPrefab != null)
            Instantiate(infectedPrefab,
                        transform.position + spawnOffset,
                        transform.rotation);
        else
            Debug.LogWarning($"[FileTarget] '{name}' : aucun Infected Prefab assigne !");

        Destroy(gameObject);
    }

    void ApplyColor(Color c)
    {
        foreach (Renderer r in rends)
        {
            Material[] mats = r.materials;
            foreach (Material m in mats)
                m.color = c;
            r.materials = mats;
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(transform.position, Vector3.one * 0.6f);
    }
}