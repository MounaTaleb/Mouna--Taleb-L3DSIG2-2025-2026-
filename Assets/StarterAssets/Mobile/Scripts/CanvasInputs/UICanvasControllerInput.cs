using UnityEngine;
using StarterAssets;

public class UICanvasControllerInput : MonoBehaviour
{
    [Header("Output")]
    public StarterAssetsInputs starterAssetsInputs;

    // ✅ FIX : référence au TPC du personnage actif
    [HideInInspector] // géré par CharacterSpawner, pas l'Inspector
    public ThirdPersonController thirdPersonController;

    public void VirtualMoveInput(Vector2 virtualMoveDirection)
    {
        starterAssetsInputs.MoveInput(virtualMoveDirection);
    }

    public void VirtualLookInput(Vector2 virtualLookDirection)
    {
        starterAssetsInputs.LookInput(virtualLookDirection);
    }

    public void VirtualJumpInput(bool virtualJumpState)
    {
        starterAssetsInputs.JumpInput(virtualJumpState);
    }

    public void VirtualSprintInput(bool virtualSprintState)
    {
        starterAssetsInputs.SprintInput(virtualSprintState);
    }

    // ✅ FIX : passe par thirdPersonController (toujours le personnage actif)
    public void VirtualCastingInput(bool state)
    {
        if (thirdPersonController == null)
        {
            Debug.LogWarning("[UICanvas] thirdPersonController non assigné !");
            return;
        }

        if (state) thirdPersonController.StartCasting();
        else        thirdPersonController.StopCasting();
    }
}