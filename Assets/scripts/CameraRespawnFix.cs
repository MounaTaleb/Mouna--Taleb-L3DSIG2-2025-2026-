using UnityEngine;
using Cinemachine;

public class CameraRespawnFix : MonoBehaviour
{
    [Header("References")]
    public CinemachineVirtualCamera playerFollowCamera;
    // OU si tu as un FreeLook :
    // public CinemachineFreeLook playerFollowCamera;

    private CinemachineBrain brain;

    void Awake()
    {
        brain = Camera.main.GetComponent<CinemachineBrain>();
    }

    // ✅ Appelle cette méthode JUSTE APRÈS avoir téléporté/respawn le joueur
    public void SnapCameraToPlayer()
    {
        StartCoroutine(ForceSnapRoutine());
    }

    private System.Collections.IEnumerator ForceSnapRoutine()
    {
        // 1️⃣ Désactiver le blend le temps du snap
        float originalBlend = brain.m_DefaultBlend.m_Time;
        brain.m_DefaultBlend.m_Time = 0f;

        // 2️⃣ Forcer Cinemachine à ignorer le delta time (snap instantané)
        brain.m_IgnoreTimeScale = true;

        // 3️⃣ Désactiver/réactiver la virtual cam pour forcer le reset
        playerFollowCamera.gameObject.SetActive(false);
        yield return null; // attendre 1 frame
        playerFollowCamera.gameObject.SetActive(true);

        yield return null; // laisser Cinemachine recalculer

        // 4️⃣ Remettre les paramètres normaux
        brain.m_DefaultBlend.m_Time = originalBlend;
        brain.m_IgnoreTimeScale = false;
    }
}