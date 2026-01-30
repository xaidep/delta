using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class EnemyBulletController : MonoBehaviour
{
    [FormerlySerializedAs("speed")] public float 弾の速度 = 10f;
    private Vector3 direction; // 移動方向

    public void Initialize(Vector3 dir)
    {
        direction = dir.normalized;
        
        // 弾の向きを進行方向に向ける (Z軸回転)
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle - 90); // -90はスプライトの向きによる補正
    }

    private Image bulletImage;
    private SpriteRenderer bulletSprite;
    [HideInInspector] public bool isRainbow = false;

    void Start()
    {
        bulletImage = GetComponent<Image>();
        bulletSprite = GetComponent<SpriteRenderer>();

        // もしUIモードの場合、速度を補正
        bool isUI = bulletImage != null;
        
        if (isUI && 弾の速度 < 100f)
        {
            弾の速度 = 500f; // UI用速度として強制上書き
        }
        
        // Colliderの自動設定
        if (GetComponent<Collider2D>() == null)
        {
            BoxCollider2D col = gameObject.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            if (isUI)
                col.size = new Vector2(30f, 30f);
            else
                col.size = new Vector2(0.3f, 0.3f);
        }

        if (GetComponent<Rigidbody2D>() == null)
        {
            Rigidbody2D rb = gameObject.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.mass = 0.0001f;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous; 
        }

        Destroy(gameObject, 5f);
    }

    void Update()
    {
        // 移動
        if (direction != Vector3.zero)
        {
            transform.Translate(direction * 弾の速度 * Time.deltaTime, Space.World);
        }

        // --- ビジュアルエフェクト ---
        Color targetColor = new Color(1f, 0.2f, 0.6f); // 鮮やかなピンク色

        // 1. 七色変化（ラスボス用）
        if (isRainbow)
        {
            // 時間経過で色相を回す
            float hue = (Time.time * 2f) % 1.0f;
            targetColor = Color.HSVToRGB(hue, 0.8f, 1.0f);
        }
        
        // 2. 点滅（見えにくいとの要望により削除）
        // targetColor.a はデフォルトの 1.0f のまま維持

        // 適用
        if (bulletImage != null) bulletImage.color = targetColor;
        if (bulletSprite != null) bulletSprite.color = targetColor;
    }
}
