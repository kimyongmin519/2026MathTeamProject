using System;
using System.Collections;
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
        StartCoroutine(ClearTime());
    }

    private IEnumerator ClearTime()
    {
        clearText.gameObject.SetActive(true);
        yield return new WaitForSeconds(2f);
        clearText.text = "I'm Yongmin who was born in 25, but your butt is nothing~";
    }
}