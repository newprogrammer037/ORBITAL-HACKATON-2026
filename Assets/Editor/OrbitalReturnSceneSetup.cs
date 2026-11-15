using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class OrbitalReturnSceneSetup
{
    private const string ScenePath = "Assets/Scenes/ReturnToEarth.unity";
    private const string EarthModelPath = "Assets/Models/NASA/tierra.obj";
    private const string EarthTexturePath = "Assets/Models/NASA/tierra_BaseColor.png";
    private const string SetupVersionKey = "ORBITAL_RETURN_SCENE_SETUP_VERSION";
    private const int SetupVersion = 1;

    static OrbitalReturnSceneSetup()
    {
        EditorApplication.delayCall += EnsureReturnSceneExists;
    }

    [MenuItem("ORBITAL/Create Return-to-Earth Scene")]
    public static void CreateReturnSceneFromMenu()
    {
        if (File.Exists(ScenePath))
        {
            AddSceneToBuildSettings();
            Debug.Log("La escena ReturnToEarth ya existe; se añadió a Build Settings si faltaba.");
            return;
        }

        CreateReturnScene();
    }

    private static void EnsureReturnSceneExists()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        if (File.Exists(ScenePath))
        {
            AddSceneToBuildSettings();
            EditorPrefs.SetInt(SetupVersionKey, SetupVersion);
            return;
        }
        if (EditorPrefs.GetInt(SetupVersionKey, 0) >= SetupVersion) return;

        AssetDatabase.ImportAsset(EarthModelPath, ImportAssetOptions.ForceSynchronousImport);
        if (!AssetDatabase.LoadAssetAtPath<GameObject>(EarthModelPath))
        {
            Debug.LogError("No se pudo importar Assets/Models/NASA/tierra.obj; no se creó la escena de regreso.");
            return;
        }

        CreateReturnScene();
    }

    private static void CreateReturnScene()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Scenes")) AssetDatabase.CreateFolder("Assets", "Scenes");
        if (!AssetDatabase.IsValidFolder("Assets/Materials/Return Scene"))
            AssetDatabase.CreateFolder("Assets/Materials", "Return Scene");

        GameObject earthAsset = AssetDatabase.LoadAssetAtPath<GameObject>(EarthModelPath);
        Texture2D earthTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(EarthTexturePath);
        if (!earthAsset)
        {
            Debug.LogError("Falta el modelo NASA de la Tierra; no se puede crear ReturnToEarth.");
            return;
        }

        Scene previousActiveScene = SceneManager.GetActiveScene();
        Scene returnScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        returnScene.name = "ReturnToEarth";
        SceneManager.SetActiveScene(returnScene);

        try
        {
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(.045f,.06f,.1f);
            RenderSettings.fog = false;

            Shader litShader = Shader.Find("Universal Render Pipeline/Lit");
            if (!litShader) litShader = Shader.Find("Standard");
            Shader unlitShader = Shader.Find("Universal Render Pipeline/Unlit");
            if (!unlitShader) unlitShader = Shader.Find("Unlit/Color");
            Material earthMaterial = GetOrCreateMaterial("Assets/Materials/Return Scene/Tierra_URP.mat", litShader,
                Color.white, Color.clear, .0f, .28f, earthTexture);
            Material shipHull = GetOrCreateMaterial("Assets/Materials/Return Scene/Ship_Hull.mat", litShader,
                new Color(.24f,.34f,.42f), Color.clear, .52f, .62f);
            Material shipTrim = GetOrCreateMaterial("Assets/Materials/Return Scene/Ship_Trim.mat", litShader,
                new Color(.56f,.69f,.73f), Color.clear, .34f, .74f);
            Material shipGlass = GetOrCreateMaterial("Assets/Materials/Return Scene/Ship_Canopy.mat", litShader,
                new Color(.025f,.39f,.62f), new Color(.01f,.16f,.3f), .12f, .38f);
            Material engineGlow = GetOrCreateMaterial("Assets/Materials/Return Scene/Engine_Glow.mat", unlitShader,
                new Color(.18f,.76f,1f), new Color(.12f,.62f,1f), .0f, .0f);
            Material starWhite = GetOrCreateMaterial("Assets/Materials/Return Scene/Star_White.mat", unlitShader,
                new Color(.82f,.91f,1f), Color.clear, .0f, .0f);
            Material starBlue = GetOrCreateMaterial("Assets/Materials/Return Scene/Star_Blue.mat", unlitShader,
                new Color(.3f,.64f,1f), Color.clear, .0f, .0f);

            CreateCamera();
            CreateSunlight();
            CreateEarth(returnScene, earthAsset, earthMaterial);
            CreateStarField(starWhite, starBlue);
            Transform ship = CreateReturnVehicle(shipHull, shipTrim, shipGlass, engineGlow);

            var cutsceneObject = new GameObject("ORBITAL Return Sequence");
            SceneManager.MoveGameObjectToScene(cutsceneObject, returnScene);
            var cutscene = cutsceneObject.AddComponent<OrbitalReturnCutscene>();
            cutscene.spacecraft = ship;
            cutscene.earth = GameObject.Find("NASA Earth")?.transform;

            if (!EditorSceneManager.SaveScene(returnScene, ScenePath))
            {
                Debug.LogError("No se pudo guardar Assets/Scenes/ReturnToEarth.unity.");
                return;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            AddSceneToBuildSettings();
            EditorPrefs.SetInt(SetupVersionKey, SetupVersion);
            Debug.Log("Escena de regreso a la Tierra creada y añadida a Build Settings.");
        }
        finally
        {
            if (previousActiveScene.IsValid() && previousActiveScene.isLoaded)
                SceneManager.SetActiveScene(previousActiveScene);
            if (returnScene.IsValid() && returnScene.isLoaded)
                EditorSceneManager.CloseScene(returnScene, true);
        }
    }

    private static void CreateCamera()
    {
        var cameraObject = new GameObject("Return Camera");
        cameraObject.transform.position = new Vector3(0f,5f,-29f);
        cameraObject.transform.rotation = Quaternion.LookRotation(new Vector3(0f,0f,82f) - cameraObject.transform.position);
        var camera = cameraObject.AddComponent<Camera>();
        camera.tag = "MainCamera";
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = Color.black;
        camera.fieldOfView = 48f;
        camera.nearClipPlane = .1f;
        camera.farClipPlane = 260f;
        cameraObject.AddComponent<AudioListener>();
    }

    private static void CreateSunlight()
    {
        var sunObject = new GameObject("Earth Return Sunlight");
        sunObject.transform.rotation = Quaternion.Euler(28f,-34f,0f);
        var sun = sunObject.AddComponent<Light>();
        sun.type = LightType.Directional;
        sun.color = new Color(1f,.84f,.68f);
        sun.intensity = 1.35f;
        sun.shadows = LightShadows.Soft;
        sun.shadowStrength = .42f;
    }

    private static void CreateEarth(Scene scene, GameObject earthAsset, Material earthMaterial)
    {
        var earth = UnityEngine.Object.Instantiate(earthAsset);
        earth.name = "NASA Earth";
        SceneManager.MoveGameObjectToScene(earth, scene);
        earth.transform.position = new Vector3(0f,0f,82f);
        earth.transform.rotation = Quaternion.Euler(0f,-24f,0f);
        earth.transform.localScale = Vector3.one * 8.2f;
        foreach (var renderer in earth.GetComponentsInChildren<Renderer>(true))
        {
            var materials = renderer.sharedMaterials;
            for (int i = 0; i < materials.Length; i++) materials[i] = earthMaterial;
            renderer.sharedMaterials = materials;
            renderer.shadowCastingMode = ShadowCastingMode.On;
            renderer.receiveShadows = true;
        }
    }

    private static void CreateStarField(Material starWhite, Material starBlue)
    {
        var root = new GameObject("Return Star Field");
        var random = new System.Random(141126);
        for (int i = 0; i < 180; i++)
        {
            var star = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            star.name = "Return Star " + (i + 1).ToString("000");
            star.transform.SetParent(root.transform, false);
            star.transform.position = new Vector3((float)(random.NextDouble() * 300.0 - 150.0),
                (float)(random.NextDouble() * 180.0 - 82.0), (float)(random.NextDouble() * 70.0 + 135.0));
            float size = (float)(random.NextDouble() * .065 + .025);
            star.transform.localScale = Vector3.one * size;
            var renderer = star.GetComponent<Renderer>();
            renderer.sharedMaterial = random.Next(4) == 0 ? starBlue : starWhite;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            var collider = star.GetComponent<Collider>();
            if (collider) UnityEngine.Object.DestroyImmediate(collider);
        }
    }

    private static Transform CreateReturnVehicle(Material hull, Material trim, Material glass, Material glow)
    {
        var ship = new GameObject("ORBITAL Return Vehicle");
        ship.transform.position = new Vector3(10f,-1f,-12f);
        CreatePart(ship.transform, "Fuselage", PrimitiveType.Capsule, Vector3.zero, new Vector3(.78f,1.45f,.72f), Quaternion.Euler(90f,0f,0f), hull);
        CreatePart(ship.transform, "Nose", PrimitiveType.Cylinder, new Vector3(0f,0f,1.18f), new Vector3(.48f,.42f,.48f), Quaternion.Euler(90f,0f,0f), trim);
        CreatePart(ship.transform, "Port Wing", PrimitiveType.Cube, new Vector3(-.92f,-.02f,-.18f), new Vector3(1.18f,.11f,.86f), Quaternion.Euler(0f,-8f,-4f), hull);
        CreatePart(ship.transform, "Starboard Wing", PrimitiveType.Cube, new Vector3(.92f,-.02f,-.18f), new Vector3(1.18f,.11f,.86f), Quaternion.Euler(0f,8f,4f), hull);
        CreatePart(ship.transform, "Port Stabilizer", PrimitiveType.Cube, new Vector3(-.42f,.28f,-.83f), new Vector3(.18f,.48f,.42f), Quaternion.identity, trim);
        CreatePart(ship.transform, "Starboard Stabilizer", PrimitiveType.Cube, new Vector3(.42f,.28f,-.83f), new Vector3(.18f,.48f,.42f), Quaternion.identity, trim);
        CreatePart(ship.transform, "Cockpit", PrimitiveType.Sphere, new Vector3(0f,.22f,.48f), new Vector3(.38f,.3f,.62f), Quaternion.identity, glass);
        CreatePart(ship.transform, "Port Engine", PrimitiveType.Cylinder, new Vector3(-.25f,0f,-.95f), new Vector3(.22f,.22f,.42f), Quaternion.Euler(90f,0f,0f), trim);
        CreatePart(ship.transform, "Starboard Engine", PrimitiveType.Cylinder, new Vector3(.25f,0f,-.95f), new Vector3(.22f,.22f,.42f), Quaternion.Euler(90f,0f,0f), trim);
        CreatePart(ship.transform, "Port Engine Glow", PrimitiveType.Sphere, new Vector3(-.25f,0f,-1.34f), new Vector3(.25f,.25f,.25f), Quaternion.identity, glow);
        CreatePart(ship.transform, "Starboard Engine Glow", PrimitiveType.Sphere, new Vector3(.25f,0f,-1.34f), new Vector3(.25f,.25f,.25f), Quaternion.identity, glow);

        AddEngineLight(ship.transform, "Port Engine Light", new Vector3(-.25f,0f,-1.25f));
        AddEngineLight(ship.transform, "Starboard Engine Light", new Vector3(.25f,0f,-1.25f));
        return ship.transform;
    }

    private static void AddEngineLight(Transform parent, string name, Vector3 localPosition)
    {
        var lightObject = new GameObject(name);
        lightObject.transform.SetParent(parent, false);
        lightObject.transform.localPosition = localPosition;
        var light = lightObject.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = new Color(.16f,.68f,1f);
        light.intensity = 1.7f;
        light.range = 5f;
        light.shadows = LightShadows.None;
    }

    private static GameObject CreatePart(Transform parent, string name, PrimitiveType type, Vector3 position, Vector3 scale, Quaternion rotation, Material material)
    {
        var part = GameObject.CreatePrimitive(type);
        part.name = name;
        part.transform.SetParent(parent, false);
        part.transform.localPosition = position;
        part.transform.localScale = scale;
        part.transform.localRotation = rotation;
        var collider = part.GetComponent<Collider>();
        if (collider) UnityEngine.Object.DestroyImmediate(collider);
        var renderer = part.GetComponent<Renderer>();
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        return part;
    }

    private static Material GetOrCreateMaterial(string path, Shader shader, Color baseColor, Color emission, float metallic, float smoothness, Texture texture = null)
    {
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (!material)
        {
            material = new Material(shader) { name = Path.GetFileNameWithoutExtension(path) };
            AssetDatabase.CreateAsset(material, path);
        }

        if (shader && material.shader != shader) material.shader = shader;
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", baseColor);
        if (material.HasProperty("_Color")) material.SetColor("_Color", baseColor);
        if (material.HasProperty("_BaseMap")) material.SetTexture("_BaseMap", texture);
        if (material.HasProperty("_MainTex")) material.SetTexture("_MainTex", texture);
        if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", metallic);
        if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", smoothness);
        if (material.HasProperty("_EmissionColor"))
        {
            if (emission.maxColorComponent > .001f) material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", emission);
        }
        EditorUtility.SetDirty(material);
        return material;
    }

    private static void AddSceneToBuildSettings()
    {
        var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        bool found = false;
        foreach (var scene in scenes)
        {
            if (scene.path != ScenePath) continue;
            scene.enabled = true;
            found = true;
        }
        if (!found) scenes.Add(new EditorBuildSettingsScene(ScenePath, true));
        EditorBuildSettings.scenes = scenes.ToArray();
    }
}
