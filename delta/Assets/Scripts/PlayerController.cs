using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [FormerlySerializedAs("bulletPrefab")] public GameObject 弾のプレハブ;
    public GameObject 斜め弾_左プレハブ;
    public GameObject 斜め弾_右プレハブ;
    public GameObject 弾_Lv6プレハブ;
    public GameObject 弾_Lv7プレハブ;
    [FormerlySerializedAs("fireInterval")] public float 発射間隔 = 0.2f;

    [Header("Bullet Scale Settings")]
    public float bulletScaleLv6 = 1.0f; // Level 6 (Bullet_3) の大きさ
    public float bulletScaleLv7 = 1.0f; // Level 7 (Bullet_4) の大きさ

    [Header("Player Settings")]
    public float 自機の大きさ倍率 = 1.5f;
    public float 当たり判定の基本サイズ = 50.0f;
    [Range(0.1f, 1.0f)] public float 当たり判定の縮小率 = 0.8f; // 見た目に対してどの程度の大きさにするか

    // --- アニメーション用画像 ---
    [Header("Animation Sprites")]
    [FormerlySerializedAs("imageCenter")] public Sprite 画像_中央; // 正面
    [FormerlySerializedAs("imageLeft")] public Sprite 画像_左;   // 左
    [FormerlySerializedAs("imageRight")] public Sprite 画像_右;  // 右

    private float timeSinceLastFire = 0f;
    private RectTransform rectTransform;
    private Image uiImage;            // UI用の場合の画像コンポーネント
    private SpriteRenderer spriteRenderer; // 2D用の場合の画像コンポーネント
    
    // 移動量を計算するための前回の位置
    private Vector3 lastPosition;

    private Camera mainCamera;

    // キャッシュ用
    private RectTransform parentRect;
    private Canvas rootCanvas;

    [Header("HP表示設定 (Hierarchy上のオブジェクトを使用)")]
    public GameObject HP表示オブジェクト;
    public Image HPゲージ画像;
    public int 最大HP = 10;
    private int 現在のHP;
    private Image hpFillImage;

    public static PlayerController instance;

    void Awake()
    {
        if (instance == null) instance = this;
    }

    void Start()
    {
        // カメラの取得
        mainCamera = Camera.main;
        if (mainCamera == null)
        {
#if UNITY_2023_1_OR_NEWER
            mainCamera = Object.FindFirstObjectByType<Camera>();
#else
            mainCamera = Object.FindObjectOfType<Camera>();
#endif
        }

        rectTransform = GetComponent<RectTransform>();
        
        // UI移動用のキャッシュ
        if (rectTransform != null)
        {
            if (transform.parent != null)
                parentRect = transform.parent.GetComponent<RectTransform>();
            
            rootCanvas = GetComponentInParent<Canvas>();
        }
        
        // マウス位置の初期化（中央）
        lastValidMousePos = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);

        // 自機の大きさを適用
        transform.localScale = Vector3.one * 自機の大きさ倍率;

        // 当たり判定の設定
        UpdateColliderSize();

        // Trigger判定にはRigidbodyが必要
        if (GetComponent<Rigidbody2D>() == null)
        {
            var rb = gameObject.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.gravityScale = 0f;
        }
        
        // 画像コンポーネントを取得
        if (rectTransform != null) uiImage = GetComponent<Image>();
        else spriteRenderer = GetComponent<SpriteRenderer>();
            
        lastPosition = transform.position;

        CreateShadow(); // 影を作成
        CreateHPBar();  // HPバーの作成
        UpdateStats();  // 初期状態の反映
    }

    private void UpdateColliderSize()
    {
        BoxCollider2D col = GetComponent<BoxCollider2D>();
        if (col == null) 
        {
            col = gameObject.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
        }
        float finalSize = 当たり判定の基本サイズ * 自機の大きさ倍率 * 当たり判定の縮小率;
        if (rectTransform != null) col.size = new Vector2(finalSize, finalSize);
    }

    void CreateHPBar()
    {
        現在のHP = 最大HP;

        // 1. 直接指定されているか確認
        if (HP表示オブジェクト == null)
        {
            // 2. なければ名前で探す
            HP表示オブジェクト = GameObject.Find("PlayerHPBar");
        }

        if (HP表示オブジェクト != null)
        {
            // 非表示になっていたら表示する
            HP表示オブジェクト.SetActive(true);

            // ゲージ用Imageの特定
            if (HPゲージ画像 != null)
            {
                hpFillImage = HPゲージ画像;
            }
            else
            {
                // なければ子要素から探す
                Image[] images = HP表示オブジェクト.GetComponentsInChildren<Image>();
                foreach (var img in images)
                {
                    if (img.type == Image.Type.Filled)
                    {
                        hpFillImage = img;
                        break;
                    }
                }
            }
        }
        else
        {
            Debug.LogWarning("PlayerHP: Hierarchy上に 'PlayerHPBar' が見つかりません。");
        }

        UpdateHPBarUI();
    }

    void OnDestroy()
    {
        if (shadowObject != null) Destroy(shadowObject);
    }

    void UpdateHPBarUI()
    {
        if (hpFillImage != null)
        {
            hpFillImage.fillAmount = Mathf.Clamp01((float)現在のHP / 最大HP);
        }
    }

    // --- 移動制御用の変数 ---
    private Vector2 lastValidMousePos; // 最後に確認された正常なマウス座標

    void Update()
    {
        MovePlayer();
        AutoFire();
        UpdateShadow(); // 影の位置更新
    }

    // マウス位置の最終追従座標（スクリーン空間）
    private Vector2 currentTrackingPos;

    void OnGUI()
    {
        // OnGUIはクリックに関わらずマウス位置イベントを受信できるため、最優先で使用
        if (Event.current != null)
        {
            Vector2 guiPos = Event.current.mousePosition;
            // OnGUI座標(左上が0) → スクリーン座標(左下が0)へ変換
            currentTrackingPos = new Vector2(guiPos.x, Screen.height - guiPos.y);
        }
    }

    void MovePlayer()
    {
        // --- 1. マウス/タッチ座標の取得と徹底的なクランプ (複数のソースから取得) ---
        // 基本ソース1: New Input System (最も堅牢でクリックに強い)
        Vector3 rawPos = currentTrackingPos;
        if (Mouse.current != null)
        {
            rawPos = Mouse.current.position.ReadValue();
        }
        else
        {
            // 基本ソース2: Legacy Input (Input Systemがない環境用)
            Vector3 legacyPos = Input.mousePosition;
            // 画面外でも追従するように、値が取得できるなら採用
            if (legacyPos != Vector3.zero || rawPos == Vector3.zero) 
            {
                rawPos = legacyPos;
            }
        }

        // 基本ソース3: OnGUI (SimulatorやEditor特有の挙動向けの最終バックアップ)
        // もしソース1,2が止まっていて、OnGUIが動いているならそちらを採用
        if (currentTrackingPos != Vector2.zero && (rawPos == Vector3.zero))
        {
            rawPos = currentTrackingPos;
        }

        // 徹底的なクランプ (境界ガードの核)
        // マウスが画面外へ出ても、値を 0〜Screenサイズ に固定することで「淵に吸い付く」挙動を実現します
        float clampedX = Mathf.Clamp(rawPos.x, 0f, Screen.width);
        float clampedY = Mathf.Clamp(rawPos.y, 0f, Screen.height);
        Vector3 targetScreenPos = new Vector3(clampedX, clampedY, 0f);

        // --- 2. 座標変換と物理的な移動 ---
        if (rectTransform != null && parentRect != null && rootCanvas != null)
        {
            // UIモード (Canvas内)
            Vector2 localPoint;
            Camera cam = (rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay) ? null : mainCamera;
            
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, targetScreenPos, cam, out localPoint))
            {
                // NaNチェック
                if (float.IsNaN(localPoint.x) || float.IsNaN(localPoint.y)) return;

                // 境界ガード：見た目ギリギリまで行けるように自機の「半径」分だけマージンを設ける
                // 0.5f（ちょうど半分）にすることで、機体の端が画面端にピタッと止まります
                float hW = (rectTransform.rect.width * 自機の大きさ倍率) * 0.5f;
                float hH = (rectTransform.rect.height * 自機の大きさ倍率) * 0.5f;

                float minX = parentRect.rect.xMin + hW;
                float maxX = parentRect.rect.xMax - hW;
                float minY = parentRect.rect.yMin + hH;
                float maxY = parentRect.rect.yMax - hH;

                localPoint.x = Mathf.Clamp(localPoint.x, minX, maxX);
                localPoint.y = Mathf.Clamp(localPoint.y, minY, maxY);

                rectTransform.anchoredPosition = localPoint;
            }
        }
        else if (rectTransform == null && mainCamera != null)
        {
            // 2D Sprite モード (World Space)
            Vector3 worldPos = mainCamera.ScreenToWorldPoint(new Vector3(targetScreenPos.x, targetScreenPos.y, 10f));
            worldPos.z = 0f;

            // Viewport範囲 (0〜1) でクランプして絶対端を維持
            Vector3 viewportPos = mainCamera.WorldToViewportPoint(worldPos);
            viewportPos.x = Mathf.Clamp01(viewportPos.x);
            viewportPos.y = Mathf.Clamp01(viewportPos.y);
            
            transform.position = mainCamera.ViewportToWorldPoint(viewportPos);
        }

        // --- 3. アニメーション制御 ---
        Vector3 currentPos = transform.position;
        float deltaX = currentPos.x - lastPosition.x;
        float deltaY = currentPos.y - lastPosition.y; 
        
        float threshold = 0.01f; 
        ControlEngineEffectState(deltaY > threshold);

        if (deltaX < -threshold) { ChangeSprite(画像_左); stationaryTimer = 0f; }
        else if (deltaX > threshold) { ChangeSprite(画像_右); stationaryTimer = 0f; }
        else
        {
            stationaryTimer += Time.deltaTime;
            if (stationaryTimer > 0.1f) ChangeSprite(画像_中央);
        }

        lastPosition = currentPos;
    }

    // メソッド名の重複を避けるための微調整
    void ControlEngineEffectState(bool isActive)
    {
        ControlEngineEffects(isActive);
    }

    // エンジンエフェクトの表示切り替え
    void ControlEngineEffects(bool isActive)
    {
        if (エンジンエフェクト == null) return;
        foreach (var effect in エンジンエフェクト)
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
        if (timeSinceLastFire >= 発射間隔)
        {
            Shoot();
            timeSinceLastFire = 0f;
        }
    }

    // 弾の発射位置を左右にずらす幅（UIならピクセル単位、WorldならUnity単位）
    [FormerlySerializedAs("bulletOffset")] public float 弾の発射位置オフセット = 30f;

    // --- 影の設定 ---
    [Header("Shadow Settings")]
    [FormerlySerializedAs("shadowOffset")] public Vector3 影の位置オフセット = new Vector3(10f, -10f, 0f); // 影の位置ズレ
    [FormerlySerializedAs("shadowScale")] public Vector3 影の拡大率 = new Vector3(1f, 1f, 1f);     // 影のサイズ倍率
    [FormerlySerializedAs("shadowColor")] public Color 影の色 = new Color(0f, 0f, 0f, 0.5f);   // 影の色（半透明の黒）

    // 影の管理用
    private GameObject shadowObject;
    private Image shadowImage;
    private SpriteRenderer shadowRenderer;

    // --- パワーアップ設定 ---
    [Header("Power Up Settings")]
    [FormerlySerializedAs("powerLevel")] public int 現在のパワーレベル = 0; // 0〜3 (計4段階)
    [FormerlySerializedAs("funnels")] public GameObject[] ファンネルリスト; // ファンネル（Inspectorで割り当て）
    [FormerlySerializedAs("powerUpUIPrefab")] public GameObject パワーアップ演出プレハブ; // パワーアップ時の演出プレハブ

    [Header("Engine Effects")]
    [FormerlySerializedAs("engineEffects")] public GameObject[] エンジンエフェクト; // プレイヤーとファンネルのSpeedEffectを登録
    [FormerlySerializedAs("muzzleFlashPrefab")] public GameObject 発射エフェクトプレハブ; // 発射時の火花エフェクト
    [FormerlySerializedAs("muzzleFlashOffset")] public Vector3 発射エフェクト位置オフセット = new Vector3(0, 30, 0); // 火花の位置調整（自機）
    [FormerlySerializedAs("funnelMuzzleFlashOffset")] public Vector3 ファンネル発射エフェクト位置オフセット = new Vector3(0, 30, 0); // 火花の位置調整（ファンネル）

    void Shoot()
    {
        if (rectTransform == null) return;

        // SE再生
        if (AudioManager.instance != null)
        {
            AudioManager.instance.PlayPlayerShoot();
        }

        // レベルに応じた攻撃力 (Lv6: 2倍, Lv7: 3倍)
        int currentDamage = 1;
        if (現在のパワーレベル >= 6) currentDamage = 3;
        else if (現在のパワーレベル >= 5) currentDamage = 2;

        // レベルに応じてメインの弾プレハブを選択
        GameObject prefabToUse = 弾のプレハブ;
        if (現在のパワーレベル >= 6 && 弾_Lv7プレハブ != null) prefabToUse = 弾_Lv7プレハブ;
        else if (現在のパワーレベル >= 5 && 弾_Lv6プレハブ != null) prefabToUse = 弾_Lv6プレハブ;

        if (prefabToUse == null) 
        {
            Debug.LogError("[PlayerController] No Main Bullet Prefab assigned!");
            return;
        }

        // --- レベル7 特別パターン (添付画像に基づく扇形) ---
        if (現在のパワーレベル >= 6)
        {
            Vector3 centerPos = rectTransform.localPosition;
            float shootY = centerPos.y + 弾の発射位置オフセット;

            // 1. 中央: 大リング (Bullet_4)
            CreateBulletUniversal(弾_Lv7プレハブ, new Vector3(centerPos.x, shootY, 0f), true, 発射エフェクト位置オフセット, currentDamage, bulletScaleLv7);

            // 2. 内側斜め: 三日月弾 (Bullet_2)
            if (斜め弾_左プレハブ != null && 斜め弾_右プレハブ != null)
            {
                CreateDiagonalBullet(transform.TransformPoint(new Vector3(-弾の発射位置オフセット * 1.5f, 弾の発射位置オフセット, 0f)), 斜め弾_左プレハブ, 20f, currentDamage);
                CreateDiagonalBullet(transform.TransformPoint(new Vector3(弾の発射位置オフセット * 1.5f, 弾の発射位置オフセット, 0f)), 斜め弾_右プレハブ, -20f, currentDamage);
            }

            // 3. 外側斜め: 小リング (Bullet_3)
            if (弾_Lv6プレハブ != null)
            {
                CreateDiagonalBullet(transform.TransformPoint(new Vector3(-弾の発射位置オフセット * 3f, 弾の発射位置オフセット, 0f)), 弾_Lv6プレハブ, 40f, currentDamage, bulletScaleLv6);
                CreateDiagonalBullet(transform.TransformPoint(new Vector3(弾の発射位置オフセット * 3f, 弾の発射位置オフセット, 0f)), 弾_Lv6プレハブ, -40f, currentDamage, bulletScaleLv6);
            }

            // 4. ファンネルからも発射 (Lv7なら全4機)
            if (ファンネルリスト != null)
            {
                for (int i = 0; i < ファンネルリスト.Length && i < 4; i++)
                {
                    if (ファンネルリスト[i] != null && ファンネルリスト[i].activeSelf)
                    {
                        CreateBulletUniversal(弾_Lv7プレハブ, ファンネルリスト[i].transform.position, false, ファンネル発射エフェクト位置オフセット, currentDamage, bulletScaleLv7);
                    }
                }
            }
            return; // Lv7パターン完了
        }

        // --- 通常のレベルアップパターン (Lv0-6) ---
        float currentScale = 1.0f;
        if (現在のパワーレベル == 5) currentScale = bulletScaleLv6;

        // 基本の左右発射 (Lv1以上)
        if (現在のパワーレベル >= 1)
        {
            Vector3 centerPos = rectTransform.localPosition;
            CreateBulletUniversal(prefabToUse, centerPos + new Vector3(-弾の発射位置オフセット, 弾の発射位置オフセット, 0f), true, 発射エフェクト位置オフセット, currentDamage, currentScale);
            CreateBulletUniversal(prefabToUse, centerPos + new Vector3(弾の発射位置オフセット, 弾の発射位置オフセット, 0f), true, 発射エフェクト位置オフセット, currentDamage, currentScale);
        }

        // 中央発射 (Lv0 または Lv3以上)
        if (現在のパワーレベル == 0 || 現在のパワーレベル >= 3)
        {
             CreateBulletUniversal(prefabToUse, rectTransform.localPosition + new Vector3(0f, 弾の発射位置オフセット, 0f), true, 発射エフェクト位置オフセット, currentDamage, currentScale);
        }

        // ファンネル発射 (Lv2以上)
        if (現在のパワーレベル >= 2 && ファンネルリスト != null)
        {
            int activeCount = (現在のパワーレベル == 2) ? 2 : 4;
            
            for (int i = 0; i < ファンネルリスト.Length && i < activeCount; i++)
            {
                if (ファンネルリスト[i] != null && ファンネルリスト[i].activeSelf)
                {
                    CreateBulletUniversal(prefabToUse, ファンネルリスト[i].transform.position, false, ファンネル発射エフェクト位置オフセット, currentDamage, currentScale);
                }
            }
        }

        // 斜め弾のロジック (レベル5, 6)
        if ((現在のパワーレベル == 4 || 現在のパワーレベル == 5) && 斜め弾_左プレハブ != null && 斜め弾_右プレハブ != null)
        {
            if (ファンネルリスト != null && ファンネルリスト.Length >= 4 && 
                ファンネルリスト[3] != null && ファンネルリスト[3].activeSelf &&
                ファンネルリスト[2] != null && ファンネルリスト[2].activeSelf)
            {
                CreateDiagonalBullet(ファンネルリスト[3].transform.position, 斜め弾_左プレハブ, 45f, currentDamage);
                CreateDiagonalBullet(ファンネルリスト[2].transform.position, 斜め弾_右プレハブ, -45f, currentDamage);
            }
        }
    }

    // 汎用的な弾生成ヘルパー
    void CreateBulletUniversal(GameObject prefab, Vector3 positionOrLocal, bool isLocal, Vector3 flashOffset = default, int damage = 1, float scaleOverride = 1.0f)
    {
        if (prefab == null) return;
        
        GameObject bullet;
        if (isLocal)
        {
            bullet = Instantiate(prefab, transform.parent);
            bullet.transform.localPosition = positionOrLocal;
        }
        else
        {
            bullet = Instantiate(prefab, positionOrLocal, Quaternion.identity);
            bullet.transform.SetParent(transform.parent);
        }

        // プレハブの元のスケールを尊重しつつ、倍率を適用する
        bullet.transform.localScale = prefab.transform.localScale * scaleOverride;
        bullet.transform.SetAsLastSibling();
        
        // Z座標を0に固定（UIモード用）
        Vector3 pos = bullet.transform.localPosition;
        pos.z = 0;
        bullet.transform.localPosition = pos;

        // マズルフラッシュ生成
        if (発射エフェクトプレハブ != null)
        {
            // 弾のワールド位置に生成
            GameObject flash = Instantiate(発射エフェクトプレハブ, bullet.transform.position, bullet.transform.rotation);
            // プレイヤーに追従させる
            flash.transform.SetParent(transform);
            flash.transform.localScale = Vector3.one;
            // 指定されたオフセットをローカル座標で適用
            flash.transform.localPosition += flashOffset;
            flash.transform.SetAsLastSibling();
        }

        // 弾のスクリプト (これがないと動かない)
        BulletController bc = bullet.GetComponent<BulletController>();
        if (bc == null)
        {
            bc = bullet.AddComponent<BulletController>();
        }
        bc.damage = damage;
    }

    // 斜め弾生成用のヘルパー
    void CreateDiagonalBullet(Vector3 position, GameObject prefab, float angle, int damage = 1, float scaleOverride = 1.0f)
    {
        if (prefab == null) return;
        
        // Canvasの親を直接指定して生成
        GameObject bullet = Instantiate(prefab, transform.parent);
        bullet.name = "DiagonalBullet_" + prefab.name;

        if (rectTransform != null)
        {
            // 位置をワールド座標で合わせる
            bullet.transform.position = position;
            
            // UI表示のためのZ座標補正
            Vector3 lPos = bullet.transform.localPosition;
            lPos.z = 0;
            bullet.transform.localPosition = lPos;
            
            // プレハブの元のスケールを尊重しつつ、倍率を適用する
            bullet.transform.localScale = prefab.transform.localScale * scaleOverride;
            
            // レイヤーを合わせる
            bullet.layer = gameObject.layer;

            // 最前面へ
            bullet.transform.SetAsLastSibling();

            // マズルフラッシュ生成 (斜め弾用)
            if (発射エフェクトプレハブ != null)
            {
                GameObject flash = Instantiate(発射エフェクトプレハブ, bullet.transform.position, bullet.transform.rotation);
                flash.transform.SetParent(transform);
                flash.transform.localScale = Vector3.one;
                // ファンネル用のオフセットを適用
                flash.transform.localPosition += ファンネル発射エフェクト位置オフセット;
                flash.transform.SetAsLastSibling();
            }
        }
        
        // 角度を設定 (進行方向を向かせる)
        bullet.transform.rotation = Quaternion.Euler(0, 0, angle);

        // スクリプト付与
        BulletController bc = bullet.GetComponent<BulletController>();
        if (bc == null) bc = bullet.AddComponent<BulletController>();
        
        bc.damage = damage;
        // 斜め弾は通常の弾(1200)より少し速く設定して爽快感を出す
        bc.bulletSpeed = 1400f;

        bullet.SetActive(true);
        // Debug.Log($"[PlayerController] CreateDiagonalBullet Success: {prefab.name}, Damage: {damage}, Angle: {angle}");
    }

    public void LevelUp()
    {
        // レベルアップ効果のフラッシュは削除 (ユーザー要望)
        // StartCoroutine(FlashWhite());

        if (現在のパワーレベル < 6)
        {
            現在のパワーレベル++;
            UpdateStats();
            Debug.Log($"Level Up! Current Level: {現在のパワーレベル}");
            
            // パワーアップ演出 (UI) を表示 (コルーチンで停止処理を含む)
            StartCoroutine(ShowPowerUpSequence());

            // SE再生
            if (AudioManager.instance != null)
            {
                AudioManager.instance.PlayPlayerPowerUp();
            }
        }
    }

    System.Collections.IEnumerator ShowPowerUpSequence()
    {
        if (パワーアップ演出プレハブ != null && rectTransform != null)
        {
            // Canvas内に生成
            GameObject uiObj = Instantiate(パワーアップ演出プレハブ, transform.parent);
            
            // 画面中央に配置
            RectTransform rt = uiObj.GetComponent<RectTransform>();
            if (rt != null) rt.anchoredPosition = Vector2.zero;
            else uiObj.transform.localPosition = Vector3.zero;
            
            // テキスト更新
            TextMeshProUGUI tmp = uiObj.GetComponentInChildren<TextMeshProUGUI>();
            if (tmp != null)
            {
                tmp.text = (現在のパワーレベル + 1).ToString("D2");
            }

            // --- ゲーム停止処理 ---
            
            // 1. 時間を止める
            Time.timeScale = 0f;

            // 2. 演出のアニメーションが止まらないように設定変更 (すべてのアニメーターに適用)
            Animator[] anims = uiObj.GetComponentsInChildren<Animator>();
            foreach (Animator anim in anims)
            {
                anim.updateMode = AnimatorUpdateMode.UnscaledTime;
            }

            // 3. リアルタイムで0.5秒待つ (さらに短縮)
            yield return new WaitForSecondsRealtime(0.5f);

            // 4. 演出を削除
            Destroy(uiObj);

            // 5. 時間を動かす
            Time.timeScale = 1f;
        }
    }

    // --- ダメージ処理 ---
    [Header("Damage Settings")]
    [FormerlySerializedAs("explosionPrefab")] public GameObject 爆発エフェクト; // プレイヤーの爆発エフェクト

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
        // 敵の弾に当たった場合
        else if (other.GetComponent<EnemyBulletController>() != null)
        {
            ApplyDamage();
            Destroy(other); // 弾を消す
        }
    }

    void ApplyDamage()
    {
        StartCoroutine(FlashWhite()); // 画面フラッシュ

        現在のHP--;
        UpdateHPBarUI();
        Debug.Log($"Damage! HP: {現在のHP}/{最大HP}");

        if (現在のHP <= 0)
        {
            Debug.Log("Player Destroyed!");
            Die();
        }
        else if (現在のパワーレベル > 0)
        {
            // パワーダウンのロジックは残しておくが現行要件ではHP優先
            現在のパワーレベル--;
            UpdateStats();
            Debug.Log($"Level Down to: {現在のパワーレベル}");
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

    [Header("Game Over Settings")]
    public GameObject ゲームオーバー演出プレハブ;

    void Die()
    {
        if (爆発エフェクト != null)
        {
            GameObject exp = Instantiate(爆発エフェクト, transform.position, Quaternion.identity);
            if (rectTransform != null)
            {
                exp.transform.SetParent(transform.parent);
                exp.transform.localScale = Vector3.one * 2f;
            }
        }
        
        // ゲームオーバー演出の生成
        if (ゲームオーバー演出プレハブ != null && rectTransform != null)
        {
            // Canvas内に生成
            GameObject go = Instantiate(ゲームオーバー演出プレハブ, transform.parent);
            go.transform.SetAsLastSibling(); // 最前面に
        }

        Destroy(gameObject);
    }

    void UpdateStats()
    {
        // レベルに合わせてファンネルを表示など
        if (ファンネルリスト == null) return;
        
        int activeCount = 0;
        if (現在のパワーレベル == 2) activeCount = 2;
        else if (現在のパワーレベル >= 3) activeCount = 4;

        Debug.Log($"[PlayerController] UpdateStats called. Level: {現在のパワーレベル}, Active Funnels: {activeCount}");

        for (int i = 0; i < ファンネルリスト.Length; i++)
        {
            if (ファンネルリスト[i] != null)
            {
                ファンネルリスト[i].SetActive(i < activeCount);
                // Debug.Log($"[PlayerController] Funnel[{i}] set to {i < activeCount}");
            }
        }
    }

    // 自分の位置からオフセットで生成
    void CreateBullet(float offsetX)
    {
        Vector3 spawnPos = transform.position;
        spawnPos.x += offsetX;
        // プレイヤー用のオフセットを使う
        CreateBulletFromPosition(spawnPos, 発射エフェクト位置オフセット);
    }
    
    // 指定されたワールド座標から生成（ファンネル用など）
    void CreateBulletFromPosition(Vector3 position, Vector3 flashOffset)
    {
        GameObject bullet = Instantiate(弾のプレハブ, position, Quaternion.identity);

        // マズルフラッシュ生成
        if (発射エフェクトプレハブ != null)
        {
            // Debug.Log("Spawning MuzzleFlash"); // デバッグ用
            // 位置調整 (Offset) を加える
            Vector3 flashPos = position + flashOffset;
            
            GameObject flash = Instantiate(発射エフェクトプレハブ, flashPos, Quaternion.identity);
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

        // 弾のスクリプトが付いていない場合は動的に追加する (Bullet_1などに対応)
        if (bullet.GetComponent<BulletController>() == null)
        {
            bullet.AddComponent<BulletController>();
        }
    }

    void CreateShadow()
    {
        // 影用のGameObject作成
        shadowObject = new GameObject("PlayerShadow");
        
        // プレイヤーと同じ親にする（Canvas内なら必須、Worldでも整理のため）
        shadowObject.transform.SetParent(transform.parent);
        
        // スケール初期設定（UpdateShadowでも更新するが、初期化時にも適用）
        shadowObject.transform.localScale = Vector3.Scale(transform.localScale, 影の拡大率);
        shadowObject.transform.rotation = transform.rotation;

        // UIモードかどうかでコンポーネントを追加
        if (rectTransform != null)
        {
            // RectTransformを追加・設定
            RectTransform shadowRect = shadowObject.AddComponent<RectTransform>();
            shadowRect.sizeDelta = rectTransform.sizeDelta;
            
            shadowImage = shadowObject.AddComponent<Image>();
            shadowImage.sprite = uiImage.sprite;
            shadowImage.color = 影の色;
            
            // プレイヤーより後ろに表示（兄弟インデックスをプレイヤーより小さくする）
            shadowObject.transform.SetSiblingIndex(transform.GetSiblingIndex());
        }
        else
        {
            shadowRenderer = shadowObject.AddComponent<SpriteRenderer>();
            shadowRenderer.sprite = spriteRenderer.sprite;
            shadowRenderer.color = 影の色;
            
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
        shadowObject.transform.position = transform.position + 影の位置オフセット;
        
        // スケールを追従（Playerのスケール * 影の拡大率）
        // ランタイムで調整できるようにここで毎フレーム更新
        shadowObject.transform.localScale = Vector3.Scale(transform.localScale, 影の拡大率);
    }
}
