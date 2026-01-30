using UnityEngine;

public class ItemSpawner : MonoBehaviour
{
    // --- 1. 定数・設定 ---
    [Header("Item Spawner Settings")]
    public GameObject パワーアップアイテムプレハブ;
    public int アイテム出現に必要な撃破数 = 5;

    [Header("Spawn Margin (Relative to Screen Size)")]
    [Range(0, 0.5f)] public float 横の余白率 = 0.4f;
    [Range(0, 1.0f)] public float 出現高度率 = 0.6f;
    [Range(0, 1.0f)] public float 到着高度率 = 0.6f;

    private int defeatedCount = 0;
    private RectTransform rectTransform;

    public static ItemSpawner instance;

    void Awake()
    {
        if (instance == null) instance = this;
        rectTransform = GetComponent<RectTransform>();
    }

    public void OnEnemyDefeated()
    {
        defeatedCount++;
        if (defeatedCount >= アイテム出現に必要な撃破数)
        {
            SpawnItem();
            defeatedCount = 0;
        }
    }

    void SpawnItem()
    {
        if (パワーアップアイテムプレハブ == null) return;

        // --- レベルキャップの判定 ---
        // プレイヤーのレベルが最大（Lv7以上、内部値 6 以上）ならドロップしない
        if (PlayerController.instance != null && PlayerController.instance.現在のパワーレベル >= 6)
        {
            Debug.Log("ItemSpawner: Player is at max power. Skipping item drop.");
            return;
        }

        // 画面全体（Canvas）を取得
        Canvas parentCanvas = GetComponentInParent<Canvas>();
        if (parentCanvas == null) return;
        RectTransform canvasRT = parentCanvas.GetComponent<RectTransform>();

        // 画面のサイズから出現・目標座標を計算
        float halfW = canvasRT.rect.width * 0.5f;
        float halfH = canvasRT.rect.height * 0.5f;

        float posX = halfW * 0.9f;
        float spawnY = halfH * 1.1f;
        float targetY = -halfH * 1.1f;

        bool isLeft = Random.value > 0.5f;
        Vector3 spawnPos = new Vector3(isLeft ? -posX : posX, spawnY, 0);
        Vector3 targetPos = new Vector3(isLeft ? posX : -posX, targetY, 0);

        // Canvasの直下に生成することで、座標計算を確実に同期させる
        GameObject item = Instantiate(パワーアップアイテムプレハブ, canvasRT);
        RectTransform itemRect = item.GetComponent<RectTransform>();
        
        if (itemRect != null)
        {
            itemRect.anchoredPosition = spawnPos;
            itemRect.localScale = Vector3.one;
            
            // Z座標を0に固定し、最前面に表示されるようにする
            Vector3 pos = itemRect.localPosition;
            pos.z = 0;
            itemRect.localPosition = pos;
            itemRect.SetAsLastSibling();

            PowerUpItem script = item.GetComponent<PowerUpItem>();
            if (script != null)
            {
                script.Initialize(targetPos - spawnPos);
            }
        }
    }

    // デバッグ用：エディタ上で出現位置と目的地を可視化する
    void OnDrawGizmos()
    {
        Canvas targetCanvas = GetComponentInParent<Canvas>();
        if (targetCanvas == null) targetCanvas = FindFirstObjectByType<Canvas>();
        if (targetCanvas == null) return;
        
        RectTransform canvasRT = targetCanvas.GetComponent<RectTransform>();

        // ギズモの行列をキャンバスのローカル行列に合わせることで、計算を簡略化
        Matrix4x4 oldGizmosMatrix = Gizmos.matrix;
        Gizmos.matrix = canvasRT.localToWorldMatrix;

        float halfW = canvasRT.rect.width * 0.5f;
        float halfH = canvasRT.rect.height * 0.5f;
        float posX = halfW * 0.9f;
        float spawnY = halfH * 1.1f;
        float targetY = -halfH * 1.1f;

        // 出現座標の球を表示
        Gizmos.color = Color.yellow;
        Vector3 leftS = new Vector3(-posX, spawnY, 0);
        Vector3 rightT = new Vector3(posX, targetY, 0);
        Gizmos.DrawSphere(leftS, 30f); // WireSphereより視認性の高いSphereに変更
        Gizmos.DrawLine(leftS, rightT);

        Gizmos.color = Color.cyan;
        Vector3 rightS = new Vector3(posX, spawnY, 0);
        Vector3 leftT = new Vector3(-posX, targetY, 0);
        Gizmos.DrawSphere(rightS, 30f);
        Gizmos.DrawLine(rightS, leftT);

        Gizmos.matrix = oldGizmosMatrix;
    }
}
