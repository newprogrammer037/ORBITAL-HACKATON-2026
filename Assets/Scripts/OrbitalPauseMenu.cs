using UnityEngine;
using UnityEngine.SceneManagement;

public class OrbitalPauseMenu : MonoBehaviour
{
    public static bool IsPaused { get; private set; }
    private GUIStyle titleStyle;
    private GUIStyle buttonStyle;

    private void Awake() { IsPaused = false; }

    public void Toggle() { SetPaused(!IsPaused); }

    private void SetPaused(bool paused)
    {
        IsPaused = paused;
        OrbitalPlayerController.SetCursor(!paused);
    }

    private void OnGUI()
    {
        if (!IsPaused) return;
        EnsureStyles();
        GUI.color = new Color(.005f,.014f,.028f,.94f);
        GUI.DrawTexture(new Rect(0,0,Screen.width,Screen.height), Texture2D.whiteTexture);
        GUI.color = Color.white;

        float center = Screen.width * .5f;
        GUI.Label(new Rect(center-260, Screen.height*.24f, 520, 72), "JUEGO EN PAUSA", titleStyle);
        float width = Mathf.Min(420, Screen.width*.72f);
        float x = center-width*.5f;
        float h = Mathf.Clamp(Screen.height*.075f, 48f, 68f);
        float gap = h*.22f;
        float y = Screen.height*.42f;
        if (GUI.Button(new Rect(x,y,width,h), "CONTINUAR", buttonStyle)) { OrbitalAudio.Play("clic_confirmacion"); SetPaused(false); }
        if (GUI.Button(new Rect(x,y+h+gap,width,h), "REINICIAR PARTIDA", buttonStyle)) { OrbitalAudio.Play("iniciar_transicion"); LoadScene("Spaceship"); }
        if (GUI.Button(new Rect(x,y+(h+gap)*2,width,h), "VOLVER AL MENÚ PRINCIPAL", buttonStyle)) { OrbitalAudio.Play("cerrar_panel_opciones"); LoadScene("MainMenu"); }
    }

    private void EnsureStyles()
    {
        if (titleStyle != null) return;
        titleStyle = new GUIStyle(GUI.skin.label) { alignment=TextAnchor.MiddleCenter, fontSize=Mathf.Clamp(Screen.height/24,24,40), fontStyle=FontStyle.Bold };
        titleStyle.normal.textColor = new Color(.48f,.88f,1f);
        buttonStyle = new GUIStyle(GUI.skin.button) { alignment=TextAnchor.MiddleCenter, fontSize=Mathf.Clamp(Screen.height/48,16,24), fontStyle=FontStyle.Bold };
        buttonStyle.normal.textColor = Color.white;
    }

    private static void LoadScene(string scene)
    {
        IsPaused = false;
        SceneManager.LoadScene(scene);
    }
}
