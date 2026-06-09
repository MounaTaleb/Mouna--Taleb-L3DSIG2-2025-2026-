using UnityEngine;

public class Minimap : MonoBehaviour
{
    [Header("Assigné auto si vide")]
    public Transform player;

    [Header("Noms des personnages (fallback)")]
    public string femaleArmatureName = "PlayerArmature_Female";
    public string maleArmatureName   = "PlayerArmature_Male";
    public string customArmatureName = "characterx";

    private void Start()
    {
        if (player != null) return;
        FindActivePlayer();
    }

    private void FindActivePlayer()
    {
        // ✅ Priorité 1 : CharacterSpawner connaît toujours le personnage actif
        CharacterSpawner spawner = FindObjectOfType<CharacterSpawner>();
        if (spawner != null && spawner.ActiveCharacter != null)
        {
            player = spawner.ActiveCharacter.transform;
            Debug.Log("Minimap → suivi via CharacterSpawner : " + player.name);
            return;
        }

        // ✅ Priorité 2 : cherche par tag "Player"
        GameObject byTag = GameObject.FindWithTag("Player");
        if (byTag != null)
        {
            player = byTag.transform;
            Debug.Log("Minimap → suivi via Tag Player : " + player.name);
            return;
        }

        // ✅ Priorité 3 : cherche par nom (custom, female, male)
        GameObject found = GameObject.Find(customArmatureName)
                        ?? GameObject.Find(femaleArmatureName)
                        ?? GameObject.Find(maleArmatureName);

        if (found != null)
        {
            player = found.transform;
            Debug.Log("Minimap → suivi via nom : " + found.name);
            return;
        }

        Debug.LogError("Minimap : aucun personnage trouvé !");
    }

    private void LateUpdate()
    {
        // Si player perdu en cours de jeu → on retente
        if (player == null)
        {
            FindActivePlayer();
            return;
        }

        Vector3 newPosition = player.position;
        newPosition.y       = transform.position.y;
        transform.position  = newPosition;

        transform.rotation = Quaternion.Euler(90f, player.eulerAngles.y, 0f);
    }
}