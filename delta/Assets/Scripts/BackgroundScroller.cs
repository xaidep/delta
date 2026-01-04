using UnityEngine;
using UnityEngine.UI;

public class BackgroundScroller : MonoBehaviour
{
    // スクロール速度（Y軸）
    public float scrollSpeed = 0.5f;
    public float speedMultiplier = 2.0f;

    // --- テクスチャアニメーション設定 ---
    // --- テクスチャアニメーション設定 ---
    [Header("Animation Settings")]
    public bool enableAnimation = false; // アニメーションするかどうか
    public Texture[] animationTextures; // アニメーションさせる画像リスト
    public float animationInterval = 0.5f; // 切り替え間隔（秒）

    // --- タイル設定 ---
    [Header("Tiling Settings")]
    // テクスチャ1枚あたりのサイズ（ピクセル）
    // 64x64なら 64 を指定
    public float textureSize = 64f; 
    public bool autoTile = true; // 画面サイズに合わせて自動で敷き詰めるか

    private RawImage rawImage;
    private int currentTextureIndex = 0;
    private float animationTimer = 0f;
    private RectTransform rectTransform;

    void Start()
    {
        rawImage = GetComponent<RawImage>();
        rectTransform = GetComponent<RectTransform>();

        // 自動タイリング設定
        if (autoTile && rawImage != null && rectTransform != null)
        {
            UpdateTiling();
        }
    }

    private float stageDuration = 0f;
    private bool useDuration = false;
    private float currentTime = 0f;

    public void SetDuration(float duration)
    {
        stageDuration = duration;
        useDuration = true;
        currentTime = 0f;
    }

    void Update()
    {
        if (rawImage == null) return;

        Rect currentRect = rawImage.uvRect;
        float y = 0f;

        if (useDuration && stageDuration > 0)
        {
            currentTime += Time.deltaTime;
            float progress = Mathf.Clamp01(currentTime / stageDuration);
            
            // 画像の端（1.0）を超えて参照すると引き伸びてしまうため、
            // 表示されている高さ分（currentRect.height）だけ手前で止める
            if (currentRect.height >= 0.99f)
            {
                Debug.LogWarning("BackgroundScroller: UV Rect Height is close to 1. Scrolling will not be visible! Set H to 0.2 approx.");
            }

            float maxY = Mathf.Max(0, 1.0f - currentRect.height);
            
            // 0 から maxY まで進む
            // 倍率をかけて、その時間内に何倍の距離を進むか（結果的にループする）
            y = Mathf.Repeat(Mathf.Lerp(0f, maxY, progress) * speedMultiplier, 1.0f);
            
            // Debug.Log($"BG Scroll: Time={currentTime}, Progress={progress}, Y={y}, MaxY={maxY}");
        }
        else
        {
            // 通常のループスクロール
            y = Mathf.Repeat(Time.time * scrollSpeed * speedMultiplier, 1);
        }

        // スクロール位置の適用
        currentRect.y = y; 
        rawImage.uvRect = currentRect;

        // --- 2. アニメーション処理 ---
        if (enableAnimation && animationTextures != null && animationTextures.Length > 0)
        {
            animationTimer += Time.deltaTime;
            if (animationTimer >= animationInterval)
            {
                animationTimer = 0f;
                currentTextureIndex = (currentTextureIndex + 1) % animationTextures.Length;
                if (rawImage != null)
                {
                    rawImage.texture = animationTextures[currentTextureIndex];
                }
            }
        }
    }

    // 画面(RectTransform)の大きさに合わせて、テクスチャを何回繰り返せばいいか計算する
    public void UpdateTiling()
    {
        if (rawImage == null || rectTransform == null || textureSize <= 0) return;

        // RawImageの幅と高さ
        float width = rectTransform.rect.width;
        float height = rectTransform.rect.height;

        // 何回繰り返すか (Canvasのピクセル数 / テクスチャのピクセル数)
        float tileX = width / textureSize;
        float tileY = height / textureSize;

        Rect uvRect = rawImage.uvRect;
        uvRect.width = tileX;
        uvRect.height = tileY;
        rawImage.uvRect = uvRect;
    }
}
