using UnityEngine;

public class Background : MonoBehaviour
{
    [SerializeField] private int seed = 42;
    [SerializeField] private int dotCount = 55;

    void Awake()
    {
        var rng = new System.Random(seed);

        // Camera center (0, -0.5), orthographicSize=6, aspect 9:16
        float halfW = 3.8f;
        float minY  = -6.8f;
        float maxY  =  5.8f;

        for (int i = 0; i < dotCount; i++)
        {
            float x    = (float)(rng.NextDouble() * halfW * 2 - halfW);
            float y    = (float)(rng.NextDouble() * (maxY - minY) + minY);
            bool  star = rng.NextDouble() < 0.28;
            float size = star
                ? (float)(rng.NextDouble() * 0.10f + 0.12f)  // 0.12 ~ 0.22
                : (float)(rng.NextDouble() * 0.06f + 0.03f); // 0.03 ~ 0.09

            var dot = new GameObject($"Dot_{i}");
            dot.transform.SetParent(transform);
            dot.transform.position = new Vector3(x, y, 0);

            var sr = dot.AddComponent<SpriteRenderer>();
            sr.sprite       = DotSprite;
            sr.sortingOrder = -5;

            // 따뜻한 황금빛 톤, 별은 좀 더 진하게
            float alpha = star ? 0.30f : 0.18f;
            sr.color = new Color(0.78f, 0.62f, 0.38f, alpha);

            dot.transform.localScale = Vector3.one * size;
        }
    }

    static Sprite _dot;
    static Sprite DotSprite
    {
        get
        {
            if (_dot != null) return _dot;
            const int s = 64;
            var tex = new Texture2D(s, s, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            float c = s / 2f, r = c - 1f;
            var px = new Color32[s * s];
            for (int y = 0; y < s; y++)
            for (int x = 0; x < s; x++)
            {
                float dx = x - c + 0.5f, dy = y - c + 0.5f;
                byte a = (byte)(Mathf.Clamp01(r - Mathf.Sqrt(dx * dx + dy * dy) + 1f) * 255);
                px[y * s + x] = new Color32(255, 255, 255, a);
            }
            tex.SetPixels32(px);
            tex.Apply();
            _dot = Sprite.Create(tex, new Rect(0, 0, s, s), Vector2.one * 0.5f, s);
            return _dot;
        }
    }
}
