using UnityEngine;
using UnityEngine.Events;
using TMPro;

public class BabyTimer : MonoBehaviour
{
    public TextMeshProUGUI timerText;
    public UnityEvent isPoop;

    public float poopTime = 10f;

    private float elapsedTime;
    private bool isRunning;
    private bool hasPooped;

    public void StartTimer()
    {
        elapsedTime = 0f;
        isRunning = true;
        hasPooped = false;
    }

    public void StopTimer()
    {
        isRunning = false;
    }

    void Update()
    {
        if (!isRunning) return;

        elapsedTime += Time.deltaTime;
        UpdateTimerUI();
        CheckPoop();
    }

    void CheckPoop()
    {
        if (hasPooped) return;

        if (elapsedTime >= poopTime)
        {
            hasPooped = true;
            isRunning = false;
            isPoop?.Invoke();
        }
    }

    void UpdateTimerUI()
    {
        if (timerText == null) return;

        int minutes = Mathf.FloorToInt(elapsedTime / 60f);
        int seconds = Mathf.FloorToInt(elapsedTime % 60f);
        int milliseconds = Mathf.FloorToInt((elapsedTime * 100f) % 100f);

        timerText.text = $"{minutes:00}:{seconds:00}.{milliseconds:00}";
    }

    public float GetElapsedTime() => elapsedTime;
}