using UnityEngine;
using UnityEngine.Serialization;

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

    void Start()
    {
        // もしUIモードの場合、速度を補正
        // UI座標系(Canvas)だと、1単位が1ピクセルなので、弾の速度=10は超遅い(10px/sec)
        // RectTransformがある、または親がCanvasの可能性があるなら補正
        bool isUI = GetComponent<RectTransform>() != null;
        
        if (isUI && 弾の速度 < 100f)
        {
            弾の速度 = 500f; // UI用速度として強制上書き
        }
        
        // Debug.Log($"Bullet Start. Speed: {弾の速度}, Direction: {direction}");

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
        // TranslateはデフォルトでローカルSpace.Selfなので、回転させている場合はそのまま直進(Vector3.up)でよいが、
        // ここでは direction (World Space) を使っているので、Space.Worldを指定する
        if (direction != Vector3.zero)
        {
            transform.Translate(direction * 弾の速度 * Time.deltaTime, Space.World);
        }
        else
        {
            // もし初期化失敗などでdirectionがゼロなら、とりあえず上に飛ばす（止まっているよりマシ）
            // transform.Translate(Vector3.up * 弾の速度 * Time.deltaTime);
        }
    }
}
