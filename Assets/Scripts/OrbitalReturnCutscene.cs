using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class OrbitalReturnCutscene : MonoBehaviour
{
    public Transform spacecraft;
    public Transform earth;
    public float durationSeconds = 12f;
    public Vector3 launchPosition = new Vector3(10f, -1f, -12f);
    public Vector3 firstControlPoint = new Vector3(4f, 1f, 15f);
    public Vector3 secondControlPoint = new Vector3(-9f, -2f, 42f);
    public Vector3 arrivalPosition = new Vector3(0f, 0f, 57f);

    private Text titleText;
    private Text detailText;
    private Button actionButton;
    private float elapsed;
    private bool sequenceComplete;

    private void Awake()
    {
        BuildInterface();
    }

    private void Start()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        UpdateFlightPath(0f);
    }

    private void Update()
    {
        if (earth) earth.Rotate(Vector3.up, 4f * Time.unscaledDeltaTime, Space.Self);

        if (sequenceComplete) return;
        elapsed += Time.unscaledDeltaTime;
        float progress = Mathf.Clamp01(elapsed / Mathf.Max(.1f, durationSeconds));
        UpdateFlightPath(progress);
        if (progress >= 1f) CompleteSequence();
    }

    private void UpdateFlightPath(float progress)
    {
        if (!spacecraft) return;
        float smoothProgress = Mathf.SmoothStep(0f, 1f, progress);
        Vector3 oneMinus = Vector3.one * (1f - smoothProgress);
        Vector3 p = launchPosition * oneMinus.x * oneMinus.x * oneMinus.x
                  + firstControlPoint * (3f * oneMinus.x * oneMinus.x * smoothProgress)
                  + secondControlPoint * (3f * oneMinus.x * smoothProgress * smoothProgress)
                  + arrivalPosition * (smoothProgress * smoothProgress * smoothProgress);
        spacecraft.position = p;

        Vector3 tangent = 3f * oneMinus.x * oneMinus.x * (firstControlPoint - launchPosition)
                        + 6f * oneMinus.x * smoothProgress * (secondControlPoint - firstControlPoint)
                        + 3f * smoothProgress * smoothProgress * (arrivalPosition - secondControlPoint);
        if (tangent.sqrMagnitude > .001f)
        {
            float bank = Mathf.Sin(smoothProgress * Mathf.PI * 3f) * 2.2f;
            spacecraft.rotation = Quaternion.LookRotation(tangent.normalized, Vector3.up) * Quaternion.Euler(0f, 0f, bank);
        }
    }

    private void CompleteSequence()
    {
        if (sequenceComplete) return;
        sequenceComplete = true;
        UpdateFlightPath(1f);
        titleText.text = "REGRESO COMPLETADO";
        detailText.text = "NAVE EN ÓRBITA TERRESTRE  ·  OPERACIÓN FINALIZADA";
        SetButtonText("VOLVER AL MENÚ");
    }

    private void HandleAction()
    {
        if (!sequenceComplete)
        {
            CompleteSequence();
            return;
        }

        if (Application.CanStreamedLevelBeLoaded("MainMenu"))
            SceneManager.LoadScene("MainMenu");
        else
            Debug.LogError("No se encontró la escena MainMenu en la lista de escenas del proyecto.", this);
    }

    private void BuildInterface()
    {
        if (!FindFirstObjectByType<EventSystem>())
            new GameObject("Return Scene Event System", typeof(EventSystem), typeof(InputSystemUIInputModule));

        var canvasObject = new GameObject("Return Scene Interface", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 30;

        var scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1600f, 900f);
        scaler.matchWidthOrHeight = .5f;

        var panelObject = new GameObject("Mission Return Status", typeof(RectTransform), typeof(Image));
        panelObject.transform.SetParent(canvas.transform, false);
        var panelRect = panelObject.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(.035f, .82f);
        panelRect.anchorMax = new Vector2(.63f, .965f);
        panelRect.offsetMin = panelRect.offsetMax = Vector2.zero;
        panelObject.GetComponent<Image>().color = new Color(.015f, .055f, .085f, .88f);

        titleText = CreateLabel(panelObject.transform, "REGRESO A LA TIERRA", 28, new Vector2(.035f, .48f), new Vector2(.965f, .94f), new Color(.52f, .91f, 1f), TextAnchor.MiddleLeft);
        titleText.fontStyle = FontStyle.Bold;
        detailText = CreateLabel(panelObject.transform, "MISIÓN TERMINADA  ·  NAVE EN RUTA DE RETORNO", 15, new Vector2(.035f, .08f), new Vector2(.965f, .51f), Color.white, TextAnchor.MiddleLeft);

        actionButton = CreateButton(canvas.transform, "OMITIR SECUENCIA", new Vector2(.76f, .055f), new Vector2(.965f, .125f));
        actionButton.onClick.AddListener(HandleAction);
    }

    private static Text CreateLabel(Transform parent, string value, int size, Vector2 min, Vector2 max, Color color, TextAnchor alignment)
    {
        var labelObject = new GameObject("Text", typeof(RectTransform), typeof(Text));
        labelObject.transform.SetParent(parent, false);
        var rect = labelObject.GetComponent<RectTransform>();
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.offsetMin = rect.offsetMax = new Vector2(8f, 4f);
        var text = labelObject.GetComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = size;
        text.color = color;
        text.alignment = alignment;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.text = value;
        return text;
    }

    private static Button CreateButton(Transform parent, string value, Vector2 min, Vector2 max)
    {
        var buttonObject = new GameObject("Return Scene Button", typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);
        var rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.offsetMin = rect.offsetMax = Vector2.zero;

        var image = buttonObject.GetComponent<Image>();
        image.color = new Color(.035f, .21f, .27f, .96f);
        var button = buttonObject.GetComponent<Button>();
        button.targetGraphic = image;
        var colors = button.colors;
        colors.normalColor = new Color(.035f, .21f, .27f);
        colors.highlightedColor = new Color(.06f, .38f, .42f);
        colors.pressedColor = new Color(.015f, .11f, .16f);
        colors.colorMultiplier = 1f;
        button.colors = colors;

        var text = CreateLabel(buttonObject.transform, value, 15, Vector2.zero, Vector2.one, Color.white, TextAnchor.MiddleCenter);
        text.fontStyle = FontStyle.Bold;
        return button;
    }

    private void SetButtonText(string value)
    {
        if (!actionButton) return;
        var label = actionButton.GetComponentInChildren<Text>();
        if (label) label.text = value;
    }
}
