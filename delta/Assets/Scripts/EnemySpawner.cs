using UnityEngine;

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

    [Header("Enemy Prefabs")]
    // 複数の敵を登録できるように変更
    public GameObject[] enemyPrefabs; 

    [Header("Spawn Settings")]
    public float intervalMin = 0.5f;
    public float intervalMax = 2.0f;
    
    [Header("Motion Paths")]
    // リストで自由にルートを増やせるように変更
    public System.Collections.Generic.List<BezierPath> paths = new System.Collections.Generic.List<BezierPath>();

    [Header("Wave Settings")]
    public int enemiesPerWave = 5; // 1回に出る敵の数
    public float timeBetweenEnemies = 0.3f; // 敵同士の間隔（ポンポンポンと出る速さ）
    public float timeBetweenWaves = 3.0f;   // 次の5匹が来るまでの待ち時間
           
    private int currentWaveIndex = 0; // 現在のウェーブ数（必要なら難易度調整などに使用）

    // 後方互換性＆初期設定のため、Inspectorでリセットされたときなどにデフォルト値を入れる
    void Reset()
    {
        SetupDefaultPaths();
    }

    void SetupDefaultPaths()
    {
        if (paths == null) paths = new System.Collections.Generic.List<BezierPath>();
        
        if (paths.Count == 0)
        {
            // 旧 Path 1 (Red) - User's Custom Values
            paths.Add(new BezierPath(new Vector3(431.2f, 2000f, 0), new Vector3(390f, 640f, 0), new Vector3(-160f, 280f, 0), Color.red) { note = "Original Red" });
            // 旧 Path 2 (Green) - User's Custom Values
            paths.Add(new BezierPath(new Vector3(680f, 2000f, 0), new Vector3(697.2f, 570.1f, 0), new Vector3(1271f, 273.7f, 0), Color.green) { note = "Original Green" });
            // 新しいバリエーション (Wide) - Generic default (User can adjust)
            paths.Add(new BezierPath(new Vector3(-500f, 2000f, 0), new Vector3(100f, 1000f, 0), new Vector3(500f, 0f, 0), Color.yellow) { note = "Wide Curve" });
        }
    }

    void Start()
    {
        // もしInspectorで空っぽになっていたらデフォルトを入れる
        if (paths == null || paths.Count == 0) SetupDefaultPaths();

        // 最初のウェーブ開始
        StartCoroutine(SpawnWave());
    }

    private bool keepSpawning = true;

    public void StopSpawning()
    {
        keepSpawning = false;
        StopAllCoroutines(); // 手っ取り早く止める場合
    }

    // Coroutineを使ってタイミング制御を簡単にします
    System.Collections.IEnumerator SpawnWave()
    {
        while (keepSpawning)
        {
            // --- 5匹セットの出現開始 ---
            // 今回のウェーブで使用するパスをランダムに決定
            BezierPath currentPath = GetRandomPath();

            // 今回のウェーブで使用する敵をランダムに決定（ウェーブ内では統一）
            GameObject currentPrefab = GetRandomEnemyPrefab();

            for (int i = 0; i < enemiesPerWave; i++)
            {
                if (!keepSpawning) yield break;

                SpawnEnemy(currentPath, currentPrefab);
                // 次の敵が出るまでの短い休憩
                yield return new WaitForSeconds(timeBetweenEnemies);
            }

            // --- 次のセットまでの長い休憩 ---
            yield return new WaitForSeconds(timeBetweenWaves);
            
            currentWaveIndex++;
        }
    }

    BezierPath GetRandomPath()
    {
        if (paths == null || paths.Count == 0) return null;
        return paths[Random.Range(0, paths.Count)];
    }

    GameObject GetRandomEnemyPrefab()
    {
        if (enemyPrefabs == null || enemyPrefabs.Length == 0) return null;
        return enemyPrefabs[Random.Range(0, enemyPrefabs.Length)];
    }

    void SpawnEnemy(BezierPath path, GameObject prefabToSpawn)
    {
        if (prefabToSpawn == null) return;
        if (path == null) return;

        GameObject enemy = Instantiate(prefabToSpawn, path.start, Quaternion.identity);
        enemy.transform.SetParent(transform); 
        
        EnemyController controller = enemy.GetComponent<EnemyController>();
        if (controller != null)
        {
            controller.Initialize(path.start, path.control, path.end);
        }
    }
    
    // ギズモ表示
    void OnDrawGizmos()
    {
        if (paths == null) return;

        foreach (var p in paths)
        {
            if (p == null) continue;
            Gizmos.color = p.gizmoColor;
            
            // 点を描画して見やすくする
            Gizmos.DrawSphere(p.start, 50f);   // 始点
            Gizmos.DrawSphere(p.end, 50f);     // 終点
            Gizmos.DrawWireSphere(p.control, 30f); // 制御点

            DrawBezier(p.start, p.control, p.end);
        }
    }

    void DrawBezier(Vector3 p0, Vector3 p1, Vector3 p2)
    {
        Vector3 prev = p0;
        for (float t = 0; t <= 1; t += 0.05f) // 少し滑らかに (0.1 -> 0.05)
        {
            float u = 1 - t;
            float tt = t * t;
            float uu = u * u;
            Vector3 p = uu * p0 + 2 * u * t * p1 + tt * p2;
            Gizmos.DrawLine(prev, p);
            prev = p;
        }
        Gizmos.DrawLine(prev, p2);
    }
}
