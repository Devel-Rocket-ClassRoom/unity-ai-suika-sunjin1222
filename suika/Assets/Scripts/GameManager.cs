using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("과일 데이터 (level 0 ~ 10 순서)")]
    [SerializeField] private FruitData[] fruitDatas;

    [Header("프리팹")]
    [SerializeField] private GameObject fruitPrefab;

    [Header("랜덤 드롭 최대 레벨 (기본 4 = 감까지)")]
    [SerializeField] private int maxRandomSpawnLevel = 4;

    public int MaxFruitLevel => fruitDatas.Length - 1;

    private int score;
    private bool isGameOver;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    public FruitData GetFruitData(int level)
    {
        return fruitDatas[Mathf.Clamp(level, 0, MaxFruitLevel)];
    }

    public FruitData GetRandomSpawnData()
    {
        if (fruitDatas == null || fruitDatas.Length == 0)
        {
            Debug.LogError("[SuikaGame] fruitDatas가 비어있습니다. GameManager Inspector에서 Fruit Datas 배열을 연결하거나, 메뉴 SuikaGame > FruitData 11개 생성을 실행하세요.");
            return null;
        }
        int max = Mathf.Min(maxRandomSpawnLevel, MaxFruitLevel);
        return fruitDatas[Random.Range(0, max + 1)];
    }

    // 항상 kinematic 상태로 반환. 호출측에서 Activate() 또는 SetAsPreview() 처리.
    public Fruit SpawnFruit(Vector3 position, FruitData data)
    {
        GameObject go = Instantiate(fruitPrefab, position, Quaternion.identity);
        Fruit fruit = go.GetComponent<Fruit>();
        fruit.Initialize(data);
        return fruit;
    }

    public void MergeFruits(Fruit a, Fruit b)
    {
        if (isGameOver) return;

        Vector3 mid = (a.transform.position + b.transform.position) / 2f;
        int nextLevel = a.data.level + 1;
        int gained = a.data.score + b.data.score;

        Destroy(a.gameObject);
        Destroy(b.gameObject);

        AddScore(gained);

        if (nextLevel <= MaxFruitLevel)
            StartCoroutine(SpawnMerged(mid, nextLevel));
    }

    private IEnumerator SpawnMerged(Vector3 pos, int level)
    {
        yield return new WaitForFixedUpdate();
        if (isGameOver) yield break;

        Fruit fruit = SpawnFruit(pos, GetFruitData(level));
        fruit.Activate();
    }

    private void AddScore(int amount)
    {
        score += amount;
        UIManager.Instance.UpdateScore(score);
    }

    public void TriggerGameOver()
    {
        if (isGameOver) return;
        isGameOver = true;
        FindFirstObjectByType<FruitSpawner>()?.SetGameOver();
        UIManager.Instance.ShowGameOver(score);
    }

    public void Restart()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
