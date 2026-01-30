using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using TMPro; // TextMeshProを使うために追加
using System.Collections.Generic;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager instance;

    [Header("UI Settings")]
    [FormerlySerializedAs("scoreText")] public TextMeshProUGUI スコア表示テキスト; // Legacy TextではなくTextMeshProに変更
    public GameObject bonusAnimationPrefab; // ボーナス演出用のプレハブ

    [Header("Game State")]
    [FormerlySerializedAs("currentScore")] public int 現在のスコア = 0;

    // --- ボーナス演出テンプレート用キャッシュ ---
    private Vector2 templateAnchorMin = new Vector2(0, 1);
    private Vector2 templateAnchorMax = new Vector2(0, 1);
    private Vector2 templatePivot = new Vector2(0.5f, 0.5f);
    private Vector2 templateAnchoredPos = new Vector2(66.6f, -147f);
    private Vector3 templateScale = new Vector3(0.7f, 0.7f, 0.7f);

    // 敵IDと点数の対応表
    private Dictionary<string, int> enemyScores = new Dictionary<string, int>();

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        LoadScoreData();
    }

    void Start()
    {
        UpdateScoreUI();
        InitializeBonusTemplate();
    }

    // Hierarchy上の既存オブジェクトから配置設定を読み取る
    void InitializeBonusTemplate()
    {
        GameObject templateObj = GameObject.Find("bonusAnimation");
        if (templateObj != null)
        {
            RectTransform rt = templateObj.GetComponent<RectTransform>();
            if (rt != null)
            {
                templateAnchorMin = rt.anchorMin;
                templateAnchorMax = rt.anchorMax;
                templatePivot = rt.pivot;
                templateAnchoredPos = rt.anchoredPosition;
                templateScale = templateObj.transform.localScale;
                
                // 本番中はテンプレート自体を非表示にする
                templateObj.SetActive(false);
                Debug.Log($"ScoreManager: Bonus Template initialized from Hierarchy at {templateAnchoredPos}");
            }
        }
        else
        {
            Debug.Log("ScoreManager: 'bonusAnimation' not found in Hierarchy. Using fallback coordinates (66.6, -147).");
        }
    }

    // CSVファイルを読み込んで辞書を作る
    void LoadScoreData()
    {
        // Resourcesフォルダから "EnemyScores.csv" を読み込む (拡張子不要)
        TextAsset csvFile = Resources.Load<TextAsset>("EnemyScores");

        if (csvFile == null)
        {
            Debug.LogError("ScoreManager: 'EnemyScores.csv' not found in Resources folder! Make sure the file is at 'Assets/Resources/EnemyScores.csv'.");
            return;
        }

        // 行ごとに分割
        string[] lines = csvFile.text.Split('\n');

        foreach (string line in lines)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;

            // カンマで分割 (ID, Score)
            string[] parts = line.Split(',');
            if (parts.Length >= 2)
            {
                string id = parts[0].Trim();
                int score = 0;
                
                if (int.TryParse(parts[1].Trim(), out score))
                {
                    if (!enemyScores.ContainsKey(id))
                    {
                        enemyScores.Add(id, score);
                    }
                }
            }
        }
        
        Debug.Log($"ScoreManager: Loaded {enemyScores.Count} enemy scores.");
    }

    // スコア加算
    public void AddScore(string enemyId)
    {
        if (enemyScores.ContainsKey(enemyId))
        {
            int points = enemyScores[enemyId];
            AddScoreAmount(points);
        }
        else
        {
            // IDが見つからない場合はデフォルト点数（例: 10点）を入れるか、ログを出す
            Debug.LogWarning($"ScoreManager: Unknown Enemy ID '{enemyId}'. Adding default 10 points.");
            AddScoreAmount(10);
        }
    }

    // 直接数値を足す場合
    public void AddScoreAmount(int amount)
    {
        現在のスコア += amount;
        UpdateScoreUI();
        FlashScore(); // 点滅開始
    }

    // ボーナスとして加算する場合（演出あり）
    public void AddBonusScore(int amount)
    {
        現在のスコア += amount;
        UpdateScoreUI();
        FlashScore();

        if (bonusAnimationPrefab != null)
        {
            // Canvas(現在の親)の直下で生成
            GameObject bonusAnim = Instantiate(bonusAnimationPrefab, transform.parent);
            RectTransform rt = bonusAnim.GetComponent<RectTransform>();

            if (rt != null)
            {
                // テンプレートの設定を適用
                rt.anchorMin = templateAnchorMin;
                rt.anchorMax = templateAnchorMax;
                rt.pivot = templatePivot;
                rt.anchoredPosition = templateAnchoredPos;
                bonusAnim.transform.localScale = templateScale;
                
                // Z座標の保険
                Vector3 pos = rt.localPosition;
                pos.z = 0;
                rt.localPosition = pos;
            }

            BonusAnimation script = bonusAnim.GetComponent<BonusAnimation>();
            if (script != null)
            {
                script.SetBonus(amount);
            }
        }
    }

    public void FlashScore()
    {
        if (スコア表示テキスト == null) return;
        StopAllCoroutines(); // 前の点滅があれば止める
        StartCoroutine(FlashRoutine());
    }

    private System.Collections.IEnumerator FlashRoutine()
    {
        Color originalColor = Color.white; // TMPの基本色
        Color flashColor = Color.yellow;   // 点滅時の色

        // 3回素早く点滅
        for (int i = 0; i < 3; i++)
        {
            スコア表示テキスト.color = flashColor;
            yield return new WaitForSeconds(0.05f);
            スコア表示テキスト.color = originalColor;
            yield return new WaitForSeconds(0.05f);
        }
    }

    void UpdateScoreUI()
    {
        if (スコア表示テキスト != null)
        {
            // 0埋め10桁で表示 (例: SCORE 0000000100)
            スコア表示テキスト.text = "<color=#FF007C>SCORE</color> " + 現在のスコア.ToString("D10");
        }
    }
}
