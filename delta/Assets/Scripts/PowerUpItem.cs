using UnityEngine;

public class PowerUpItem : MonoBehaviour
{
    [Header("Movement Settings")]
    public float speed = 300f; // UIモードを想定して少し大きめ
    public Vector3 direction = Vector3.down; // 基本は下方向だが、生成時に書き換える

    [Header("Animation Settings")]
    public float pulseSpeed = 5f;
    public float pulseScaleRange = 0.2f; // 元のサイズ ± この値
    private Vector3 originalScale;

    void Start()
    {
        originalScale = transform.localScale;
        
        // 当たり判定用コライダー (Trigger)
        BoxCollider2D col = GetComponent<BoxCollider2D>();
        if (col == null)
        {
            col = gameObject.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
        }

        // UIモードならサイズを大きくする (50x50)
        col.size = new Vector2(50f, 50f);

        // TriggerイベントにはRigidbodyが必要 (Playerについていない場合用)
        if (GetComponent<Rigidbody2D>() == null)
        {
            var rb = gameObject.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic; // 物理演算はしない
            rb.gravityScale = 0f;
        }
        
        Debug.Log($"PowerUpItem Initialized. Pos: {transform.position}, Collider Size: {col.size}");
        
        // 5秒で自動消滅（画面外に出なくても消える保険）
        Destroy(gameObject, 5f);
    }

    void Update()
    {
        // 移動
        transform.Translate(direction * speed * Time.deltaTime);

        // 拡大縮小アニメーション (PingPong)
        float scale = 1f + Mathf.Sin(Time.time * pulseSpeed) * pulseScaleRange;
        transform.localScale = originalScale * scale;
    }

    // プレイヤーが「Trigger」を持っている場合
    void OnTriggerEnter2D(Collider2D other)
    {
        HandleHit(other.gameObject);
    }

    // プレイヤーが「Collision」の場合
    void OnCollisionEnter2D(Collision2D collision)
    {
        HandleHit(collision.gameObject);
    }

    void HandleHit(GameObject other)
    {
        // プレイヤーかどうか判定（タグ、またはスクリプト）
        PlayerController player = other.GetComponent<PlayerController>();
        if (player == null)
        {
            player = other.GetComponentInParent<PlayerController>();
        }

        if (player != null || other.CompareTag("Player"))
        {
            Debug.Log("PowerUp Get!");
            
            // プレイヤーのレベルアップ処理を呼ぶ
            if (player != null)
            {
                player.LevelUp();
            }

            // 自分は消える
            Destroy(gameObject);
        }
    }
    
    // 生成時に呼び出して方向を決める
    public void Initialize(Vector3 moveDirection)
    {
        direction = moveDirection.normalized;
    }
}
