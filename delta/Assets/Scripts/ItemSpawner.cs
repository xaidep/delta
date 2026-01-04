using UnityEngine;
using UnityEngine.Serialization;

public class ItemSpawner : MonoBehaviour
{
    [FormerlySerializedAs("powerUpPrefab")] public GameObject パワーアップアイテムプレハブ;

    [Header("Spawn Settings")]
    [FormerlySerializedAs("intervalMin")] public float 出現間隔_最小 = 3f; // テスト用に短く修正
    [FormerlySerializedAs("intervalMax")] public float 出現間隔_最大 = 8f;
    
    [Header("Spawn Positions (Canvas Space)")]
    // 画面の左上と右上を想定（UI座標なので調整が必要かもしれません）
    [FormerlySerializedAs("leftSpawnPos")] public Vector3 左側の出現位置 = new Vector3(-350, 600, 0);
    [FormerlySerializedAs("rightSpawnPos")] public Vector3 右側の出現位置 = new Vector3(350, 600, 0);
    
    // 目的地（画面の下の方）
    [FormerlySerializedAs("leftTargetPos")] public Vector3 左側の移動目標 = new Vector3(-350, -600, 0); 
    [FormerlySerializedAs("rightTargetPos")] public Vector3 右側の移動目標 = new Vector3(350, -600, 0);

    public static ItemSpawner instance;

    private int defeatedCount = 0;
    [FormerlySerializedAs("killsToSpawn")] public int アイテム出現に必要な撃破数 = 5; // 何匹倒したら出るか

    void Awake()
    {
        if (instance == null) instance = this;
    }



    // 敵が倒されたときに呼ばれる
    public void OnEnemyDefeated()
    {
        defeatedCount++;
        Debug.Log($"Enemy Defeated! Count: {defeatedCount}/{アイテム出現に必要な撃破数}");

        if (defeatedCount >= アイテム出現に必要な撃破数)
        {
            // プレイヤーのレベルを確認
#if UNITY_2023_1_OR_NEWER
            PlayerController player = Object.FindFirstObjectByType<PlayerController>();
#else
            PlayerController player = Object.FindObjectOfType<PlayerController>();
#endif

            if (player != null && player.現在のパワーレベル >= 3)
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
        if (パワーアップアイテムプレハブ == null)
        {
            Debug.LogError("ItemSpawner: Power Up Prefab is NOT assigned!");
            return;
        }

        Debug.Log("ItemSpawner: Spawning Item...");

        // 50%の確率で左か右かを決める
        bool isLeftNode = Random.value > 0.5f;
        
        Vector3 spawnPos = isLeftNode ? 左側の出現位置 : 右側の出現位置;
        // 左から出るなら右下へ、右から出るなら左下へ（クロス）
        Vector3 targetPos = isLeftNode ? 右側の移動目標 : 左側の移動目標; 

        GameObject item = Instantiate(パワーアップアイテムプレハブ, spawnPos, Quaternion.identity);
        
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
        Gizmos.DrawWireSphere(左側の出現位置, 20f);
        Gizmos.DrawLine(左側の出現位置, 右側の移動目標);

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(右側の出現位置, 20f);
        Gizmos.DrawLine(右側の出現位置, 左側の移動目標);
    }
}
