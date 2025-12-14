using UnityEngine;
using UnityEngine.UI;

public class BackgroundScroller : MonoBehaviour
{
    // スクロール速度（Y軸）
    public float scrollSpeed = 0.5f;

    // --- テクスチャアニメーション設定 ---
    [Header("Animation Settings")]
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

    void Update()
    {
        // --- 1. スクロール処理 ---
        float y = Mathf.Repeat(Time.time * scrollSpeed, 1);
        
        if (rawImage != null)
        {
            Rect currentRect = rawImage.uvRect;
            // スクロール位置だけ更新し、タイリング幅(w, h)は維持する
            currentRect.y = y * currentRect.height; // タイリング数に合わせてスクロール速度も見た目上調整する場合
            // スクロール方向を逆にしました（上に向かって進む＝背景が下に流れる）
            currentRect.y = y; 
            
            rawImage.uvRect = currentRect;
        }

        // --- 2. アニメーション処理 ---
        if (animationTextures != null && animationTextures.Length > 0)
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
