using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace Member.KYM.Scripts.Players
{
    /// <summary>
    /// 3, 2, 1 카운트다운을 표시한 뒤 시작 메시지와 UnityEvent를 실행합니다.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class CountdownUI : MonoBehaviour
    {
        [Header("UI")]
        // 카운트다운 숫자와 시작 문구를 표시할 TextMeshPro UI 텍스트입니다.
        [SerializeField] private TMP_Text countdownText;

        [Header("Countdown")]
        // 활성화될 때 카운트다운을 자동으로 시작할지 결정합니다.
        [SerializeField] private bool playOnEnable = true;
        // 처음 표시할 숫자입니다. 기본값 3이면 3, 2, 1 순서로 표시합니다.
        [SerializeField, Min(1)] private int startCount = 3;
        // 각 숫자가 화면에 유지되는 시간입니다.
        [SerializeField, Min(0f)] private float numberDisplayDuration = 1f;
        // 숫자 카운트가 끝난 뒤 표시할 문구입니다.
        [SerializeField] private string startMessage = "시작!";
        // 시작 문구가 화면에 유지되는 시간입니다.
        [SerializeField, Min(0f)] private float startMessageDuration = 0.8f;
        // Time.timeScale이 0이어도 카운트다운이 진행되도록 실제 시간을 사용할지 결정합니다.
        [SerializeField] private bool useUnscaledTime = true;
        // 시작 문구 표시가 끝난 뒤 텍스트 오브젝트를 숨길지 결정합니다.
        [SerializeField] private bool hideTextAfterCountdown = true;

        [Header("Sound")]
        // 카운트다운 효과음을 재생할 AudioSource입니다. 비어 있으면 같은 오브젝트에서 자동으로 가져옵니다.
        [SerializeField] private AudioSource audioSource;
        // 3, 2, 1 숫자가 표시될 때마다 한 번씩 재생할 효과음입니다.
        [SerializeField] private AudioClip countSound;
        // "시작!" 문구가 표시될 때 재생할 효과음입니다.
        [SerializeField] private AudioClip startSound;
        // 카운트다운과 시작 효과음의 재생 음량입니다.
        [SerializeField, Range(0f, 1f)] private float soundVolume = 1f;

        [Header("Events")]
        // "시작!" 문구가 표시되는 순간 한 번 호출됩니다.
        [SerializeField] private UnityEvent onCountdownStarted;

        public bool IsCountingDown => _countdownCoroutine != null;

        private Coroutine _countdownCoroutine;

        private void Awake()
        {
            if (countdownText == null)
            {
                countdownText = GetComponentInChildren<TMP_Text>(true);
            }

            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
            }

            audioSource.playOnAwake = false;
        }

        private void OnEnable()
        {
            if (playOnEnable)
            {
                StartCountdown();
            }
        }

        private void OnDisable()
        {
            StopCountdown();
        }

        /// <summary>
        /// 진행 중인 카운트다운을 처음부터 다시 시작합니다.
        /// </summary>
        public void StartCountdown()
        {
            StopCountdown();

            if (countdownText == null)
            {
                Debug.LogWarning($"{nameof(CountdownUI)}에 표시할 TMP_Text가 연결되지 않았습니다.", this);
                return;
            }

            countdownText.gameObject.SetActive(true);
            _countdownCoroutine = StartCoroutine(CountdownRoutine());
        }

        /// <summary>
        /// 현재 진행 중인 카운트다운을 중지합니다.
        /// </summary>
        public void StopCountdown()
        {
            if (_countdownCoroutine == null)
            {
                return;
            }

            StopCoroutine(_countdownCoroutine);
            _countdownCoroutine = null;
        }

        private IEnumerator CountdownRoutine()
        {
            for (int count = startCount; count >= 1; count--)
            {
                countdownText.text = count.ToString();
                PlaySound(countSound);
                yield return Wait(numberDisplayDuration);
            }

            countdownText.text = startMessage;
            PlaySound(startSound);
            onCountdownStarted?.Invoke();

            yield return Wait(startMessageDuration);

            if (hideTextAfterCountdown)
            {
                countdownText.gameObject.SetActive(false);
            }

            _countdownCoroutine = null;
        }

        private void PlaySound(AudioClip clip)
        {
            if (audioSource == null || clip == null)
            {
                return;
            }

            audioSource.PlayOneShot(clip, soundVolume);
        }

        private IEnumerator Wait(float duration)
        {
            if (duration <= 0f)
            {
                yield return null;
                yield break;
            }

            if (useUnscaledTime)
            {
                yield return new WaitForSecondsRealtime(duration);
            }
            else
            {
                yield return new WaitForSeconds(duration);
            }
        }
    }
}
