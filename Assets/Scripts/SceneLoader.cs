using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// AUDIOVIDO — Scene Loader (spec §12.3)
/// Handles single and additive scene transitions with fade-to-black.
/// Persists across scenes as a singleton.
/// </summary>
public class SceneLoader : MonoBehaviour
{
    public static SceneLoader Instance { get; private set; }

    [SerializeField] CanvasGroup fadeCanvas;
    [SerializeField] float fadeDuration = 0.3f; // spec §9.1: 300ms fade

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>Load additively with fade (spec §12.3 — 3D spaces load additive).</summary>
    public void LoadAdditive(string sceneName) =>
        StartCoroutine(LoadAdditiveRoutine(sceneName));

    /// <summary>Load as single scene (replaces everything).</summary>
    public void LoadSingle(string sceneName) =>
        StartCoroutine(LoadSingleRoutine(sceneName));

    // Legacy support
    public void LoadRoom(string sceneName) => LoadSingle(sceneName);

    /// <summary>
    /// Re-assign the fade overlay after a scene reload — this singleton outlives
    /// scenes, but its fade canvas dies with the scene it was created in.
    /// </summary>
    public void SetFadeCanvas(CanvasGroup cg) => fadeCanvas = cg;

    IEnumerator LoadAdditiveRoutine(string sceneName)
    {
        yield return FadeTo(1f);
        yield return SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
        yield return FadeTo(0f);
    }

    IEnumerator LoadSingleRoutine(string sceneName)
    {
        yield return FadeTo(1f);
        yield return SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
        yield return FadeTo(0f);
    }

    IEnumerator FadeTo(float target)
    {
        if (fadeCanvas == null) yield break;
        fadeCanvas.gameObject.SetActive(true);
        float start = fadeCanvas.alpha;
        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            fadeCanvas.alpha = Mathf.Lerp(start, target, t / fadeDuration);
            yield return null;
        }
        fadeCanvas.alpha = target;
        if (target == 0f) fadeCanvas.gameObject.SetActive(false);
    }
}
