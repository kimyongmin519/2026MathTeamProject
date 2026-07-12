using System.Collections;
using UnityEngine;

public class PivotRotate : MonoBehaviour
{
    [Header("Pivot 기준점")]
    [SerializeField] private Transform pivotPoint;

    [Header("회전 설정")]
    [SerializeField] private Vector3 rotateAxis = Vector3.up;
    [SerializeField] private float targetAngle = 95f;
    [SerializeField] private float rotateSpeed = 240f;

    private bool isRotating;

    public bool IsRotating => isRotating;

    public void RotateOnce()
    {
        if (!isRotating)
        {
            StartCoroutine(RotateOnceCoroutine());
        }
    }

    public IEnumerator RotateOnceCoroutine()
    {
        if (isRotating)
        {
            yield break;
        }

        isRotating = true;

        Vector3 startPosition = transform.position;
        Quaternion startRotation = transform.rotation;

        Vector3 axis = rotateAxis.normalized;

        // 내려가기
        yield return RotateByAngle(axis, targetAngle);

        // 원래 자리로 돌아오기
        yield return RotateByAngle(axis, -targetAngle);

        // 반복하다 보면 미세하게 위치/회전이 틀어질 수 있어서 정확히 복구
        transform.position = startPosition;
        transform.rotation = startRotation;

        isRotating = false;
    }

    private IEnumerator RotateByAngle(Vector3 axis, float angle)
    {
        float rotatedAngle = 0f;
        float direction = Mathf.Sign(angle);
        float target = Mathf.Abs(angle);

        while (rotatedAngle < target)
        {
            float step = Mathf.Max(0.01f, rotateSpeed) * Time.deltaTime;

            if (rotatedAngle + step > target)
            {
                step = target - rotatedAngle;
            }

            float finalStep = step * direction;

            if (pivotPoint != null)
            {
                transform.RotateAround(pivotPoint.position, axis, finalStep);
            }
            else
            {
                transform.Rotate(axis, finalStep, Space.Self);
            }

            rotatedAngle += step;

            yield return null;
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {

        }
    }
}