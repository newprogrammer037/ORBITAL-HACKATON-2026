using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class OrbitalPlayerController : MonoBehaviour
{
    [SerializeField] private Camera playerCamera;
    [SerializeField] private float walkSpeed = 4f;
    [SerializeField] private float sprintSpeed = 6.5f;
    [SerializeField] private float lookSensitivity = 0.12f;
    [SerializeField] private float interactionDistance = 3.2f;
    private CharacterController controller;
    private float pitch;
    private OrbitalMissionConsole activeConsole;
    private OrbitalPauseMenu pauseMenu;
    private float footstepTimer;
    private GUIStyle hintStyle;
    private GUIStyle footerStyle;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        pauseMenu = GetComponent<OrbitalPauseMenu>();
        if (!pauseMenu) pauseMenu = gameObject.AddComponent<OrbitalPauseMenu>();
        if (!playerCamera) playerCamera = GetComponentInChildren<Camera>();
        AudioListener.volume = PlayerPrefs.GetFloat("ORBITAL_VOLUME", 1f);
        if (playerCamera) playerCamera.fieldOfView = PlayerPrefs.GetFloat("ORBITAL_FOV", playerCamera.fieldOfView);
        SetCursor(true);
    }

    private void Update()
    {
        bool uiOpen = OrbitalMissionConsole.UIOpen;
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (uiOpen) OrbitalMissionConsole.CloseUI();
            else if (pauseMenu) pauseMenu.Toggle();
        }

        if (OrbitalPauseMenu.IsPaused) return;

        if (!uiOpen)
        {
            Look();
            Move();
            UpdateFootsteps();
            ProbeConsole();
            if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame && activeConsole != null)
                activeConsole.Open();
        }
        else activeConsole = null;
    }

    private void UpdateFootsteps()
    {
        if (!controller.isGrounded || controller.velocity.sqrMagnitude < .5f) { footstepTimer = 0f; return; }
        footstepTimer += Time.deltaTime;
        if (footstepTimer < (Keyboard.current != null && Keyboard.current.leftShiftKey.isPressed ? .34f : .48f)) return;
        footstepTimer = 0f;
        OrbitalAudio.Play("pasos_nave_espacial", .42f);
    }

    private void Look()
    {
        if (Mouse.current == null || Cursor.lockState != CursorLockMode.Locked) return;
        Vector2 delta = Mouse.current.delta.ReadValue() * lookSensitivity;
        transform.Rotate(Vector3.up * delta.x);
        pitch = Mathf.Clamp(pitch - delta.y, -78f, 78f);
        if (playerCamera) playerCamera.transform.localRotation = Quaternion.Euler(pitch, 0, 0);
    }

    private void Move()
    {
        if (Keyboard.current == null) return;
        float x = (Keyboard.current.dKey.isPressed ? 1 : 0) - (Keyboard.current.aKey.isPressed ? 1 : 0);
        float z = (Keyboard.current.wKey.isPressed ? 1 : 0) - (Keyboard.current.sKey.isPressed ? 1 : 0);
        Vector3 direction = Vector3.ClampMagnitude(transform.right * x + transform.forward * z, 1f);
        float speed = Keyboard.current.leftShiftKey.isPressed ? sprintSpeed : walkSpeed;
        Vector3 velocity = direction * speed;
        if (controller.isGrounded) velocity.y = -1f;
        else velocity.y = -9.81f;
        controller.Move(velocity * Time.deltaTime);
    }

    private void ProbeConsole()
    {
        activeConsole = null;
        if (!playerCamera) return;
        if (Physics.Raycast(playerCamera.transform.position, playerCamera.transform.forward, out RaycastHit hit, interactionDistance))
            activeConsole = hit.collider.GetComponentInParent<OrbitalMissionConsole>();
    }

    private void OnGUI()
    {
        if (OrbitalMissionConsole.UIOpen || OrbitalPauseMenu.IsPaused) return;
        if (pauseMenu && GUI.Button(new Rect(Screen.width - 150, 18, 130, 36), "PAUSA  [ESC]"))
            pauseMenu.Toggle();
        float cx = Screen.width * .5f, cy = Screen.height * .5f;
        GUI.color = new Color(.5f, .9f, 1f, .9f);
        GUI.DrawTexture(new Rect(cx - 1, cy - 8, 2, 16), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(cx - 8, cy - 1, 16, 2), Texture2D.whiteTexture);
        GUI.color = Color.white;
        EnsureStyles();
        string hint = activeConsole ? "[ E ]  ABRIR CENTRO DE COMANDO" : "WASD MOVER   ·   MOUSE MIRAR   ·   SHIFT CORRER   ·   ACÉRCATE A LA CONSOLA";
        GUI.Label(new Rect(0, Screen.height - 62, Screen.width, 30), hint, hintStyle);
        GUI.Label(new Rect(0, Screen.height - 34, Screen.width, 22), "ORBITAL  /  ESTACIÓN DE OBSERVACIÓN", footerStyle);
    }

    private void EnsureStyles()
    {
        if (hintStyle != null) return;

        hintStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 15,
            fontStyle = FontStyle.Bold
        };
        hintStyle.normal.textColor = Color.white;

        footerStyle = new GUIStyle(hintStyle) { fontSize = 12 };
        footerStyle.normal.textColor = new Color(.45f, .78f, .9f);
    }

    public static void SetCursor(bool locked)
    {
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
    }
}
