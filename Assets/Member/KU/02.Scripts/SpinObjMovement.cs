using UnityEngine;

public class SpinObjMovement : MonoBehaviour
{
    [Header("회전 속도")]
    [SerializeField] private Vector3 rotationSpeed = new Vector3(0f, 20f, 0f);

    private void Update()
    {
        transform.Rotate(rotationSpeed * Time.deltaTime);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {

        }
    }
}
