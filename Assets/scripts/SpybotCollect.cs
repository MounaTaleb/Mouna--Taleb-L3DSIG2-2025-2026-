using UnityEngine;

public class SpybotCollect : MonoBehaviour
{
    private bool        isCollected;
    private float       timer;
    private Collider    myCollider;
    private Renderer    myRenderer;
    private float       respawnTime = 5f;

    public AudioClip    collectSound;
    private AudioSource audioSource;
    public int          spybotValue = 1;

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
            // 1️⃣ GameManager EN PREMIER → spybot incrémenté avant QuestManager2
            if (GameManager.Instance != null)
                GameManager.Instance.AddSpybot(spybotValue);
            else
                Debug.LogError("[SpybotCollect] GameManager.Instance est NULL !");

            // 2️⃣ QuestManager2 EN SECOND → lit GameManager.spybot déjà à jour
            if (QuestManager2.Instance != null)
                QuestManager2.Instance.OnSpybotCollected(spybotValue);
            else
                Debug.LogWarning("[SpybotCollect] QuestManager2.Instance est NULL !");

            isCollected = true;

            if (collectSound != null)
                audioSource.PlayOneShot(collectSound);
        }
    }
}