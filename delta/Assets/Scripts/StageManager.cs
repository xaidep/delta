using UnityEngine;

public class StageManager : MonoBehaviour
{
    [Header("References")]
    public AudioSource bgmSource;
    public BackgroundScroller backgroundScroller;
    public BackgroundPrefabManager backgroundPrefabManager;
    public EnemySpawner enemySpawner;

    private float stageDuration;
    private bool isStageActive = false;
    private float timer = 0f;

    void Start()
    {
        if (bgmSource != null && bgmSource.clip != null)
        {
            // BGMの長さを取得
            stageDuration = bgmSource.clip.length;
            
            // 背景スクロールに時間を設定
            if (backgroundScroller != null)
            {
                backgroundScroller.SetDuration(stageDuration);
            }

            // BGM再生
            bgmSource.Play();
            isStageActive = true;
            
            Debug.Log($"Stage Started. Duration: {stageDuration} sec");
        }
        else
        {
            Debug.LogError("StageManager: AudioSource or Clip is missing!");
        }
    }

    private bool bossTriggered = false;

    void Update()
    {
        if (!isStageActive) return;

        timer += Time.deltaTime;

        // --- 2:34 (154秒) でラスボス出現 ---
        if (!bossTriggered && bgmSource != null && bgmSource.time >= 154.0f)
        {
            if (enemySpawner != null)
            {
                enemySpawner.TriggerFinalBoss();
                bossTriggered = true;
                Debug.Log("STAGEMANAGER: 2:34 reatched. Triggering Final Boss!");
            }
        }

        // BGMが終わったら（または時間が来たら）ステージ終了処理
        if (timer >= stageDuration || (bgmSource != null && !bgmSource.isPlaying && bgmSource.time > 0.1f))
        {
            EndStage();
        }
    }

    void EndStage()
    {
        isStageActive = false;
        Debug.Log("Stage Ended.");

        // 敵の出現を停止
        if (enemySpawner != null)
        {
            enemySpawner.StopSpawning();
        }

        // 背景スクロールを強制的に完了状態にするなどの処理があればここに追加
        // 現状はBackgroundScroller側で時間経過で止まる想定
    }
}
