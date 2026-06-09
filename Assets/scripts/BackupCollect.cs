using UnityEngine;

public class BackupCollect : MonoBehaviour
{
    private bool      isCollected;
    private float     timer;
    private Collider  myCollider;
    private Renderer  myRenderer;
    private float     respawnTime = 5f;

    public AudioClip  collectSound;
    private AudioSource audioSource;

    public int backupValue = 1;

    // ✅ FIX : assigne 0, 1 ou 2 dans l'Inspector sur chaque prefab
    // 0 = Données  |  1 = Système  |  2 = Cloud
    [Header("Type de backup (0 = Données, 1 = Système, 2 = Cloud)")]
    public int backupType = 0;

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
                timer              = 0f;
            }
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (!isCollected && other.CompareTag("Player"))
        {
            // Appel GameManager si utilisé ailleurs dans le projet
            if (GameManager.Instance != null)
                GameManager.Instance.AddBackup(backupValue);

            // ✅ FIX : notifie le QuestManager → Q2 progresse
            if (QuestManager2_Round2.Instance != null)
                QuestManager2_Round2.Instance.OnBackupCollected(backupType, backupValue);
            else
                Debug.LogWarning("[BackupCollect] QuestManager2_Round2.Instance est NULL !");

            isCollected = true;

            if (collectSound != null)
                audioSource.PlayOneShot(collectSound);
        }
    }
}