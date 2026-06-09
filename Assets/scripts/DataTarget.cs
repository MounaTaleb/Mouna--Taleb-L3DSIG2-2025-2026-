using System.Collections;
using UnityEngine;

public class DataTarget : MonoBehaviour
{
    public enum DataType { Image, PasswordFile, Location, BankCard }

    [Header("Configuration")]
    public DataType dataType = DataType.Image;
    public GameObject compromisedPrefab;

    [Header("Effets Audio (FORCÉ)")]
    public AudioClip stealSound;
    [Range(0f, 2f)] public float volumeBoost = 1.2f; // Tu peux monter au-dessus de 1.0 ici

    [Header("Effets Visuels")]
    public ParticleSystem stealEffect;
    public float shrinkDuration = 0.2f;

    public bool isCompromised { get; private set; } = false;

    public void StealData()
    {
        if (isCompromised) return;
        StartCoroutine(TransformationSequence());
    }

    private IEnumerator TransformationSequence()
    {
        isCompromised = true;

        // --- 1. LOGIQUE ---
        if (SystemHealth.Instance != null) SystemHealth.Instance.TakeDamage(5f);
        if (CyberAlertSystem.Instance != null) CyberAlertSystem.Instance.TriggerAttackAlert(dataType);

        // --- 2. AUDIO SANS ÉCHEC ---
        if (stealSound != null)
        {
            // On crée un objet AudioSource permanent pour la durée du son
            GameObject g = new GameObject("ForcedAudio");
            AudioSource source = g.AddComponent<AudioSource>();
            
            source.clip = stealSound;
            source.volume = volumeBoost;
            source.spatialBlend = 0f; // Force 2D (joue dans les deux oreilles à fond)
            source.priority = 0;      // Priorité maximale pour ne pas être coupé
            source.playOnAwake = false;
            
            source.Play();
            Destroy(g, stealSound.length + 0.1f);
        }

        // --- 3. VISUEL ---
        if (stealEffect != null)
        {
            ParticleSystem ps = Instantiate(stealEffect, transform.position, Quaternion.identity);
            Destroy(ps.gameObject, 1.5f);
        }

        // --- 4. ANIMATION & SPAWN ---
        float elapsed = 0f;
        Vector3 initialScale = transform.localScale;
        while (elapsed < shrinkDuration)
        {
            elapsed += Time.deltaTime;
            transform.localScale = Vector3.Lerp(initialScale, Vector3.zero, elapsed / shrinkDuration);
            yield return null;
        }

        if (compromisedPrefab != null)
            Instantiate(compromisedPrefab, transform.position, transform.rotation);

        Destroy(gameObject);
    }
}