using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("HUD")]
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private Image nextFruitImage;
    [SerializeField] private TMP_Text nextFruitName;

    [Header("Game Over")]
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private TMP_Text finalScoreText;

    private bool isGameOver = false;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        gameOverPanel.SetActive(false);
        UpdateScore(0);
    }

    void Update()
    {
        // 버튼이 안 될 경우를 대비한 키보드 단축키
        if (isGameOver && Keyboard.current.rKey.wasPressedThisFrame)
            GameManager.Instance.Restart();
    }

    public void UpdateScore(int score)
    {
        scoreText.text = $"Score: {score:N0}";
    }

    public void UpdateNextFruit(FruitData data)
    {
        if (nextFruitName != null)
        {
            nextFruitName.text = data.fruitName;
            nextFruitName.color = data.fallbackColor;
        }
        if (nextFruitImage != null)
        {
            nextFruitImage.sprite = data.sprite;
            nextFruitImage.color = data.sprite != null ? Color.white : data.fallbackColor;
        }
    }

    public void ShowGameOver(int finalScore)
    {
        isGameOver = true;
        gameOverPanel.SetActive(true);
        finalScoreText.text = $"Final Score\n{finalScore:N0}";
    }

    // 게임 오버 패널 버튼의 OnClick 이벤트에 연결
    public void OnRestartButton() => GameManager.Instance.Restart();
}
