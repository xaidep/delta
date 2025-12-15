using UnityEngine;

public class AutoDestroy : MonoBehaviour
{
    public float delay = 0.5f;

    void Start()
    {
        // アニメーションの長さに合わせるのがベストですが、簡易的に時間で消します
        Destroy(gameObject, delay);
    }
}
