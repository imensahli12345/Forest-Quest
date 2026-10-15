using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

// Small credits line at the bottom of the main menu (required by the CC-BY-SA license of the death grunt).
// Nothing to set up: like PauseMenu, it adds itself to the MainMenu scene and builds its text by code.
public class Credits : MonoBehaviour
{
    const string line = "Voice sounds: \"Damage Grunts (Male)\" by VoiceBosch, CC-BY-SA 4.0";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Setup()
    {
        AddToScene(SceneManager.GetActiveScene());                       // the scene where Play was pressed
        SceneManager.sceneLoaded += (scene, mode) => AddToScene(scene);  // the next scenes
    }

    static void AddToScene(Scene scene)
    {
        if (scene.name == "MainMenu") new GameObject("Credits").AddComponent<Credits>();
    }

    void Start()
    {
        // Own canvas, under the menu pop-ups but over the grass
        GameObject canvasObject = new GameObject("CreditsCanvas");
        canvasObject.transform.SetParent(transform);
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 5;
        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        // Dark see-through strip at the bottom so the text is readable on the grass
        RectTransform strip = UIFactory.NewRect("CreditsStrip", canvasObject.transform, new Vector2(0, 0), new Vector2(0, 40));
        strip.anchorMin = new Vector2(0, 0);
        strip.anchorMax = new Vector2(1, 0);
        strip.pivot = new Vector2(0.5f, 0);
        Image back = strip.gameObject.AddComponent<Image>();
        back.color = new Color(0f, 0f, 0f, 0.5f);
        back.raycastTarget = false;

        UIFactory.Text("CreditsText", strip, line, 24, UIFactory.cream, Vector2.zero, new Vector2(1800, 40));
    }
}
