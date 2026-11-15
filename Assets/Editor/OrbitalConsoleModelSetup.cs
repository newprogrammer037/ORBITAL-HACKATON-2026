using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class OrbitalConsoleModelSetup
{
    private const string ScenePath = "Assets/Scenes/Spaceship.unity";
    private const string ModelPath = "Assets/Models/Screens_LowPoly_console.obj";
    private const string TargetName = "Mission console - interact with E";
    private const string ModelChildName = "Screens LowPoly console";

    [MenuItem("ORBITAL/Replace Mission Console With Screens LowPoly Model")]
    public static void ReplaceMissionConsole()
    {
        var modelAsset = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
        if (!modelAsset)
        {
            Debug.LogError("No se encontró el modelo importado: " + ModelPath);
            return;
        }

        Scene previousActive = SceneManager.GetActiveScene();
        Scene scene = SceneManager.GetSceneByPath(ScenePath);
        bool openedForSetup = !scene.IsValid() || !scene.isLoaded;
        if (openedForSetup) scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);

        try
        {
            SceneManager.SetActiveScene(scene);
            GameObject target = FindObject(scene, TargetName);
            if (!target)
            {
                Debug.LogError("No se encontró el objeto interactivo de la consola en Spaceship.");
                return;
            }

            var oldChild = target.transform.Find(ModelChildName);
            if (oldChild) Object.DestroyImmediate(oldChild.gameObject);

            var oldRenderer = target.GetComponent<MeshRenderer>();
            if (oldRenderer) Object.DestroyImmediate(oldRenderer);
            var oldFilter = target.GetComponent<MeshFilter>();
            if (oldFilter) Object.DestroyImmediate(oldFilter);

            target.transform.localScale = Vector3.one;
            var box = target.GetComponent<BoxCollider>();
            if (!box) box = target.AddComponent<BoxCollider>();
            box.center = new Vector3(0f,0f,-.55f);
            box.size = new Vector3(3.5f,1.25f,1.2f);

            var model = PrefabUtility.InstantiatePrefab(modelAsset, scene) as GameObject;
            if (!model)
            {
                Debug.LogError("Unity no pudo crear una instancia del modelo Screens LowPoly console.");
                return;
            }

            model.name = ModelChildName;
            model.transform.SetParent(target.transform, false);
            model.transform.localRotation = Quaternion.Euler(0f,90f,0f);
            model.transform.localScale = Vector3.one * .35f;
            // Center the imported GLB-derived mesh and rest its base on the existing console surface.
            model.transform.localPosition = new Vector3(-.374f,.19f,-1.03f);

            foreach (var collider in model.GetComponentsInChildren<Collider>(true))
                Object.DestroyImmediate(collider);

            EditorUtility.SetDirty(target);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log("Screens LowPoly console reemplazó la geometría del panel y conserva la interacción con E.");
        }
        finally
        {
            if (previousActive.IsValid() && previousActive.isLoaded) SceneManager.SetActiveScene(previousActive);
            if (openedForSetup && scene.IsValid() && scene.isLoaded) EditorSceneManager.CloseScene(scene, true);
        }
    }

    private static GameObject FindObject(Scene scene, string objectName)
    {
        foreach (var root in scene.GetRootGameObjects())
        {
            if (root.name == objectName) return root;
            var child = root.transform.Find(objectName);
            if (child) return child.gameObject;
        }
        return null;
    }
}
