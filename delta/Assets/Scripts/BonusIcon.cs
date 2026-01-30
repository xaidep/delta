using UnityEngine;
using UnityEngine.Serialization;

public class BonusIcon : MonoBehaviour
{
    private Vector3 targetPosition;
    private bool isFlying = false;

    [Header("演出設定")]
    [FormerlySerializedAs("flightDuration")] public float 飛行時間 = 0.8f;
    [FormerlySerializedAs("rotateSpeed")] public float 回転速度 = 0f; // デフォルトを0にして回転を止める
    [FormerlySerializedAs("startScaleMultiplier")] public float 開始時の大きさ倍率 = 0.75f;
    [FormerlySerializedAs("endScaleMultiplier")] public float 終了時の大きさ倍率 = 0.15f;

    [Header("サイズ設定")]
    public float アイテムの大きさ = 1.0f;

    [Header("点数設定")]
    [FormerlySerializedAs("bonusAmount")] public int ボーナス点数 = 1000;

    private Vector3 startPosition;
    private float elapsedTime = 0f;

    void Start()
    {
        // ScoreManagerからスコアテキストの位置を取得
        if (ScoreManager.instance != null && ScoreManager.instance.スコア表示テキスト != null)
        {
            startPosition = transform.position;
            targetPosition = ScoreManager.instance.スコア表示テキスト.transform.position;
            
            // 開始時の大きさを設定
            transform.localScale = Vector3.one * (開始時の大きさ倍率 * アイテムの大きさ);
            
            isFlying = true;
        }
        else
        {
            Debug.LogWarning("BonusIcon: ScoreManager or ScoreText not found!");
            Destroy(gameObject);
        }
    }

    void Update()
    {
        if (!isFlying) return;

        elapsedTime += Time.deltaTime;
        float normalizedTime = Mathf.Clamp01(elapsedTime / 飛行時間);

        // --- 1. イージング (Ease In: 最初はゆっくり、後半は速く) ---
        float t = normalizedTime * normalizedTime;

        // --- 2. 回転 (不要な場合は0に設定) ---
        if (回転速度 != 0)
        {
            transform.Rotate(0, 0, 回転速度 * Time.deltaTime);
        }

        // --- 3. 移動 (Lerp) ---
        transform.position = Vector3.Lerp(startPosition, targetPosition, t);

        // --- 4. スケール (Lerp) ---
        float currentScale = Mathf.Lerp(開始時の大きさ倍率, 終了時の大きさ倍率, t) * アイテムの大きさ;
        transform.localScale = Vector3.one * currentScale;

        // --- 5. 到着判定 ---
        if (normalizedTime >= 1.0f)
        {
            OnArrival();
        }
    }

    void OnArrival()
    {
        // スコア加算
        if (ScoreManager.instance != null)
        {
            ScoreManager.instance.AddBonusScore(ボーナス点数);
            Debug.Log($"Bonus {ボーナス点数} Added!");
        }

        // 自身を消去
        Destroy(gameObject);
    }
}
