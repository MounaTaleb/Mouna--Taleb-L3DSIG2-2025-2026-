using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.Android;

public class ARPermissionManager : MonoBehaviour
{
    [SerializeField] private ARSession arSession;

    void Start()
    {
        // Demander la permission caméra avant de démarrer l'AR
#if UNITY_ANDROID
        if (!Permission.HasUserAuthorizedPermission(Permission.Camera))
        {
            Permission.RequestUserPermission(Permission.Camera);
            arSession.enabled = false; // Désactiver AR en attendant
        }
        else
        {
            arSession.enabled = true;
        }
#endif
    }

    // Appelé quand l'utilisateur répond à la permission
    void OnApplicationFocus(bool hasFocus)
    {
        if (hasFocus && Permission.HasUserAuthorizedPermission(Permission.Camera))
        {
            arSession.enabled = true;
        }
    }
}