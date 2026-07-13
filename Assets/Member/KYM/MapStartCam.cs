using System;
using System.Collections;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Events;

namespace Member.KYM
{
    public class MapStartCam : MonoBehaviour
    {
        [SerializeField] private float delay;
        [SerializeField] private float invokeDelay;
        public UnityEvent OnEvent;

        private CinemachineCamera _camera;

        private void Awake()
        {
            _camera = GetComponent<CinemachineCamera>();
        }

        private void Start()
        {
            StartCoroutine(Cor());
        }

        private IEnumerator Cor()
        {
            yield return new WaitForSeconds(delay);
            _camera.Priority = -100;
            yield return new WaitForSeconds(invokeDelay);
            OnEvent?.Invoke();
        }
    }
}
