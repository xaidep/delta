using UnityEngine;
using UnityEditor;
using System.Linq;
using System.Collections.Generic;

public class SetupEnemy3 : MonoBehaviour
{
    [MenuItem("Tools/Setup Enemy 3")]
    public static void Setup()
    {
        // 1. Prefabをロード
        string prefabPath = "Assets/design/prefab/Enemy Prefab3.prefab";
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

        if (prefab == null)
        {
            Debug.LogError($"Prefab not found at {prefabPath}");
            return;
        }

        // 2. Sprite Sheetから全スプライトをロード
        // NOTE: enemy3-SheetがEnemy2と同じ見た目であるため、boss01-Sheetを使用する
        string spriteSheetPath = "Assets/design/image/boss01-Sheet.png";
        Object[] allAssets = AssetDatabase.LoadAllAssetsAtPath(spriteSheetPath);
        Sprite[] sprites = allAssets.OfType<Sprite>().OrderBy(s => s.name).ToArray();

        if (sprites.Length == 0)
        {
            Debug.LogError($"No sprites found at {spriteSheetPath}");
            return;
        }

        // 3. EnemyControllerを取得して設定
        EnemyController controller = prefab.GetComponent<EnemyController>();
        if (controller == null)
        {
            Debug.LogError("EnemyController component missing on prefab.");
            return;
        }

        // Undo登録
        Undo.RecordObject(controller, "Setup Enemy 3");

        // アニメーション設定
        controller.アニメーション画像リスト = sprites;
        controller.敵ID = "Enemy3";
        controller.アニメーション速度FPS = 12f;

        // Imageコンポーネントの画像も更新（Editor上で見た目を反映）
        UnityEngine.UI.Image img = prefab.GetComponent<UnityEngine.UI.Image>();
        if (img != null && sprites.Length > 0)
        {
            img.sprite = sprites[0];
            img.color = Color.white; // 色は元に戻す
            EditorUtility.SetDirty(img);
        }

        // ボス設定
        controller.ボス設定 = true;
        controller.ボス滞在時間 = 20f;
        controller.ボス左右移動幅 = 200f; // 左右に200px振幅
        controller.ボス移動速度 = 2.0f; // 速度
        controller.敵の大きさ倍率 = 2.0f; // 大きく
        controller.最大HP = 50; // 硬く


        // 保存
        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();

        Debug.Log($"Enemy 3 Setup Complete! Assigned {sprites.Length} sprites.");

        // 4. シーン内のEnemySpawnerに自動登録
#if UNITY_2023_1_OR_NEWER
        EnemySpawner spawner = Object.FindFirstObjectByType<EnemySpawner>();
#else
        EnemySpawner spawner = Object.FindObjectOfType<EnemySpawner>();
#endif
        if (spawner != null)
        {
            // まず既存のPrefab3があれば削除して、新しいものを追加（重複防止）
            var list = spawner.enemyPrefabs != null ? spawner.enemyPrefabs.ToList() : new List<GameObject>();
            
            // 古い参照やnullを除去
            list.RemoveAll(p => p == null || p.name.Contains("Prefab3"));
            
            // 新しいPrefabを追加
            list.Add(prefab);
            
            spawner.enemyPrefabs = list.ToArray();
            EditorUtility.SetDirty(spawner);

            Debug.Log("Enemy 3 Registered to Spawner.");
            Debug.Log("Current Enemy Prefabs in Spawner:");
            foreach(var p in spawner.enemyPrefabs)
            {
                Debug.Log("- " + (p != null ? p.name : "null"));
            }
        }
        else
        {
            Debug.LogWarning("EnemySpawner not found in the current scene. Please add Enemy Prefab3 to the spawner manually.");
        }
    }
}
