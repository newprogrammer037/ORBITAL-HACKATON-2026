using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class OrbitalMainMenu : MonoBehaviour
{
    private Font font;
    private Transform canvasRoot;
    private Camera menuCamera;
    private GameObject optionsPopup;
    private GameObject creditsPopup;
    private Text volumeValue;
    private Text fovValue;
    private Slider volumeSlider;
    private Slider fovSlider;
    private float nextOptionSound;

    private void Awake()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        menuCamera = Camera.main;
        AudioListener.volume = PlayerPrefs.GetFloat("ORBITAL_VOLUME", 1f);
        if (menuCamera) menuCamera.fieldOfView = PlayerPrefs.GetFloat("ORBITAL_FOV", 76f);
        if (!FindFirstObjectByType<EventSystem>())
            new GameObject("UI Event System", typeof(EventSystem), typeof(InputSystemUIInputModule));
        BuildUI();
    }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (optionsPopup) optionsPopup.SetActive(false);
            if (creditsPopup) creditsPopup.SetActive(false);
        }
    }

    private void BuildUI()
    {
        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        var canvasObject = new GameObject("Main Menu UI", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = canvasObject.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 10;
        canvasRoot = canvas.transform;
        var scaler = canvasObject.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(1600, 900);

        var backdrop = new GameObject("Backdrop", typeof(RectTransform), typeof(Image)); backdrop.transform.SetParent(canvas.transform, false);
        var bgRect = backdrop.GetComponent<RectTransform>(); bgRect.anchorMin = Vector2.zero; bgRect.anchorMax = Vector2.one; bgRect.offsetMin = bgRect.offsetMax = Vector2.zero;
        backdrop.GetComponent<Image>().color = new Color(.008f, .022f, .045f, .88f);

        AddText(canvas.transform, "ORBITAL", 72, new Vector2(.09f,.59f), new Vector2(.64f,.82f), new Color(.53f,.9f,1), TextAnchor.MiddleLeft, FontStyle.Bold);
        AddText(canvas.transform, "CENTRO DE OPERACIONES PLANETARIAS", 22, new Vector2(.10f,.52f), new Vector2(.68f,.61f), Color.white, TextAnchor.MiddleLeft, FontStyle.Bold);
        AddText(canvas.transform, "Una estación. Seis regiones. Recursos limitados.\nProtege el planeta desde la órbita.", 18, new Vector2(.10f,.39f), new Vector2(.63f,.51f), new Color(.65f,.78f,.87f), TextAnchor.UpperLeft, FontStyle.Normal);

        Button("INICIAR MISIÓN", new Vector2(.10f,.27f), new Vector2(.39f,.36f), () => SceneManager.LoadScene("Spaceship"));
        Button("OPCIONES", new Vector2(.10f,.16f), new Vector2(.245f,.235f), OpenOptions);
        Button("CRÉDITOS", new Vector2(.255f,.16f), new Vector2(.39f,.235f), OpenCredits);
        AddText(canvas.transform, "CENTRO DE OPERACIONES PLANETARIAS", 13, new Vector2(.10f,.075f), new Vector2(.70f,.13f), new Color(.48f,.68f,.78f), TextAnchor.MiddleLeft, FontStyle.Normal);
        AddText(canvas.transform, "01 / 03     SYSTEMS NOMINAL", 14, new Vector2(.7f,.1f), new Vector2(.91f,.16f), new Color(.38f,.84f,.9f), TextAnchor.MiddleRight, FontStyle.Bold);
        BuildOptionsPopup();
        BuildCreditsPopup();
    }

    private void OpenOptions()
    {
        OrbitalAudio.Play("abrir_panel_opciones");
        creditsPopup.SetActive(false);
        optionsPopup.SetActive(true);
    }

    private void OpenCredits()
    {
        OrbitalAudio.Play("abrir_panel_opciones");
        optionsPopup.SetActive(false);
        creditsPopup.SetActive(true);
    }

    private void BuildOptionsPopup()
    {
        Transform panel;
        optionsPopup = CreatePopup("Options Popup", "OPCIONES", out panel);
        AddText(panel, "VOLUMEN", 16, new Vector2(.1f,.68f), new Vector2(.9f,.8f), Color.white, TextAnchor.MiddleLeft, FontStyle.Bold);
        volumeSlider = CreateSlider(panel, new Vector2(.1f,.57f), new Vector2(.73f,.66f), 0f, 1f, PlayerPrefs.GetFloat("ORBITAL_VOLUME", 1f));
        volumeValue = AddText(panel, "", 15, new Vector2(.75f,.57f), new Vector2(.91f,.66f), new Color(.48f,.88f,1), TextAnchor.MiddleRight, FontStyle.Bold);
        volumeSlider.onValueChanged.AddListener(SetVolume);

        AddText(panel, "CAMPO DE VISIÓN", 16, new Vector2(.1f,.4f), new Vector2(.9f,.52f), Color.white, TextAnchor.MiddleLeft, FontStyle.Bold);
        fovSlider = CreateSlider(panel, new Vector2(.1f,.29f), new Vector2(.73f,.38f), 60f, 100f, PlayerPrefs.GetFloat("ORBITAL_FOV", 76f));
        fovValue = AddText(panel, "", 15, new Vector2(.75f,.29f), new Vector2(.91f,.38f), new Color(.48f,.88f,1), TextAnchor.MiddleRight, FontStyle.Bold);
        fovSlider.onValueChanged.AddListener(SetFieldOfView);
        SetVolume(volumeSlider.value);
        SetFieldOfView(fovSlider.value);
        Button("CERRAR  [ESC]", new Vector2(.32f,.1f), new Vector2(.68f,.21f), () => optionsPopup.SetActive(false), panel);
        optionsPopup.SetActive(false);
    }

    private void BuildCreditsPopup()
    {
        Transform panel;
        creditsPopup = CreatePopup("Credits Popup", "CRÉDITOS", out panel);
        AddText(panel, "Equipo:\nIntercultural-1ros\n\nModelos:\n\"Screens Low Poly/Pantallas\" (https://skfb.ly/oW9JO) by JackSpector_Ink is licensed under Creative Commons Attribution (http://creativecommons.org/licenses/by/4.0/).\n\n\"VINTAGE KEYBOARD\" (https://skfb.ly/oyDpV) by Diego Clemente is licensed under Creative Commons Attribution (http://creativecommons.org/licenses/by/4.0/).\n\nHerramientas:\nCodex", 15, new Vector2(.07f,.25f), new Vector2(.93f,.82f), Color.white, TextAnchor.UpperLeft, FontStyle.Normal);
        Button("CERRAR  [ESC]", new Vector2(.32f,.1f), new Vector2(.68f,.21f), () => creditsPopup.SetActive(false), panel);
        creditsPopup.SetActive(false);
    }

    private GameObject CreatePopup(string name, string title, out Transform panel)
    {
        var overlay = new GameObject(name, typeof(RectTransform), typeof(Image)); overlay.transform.SetParent(canvasRoot, false);
        var overlayRect = overlay.GetComponent<RectTransform>(); overlayRect.anchorMin = Vector2.zero; overlayRect.anchorMax = Vector2.one; overlayRect.offsetMin = overlayRect.offsetMax = Vector2.zero;
        overlay.GetComponent<Image>().color = new Color(.005f,.015f,.03f,.78f);
        var panelObject = new GameObject("Dialog", typeof(RectTransform), typeof(Image)); panelObject.transform.SetParent(overlay.transform, false);
        var panelRect = panelObject.GetComponent<RectTransform>(); panelRect.anchorMin = new Vector2(.12f,.08f); panelRect.anchorMax = new Vector2(.88f,.92f); panelRect.offsetMin = panelRect.offsetMax = Vector2.zero;
        panelObject.GetComponent<Image>().color = new Color(.025f,.075f,.11f,.99f);
        panel = panelObject.transform;
        AddText(panel, title, 28, new Vector2(.08f,.83f), new Vector2(.92f,.97f), new Color(.53f,.9f,1), TextAnchor.MiddleCenter, FontStyle.Bold);
        return overlay;
    }

    private Slider CreateSlider(Transform parent, Vector2 min, Vector2 max, float minValue, float maxValue, float value)
    {
        var root = new GameObject("Slider", typeof(RectTransform), typeof(Slider)); root.transform.SetParent(parent, false);
        var rootRect = root.GetComponent<RectTransform>(); rootRect.anchorMin = min; rootRect.anchorMax = max; rootRect.offsetMin = rootRect.offsetMax = Vector2.zero;

        var track = new GameObject("Track", typeof(RectTransform), typeof(Image)); track.transform.SetParent(root.transform, false);
        var trackRect = track.GetComponent<RectTransform>(); trackRect.anchorMin = new Vector2(0,.35f); trackRect.anchorMax = new Vector2(1,.65f); trackRect.offsetMin = trackRect.offsetMax = Vector2.zero;
        track.GetComponent<Image>().color = new Color(.08f,.15f,.19f);

        var fill = new GameObject("Fill", typeof(RectTransform), typeof(Image)); fill.transform.SetParent(track.transform, false);
        var fillRect = fill.GetComponent<RectTransform>(); fillRect.anchorMin = Vector2.zero; fillRect.anchorMax = Vector2.one; fillRect.offsetMin = fillRect.offsetMax = Vector2.zero;
        fill.GetComponent<Image>().color = new Color(.08f,.62f,.82f);

        var handle = new GameObject("Handle", typeof(RectTransform), typeof(Image)); handle.transform.SetParent(root.transform, false);
        var handleRect = handle.GetComponent<RectTransform>(); handleRect.anchorMin = new Vector2(0,.05f); handleRect.anchorMax = new Vector2(0,.95f); handleRect.sizeDelta = new Vector2(18,0);
        var handleImage = handle.GetComponent<Image>(); handleImage.color = new Color(.55f,.92f,1f);

        var slider = root.GetComponent<Slider>(); slider.direction = Slider.Direction.LeftToRight; slider.minValue = minValue; slider.maxValue = maxValue;
        slider.fillRect = fillRect; slider.handleRect = handleRect; slider.targetGraphic = handleImage; slider.SetValueWithoutNotify(value);
        return slider;
    }

    private void SetVolume(float value)
    {
        PlayOptionSound();
        AudioListener.volume = value;
        PlayerPrefs.SetFloat("ORBITAL_VOLUME", value);
        if (volumeValue) volumeValue.text = $"{Mathf.RoundToInt(value * 100f)}%";
    }

    private void SetFieldOfView(float value)
    {
        PlayOptionSound();
        if (menuCamera) menuCamera.fieldOfView = value;
        PlayerPrefs.SetFloat("ORBITAL_FOV", value);
        if (fovValue) fovValue.text = $"{Mathf.RoundToInt(value)}°";
    }

    private void PlayOptionSound()
    {
        if (Time.unscaledTime < nextOptionSound) return;
        nextOptionSound = Time.unscaledTime + .12f;
        OrbitalAudio.Play("ajustar_parametro_1s", .55f);
    }

    private Button Button(string label, Vector2 min, Vector2 max, UnityEngine.Events.UnityAction action, Transform parent = null)
    {
        var go = new GameObject(label, typeof(RectTransform), typeof(Image), typeof(Button)); go.transform.SetParent(parent ? parent : canvasRoot, false);
        var rect = go.GetComponent<RectTransform>(); rect.anchorMin = min; rect.anchorMax = max; rect.offsetMin = rect.offsetMax = Vector2.zero;
        var image = go.GetComponent<Image>(); image.color = new Color(.035f,.16f,.23f,.95f);
        var button = go.GetComponent<Button>(); button.targetGraphic = image; button.onClick.AddListener(() => { OrbitalAudio.Play("clic_seleccion"); action?.Invoke(); });
        go.AddComponent<OrbitalButtonHoverSound>();
        ApplyButtonFeedback(button, new Color(.035f,.16f,.23f), new Color(.018f,.105f,.16f), new Color(.008f,.045f,.08f));
        var child = new GameObject("Label", typeof(RectTransform), typeof(Text)); child.transform.SetParent(go.transform,false);
        var childRect=child.GetComponent<RectTransform>();childRect.anchorMin=Vector2.zero;childRect.anchorMax=Vector2.one;childRect.offsetMin=childRect.offsetMax=new Vector2(10,4);
        var text=child.GetComponent<Text>();text.font=font;text.fontSize=18;text.fontStyle=FontStyle.Bold;text.alignment=TextAnchor.MiddleCenter;text.color=Color.white;text.text=label;
        return button;
    }

    private static void ApplyButtonFeedback(Button button, Color normal, Color highlighted, Color pressed)
    {
        ColorBlock colors = button.colors;
        colors.normalColor = normal;
        colors.highlightedColor = highlighted;
        colors.pressedColor = pressed;
        colors.selectedColor = new Color(.08f,.3f,.4f);
        colors.disabledColor = new Color(.12f,.14f,.16f,.75f);
        colors.colorMultiplier = 1f;
        colors.fadeDuration = .08f;
        button.colors = colors;
        button.transition = Selectable.Transition.ColorTint;
    }

    private Text AddText(Transform parent,string value,int size,Vector2 min,Vector2 max,Color color,TextAnchor alignment,FontStyle style)
    {
        var go=new GameObject("Text",typeof(RectTransform),typeof(Text));go.transform.SetParent(parent,false);
        var rect=go.GetComponent<RectTransform>();rect.anchorMin=min;rect.anchorMax=max;rect.offsetMin=rect.offsetMax=new Vector2(8,4);
        var text=go.GetComponent<Text>();text.font=font;text.fontSize=size;text.fontStyle=style;text.alignment=alignment;text.color=color;text.horizontalOverflow=HorizontalWrapMode.Wrap;text.verticalOverflow=VerticalWrapMode.Overflow;text.text=value;return text;
    }
}
