using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace GanzSe
{
    public class ModularHeroController : MonoBehaviour
    {
        [Header("Armor Parts Root (Vetements)")]
        public Transform armorPartsRoot;

        [Header("Face Parts Root (Visage)")]
        public Transform facePartsRoot;

        // ✅ Activer un article précis par catégorie + index
        public void SelectPart(string categoryName, int index)
        {
            Transform category = FindCategory(categoryName);
            if (category == null)
            {
                Debug.LogWarning($"Catégorie '{categoryName}' introuvable !");
                return;
            }

            foreach (Transform child in category)
                child.gameObject.SetActive(false);

            if (index >= 0 && index < category.childCount)
                category.GetChild(index).gameObject.SetActive(true);
        }

        // ✅ Cherche dans Vetements ET Visage
        private Transform FindCategory(string categoryName)
        {
            if (armorPartsRoot != null)
            {
                Transform t = armorPartsRoot.Find(categoryName);
                if (t != null) return t;
            }
            if (facePartsRoot != null)
            {
                Transform t = facePartsRoot.Find(categoryName);
                if (t != null) return t;
            }
            return null;
        }

        // ✅ Affiche tous les noms dans la Console
        public void LogAllCategories()
        {
            if (armorPartsRoot != null)
            {
                Debug.Log("=== VETEMENTS ===");
                foreach (Transform child in armorPartsRoot)
                    Debug.Log($"  [{child.name}]  ({child.childCount} enfants)");
            }
            if (facePartsRoot != null)
            {
                Debug.Log("=== VISAGE ===");
                foreach (Transform child in facePartsRoot)
                    Debug.Log($"  [{child.name}]  ({child.childCount} enfants)");
            }
        }

        // ✅ Randomize TOUT
        public void RandomizeAll()
        {
            RandomizeArmorParts();
            RandomizeFaceParts();
        }

        public void RandomizeArmorParts()
        {
            if (armorPartsRoot == null) return;
            foreach (Transform category in armorPartsRoot)
                SetRandomActiveChild(category);
        }

        public void RandomizeFaceParts()
        {
            if (facePartsRoot == null) return;
            foreach (Transform category in facePartsRoot)
            {
                // ✅ Ignorer NOSES (pas de panel UI)
                if (category.name == "NOSES") continue;
                SetRandomActiveChild(category);
            }
        }

        private void SetRandomActiveChild(Transform category)
        {
            if (category.childCount == 0) return;
            foreach (Transform child in category)
                child.gameObject.SetActive(false);
            int rand = Random.Range(0, category.childCount);
            category.GetChild(rand).gameObject.SetActive(true);
        }
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(ModularHeroController))]
    public class ModularCharacterEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            ModularHeroController controller = (ModularHeroController)target;

            GUILayout.Space(10);
            GUILayout.Label("Editor Controls", EditorStyles.boldLabel);

            if (GUILayout.Button("Log All Category Names"))
                controller.LogAllCategories();

            GUILayout.Space(5);

            if (GUILayout.Button("Randomize ALL"))
                controller.RandomizeAll();

            if (GUILayout.Button("Randomize Armor Only"))
                controller.RandomizeArmorParts();

            if (GUILayout.Button("Randomize Face Only"))
                controller.RandomizeFaceParts();
        }
    }
#endif
}