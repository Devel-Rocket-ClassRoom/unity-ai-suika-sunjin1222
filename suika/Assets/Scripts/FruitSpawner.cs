using UnityEngine;
using UnityEngine.InputSystem;

public class FruitSpawner : MonoBehaviour
{
    [Tooltip("드롭 가능한 X 범위 (컨테이너 벽 안쪽)")]
    [SerializeField] private float minX = -3f;
    [SerializeField] private float maxX = 3f;

    [Tooltip("드롭 후 다음 과일 준비까지 대기 시간")]
    [SerializeField] private float dropCooldown = 0.5f;

    private Fruit previewFruit;
    private FruitData nextData;
    private bool canDrop = true;
    private bool gameOver = false;

    void Start()
    {
        PrepareNext();
    }

    void Update()
    {
        if (gameOver || previewFruit == null) return;

        float x = ClampedMouseX();
        previewFruit.transform.position = new Vector3(x, transform.position.y, 0f);

        if (Mouse.current.leftButton.wasPressedThisFrame && canDrop)
            Drop();
    }

    void Drop()
    {
        canDrop = false;
        previewFruit.Activate();
        previewFruit = null;
        Invoke(nameof(PrepareNext), dropCooldown);
    }

    void PrepareNext()
    {
        canDrop = true;
        nextData = GameManager.Instance.GetRandomSpawnData();
        if (nextData == null) return;

        Vector3 spawnPos = new Vector3(ClampedMouseX(), transform.position.y, 0f);
        previewFruit = GameManager.Instance.SpawnFruit(spawnPos, nextData);
        previewFruit.SetAsPreview();

        UIManager.Instance.UpdateNextFruit(nextData);
    }

    float ClampedMouseX()
    {
        Vector2 screenPos = Mouse.current.position.ReadValue();
        Vector3 world = Camera.main.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, 0f));
        float r = nextData != null ? nextData.radius : 0f;
        return Mathf.Clamp(world.x, minX + r, maxX - r);
    }

    public void SetGameOver()
    {
        gameOver = true;
        if (previewFruit != null)
        {
            Destroy(previewFruit.gameObject);
            previewFruit = null;
        }
    }
}
