using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PivotRotateManager : MonoBehaviour
{
    [Header("순서대로 회전시킬 오브젝트들")]
    [SerializeField] private List<PivotRotate> pivotRotates = new List<PivotRotate>();

    [Header("시작하자마자 실행할지")]
    [SerializeField] private bool playOnStart = true;

    [Header("계속 반복할지")]
    [SerializeField] private bool loop = true;

    private Coroutine sequenceCoroutine;

    private void Start()
    {
        if (playOnStart)
        {
            StartSequence();
        }
    }

    public void StartSequence()
    {
        if (sequenceCoroutine == null)
        {
            sequenceCoroutine = StartCoroutine(SequenceCoroutine());
        }
    }

    public void StopSequence()
    {
        if (sequenceCoroutine != null)
        {
            StopCoroutine(sequenceCoroutine);
            sequenceCoroutine = null;
        }
    }

    private IEnumerator SequenceCoroutine()
    {
        do
        {
            for (int i = 0; i < pivotRotates.Count; i++)
            {
                PivotRotate pivotRotate = pivotRotates[i];

                if (pivotRotate == null)
                {
                    continue;
                }

                yield return pivotRotate.RotateOnceCoroutine();
            }

        } while (loop);

        sequenceCoroutine = null;
    }
}