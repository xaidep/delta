using UnityEngine;
using TMPro;

public class BonusAnimation : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI textBonus;
    [SerializeField] private float destroyDelay = 2.0f;

    public void SetBonus(int amount)
    {
        if (textBonus != null)
        {
            textBonus.text = amount.ToString();
        }
        else
        {
            // If textBonus is not assigned, try to find it by name as requested
            Transform child = transform.Find("Text_bonus");
            if (child != null)
            {
                textBonus = child.GetComponent<TextMeshProUGUI>();
                if (textBonus != null)
                {
                    textBonus.text = amount.ToString();
                }
            }
        }

        // Automatically destroy after animation
        // Note: Using a slightly longer delay as a fallback
        Destroy(gameObject, 5.0f);

        // Start animation from the frame after the stop event (frame 1)
        Animator animator = GetComponent<Animator>();
        if (animator != null)
        {
            // Play the state named "bonusAnimation" (matching the .anim name usually)
            // Normalized time offset to skip the first frame (approx 1/60th of a second)
            animator.Play("bonusAnimation", 0, 0.02f);
            animator.speed = 1.0f;
        }
    }

    // This method can be called by an Animation Event at the end of the clip
    public void StopAnimation()
    {
        gameObject.SetActive(false);
        Destroy(gameObject);
    }
}
