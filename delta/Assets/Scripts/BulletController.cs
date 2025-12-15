using UnityEngine;

public class BulletController : MonoBehaviour
{
    // 弾の移動速度
    // UI(Canvas)で使用する場合は、数値を大きくしてください（例：500〜1000）
    public float speed = 10f;

    void Start()
    {
        // もしUIコンポーネント(RectTransform)がついている場合、
        // 速度がデフォルト(10)のままだと遅すぎるため、自動補正する
        if (GetComponent<RectTransform>() != null && speed == 10f)
        {
            speed = 1200f; // UI用のデフォルト速度（倍速にしました）
        }

        // 衝突判定用にColliderが必要
        // UIモード (RectTransformあり) か、座標が非常に大きい場合はサイズを大きくする
        bool isUIMode = GetComponent<RectTransform>() != null || transform.position.magnitude > 100f;
        
        if (GetComponent<BoxCollider2D>() == null)
        {
            BoxCollider2D collider = gameObject.AddComponent<BoxCollider2D>();
            collider.isTrigger = true; 
            
            // UIモードなら大きく(50px)、Worldなら小さく(0.5m)
            if (isUIMode)
                collider.size = new Vector2(50f, 50f);
            else
                collider.size = new Vector2(0.5f, 0.5f);

            Debug.Log($"Bullet Generated. UI Mode: {isUIMode}, Position: {transform.position}, Collider Size: {collider.size}");
        }
        else
        {
             // 元からついていた場合、サイズが極端に小さいなら補正を提案
             var col = GetComponent<BoxCollider2D>();
             if(col != null)
             {
                 if (isUIMode && col.size.x < 10f)
                 {
                     col.size = new Vector2(50f, 50f);
                     Debug.Log("Bullet Collider resized for UI Mode.");
                 }
                 Debug.Log($"Bullet Pre-existing Collider Size: {col.size}");
             }
        }

        // Trigger判定には、少なくとも片方がRigidbodyを持っている必要がある
        if (GetComponent<Rigidbody2D>() == null)
        {
            Rigidbody2D rb = gameObject.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f; // 重力無効
            // rb.isKinematic = true; // ← Kinematicだと設定によってはStaticなColliderと反応しないことがあるため、Dynamicにします
            rb.bodyType = RigidbodyType2D.Dynamic; // Dynamicにする（デフォルト）
            rb.mass = 0.0001f; // 質量を小さくして物理影響を最小限に
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous; // すり抜け防止
        }

        // 3秒後に消滅
        Destroy(gameObject, 3f);
    }

    void Update()
    {
        // 上方向へ移動
        transform.Translate(Vector3.up * speed * Time.deltaTime);
    }
}
