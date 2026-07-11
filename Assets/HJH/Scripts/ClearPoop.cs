using UnityEngine;
using UnityEngine.Events;

public class ClearPoop : MonoBehaviour
{
    public BabyTimer babyTimer;
    public UnityEvent onClear;
    private bool isCleared;

    private void OnTriggerEnter(Collider other)
    {
        if (isCleared) return;
        if (!other.CompareTag("Player")) return;

        isCleared = true;
        babyTimer.StopTimer();
        onClear?.Invoke();
    }
}