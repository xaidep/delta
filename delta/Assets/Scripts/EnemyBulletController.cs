using UnityEngine;

public class EnemyBulletController : MonoBehaviour
{
    public float speed = 10f;
    private Vector3 direction; // 移動方向

    public void Initialize(Vector3 dir)
    {
        direction = dir.normalized;
        
        // 弾の向きを進行方向に向ける (Z軸回転)
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle - 90); // -90はスプライトの向きによる補正
    }

    void Start()
    {
        // もしUIモードの場合、速度を補正
        if (GetComponent<RectTransform>() != null && speed == 10f)
        {
            speed = 800f; // UI用速度
        }

        // Colliderの自動設定 (PlayerController同様)
        if (GetComponent<Collider2D>() == null)
        {
            BoxCollider2D col = gameObject.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            if (GetComponent<RectTransform>() != null)
                col.size = new Vector2(30f, 30f); // 少し小さめ
            else
                col.size = new Vector2(0.3f, 0.3f);
        }

         // Rigidbodyの自動設定
        if (GetComponent<Rigidbody2D>() == null)
        {
            Rigidbody2D rb = gameObject.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.mass = 0.0001f;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous; 
        }

        // 5秒後に自動消滅 (画面外に出た場合の保険)
        Destroy(gameObject, 5f);
    }

    void Update()
    {
        // 設定された方向へ移動
        // Translateはローカル座標基準で動くため、回転させているなら Vector3.up でも良いが、
        // ここではワールド座標で方向を指定して移動させる
        transform.position += direction * speed * Time.deltaTime;
    }
}
