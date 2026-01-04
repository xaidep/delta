using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections;

public class GameOverManager : MonoBehaviour
{
    [Header("UI Components")]
    public TextMeshProUGUI textCount;
    public Button buttonContinue; // button_pink
    public Button buttonHome;     // button_blue

    [Header("Settings")]
    public int countdownStart = 10;
    
    private int currentCount;
    private bool isCountingDown = false;

    void Start()
    {
        currentCount = countdownStart;
        if (textCount != null)
        {
            textCount.text = currentCount.ToString();
        }

        // Initialize button listeners
        if (buttonContinue != null)
        {
            buttonContinue.onClick.AddListener(OnContinuePressed);
        }

        if (buttonHome != null)
        {
            buttonHome.onClick.AddListener(OnHomePressed);
        }
    }

    /// <summary>
    /// Called by Animation Event at the end of the Game Over entry animation.
    /// </summary>
    public void Stop()
    {
        Debug.Log("Game Over Animation Finished. Starting Countdown.");
        StartCoroutine(StartCountdown());
    }

    IEnumerator StartCountdown()
    {
        if (isCountingDown) yield break;
        isCountingDown = true;

        while (currentCount > 0)
        {
            yield return new WaitForSecondsRealtime(1f);
            currentCount--;

            if (textCount != null)
            {
                textCount.text = currentCount.ToString();
            }
        }

        // Countdown reached 0
        OnCountdownFinished();
    }

    void OnCountdownFinished()
    {
        Debug.Log("Countdown Finished. Disabling Continue Button.");
        if (buttonContinue != null)
        {
            buttonContinue.interactable = false;
        }
    }

    void OnContinuePressed()
    {
        Debug.Log("Continue Pressed. Reloading Scene.");
        // Resume time and reload current scene
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    void OnHomePressed()
    {
        Debug.Log("Home Pressed. (Placeholder)");
        // Placeholder for home screen logic
        // SceneManager.LoadScene("HomeScene"); 
    }
}
