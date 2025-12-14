using UnityEngine;

public class BulletController : MonoBehaviour
{
    // 弾の移動速度
    // UI(Canvas)で使用する場合は、数値を大きくしてください（例：500〜1000）
    public float speed = 10f;

    void Start()
    {
        // もしUIコンポーネント(RectTransform)がついている場合、
        // 速度がデフォルト(10)のままだと遅すぎるため、自動補正する
        if (GetComponent<RectTransform>() != null && speed == 10f)
        {
            speed = 600f; // UI用のデフォルト速度
        }

        // 3秒後に消滅
        Destroy(gameObject, 3f);
    }

    void Update()
    {
        // 上方向へ移動
        transform.Translate(Vector3.up * speed * Time.deltaTime);
    }
}
