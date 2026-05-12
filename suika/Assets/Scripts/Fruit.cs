using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(CircleCollider2D))]
public class Fruit : MonoBehaviour
{
    public FruitData data { get; private set; }
    public bool isMerging { get; private set; }
    public bool isActive { get; private set; }

    private Rigidbody2D rb;
    private CircleCollider2D col;
    private SpriteRenderer sr;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<CircleCollider2D>();

        // 루트에 남아있는 SR이 있으면 비활성화 (Visual 자식이 담당)
        var rootSr = GetComponent<SpriteRenderer>();
        if (rootSr != null) rootSr.enabled = false;
    }

    private static Sprite _circleSprite;
    private static Sprite CircleSprite
    {
        get
        {
            if (_circleSprite == null) _circleSprite = CreateCircleSprite(256);
            return _circleSprite;
        }
    }

    private static Sprite CreateCircleSprite(int size)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        float c = size / 2f;
        float r = c - 1f;
        var pixels = new Color32[size * size];
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float dx = x - c + 0.5f, dy = y - c + 0.5f;
            float dist = Mathf.Sqrt(dx * dx + dy * dy);
            byte a = (byte)(Mathf.Clamp01(r - dist + 1f) * 255);
            pixels[y * size + x] = new Color32(255, 255, 255, a);
        }
        tex.SetPixels32(pixels);
        tex.Apply();
        // PPU=size → world diameter = 1 at localScale=1
        return Sprite.Create(tex, new Rect(0, 0, size, size), Vector2.one * 0.5f, size);
    }

    private static PhysicsMaterial2D _fruitMaterial;
    private static PhysicsMaterial2D FruitMaterial
    {
        get
        {
            if (_fruitMaterial == null)
            {
                _fruitMaterial = new PhysicsMaterial2D("FruitMat");
                _fruitMaterial.friction   = 0.05f;
                _fruitMaterial.bounciness = 0.05f;
            }
            return _fruitMaterial;
        }
    }

    public void Initialize(FruitData fruitData)
    {
        data = fruitData;

        // transform.localScale로 물리 크기 결정 (col.radius=0.5 고정, world radius = scale*0.5 = data.radius)
        transform.localScale = Vector3.one * data.radius * 2f;

        col.radius = 0.5f;
        col.sharedMaterial = FruitMaterial;

        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.constraints   = RigidbodyConstraints2D.None;
        rb.mass          = data.radius * data.radius * 3f;
        rb.linearDamping  = 0.5f;
        rb.angularDamping = 5f;

        SetupVisual();
    }

    void SetupVisual()
    {
        Transform visual = transform.Find("Visual");
        if (visual == null)
        {
            visual = new GameObject("Visual").transform;
            visual.SetParent(transform, false);
            visual.localPosition = Vector3.zero;
            visual.localRotation = Quaternion.identity;
        }

        sr = visual.GetComponent<SpriteRenderer>();
        if (sr == null)
            sr = visual.gameObject.AddComponent<SpriteRenderer>();

        Sprite sprite = data.sprite != null ? data.sprite : CircleSprite;
        sr.sprite = sprite;
        sr.color  = data.sprite != null ? Color.white : data.fallbackColor;

        // min 기준 스케일: 짧은 변이 지름에 맞고 긴 변은 살짝 넘침 → 납작해 보이지 않음
        float minPx = Mathf.Min(sprite.rect.width, sprite.rect.height);
        visual.localScale = Vector3.one * (sprite.pixelsPerUnit / minPx);
    }

    public void SetAsPreview()
    {
        col.enabled = false;
        Color c = sr.color;
        sr.color = new Color(c.r, c.g, c.b, 0.55f);
        sr.sortingOrder = 10;
    }

    public void Activate()
    {
        col.enabled = true;
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.angularVelocity = Random.Range(-120f, 120f);
        Color c = sr.color;
        sr.color = new Color(c.r, c.g, c.b, 1f);
        sr.sortingOrder = 0;
        isActive = true;
    }

    public void SetMerging() => isMerging = true;

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (!isActive || isMerging) return;

        Fruit other = collision.gameObject.GetComponent<Fruit>();
        if (other == null || !other.isActive || other.isMerging) return;
        if (data.level != other.data.level) return;
        if (data.level >= GameManager.Instance.MaxFruitLevel) return;

        if (GetInstanceID() < other.GetInstanceID())
        {
            SetMerging();
            other.SetMerging();
            GameManager.Instance.MergeFruits(this, other);
        }
    }
}
