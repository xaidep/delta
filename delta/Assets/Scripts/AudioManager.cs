using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager instance;

    [Header("SEクリップ設定")]
    [UnityEngine.Serialization.FormerlySerializedAs("commonEnemyDieClip")] public AudioClip 雑魚敵撃破音;
    [Range(0, 1)] public float 雑魚敵撃破音量 = 1f;
    [UnityEngine.Serialization.FormerlySerializedAs("bossDieClip")] public AudioClip ボス撃破音;
    [Range(0, 1)] public float ボス撃破音量 = 1f;
    [UnityEngine.Serialization.FormerlySerializedAs("itemGetClip")] public AudioClip アイテム取得音;
    [Range(0, 1)] public float アイテム取得音量 = 1f;
    [UnityEngine.Serialization.FormerlySerializedAs("playerPowerUpClip")] public AudioClip 自機パワーアップ音;
    [Range(0, 1)] public float 自機パワーアップ音量 = 1f;
    [UnityEngine.Serialization.FormerlySerializedAs("bossWarningClip")] public AudioClip ボス出現警告音;
    [Range(0, 1)] public float ボス出現警告音量 = 1f;
    [UnityEngine.Serialization.FormerlySerializedAs("playerShootClip")] public AudioClip 射撃音;
    [Range(0, 1)] public float 射撃音量 = 1f;

    private AudioSource audioSource;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            // シーンを跨いでも破棄されないようにしたい場合はコメント解除
            // DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        audioSource = gameObject.AddComponent<AudioSource>();
    }

    public void PlayCommonEnemyDie() => PlaySE(雑魚敵撃破音, 雑魚敵撃破音量);
    public void PlayBossDie() => PlaySE(ボス撃破音, ボス撃破音量);
    public void PlayItemGet() => PlaySE(アイテム取得音, アイテム取得音量);
    public void PlayPlayerPowerUp() => PlaySE(自機パワーアップ音, 自機パワーアップ音量);
    
    public void PlayBossWarning()
    {
        StartCoroutine(PlayBossWarningRoutine());
    }

    private System.Collections.IEnumerator PlayBossWarningRoutine()
    {
        // 1回目
        PlaySE(ボス出現警告音, ボス出現警告音量);
        if (ボス出現警告音 != null)
        {
            yield return new WaitForSeconds(ボス出現警告音.length);
        }
        // 2回目
        PlaySE(ボス出現警告音, ボス出現警告音量);
    }

    public void PlayPlayerShoot() => PlaySE(射撃音, 射撃音量);

    private void PlaySE(AudioClip clip, float volume)
    {
        if (clip != null && audioSource != null)
        {
            audioSource.PlayOneShot(clip, volume);
        }
    }
}
