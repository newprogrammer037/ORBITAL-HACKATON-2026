using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class OrbitalAudio : MonoBehaviour
{
    private static OrbitalAudio instance;
    private readonly Dictionary<string, AudioClip> clips = new Dictionary<string, AudioClip>();
    private AudioSource effects;
    private AudioSource ambience;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void CreateInstance()
    {
        if (instance) return;
        var go = new GameObject("ORBITAL Audio");
        instance = go.AddComponent<OrbitalAudio>();
        DontDestroyOnLoad(go);
    }

    private void Awake()
    {
        if (instance && instance != this) { Destroy(gameObject); return; }
        instance = this;
        effects = gameObject.AddComponent<AudioSource>();
        effects.playOnAwake = false;
        effects.loop = false;
        ambience = gameObject.AddComponent<AudioSource>();
        ambience.playOnAwake = false;
        ambience.loop = true;
        SceneManager.sceneLoaded += OnSceneLoaded;
        OnSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
    }

    private void OnDestroy()
    {
        if (instance != this) return;
        SceneManager.sceneLoaded -= OnSceneLoaded;
        instance = null;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        string key = scene.name == "Spaceship" ? "ambiente_nave_realista_2min" : null;
        if (string.IsNullOrEmpty(key)) { ambience.Stop(); ambience.clip = null; return; }
        AudioClip clip = GetClip(key);
        if (!clip) return;
        ambience.clip = clip;
        ambience.Play();
    }

    public static void Play(string key, float volume = 1f)
    {
        if (!instance || string.IsNullOrEmpty(key)) return;
        AudioClip clip = instance.GetClip(key);
        if (clip) instance.effects.PlayOneShot(clip, volume);
    }

    private AudioClip GetClip(string key)
    {
        if (!clips.TryGetValue(key, out var clip))
        {
            clip = Resources.Load<AudioClip>("Audio/SFX/" + key);
            clips.Add(key, clip);
            if (!clip) Debug.LogWarning("No se encontró el efecto de sonido: " + key);
        }
        return clip;
    }
}
