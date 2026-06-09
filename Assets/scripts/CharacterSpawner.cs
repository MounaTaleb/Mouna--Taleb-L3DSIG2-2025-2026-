using UnityEngine;
using UnityEngine.InputSystem;
using Cinemachine;
using StarterAssets;
using System.Collections;
using GanzSe;

public class CharacterSpawner : MonoBehaviour
{
    [Header("Personnages (désactivés dans la scène)")]
    public GameObject femaleArmature;
    public GameObject maleArmature;
    public GameObject customArmature;

    [Header("Caméra")]
    public CinemachineVirtualCamera playerFollowCamera;

    [Header("UI Canvas")]
    public GameObject uiCanvas;

    public GameObject ActiveCharacter      { get; private set; }
    public Vector3    InitialPosition      { get; private set; }
    public Quaternion InitialRotation      { get; private set; }
    public bool       InitialPositionReady { get; private set; } = false;

    private const string CHARACTER_KEY = "SelectedCharacter";

    private void Awake()
    {
        femaleArmature.SetActive(false);
        maleArmature.SetActive(false);
        if (customArmature != null) customArmature.SetActive(false);

        // ✅ FIX : valeur par défaut "none" au lieu de "male"
        // pour éviter que hasExplicitGender soit true quand aucun genre n'est choisi
        string selected = PlayerPrefs.GetString(CHARACTER_KEY, "none");
        SpawnCharacter(selected);
        StartCoroutine(SyncWithFirebaseInBackground());
    }

    // ─────────────────────────────────────────────────────
    //  Sync Firebase en arrière-plan
    // ─────────────────────────────────────────────────────
    private IEnumerator SyncWithFirebaseInBackground()
    {
        float timeout = 10f, elapsed = 0f;
        while (elapsed < timeout)
        {
            if (RealtimeDBManager.Instance != null
             && RealtimeDBManager.Instance.IsInitialized
             && RealtimeDBManager.Instance.HasSession)
                break;
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (RealtimeDBManager.Instance == null || !RealtimeDBManager.Instance.HasSession)
        {
            Debug.LogWarning("Firebase non disponible — PlayerPrefs utilisé.");
            yield break;
        }

        RealtimeDBManager.Instance.LoadUserData(userData =>
        {
            if (userData == null || string.IsNullOrEmpty(userData.profile.character)) return;

            string firebaseChar = userData.profile.character;
            string cachedChar   = PlayerPrefs.GetString(CHARACTER_KEY, "none");

            if (firebaseChar != cachedChar)
            {
                Debug.Log($"Firebase override : {cachedChar} → {firebaseChar}");
                PlayerPrefs.SetString(CHARACTER_KEY, firebaseChar);

                // ✅ Si Firebase renvoie male/female, on supprime la customisation
                // pour éviter que le custom character override le genre Firebase
                if (firebaseChar == "female" || firebaseChar == "male")
                {
                    PlayerPrefs.DeleteKey(CustomizationData.PREFS_KEY);
                    PlayerPrefs.Save();
                }

                SpawnCharacter(firebaseChar);
            }
        });
    }

    // ─────────────────────────────────────────────────────
    //  Spawn
    // ─────────────────────────────────────────────────────
    private void SpawnCharacter(string selected)
    {
        bool hasCustomization = PlayerPrefs.HasKey(CustomizationData.PREFS_KEY);

        // ✅ FIX : hasExplicitGender est false si selected vaut "none" ou toute
        // valeur autre que "male"/"female" — le custom character peut alors s'afficher
        bool hasExplicitGender = selected == "female" || selected == "male";

        if (hasCustomization && customArmature != null && !hasExplicitGender)
        {
            femaleArmature.SetActive(false);
            maleArmature.SetActive(false);
            customArmature.SetActive(true);
            ActiveCharacter = customArmature;

            StartCoroutine(ApplyCustomizationNextFrame(customArmature));
            Debug.Log("Spawned : Custom (Modular Hero)");
        }
        else
        {
            // ✅ FIX : si selected n'est ni "female" ni un genre valide,
            // on affiche le mâle par défaut uniquement dans ce bloc
            bool isFemale = selected == "female";
            femaleArmature.SetActive(isFemale);
            maleArmature.SetActive(!isFemale);
            if (customArmature != null) customArmature.SetActive(false);
            ActiveCharacter = isFemale ? femaleArmature : maleArmature;
            Debug.Log($"Spawned : {selected}");
        }

        if (!InitialPositionReady)
        {
            InitialPosition      = ActiveCharacter.transform.position;
            InitialRotation      = ActiveCharacter.transform.rotation;
            InitialPositionReady = true;
        }

        LinkCamera(ActiveCharacter);
        LinkCastingEffect(ActiveCharacter);
        LinkUICanvas(ActiveCharacter);
    }

    // ─────────────────────────────────────────────────────
    //  Applique les vêtements après une frame
    // ─────────────────────────────────────────────────────
    private IEnumerator ApplyCustomizationNextFrame(GameObject character)
    {
        yield return null;

        string json            = PlayerPrefs.GetString(CustomizationData.PREFS_KEY);
        CustomizationData data = JsonUtility.FromJson<CustomizationData>(json);

        var hero = character.GetComponent<ModularHeroController>()
                ?? character.GetComponentInChildren<ModularHeroController>();

        if (hero == null)
        {
            Debug.LogWarning("ModularHeroController introuvable sur customArmature !");
            yield break;
        }

        hero.SelectPart("HAIRS",      data.hair);
        hero.SelectPart("FACE HAIRS", data.facehair);
        hero.SelectPart("EYES",       data.eyes);
        hero.SelectPart("CHESTS",     data.chests);
        hero.SelectPart("LEGS",       data.legs);
        hero.SelectPart("FEET",       data.feet);

        Debug.Log($"Customisation appliquée : {json}");
    }

    // ─────────────────────────────────────────────────────
    //  Lock position
    // ─────────────────────────────────────────────────────
    public void LockInitialPosition()
    {
        if (ActiveCharacter != null && !InitialPositionReady)
        {
            InitialPosition      = ActiveCharacter.transform.position;
            InitialRotation      = ActiveCharacter.transform.rotation;
            InitialPositionReady = true;
            Debug.Log($"[CharacterSpawner] Position verrouillée : {InitialPosition}");
        }
        else
        {
            Debug.Log("[CharacterSpawner] Position déjà verrouillée.");
        }
    }

    // ─────────────────────────────────────────────────────
    //  Links
    // ─────────────────────────────────────────────────────
    private void LinkCamera(GameObject character)
    {
        if (playerFollowCamera == null) return;

        Transform cameraRoot = FindDeepChild(character.transform, "PlayerCameraRoot");
        Transform target     = cameraRoot != null ? cameraRoot : character.transform;

        playerFollowCamera.Follow = target;
        playerFollowCamera.LookAt = target;

        Debug.Log(cameraRoot != null
            ? $"Caméra liée à : {cameraRoot.name}"
            : "PlayerCameraRoot introuvable — caméra liée à la racine du personnage");
    }

    private Transform FindDeepChild(Transform parent, string childName)
    {
        foreach (Transform child in parent.GetComponentsInChildren<Transform>(true))
        {
            if (child.name == childName)
                return child;
        }
        return null;
    }

    private void LinkCastingEffect(GameObject character)
    {
        var tpc = character.GetComponent<ThirdPersonController>();
        if (tpc == null) return;
        var effect = character.GetComponentInChildren<CastingEffect>(includeInactive: false)
                  ?? character.GetComponent<CastingEffect>();
        if (effect != null) tpc.castingEffect = effect;
    }

    private void LinkUICanvas(GameObject character)
    {
        if (uiCanvas == null) return;
        var canvasInput = uiCanvas.GetComponent<UICanvasControllerInput>();
        if (canvasInput != null)
        {
            var starterInput = character.GetComponent<StarterAssetsInputs>();
            if (starterInput != null) canvasInput.starterAssetsInputs = starterInput;
            var tpc = character.GetComponent<ThirdPersonController>();
            if (tpc != null) canvasInput.thirdPersonController = tpc;
        }
        var mobileSwitch = uiCanvas.GetComponent<MobileDisableAutoSwitchControls>();
        if (mobileSwitch != null)
        {
            var playerInput = character.GetComponent<PlayerInput>();
            if (playerInput != null) mobileSwitch.playerInput = playerInput;
        }
    }
}