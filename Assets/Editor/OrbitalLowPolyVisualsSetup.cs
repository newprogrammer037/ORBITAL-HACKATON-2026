using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class OrbitalLowPolyVisualsSetup
{
    private const string ScenePath = "Assets/Scenes/Spaceship.unity";
    private const string OtherPath = "Assets/Models/Screens_LowPoly_other.obj";
    private const string WallPath = "Assets/Models/Screens_LowPoly_wall.obj";

    [MenuItem("ORBITAL/Replace Console Displays And Insignia With LowPoly Screens")]
    public static void ReplaceVisuals()
    {
        var otherAsset = AssetDatabase.LoadAssetAtPath<GameObject>(OtherPath);
        var wallAsset = AssetDatabase.LoadAssetAtPath<GameObject>(WallPath);
        if (!otherAsset || !wallAsset)
        {
            Debug.LogError("No se encontraron ambos modelos Screens LowPoly importados.");
            return;
        }

        Scene previousActive = SceneManager.GetActiveScene();
        Scene scene = SceneManager.GetSceneByPath(ScenePath);
        bool openedForSetup = !scene.IsValid() || !scene.isLoaded;
        if (openedForSetup) scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);

        try
        {
            SceneManager.SetActiveScene(scene);
            int displays = ReplaceAll(scene, "Console display", "Screens LowPoly other", otherAsset, true);
            int insignia = ReplaceAll(scene, "Orbital insignia", "Screens LowPoly wall", wallAsset, false);
            if (displays != 6 || insignia != 1)
            {
                Debug.LogError($"Reemplazo incompleto: displays={displays}, insignia={insignia}. Se esperaba 6 y 1.");
                return;
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log("Se reemplazaron los 6 Console display y Orbital insignia; se conservaron sus colliders y posiciones.");
        }
        finally
        {
            if (previousActive.IsValid() && previousActive.isLoaded) SceneManager.SetActiveScene(previousActive);
            if (openedForSetup && scene.IsValid() && scene.isLoaded) EditorSceneManager.CloseScene(scene, true);
        }
    }

    private static int ReplaceAll(Scene scene, string targetName, string childName, GameObject modelAsset, bool tabletop)
    {
        int replaced = 0;
        foreach (var root in scene.GetRootGameObjects())
            ReplaceRecursive(root.transform, targetName, childName, modelAsset, tabletop, ref replaced);
        return replaced;
    }

    private static void ReplaceRecursive(Transform current, string targetName, string childName, GameObject modelAsset, bool tabletop, ref int replaced)
    {
        if (current.name == targetName)
        {
            Transform existing = current.Find(childName);
            if (existing)
            {
                replaced++;
                return;
            }

            var oldFilter = current.GetComponent<MeshFilter>();
            var oldRenderer = current.GetComponent<MeshRenderer>();
            if (!oldFilter || !oldRenderer || !oldFilter.sharedMesh)
            {
                Debug.LogWarning("Se omitió " + current.name + " porque ya no conserva su visual original.");
                return;
            }

            Vector3 originalSize = Vector3.Scale(oldFilter.sharedMesh.bounds.size, current.localScale);
            if (oldRenderer) Object.DestroyImmediate(oldRenderer);
            if (oldFilter) Object.DestroyImmediate(oldFilter);

            current.localScale = Vector3.one;
            var collider = current.GetComponent<BoxCollider>();
            if (!collider) collider = current.gameObject.AddComponent<BoxCollider>();
            collider.center = Vector3.zero;
            collider.size = originalSize;

            var model = PrefabUtility.InstantiatePrefab(modelAsset, current.gameObject.scene) as GameObject;
            if (!model)
            {
                Debug.LogError("Unity no pudo instanciar " + childName);
                return;
            }

            model.name = childName;
            model.transform.SetParent(current, false);
            var modelFilter = model.GetComponentInChildren<MeshFilter>();
            if (!modelFilter || !modelFilter.sharedMesh)
            {
                Object.DestroyImmediate(model);
                Debug.LogError("El modelo no contiene una malla: " + childName);
                return;
            }

            Bounds bounds = modelFilter.sharedMesh.bounds;
            Vector3 scale;
            if (tabletop)
            {
                model.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
                scale = new Vector3(originalSize.y / bounds.size.x, originalSize.x / bounds.size.y, originalSize.z / bounds.size.z);
            }
            else
            {
                model.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
                scale = new Vector3(originalSize.z / bounds.size.x, originalSize.y / bounds.size.y, originalSize.x / bounds.size.z);
            }

            model.transform.localScale = scale;
            model.transform.localPosition = -(model.transform.localRotation * Vector3.Scale(bounds.center, scale));
            foreach (var nestedCollider in model.GetComponentsInChildren<Collider>(true))
                Object.DestroyImmediate(nestedCollider);

            EditorUtility.SetDirty(current.gameObject);
            replaced++;
            return;
        }

        for (int i = 0; i < current.childCount; i++)
            ReplaceRecursive(current.GetChild(i), targetName, childName, modelAsset, tabletop, ref replaced);
    }
}
