using UnityEngine;

public class AntivirusCollect : MonoBehaviour
{
    private bool      isCollected;
    private float     timer;
    private Collider  myCollider;
    private Renderer  myRenderer;
    private float     respawnTime = 5f;

    public AudioClip  collectSound;
    private AudioSource audioSource;
    public int        antiValue = 1;

    void Start()
    {
        isCollected = false;
        timer       = 0f;
        myCollider  = GetComponent<Collider>();
        myRenderer  = GetComponent<Renderer>();

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();
    }

    void Update()
    {
        if (isCollected)
        {
            timer += Time.deltaTime;
            myCollider.enabled = false;
            myRenderer.enabled = false;

            if (timer > respawnTime)
            {
                isCollected        = false;
                myCollider.enabled = true;
                myRenderer.enabled = true;
                timer              = 0;
            }
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (!isCollected && other.CompareTag("Player"))
        {
            // ── GameManager ──
            if (GameManager.Instance != null)
                GameManager.Instance.AddAntivirus(antiValue);
            else
                Debug.LogError("GameManager.Instance est NULL !");

            // ── QuestManager — Quest 1 ──
            if (QuestManager.Instance != null)
                QuestManager.Instance.OnAntivirusCollected(antiValue);

            isCollected = true;

            if (collectSound != null)
                audioSource.PlayOneShot(collectSound);
        }
    }
}