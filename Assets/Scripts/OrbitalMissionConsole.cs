using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[Serializable]
public class OrbitalRegion
{
    public string name;
    public string hazard;
    public string conditions;
    public int risk;
    public bool resolved;

    public OrbitalRegion(string name, string hazard, string conditions, int risk)
    { this.name = name; this.hazard = hazard; this.conditions = conditions; this.risk = risk; }
}

public class OrbitalMissionConsole : MonoBehaviour
{
    public static bool UIOpen { get; private set; }
    private static OrbitalMissionConsole current;
    private static Font uiFont;
    private System.Random weatherRandom;
    private OrbitalRegion[] regions;
    private int selected = 1;
    private int budget = 680000000;
    private int operations;
    private long simulatedMinutes = 8 * 60;
    private float realElapsed;
    private int lastDisplayedElapsedSecond = -1;
    private bool missionStarted;
    private bool missionComplete;
    private GameObject root;
    private GameObject completionBanner;
    private Text statusText, detailText, clockText, resolutionText;
    private Button[] interventionButtons;
    private OrbitalPlanetView planetView;
    private RenderTexture planetTexture;
    private Camera planetCamera;
    private Transform planetRoot;
    private Transform[] planetMarkers;

    private struct InterventionOption
    {
        public string name;
        public int cost;
        public int durationHours;
        public int riskReduction;
        public InterventionOption(string name, int cost, int durationHours, int riskReduction)
        { this.name = name; this.cost = cost; this.durationHours = durationHours; this.riskReduction = riskReduction; }
    }

    private readonly InterventionOption[] options = {
        new InterventionOption("RESPUESTA FOCALIZADA", 60000000, 24, 26),
        new InterventionOption("CONTENCIÓN COORDINADA", 105000000, 48, 52),
        new InterventionOption("OPERACIÓN ESTRATÉGICA", 170000000, 72, 78)
    };

    private void Awake()
    {
        current = this;
        regions = new[] {
            new OrbitalRegion("AMÉRICA DEL NORTE", "Incendios forestales", "", 76),
            new OrbitalRegion("AMÉRICA DEL SUR", "Sequía e incendios", "", 88),
            new OrbitalRegion("EUROPA", "Ola de calor", "", 61),
            new OrbitalRegion("ÁFRICA", "Estrés hídrico", "", 82),
            new OrbitalRegion("ASIA", "Monzón e inundaciones", "", 72),
            new OrbitalRegion("OCEANÍA", "Riesgo de ciclón", "", 67)
        };
        weatherRandom = new System.Random(unchecked((int)(DateTime.UtcNow.Ticks ^ Guid.NewGuid().GetHashCode())));
        RandomizeWeatherAndRisk();
        budget = Mathf.Max(budget, CalculateMinimumMissionBudget());
        try
        {
            BuildUI();
            Close();
        }
        catch (Exception exception)
        {
            UIOpen = false;
            if (root) root.SetActive(false);
            Debug.LogException(exception, this);
        }
    }

    private void Update()
    {
        if (OrbitalPauseMenu.IsPaused) return;
        if (missionStarted && !missionComplete)
        {
            realElapsed += Time.unscaledDeltaTime;
            int elapsedSecond = Mathf.FloorToInt(realElapsed);
            if (elapsedSecond == lastDisplayedElapsedSecond) return;
            RefreshStatus();
        }
    }

    public void Open()
    {
        if (!root || planetView == null || interventionButtons == null || interventionButtons.Length != options.Length)
        {
            Debug.LogError("La consola ORBITAL no pudo inicializar su interfaz; revisa los errores anteriores.", this);
            return;
        }
        UIOpen = true;
        OrbitalAudio.Play("consola_activacion");
        root.SetActive(true);
        if (planetCamera) planetCamera.enabled = true;
        if (!missionStarted)
        {
            missionStarted = true;
            realElapsed = 0f;
        }
        OrbitalPlayerController.SetCursor(false);
        RefreshAll();
    }

    public static void CloseUI() { if (current) current.Close(); }
    private void Close()
    {
        if (missionComplete) return;
        OrbitalAudio.Play("cerrar_consola");
        UIOpen = false;
        if (root) root.SetActive(false);
        if (planetCamera) planetCamera.enabled = false;
        OrbitalPlayerController.SetCursor(true);
    }

    private void OnDestroy()
    {
        if (planetTexture)
        {
            planetTexture.Release();
            Destroy(planetTexture);
        }
    }

    private void BuildUI()
    {
        if (!FindFirstObjectByType<EventSystem>())
            new GameObject("UI Event System", typeof(EventSystem), typeof(InputSystemUIInputModule));
        var canvasGO = new GameObject("Mission Interface", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = canvasGO.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 20;
        var scaler = canvasGO.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(1600, 900);
        root = Panel("Backdrop", canvas.transform, new Color(0.015f, 0.035f, 0.065f, 0.97f), new Vector2(.04f,.05f), new Vector2(.96f,.95f));
        root.SetActive(false);
        Label("ORBITAL  /  CENTRO DE COMANDO", root.transform, 30, new Vector2(.04f,.9f), new Vector2(.5f,.98f), new Color(.42f,.88f,1));
        Label("SELECCIONA UN SECTOR DEL PLANETA", root.transform, 13, new Vector2(.04f,.85f), new Vector2(.53f,.91f), new Color(.48f,.65f,.78f));
        statusText = Label("", root.transform, 14, new Vector2(.55f,.9f), new Vector2(.79f,.98f), new Color(.55f,1,.78f), TextAnchor.MiddleRight);
        clockText = Label("", root.transform, 14, new Vector2(.55f,.83f), new Vector2(.96f,.9f), Color.white, TextAnchor.MiddleRight);
        Label("INFORME METEOROLÓGICO DE LA MISIÓN", root.transform, 12, new Vector2(.04f,.03f), new Vector2(.72f,.08f), new Color(.5f,.78f,.86f));

        var planetPanel=Panel("Planet Viewport",root.transform,new Color(.035f,.085f,.13f,.95f),new Vector2(.04f,.14f),new Vector2(.55f,.81f));
        Label("TIERRA  /  MAPA DE OPERACIONES",planetPanel.transform,16,new Vector2(.04f,.9f),new Vector2(.95f,.99f),Color.white);
        var rawObject=new GameObject("Interactive 3D Planet",typeof(RectTransform),typeof(RawImage),typeof(OrbitalPlanetView));rawObject.transform.SetParent(planetPanel.transform,false);
        var rawRect=rawObject.GetComponent<RectTransform>();rawRect.anchorMin=new Vector2(.035f,.12f);rawRect.anchorMax=new Vector2(.965f,.89f);rawRect.offsetMin=rawRect.offsetMax=Vector2.zero;
        var rawImage=rawObject.GetComponent<RawImage>();rawImage.color=Color.white;rawImage.raycastTarget=false;
        planetView=rawObject.GetComponent<OrbitalPlanetView>();
        BuildPlanet(rawImage);
        Label("ARRASTRA PARA ROTAR  ·  RUEDA PARA ACERCAR  ·  CLIC EN UN SECTOR",planetPanel.transform,12,new Vector2(.03f,.02f),new Vector2(.97f,.11f),new Color(.5f,.78f,.88f),TextAnchor.MiddleCenter);

        var right=Panel("Mission Details",root.transform,new Color(.035f,.085f,.13f,.95f),new Vector2(.58f,.14f),new Vector2(.96f,.81f));
        Label("MISIÓN Y RESPUESTA",right.transform,16,new Vector2(.05f,.9f),new Vector2(.95f,.99f),new Color(.42f,.88f,1));
        detailText=Label("",right.transform,17,new Vector2(.05f,.49f),new Vector2(.95f,.88f),Color.white);
        resolutionText=Label("",right.transform,13,new Vector2(.05f,.41f),new Vector2(.95f,.5f),new Color(.58f,.84f,.74f));
        Label("OPCIONES DE INTERVENCIÓN  /  COSTO · TIEMPO · EFECTO",right.transform,12,new Vector2(.05f,.33f),new Vector2(.95f,.41f),new Color(.58f,.78f,.86f));
        interventionButtons=new Button[options.Length];
        for(int i=0;i<options.Length;i++)
        {
            int optionIndex=i;
            float y=.22f-i*.105f;
            interventionButtons[i]=MakeButton(right.transform,"",new Vector2(.05f,y),new Vector2(.95f,y+.09f));
            interventionButtons[i].onClick.AddListener(()=>{ OrbitalAudio.Play("clic_ejecucion"); Deploy(optionIndex); });
        }
        var close=MakeButton(root.transform,"VOLVER A LA NAVE  [ESC]",new Vector2(.72f,.095f),new Vector2(.96f,.145f));
        close.onClick.AddListener(Close);

        completionBanner = Panel("Mission Complete Banner", root.transform, new Color(.015f,.11f,.065f,.98f), new Vector2(.18f,.34f), new Vector2(.82f,.67f));
        var completionTitle = Label("MISIÓN TERMINADA", completionBanner.transform, 34, new Vector2(.04f,.47f), new Vector2(.96f,.88f), new Color(.43f,1f,.63f), TextAnchor.MiddleCenter);
        completionTitle.fontStyle = FontStyle.Bold;
        var completionSubtitle = Label("SECTORES ESTABILIZADOS  ·  REGRESO A LA TIERRA EN CURSO", completionBanner.transform, 16, new Vector2(.06f,.15f), new Vector2(.94f,.45f), Color.white, TextAnchor.MiddleCenter);
        completionSubtitle.fontStyle = FontStyle.Bold;
        completionBanner.SetActive(false);
    }

    private void RefreshAll()
    {
        if (regions == null || detailText == null || resolutionText == null) return;
        var region=regions[selected];
        detailText.text=$"{region.name}\n\nPROBLEMA  /  {region.hazard}\n{region.conditions}\nRIESGO  /  {region.risk}%\nPRIORIDAD  /  {(region.risk>=80?"CRÍTICA":region.risk>=65?"ALTA":"MODERADA")}";
        resolutionText.text=region.resolved?"SECTOR ESTABILIZADO · Intervención completada":"Seleccione una intervención para reducir el riesgo.";
        planetView.SetSelected(selected);
        for (int i=0;i<options.Length;i++)
        {
            InterventionOption option=options[i];
            interventionButtons[i].GetComponentInChildren<Text>().text=$"{option.name}   ·   US$ {option.cost:N0}   ·   {option.durationHours} H   ·   -{option.riskReduction}% RIESGO";
            interventionButtons[i].interactable=!missionComplete&&!region.resolved&&budget>=option.cost;
        }
        RefreshStatus();
    }

    private void RandomizeWeatherAndRisk()
    {
        if (regions == null) return;
        for (int i = 0; i < regions.Length; i++)
        {
            // Keep broad regional climate ranges while varying the displayed scenario each session.
            int temperature = weatherRandom.Next(i == 3 ? 24 : 8, i == 3 ? 43 : 37);
            int humidity = weatherRandom.Next(18, 91);
            int wind = weatherRandom.Next(4, 71);
            int rain = weatherRandom.Next(0, 36);
            regions[i].risk = weatherRandom.Next(48, 96);
            regions[i].conditions = $"Temperatura {temperature} °C · Humedad {humidity}% · Viento {wind} km/h · Lluvia {rain} mm";
        }
    }

    private void RefreshStatus()
    {
        if (!statusText) return;
        lastDisplayedElapsedSecond = Mathf.FloorToInt(realElapsed);
        int remaining=0; foreach(var r in regions) if(!r.resolved) remaining++;
        long dayIndex=simulatedMinutes/(24*60);
        long minutesInDay=simulatedMinutes%(24*60);
        string operational=$"DÍA {dayIndex+1:00}  {minutesInDay/60:00}:{minutesInDay%60:00}";
        string realClock=FormatElapsed(realElapsed);
        if (missionStarted && remaining==0)
        {
            int score=6000+budget/3+Mathf.Max(0,240-(int)(simulatedMinutes/60))*8;
            statusText.text=$"MISIÓN TERMINADA   ·   {operations} INTERVENCIONES";
            clockText.text=$"TIEMPO REAL {realClock}   ·   {operational}   ·   PUNTUACIÓN {score:N0}";
        }
        else
        {
            statusText.text=$"PRESUPUESTO   US$ {budget:N0}    ·    PENDIENTES   {remaining}/6";
            clockText.text=$"TIEMPO REAL {realClock}   ·   TIEMPO OPERATIVO {operational}";
        }
    }

    private static string FormatElapsed(float elapsedSeconds)
    {
        long totalSeconds=Math.Max(0,(long)elapsedSeconds);
        return $"{totalSeconds/3600:00}:{(totalSeconds/60)%60:00}:{totalSeconds%60:00}";
    }

    private void SelectRegion(int index)
    {
        if (missionComplete||index<0||index>=regions.Length) return;
        selected=index;
        OrbitalAudio.Play("seleccionar_region");
        RefreshAll();
    }
    private void Deploy(int optionIndex)
    {
        if(!missionStarted||missionComplete||optionIndex<0||optionIndex>=options.Length)return;
        InterventionOption option=options[optionIndex];
        OrbitalRegion region=regions[selected];
        if(budget<option.cost) { OrbitalAudio.Play("presupuesto_insuficiente"); return; }
        if(region.resolved)return;
        OrbitalAudio.Play("desplegar_intervencion");
        budget-=option.cost;
        operations++;
        simulatedMinutes+=(long)option.durationHours*60;
        region.risk=Mathf.Max(0,region.risk-option.riskReduction);
        region.resolved=region.risk<=38;
        if (region.resolved) OrbitalAudio.Play("confirmacion_intervencion_completada");
        RefreshAll();
        if (AllRegionsResolved()) BeginMissionComplete();
    }

    private bool AllRegionsResolved()
    {
        if (regions == null || regions.Length == 0) return false;
        foreach (var region in regions) if (!region.resolved) return false;
        return true;
    }

    private int CalculateMinimumMissionBudget()
    {
        long total = 0;
        foreach (var region in regions)
        {
            var minimumCost = new long[101];
            for (int i = 0; i < minimumCost.Length; i++) minimumCost[i] = long.MaxValue;
            minimumCost[region.risk] = 0;

            // Each intervention only lowers risk, so a descending pass finds the cheapest
            // combination that reaches the same completion threshold used by Deploy().
            for (int risk = region.risk; risk > 38; risk--)
            {
                if (minimumCost[risk] == long.MaxValue) continue;
                foreach (var option in options)
                {
                    int nextRisk = Mathf.Max(0, risk - option.riskReduction);
                    minimumCost[nextRisk] = Math.Min(minimumCost[nextRisk], minimumCost[risk] + option.cost);
                }
            }

            long regionCost = long.MaxValue;
            for (int risk = 0; risk <= 38; risk++) regionCost = Math.Min(regionCost, minimumCost[risk]);
            total += regionCost;
        }
        return (int)Math.Min(int.MaxValue, total);
    }

    private void BeginMissionComplete()
    {
        if (missionComplete) return;
        missionComplete = true;
        SetVictoryLighting();
        OrbitalAudio.Play("mision_completada");
        if (completionBanner) completionBanner.SetActive(true);
        if (statusText) statusText.color = new Color(.43f,1f,.63f);
        if (clockText) clockText.color = new Color(.78f,1f,.84f);
        for (int i = 0; i < interventionButtons.Length; i++) interventionButtons[i].interactable = false;
        StartCoroutine(LoadReturnSceneAfterBanner());
    }

    private IEnumerator LoadReturnSceneAfterBanner()
    {
        yield return new WaitForSecondsRealtime(3.2f);
        if (Application.CanStreamedLevelBeLoaded("ReturnToEarth"))
            SceneManager.LoadScene("ReturnToEarth");
        else
            Debug.LogError("No se encontró la escena ReturnToEarth en la lista de escenas del proyecto.", this);
    }

    private static void SetVictoryLighting()
    {
        Color victoryGreen = new Color(.22f,1f,.43f);
        foreach (var light in FindObjectsByType<Light>(FindObjectsSortMode.None))
        {
            if (light.type != LightType.Spot) continue;
            light.color = victoryGreen;
        }

        foreach (var renderer in FindObjectsByType<Renderer>(FindObjectsSortMode.None))
        {
            string objectName = renderer.gameObject.name;
            bool colorPrimaryMaterial =
                objectName.StartsWith("Ceiling light", StringComparison.OrdinalIgnoreCase) ||
                objectName.StartsWith("Console display", StringComparison.OrdinalIgnoreCase) ||
                objectName.StartsWith("Control key", StringComparison.OrdinalIgnoreCase);
            Material[] sharedMaterials = renderer.sharedMaterials;
            bool hasSignalMaterial = false;
            for (int i = 0; i < sharedMaterials.Length; i++)
            {
                if (sharedMaterials[i] && sharedMaterials[i].name.StartsWith("Signal_cyan", StringComparison.OrdinalIgnoreCase))
                {
                    hasSignalMaterial = true;
                    break;
                }
            }

            if (!colorPrimaryMaterial && !hasSignalMaterial) continue;

            Material[] materials = renderer.materials;
            for (int i = 0; i < materials.Length; i++)
            {
                bool isSignalMaterial = i < sharedMaterials.Length && sharedMaterials[i] &&
                    sharedMaterials[i].name.StartsWith("Signal_cyan", StringComparison.OrdinalIgnoreCase);
                if (isSignalMaterial || (colorPrimaryMaterial && i == 0))
                    SetVictoryMaterialColor(materials[i], victoryGreen);
            }
        }
    }

    private static void SetVictoryMaterialColor(Material material, Color color)
    {
        material.color = color;
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
        if (material.HasProperty("_Color")) material.SetColor("_Color", color);
        if (material.HasProperty("_EmissionColor"))
        {
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", color * 2.5f);
        }
    }

    private void BuildPlanet(RawImage rawImage)
    {
        int planetLayer=31;
        planetRoot=new GameObject("Planet Model Root").transform;
        planetRoot.gameObject.layer=planetLayer;
        planetTexture=new RenderTexture(768,576,16,RenderTextureFormat.ARGB32){name="ORBITAL Planet View"};
        planetTexture.Create();
        if (SystemInfo.graphicsUVStartsAtTop) rawImage.uvRect=new Rect(0f,1f,1f,-1f);
        var cameraObject=new GameObject("Planet View Camera");
        cameraObject.transform.position=new Vector3(0,0,-5f);
        planetCamera=cameraObject.AddComponent<Camera>();
        planetCamera.clearFlags=CameraClearFlags.SolidColor;
        planetCamera.backgroundColor=new Color(.006f,.018f,.035f);
        planetCamera.fieldOfView=36;
        planetCamera.nearClipPlane=.1f;
        planetCamera.farClipPlane=20f;
        planetCamera.cullingMask=1<<planetLayer;
        planetCamera.targetTexture=planetTexture;
        planetCamera.enabled=false;
        rawImage.texture=planetTexture;

        Shader shader=Shader.Find("Universal Render Pipeline/Unlit");
        if(!shader)shader=Shader.Find("Unlit/Color");
        if(!shader)shader=Shader.Find("Sprites/Default");
        if(!shader)shader=Shader.Find("Standard");
        var ocean=new Material(shader){name="ORBITAL ocean"};ocean.color=new Color(.035f,.24f,.48f);
        var globe=GameObject.CreatePrimitive(PrimitiveType.Sphere);globe.name="Interactive Earth";globe.transform.SetParent(planetRoot,false);globe.transform.localScale=Vector3.one*2.5f;globe.layer=planetLayer;
        globe.GetComponent<Renderer>().sharedMaterial=ocean;

        var landMaterial=new Material(shader){name="ORBITAL land"};landMaterial.color=new Color(.19f,.5f,.32f);
        var landPositions=new[]{
            new Vector3(-.44f,.39f,-.79f),new Vector3(-.27f,-.27f,-.92f),new Vector3(.18f,.49f,-.85f),
            new Vector3(.25f,-.23f,-.94f),new Vector3(.71f,.28f,-.65f),new Vector3(.78f,-.43f,-.45f)
        };
        for(int i=0;i<landPositions.Length;i++)
        {
            var patch=GameObject.CreatePrimitive(PrimitiveType.Sphere);patch.name="Land mass";patch.transform.SetParent(planetRoot,false);patch.layer=planetLayer;
            patch.transform.localPosition=landPositions[i].normalized*1.255f;
            patch.transform.localRotation=Quaternion.FromToRotation(Vector3.forward,landPositions[i].normalized);
            patch.transform.localScale=new Vector3(.62f,.34f,.06f);
            patch.GetComponent<Renderer>().sharedMaterial=landMaterial;
            var patchCollider=patch.GetComponent<Collider>();if(patchCollider)Destroy(patchCollider);
        }
        var markerMaterial=new Material(shader){name="ORBITAL region marker"};
        planetMarkers=new Transform[regions.Length];
        var markerDirections=new[]{
            new Vector3(-.44f,.39f,-.79f),new Vector3(-.27f,-.27f,-.92f),new Vector3(.18f,.49f,-.85f),
            new Vector3(.25f,-.23f,-.94f),new Vector3(.71f,.28f,-.65f),new Vector3(.78f,-.43f,-.45f)
        };
        for(int i=0;i<planetMarkers.Length;i++)
        {
            var marker=GameObject.CreatePrimitive(PrimitiveType.Sphere);marker.name=regions[i].name+" marker";marker.transform.SetParent(planetRoot,false);marker.layer=planetLayer;
            marker.transform.localPosition=markerDirections[i].normalized*1.31f;marker.transform.localScale=Vector3.one*.12f;
            marker.GetComponent<Renderer>().sharedMaterial=markerMaterial;
            var markerCollider=marker.GetComponent<Collider>();if(markerCollider)Destroy(markerCollider);
            planetMarkers[i]=marker.transform;
        }
        planetView.Initialize(planetCamera,planetRoot,planetMarkers,SelectRegion);
        planetView.SetSelected(selected);
    }

    private static Font FontAsset()
    {
        if(uiFont)return uiFont;
        uiFont=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        return uiFont;
    }
    private GameObject Panel(string name,Transform parent,Color color,Vector2 anchorMin,Vector2 anchorMax)
    {
        var go=new GameObject(name,typeof(RectTransform),typeof(Image)); go.transform.SetParent(parent,false);
        var rt=go.GetComponent<RectTransform>();rt.anchorMin=anchorMin;rt.anchorMax=anchorMax;rt.offsetMin=rt.offsetMax=Vector2.zero;
        go.GetComponent<Image>().color=color;return go;
    }
    private Text Label(string value,Transform parent,int size,Vector2 min,Vector2 max,Color color,TextAnchor alignment=TextAnchor.UpperLeft)
    {
        var go=new GameObject("Text",typeof(RectTransform),typeof(Text));go.transform.SetParent(parent,false);
        var rt=go.GetComponent<RectTransform>();rt.anchorMin=min;rt.anchorMax=max;rt.offsetMin=rt.offsetMax=new Vector2(10,5);
        var text=go.GetComponent<Text>();text.font=FontAsset();text.fontSize=size;text.color=color;text.alignment=alignment;text.horizontalOverflow=HorizontalWrapMode.Wrap;text.verticalOverflow=VerticalWrapMode.Overflow;text.text=value;return text;
    }
    private Button MakeButton(Transform parent,string value,Vector2 min,Vector2 max)
    {
        var go=new GameObject("Button",typeof(RectTransform),typeof(Image),typeof(Button));go.transform.SetParent(parent,false);
        var rt=go.GetComponent<RectTransform>();rt.anchorMin=min;rt.anchorMax=max;rt.offsetMin=rt.offsetMax=Vector2.zero;
        var image=go.GetComponent<Image>();image.color=new Color(.06f,.17f,.24f);
        var button=go.GetComponent<Button>();button.targetGraphic=image;
        go.AddComponent<OrbitalButtonHoverSound>();
        ColorBlock colors=button.colors;
        colors.normalColor=new Color(.06f,.17f,.24f);
        colors.highlightedColor=new Color(.025f,.11f,.17f);
        colors.pressedColor=new Color(.01f,.05f,.085f);
        colors.selectedColor=new Color(.08f,.3f,.4f);
        colors.disabledColor=new Color(.12f,.14f,.16f,.75f);
        colors.colorMultiplier=1f;colors.fadeDuration=.08f;button.colors=colors;
        var label=Label(value,go.transform,14,new Vector2(0,0),new Vector2(1,1),Color.white,TextAnchor.MiddleLeft);label.fontStyle=FontStyle.Bold;
        return button;
    }
}
