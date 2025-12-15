using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    public GameObject enemyPrefab;
    
    [Header("Spawn Settings")]
    public float intervalMin = 0.5f;
    public float intervalMax = 2.0f;
    
    [Header("Path 1 (Red) Settings")]
    public Vector3 path1Start = new Vector3(-2, 6, 0);
    public Vector3 path1Control = new Vector3(4, 0, 0);
    public Vector3 path1End = new Vector3(-2, -6, 0);

    [Header("Path 2 (Green) Settings")]
    public Vector3 path2Start = new Vector3(2, 6, 0);
    public Vector3 path2Control = new Vector3(-4, 0, 0);
    public Vector3 path2End = new Vector3(2, -6, 0);

    [Header("Wave Settings")]
    public int enemiesPerWave = 5; // 1回に出る敵の数
    public float timeBetweenEnemies = 0.3f; // 敵同士の間隔（ポンポンポンと出る速さ）
    public float timeBetweenWaves = 3.0f;   // 次の5匹が来るまでの待ち時間
           

    private bool usePath1 = true; // ウェーブごとか、敵ごとに切り替えるか

    void Start()
    {
        // 最初のウェーブ開始
        StartCoroutine(SpawnWave());
    }

    // Coroutineを使ってタイミング制御を簡単にします
    System.Collections.IEnumerator SpawnWave()
    {
        while (true)
        {
            // --- 5匹セットの出現開始 ---
            for (int i = 0; i < enemiesPerWave; i++)
            {
                SpawnEnemy();
                // 次の敵が出るまでの短い休憩
                yield return new WaitForSeconds(timeBetweenEnemies);
            }

            // --- 次のセットまでの長い休憩 ---
            yield return new WaitForSeconds(timeBetweenWaves);
            
            // パスの開始地点などを少し変えるならここで抽選など
            // 例: 次のウェーブは逆パターンにするなど
            // usePath1 = !usePath1; // ここで切り替えると「5匹右、5匹左」になります
        }
    }

    void SpawnEnemy()
    {
        if (enemyPrefab == null) return;

        // 交互にパスを選択 (敵1匹ごとに左右を変えるならここ)
        Vector3 start, control, end;
        if (usePath1)
        {
            start = path1Start;
            control = path1Control;
            end = path1End;
        }
        else
        {
            start = path2Start;
            control = path2Control;
            end = path2End;
        }

        GameObject enemy = Instantiate(enemyPrefab, start, Quaternion.identity);
        enemy.transform.SetParent(transform); 
        
        EnemyController controller = enemy.GetComponent<EnemyController>();
        if (controller != null)
        {
            controller.Initialize(start, control, end);
        }

        // 次の敵は逆のルートから
        usePath1 = !usePath1;
    }
    
    // ギズモ表示はそのまま
    void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        DrawBezier(path1Start, path1Control, path1End);
        
        Gizmos.color = Color.green;
        DrawBezier(path2Start, path2Control, path2End);
    }

    void DrawBezier(Vector3 p0, Vector3 p1, Vector3 p2)
    {
        Vector3 prev = p0;
        for (float t = 0; t <= 1; t += 0.1f)
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
