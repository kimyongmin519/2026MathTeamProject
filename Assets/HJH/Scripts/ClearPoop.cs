using TMPro;
using UnityEngine;
using UnityEngine.Events;

public class ClearPoop : MonoBehaviour
{
    public BabyTimer babyTimer;
    public UnityEvent onClear;
    private bool isCleared;

    [SerializeField] private TextMeshProUGUI clearText;

    private void Start()
    {
        clearText.gameObject.SetActive(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isCleared) return;
        if (!other.CompareTag("Player")) return;

        isCleared = true;
        babyTimer.StopTimer();
        onClear?.Invoke();
    }

    public void Clear()
    {
        clearText.gameObject.SetActive(true);
    }
}