using System.Collections;
using UnityEngine;

public class respawn : MonoBehaviour
{
    private bool isCollected;
    private float timer;
    private Collider myCollider;
    private Renderer myRenderer;
    private float respawnTime = 5f;

    public AudioClip collectSound;
    private AudioSource audioSource;
    public int coinValue = 1;

    void Start()
    {
        isCollected = false;
        timer = 0f;
        myCollider = GetComponent<Collider>();
        myRenderer = GetComponent<Renderer>();

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();
    }

    void Update()
    {
        if (isCollected)
        {
            timer += Time.deltaTime;

            if (timer <= respawnTime)
            {
                myCollider.enabled = false;
                myRenderer.enabled = false;
            }

            if (timer > respawnTime)
            {
                isCollected = false;
                myCollider.enabled = true;
                myRenderer.enabled = true;
                timer = 0;
            }
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (!isCollected && other.CompareTag("Player"))
        {
            // ✅ Vérification Instance avant appel
            if (GameManager.Instance != null)
                GameManager.Instance.AddCoin(coinValue);
            else
                Debug.LogError("GameManager.Instance est NULL ! Vérifie la scène.");

            isCollected = true;

            if (collectSound != null)
                audioSource.PlayOneShot(collectSound);

            if (FloatingTextSpawner.Instance != null)
                FloatingTextSpawner.Instance.ShowScorePopup("+" + coinValue, Color.yellow);
            else
                Debug.LogWarning("FloatingTextSpawner.Instance est NULL !");
        }
    }
}