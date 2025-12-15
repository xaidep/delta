using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class EffectController : MonoBehaviour
{
    [Header("Sorting Settings")]
    public string sortingLayerName = "Default";
    public int sortingOrder = 10; // プレイヤーや敵より手前にする

    [Header("Animation Settings")]
    public float animationSpeed = 0.1f; // 1コマの時間
    public List<Sprite> animationSprites; // アニメーションさせる画像のリスト（インスペクタで登録）

    private SpriteRenderer spriteRenderer;
    private UnityEngine.UI.Image uiImage;

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        uiImage = GetComponent<UnityEngine.UI.Image>();

        if (spriteRenderer != null)
        {
            spriteRenderer.sortingLayerName = sortingLayerName;
            spriteRenderer.sortingOrder = sortingOrder;
        }

        // アニメーション開始
        if (animationSprites != null && animationSprites.Count > 0)
        {
            StartCoroutine(PlayAnimation());
        }
    }

    IEnumerator PlayAnimation()
    {
        foreach (Sprite s in animationSprites)
        {
            if (spriteRenderer != null) spriteRenderer.sprite = s;
            if (uiImage != null) uiImage.sprite = s;
                
            yield return new WaitForSeconds(animationSpeed);
        }

        Destroy(gameObject);
    }
}
