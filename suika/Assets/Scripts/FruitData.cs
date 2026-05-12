using UnityEngine;

[CreateAssetMenu(fileName = "FruitData", menuName = "SuikaGame/FruitData")]
public class FruitData : ScriptableObject
{
    public string fruitName;
    public int level;
    // 반지름 (유니티 월드 단위). 스케일로 조절하므로 sprite PPU는 100으로 통일.
    public float radius = 0.5f;
    public Sprite sprite;
    public Color fallbackColor = Color.white;
    public int score;
}
