using UnityEngine;

public class ItemSpawner : MonoBehaviour
{
    public GameObject powerUpPrefab;

    [Header("Spawn Settings")]
    public float intervalMin = 3f; // テスト用に短く修正
    public float intervalMax = 8f;
    
    [Header("Spawn Positions (Canvas Space)")]
    // 画面の左上と右上を想定（UI座標なので調整が必要かもしれません）
    public Vector3 leftSpawnPos = new Vector3(-350, 600, 0);
    public Vector3 rightSpawnPos = new Vector3(350, 600, 0);
    
    // 目的地（画面の下の方）
    public Vector3 leftTargetPos = new Vector3(-350, -600, 0); 
    public Vector3 rightTargetPos = new Vector3(350, -600, 0);

    public static ItemSpawner instance;

    private int defeatedCount = 0;
    public int killsToSpawn = 5; // 何匹倒したら出るか

    void Awake()
    {
        if (instance == null) instance = this;
    }



    // 敵が倒されたときに呼ばれる
    public void OnEnemyDefeated()
    {
        defeatedCount++;
        Debug.Log($"Enemy Defeated! Count: {defeatedCount}/{killsToSpawn}");

        if (defeatedCount >= killsToSpawn)
        {
            // プレイヤーのレベルを確認
            // Unity 2023以降でFindObjectOfTypeは警告が出るため、FindFirstObjectByTypeを使用
            PlayerController player = FindFirstObjectByType<PlayerController>();
            if (player == null) 
            {
                // 古いバージョンのUnityの場合のフォールバック (もしコンパイルエラーになるならこちらを使ってください)
                 player = FindObjectOfType<PlayerController>();
            }

            if (player != null && player.powerLevel >= 3)
            {
                Debug.Log("Max Level Reached. No Item Spawn.");
                defeatedCount = 0;
                return;
            }

            SpawnItem();
            defeatedCount = 0; // カウントリセット
        }
    }

    // 時間での生成は廃止（必要なら復活可能）
    void Update()
    {
        // NO OP
    }

    void SetNextInterval()
    {
        // NO OP
    }

    void SpawnItem()
    {
        if (powerUpPrefab == null)
        {
            Debug.LogError("ItemSpawner: Power Up Prefab is NOT assigned!");
            return;
        }

        Debug.Log("ItemSpawner: Spawning Item...");

        // 50%の確率で左か右かを決める
        bool isLeftNode = Random.value > 0.5f;
        
        Vector3 spawnPos = isLeftNode ? leftSpawnPos : rightSpawnPos;
        // 左から出るなら右下へ、右から出るなら左下へ（クロス）
        Vector3 targetPos = isLeftNode ? rightTargetPos : leftTargetPos; 

        GameObject item = Instantiate(powerUpPrefab, spawnPos, Quaternion.identity);
        
        // Canvas内に正しく表示されるように親をセット
        // trueにすることで、生成した「見た目の位置」をキープしたまま親を変更します
        item.transform.SetParent(transform, true);
        item.transform.localScale = Vector3.one; // スケールだけは1にリセット

        // 方向を設定
        PowerUpItem script = item.GetComponent<PowerUpItem>();
        if (script != null)
        {
            Vector3 dir = targetPos - spawnPos;
            script.Initialize(dir);
        }
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(leftSpawnPos, 20f);
        Gizmos.DrawLine(leftSpawnPos, rightTargetPos);

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(rightSpawnPos, 20f);
        Gizmos.DrawLine(rightSpawnPos, leftTargetPos);
    }
}
