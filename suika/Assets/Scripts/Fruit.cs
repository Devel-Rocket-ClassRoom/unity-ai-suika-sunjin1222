using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(CircleCollider2D))]
[RequireComponent(typeof(SpriteRenderer))]
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
        sr = GetComponent<SpriteRenderer>();
    }

    // 모든 인스턴스가 공유하는 흰색 원 스프라이트 (스프라이트 미할당 시 폴백)
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
        return Sprite.Create(tex, new Rect(0, 0, size, size), Vector2.one * 0.5f, size);
    }

    // 모든 인스턴스가 공유하는 물리 재질 (마찰 낮음, 반발 없음)
    private static PhysicsMaterial2D _fruitMaterial;
    private static PhysicsMaterial2D FruitMaterial
    {
        get
        {
            if (_fruitMaterial == null)
            {
                _fruitMaterial = new PhysicsMaterial2D("FruitMat");
                _fruitMaterial.friction   = 0.05f;  // 낮은 마찰 → 딱 붙지 않음
                _fruitMaterial.bounciness = 0.05f;  // 약간의 탄성
            }
            return _fruitMaterial;
        }
    }

    // radius는 transform.scale로 반영. 콜라이더는 radius=0.5 고정 (world radius = scale.x * 0.5 = data.radius)
    public void Initialize(FruitData fruitData)
    {
        data = fruitData;
        transform.localScale = Vector3.one * data.radius * 2f;

        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.constraints   = RigidbodyConstraints2D.None; // 회전 허용
        rb.mass          = data.radius * data.radius * 3f;
        rb.linearDamping  = 0.5f;
        rb.angularDamping = 5f;

        col.sharedMaterial = FruitMaterial;

        sr.sprite = data.sprite != null ? data.sprite : CircleSprite;
        sr.color  = data.sprite != null ? Color.white : data.fallbackColor;
    }

    // 미리보기 상태: 반투명 + 콜라이더 off
    public void SetAsPreview()
    {
        col.enabled = false;
        Color c = sr.color;
        sr.color = new Color(c.r, c.g, c.b, 0.55f);
        sr.sortingOrder = 10;
    }

    // 실제 물리 과일로 전환
    public void Activate()
    {
        col.enabled = true;
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.angularVelocity = Random.Range(-120f, 120f); // 랜덤 초기 회전
        Color c = sr.color;
        sr.color = new Color(c.r, c.g, c.b, 1f);
        sr.sortingOrder = 0;
        isActive = true;
    }

    public void SetMerging()
    {
        isMerging = true;
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (!isActive || isMerging) return;

        Fruit other = collision.gameObject.GetComponent<Fruit>();
        if (other == null || !other.isActive || other.isMerging) return;

        if (data.level != other.data.level) return;
        if (data.level >= GameManager.Instance.MaxFruitLevel) return;

        // instanceID 낮은 쪽이 merge 주도 (양쪽 동시 처리 방지)
        if (GetInstanceID() < other.GetInstanceID())
        {
            SetMerging();
            other.SetMerging();
            GameManager.Instance.MergeFruits(this, other);
        }
    }
}
