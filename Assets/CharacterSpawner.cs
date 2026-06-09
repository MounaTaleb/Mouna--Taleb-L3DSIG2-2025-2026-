using UnityEngine;
using UnityEngine.InputSystem;
using Cinemachine;
using StarterAssets; // ← pour UICanvasControllerInput

public class CharacterSpawner : MonoBehaviour
{
    [Header("Personnages (désactivés dans la scène)")]
    public GameObject femaleArmature;
    public GameObject maleArmature;

    [Header("Caméra")]
    public CinemachineVirtualCamera playerFollowCamera;

    [Header("UI Canvas")]
    public GameObject uiCanvas; // ← drag UI_Canvas_StarterAssetsInputs_Joysticks

    private const string CHARACTER_KEY = "SelectedCharacter";

    private void Awake()
    {
        string selected = PlayerPrefs.GetString(CHARACTER_KEY, "male");
        GameObject activeCharacter;

        if (selected == "female")
        {
            femaleArmature.SetActive(true);
            maleArmature.SetActive(false);
            activeCharacter = femaleArmature;
            Debug.Log("✅ Female activée");
        }
        else
        {
            maleArmature.SetActive(true);
            femaleArmature.SetActive(false);
            activeCharacter = maleArmature;
            Debug.Log("✅ Male activé");
        }

        LinkCamera(activeCharacter);
        LinkUICanvas(activeCharacter); // ← Fix le problème input
    }

    private void LinkCamera(GameObject character)
    {
        if (playerFollowCamera == null) return;

        Transform cameraRoot = character.transform.Find("PlayerCameraRoot");
        Transform target = cameraRoot != null ? cameraRoot : character.transform;

        playerFollowCamera.Follow = target;
        playerFollowCamera.LookAt = target;
        Debug.Log("🎥 Caméra liée à : " + target.name);
    }

    private void LinkUICanvas(GameObject character)
    {
        if (uiCanvas == null) return;

        // ── 1. UICanvasControllerInput ──────────────────────────────
        var canvasInput = uiCanvas.GetComponent<UICanvasControllerInput>();
        if (canvasInput != null)
        {
            var starterInput = character.GetComponent<StarterAssetsInputs>();
            if (starterInput != null)
            {
                canvasInput.starterAssetsInputs = starterInput;
                Debug.Log("🎮 UICanvasControllerInput → " + character.name);
            }
        }

        // ── 2. MobileDisableAutoSwitchControls ──────────────────────
        var mobileSwitch = uiCanvas.GetComponent<MobileDisableAutoSwitchControls>();
        if (mobileSwitch != null)
        {
            var playerInput = character.GetComponent<PlayerInput>();
            if (playerInput != null)
            {
                mobileSwitch.playerInput = playerInput;
                Debug.Log("📱 MobileDisableAutoSwitchControls → " + character.name);
            }
        }
    }
}