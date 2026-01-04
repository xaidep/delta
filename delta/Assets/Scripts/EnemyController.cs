using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI; // UIのImageを操作するために必要

public class EnemyController : MonoBehaviour
{
    [Header("Movement Settings")]
    [FormerlySerializedAs("speed")] public float 移動速度 = 5f;
    [FormerlySerializedAs("enableRotation")] public bool 回転を有効にする = true; // 回転を無効化できるようにする
    [FormerlySerializedAs("rotateSpeed")] public float 回転速度 = 360f; // 1秒あたりの回転角度

    [Header("Animation Settings")]
    [FormerlySerializedAs("animationFrames")] public Sprite[] アニメーション画像リスト; // 連番画像
    [FormerlySerializedAs("framesPerSecond")] public float アニメーション速度FPS = 10f; // アニメーション速度

    [Header("Explosion Settings")]
    [FormerlySerializedAs("explosionPrefab")] public GameObject 爆発エフェクト;

    [Header("Size Settings")]
    public float 敵の大きさ倍率 = 1.0f;

    [Header("Boss Settings")]
    [FormerlySerializedAs("isBoss")] public bool ボス設定 = false; // ボスフラグ
    [FormerlySerializedAs("bossLifeTime")] public float ボス滞在時間 = 20f; // ボスの滞在時間
    [FormerlySerializedAs("bossHoverWidth")] public float ボス左右移動幅 = 200f; // 左右移動の幅
    [FormerlySerializedAs("bossHoverSpeed")] public float ボス移動速度 = 1.0f; // 左右移動の速さ
    
    private bool isBossActive = false; // ボスモード（移動完了後）
    private Vector3 bossBasePos; // 左右移動の基準点
    private float spiralAngle = 0f; // 螺旋弾用角度
    private float bossTimer = 0f; // ボス動作用タイマー


    private bool isInitialized = false;
    private bool wasDefeated = false; // プレイヤーに倒されたか
    private EnemySpawner spawner; // 生みの親
    private float t = 0f; // ベジェ曲線用のパラメータ (0.0 ～ 1.0)
    private Image targetImage; // アニメーションさせる対象
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
    }

    public void Initialize(Vector3 start, Vector3 control, Vector3 end, EnemySpawner owner)
    {
        p0 = start;
        p1 = control;
        p2 = end;
        spawner = owner;
        
        transform.localPosition = p0;
        isInitialized = true;
        t = 0f;
        
        Debug.Log($"Enemy Spawned at: {transform.position} (Check if this matches Bullet coordinates!)");
    }

    [Header("Attack Settings")]
    [FormerlySerializedAs("bulletPrefab")] public GameObject 弾のプレハブ;
    [FormerlySerializedAs("fireInterval")] public float 発射間隔 = 2.0f;
    [FormerlySerializedAs("bulletScale")] public float 弾の大きさ倍率 = 0.3f; // 弾の大きさ調整用
    private float fireTimer;
    private Transform playerTransform;

    void Start()
    {
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

        // 大きさを適用
        transform.localScale = Vector3.one * 敵の大きさ倍率;

        CreateHPBar();
    }

    void CreateHPBar()
    {
        if (HPのプレハブ == null) return;

        現在のHP = 最大HP;
        
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
            spawner.OnEnemyRemoved(transform.localPosition, wasDefeated);
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
        // 回転アニメーション（有効な場合のみ）
        if (回転を有効にする)
        {
            transform.Rotate(0, 0, 回転速度 * Time.deltaTime);
        }

        UpdateHPBarPosition(); // HPバーの追従

        // 連番画像アニメーション処理
        if (アニメーション画像リスト != null && アニメーション画像リスト.Length > 0 && targetImage != null)
        {
            animationTimer += Time.deltaTime;
            if (animationTimer >= 1f / アニメーション速度FPS)
            {
                animationTimer = 0f;
                currentFrameIndex = (currentFrameIndex + 1) % アニメーション画像リスト.Length;
                targetImage.sprite = アニメーション画像リスト[currentFrameIndex];
            }
        }

        if (!isInitialized) return;

        // --- ボスアクティブ中の挙動 (Bezier計算より優先) ---
        if (isBossActive)
        {
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
            yield return StartCoroutine(FireSpiralWave());
            
            // 次の攻撃までの待機
            yield return new WaitForSeconds(2.0f);
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

    void ApplyDamage(GameObject bullet)
    {
        現在のHP--;
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

        Destroy(bullet);
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
        else
        {
            Debug.LogWarning("ItemSpawner instance not found! Did you create the Empty Object and attach the script?");
        }

        if (爆発エフェクト != null)
        {
            GameObject exp = Instantiate(爆発エフェクト, transform.position, Quaternion.identity);
            
            // UIモードの場合、親キャンバス内にいないと表示されないため、親を敵と同じにする
            exp.transform.SetParent(transform.parent);
            exp.transform.localScale = Vector3.one * 2f; // 少し大きく表示（好みで調整可能）
        }

        Destroy(gameObject);
    }
}
