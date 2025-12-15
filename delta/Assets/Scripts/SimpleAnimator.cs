using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI; // UI用

public class SimpleAnimator : MonoBehaviour
{
    [Header("Animation Settings")]
    public List<Sprite> sprites; // アニメーション画像リスト
    public float animationSpeed = 0.1f; // 切り替え速度
    
    private Image uiImage;
    private SpriteRenderer spriteRenderer;
    private int currentIndex = 0;
    private float timer = 0f;

    void Start()
    {
        uiImage = GetComponent<Image>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        if (sprites == null || sprites.Count == 0) return;

        timer += Time.deltaTime;
        if (timer >= animationSpeed)
        {
            timer = 0f;
            currentIndex = (currentIndex + 1) % sprites.Count; // 次の画像へ（ループ）
            
            // 画像更新
            Sprite nextSprite = sprites[currentIndex];
            
            if (uiImage != null) uiImage.sprite = nextSprite;
            if (spriteRenderer != null) spriteRenderer.sprite = nextSprite;
        }
    }
}
