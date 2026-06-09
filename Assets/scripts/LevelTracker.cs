using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// ✅ Sauvegarde level = scène actuelle au DÉMARRAGE (même si perdu/quitté).
/// Sauvegarde level = scène + 1 quand le niveau est COMPLÉTÉ.
///
/// Exemple :
///   Scène 1 démarre  → level = 1 sauvegardé → ProgressPanel garanti
///   Scène 1 terminée → level = 2 sauvegardé → Continue charge scène 2
/// </summary>
public class LevelTracker : MonoBehaviour
{
    private int _currentScene;

    private void Start()
    {
        _currentScene = SceneManager.GetActiveScene().buildIndex;

        // ✅ FIX PRINCIPAL : sauvegarder dès le démarrage de la scène
        // → même si le joueur perd/quitte, le level est enregistré
        // → MenuManager verra level >= 1 → ProgressPanel (jamais FirstTimePanel)
        if (RealtimeDBManager.Instance != null)
        {
            RealtimeDBManager.Instance.UpdateLevel(_currentScene);
            Debug.Log($"🎮 Scène {_currentScene} démarrée → level {_currentScene} sauvegardé");
        }
        else
        {
            Debug.LogWarning("⚠️ RealtimeDBManager introuvable — level non sauvegardé au démarrage");
        }
    }

    /// <summary>
    /// ✅ Appeler quand le joueur TERMINE ce niveau.
    /// Sauvegarde le prochain level (ex: finir scène 1 → level = 2).
    /// </summary>
    public void SaveLevelCompleted()
    {
        int nextLevel = _currentScene + 1;

        if (RealtimeDBManager.Instance != null)
        {
            RealtimeDBManager.Instance.UpdateLevel(nextLevel);
            Debug.Log($"🏆 Niveau {_currentScene} complété → level {nextLevel} sauvegardé");
        }
        else
        {
            Debug.LogWarning("⚠️ RealtimeDBManager introuvable — level non sauvegardé");
        }
    }

    /// <summary>
    /// Alternative pour un système de niveaux non-linéaire.
    /// </summary>
    public void SaveSpecificLevel(int level)
    {
        if (RealtimeDBManager.Instance != null)
        {
            RealtimeDBManager.Instance.UpdateLevel(level);
            Debug.Log($"💾 Level spécifique sauvegardé : {level}");
        }
        else
        {
            Debug.LogWarning("⚠️ RealtimeDBManager introuvable — level non sauvegardé");
        }
    }
}