using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class OrbitalPrototypeBuilder
{
    private const string ScenePath = "Assets/Scenes/Spaceship.unity";
    private const string MenuScenePath = "Assets/Scenes/MainMenu.unity";
    static OrbitalPrototypeBuilder()
    {
        var startupScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(MenuScenePath);
        if (startupScene) EditorSceneManager.playModeStartScene = startupScene;
    }

    [MenuItem("ORBITAL/Build Spaceship Prototype")]
    public static void Build()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Scenes")) AssetDatabase.CreateFolder("Assets", "Scenes");
        var previous = EditorSceneManager.GetSceneManagerSetup();
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        scene.name = "Spaceship";
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(.07f,.08f,.1f);
        RenderSettings.fog = true; RenderSettings.fogColor = new Color(.003f,.006f,.018f); RenderSettings.fogMode = FogMode.Exponential; RenderSettings.fogDensity = .004f;

        Material hull = Mat("Hull - graphite", new Color(.045f,.075f,.105f), .75f);
        Material floor = Mat("Deck - dark composite", new Color(.07f,.12f,.16f), .35f);
        Material trim = Mat("Trim - brushed alloy", new Color(.25f,.38f,.45f), .72f);
        Material cyan = Emissive("Signal - cyan", new Color(.05f,.75f,1f), 2.3f);
        Material blue = Mat("Planet - ocean", new Color(.025f,.22f,.53f), .18f);
        Material green = Mat("Planet - land", new Color(.18f,.48f,.29f), .2f);
        Material glass = Emissive("Viewport - deep space", new Color(.008f,.025f,.075f), .45f);

        // Compact command bridge with a broad observation viewport.
        Cube("Deck", new Vector3(0,-.18f,0), new Vector3(18,.36f,15), floor);
        Cube("Ceiling", new Vector3(0,4.8f,0), new Vector3(18,.28f,15), hull);
        Cube("Port bulkhead", new Vector3(-9,2.35f,0), new Vector3(.3f,4.7f,15), hull);
        Cube("Starboard bulkhead", new Vector3(9,2.35f,0), new Vector3(.3f,4.7f,15), hull);
        Cube("Rear bulkhead", new Vector3(0,2.35f,7.5f), new Vector3(18,4.7f,.3f), hull);
        Cube("Space backdrop", new Vector3(0,2.76f,-10.6f), new Vector3(24f,7f,.12f), glass);
        // Structural window ribs and floor guidance.
        for(int x=-7;x<=7;x+=7) Cube("Window frame",new Vector3(x,2.5f,-7.18f),new Vector3(.14f,4.45f,.16f),trim);
        Cube("Window frame",new Vector3(0,.38f,-7.18f),new Vector3(15.9f,.14f,.16f),trim);
        Cube("Window frame",new Vector3(0,4.62f,-7.18f),new Vector3(15.9f,.14f,.16f),trim);
        for(int x=-7;x<=7;x+=2)
        {
            Cube("Deck guide",new Vector3(x,.018f,0),new Vector3(.035f,.015f,12.5f), x%4==0?cyan:trim);
            Cube("Ceiling light",new Vector3(x,4.56f,0),new Vector3(.055f,.025f,11f),cyan);
        }
        // Planet presentation visible through the front window.
        Sphere("Earth",new Vector3(0,2.7f,-9.3f),new Vector3(3.45f,3.45f,1.1f),blue);
        Vector3[] land = {new Vector3(-.47f,.56f,-9.05f),new Vector3(.55f,.18f,-9.02f),new Vector3(-.05f,-.55f,-9.0f),new Vector3(.78f,-.48f,-9.02f),new Vector3(-.95f,-.12f,-9.0f)};
        foreach(var p in land) Sphere("Continental relief",new Vector3(p.x,2.7f+p.y,p.z),new Vector3(.68f,.42f,.09f),green);
        // Workstations on both sides leave a clear aisle to the mission console.
        for(int side=-1;side<=1;side+=2)
        {
            for(int i=0;i<3;i++)
            {
                float z=3.5f-i*3.0f;
                Cube("Side console",new Vector3(side*6.75f,1.05f,z),new Vector3(1.25f,1.7f,2.05f),hull);
                Cube("Console display",new Vector3(side*6.75f,2.0f,z-.15f),new Vector3(1.05f,.06f,1.3f),cyan);
                Cube("Console footing",new Vector3(side*6.75f,.12f,z),new Vector3(1.4f,.24f,2.2f),trim);
            }
        }
        // Interactive mission station stands in the center of the bridge.
        Cube("Command pedestal",new Vector3(0,.72f,-1.2f),new Vector3(4.8f,1.35f,1.0f),hull);
        Cube("Command console surface",new Vector3(0,1.42f,-1.05f),new Vector3(4.6f,.12f,1.55f),trim);
        GameObject consoleTarget=Cube("Mission console - interact with E",new Vector3(0,1.98f,-.02f),new Vector3(3.5f,1.05f,.16f),cyan);
        consoleTarget.AddComponent<OrbitalMissionConsole>();
        Cube("Console lower bezel",new Vector3(0,1.48f,-.02f),new Vector3(3.8f,.13f,.2f),trim);
        for(int i=0;i<8;i++)
        {
            float x=-1.7f+i*.48f;
            Cube("Control key",new Vector3(x,1.51f,-.52f),new Vector3(.27f,.07f,.28f),i%2==0?cyan:trim);
        }
        // Center aisle wayfinding and station signage.
        Cube("Orbital insignia",new Vector3(0,3.85f,7.31f),new Vector3(3.6f,.8f,.08f),cyan);
        Label("O R B I T A L   /   M I S S I O N   C O N T R O L",new Vector3(0,3.85f,7.24f),Quaternion.Euler(0,180,0),2.4f,Color.white);

        var player=new GameObject("Astronaut - First Person");
        player.transform.position=new Vector3(0,0,4.75f);
        var character=player.AddComponent<CharacterController>(); character.height=1.8f;character.radius=.34f;character.center=new Vector3(0,.9f,0);character.stepOffset=.3f;
        var camGo=new GameObject("Player Camera");camGo.transform.SetParent(player.transform,false);camGo.transform.localPosition=new Vector3(0,1.58f,0);
        var cam=camGo.AddComponent<Camera>();cam.fieldOfView=76;cam.nearClipPlane=.05f;cam.farClipPlane=180;cam.tag="MainCamera";
        camGo.AddComponent<AudioListener>();
        var controller=player.AddComponent<OrbitalPlayerController>();
        var serialized=new SerializedObject(controller);serialized.FindProperty("playerCamera").objectReferenceValue=cam;serialized.ApplyModifiedPropertiesWithoutUndo();

        ApplyNightLighting(scene);

        EditorSceneManager.SaveScene(scene,ScenePath);
        const string menuPath="Assets/Scenes/MainMenu.unity";
        Scene menu=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        menu.name="MainMenu";
        RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight=new Color(.12f,.18f,.27f);
        var cameraObject=new GameObject("Menu Camera");cameraObject.transform.position=new Vector3(0,1.2f,-12);cameraObject.transform.rotation=Quaternion.identity;
        var menuCamera=cameraObject.AddComponent<Camera>();menuCamera.tag="MainCamera";menuCamera.clearFlags=CameraClearFlags.SolidColor;menuCamera.backgroundColor=new Color(.006f,.018f,.04f);menuCamera.fieldOfView=60;
        Cube("Distant starfield",new Vector3(0,1.2f,11),new Vector3(26,15,.2f),glass);
        Sphere("Orbiting Earth",new Vector3(8.2f,3.5f,5.3f),new Vector3(5.4f,5.4f,1.2f),blue);
        foreach(var p in new[]{new Vector3(7.5f,3.0f,0),new Vector3(8.7f,4.05f,0),new Vector3(9.5f,3.25f,0),new Vector3(8.3f,2.2f,0)})
            Sphere("Earth continent",new Vector3(p.x,p.y,4.65f),new Vector3(.68f,.44f,.08f),green);
        var menuLightObject=new GameObject("Menu key light");menuLightObject.transform.rotation=Quaternion.Euler(25,-32,0);
        var menuLight=menuLightObject.AddComponent<Light>();menuLight.type=LightType.Directional;menuLight.color=new Color(.62f,.78f,1);menuLight.intensity=1.25f;
        new GameObject("ORBITAL Main Menu").AddComponent<OrbitalMainMenu>();
        EditorSceneManager.SaveScene(menu,menuPath);
        var scenes=new List<EditorBuildSettingsScene>();
        scenes.Add(new EditorBuildSettingsScene(menuPath,true));
        scenes.Add(new EditorBuildSettingsScene(ScenePath,true));
        foreach(var item in EditorBuildSettings.scenes) if(item.path!=ScenePath) scenes.Add(item);
        EditorBuildSettings.scenes=scenes.ToArray();
        AssetDatabase.SaveAssets();
        if(previous!=null && previous.Length>0) EditorSceneManager.RestoreSceneManagerSetup(previous);
        Debug.Log("ORBITAL: nave guardada en " + ScenePath);
    }

    [MenuItem("ORBITAL/Apply Night Exterior and Spot Lighting")]
    public static void ApplyNightExteriorAndSpots()
    {
        Scene previousActive = SceneManager.GetActiveScene();
        Scene scene = SceneManager.GetSceneByPath(ScenePath);
        bool openedForSetup = !scene.IsValid() || !scene.isLoaded;
        try
        {
            if (openedForSetup) scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            SceneManager.SetActiveScene(scene);
            ApplyNightLighting(scene);
            EditorSceneManager.SaveScene(scene);
        }
        finally
        {
            if (previousActive.IsValid() && previousActive.isLoaded && previousActive != scene)
                SceneManager.SetActiveScene(previousActive);
            if (openedForSetup && scene.IsValid() && scene.isLoaded)
                EditorSceneManager.CloseScene(scene, true);
        }
    }

    private static void ApplyNightLighting(Scene scene)
    {
        var previousActive = SceneManager.GetActiveScene();
        if (scene.IsValid()) SceneManager.SetActiveScene(scene);

        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(.07f,.08f,.1f);
        RenderSettings.fog = true;
        RenderSettings.fogColor = new Color(.003f,.006f,.018f);
        RenderSettings.fogMode = FogMode.Exponential;
        RenderSettings.fogDensity = .004f;

        foreach (var root in scene.GetRootGameObjects())
        {
            if (root.name == "Bridge lighting" || root.name == "Cyan fill light" || root.name == "Warm console fill" ||
                root.name == "Exterior Stars" || root.name == "Interior Spotlights" || root.name == "Night Moonlight" || root.name == "Window Safety Barrier")
                Object.DestroyImmediate(root);
        }

        foreach (var root in scene.GetRootGameObjects())
        {
            if (root.name == "Earth" || root.name == "Continental relief" || root.name == "Space backdrop")
                SetLayerRecursively(root, 31);
        }

        var backdrop = FindRoot(scene, "Space backdrop");
        if (backdrop)
        {
            backdrop.transform.position = new Vector3(0f,2.76f,-10.6f);
            backdrop.transform.localScale = new Vector3(24f,7f,.12f);
            var renderer = backdrop.GetComponent<Renderer>();
            if (renderer) renderer.sharedMaterial = GetOrCreateUnlitMaterial("Night Space Backdrop", new Color(.0015f,.0025f,.009f));
        }

        var windowBarrier = new GameObject("Window Safety Barrier");
        windowBarrier.transform.position = new Vector3(0f,2.5f,-7.04f);
        var barrierCollider = windowBarrier.AddComponent<BoxCollider>();
        barrierCollider.size = new Vector3(15.8f,4.35f,.12f);
        SceneManager.MoveGameObjectToScene(windowBarrier, scene);

        var starRoot = new GameObject("Exterior Stars");
        starRoot.layer = 31;
        var starMaterials = new[]
        {
            GetOrCreateUnlitMaterial("Star White", new Color(.9f,.94f,1f)),
            GetOrCreateUnlitMaterial("Star Cool", new Color(.55f,.74f,1f)),
            GetOrCreateUnlitMaterial("Star Warm", new Color(1f,.83f,.62f))
        };
        var random = new System.Random(241108);
        for (int i = 0; i < 150; i++)
        {
            var star = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            star.name = "Star " + (i + 1).ToString("000");
            star.transform.SetParent(starRoot.transform, false);
            star.transform.localPosition = new Vector3((float)(random.NextDouble() * 22.2 - 11.1), (float)(random.NextDouble() * 6.5 - .5), -10.49f);
            float size = (float)(random.NextDouble() * .035 + .018);
            star.transform.localScale = Vector3.one * size;
            star.layer = 31;
            var starRenderer = star.GetComponent<Renderer>();
            starRenderer.sharedMaterial = starMaterials[random.Next(starMaterials.Length)];
            starRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            starRenderer.receiveShadows = false;
            var collider = star.GetComponent<Collider>();
            if (collider) Object.DestroyImmediate(collider);
        }
        SceneManager.MoveGameObjectToScene(starRoot, scene);

        var moonLightObject = new GameObject("Night Moonlight");
        moonLightObject.layer = 31;
        moonLightObject.transform.rotation = Quaternion.Euler(24f, -38f, 0f);
        var moonLight = moonLightObject.AddComponent<Light>();
        moonLight.type = LightType.Directional;
        moonLight.color = new Color(.52f,.68f,1f);
        moonLight.intensity = .38f;
        moonLight.cullingMask = 1 << 31;
        moonLight.shadows = LightShadows.None;

        var spotRoot = new GameObject("Interior Spotlights");
        Vector3[] ceilingSpots =
        {
            new Vector3(-6f,4.42f,-3.2f), new Vector3(-2f,4.42f,-3.2f), new Vector3(2f,4.42f,-3.2f), new Vector3(6f,4.42f,-3.2f),
            new Vector3(-6f,4.42f,3.1f), new Vector3(-2f,4.42f,3.1f), new Vector3(2f,4.42f,3.1f), new Vector3(6f,4.42f,3.1f)
        };
        for (int i = 0; i < ceilingSpots.Length; i++)
        {
            Vector3 position = ceilingSpots[i];
            CreateSpot(spotRoot.transform, "Ceiling Spot " + (i + 1).ToString("00"), position, new Vector3(position.x,.15f,position.z),
                4.2f, 7.5f, 68f, new Color(.76f,.86f,1f), LightShadows.None);
        }
        CreateSpot(spotRoot.transform, "Console Focus Spot L", new Vector3(-2.3f,4.25f,-.4f), new Vector3(0f,1.2f,-.25f),
            5.5f, 6.5f, 48f, new Color(.65f,.82f,1f), LightShadows.Soft);
        CreateSpot(spotRoot.transform, "Console Focus Spot R", new Vector3(2.3f,4.25f,-.4f), new Vector3(0f,1.2f,-.25f),
            5.5f, 6.5f, 48f, new Color(.65f,.82f,1f), LightShadows.Soft);
        SceneManager.MoveGameObjectToScene(spotRoot, scene);

        if (previousActive.IsValid() && previousActive != scene) SceneManager.SetActiveScene(previousActive);
    }

    private static void CreateSpot(Transform parent, string name, Vector3 position, Vector3 target, float intensity, float range, float angle, Color color, LightShadows shadows)
    {
        var spotObject = new GameObject(name);
        spotObject.transform.SetParent(parent, false);
        spotObject.transform.position = position;
        spotObject.transform.rotation = Quaternion.LookRotation(target - position, Vector3.forward);
        var spot = spotObject.AddComponent<Light>();
        spot.type = LightType.Spot;
        spot.color = color;
        spot.intensity = intensity;
        spot.range = range;
        spot.spotAngle = angle;
        spot.innerSpotAngle = angle * .68f;
        spot.shadows = shadows;
        spot.shadowStrength = .38f;
        spot.cullingMask = 1;
    }

    private static GameObject FindRoot(Scene scene, string name)
    {
        foreach (var root in scene.GetRootGameObjects()) if (root.name == name) return root;
        return null;
    }

    private static void SetLayerRecursively(GameObject target, int layer)
    {
        target.layer = layer;
        foreach (Transform child in target.transform) SetLayerRecursively(child.gameObject, layer);
    }

    private static Material GetOrCreateUnlitMaterial(string name, Color color)
    {
        const string folder = "Assets/Materials";
        string path = folder + "/" + name.Replace(" ", "_") + ".mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (!material)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (!shader) shader = Shader.Find("Unlit/Color");
            if (!shader) shader = Shader.Find("Sprites/Default");
            if (!shader) shader = Shader.Find("Standard");
            material = new Material(shader) { name = name };
            AssetDatabase.CreateAsset(material, path);
        }
        material.color = color;
        EditorUtility.SetDirty(material);
        return material;
    }

    private static Material Mat(string name,Color color,float metallic)
    {
        const string folder="Assets/Materials";
        if(!AssetDatabase.IsValidFolder(folder)) AssetDatabase.CreateFolder("Assets","Materials");
        string path=folder+"/"+name.Replace(" - ","_").Replace(" ","_")+".mat";
        var existing=AssetDatabase.LoadAssetAtPath<Material>(path);if(existing)return existing;
        Shader shader=Shader.Find("Universal Render Pipeline/Lit");if(!shader)shader=Shader.Find("Standard");
        var m=new Material(shader){name=name,color=color};
        if(m.HasProperty("_Metallic"))m.SetFloat("_Metallic",metallic);
        if(m.HasProperty("_Smoothness"))m.SetFloat("_Smoothness",.42f);
        AssetDatabase.CreateAsset(m,path);
        return m;
    }
    private static Material Emissive(string name,Color color,float power)
    {
        var m=Mat(name,color,.25f);if(m.HasProperty("_EmissionColor")){m.EnableKeyword("_EMISSION");m.SetColor("_EmissionColor",color*power);}return m;
    }
    private static GameObject Cube(string name,Vector3 position,Vector3 scale,Material material)
    {
        var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.transform.position=position;go.transform.localScale=scale;go.GetComponent<Renderer>().sharedMaterial=material;return go;
    }
    private static GameObject Sphere(string name,Vector3 position,Vector3 scale,Material material)
    {
        var go=GameObject.CreatePrimitive(PrimitiveType.Sphere);go.name=name;go.transform.position=position;go.transform.localScale=scale;go.GetComponent<Renderer>().sharedMaterial=material;return go;
    }
    private static void Label(string text,Vector3 pos,Quaternion rotation,float size,Color color)
    {
        var go=new GameObject("Station title",typeof(TextMesh));go.transform.position=pos;go.transform.rotation=rotation;
        var mesh=go.GetComponent<TextMesh>();mesh.text=text;mesh.fontSize=64;mesh.characterSize=size/10f;mesh.anchor=TextAnchor.MiddleCenter;mesh.color=color;
    }
}
