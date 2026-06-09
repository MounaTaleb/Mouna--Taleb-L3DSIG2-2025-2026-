using System.Collections;
using UnityEngine;

public class DoorController : MonoBehaviour
{
    private Animator animator;
    private bool isOpen = false;

    [Header("Joueur (assigné auto si vide)")]
    public Transform player;
    public string femaleArmatureName = "PlayerArmature_Female";
    public string maleArmatureName   = "PlayerArmature_Male";

    [Header("Paramètres")]
    public float distance      = 3f;
    public float autoCloseDelay = 3f;

    [Header("Audio")]
    public AudioClip openSound;
    public AudioClip closeSound;
    private AudioSource audioSource;

    private Coroutine autoCloseCoroutine;

    void Start()
    {
        animator    = GetComponentInChildren<Animator>();
        audioSource = GetComponent<AudioSource>();

        // ── Trouve le personnage actif automatiquement ──
        if (player == null)
            FindActivePlayer();
    }

    private void FindActivePlayer()
    {
        string selected  = PlayerPrefs.GetString("SelectedCharacter", "male");
        string targetName = selected == "female" ? femaleArmatureName : maleArmatureName;

        GameObject found = GameObject.Find(targetName);

        if (found != null)
        {
            player = found.transform;
            Debug.Log("🚪 DoorController → joueur : " + found.name);
        }
        else
        {
            // Fallback : cherche n'importe lequel des deux
            GameObject fallback = GameObject.Find(femaleArmatureName)
                               ?? GameObject.Find(maleArmatureName);
            if (fallback != null)
            {
                player = fallback.transform;
                Debug.LogWarning("🚪 DoorController fallback → " + fallback.name);
            }
            else
            {
                Debug.LogError("❌ DoorController : aucun personnage trouvé !");
            }
        }
    }

    void Update()
    {
        // Sécurité si player non trouvé
        if (player == null) return;

        float dist = Vector3.Distance(player.position, transform.position);

        if (dist <= distance && !isOpen)
            OpenDoor();

        if (isOpen && dist > distance)
        {
            if (autoCloseCoroutine == null)
                autoCloseCoroutine = StartCoroutine(AutoCloseDoor());
        }
        else if (isOpen && dist <= distance)
        {
            if (autoCloseCoroutine != null)
            {
                StopCoroutine(autoCloseCoroutine);
                autoCloseCoroutine = null;
            }
        }
    }

    void OpenDoor()
    {
        isOpen = true;
        animator.SetBool("isOpen", true);
        if (openSound != null) audioSource.PlayOneShot(openSound);
    }

    IEnumerator AutoCloseDoor()
    {
        yield return new WaitForSeconds(autoCloseDelay);
        CloseDoor();
        autoCloseCoroutine = null;
    }

    void CloseDoor()
    {
        isOpen = false;
        animator.SetBool("isOpen", false);
        if (closeSound != null) audioSource.PlayOneShot(closeSound);
    }
}