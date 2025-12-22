using UnityEngine;
using UnityEngine.UI; // UIのImageを操作するために必要

public class EnemyController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float speed = 5f;
    public bool enableRotation = true; // 回転を無効化できるようにする
    public float rotateSpeed = 360f; // 1秒あたりの回転角度

    [Header("Animation Settings")]
    public Sprite[] animationFrames; // 連番画像
    public float framesPerSecond = 10f; // アニメーション速度

    [Header("Explosion Settings")]
    public GameObject explosionPrefab;

    private bool isInitialized = false;
    private float t = 0f; // ベジェ曲線用のパラメータ (0.0 ～ 1.0)
    private Image targetImage; // アニメーションさせる対象
    private float animationTimer = 0f;
    private int currentFrameIndex = 0;

    // ベジェ曲線のための制御点
    private Vector3 p0, p1, p2;

    void Awake()
    {
        targetImage = GetComponent<Image>();
    }

    public void Initialize(Vector3 start, Vector3 control, Vector3 end)
    {
        p0 = start;
        p1 = control;
        p2 = end;
        
        transform.position = p0;
        isInitialized = true;
        t = 0f;
        
        Debug.Log($"Enemy Spawned at: {transform.position} (Check if this matches Bullet coordinates!)");
    }

    void Update()
    {
        // 回転アニメーション（有効な場合のみ）
        if (enableRotation)
        {
            transform.Rotate(0, 0, rotateSpeed * Time.deltaTime);
        }

        // 連番画像アニメーション処理
        if (animationFrames != null && animationFrames.Length > 0 && targetImage != null)
        {
            animationTimer += Time.deltaTime;
            if (animationTimer >= 1f / framesPerSecond)
            {
                animationTimer = 0f;
                currentFrameIndex = (currentFrameIndex + 1) % animationFrames.Length;
                targetImage.sprite = animationFrames[currentFrameIndex];
            }
        }

        if (!isInitialized) return;

        // ベジェ曲線移動 (Quadratic Bezier)
        // B(t) = (1-t)^2 * P0 + 2(1-t)t * P1 + t^2 * P2
        t += Time.deltaTime * speed / Vector3.Distance(p0, p2); // 近似的な速度調整

        if (t >= 1f)
        {
            // パス終了→消滅
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

        transform.position = p;
    }

    // 共通のヒット処理
    void HandleHit(GameObject otherObj)
    {
        Debug.Log($"Enemy Hit Check: {otherObj.name}");
        
        BulletController bullet = otherObj.GetComponent<BulletController>();
        if (bullet == null)
        {
            bullet = otherObj.GetComponentInParent<BulletController>();
        }

        if (bullet != null || otherObj.CompareTag("Bullet"))
        {
            Debug.Log("Enemy Destroyed by HandleHit!");
            Die();

            if (bullet != null)
                Destroy(bullet.gameObject);
            else
                Destroy(otherObj);
        }
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
    public string enemyID = "Enemy1"; // CSVに書いたIDと合わせる

    void Die()
    {
        // スコア加算
        if (ScoreManager.instance != null)
        {
            ScoreManager.instance.AddScore(enemyID);
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

        if (explosionPrefab != null)
        {
            GameObject exp = Instantiate(explosionPrefab, transform.position, Quaternion.identity);
            
            // UIモードの場合、親キャンバス内にいないと表示されないため、親を敵と同じにする
            exp.transform.SetParent(transform.parent);
            exp.transform.localScale = Vector3.one * 2f; // 少し大きく表示（好みで調整可能）
        }
        
        Destroy(gameObject);
    }
}
