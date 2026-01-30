using UnityEngine;
using System.Collections.Generic;

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

    public enum OriginType { 
        Center,     // (0,0) is middle of screen
        BottomLeft  // (0,0) is bottom-left of screen
    }

    [Header("Coordinate Mapping")]
    [Tooltip("The reference resolution these paths were designed for.")]
    public Vector2 designResolution = new Vector2(1080, 1920);
    [Tooltip("Where is (0,0) in your coordinate data?")]
    public OriginType coordinateOrigin = OriginType.BottomLeft;

    [Header("Enemy Prefabs")]
    public GameObject[] enemyPrefabs;
    [Header("UI Prefabs")]
    public GameObject bonusPrefab; // ボーナスアイコンのプレハブ
    public GameObject warningPrefab; // WARNING演出用のプレハブ

    [Header("Spawn Settings")]
    public int 一回に出る敵の数 = 5;
    public float 敵と敵の間隔 = 0.3f;
    public float 次のウェーブまでの待ち時間 = 3.0f;

    [Header("Motion Paths")]
    public List<BezierPath> paths = new List<BezierPath>();

    private RectTransform rectTransform;
    private bool keepSpawning = true;
    private int activeEnemiesInWave = 0; // 現在のウェーブで生き残っている敵の数
    private bool spawningInProgress = false; // 出現中フラグ
    private int waveCount = 0; // ウェーブ数カウント
    private int bossPersistentHP = -1; // ボスの残りHP（-1なら初期値）


    public enum SpawnerPhase { Early, MidBoss, Late, FinalBoss }
    private SpawnerPhase currentPhase = SpawnerPhase.Early;

    // --- Helper for the user to quickly setup the spawner ---
    [ContextMenu("Fit to Parent (Full Screen)")]
    void FitToParent()
    {
        RectTransform rt = GetComponent<RectTransform>();
        if (rt == null) return;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        rt.pivot = new Vector2(0.5f, 0.5f);
    }

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
    }

    void Start()
    {
        if (paths.Count == 0) SetupDefaultPaths();
        EnsureEssentialPaths();
        
        StartCoroutine(SpawnWave());
    }

    void EnsureEssentialPaths()
    {
        // 必須のパスが存在するか確認し、なければ追加する
        if (!paths.Exists(p => p.note == "Boss Entry"))
        {
            paths.Add(new BezierPath(new Vector3(540f, 2200f, 0), new Vector3(540f, 1800f, 0), new Vector3(540f, 1100f, 0), Color.cyan) { note = "Boss Entry" });
        }

        // --- Side Entry Paths (Late Phase用: ランダム化のため大幅に追加) ---
        
        // 左側候補 (Side Left ...)
        AddSidePathIfMissing("Side Left Mid",   -200f, 1500f,  540f, 1300f, 1280f, 1100f, Color.magenta);
        AddSidePathIfMissing("Side Left High",  -200f, 1800f,  540f, 1600f, 1280f, 1400f, Color.magenta);
        AddSidePathIfMissing("Side Left Low",   -200f, 1200f,  540f, 1000f, 1280f, 800f, Color.magenta);
        AddSidePathIfMissing("Side Left Curve", -200f, 1600f, 1080f, 1920f, 1280f, 1000f, Color.red);     // 大きく弧を描く
        AddSidePathIfMissing("Side Left Dive",  -200f, 1900f,  300f, 500f,  1280f, 300f, Color.yellow);  // 下方へ急降下
        AddSidePathIfMissing("Side Left Arch",  -200f, 1000f,  540f, 2200f, 1280f, 1000f, Color.green);   // 上へ盛り上がる

        // 右側候補 (Side Right ...)
        AddSidePathIfMissing("Side Right Mid",   1280f, 1600f,  540f, 1400f, -200f, 1200f, Color.magenta);
        AddSidePathIfMissing("Side Right High",  1280f, 1900f,  540f, 1700f, -200f, 1500f, Color.magenta);
        AddSidePathIfMissing("Side Right Low",   1280f, 1300f,  540f, 1100f, -200f, 900f, Color.magenta);
        AddSidePathIfMissing("Side Right Curve", 1280f, 1700f,  0f, 1920f, -200f, 1100f, Color.red);      // 大きく弧を描く
        AddSidePathIfMissing("Side Right Dive",  1280f, 2000f,  780f, 600f,  -200f, 400f, Color.yellow);   // 下方へ急降下
        AddSidePathIfMissing("Side Right Arch",  1280f, 1100f,  540f, 2300f, -200f, 1100f, Color.green);    // 上へ盛り上がる
    }

    void AddSidePathIfMissing(string note, float sx, float sy, float cx, float cy, float ex, float ey, Color col)
    {
        if (!paths.Exists(p => p.note == note))
        {
            paths.Add(new BezierPath(new Vector3(sx, sy, 0), new Vector3(cx, cy, 0), new Vector3(ex, ey, 0), col) { note = note });
        }
    }

    public void StopSpawning()
    {
        keepSpawning = false;
        StopAllCoroutines();
    }

    // 外部（StageManager）からラスボス出現を強制する
    public void TriggerFinalBoss()
    {
        if (currentPhase == SpawnerPhase.FinalBoss) return;
        
        Debug.Log("ENEMY SPAWNER: FINAL BOSS TRIGGERED!");
        currentPhase = SpawnerPhase.FinalBoss;
        
        // 現在のウェーブを中断してボスを出すためにコルーチンを再起動
        StopAllCoroutines();
        StartCoroutine(FinalBossSequence());
    }

    private System.Collections.IEnumerator FinalBossSequence()
    {
        yield return StartCoroutine(ShowWarningRoutine());
        yield return StartCoroutine(SpawnWave());
    }

    private System.Collections.IEnumerator ShowWarningRoutine()
    {
        if (warningPrefab != null)
        {
            Debug.Log("ENEMY SPAWNER: Displaying WARNING!");
            // キャンバス内に生成
            GameObject warning = Instantiate(warningPrefab, transform);
            warning.transform.localPosition = Vector3.zero;
            warning.transform.localScale = Vector3.one;

            // SE再生
            if (AudioManager.instance != null)
            {
                AudioManager.instance.PlayBossWarning();
            }

            // アニメーションが1秒とのことなので、少し余裕を持って2秒待つ
            yield return new WaitForSeconds(2.0f);
            
            Destroy(warning);
        }
        else
        {
            yield return null;
        }
    }

    System.Collections.IEnumerator SpawnWave()
    {
        while (keepSpawning)
        {
            // フェーズ判定
            if (currentPhase == SpawnerPhase.Early && waveCount >= 4)
            {
                currentPhase = SpawnerPhase.MidBoss;
                // 中ボスの前に警告を出す
                yield return StartCoroutine(ShowWarningRoutine());
            }

            GameObject prefab = null;
            BezierPath path = null;
            int count = 一回に出る敵の数;
            bool isBossType = false;

            if (currentPhase == SpawnerPhase.FinalBoss)
            {
                // Final Boss (Prefab 4)
                prefab = GetSpecificPrefab("Prefab4");
                path = GetBossPath();
                count = 1;
                isBossType = true;
                Debug.Log("Wave: FINAL BOSS APPEARS!");
            }
            else if (currentPhase == SpawnerPhase.MidBoss)
            {
                // Mid Boss (Prefab 3)
                prefab = GetSpecificPrefab("Prefab3");
                path = GetBossPath();
                count = 1;
                isBossType = true;
                Debug.Log("Wave: MID BOSS APPEARS!");
            }
            else
            {
                // Normal Wave (Early or Late)
                prefab = GetRandomMobPrefab();
                path = GetRandomMobPath();
            }

            // --- ウェーブ開始前のクリーンアップ確認 ---
            if (activeEnemiesInWave > 0)
            {
                while (activeEnemiesInWave > 0)
                {
                    yield return new WaitForSeconds(0.5f);
                }
                yield return new WaitForSeconds(1.0f);
            }

            if (path != null && prefab != null)
            {
                spawningInProgress = true;
                activeEnemiesInWave = 0;

                // 終盤フェーズ用のパターン決定 (波の開始時に1回だけ決める)
                int patternType = 0; // 0: Dual Columns, 1: Chaotic Swarm, 2: Classic Row
                BezierPath fixedPath1 = null;
                BezierPath fixedPath2 = null;

                if (currentPhase == SpawnerPhase.Late)
                {
                    patternType = Random.Range(0, 3);
                    
                    if (patternType == 0) // Dual Columns (Symmetrical)
                    {
                        string[] types = { "Mid", "High", "Low", "Curve", "Dive", "Arch" };
                        string chosenType = types[Random.Range(0, types.Length)];

                        fixedPath1 = paths.Find(p => p.note == "Side Left " + chosenType);
                        fixedPath2 = paths.Find(p => p.note == "Side Right " + chosenType);
                        
                        // 万が一見つからない場合のフォールバック
                        if (fixedPath1 == null || fixedPath2 == null)
                        {
                            List<BezierPath> lefts = paths.FindAll(p => p.note.Contains("Side Left"));
                            List<BezierPath> rights = paths.FindAll(p => p.note.Contains("Side Right"));
                            fixedPath1 = (lefts.Count > 0) ? lefts[Random.Range(0, lefts.Count)] : null;
                            fixedPath2 = (rights.Count > 0) ? rights[Random.Range(0, rights.Count)] : null;
                        }
                        Debug.Log($"EnemySpawner: Pattern [Symmetric Dual Columns] - Type: {chosenType}");
                    }
                    else if (patternType == 2) // Classic Row (Mixed)
                    {
                        List<BezierPath> allMobPaths = paths.FindAll(p => !p.note.Contains("Boss Entry"));
                        fixedPath1 = (allMobPaths.Count > 0) ? allMobPaths[Random.Range(0, allMobPaths.Count)] : null;
                        Debug.Log($"EnemySpawner: Pattern [Classic Row] - Path:{fixedPath1?.note}");
                    }
                    else
                    {
                        Debug.Log("EnemySpawner: Pattern [Chaotic Swarm]");
                    }
                }

                for (int i = 0; i < count; i++)
                {
                    if (!keepSpawning) yield break;

                    if (currentPhase == SpawnerPhase.Late)
                    {
                        switch (patternType)
                        {
                            case 0: // Dual Columns (同時2列)
                                if (fixedPath1 != null && fixedPath2 != null)
                                {
                                    SpawnEnemy(fixedPath1, prefab, isBossType);
                                    SpawnEnemy(fixedPath2, prefab, isBossType);
                                }
                                break;

                            case 1: // Chaotic Swarm (1体ずつバラバラ、初期ルートも混在)
                                List<BezierPath> allMobPaths = paths.FindAll(p => !p.note.Contains("Boss Entry"));
                                BezierPath randomPath = (allMobPaths.Count > 0) ? allMobPaths[Random.Range(0, allMobPaths.Count)] : null;
                                if (randomPath != null) SpawnEnemy(randomPath, prefab, isBossType);
                                break;

                            case 2: // Classic Row (1体ずつ、同じルートで並んでくる)
                                if (fixedPath1 != null) SpawnEnemy(fixedPath1, prefab, isBossType);
                                break;
                        }
                    }
                    else
                    {
                        // 序盤などは元々のルートを使って整列して出てくる
                        SpawnEnemy(path, prefab, isBossType);
                    }

                    yield return new WaitForSeconds(敵と敵の間隔);
                }
                
                spawningInProgress = false;
            }
            
            // ボスフェーズならループを抜けて待機（StageManagerの終了を待つ）
            if (currentPhase == SpawnerPhase.FinalBoss)
            {
                yield break;
            }

            // 中ボスフェーズなら、撃破されるまでここでループを止める
            bool wasMidBoss = (currentPhase == SpawnerPhase.MidBoss);
            if (wasMidBoss)
            {
                while (currentPhase == SpawnerPhase.MidBoss)
                {
                    yield return new WaitForSeconds(0.1f); // 1.0sから0.1sへ短縮してレスポンス向上
                }
                // 中ボスを倒した/逃げた直後はウェーブカウントをリセットせず続行
                // 次の敵をすぐ出すために待ち時間をスキップするフラグとして使用
            }

            waveCount++;
            
            // ボス戦の直後は、余韻を楽しませるよりも即座に雑魚を出す（ユーザー要望）
            if (!wasMidBoss)
            {
                yield return new WaitForSeconds(次のウェーブまでの待ち時間);
            }
            else
            {
                Debug.Log("BOSS FINISHED: Skipping wait time for immediate transition.");
                yield return new WaitForSeconds(0.5f); // ほんの少しだけ間を置く
            }
        }
    }

    // --- Helper Methods for Wave Logic ---
    GameObject GetSpecificPrefab(string namePart)
    {
        foreach (var p in enemyPrefabs)
        {
            if (p != null && p.name.Contains(namePart)) return p;
        }
        return null;
    }

    GameObject GetRandomMobPrefab()
    {
        List<GameObject> mobs = new List<GameObject>();
        string[] targetNames;

        if (currentPhase == SpawnerPhase.Early)
        {
            // 序盤：Prefab 1, 2
            targetNames = new string[] { "Prefab1", "Prefab2" };
        }
        else
        {
            // 終盤：Prefab 5, 6, 7
            targetNames = new string[] { "Prefab5", "Prefab6", "Prefab7" };
        }

        foreach (var p in enemyPrefabs)
        {
            if (p == null) continue;
            foreach (var name in targetNames)
            {
                if (p.name.Contains(name))
                {
                    mobs.Add(p);
                    break;
                }
            }
        }
        
        return mobs.Count > 0 ? mobs[Random.Range(0, mobs.Count)] : null;
    }

    BezierPath GetBossPath()
    {
        return paths.Find(p => p.note.Contains("Boss Entry"));
    }

    BezierPath GetRandomMobPath()
    {
        // 終盤フェーズなら「Side」が含まれるパスを優先的に選ぶ
        if (currentPhase == SpawnerPhase.Late)
        {
            List<BezierPath> sidePaths = paths.FindAll(p => p.note.Contains("Side"));
            if (sidePaths.Count > 0)
            {
                return sidePaths[Random.Range(0, sidePaths.Count)];
            }
        }

        // 通常時、またはSideパスがない場合
        List<BezierPath> mobPaths = paths.FindAll(p => !p.note.Contains("Boss Entry") && !p.note.Contains("Side"));
        if (mobPaths.Count == 0) mobPaths = paths; // 予備
        
        return mobPaths.Count > 0 ? mobPaths[Random.Range(0, mobPaths.Count)] : null;
    }

    public void OnEnemyRemoved(Vector3 lastLocalPos, bool wasDefeated, int currentHP, bool isBoss)
    {
        activeEnemiesInWave--;

        // 中ボス(Prefab 3)の処理
        if (isBoss && currentPhase == SpawnerPhase.MidBoss)
        {
            if (currentHP <= 0)
            {
                // 撃破成功
                Debug.Log("MID BOSS DEFEATED! Transitioning to LATE phase.");
                currentPhase = SpawnerPhase.Late;
                // ボスのHPを保存しない（撃破されたため）
                bossPersistentHP = -1;
            }
            else
            {
                // 逃走（倒せなかった）
                Debug.Log("MID BOSS ESCAPED! Reverting to EARLY phase and resetting waves.");
                currentPhase = SpawnerPhase.Early;
                waveCount = 0; // ウェーブを最初からやり直して雑魚敵1-2を出す
                // ボスのHPを保存（次回登場時に引き継ぐ）
                bossPersistentHP = currentHP;
            }
        }
        else if (isBoss)
        {
            // それ以外のボス（ラスボス等）のHP保存
            if (currentHP <= 0) bossPersistentHP = -1;
            else bossPersistentHP = currentHP;
        }

        if (activeEnemiesInWave <= 0 && !spawningInProgress)
        {
            if (wasDefeated && bonusPrefab != null)
            {
                SpawnBonus(lastLocalPos);
            }
        }
    }

    void SpawnBonus(Vector3 localPos)
    {
        GameObject bonus = Instantiate(bonusPrefab, transform);
        bonus.transform.localPosition = localPos;
        bonus.transform.localScale = Vector3.one;
    }

    void SpawnEnemy(BezierPath path, GameObject prefab, bool forceBossMode = false)
    {
        Vector3 localStart = MapDesignToLocal(path.start);
        Vector3 localControl = MapDesignToLocal(path.control);
        Vector3 localEnd = MapDesignToLocal(path.end);

        GameObject enemy = Instantiate(prefab, transform);
        enemy.transform.localPosition = localStart;
        enemy.transform.localRotation = Quaternion.identity;
        enemy.transform.localScale = Vector3.one;

        EnemyController controller = enemy.GetComponent<EnemyController>();
        if (controller != null)
        {
            bool isRightSide = path.start.x > (designResolution.x / 2f);
            controller.Initialize(localStart, localControl, localEnd, this, isRightSide);
            activeEnemiesInWave++;

            // 明示的にボスとして扱う
            if (forceBossMode)
            {
                controller.ボス設定 = true;
                if (bossPersistentHP > 0) controller.SetCurrentHP(bossPersistentHP);
            }
        }
    }

    // Maps Design Pixels (e.g. 1080x1920) directly to the Spawner's Rect bounds.
    // This is the MOST reliable way for UI systems.
    Vector3 MapDesignToLocal(Vector3 designPos)
    {
        if (rectTransform == null) rectTransform = GetComponent<RectTransform>();
        
        // 1. Normalize based on origin
        float nx, ny;
        if (coordinateOrigin == OriginType.Center)
        {
            nx = (designPos.x / designResolution.x) + 0.5f;
            ny = (designPos.y / designResolution.y) + 0.5f;
        }
        else
        {
            nx = designPos.x / designResolution.x;
            ny = designPos.y / designResolution.y;
        }

        // 2. Map normalized (0-1) to actual Spawner Rect (handles all pivot/size)
        float localX = Mathf.Lerp(rectTransform.rect.xMin, rectTransform.rect.xMax, nx);
        float localY = Mathf.Lerp(rectTransform.rect.yMin, rectTransform.rect.yMax, ny);

        return new Vector3(localX, localY, designPos.z);
    }

    BezierPath GetRandomPath() => paths.Count > 0 ? paths[Random.Range(0, paths.Count)] : null;
    GameObject GetRandomEnemyPrefab() => enemyPrefabs.Length > 0 ? enemyPrefabs[Random.Range(0, enemyPrefabs.Length)] : null;

    void SetupDefaultPaths()
    {
        paths.Add(new BezierPath(new Vector3(431.2f, 2000f, 0), new Vector3(390f, 640f, 0), new Vector3(-160f, 280f, 0), Color.red) { note = "Original Red" });
        paths.Add(new BezierPath(new Vector3(680f, 2000f, 0), new Vector3(697.2f, 570.1f, 0), new Vector3(1271f, 273.7f, 0), Color.green) { note = "Original Green" });
        paths.Add(new BezierPath(new Vector3(-500f, 2000f, 0), new Vector3(100f, 1000f, 0), new Vector3(500f, 0f, 0), Color.yellow) { note = "Wide Curve" });
        
        // Enemy 3 (Boss) Entry: 上から出現して、画面中央寄り(Y=1100付近)へ
        paths.Add(new BezierPath(new Vector3(540f, 2200f, 0), new Vector3(540f, 1800f, 0), new Vector3(540f, 1100f, 0), Color.cyan) { note = "Boss Entry" });

        // --- Side Entry Paths (Late Phase用) ---
        // 左から右へ横切るパス
        paths.Add(new BezierPath(new Vector3(-200f, 1500f, 0), new Vector3(540f, 1300f, 0), new Vector3(1280f, 1100f, 0), Color.magenta) { note = "Side Left" });
        // 右から左へ横切るパス
        paths.Add(new BezierPath(new Vector3(1280f, 1600f, 0), new Vector3(540f, 1400f, 0), new Vector3(-200f, 1200f, 0), Color.magenta) { note = "Side Right" });
    }

    void OnDrawGizmos()
    {
        if (paths == null || paths.Count == 0) return;
        if (rectTransform == null) rectTransform = GetComponent<RectTransform>();

        foreach (var p in paths)
        {
            Gizmos.color = p.gizmoColor;
            
            // Gizmos are in World Space, so we multiply Local -> World
            Vector3 worldStart = transform.TransformPoint(MapDesignToLocal(p.start));
            Vector3 worldControl = transform.TransformPoint(MapDesignToLocal(p.control));
            Vector3 worldEnd = transform.TransformPoint(MapDesignToLocal(p.end));

            Gizmos.DrawSphere(worldStart, 15f);
            Gizmos.DrawSphere(worldEnd, 15f);
            Gizmos.DrawWireSphere(worldControl, 10f);

            Vector3 prev = worldStart;
            for (float t = 0.05f; t <= 1.01f; t += 0.05f)
            {
                float u = 1 - t;
                Vector3 pos = (u * u) * worldStart + (2 * u * t) * worldControl + (t * t) * worldEnd;
                Gizmos.DrawLine(prev, pos);
                prev = pos;
            }
        }
    }
}
