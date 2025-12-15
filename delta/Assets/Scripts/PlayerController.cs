using UnityEngine;
using UnityEngine.UI;

public class PlayerController : MonoBehaviour
{
    public GameObject bulletPrefab;
    public float fireInterval = 0.2f;

    // --- アニメーション用画像 ---
    [Header("Animation Sprites")]
    public Sprite imageCenter; // 正面
    public Sprite imageLeft;   // 左
    public Sprite imageRight;  // 右

    private float timeSinceLastFire = 0f;
    private RectTransform rectTransform;
    private Image uiImage;            // UI用の場合の画像コンポーネント
    private SpriteRenderer spriteRenderer; // 2D用の場合の画像コンポーネント
    
    // 移動量を計算するための前回の位置
    private Vector3 lastPosition;

    void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        
        // 当たり判定がない場合は追加 (UIモード用にサイズ調整)
        if (GetComponent<Collider2D>() == null)
        {
            BoxCollider2D col = gameObject.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            if (rectTransform != null) col.size = new Vector2(50f, 50f);
        }

        // Trigger判定にはRigidbodyが必要
        if (GetComponent<Rigidbody2D>() == null)
        {
            var rb = gameObject.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic; // 物理挙動はさせない
            rb.gravityScale = 0f;
        }
        
        // 画像コンポーネントを取得しておく
        if (rectTransform != null)
            uiImage = GetComponent<Image>();
        else
            spriteRenderer = GetComponent<SpriteRenderer>();

        lastPosition = transform.position;

        lastPosition = transform.position;

        CreateShadow(); // 影を作成
        UpdateStats();  // 初期状態の反映（ファンネルを隠すなど）
    }


    void Update()
    {
        MovePlayer();
        AutoFire();
        UpdateShadow(); // 影の位置更新
    }

    void MovePlayer()
    {
        Vector3 currentPos = transform.position;

        // 画面外に出ないようにマウス座標を制限 (Clamp)
        float padding = 30f; // 画面端からの余白
        Vector3 clampedMousePos = Input.mousePosition;
        
        clampedMousePos.x = Mathf.Clamp(clampedMousePos.x, padding, Screen.width - padding);
        clampedMousePos.y = Mathf.Clamp(clampedMousePos.y, padding, Screen.height - padding);

        // UIモード (Canvas内) かどうかで処理を分ける
        if (rectTransform != null)
        {
            // --- UIモード (Canvas Overlay) ---
            transform.position = clampedMousePos;
        }
        else
        {
            // --- 2D Sprite モード (World Space) ---
            Vector3 mousePos = clampedMousePos;
            mousePos.z = 10f; 
            Vector3 worldPos = Camera.main.ScreenToWorldPoint(mousePos);
            worldPos.z = 0f;
            transform.position = worldPos;
        }
        
        currentPos = transform.position; // 移動後の位置

        // --- 移動方向による画像の切り替え ---
        float deltaX = currentPos.x - lastPosition.x;
        float deltaY = currentPos.y - lastPosition.y; // Y方向の移動量
        
        // 感度調整
        float threshold = 0.01f; 

        // 前進（上移動）している時だけエフェクトを表示
        bool isMovingForward = deltaY > threshold;
        ControlEngineEffects(isMovingForward);

        if (deltaX < -threshold)
        {
            // 左移動
            ChangeSprite(imageLeft);
            stationaryTimer = 0f;
        }
        else if (deltaX > threshold)
        {
            // 右移動
            ChangeSprite(imageRight);
            stationaryTimer = 0f;
        }
        else
        {
            // ほぼ静止している場合
            stationaryTimer += Time.deltaTime;
            if (stationaryTimer > 0.1f)
            {
                ChangeSprite(imageCenter);
            }
        }

        lastPosition = currentPos;
    }

    // エンジンエフェクトの表示切り替え
    void ControlEngineEffects(bool isActive)
    {
        if (engineEffects == null) return;
        foreach (var effect in engineEffects)
        {
            if (effect != null)
            {
                // もし既に同じ状態なら何もしない（負荷軽減）
                if (effect.activeSelf != isActive)
                {
                    effect.SetActive(isActive);
                }
            }
        }
    }

    // 静止判定用のタイマー
    private float stationaryTimer = 0f;

    void ChangeSprite(Sprite newSprite)
    {
        if (newSprite == null) return;
        
        // 現在の画像と同じなら変更しない（処理負荷軽減）
        if (uiImage != null && uiImage.sprite == newSprite) return;
        if (spriteRenderer != null && spriteRenderer.sprite == newSprite) return;

        if (uiImage != null)
            uiImage.sprite = newSprite;
        else if (spriteRenderer != null)
            spriteRenderer.sprite = newSprite;

        // 影の画像も更新
        if (shadowImage != null)
            shadowImage.sprite = newSprite;
        else if (shadowRenderer != null)
            shadowRenderer.sprite = newSprite;
    }

    void AutoFire()
    {
        timeSinceLastFire += Time.deltaTime;
        if (timeSinceLastFire >= fireInterval)
        {
            Shoot();
            timeSinceLastFire = 0f;
        }
    }

    // 弾の発射位置を左右にずらす幅（UIならピクセル単位、WorldならUnity単位）
    public float bulletOffset = 30f;

    // --- 影の設定 ---
    [Header("Shadow Settings")]
    public Vector3 shadowOffset = new Vector3(10f, -10f, 0f); // 影の位置ズレ
    public Vector3 shadowScale = new Vector3(1f, 1f, 1f);     // 影のサイズ倍率
    public Color shadowColor = new Color(0f, 0f, 0f, 0.5f);   // 影の色（半透明の黒）

    // 影の管理用
    private GameObject shadowObject;
    private Image shadowImage;
    private SpriteRenderer shadowRenderer;

    // --- パワーアップ設定 ---
    [Header("Power Up Settings")]
    public int powerLevel = 0; // 0〜3 (計4段階)
    public GameObject[] funnels; // ファンネル（Inspectorで割り当て）
    
    [Header("Engine Effects")]
    public GameObject[] engineEffects; // プレイヤーとファンネルのSpeedEffectを登録
    public GameObject muzzleFlashPrefab; // 発射時の火花エフェクト
    public Vector3 muzzleFlashOffset = new Vector3(0, 30, 0); // 火花の位置調整（自機）
    public Vector3 funnelMuzzleFlashOffset = new Vector3(0, 30, 0); // 火花の位置調整（ファンネル）

    void Shoot()
    {
        if (bulletPrefab == null) return;

        // レベルに応じた発射パターン
        // Lv0: 1発 (中央)
        // Lv1: 2発 (左右)
        // Lv2: 4発 (左右 + ファンネル2)
        // Lv3: 7発 (左右 + ファンネル4 + 中央)

        // 基本の左右発射 (Lv1以上)
        if (powerLevel >= 1)
        {
            CreateBullet(-bulletOffset);
            CreateBullet(bulletOffset);
        }

        // 中央発射 (Lv0 または Lv3)
        if (powerLevel == 0 || powerLevel >= 3)
        {
             CreateBullet(0f);
        }

        // ファンネル発射 (Lv2以上)
        if (powerLevel >= 2 && funnels != null)
        {
            // ファンネルのリストから、有効なものだけ発射
            // Lv2なら2つ、Lv3なら全部(4つ)と想定
            int activeCount = (powerLevel == 2) ? 2 : 4;
            
            for (int i = 0; i < funnels.Length && i < activeCount; i++)
            {
                if (funnels[i] != null && funnels[i].activeSelf)
                {
                    // ファンネル用のオフセットを使う
                    CreateBulletFromPosition(funnels[i].transform.position, funnelMuzzleFlashOffset);
                }
            }
        }
    }
    
    public void LevelUp()
    {
        // レベルアップ効果としてフラッシュを入れる
        StartCoroutine(FlashWhite());

        if (powerLevel < 3)
        {
            powerLevel++;
            UpdateStats();
            Debug.Log($"Level Up! Current Level: {powerLevel}");
        }
    }

    // --- ダメージ処理 ---
    [Header("Damage Settings")]
    public GameObject explosionPrefab; // プレイヤーの爆発エフェクト

    // 敵または敵の弾に当たった時
    void OnCollisionEnter2D(Collision2D collision)
    {
        CheckDamage(collision.gameObject);
    }
    
    void OnTriggerEnter2D(Collider2D other)
    {
        // Debug.Log($"Player hit: {other.name}"); // デバッグ用
        CheckDamage(other.gameObject);
    }

    void CheckDamage(GameObject other)
    {
        // うっかりTagが設定されていないプロジェクトでエラーになるのを防ぐため、
        // スクリプトの有無だけで判定するように変更
        if (other.GetComponent<EnemyController>() != null)
        {
            ApplyDamage();
            // 敵も倒す（相打ち）
            Destroy(other);
        }
    }

    void ApplyDamage()
    {
        StartCoroutine(FlashWhite()); // 画面フラッシュ

        if (powerLevel > 0)
        {
            powerLevel--;
            UpdateStats();
            Debug.Log($"Damage! Level Down to: {powerLevel}");
        }
        else
        {
            Debug.Log("Player Destroyed!");
            Die();
        }
    }
    
    // 画面を白く光らせるコルーチン（UIモード用）
    System.Collections.IEnumerator FlashWhite()
    {
        if (rectTransform == null) yield break;

        // フラッシュ用の一時的なImageを作成
        GameObject flashObj = new GameObject("DamageFlash");
        flashObj.transform.SetParent(transform.parent, false);
        flashObj.transform.SetAsLastSibling(); // 最前面に表示

        Image img = flashObj.AddComponent<Image>();
        img.color = new Color(1f, 1f, 1f, 0.8f); // 真っ白、不透明度80%
        img.raycastTarget = false; // クリックを邪魔しないように

        // 全画面に広げる
        RectTransform rt = flashObj.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        // フェードアウト
        float duration = 0.2f; // 0.2秒で消える
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float alpha = Mathf.Lerp(0.8f, 0f, t / duration);
            img.color = new Color(1f, 1f, 1f, alpha);
            yield return null;
        }

        Destroy(flashObj);
    }

    void Die()
    {
        if (explosionPrefab != null)
        {
            GameObject exp = Instantiate(explosionPrefab, transform.position, Quaternion.identity);
            if (rectTransform != null)
            {
                exp.transform.SetParent(transform.parent);
                exp.transform.localScale = Vector3.one * 2f;
            }
        }
        
        Destroy(gameObject);
    }

    void UpdateStats()
    {
        // レベルに合わせてファンネルを表示など
        if (funnels == null) return;
        
        int activeCount = 0;
        if (powerLevel == 2) activeCount = 2;
        else if (powerLevel >= 3) activeCount = 4;

        for (int i = 0; i < funnels.Length; i++)
        {
            if (funnels[i] != null)
                funnels[i].SetActive(i < activeCount);
        }
    }

    // 自分の位置からオフセットで生成
    void CreateBullet(float offsetX)
    {
        Vector3 spawnPos = transform.position;
        spawnPos.x += offsetX;
        // プレイヤー用のオフセットを使う
        CreateBulletFromPosition(spawnPos, muzzleFlashOffset);
    }
    
    // 指定されたワールド座標から生成（ファンネル用など）
    void CreateBulletFromPosition(Vector3 position, Vector3 flashOffset)
    {
        GameObject bullet = Instantiate(bulletPrefab, position, Quaternion.identity);

        // マズルフラッシュ生成
        if (muzzleFlashPrefab != null)
        {
            // Debug.Log("Spawning MuzzleFlash"); // デバッグ用
            // 位置調整 (Offset) を加える
            Vector3 flashPos = position + flashOffset;
            
            GameObject flash = Instantiate(muzzleFlashPrefab, flashPos, Quaternion.identity);
            if (rectTransform != null)
            {
                // プレイヤーの動きに追従させるため、親をPlayer(自分自身)にする
                flash.transform.SetParent(transform);
                flash.transform.localScale = Vector3.one; 
                flash.transform.SetAsLastSibling(); 
            }
        }

        if (rectTransform != null)
        {
            bullet.transform.SetParent(transform.parent);
            bullet.transform.localScale = Vector3.one;
            bullet.transform.SetAsLastSibling();
        }
    }

    void CreateShadow()
    {
        // 影用のGameObject作成
        shadowObject = new GameObject("PlayerShadow");
        
        // プレイヤーと同じ親にする（Canvas内なら必須、Worldでも整理のため）
        shadowObject.transform.SetParent(transform.parent);
        
        // スケール初期設定（UpdateShadowでも更新するが、初期化時にも適用）
        shadowObject.transform.localScale = Vector3.Scale(transform.localScale, shadowScale);
        shadowObject.transform.rotation = transform.rotation;

        // UIモードかどうかでコンポーネントを追加
        if (rectTransform != null)
        {
            // RectTransformを追加・設定
            RectTransform shadowRect = shadowObject.AddComponent<RectTransform>();
            shadowRect.sizeDelta = rectTransform.sizeDelta;
            
            shadowImage = shadowObject.AddComponent<Image>();
            shadowImage.sprite = uiImage.sprite;
            shadowImage.color = shadowColor;
            
            // プレイヤーより後ろに表示（兄弟インデックスをプレイヤーより小さくする）
            shadowObject.transform.SetSiblingIndex(transform.GetSiblingIndex());
        }
        else
        {
            shadowRenderer = shadowObject.AddComponent<SpriteRenderer>();
            shadowRenderer.sprite = spriteRenderer.sprite;
            shadowRenderer.color = shadowColor;
            
            // ソーティングレイヤーなどはプレイヤーに合わせるか、背景寄り設定が必要
            // ここではOrderInLayerをプレイヤーより1つ下げる
            shadowRenderer.sortingLayerID = spriteRenderer.sortingLayerID;
            shadowRenderer.sortingOrder = spriteRenderer.sortingOrder - 1;
        }
    }

    void UpdateShadow()
    {
        if (shadowObject == null) return;

        // 位置を追従
        shadowObject.transform.position = transform.position + shadowOffset;
        
        // スケールを追従（Playerのスケール * shadowScale）
        // ランタイムで調整できるようにここで毎フレーム更新
        shadowObject.transform.localScale = Vector3.Scale(transform.localScale, shadowScale);
    }
}
