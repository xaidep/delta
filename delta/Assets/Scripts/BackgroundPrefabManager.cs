using UnityEngine;
using System.Collections.Generic;

public class BackgroundPrefabManager : MonoBehaviour
{
    [Header("Settings")]
    public GameObject[] backgroundPrefabs;
    public float scrollSpeed = 200f; // Pixels per second
    public RectTransform container;
    
    [Tooltip("If 0, it uses the root RectTransform height of the prefab. If your prefab root is 100x100, set this to the actual image height (e.g. 1080).")]
    public float manualPrefabHeight = 0f;

    private List<RectTransform> activeBackgrounds = new List<RectTransform>();
    private int nextPrefabIndex = 0;
    private float prefabHeight = 0f;

    void Start()
    {
        if (backgroundPrefabs == null || backgroundPrefabs.Length == 0)
        {
            Debug.LogError("BackgroundPrefabManager: No prefabs assigned!");
            return;
        }

        if (container == null) container = GetComponent<RectTransform>();

        // 最初の背景を生成して、画面全体を埋めるまでループ
        float currentY = 0f; // 画面の上端(y=0)から開始
        float screenHeight = container.rect.height > 0 ? container.rect.height : 2000f;

        // 画面を覆い尽くすまで（下方向に）生成
        // ピボット(0.5, 1)なので、anchoredPosition.y=0 は上端がコンテナの最上部にある状態
        // そこから 1つ分ずつ下にずらして生成していく
        while (currentY > -screenHeight - 1000f) // 画面下端を越えるまで
        {
            SpawnBackground(new Vector2(0, currentY));
            currentY -= prefabHeight; // 下にずらす
        }
    }

    void Update()
    {
        if (activeBackgrounds.Count == 0) return;

        // すべての背景を移動
        float moveAmount = scrollSpeed * Time.deltaTime;
        for (int i = 0; i < activeBackgrounds.Count; i++)
        {
            activeBackgrounds[i].anchoredPosition += Vector2.down * moveAmount;
        }

        // 最上部の背景の上端がコンテナの上端(y=0)より下に来たら、新しい背景を上に繋げる
        RectTransform topBG = activeBackgrounds[activeBackgrounds.Count - 1];
        if (topBG.anchoredPosition.y <= 0)
        {
            SpawnBackground(new Vector2(0, topBG.anchoredPosition.y + prefabHeight));
        }

        // 一番下の背景が画面外（下端）に完全に出たら削除
        // 画面の高さ + 背景の高さ 分だけ下がったら削除
        float screenHeight = container.rect.height > 0 ? container.rect.height : 2000f;
        RectTransform bottomBG = activeBackgrounds[0];
        if (bottomBG.anchoredPosition.y <= -screenHeight - prefabHeight)
        {
            activeBackgrounds.RemoveAt(0);
            Destroy(bottomBG.gameObject);
        }
    }

    private void SpawnBackground(Vector2 position)
    {
        GameObject prefab = backgroundPrefabs[nextPrefabIndex];
        GameObject go = Instantiate(prefab, container);
        RectTransform rt = go.GetComponent<RectTransform>();

        if (rt != null)
        {
            // 重要：プレハブの設定に寄らず、スクリプトで強制的に「上端中央」基準にする
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            
            // 初回生成時または手動設定がある場合に高さを確定
            if (prefabHeight == 0)
            {
                if (manualPrefabHeight > 0)
                {
                    prefabHeight = manualPrefabHeight;
                }
                else
                {
                    prefabHeight = rt.rect.height;
                    // もし高さが極端に小さい(100以下など)場合は警告
                    if (prefabHeight <= 100.1f)
                    {
                        Debug.LogWarning($"BackgroundPrefabManager: Prefab {prefab.name} height is very small ({prefabHeight}). Check root RectTransform size or use ManualPrefabHeight!");
                    }
                }
            }

            rt.anchoredPosition = position;
            activeBackgrounds.Add(rt);
        }

        // 次のプレハブへ（ループ）
        nextPrefabIndex = (nextPrefabIndex + 1) % backgroundPrefabs.Length;
    }

    public void SetSpeed(float speed)
    {
        scrollSpeed = speed;
    }
}
