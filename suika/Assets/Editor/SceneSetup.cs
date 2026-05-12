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
        if (GameObject.Find("Canvas") == null) CreateUI();

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorUtility.DisplayDialog("완료",
            "씬 세팅 완료!\n\n남은 작업:\n• GameManager의 Fruit Datas 배열에 FruitData 11개 연결\n• 각 FruitData에 스프라이트 할당",
            "확인");
    }

    [MenuItem("SuikaGame/UI 재구성")]
    static void RebuildUIMenu()
    {
        var existing = GameObject.Find("Canvas");
        if (existing != null) Object.DestroyImmediate(existing);
        CreateUI();
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        EditorUtility.DisplayDialog("완료", "UI 재구성 완료!", "확인");
    }

    [MenuItem("SuikaGame/타이틀 씬 생성")]
    static void CreateTitleSceneMenu()
    {
        // Save and remember current scene
        var prevScene = EditorSceneManager.GetActiveScene();
        if (prevScene.isDirty) EditorSceneManager.SaveScene(prevScene);
        string prevPath = prevScene.path;

        // New empty scene
        var titleScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // Camera
        var camGO = new GameObject("Main Camera") { tag = "MainCamera" };
        var cam = camGO.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 6f;
        cam.transform.position = new Vector3(0, 0, -10);
        cam.backgroundColor = new Color(0.96f, 0.93f, 0.86f);
        cam.clearFlags = CameraClearFlags.SolidColor;
        camGO.AddComponent<AudioListener>();

        // EventSystem
        var esGO = new GameObject("EventSystem");
        esGO.AddComponent<EventSystem>();
        esGO.AddComponent<InputSystemUIInputModule>();

        // TitleManager
        var tmGO = new GameObject("TitleManager");
        var tm = tmGO.AddComponent<TitleManager>();

        // Canvas
        var roundedSpr = MakeRoundedSprite(64, 14);
        var canvasGO = new GameObject("Canvas");
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.matchWidthOrHeight = 0.5f;
        canvasGO.AddComponent<GraphicRaycaster>();

        // "SUIKA" 타이틀
        var suikaGO = MakeText(canvasGO, "TitleSuika", "SUIKA", 130,
            TextAlignmentOptions.Center, new Color(0.15f, 0.15f, 0.15f));
        suikaGO.GetComponent<TMP_Text>().fontStyle = FontStyles.Bold;
        SetRect(suikaGO, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0, 220), new Vector2(900, 150));

        // "GAME" 부제목
        var gameGO = MakeText(canvasGO, "TitleGame", "GAME", 90,
            TextAlignmentOptions.Center, new Color(0.95f, 0.38f, 0.18f));
        gameGO.GetComponent<TMP_Text>().fontStyle = FontStyles.Bold;
        SetRect(gameGO, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0, 100), new Vector2(900, 110));

        // Best Score 텍스트
        var bestScoreGO = MakeText(canvasGO, "BestScoreText", "", 42,
            TextAlignmentOptions.Center, new Color(0.55f, 0.55f, 0.55f));
        SetRect(bestScoreGO, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0, -20), new Vector2(700, 56));

        // START 버튼
        var btnGO = MakeRoundedPanel(canvasGO, "StartButton", new Color(1f, 0.58f, 0.14f), roundedSpr);
        SetRect(btnGO, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0, -180), new Vector2(400, 100));
        var btn = btnGO.AddComponent<Button>();
        var btnColors = ColorBlock.defaultColorBlock;
        btnColors.highlightedColor = new Color(1f, 0.68f, 0.28f);
        btnColors.pressedColor     = new Color(0.88f, 0.48f, 0.08f);
        btn.colors = btnColors;
        var btnTextGO = MakeText(btnGO, "StartText", "START", 52,
            TextAlignmentOptions.Center, Color.white);
        btnTextGO.GetComponent<TMP_Text>().fontStyle = FontStyles.Bold;
        StretchFill(btnTextGO);

        // TitleManager 필드 연결
        var soTM = new SerializedObject(tm);
        soTM.FindProperty("bestScoreText").objectReferenceValue = bestScoreGO.GetComponent<TMP_Text>();
        soTM.ApplyModifiedPropertiesWithoutUndo();

        // 버튼 → TitleManager.OnStartButton
        UnityEventTools.AddPersistentListener(btn.onClick, tm.OnStartButton);

        // 저장
        if (!AssetDatabase.IsValidFolder("Assets/Scenes"))
            AssetDatabase.CreateFolder("Assets", "Scenes");
        EditorSceneManager.SaveScene(titleScene, "Assets/Scenes/TitleScene.unity");

        // Build Settings 에 두 씬 등록 (TitleScene=0, SampleScene=1)
        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene("Assets/Scenes/TitleScene.unity", true),
            new EditorBuildSettingsScene("Assets/Scenes/SampleScene.unity", true),
        };

        // SampleScene 복귀
        if (!string.IsNullOrEmpty(prevPath))
            EditorSceneManager.OpenScene(prevPath);

        EditorUtility.DisplayDialog("완료",
            "타이틀 씬 생성 완료!\nBuild Settings: TitleScene(0), SampleScene(1)", "확인");
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
        var roundedSpr = MakeRoundedSprite(64, 14);

        // Canvas
        var canvasGO = new GameObject("Canvas");
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;

        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.matchWidthOrHeight = 0.5f;
        canvasGO.AddComponent<GraphicRaycaster>();

        // ── HUD 패널 (우상단, 컴팩트) ──
        var hudPanel = MakeRoundedPanel(canvasGO, "HUDPanel", Color.white, roundedSpr);
        // anchor top-right, pivot top-right, 160×240
        SetRect(hudPanel, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-20, -20), new Vector2(160, 240));
        hudPanel.AddComponent<Shadow>().effectColor = new Color(0, 0, 0, 0.18f);

        // 점수 라벨 (작게)
        var scoreLabelGO = MakeText(hudPanel, "ScoreLabel", "SCORE", 16,
            TextAlignmentOptions.Center, new Color(0.55f, 0.55f, 0.55f));
        SetRect(scoreLabelGO, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -12), new Vector2(140, 20));

        // 점수 숫자 (크고 굵게, scoreText)
        var scoreTextGO = MakeText(hudPanel, "ScoreText", "0", 38,
            TextAlignmentOptions.Center, new Color(0.15f, 0.15f, 0.15f));
        scoreTextGO.GetComponent<TMP_Text>().fontStyle = FontStyles.Bold;
        SetRect(scoreTextGO, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -46), new Vector2(140, 44));

        // 구분선
        var divider = new GameObject("Divider");
        divider.transform.SetParent(hudPanel.transform, false);
        divider.AddComponent<Image>().color = new Color(0.88f, 0.88f, 0.88f);
        SetRect(divider, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -98), new Vector2(130, 2));

        // 다음 과일 라벨 (작게)
        var nextLabelGO = MakeText(hudPanel, "NextLabel", "NEXT", 16,
            TextAlignmentOptions.Center, new Color(0.55f, 0.55f, 0.55f));
        SetRect(nextLabelGO, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -110), new Vector2(140, 20));

        // 다음 과일 아이콘 80×80, 패널 안에 고정 (nextFruitImage)
        var nextImgGO = new GameObject("NextFruitImage");
        nextImgGO.transform.SetParent(hudPanel.transform, false);
        var nextImg = nextImgGO.AddComponent<Image>();
        nextImg.preserveAspect = true;
        // pivot top-center → anchoredPosition.y=-140 → top at 140px, bottom at 220px (panel 240px)
        SetRect(nextImgGO, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -140), new Vector2(80, 80));

        // ── 게임오버 패널 ──
        var gameOverPanelGO = new GameObject("GameOverPanel");
        gameOverPanelGO.transform.SetParent(canvasGO.transform, false);
        var goOverlay = gameOverPanelGO.AddComponent<Image>();
        goOverlay.color = new Color(0f, 0f, 0f, 0.55f);
        var goOverlayRect = gameOverPanelGO.GetComponent<RectTransform>();
        goOverlayRect.anchorMin = Vector2.zero;
        goOverlayRect.anchorMax = Vector2.one;
        goOverlayRect.offsetMin = goOverlayRect.offsetMax = Vector2.zero;

        // 카드
        var goCard = MakeRoundedPanel(gameOverPanelGO, "GameOverCard", Color.white, roundedSpr);
        SetRect(goCard, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 0), new Vector2(540, 460));
        goCard.AddComponent<Shadow>().effectColor = new Color(0, 0, 0, 0.25f);

        // 게임오버 타이틀
        var goTitleGO = MakeText(goCard, "GameOverTitle", "GAME OVER", 68,
            TextAlignmentOptions.Center, new Color(0.95f, 0.38f, 0.18f));
        goTitleGO.GetComponent<TMP_Text>().fontStyle = FontStyles.Bold;
        SetRect(goTitleGO, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -85), new Vector2(480, 82));

        // 최종 점수 라벨
        var finalLabelGO = MakeText(goCard, "FinalLabel", "Final Score", 30,
            TextAlignmentOptions.Center, new Color(0.55f, 0.55f, 0.55f));
        SetRect(finalLabelGO, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -185), new Vector2(420, 38));

        // 최종 점수 숫자 (finalScoreText)
        var finalScoreGO = MakeText(goCard, "FinalScoreText", "0", 76,
            TextAlignmentOptions.Center, new Color(0.15f, 0.15f, 0.15f));
        finalScoreGO.GetComponent<TMP_Text>().fontStyle = FontStyles.Bold;
        SetRect(finalScoreGO, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -285), new Vector2(440, 88));

        // 다시 시작 버튼
        var btnGO = MakeRoundedPanel(goCard, "RestartButton", new Color(1f, 0.58f, 0.14f), roundedSpr);
        SetRect(btnGO, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 52), new Vector2(320, 78));
        var btn = btnGO.AddComponent<Button>();
        var btnColors = ColorBlock.defaultColorBlock;
        btnColors.highlightedColor = new Color(1f, 0.68f, 0.28f);
        btnColors.pressedColor     = new Color(0.88f, 0.48f, 0.08f);
        btn.colors = btnColors;
        var btnTextGO = MakeText(btnGO, "RestartText", "다시 시작", 36,
            TextAlignmentOptions.Center, Color.white);
        btnTextGO.GetComponent<TMP_Text>().fontStyle = FontStyles.Bold;
        StretchFill(btnTextGO);

        gameOverPanelGO.SetActive(false);

        // ── UIManager 연결 ──
        var uiManager = canvasGO.AddComponent<UIManager>();
        var so = new SerializedObject(uiManager);
        so.FindProperty("scoreText").objectReferenceValue      = scoreTextGO.GetComponent<TMP_Text>();
        so.FindProperty("nextFruitImage").objectReferenceValue = nextImg;
        so.FindProperty("nextFruitName").objectReferenceValue  = null;
        so.FindProperty("gameOverPanel").objectReferenceValue  = gameOverPanelGO;
        so.FindProperty("finalScoreText").objectReferenceValue = finalScoreGO.GetComponent<TMP_Text>();
        so.ApplyModifiedPropertiesWithoutUndo();

        UnityEventTools.AddPersistentListener(btn.onClick, uiManager.OnRestartButton);
    }

    // ─────────────── UI 헬퍼 ───────────────
    static Sprite MakeRoundedSprite(int size = 64, int cornerRadius = 14)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        var pixels = new Color32[size * size];
        int r = cornerRadius;
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            bool inCorner = (x < r || x >= size - r) && (y < r || y >= size - r);
            float alpha = 1f;
            if (inCorner)
            {
                int cx = x < r ? r : size - r - 1;
                int cy = y < r ? r : size - r - 1;
                float dist = Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy));
                alpha = Mathf.Clamp01(r - dist + 0.5f);
            }
            pixels[y * size + x] = new Color32(255, 255, 255, (byte)(alpha * 255));
        }
        tex.SetPixels32(pixels);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), Vector2.one * 0.5f,
            100f, 0, SpriteMeshType.FullRect, new Vector4(r, r, r, r));
    }

    static GameObject MakeRoundedPanel(GameObject parent, string name, Color color, Sprite roundedSpr)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        var img = go.AddComponent<Image>();
        img.sprite = roundedSpr;
        img.type   = Image.Type.Sliced;
        img.color  = color;
        return go;
    }

    static GameObject MakeText(GameObject parent, string name, string text,
        int fontSize, TextAlignmentOptions align, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text      = text;
        tmp.fontSize  = fontSize;
        tmp.color     = color;
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

    static void SetRect(GameObject go, Vector2 anchorMin, Vector2 anchorMax, Vector2 pos, Vector2 size)
    {
        var r = go.GetComponent<RectTransform>();
        r.anchorMin = anchorMin;
        r.anchorMax = anchorMax;
        r.pivot = (anchorMin + anchorMax) * 0.5f;
        r.anchoredPosition = pos;
        r.sizeDelta = size;
    }

    // 특정 앵커 코너에 고정 (하위 호환)
    static void AnchorCorner(GameObject go, Vector2 anchor, Vector2 pos, Vector2 size)
    {
        var r = go.GetComponent<RectTransform>();
        r.anchorMin = r.anchorMax = anchor;
        r.pivot = anchor;
        r.anchoredPosition = pos;
        r.sizeDelta = size;
    }

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
