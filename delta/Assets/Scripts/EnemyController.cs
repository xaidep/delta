using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI; // UIのImageを操作するために必要

public class EnemyController : MonoBehaviour
{
    [Header("Movement Settings")]
    [FormerlySerializedAs("speed")] public float 移動速度 = 5f;
    [FormerlySerializedAs("enableRotation")] public bool 回転を有効にする = false; // 回転を無効化できるようにする
    [FormerlySerializedAs("rotateSpeed")] public float 回転速度 = 360f; // 1秒あたりの回転角度

    [Header("Animation Settings")]
    [FormerlySerializedAs("animationFrames")] public Sprite[] アニメーション画像リスト; // 連番画像
    [FormerlySerializedAs("framesPerSecond")] public float アニメーション速度FPS = 10f; // アニメーション速度

    [Header("Explosion Settings")]
    [FormerlySerializedAs("explosionPrefab")] public GameObject 爆発エフェクト;

    [Header("Size Settings")]
    public float 敵の大きさ倍率 = 1.0f; // ★これはScaleに使用
    public float 表示サイズX = 64f;    // ★これはRectTransformの幅に使用
    public float 表示サイズY = 64f;    // ★これはRectTransformの高さに使用

    [Header("Boss Settings")]
    [FormerlySerializedAs("isBoss")] public bool ボス設定 = false; // ボスフラグ
    [FormerlySerializedAs("bossLifeTime")] public float ボス滞在時間 = 20f; // ボスの滞在時間
    [FormerlySerializedAs("bossHoverWidth")] public float ボス左右移動幅 = 200f; // 左右移動の幅
    [FormerlySerializedAs("bossHoverSpeed")] public float ボス移動速度 = 1.0f; // 左右移動の速さ
    
    private bool isBossActive = false; // ボスモード（移動完了後）
    private Vector3 bossBasePos; // 左右移動の基準点
    private float spiralAngle = 0f; // 螺旋弾用角度
    private float bossTimer = 0f; // ボス動作用タイマー
    private bool shouldFlip = false; // 左から登場する場合に反転
    private bool isDying = false; // 撃破演出中フラグ


    private bool isInitialized = false;
    private bool wasDefeated = false; // プレイヤーに倒されたか
    private EnemySpawner spawner; // 生みの親
    private float t = 0f; // ベジェ曲線用のパラメータ (0.0 ～ 1.0)
    private Image targetImage; // アニメーションさせる対象 (UI用)
    private SpriteRenderer targetSpriteRenderer; // アニメーションさせる対象 (Sprite用)
    private float animationTimer = 0f;
    private int currentFrameIndex = 0;
    
    [Header("HP Settings")]
    public int 最大HP = 2;
    public GameObject HPのプレハブ;
    public Vector3 HPバーの位置オフセット = new Vector3(0, -60, 0);
    private int 現在のHP;
    private GameObject hpBarObject;
    private Image hpFillImage;
    private Coroutine hpVisibilityCoroutine;

    // ベジェ曲線のための制御点
    private Vector3 p0, p1, p2;

    void Awake()
    {
        targetImage = GetComponent<Image>();
        targetSpriteRenderer = GetComponent<SpriteRenderer>();
    }

    public void Initialize(Vector3 start, Vector3 control, Vector3 end, EnemySpawner owner, bool flip)
    {
        p0 = start;
        p1 = control;
        p2 = end;
        spawner = owner;
        
        transform.localPosition = p0;
        isInitialized = true;
        t = 0f;
        shouldFlip = flip; // Spawnerから渡された反転フラグをセット
        
        Debug.Log($"Enemy Spawned at X: {p0.x}, Flipping: {shouldFlip}");
    }

    [Header("Attack Settings")]
    [FormerlySerializedAs("bulletPrefab")] public GameObject 弾のプレハブ;
    [FormerlySerializedAs("fireInterval")] public float 発射間隔 = 2.0f;
    [FormerlySerializedAs("bulletScale")] public float 弾の大きさ倍率 = 0.3f; // 弾の大きさ調整用
    private float fireTimer;
    private Transform playerTransform;

    void Start()
    {
        // サイズを強制設定 (インスペクターで指定した値)
        RectTransform rt = GetComponent<RectTransform>();
        if (rt != null)
        {
            rt.sizeDelta = new Vector2(表示サイズX, 表示サイズY);
        }

        // プレイヤーを探しておく（負荷軽減のためStartで）
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            playerTransform = playerObj.transform;
        }
        else
        {
            // 念のため、キャッシュせずにShootで毎回探すフラグなどを立てるか、
            // FindObjectOfType<PlayerController>() で探してみる
#if UNITY_2023_1_OR_NEWER
            var pc = FindFirstObjectByType<PlayerController>();
#else
            var pc = FindObjectOfType<PlayerController>();
#endif
            if (pc != null)
            {
               playerTransform = pc.transform;
            }
        }
        
        // 最初の発射までの時間をランダムに少しずらす
        fireTimer = Random.Range(0.5f, 発射間隔);

        // 登場位置に応じた反転と倍率を適用
        float flip = shouldFlip ? -1f : 1f;
        transform.localScale = new Vector3(flip * 敵の大きさ倍率, 敵の大きさ倍率, 1.0f);

        // アニメーションの最初のコマを即座に反映してチラつきを防止（Prefabに古い画像が設定されている場合対策）
        if (アニメーション画像リスト != null && アニメーション画像リスト.Length > 0)
        {
            currentFrameIndex = 0;
            Sprite firstSprite = アニメーション画像リスト[currentFrameIndex];
            if (targetImage != null) targetImage.sprite = firstSprite;
            if (targetSpriteRenderer != null) targetSpriteRenderer.sprite = firstSprite;
        }

        CreateHPBar();
    }

    void CreateHPBar()
    {
        if (HPのプレハブ == null) return;

        if (現在のHP <= 0) 現在のHP = 最大HP;
        
        // HPバーを生成（親は敵と同じにする）
        hpBarObject = Instantiate(HPのプレハブ, transform.parent);
        hpBarObject.name = gameObject.name + "_HPBar";

        // ★ボスならHPバーも大きくする
        if (ボス設定)
        {
            hpBarObject.transform.localScale = Vector3.one * 0.3f; 
        }

        // 初期位置とUI設定
        UpdateHPBarPosition();

        Image[] images = hpBarObject.GetComponentsInChildren<Image>();
        foreach (var img in images)
        {
            if (img.type == Image.Type.Filled)
            {
                hpFillImage = img;
                break;
            }
        }
        
        UpdateHPBarUI();
        
        // 初期状態は非表示
        hpBarObject.SetActive(false);
    }

    void OnDestroy()
    {
        // Spawnerに通知
        if (spawner != null)
        {
            spawner.OnEnemyRemoved(transform.localPosition, wasDefeated, 現在のHP, ボス設定);
        }

        // 自分が消える時（撃破または画面外消滅）、HPバーも一緒に消す
        if (hpBarObject != null)
        {
            Destroy(hpBarObject);
        }
    }

    void UpdateHPBarPosition()
    {
        if (hpBarObject == null) return;
        hpBarObject.transform.localPosition = transform.localPosition + HPバーの位置オフセット;
    }

    void UpdateHPBarUI()
    {
        if (hpFillImage != null)
        {
            hpFillImage.fillAmount = (float)現在のHP / 最大HP;
        }
    }

    System.Collections.IEnumerator ShowHPBarTemporarily()
    {
        if (hpBarObject == null) yield break;

        hpBarObject.SetActive(true);
        yield return new WaitForSeconds(1.0f);
        if (hpBarObject != null) hpBarObject.SetActive(false);
    }

    System.Collections.IEnumerator FlashRedRoutine()
    {
        if (targetImage == null) yield break;

        targetImage.color = Color.red;
        yield return new WaitForSeconds(0.1f);
        if (targetImage != null) targetImage.color = Color.white;
    }
    
    // ... (Update method is unchanged)



    void Update()
    {
        /* 回転アニメーションを完全に無効化
        if (回転を有効にする)
        {
            transform.Rotate(0, 0, 回転速度 * Time.deltaTime);
        }
        */

        UpdateHPBarPosition(); // HPバーの追従

        // 連番画像アニメーション処理
        if (アニメーション画像リスト != null && アニメーション画像リスト.Length > 0)
        {
            animationTimer += Time.deltaTime;
            if (animationTimer >= 1f / アニメーション速度FPS)
            {
                animationTimer = 0f;
                currentFrameIndex = (currentFrameIndex + 1) % アニメーション画像リスト.Length;
                
                Sprite nextSprite = アニメーション画像リスト[currentFrameIndex];
                if (targetImage != null)
                {
                    targetImage.sprite = nextSprite;
                    // targetImage.SetNativeSize(); // ← 巨大化の原因になるため削除
                }
                if (targetSpriteRenderer != null) targetSpriteRenderer.sprite = nextSprite;
            }
        }

        if (!isInitialized) return;

        // --- ボスアクティブ中の挙動 (Bezier計算より優先) ---
        if (isBossActive)
        {
            if (isDying) return; // 撃破演出中は移動停止

            // 退場モード
            if (isExiting)
            {
                transform.localPosition += Vector3.up * 500f * Time.deltaTime; // 上へ移動
                if (transform.localPosition.y > 2500f) // 画面外に出たら消す
                {
                    Destroy(gameObject);
                }
                return;
            }

            // 左右移動 (Sin波)
            // Time.time だと開始時に 0 ではないため、いきなり変な位置に飛んでしまう。
            // ローカルなタイマーを使うことで、0 (中心) から滑らかに動かし始める。
            bossTimer += Time.deltaTime;
            float offsetX = Mathf.Sin(bossTimer * ボス移動速度) * ボス左右移動幅;
            transform.localPosition = bossBasePos + new Vector3(offsetX, 0, 0);
            return; // Bezier計算はスキップ
        }

        // --- 攻撃処理 (通常時のみ) ---
        HandleShooting();

        // --- ベジェ曲線移動 (Quadratic Bezier) ---
        // B(t) = (1-t)^2 * P0 + 2(1-t)t * P1 + t^2 * P2
        if (isDying) return; // 撃破演出中は移動停止
        t += Time.deltaTime * 移動速度 / Vector3.Distance(p0, p2); // 近似的な速度調整

        if (t >= 1f)
        {
            if (ボス設定)
            {
                // まだアクティブでないならアクティブ化
                isBossActive = true;
                bossBasePos = transform.localPosition; // 現在位置（パスの終点）を基準にする
                StartCoroutine(BossBehaviorRoutine());
                return;
            }

            // パス終了→消滅（ボスでなければ）
            Destroy(gameObject);
            return;
        }

        // 位置計算
        float u = 1 - t;
        float tt = t * t;
        float uu = u * u;

        Vector3 p = uu * p0; // (1-t)^2 * P0
        p += 2 * u * t * p1; // 2(1-t)t * P1
        p += tt * p2;        // t^2 * P2

        transform.localPosition = p;
    }

    private bool isExiting = false;

    // ボスの行動パターン
    System.Collections.IEnumerator BossBehaviorRoutine()
    {
        // 到着後一呼吸置く
        yield return new WaitForSeconds(1.0f);

        int maxWaves = 5;
        for (int i = 0; i < maxWaves; i++)
        {
            // 螺旋弾発射
            if (現在のHP > 0) yield return StartCoroutine(FireSpiralWave());
            
            // 次の攻撃までの待機
            if (現在のHP > 0) yield return new WaitForSeconds(2.0f);
        }

        // 終了後、退場フラグを立てる
        isExiting = true;
    }

    System.Collections.IEnumerator FireSpiralWave()
    {
        int bulletCount = 30; // 1ウェーブあたりの弾数
        float anglePerShot = 15f; // 回転角度
        float delay = 0.05f; // 連射速度

        for (int i = 0; i < bulletCount; i++)
        {
            // 撃つ
            if (現在のHP <= 0) yield break;
            if (弾のプレハブ != null)
            {
                spiralAngle += anglePerShot;
                // 2Way (左右対称のようなスパイラルにする場合)
                CreateBullet(GetDir(spiralAngle), 0.35f);
                CreateBullet(GetDir(spiralAngle + 180f), 0.35f);
            }
            yield return new WaitForSeconds(delay);
        }
    }
    
    Vector3 GetDir(float angle)
    {
        return new Vector3(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad), 0);
    }

    void HandleShooting()

    {
        // ボスの場合はコルーチンで制御するので、ここは無視する
        if (ボス設定) return;

        fireTimer -= Time.deltaTime;
        if (fireTimer <= 0f)
        {
            ShootNormal();
            fireTimer = 発射間隔; // タイマーリセット
        }
    }

    void ShootNormal()
    {
        // プレイヤーがいなくなったら撃たない
        if (playerTransform == null && !TryFindPlayer()) return;

        // プレイヤーへの方向を計算
        Vector3 direction = (playerTransform.position - transform.position).normalized;
        CreateBullet(direction, 弾の大きさ倍率);
    }

    bool TryFindPlayer()
    {
         GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
         if (playerObj != null)
         {
             playerTransform = playerObj.transform;
             return true;
         }
         return false;
    }

    void CreateBullet(Vector3 direction, float scale)
    {
        // 弾を生成
        GameObject bullet = Instantiate(弾のプレハブ, transform.position, Quaternion.identity);

        // UIモード対応
        if (transform.parent != null)
        {
            bullet.transform.SetParent(transform.parent);
        }
        
        bullet.transform.localScale = Vector3.one * scale;

        // 弾のスクリプトを取得して初期化
        EnemyBulletController bulletScript = bullet.GetComponent<EnemyBulletController>();
        
        // もしEnemyBulletControllerがついていない場合（間違えて普通のBulletプレハブを入れた場合など）
        if (bulletScript == null)
        {
            bulletScript = bullet.AddComponent<EnemyBulletController>();
        }

        if (bulletScript != null)
        {
            bulletScript.Initialize(direction);
            
            // --- 七色弾の判定 ---
            // 自分（Enemy）の名前がPrefab4（ラスボス）を含んでいれば、弾を七色にする
            if (gameObject.name.Contains("Prefab4"))
            {
                bulletScript.isRainbow = true;
            }
        }
    }

    // 画面上の全ての敵の弾を消去する
    void ClearAllEnemyBullets()
    {
#if UNITY_2023_1_OR_NEWER
        var bullets = Object.FindObjectsByType<EnemyBulletController>(FindObjectsSortMode.None);
#else
        var bullets = Object.FindObjectsOfType<EnemyBulletController>();
#endif
        foreach (var bullet in bullets)
        {
            Destroy(bullet.gameObject);
        }
    }


    // 共通のヒット処理
    void HandleHit(GameObject otherObj)
    {
        // 自分の撃った弾（EnemyBullet）には反応しないようにする
        if (otherObj.GetComponent<EnemyBulletController>() != null)
        {
            return;
        }

        // Debug.Log($"Enemy Hit Check: {otherObj.name}"); // ログがうるさいのでコメントアウト
        
        BulletController bullet = otherObj.GetComponent<BulletController>();
        if (bullet == null)
        {
            bullet = otherObj.GetComponentInParent<BulletController>();
        }

        // otherObj.CompareTag("Bullet") はプロジェクト設定にタグがないとエラーになるため削除
        // GetComponent<BulletController> だけで判定する
        if (bullet != null)
        {
            ApplyDamage(bullet.gameObject);
        }
    }

    void ApplyDamage(GameObject bulletObj)
    {
        int damage = 1;
        BulletController bulletScript = bulletObj.GetComponent<BulletController>();
        if (bulletScript == null) 
            bulletScript = bulletObj.GetComponentInParent<BulletController>();
            
        if (bulletScript != null)
        {
            damage = bulletScript.damage;
        }

        現在のHP -= damage;
        UpdateHPBarUI();

        // 赤く点滅
        StartCoroutine(FlashRedRoutine());

        // HPバーを一時表示
        if (hpVisibilityCoroutine != null) StopCoroutine(hpVisibilityCoroutine);
        hpVisibilityCoroutine = StartCoroutine(ShowHPBarTemporarily());

        if (現在のHP <= 0)
        {
            wasDefeated = true; // 撃破フラグ
            Die();
        }

        Destroy(bulletObj);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        HandleHit(other.gameObject);
    }
    
    // バックアップ：もしIsTriggerがオフになっていても反応するように
    void OnCollisionEnter2D(Collision2D collision)
    {
        HandleHit(collision.gameObject);
    }

    [Header("Score Settings")]
    [FormerlySerializedAs("enemyID")] public string 敵ID = "Enemy1"; // CSVに書いたIDと合わせる

    void Die()
    {
        // スコア加算
        if (ScoreManager.instance != null)
        {
            ScoreManager.instance.AddScore(敵ID);
        }

        // 撃破カウントを加算
        if (ItemSpawner.instance != null)
        {
            ItemSpawner.instance.OnEnemyDefeated();
        }

        // SE再生
        if (AudioManager.instance != null)
        {
            AudioManager.instance.PlayCommonEnemyDie();
        }
        else
        {
            Debug.LogWarning("ItemSpawner instance not found! Did you create the Empty Object and attach the script?");
        }

        if (ボス設定)
        {
            // ボス専用の派手な爆破演出
            StartCoroutine(BossExplosionRoutine());
        }
        else
        {
            // 通常の敵の爆破
            if (爆発エフェクト != null)
            {
                SpawnExplosion(transform.localPosition, 2f);
            }
            Destroy(gameObject);
        }
    }

    // ボスの連続爆発ルーチン
    System.Collections.IEnumerator BossExplosionRoutine()
    {
        isDying = true; // 移動を停止させる

        // ヒット判定を消し、攻撃を止める
        if (TryGetComponent<Collider2D>(out var col)) col.enabled = false;
        if (hpBarObject != null) hpBarObject.SetActive(false);
        
        // 既存の弾を消去
        ClearAllEnemyBullets();

        // 数秒間、ボスの姿を残したままあちこちで爆発を起こす
        int explosionCount = 12;
        for (int i = 0; i < explosionCount; i++)
        {
            // ボスの矩形範囲内でランダムに位置をずらす
            Vector3 offset = new Vector3(
                Random.Range(-表示サイズX * 0.5f, 表示サイズX * 0.5f),
                Random.Range(-表示サイズY * 0.5f, 表示サイズY * 0.5f),
                0
            );

            SpawnExplosion(transform.localPosition + offset, Random.Range(2.5f, 5.0f));
            
            yield return new WaitForSeconds(0.15f);
        }

        // 最後に中央で大きな爆発を起こし、ボスの姿を消す
        if (targetImage != null) targetImage.enabled = false;
        if (targetSpriteRenderer != null) targetSpriteRenderer.enabled = false;
        
        // ボス撃破SE
        if (AudioManager.instance != null)
        {
            AudioManager.instance.PlayBossDie();
        }

        SpawnExplosion(transform.localPosition, 8.0f);
        yield return new WaitForSeconds(0.5f);

        Destroy(gameObject);
    }

    // 爆発を1つ生成するヘルパー（親空間の座標を使用）
    void SpawnExplosion(Vector3 localPos, float scale)
    {
        if (爆発エフェクト == null) return;

        GameObject exp = Instantiate(爆発エフェクト, transform.parent);
        exp.transform.localPosition = localPos;
        exp.transform.localScale = Vector3.one * scale;
    }

    // HPを外部からセットする（再登場時用）
    public void SetCurrentHP(int hp)
    {
        現在のHP = hp;
        UpdateHPBarUI();
    }
}
