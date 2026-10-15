using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

// GAME OVER pop-up: shown over the level when the hero loses the last heart (PlayerHealth calls Show).
// The game stops, and the player can Retry the level or go back to the Main menu.
// Nothing to set up: like PauseMenu, it adds itself to every level (scenes whose name starts with "Level").
// While it is shown Time.timeScale is 0, so PauseMenu ignores Esc.
public class GameOverMenu : MonoBehaviour
{
    private static GameOverMenu instance;   // the pop-up of the current level

    private GameObject menu;
    private TMP_Text scoreText;
    private int startScore;      // score when the level started (given back by Retry)
    private bool startHasKey;

    // ---------- Add itself to every level ----------

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Setup()
    {
        AddToScene(SceneManager.GetActiveScene());                       // the scene where Play was pressed
        SceneManager.sceneLoaded += (scene, mode) => AddToScene(scene);  // the next scenes
    }

    static void AddToScene(Scene scene)
    {
        if (scene.name.StartsWith("Level")) new GameObject("GameOverMenu").AddComponent<GameOverMenu>();
    }

    // ---------- Show / buttons ----------

    void Awake()
    {
        instance = this;
        // Remember how the level started, before the player can change anything
        startScore = GameState.score;
        startHasKey = GameState.hasKey;
    }

    void Start()
    {
        BuildMenu();
        menu.SetActive(false);
    }

    public static void Show()
    {
        if (instance == null) return;
        Time.timeScale = 0f;   // stops the hero, the monsters and the physics
        instance.scoreText.text = "Score: " + GameState.score;
        instance.menu.SetActive(true);
        SoundFX.PlayGameOver();
    }

    void Retry()
    {
        GameState.score = startScore;
        GameState.hasKey = startHasKey;
        GameState.hearts = 3;
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    void GoToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
    }

    // ---------- Build the pop-up by code (same style as PauseMenu) ----------

    void BuildMenu()
    {
        // Own canvas, drawn over the HUD and the pause menu
        GameObject canvasObject = new GameObject("GameOverCanvas");
        canvasObject.transform.SetParent(transform);
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 11;
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

        menu = UIFactory.Overlay("GameOverMenu", canvasObject.transform).gameObject;
        Transform box = UIFactory.Frame("Box", menu.transform, Vector2.zero, new Vector2(760, 540), UIFactory.dark).transform;

        TMP_Text title = UIFactory.Text("Title", box, "GAME OVER", 80, new Color32(0xE2, 0x68, 0x5A, 0xFF), new Vector2(0, 175), new Vector2(720, 120));
        title.fontStyle = FontStyles.Bold;
        scoreText = UIFactory.Text("Score", box, "", 40, UIFactory.cream, new Vector2(0, 75), new Vector2(700, 60));

        Vector2 size = new Vector2(460, 96);
        UIFactory.MakeButton("RetryButton", box, "RETRY", new Vector2(0, -50), size, out _).onClick.AddListener(Retry);
        UIFactory.MakeButton("MainMenuButton", box, "MAIN MENU", new Vector2(0, -170), size, out _).onClick.AddListener(GoToMainMenu);
    }
}
