using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TitleManager : MonoBehaviour
{
    [SerializeField] private TMP_Text bestScoreText;

    void Start()
    {
        int best = PlayerPrefs.GetInt("BestScore", 0);
        if (bestScoreText != null)
            bestScoreText.text = best > 0 ? $"Best  {best:N0}" : "";
    }

    public void OnStartButton() => SceneManager.LoadScene("SampleScene");
}
