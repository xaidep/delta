using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI; // UI名前空間を追加

public class PowerUpItem : MonoBehaviour
{
    [Header("Movement Settings")]
    [FormerlySerializedAs("speed")] public float 移動速度 = 300f; // UIモードを想定して少し大きめ
    [FormerlySerializedAs("direction")] public Vector3 移動方向 = Vector3.down; // 基本は下方向だが、生成時に書き換える

    [Header("Size Settings")]
    public float アイテムの大きさ = 1.0f;

    [Header("Animation Settings")]
    [FormerlySerializedAs("pulseSpeed")] public float 点滅速度 = 5f;
    [FormerlySerializedAs("pulseScaleRange")] public float 点滅サイズ範囲 = 0.2f; // 元のサイズ ± この値
    private Vector3 originalScale;

    [Header("Glow Settings")]
    [FormerlySerializedAs("glowColor")] public Color 発光色 = new Color(1f, 1f, 0f, 0.5f); // デフォルトは黄色半透明
    [FormerlySerializedAs("glowPulseSpeed")] public float 発光点滅速度 = 10f;
    [FormerlySerializedAs("glowScaleRatio")] public float 発光サイズ倍率 = 1.2f; // 親より一回り大きく
    private Image glowImage; // SpriteRendererからImageに変更

    void Start()
    {
        // 指定された大きさ倍率を適用
        transform.localScale *= アイテムの大きさ;
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
        
        // 60秒で自動消滅（画面を横切るのに十分すぎる時間。途中で消えるのを防ぐ）
        Destroy(gameObject, 60f);

        SetupGlow();
    }

    void Update()
    {
        // 直移動 (localPosition) でより安定した挙動に
        transform.localPosition += 移動方向 * 移動速度 * Time.deltaTime;
        
        // Z座標を確実に0に固定
        Vector3 fixedPos = transform.localPosition;
        fixedPos.z = 0f;
        transform.localPosition = fixedPos;

        // 画面下端を大幅に超えたら消去 (解像度によらず安全に消す)
        if (transform.localPosition.y < -2000f)
        {
            Destroy(gameObject);
        }

        // 拡大縮小アニメーション (PingPong)
        float scale = 1f + Mathf.Sin(Time.time * 点滅速度) * 点滅サイズ範囲;
        transform.localScale = originalScale * scale;

        // グローアニメーション
        if (glowImage != null)
        {
            // アルファ値を点滅
            float alpha = 0.3f + Mathf.PingPong(Time.time * 発光点滅速度, 0.5f); 
            Color c = 発光色;
            c.a = alpha;
            glowImage.color = c;
        }
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

            // SE再生
            if (AudioManager.instance != null)
            {
                AudioManager.instance.PlayItemGet();
            }

            // 自分は消える
            Destroy(gameObject);
        }
    }
    
    // 生成時に呼び出して方向を決める
    public void Initialize(Vector3 moveDirection)
    {
        移動方向 = moveDirection.normalized;
    }

    void SetupGlow()
    {
        // Imageコンポーネントを取得（uGUI対応）
        Image mainImage = GetComponent<Image>();
        if (mainImage != null)
        {
            // グロー効果用の子オブジェクト作成
            GameObject glowObj = new GameObject("GlowEffect");
            glowObj.transform.SetParent(transform);
            glowObj.transform.localPosition = Vector3.zero;
            glowObj.transform.localScale = Vector3.one * 発光サイズ倍率;

            // Image設定
            glowImage = glowObj.AddComponent<Image>();
            glowImage.sprite = mainImage.sprite;
            glowImage.color = 発光色;
            glowImage.raycastTarget = false; // レイキャストはブロックしない

            // RectTransformの設定（全画面に広がらないようにサイズ合わせ）
            RectTransform glowRect = glowImage.rectTransform;
            RectTransform mainRect = mainImage.rectTransform;
            glowRect.sizeDelta = mainRect.sizeDelta;
            
            // 注意: uGUIの親子関係上、子は親の上に描画されるため、
            // グローがアイテムの手前に表示されます。
            // 半透明なので「輝いている」演出としては機能します。
        }
        else
        {
            // Fallback: もしImageがなくてSpriteRendererだった場合（念のため残す）
            SpriteRenderer sr = GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                 GameObject glowObj = new GameObject("GlowEffect");
                glowObj.transform.SetParent(transform);
                glowObj.transform.localPosition = Vector3.zero;
                glowObj.transform.localScale = Vector3.one * 発光サイズ倍率;

                var srGlow = glowObj.AddComponent<SpriteRenderer>();
                srGlow.sprite = sr.sprite;
                srGlow.color = 発光色;
                srGlow.sortingLayerID = sr.sortingLayerID;
                srGlow.sortingOrder = sr.sortingOrder - 1;
                
                // glowImageフィールドの型がImageなので、共通化するためにここではキャストできないが、
                // 今回はImage優先で実装。SpriteRendererの場合は点滅しない（要修正だが、画像からImage確定なので簡易対応）
            }
        }
    }
    void OnDestroy()
    {
        Debug.Log($"PowerUpItem Destroyed at Pos: {transform.position}");
    }
}
