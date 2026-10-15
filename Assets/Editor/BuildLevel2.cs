using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using UnityEngine.UI;
using TMPro;

// EDITOR ONLY (not part of the game): builds Level2 from a copy of Level1.
// Click Tools > Build Level2. It replaces Assets/Level2.unity each time, so you can run it again
// after changing the map below.
// It keeps the camera, hero, HUD, messages and sounds of Level1, removes the river puzzle and the key,
// paints the map below and adds the door riddle, the princess and their two pop-ups.
public static class BuildLevel2
{
    // The map: one character = one tile (15 columns x 11 rows), same size as Level1.
    // '#' = tree, 'W' = stone wall of the princess room, '_' = stone floor of the room, '.' = grass,
    // 'L' = lake: one round lake picture (with rocks) over all the 'L' cells. It blocks the way.
    // 'r' = grass with small stones (decoration, you can walk on it).
    // Objects: S = hero start, c = coin, h = heart, m = slime, D = door (with the riddle), P = princess.
    static readonly string[] map =
    {
        "###############",   // row 0
        "#S..c..#m.W___#",   // row 1
        "#.##.#.#..W_P_#",   // row 2
        "#c#..#...cWWDW#",   // row 3
        "#..#.##.#..m..#",   // row 4
        "#..m.c....#.m.#",   // row 5
        "#.#LLLL.###.#.#",   // row 6
        "#cmLLLLrc...#c#",   // row 7
        "#.#LLLL.#.#.#m#",   // row 8
        "#.m.cr.h..m...#",   // row 9
        "###############",   // row 10
    };

    // Same grid as Level1: column 0 is cell x = -7, row 0 is cell y = 5.
    const int left = -7;
    const int top = 5;

    const string coinPrefab = "Assets/Prefabs/Coin.prefab";
    const string slimePrefab = "Assets/Prefabs/Slime.prefab";
    const string heartPrefab = "Assets/Prefabs/Heart.prefab";

    [MenuItem("Tools/Build Level2")]
    static void BuildFromMenu()
    {
        if (!EditorUtility.DisplayDialog("Build Level2",
            "This replaces Assets/Level2.unity with a new level made from Level1.", "Build", "Cancel")) return;
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        Build();
    }

    // Public so it can also run from the command line (batch mode)
    public static void Build()
    {
        Scene level1 = EditorSceneManager.OpenScene("Assets/Level1.unity", OpenSceneMode.Single);
        EditorSceneManager.SaveScene(level1, "Assets/Level2.unity", true);   // copy, Level1 is not changed
        Scene scene = EditorSceneManager.OpenScene("Assets/Level2.unity", OpenSceneMode.Single);

        RemoveLevel1Objects(scene);
        if (!PaintMap()) return;
        AddLake();

        // Hero at the start
        FindRoot(scene, "Player").transform.position = CellCenter('S')[0];

        // Slimes and coins
        GameObject monsters = new GameObject("Monsters");
        foreach (Vector3 pos in CellCenter('m')) Spawn(slimePrefab, pos, monsters.transform, "Slime");
        GameObject coins = new GameObject("Coins");
        foreach (Vector3 pos in CellCenter('c')) Spawn(coinPrefab, pos, coins.transform, "Coin");
        foreach (Vector3 pos in CellCenter('h'))
        {
            GameObject heart = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(heartPrefab), scene);
            heart.transform.position = pos;
        }

        // The Level1 gate becomes the door (same sprite and collider, new script)
        GameObject door = FindRoot(scene, "Gate");
        door.name = "Door";
        Object.DestroyImmediate(door.GetComponent<Gate>());
        door.transform.position = CellCenter('D')[0];
        DoorRiddle riddle = door.AddComponent<DoorRiddle>();

        // The princess behind the door
        GameObject princessObject = new GameObject("Princess");
        princessObject.transform.position = CellCenter('P')[0];
        SpriteRenderer sr = princessObject.AddComponent<SpriteRenderer>();
        sr.sprite = UIBuilder.LoadSprite("Assets/Art/TinyDungeon/TinyDungeon.png", "TinyDungeon_99");
        sr.sortingOrder = 1;
        BoxCollider2D trigger = princessObject.AddComponent<BoxCollider2D>();
        trigger.isTrigger = true;
        trigger.size = new Vector2(0.8f, 0.8f);
        Princess princess = princessObject.AddComponent<Princess>();

        // HUD: new level name, no key in this level
        GameObject canvas = FindRoot(scene, "HUD Canvas");
        UIFactory.font = FindText(canvas, "MessageText").font;
        FindText(canvas, "LevelText").text = "LEVEL 2 · THE PRINCESS";
        HUD hud = canvas.GetComponent<HUD>();
        hud.barKeyText.gameObject.SetActive(false);
        hud.barKeyText = null;

        BuildRiddlePopup(canvas.transform, riddle);
        BuildWinPopup(canvas.transform, princess);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("Level2 built and saved.");
    }

    // ---------- Lake ----------

    // Adds the lake to the Level2 that already exists, without building the whole level again
    [MenuItem("Tools/Add Lake to Level2")]
    static void AddLakeFromMenu()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        Scene scene = EditorSceneManager.OpenScene("Assets/Level2.unity", OpenSceneMode.Single);
        AddLake();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("Lake added to Level2.");
    }

    // One round lake picture over all the 'L' cells (Assets/Art/Water/Lake.png, 4x3 tiles) with a group
    // of 3 rocks on its shore (LakeRocks.png). Solid colliders follow the round water and each rock,
    // so the knight and the slimes can't walk on them. Waves move on the water like on the river of Level1.
    static void AddLake()
    {
        GameObject old = GameObject.Find("Lake");
        if (old != null) Object.DestroyImmediate(old);   // running it again replaces the lake

        // No trees under the water, small stones on the 'r' cells
        Tilemap walls = GameObject.Find("Walls").GetComponent<Tilemap>();
        Tilemap decorations = GameObject.Find("Decorations").GetComponent<Tilemap>();
        List<Vector3> cells = CellCenter('L');
        foreach (Vector3 pos in cells) walls.SetTile(walls.WorldToCell(pos), null);
        foreach (Vector3 pos in CellCenter('r')) decorations.SetTile(decorations.WorldToCell(pos), LoadTile(43));

        // The lake in the middle of its cells
        Vector3 center = Vector3.zero;
        foreach (Vector3 pos in cells) center += pos / cells.Count;
        GameObject lake = new GameObject("Lake");
        lake.transform.position = center;
        SpriteRenderer sr = lake.AddComponent<SpriteRenderer>();
        sr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Water/Lake.png");
        sr.sortingOrder = -1;
        PolygonCollider2D water = lake.AddComponent<PolygonCollider2D>();
        water.pathCount = 1;   // replace the automatic outline by our own oval
        water.SetPath(0, Oval(Vector2.zero, new Vector2(1.875f, 1.375f)));   // edge of the water (30 x 22 px)

        // Rock group on the bottom-right shore, still inside the lake's cells (it blocks no path)
        GameObject rocks = new GameObject("Rocks");
        rocks.transform.SetParent(lake.transform);
        rocks.transform.localPosition = new Vector3(1.3f, -0.95f, 0f);
        SpriteRenderer rsr = rocks.AddComponent<SpriteRenderer>();
        rsr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Water/LakeRocks.png");
        rsr.sortingOrder = 0;   // over the water
        PolygonCollider2D rockCollider = rocks.AddComponent<PolygonCollider2D>();
        rockCollider.pathCount = 3;   // one oval per rock: big, medium, small
        rockCollider.SetPath(0, Oval(new Vector2(-0.156f, -0.063f), new Vector2(0.32f, 0.26f)));
        rockCollider.SetPath(1, Oval(new Vector2(0.219f, 0.125f), new Vector2(0.26f, 0.2f)));
        rockCollider.SetPath(2, Oval(new Vector2(0.313f, -0.219f), new Vector2(0.2f, 0.17f)));

        // Three waves on the water (same look as the river waves)
        Sprite wave = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Water/Wave.png");
        Vector3[] wavePlaces = { new Vector3(-0.8f, 0.5f, 0f), new Vector3(0.7f, 0.1f, 0f), new Vector3(-0.2f, -0.6f, 0f) };
        foreach (Vector3 place in wavePlaces)
        {
            GameObject w = new GameObject("Wave");
            w.transform.SetParent(lake.transform);
            w.transform.localPosition = place;
            SpriteRenderer wsr = w.AddComponent<SpriteRenderer>();
            wsr.sprite = wave;
            wsr.color = new Color(0.8f, 0.95f, 1f, 0.85f);
            w.AddComponent<WaterWaves>();
        }

        // A slime standing where the water is now goes to a free 'm' place of the map
        List<Vector3> free = CellCenter('m');
        Monster[] slimes = Object.FindObjectsOfType<Monster>();
        foreach (Monster m in slimes) free.RemoveAll(p => Vector3.Distance(p, m.transform.position) < 0.5f);
        foreach (Monster m in slimes)
        {
            bool inWater = cells.Exists(p => Vector3.Distance(p, m.transform.position) < 0.5f);
            if (inWater && free.Count > 0) { m.transform.position = free[0]; free.RemoveAt(0); }
        }
    }

    // Points of an oval (for the round colliders)
    static Vector2[] Oval(Vector2 center, Vector2 radius)
    {
        Vector2[] points = new Vector2[32];
        for (int i = 0; i < points.Length; i++)
        {
            float a = i * Mathf.PI * 2f / points.Length;
            points[i] = center + new Vector2(Mathf.Cos(a) * radius.x, Mathf.Sin(a) * radius.y);
        }
        return points;
    }

    // ---------- Level ----------

    static void RemoveLevel1Objects(Scene scene)
    {
        // The river puzzle, the key and an old hidden test square
        string[] names = { "RiverLeft", "RiverRight", "RiverWaves", "WaterBlock", "Bridge", "Plate", "Rock", "Square", "Key" };
        foreach (string name in names)
        {
            GameObject go = FindRoot(scene, name);
            if (go != null) Object.DestroyImmediate(go);
        }

        // All the coins, slimes and the heart of Level1 (they get new places)
        List<GameObject> old = new List<GameObject>();
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
            {
                if (!PrefabUtility.IsOutermostPrefabInstanceRoot(t.gameObject)) continue;
                string path = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(t.gameObject);
                if (path == coinPrefab || path == slimePrefab || path == heartPrefab) old.Add(t.gameObject);
            }
        }
        foreach (GameObject go in old)
            if (go != null) Object.DestroyImmediate(go);
    }

    static bool PaintMap()
    {
        Tilemap ground = GameObject.Find("Ground").GetComponent<Tilemap>();
        Tilemap decorations = GameObject.Find("Decorations").GetComponent<Tilemap>();
        Tilemap walls = GameObject.Find("Walls").GetComponent<Tilemap>();

        TileBase grass = LoadTile(0);
        TileBase flowers = LoadTile(2);
        TileBase tree = LoadTile(28);
        TileBase mushrooms = LoadTile(29);
        TileBase stoneFloor = LoadTile(109);
        TileBase stoneWall = LoadTile(126);
        if (grass == null || flowers == null || tree == null || mushrooms == null || stoneFloor == null || stoneWall == null)
            return false;

        ground.ClearAllTiles();
        decorations.ClearAllTiles();
        walls.ClearAllTiles();

        for (int row = 0; row < map.Length; row++)
        {
            for (int col = 0; col < map[row].Length; col++)
            {
                Vector3Int cell = new Vector3Int(left + col, top - row, 0);
                char c = map[row][col];

                // Stone floor inside the princess room (and under the door), grass everywhere else
                bool inRoom = c == '_' || c == 'P' || c == 'D' || c == 'W';
                ground.SetTile(cell, inRoom ? stoneFloor : grass);

                if (c == '#') walls.SetTile(cell, tree);
                if (c == 'W') walls.SetTile(cell, stoneWall);
            }
        }

        // A few decorations on '.' cells (column, row)
        decorations.SetTile(new Vector3Int(left + 9, top - 1, 0), flowers);
        decorations.SetTile(new Vector3Int(left + 13, top - 4, 0), mushrooms);
        decorations.SetTile(new Vector3Int(left + 1, top - 9, 0), flowers);
        decorations.SetTile(new Vector3Int(left + 10, top - 7, 0), mushrooms);
        return true;
    }

    // World position of the center of every cell with this letter
    static List<Vector3> CellCenter(char letter)
    {
        List<Vector3> list = new List<Vector3>();
        for (int row = 0; row < map.Length; row++)
            for (int col = 0; col < map[row].Length; col++)
                if (map[row][col] == letter) list.Add(new Vector3(col - 6.5f, 5.5f - row, 0));
        return list;
    }

    static void Spawn(string prefabPath, Vector3 pos, Transform parent, string baseName)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        go.transform.position = pos;
        int n = parent.childCount - 1;
        go.name = n == 0 ? baseName : baseName + " (" + n + ")";
    }

    // ---------- Pop-ups ----------

    static void BuildRiddlePopup(Transform canvas, DoorRiddle riddle)
    {
        RectTransform popup = UIFactory.Overlay("RiddlePopup", canvas);
        RectTransform box = UIFactory.Frame("Box", popup, Vector2.zero, new Vector2(1000, 540), UIFactory.dark).rectTransform;

        UIFactory.Text("Title", box, "THE DOOR IS LOCKED", 48, UIFactory.gold, new Vector2(0, 195), new Vector2(920, 70)).fontStyle = FontStyles.Bold;
        TMP_Text question = UIFactory.Text("Question", box, riddle.question, 40, UIFactory.cream, new Vector2(0, 95), new Vector2(920, 110));

        Button[] buttons = new Button[3];
        TMP_Text[] labels = new TMP_Text[3];
        for (int i = 0; i < 3; i++)
        {
            buttons[i] = UIFactory.MakeButton("Answer " + (i + 1), box, riddle.answers[i], new Vector2(-300 + 300 * i, -40), new Vector2(240, 100), out labels[i]);
            UnityEventTools.AddIntPersistentListener(buttons[i].onClick, riddle.Answer, i);
        }

        TMP_Text feedback = UIFactory.Text("Feedback", box, "", 32, UIFactory.red, new Vector2(0, -175), new Vector2(980, 60));   // wide enough for "Wrong answer! Back to the beginning..."

        riddle.riddlePanel = popup.gameObject;
        riddle.questionText = question;
        riddle.answerButtons = buttons;
        riddle.answerTexts = labels;
        riddle.feedbackText = feedback;
        popup.gameObject.SetActive(false);
    }

    static void BuildWinPopup(Transform canvas, Princess princess)
    {
        RectTransform popup = UIFactory.Overlay("WinPopup", canvas);
        RectTransform box = UIFactory.Frame("Box", popup, Vector2.zero, new Vector2(1000, 500), UIFactory.dark).rectTransform;

        UIFactory.Text("Title", box, "YOU WIN!", 72, UIFactory.gold, new Vector2(0, 160), new Vector2(920, 100)).fontStyle = FontStyles.Bold;
        UIFactory.Text("Text", box, "You saved the princess!", 40, UIFactory.cream, new Vector2(0, 65), new Vector2(920, 70));
        TMP_Text score = UIFactory.Text("Score", box, "SCORE 0", 40, UIFactory.green, new Vector2(0, -10), new Vector2(920, 60));

        Button playAgain = UIFactory.MakeButton("PlayAgainButton", box, "PLAY AGAIN", new Vector2(0, -135), new Vector2(360, 100), out _);
        UnityEventTools.AddPersistentListener(playAgain.onClick, princess.PlayAgain);

        princess.winPanel = popup.gameObject;
        princess.winScoreText = score;
        popup.gameObject.SetActive(false);
    }

    // ---------- Helpers ----------

    static GameObject FindRoot(Scene scene, string name)
    {
        foreach (GameObject go in scene.GetRootGameObjects())
            if (go.name == name) return go;
        return null;
    }

    static TMP_Text FindText(GameObject canvas, string name)
    {
        foreach (TMP_Text t in canvas.GetComponentsInChildren<TMP_Text>(true))
            if (t.name == name) return t;
        return null;
    }

    static TileBase LoadTile(int number)
    {
        string file = "Assets/Art/Tiles/TinyTown/TinyTown_" + number + ".asset";
        TileBase tile = AssetDatabase.LoadAssetAtPath<TileBase>(file);
        if (tile == null) Debug.LogError("Tile not found: " + file);
        return tile;
    }
}
