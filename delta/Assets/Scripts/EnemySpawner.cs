using UnityEngine;
using System.Collections.Generic;

public class EnemySpawner : MonoBehaviour
{
    [System.Serializable]
    public class BezierPath
    {
        public string note = "Path Name";
        public Vector3 start;
        public Vector3 control;
        public Vector3 end;
        public Color gizmoColor = Color.white;

        public BezierPath(Vector3 s, Vector3 c, Vector3 e, Color col)
        {
            start = s; control = c; end = e; gizmoColor = col;
        }
    }

    public enum OriginType { 
        Center,     // (0,0) is middle of screen
        BottomLeft  // (0,0) is bottom-left of screen
    }

    [Header("Coordinate Mapping")]
    [Tooltip("The reference resolution these paths were designed for.")]
    public Vector2 designResolution = new Vector2(1080, 1920);
    [Tooltip("Where is (0,0) in your coordinate data?")]
    public OriginType coordinateOrigin = OriginType.BottomLeft;

    [Header("Enemy Prefabs")]
    public GameObject[] enemyPrefabs;
    public GameObject bonusPrefab; // ボーナスアイコンのプレハブ

    [Header("Spawn Settings")]
    public int 一回に出る敵の数 = 5;
    public float 敵と敵の間隔 = 0.3f;
    public float 次のウェーブまでの待ち時間 = 3.0f;

    [Header("Motion Paths")]
    public List<BezierPath> paths = new List<BezierPath>();

    private RectTransform rectTransform;
    private bool keepSpawning = true;
    private int activeEnemiesInWave = 0; // 現在のウェーブで生き残っている敵の数
    private bool spawningInProgress = false; // 出現中フラグ
    private int waveCount = 0; // ウェーブ数カウント


    // --- Helper for the user to quickly setup the spawner ---
    [ContextMenu("Fit to Parent (Full Screen)")]
    void FitToParent()
    {
        RectTransform rt = GetComponent<RectTransform>();
        if (rt == null) return;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        rt.pivot = new Vector2(0.5f, 0.5f);
    }

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
    }

    void Start()
    {
        if (paths.Count == 0) SetupDefaultPaths();
        
        // Ensure Boss Entry path exists (Fix for existing saves)
        if (!paths.Exists(p => p.note.Contains("Boss Entry")))
        {
            paths.Add(new BezierPath(new Vector3(540f, 2200f, 0), new Vector3(540f, 1800f, 0), new Vector3(540f, 1100f, 0), Color.cyan) { note = "Boss Entry" });
            Debug.Log("Added missing Boss Entry path.");
        }

        StartCoroutine(SpawnWave());
    }

    public void StopSpawning()
    {
        keepSpawning = false;
        StopAllCoroutines();
    }

    System.Collections.IEnumerator SpawnWave()
    {
        while (keepSpawning)
        {
            // ウェーブサイクル (0-3: Mob, 4: Boss)
            bool isBossWave = (waveCount % 5 == 4);

            GameObject prefab = null;
            BezierPath path = null;
            int count = 一回に出る敵の数;

            if (isBossWave)
            {
                // Boss Wave
                prefab = GetBossEnemyPrefab();
                path = GetBossPath();
                count = 1;
                
                if (prefab == null) Debug.LogError("BOSS WAVE FAILED: 'Enemy Prefab3' not found in Spawner list!");
                if (path == null) Debug.LogError("BOSS WAVE FAILED: 'Boss Entry' path not found!");

                Debug.Log($"Wave {waveCount + 1}: BOSS WAVE! (Prefab: {prefab?.name}, Path: {path?.note})");
            }
            else
            {
                // Mob Wave
                prefab = GetRandomMobPrefab();
                path = GetRandomMobPath();
                // Debug.Log($"Wave {waveCount + 1}: Normal Wave");
            }

            // --- ウェーブ開始前のクリーンアップ確認 ---
            // ボスの前後だけでなく、基本的には常に「前の敵が全部消えてから」次を出すと安全
            if (activeEnemiesInWave > 0)
            {
                while (activeEnemiesInWave > 0)
                {
                    yield return new WaitForSeconds(0.5f);
                }
                // 敵が全滅してから少し間を置く
                yield return new WaitForSeconds(1.0f);
            }

            if (path != null && prefab != null)
            {
                spawningInProgress = true;
                activeEnemiesInWave = 0; // ここでリセット（SpawnEnemyで加算される）

                for (int i = 0; i < count; i++)
                {
                    if (!keepSpawning) yield break;
                    SpawnEnemy(path, prefab);
                    yield return new WaitForSeconds(敵と敵の間隔);
                }
                
                spawningInProgress = false;
            }
            
            waveCount++;
            yield return new WaitForSeconds(次のウェーブまでの待ち時間);
        }
    }

    // --- Helper Methods for Wave Logic ---
    GameObject GetBossEnemyPrefab()
    {
        foreach (var p in enemyPrefabs)
        {
            if (p != null && p.name.Contains("Prefab3")) return p;
        }
        return null;
    }

    GameObject GetRandomMobPrefab()
    {
        // "Prefab3" 以外からランダムに選ぶ
        List<GameObject> mobs = new List<GameObject>();
        foreach (var p in enemyPrefabs)
        {
            if (p != null && !p.name.Contains("Prefab3")) mobs.Add(p);
        }
        return mobs.Count > 0 ? mobs[Random.Range(0, mobs.Count)] : null;
    }

    BezierPath GetBossPath()
    {
        return paths.Find(p => p.note.Contains("Boss Entry"));
    }

    BezierPath GetRandomMobPath()
    {
        // "Boss Entry" 以外からランダムに選ぶ
        List<BezierPath> mobPaths = paths.FindAll(p => !p.note.Contains("Boss Entry"));
        return mobPaths.Count > 0 ? mobPaths[Random.Range(0, mobPaths.Count)] : null;
    }

    public void OnEnemyRemoved(Vector3 lastLocalPos, bool wasDefeated)
    {
        activeEnemiesInWave--;

        // すべての敵が消え、かつ出現処理が終わっている場合
        if (activeEnemiesInWave <= 0 && !spawningInProgress)
        {
            // 最後の敵が「撃破」された場合のみボーナスを出す
            if (wasDefeated && bonusPrefab != null)
            {
                SpawnBonus(lastLocalPos);
            }
        }
    }

    void SpawnBonus(Vector3 localPos)
    {
        GameObject bonus = Instantiate(bonusPrefab, transform);
        bonus.transform.localPosition = localPos;
        bonus.transform.localScale = Vector3.one;
        
        // BonusIconスクリプトが付いていることを期待
        // (次の手順で作成)
    }

    void SpawnEnemy(BezierPath path, GameObject prefab)
    {
        // Convert design pixels to current Spawner's local rect space
        Vector3 localStart = MapDesignToLocal(path.start);
        Vector3 localControl = MapDesignToLocal(path.control);
        Vector3 localEnd = MapDesignToLocal(path.end);

        GameObject enemy = Instantiate(prefab, transform);
        enemy.transform.localPosition = localStart;
        enemy.transform.localRotation = Quaternion.identity;
        enemy.transform.localScale = Vector3.one;

        EnemyController controller = enemy.GetComponent<EnemyController>();
        if (controller != null)
        {
            controller.Initialize(localStart, localControl, localEnd, this);
            activeEnemiesInWave++; // カウントアップ
        }
    }

    // Maps Design Pixels (e.g. 1080x1920) directly to the Spawner's Rect bounds.
    // This is the MOST reliable way for UI systems.
    Vector3 MapDesignToLocal(Vector3 designPos)
    {
        if (rectTransform == null) rectTransform = GetComponent<RectTransform>();
        
        // 1. Normalize based on origin
        float nx, ny;
        if (coordinateOrigin == OriginType.Center)
        {
            nx = (designPos.x / designResolution.x) + 0.5f;
            ny = (designPos.y / designResolution.y) + 0.5f;
        }
        else
        {
            nx = designPos.x / designResolution.x;
            ny = designPos.y / designResolution.y;
        }

        // 2. Map normalized (0-1) to actual Spawner Rect (handles all pivot/size)
        float localX = Mathf.Lerp(rectTransform.rect.xMin, rectTransform.rect.xMax, nx);
        float localY = Mathf.Lerp(rectTransform.rect.yMin, rectTransform.rect.yMax, ny);

        return new Vector3(localX, localY, designPos.z);
    }

    BezierPath GetRandomPath() => paths.Count > 0 ? paths[Random.Range(0, paths.Count)] : null;
    GameObject GetRandomEnemyPrefab() => enemyPrefabs.Length > 0 ? enemyPrefabs[Random.Range(0, enemyPrefabs.Length)] : null;

    void SetupDefaultPaths()
    {
        paths.Add(new BezierPath(new Vector3(431.2f, 2000f, 0), new Vector3(390f, 640f, 0), new Vector3(-160f, 280f, 0), Color.red) { note = "Original Red" });
        paths.Add(new BezierPath(new Vector3(680f, 2000f, 0), new Vector3(697.2f, 570.1f, 0), new Vector3(1271f, 273.7f, 0), Color.green) { note = "Original Green" });
        paths.Add(new BezierPath(new Vector3(-500f, 2000f, 0), new Vector3(100f, 1000f, 0), new Vector3(500f, 0f, 0), Color.yellow) { note = "Wide Curve" });
        
        // Enemy 3 (Boss) Entry: 上から出現して、画面中央寄り(Y=1100付近)へ
        paths.Add(new BezierPath(new Vector3(540f, 2200f, 0), new Vector3(540f, 1800f, 0), new Vector3(540f, 1100f, 0), Color.cyan) { note = "Boss Entry" });
    }

    void OnDrawGizmos()
    {
        if (paths == null || paths.Count == 0) return;
        if (rectTransform == null) rectTransform = GetComponent<RectTransform>();

        foreach (var p in paths)
        {
            Gizmos.color = p.gizmoColor;
            
            // Gizmos are in World Space, so we multiply Local -> World
            Vector3 worldStart = transform.TransformPoint(MapDesignToLocal(p.start));
            Vector3 worldControl = transform.TransformPoint(MapDesignToLocal(p.control));
            Vector3 worldEnd = transform.TransformPoint(MapDesignToLocal(p.end));

            Gizmos.DrawSphere(worldStart, 15f);
            Gizmos.DrawSphere(worldEnd, 15f);
            Gizmos.DrawWireSphere(worldControl, 10f);

            Vector3 prev = worldStart;
            for (float t = 0.05f; t <= 1.01f; t += 0.05f)
            {
                float u = 1 - t;
                Vector3 pos = (u * u) * worldStart + (2 * u * t) * worldControl + (t * t) * worldEnd;
                Gizmos.DrawLine(prev, pos);
                prev = pos;
            }
        }
    }
}
