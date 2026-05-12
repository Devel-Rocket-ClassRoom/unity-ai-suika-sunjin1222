using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem.UI;
using UnityEngine.EventSystems;
using TMPro;

public static class SceneSetup
{
    // 게임 영역 상수
    const float WALL_X   =  3.0f;   // 벽 내벽 X
    const float FLOOR_Y  = -5.0f;   // 바닥 Y
    const float DANGER_Y =  3.0f;   // 위험선 Y
    const float SPAWN_Y  =  4.2f;   // 과일 드롭 시작 Y

    // 배치 모드 전용 (Unity.exe -executeMethod SceneSetup.BatchRun)
    public static void BatchRun()
    {
        string scenePath = "Assets/Scenes/SampleScene.unity";
        EditorSceneManager.OpenScene(scenePath);
        SetupCamera();
        CreateContainer();
        CreateDangerZone();
        EnsureGameManager();
        EnsureFruitSpawner();
        EnsureFruitPrefab();
        CreateUI();
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        Debug.Log("[SuikaGame] 배치 씬 세팅 완료 & 저장됨");
    }

    [MenuItem("SuikaGame/EventSystem 추가")]
    static void AddEventSystem()
    {
        if (Object.FindFirstObjectByType<EventSystem>() != null)
        {
            EditorUtility.DisplayDialog("알림", "EventSystem이 이미 씬에 있습니다.", "확인");
            return;
        }
        var go = new GameObject("EventSystem");
        go.AddComponent<EventSystem>();
        go.AddComponent<InputSystemUIInputModule>();
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        Debug.Log("[SuikaGame] EventSystem 추가 완료");
    }

    [MenuItem("SuikaGame/씬 자동 세팅")]
    static void Run()
    {
        SetupCamera();
        CreateContainer();
        CreateDangerZone();
        EnsureGameManager();
        EnsureFruitSpawner();
        EnsureFruitPrefab();
        CreateUI();

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorUtility.DisplayDialog("완료",
            "씬 세팅 완료!\n\n남은 작업:\n• GameManager의 Fruit Datas 배열에 FruitData 11개 연결\n• 각 FruitData에 스프라이트 할당",
            "확인");
    }

    // ─────────────── 카메라 ───────────────
    static void SetupCamera()
    {
        Camera cam = Camera.main;
        if (cam == null)
        {
            var go = new GameObject("Main Camera") { tag = "MainCamera" };
            cam = go.AddComponent<Camera>();
            go.AddComponent<AudioListener>();
        }
        cam.orthographic = true;
        cam.orthographicSize = 6f;
        cam.transform.position = new Vector3(0, -0.5f, -10);
        cam.backgroundColor = new Color(0.96f, 0.93f, 0.86f);
        cam.clearFlags = CameraClearFlags.SolidColor;
    }

    // ─────────────── 컨테이너 (벽 3개) ───────────────
    static void CreateContainer()
    {
        if (GameObject.Find("Container") != null) return;
        var root = new GameObject("Container");

        float midY = (DANGER_Y + FLOOR_Y) / 2f;
        float height = DANGER_Y - FLOOR_Y;

        MakeWall(root, "LeftWall",   new Vector2(-WALL_X - 0.1f, midY),  new Vector2(0.2f, height + 0.2f));
        MakeWall(root, "RightWall",  new Vector2( WALL_X + 0.1f, midY),  new Vector2(0.2f, height + 0.2f));
        MakeWall(root, "BottomWall", new Vector2(0, FLOOR_Y - 0.1f),     new Vector2(WALL_X * 2 + 0.4f, 0.2f));
    }

    static void MakeWall(GameObject parent, string name, Vector2 pos, Vector2 size)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent.transform);
        go.transform.position = pos;
        go.transform.localScale = new Vector3(size.x, size.y, 1);

        var col = go.AddComponent<BoxCollider2D>();
        col.size = Vector2.one; // scale이 실제 크기 담당

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = MakeWhiteSprite();
        sr.color = new Color(0.55f, 0.38f, 0.22f);
        sr.sortingOrder = -1;
    }

    // ─────────────── 위험 구역 ───────────────
    static void CreateDangerZone()
    {
        if (GameObject.Find("DangerZone") != null) return;

        // 빨간 위험선 (시각)
        var line = new GameObject("DangerLine");
        line.transform.position = new Vector3(0, DANGER_Y, 0);
        line.transform.localScale = new Vector3(WALL_X * 2, 0.04f, 1);
        var sr = line.AddComponent<SpriteRenderer>();
        sr.sprite = MakeWhiteSprite();
        sr.color = new Color(1f, 0.2f, 0.2f, 0.7f);
        sr.sortingOrder = 5;

        // 트리거 박스 (게임오버 판정)
        var zone = new GameObject("DangerZone");
        zone.transform.position = new Vector3(0, DANGER_Y + 1.5f, 0);
        var col = zone.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(WALL_X * 2, 3f);
        zone.AddComponent<DangerZone>();
    }

    // ─────────────── GameManager ───────────────
    static void EnsureGameManager()
    {
        if (GameObject.Find("GameManager") != null) return;
        var go = new GameObject("GameManager");
        go.AddComponent<GameManager>();
    }

    // ─────────────── FruitSpawner ───────────────
    static void EnsureFruitSpawner()
    {
        if (GameObject.Find("FruitSpawner") != null) return;
        var go = new GameObject("FruitSpawner");
        go.transform.position = new Vector3(0, SPAWN_Y, 0);
        go.AddComponent<FruitSpawner>();
    }

    // ─────────────── Fruit 프리팹 ───────────────
    static void EnsureFruitPrefab()
    {
        const string path = "Assets/Prefabs/Fruit.prefab";
        if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null) return;

        var go = new GameObject("Fruit");

        var rb = go.AddComponent<Rigidbody2D>();
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;

        var col = go.AddComponent<CircleCollider2D>();
        col.radius = 0.5f;

        go.AddComponent<SpriteRenderer>();
        go.AddComponent<Fruit>();

        var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);

        // GameManager에 프리팹 자동 연결
        var gm = Object.FindFirstObjectByType<GameManager>();
        if (gm != null)
        {
            var so = new SerializedObject(gm);
            so.FindProperty("fruitPrefab").objectReferenceValue = prefab;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }

    // ─────────────── UI ───────────────
    static void CreateUI()
    {
        if (GameObject.Find("Canvas") != null) return;

        // Canvas
        var canvasGO = new GameObject("Canvas");
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.matchWidthOrHeight = 0.5f;
        canvasGO.AddComponent<GraphicRaycaster>();

        // 점수 텍스트 (좌상단)
        var scoreGO = MakeText(canvasGO, "ScoreText", "점수: 0", 32, TextAlignmentOptions.Left);
        AnchorCorner(scoreGO, new Vector2(0, 1), new Vector2(20, -20), new Vector2(280, 55));

        // 다음 과일 패널 (우상단)
        var nextPanel = MakePanel(canvasGO, "NextFruitPanel", new Color(0, 0, 0, 0.35f));
        AnchorCorner(nextPanel, new Vector2(1, 1), new Vector2(-20, -20), new Vector2(140, 175));

        var nextLabelGO = MakeText(nextPanel, "NextLabel", "다음", 22, TextAlignmentOptions.Center);
        AnchorCorner(nextLabelGO, new Vector2(0.5f, 1), new Vector2(0, -18), new Vector2(130, 30));

        var nextImgGO = new GameObject("NextFruitImage");
        nextImgGO.transform.SetParent(nextPanel.transform, false);
        var nextImg = nextImgGO.AddComponent<Image>();
        var ir = nextImgGO.GetComponent<RectTransform>();
        ir.anchorMin = ir.anchorMax = new Vector2(0.5f, 1);
        ir.pivot = new Vector2(0.5f, 1);
        ir.anchoredPosition = new Vector2(0, -55);
        ir.sizeDelta = new Vector2(100, 100);

        var nextNameGO = MakeText(nextPanel, "NextFruitName", "", 20, TextAlignmentOptions.Center);
        AnchorCorner(nextNameGO, new Vector2(0.5f, 1), new Vector2(0, -162), new Vector2(130, 30));

        // 게임오버 패널 (전체 화면)
        var gameOverPanel = MakePanel(canvasGO, "GameOverPanel", new Color(0, 0, 0, 0.82f));
        var goRect = gameOverPanel.GetComponent<RectTransform>();
        goRect.anchorMin = Vector2.zero;
        goRect.anchorMax = Vector2.one;
        goRect.offsetMin = goRect.offsetMax = Vector2.zero;

        var goTitle = MakeText(gameOverPanel, "GameOverTitle", "GAME OVER", 60, TextAlignmentOptions.Center);
        AnchorCorner(goTitle, new Vector2(0.5f, 0.5f), new Vector2(0, 110), new Vector2(600, 85));

        var finalGO = MakeText(gameOverPanel, "FinalScoreText", "최종 점수\n0", 40, TextAlignmentOptions.Center);
        AnchorCorner(finalGO, new Vector2(0.5f, 0.5f), new Vector2(0, 15), new Vector2(500, 130));

        // 재시작 버튼
        var btnGO = MakePanel(gameOverPanel, "RestartButton", new Color(0.95f, 0.72f, 0.15f));
        AnchorCorner(btnGO, new Vector2(0.5f, 0.5f), new Vector2(0, -90), new Vector2(260, 75));
        var btn = btnGO.AddComponent<Button>();
        var btnTxtGO = MakeText(btnGO, "BtnText", "다시 시작", 30, TextAlignmentOptions.Center);
        StretchFill(btnTxtGO);

        gameOverPanel.SetActive(false);

        // UIManager 추가 & 레퍼런스 연결
        var uiManager = canvasGO.AddComponent<UIManager>();
        var so = new SerializedObject(uiManager);
        so.FindProperty("scoreText").objectReferenceValue       = scoreGO.GetComponent<TMP_Text>();
        so.FindProperty("nextFruitImage").objectReferenceValue  = nextImg;
        so.FindProperty("nextFruitName").objectReferenceValue   = nextNameGO.GetComponent<TMP_Text>();
        so.FindProperty("gameOverPanel").objectReferenceValue   = gameOverPanel;
        so.FindProperty("finalScoreText").objectReferenceValue  = finalGO.GetComponent<TMP_Text>();
        so.ApplyModifiedPropertiesWithoutUndo();

        // 버튼 → UIManager.OnRestartButton 연결
        UnityEventTools.AddPersistentListener(btn.onClick, uiManager.OnRestartButton);
    }

    // ─────────────── UI 헬퍼 ───────────────
    static GameObject MakeText(GameObject parent, string name, string text,
        int fontSize, TextAlignmentOptions align)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.color = Color.white;
        tmp.alignment = align;
        return go;
    }

    static GameObject MakePanel(GameObject parent, string name, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        go.AddComponent<Image>().color = color;
        return go;
    }

    // 특정 앵커 코너에 고정
    static void AnchorCorner(GameObject go, Vector2 anchor, Vector2 pos, Vector2 size)
    {
        var r = go.GetComponent<RectTransform>();
        r.anchorMin = r.anchorMax = anchor;
        r.pivot = anchor;
        r.anchoredPosition = pos;
        r.sizeDelta = size;
    }

    // 부모를 꽉 채우게 stretch
    static void StretchFill(GameObject go)
    {
        var r = go.GetComponent<RectTransform>();
        r.anchorMin = Vector2.zero;
        r.anchorMax = Vector2.one;
        r.offsetMin = r.offsetMax = Vector2.zero;
    }

    static Sprite MakeWhiteSprite()
    {
        var tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, Color.white);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, 1, 1), Vector2.one * 0.5f, 1f);
    }

    // ─────────────── FruitData 생성 ───────────────
    public static void BatchCreateFruitDatas()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
        CreateFruitDatas();
        AssetDatabase.SaveAssets();
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        Debug.Log("[SuikaGame] FruitData 11개 생성 & GameManager 연결 완료");
    }

    [MenuItem("SuikaGame/FruitData 11개 생성")]
    static void CreateFruitDatasMenu()
    {
        CreateFruitDatas();
        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        EditorUtility.DisplayDialog("완료", "FruitData 11개 생성 & GameManager 연결 완료!", "확인");
    }

    static void CreateFruitDatas()
    {
        if (!AssetDatabase.IsValidFolder("Assets/FruitDatas"))
            AssetDatabase.CreateFolder("Assets", "FruitDatas");

        var table = new (string name, float radius, int score, Color color)[]
        {
            ("Cherry",     0.28f,  1, new Color(0.80f, 0.10f, 0.10f)),
            ("Strawberry", 0.392f, 3, new Color(0.95f, 0.25f, 0.25f)),
            ("Grape",      0.504f, 6, new Color(0.50f, 0.10f, 0.60f)),
            ("Tangerine",  0.616f,10, new Color(1.00f, 0.60f, 0.10f)),
            ("Persimmon",  0.728f,15, new Color(0.95f, 0.40f, 0.10f)),
            ("Apple",      0.868f,21, new Color(0.90f, 0.15f, 0.15f)),
            ("Pear",       1.036f,28, new Color(0.80f, 0.85f, 0.30f)),
            ("Peach",      1.218f,36, new Color(1.00f, 0.70f, 0.50f)),
            ("Pineapple",  1.40f, 45, new Color(0.95f, 0.80f, 0.10f)),
            ("Melon",      1.61f, 55, new Color(0.30f, 0.75f, 0.30f)),
            ("Watermelon", 1.82f, 66, new Color(0.10f, 0.50f, 0.10f)),
        };

        var datas = new FruitData[table.Length];
        for (int i = 0; i < table.Length; i++)
        {
            var (fname, radius, score, color) = table[i];
            string path = $"Assets/FruitDatas/{i:00}_{fname}.asset";

            var asset = AssetDatabase.LoadAssetAtPath<FruitData>(path)
                        ?? ScriptableObject.CreateInstance<FruitData>();

            asset.fruitName    = fname;
            asset.level        = i;
            asset.radius       = radius;
            asset.score        = score;
            asset.fallbackColor = color;

            if (string.IsNullOrEmpty(AssetDatabase.GetAssetPath(asset)))
                AssetDatabase.CreateAsset(asset, path);
            else
                EditorUtility.SetDirty(asset);

            datas[i] = asset;
        }

        // GameManager Fruit Datas 배열 연결
        var gm = Object.FindFirstObjectByType<GameManager>();
        if (gm != null)
        {
            var so = new SerializedObject(gm);
            var arr = so.FindProperty("fruitDatas");
            arr.arraySize = datas.Length;
            for (int i = 0; i < datas.Length; i++)
                arr.GetArrayElementAtIndex(i).objectReferenceValue = datas[i];
            so.ApplyModifiedPropertiesWithoutUndo();
        }
        else
        {
            Debug.LogWarning("[SuikaGame] GameManager를 씬에서 찾지 못했습니다. FruitData는 생성됐으나 연결되지 않았습니다.");
        }
    }

    [MenuItem("SuikaGame/스프라이트 자동 연결")]
    static void AssignSpritesMenu()
    {
        string texPath = "Assets/Sprites/Fruit.png";
        var allAssets = AssetDatabase.LoadAllAssetsAtPath(texPath);

        // Fruit_2: 포도만 잡도록 메타 수정됨 (포도 복원)
        // Fruit_6: 3행 1열 여분 과일 → 건너뜀
        // 레벨 0~5 → Fruit_0~5, 레벨 6~10 → Fruit_7~11
        string[] names = { "Cherry","Strawberry","Grape","Tangerine","Persimmon",
                            "Apple","Pear","Peach","Pineapple","Melon","Watermelon" };
        int[] spriteIndices = { 0, 1, 2, 3, 4, 5, 7, 8, 9, 10, 11 };

        // 실제 파일 이름에 관계없이 접두어(00_, 01_, ...)로 FruitData 찾기
        string[] guids = AssetDatabase.FindAssets("t:FruitData", new[] { "Assets/FruitDatas" });
        var dataMap = new System.Collections.Generic.Dictionary<int, FruitData>();
        foreach (var guid in guids)
        {
            string p = AssetDatabase.GUIDToAssetPath(guid);
            string fname = System.IO.Path.GetFileNameWithoutExtension(p);
            if (fname.Length >= 2 && int.TryParse(fname.Substring(0, 2), out int lvl))
            {
                var d = AssetDatabase.LoadAssetAtPath<FruitData>(p);
                if (d != null) dataMap[lvl] = d;
            }
        }

        int assigned = 0;
        for (int level = 0; level <= 10; level++)
        {
            string spriteName = $"Fruit_{spriteIndices[level]}";
            Sprite spr = null;
            foreach (var a in allAssets)
                if (a is Sprite s && s.name == spriteName) { spr = s; break; }

            if (spr == null)
            {
                Debug.LogWarning($"[SuikaGame] 스프라이트 '{spriteName}' 를 찾을 수 없습니다.");
                continue;
            }

            if (!dataMap.TryGetValue(level, out var data))
            {
                Debug.LogWarning($"[SuikaGame] FruitData 레벨 {level} 없음");
                continue;
            }

            data.sprite = spr;
            EditorUtility.SetDirty(data);
            assigned++;
        }

        AssetDatabase.SaveAssets();
        EditorUtility.DisplayDialog("완료", $"스프라이트 {assigned}개 연결 완료!", "확인");
    }
}
