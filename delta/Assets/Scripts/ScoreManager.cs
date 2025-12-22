using UnityEngine;
using UnityEngine.UI;
using TMPro; // TextMeshProを使うために追加
using System.Collections.Generic;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager instance;

    [Header("UI Settings")]
    public TextMeshProUGUI scoreText; // Legacy TextではなくTextMeshProに変更

    [Header("Game State")]
    public int currentScore = 0;

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

    // 直接数値を足す場合（ボーナスなど）
    public void AddScoreAmount(int amount)
    {
        currentScore += amount;
        UpdateScoreUI();
    }

    void UpdateScoreUI()
    {
        if (scoreText != null)
        {
            // 0埋め10桁で表示 (例: SCORE 0000000100)
            scoreText.text = "SCORE " + currentScore.ToString("D10");
        }
    }
}
