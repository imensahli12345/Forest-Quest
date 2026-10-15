using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

// Press Esc in a level: the game stops and a pause menu appears (Resume, Settings, Main menu).
// Press Esc again (or Resume) to continue.
// Nothing to set up: it adds itself to every level (the scenes whose name starts with "Level")
// and builds its own buttons by code.
public class PauseMenu : MonoBehaviour
{
    private GameObject menu;          // the whole pause menu (dark layer + box)
    private GameObject mainPage;      // Resume / Settings / Main menu
    private GameObject settingsPage;  // volume, screen shake, fullscreen
    private TMP_Text volumeText;
    private TMP_Text shakeText;
    private TMP_Text fullscreenText;
    private bool paused = false;

    // ---------- Add itself to every level ----------

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Setup()
    {
        AddToScene(SceneManager.GetActiveScene());                    // the scene where Play was pressed
        SceneManager.sceneLoaded += (scene, mode) => AddToScene(scene);  // the next scenes
    }

    static void AddToScene(Scene scene)
    {
        if (scene.name.StartsWith("Level")) new GameObject("PauseMenu").AddComponent<PauseMenu>();
    }

    // ---------- Pause / resume ----------

    void Start()
    {
        BuildMenu();
        menu.SetActive(false);
    }

    void Update()
    {
        if (!Input.GetKeyDown(KeyCode.Escape)) return;

        if (!paused)
        {
            // Another pop-up already stopped the game (door question, "You win!"): don't open over it
            if (Time.timeScale == 0f) return;
            Pause();
        }
        else if (settingsPage.activeSelf) ShowMainPage();   // Esc in the settings = back
        else Resume();
    }

    void Pause()
    {
        paused = true;
        Time.timeScale = 0f;
        ShowMainPage();
        menu.SetActive(true);
    }

    public void Resume()
    {
        paused = false;
        Time.timeScale = 1f;
        menu.SetActive(false);
    }

    public void GoToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
    }

    void ShowMainPage()
    {
        mainPage.SetActive(true);
        settingsPage.SetActive(false);
    }

    void ShowSettingsPage()
    {
        mainPage.SetActive(false);
        settingsPage.SetActive(true);
        RefreshSettings();
    }

    // ---------- Settings ----------

    void RefreshSettings()
    {
        volumeText.text = Settings.VolumeText();
        shakeText.text = Settings.ShakeText();
        fullscreenText.text = Settings.FullscreenText(Settings.Fullscreen);
    }

    void ToggleFullscreen()
    {
        // Unity changes the screen at the end of the frame, so show the new value ourselves
        bool on = !Settings.Fullscreen;
        Settings.Fullscreen = on;
        fullscreenText.text = Settings.FullscreenText(on);
    }

    // ---------- Build the menu by code ----------

    void BuildMenu()
    {
        // Own canvas, drawn over the HUD and the other pop-ups
        GameObject canvasObject = new GameObject("PauseCanvas");
        canvasObject.transform.SetParent(transform);
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;
        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        canvasObject.AddComponent<GraphicRaycaster>();

        // The buttons need an EventSystem (the levels already have one)
        if (FindObjectOfType<EventSystem>() == null)
        {
            GameObject es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<StandaloneInputModule>();
        }

        menu = UIFactory.Overlay("PauseMenu", canvasObject.transform).gameObject;
        Transform box = UIFactory.Frame("Box", menu.transform, Vector2.zero, new Vector2(760, 600), UIFactory.dark).transform;

        TMP_Text title = UIFactory.Text("Title", box, "PAUSED", 64, UIFactory.gold, new Vector2(0, 225), new Vector2(700, 90));
        title.fontStyle = FontStyles.Bold;

        // Main page
        mainPage = UIFactory.Panel("MainPage", box).gameObject;
        UIFactory.Text("Hint", mainPage.transform, "Press Esc to continue", 24, UIFactory.cream, new Vector2(0, -235), new Vector2(700, 40));
        Vector2 size = new Vector2(460, 96);
        UIFactory.MakeButton("ResumeButton", mainPage.transform, "RESUME", new Vector2(0, 95), size, out _).onClick.AddListener(Resume);
        UIFactory.MakeButton("SettingsButton", mainPage.transform, "SETTINGS", new Vector2(0, -20), size, out _).onClick.AddListener(ShowSettingsPage);
        UIFactory.MakeButton("MainMenuButton", mainPage.transform, "MAIN MENU", new Vector2(0, -135), size, out _).onClick.AddListener(GoToMainMenu);

        // Settings page:  [-] VOLUME 100% [+] / SCREEN SHAKE / FULLSCREEN / BACK
        settingsPage = UIFactory.Panel("SettingsPage", box).gameObject;
        Transform s = settingsPage.transform;
        UIFactory.MakeButton("VolumeDownButton", s, "-", new Vector2(-260, 115), new Vector2(90, 86), out _)
            .onClick.AddListener(() => { Settings.Volume -= 0.1f; RefreshSettings(); });
        volumeText = UIFactory.Text("VolumeText", s, "", 40, UIFactory.cream, new Vector2(0, 115), new Vector2(400, 86));
        volumeText.fontStyle = FontStyles.Bold;
        UIFactory.MakeButton("VolumeUpButton", s, "+", new Vector2(260, 115), new Vector2(90, 86), out _)
            .onClick.AddListener(() => { Settings.Volume += 0.1f; RefreshSettings(); });
        UIFactory.MakeButton("ShakeButton", s, "", new Vector2(0, 10), new Vector2(610, 86), out shakeText)
            .onClick.AddListener(() => { Settings.ScreenShake = !Settings.ScreenShake; RefreshSettings(); });
        UIFactory.MakeButton("FullscreenButton", s, "", new Vector2(0, -90), new Vector2(610, 86), out fullscreenText)
            .onClick.AddListener(ToggleFullscreen);
        UIFactory.MakeButton("BackButton", s, "BACK", new Vector2(0, -190), new Vector2(300, 86), out _)
            .onClick.AddListener(ShowMainPage);
    }
}
