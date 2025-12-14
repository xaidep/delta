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
        
        // 画像コンポーネントを取得しておく
        if (rectTransform != null)
            uiImage = GetComponent<Image>();
        else
            spriteRenderer = GetComponent<SpriteRenderer>();

        lastPosition = transform.position;

        CreateShadow(); // 影を作成
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

        // UIモード (Canvas内) かどうかで処理を分ける
        if (rectTransform != null)
        {
            // --- UIモード (Canvas Overlay) ---
            transform.position = Input.mousePosition;
        }
        else
        {
            // --- 2D Sprite モード (World Space) ---
            Vector3 mousePos = Input.mousePosition;
            mousePos.z = 10f; 
            Vector3 worldPos = Camera.main.ScreenToWorldPoint(mousePos);
            worldPos.z = 0f;
            transform.position = worldPos;
        }
        
        currentPos = transform.position; // 移動後の位置

        // --- 移動方向による画像の切り替え ---
        float deltaX = currentPos.x - lastPosition.x;
        
        // 感度調整（World座標系だと小さくなるので、少し小さめに設定）
        // UIモード（ピクセル単位）ならこれでも十分反応します
        float threshold = 0.01f; 

        if (deltaX < -threshold)
        {
            // 左移動
            ChangeSprite(imageLeft);
            stationaryTimer = 0f; // 動いているのでタイマーリセット
        }
        else if (deltaX > threshold)
        {
            // 右移動
            ChangeSprite(imageRight);
            stationaryTimer = 0f; // 動いているのでタイマーリセット
        }
        else
        {
            // ほぼ静止している場合
            // すぐに正面に戻すとパタパタしてしまうため、
            // 「しばらく止まっていたら」正面に戻すようにする
            stationaryTimer += Time.deltaTime;
            
            // 0.1秒以上止まっていたら正面に戻す
            if (stationaryTimer > 0.1f)
            {
                ChangeSprite(imageCenter);
            }
        }

        lastPosition = currentPos;
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

    void Shoot()
    {
        if (bulletPrefab == null) return;

        // 左の翼から発射
        CreateBullet(-bulletOffset);
        // 右の翼から発射
        CreateBullet(bulletOffset);
    }

    void CreateBullet(float offsetX)
    {
        // 発射位置を計算（今の位置 + X方向のズレ）
        Vector3 spawnPos = transform.position;
        spawnPos.x += offsetX;

        // 弾を生成
        GameObject bullet = Instantiate(bulletPrefab, spawnPos, Quaternion.identity);

        // UIモードの場合、親要素（Canvasなど）を正しく設定しないと表示されない場合がある
        if (rectTransform != null)
        {
            // プレイヤーと同じ親（例: Canvas）の子にする
            bullet.transform.SetParent(transform.parent);
            // スケールがおかしくなることがあるので1にリセット
            bullet.transform.localScale = Vector3.one;
            // プレイヤーより手前に表示されるように順序調整（任意）
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
