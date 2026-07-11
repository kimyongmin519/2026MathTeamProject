using System.Collections;
using UnityEngine;

namespace Member.KYM.Scripts.Players
{
    /// <summary>
    /// 플레이어에게 붙여서 쓰는 간단한 사운드 재생 컴포넌트입니다.
    /// 정해진 시간마다 랜덤 클립을 재생하거나, UnityEvent에서 원하는 클립을 넘겨 즉시 재생할 수 있습니다.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(AudioSource))]
    public class PlayerSoundPlayer : MonoBehaviour
    {
        [Header("Audio Source")]
        // 실제로 소리를 재생할 AudioSource입니다. 비워두면 같은 오브젝트에서 자동으로 찾습니다.
        [SerializeField] private AudioSource audioSource;

        // true면 이전 소리를 끊지 않고 겹쳐서 재생합니다. 효과음 재생에는 보통 켜두는 게 좋습니다.
        [SerializeField] private bool usePlayOneShot = true;

        // 전체 재생 볼륨 배율입니다. 1이면 원본 볼륨 그대로 재생합니다.
        [SerializeField, Range(0f, 1f)] private float volumeScale = 1f;

        [Header("Engine Sound")]
        // 속도를 읽어올 플레이어 컨트롤러입니다. 비워두면 같은 오브젝트에서 자동으로 찾습니다.
        [SerializeField] private PlayerController playerController;
        // 엔진음을 전용으로 재생할 AudioSource입니다. 비워두면 실행 중 자동으로 하나를 추가해서 사용합니다.
        [SerializeField] private AudioSource engineAudioSource;
        // 움직이는 동안 반복 재생할 엔진 클립입니다.
        [SerializeField] private AudioClip engineClip;
        // true면 플레이어가 움직일 때 엔진음을 자동으로 재생하고, 멈추면 자동으로 끕니다.
        [SerializeField] private bool playEngineBySpeed = true;
        // 이 속도 이상부터 엔진음이 재생됩니다. 너무 낮으면 정지 중에도 소리가 날 수 있습니다.
        [SerializeField, Min(0f)] private float engineStartSpeed = 0.2f;
        // 이 속도에 도달했을 때 엔진음 피치가 최대값에 가까워집니다.
        [SerializeField, Min(0.1f)] private float engineFullSpeed = 40f;
        // 느리게 움직일 때의 엔진음 피치입니다.
        [SerializeField, Min(0.01f)] private float engineMinPitch = 0.75f;
        // 빠르게 움직일 때의 엔진음 피치입니다. 높을수록 소리가 더 빠르고 높게 들립니다.
        [SerializeField, Min(0.01f)] private float engineMaxPitch = 1.6f;
        // 엔진음 볼륨입니다.
        [SerializeField, Range(0f, 1f)] private float engineVolume = 0.6f;
        // 속도 변화에 따라 피치가 따라가는 부드러움입니다. 높을수록 더 빠르게 반응합니다.
        [SerializeField, Min(0f)] private float enginePitchSmoothing = 8f;

        [Header("Loop Clip")]
        // PlayLoopClip으로 재생 중인 루프 사운드를 StopLoopClip에서 끊을 때 원래 loop 설정으로 되돌리기 위한 값입니다.
        [SerializeField] private bool restoreLoopSettingOnStop = true;

        [Header("Random Clip Loop")]
        // 정해진 시간마다 랜덤으로 재생할 클립 목록입니다.
        [SerializeField] private AudioClip[] randomClips;

        // 컴포넌트가 켜질 때 랜덤 재생 루프를 자동으로 시작할지 정합니다.
        [SerializeField] private bool playRandomLoopOnEnable = true;

        // 켜진 직후 한 번 바로 재생할지 정합니다. 꺼두면 playInterval 뒤에 첫 소리가 납니다.
        [SerializeField] private bool playImmediatelyOnEnable;

        // 랜덤 클립을 몇 초마다 재생할지 정합니다.
        [SerializeField, Min(0.01f)] private float playInterval = 5f;

        // true면 Time.timeScale의 영향을 받지 않습니다. 일시정지 중에도 UI/효과음을 내고 싶을 때 켭니다.
        [SerializeField] private bool useUnscaledTime;

        [Header("Pitch Variation")]
        // 재생할 때마다 피치를 랜덤으로 흔들지 정합니다. 같은 클립 반복의 기계적인 느낌을 줄일 수 있습니다.
        [SerializeField] private bool useRandomPitch;

        // useRandomPitch가 켜져 있을 때 사용할 피치 범위입니다. 1은 원래 피치입니다.
        [SerializeField] private Vector2 randomPitchRange = new Vector2(0.95f, 1.05f);

        private Coroutine _randomLoopCoroutine;
        private float _defaultPitch = 1f;
        private bool _defaultLoop;
        private AudioClip _loopClip;
        private float _engineDefaultPitch = 1f;

        private void Reset()
        {
            audioSource = GetComponent<AudioSource>();
            playerController = GetComponent<PlayerController>();
            ConfigureAudioSource();
        }

        private void Awake()
        {
            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
            }

            if (audioSource != null)
            {
                _defaultPitch = audioSource.pitch;
                _defaultLoop = audioSource.loop;
            }

            if (playerController == null)
            {
                playerController = GetComponent<PlayerController>();
            }

            if (engineAudioSource == null)
            {
                engineAudioSource = gameObject.AddComponent<AudioSource>();
            }

            ConfigureEngineAudioSource();
        }

        private void OnEnable()
        {
            if (playRandomLoopOnEnable)
            {
                StartRandomLoop();
            }
        }

        private void Update()
        {
            UpdateEngineSound();
        }

        private void OnDisable()
        {
            StopRandomLoop();
            StopEngineSound();

            if (audioSource != null)
            {
                StopLoopClip();
                audioSource.pitch = _defaultPitch;
            }
        }

        /// <summary>
        /// UnityEvent에 연결해서 특정 클립을 즉시 재생할 때 사용합니다.
        /// </summary>
        public void PlayClip(AudioClip clip)
        {
            if (clip == null || audioSource == null)
            {
                return;
            }

            ApplyRandomPitch();

            if (usePlayOneShot)
            {
                audioSource.PlayOneShot(clip, volumeScale);
                return;
            }

            audioSource.clip = clip;
            audioSource.volume = volumeScale;
            audioSource.Play();
        }

        /// <summary>
        /// UnityEvent에 연결해서 드리프트처럼 유지되는 동안 반복 재생할 사운드에 사용합니다.
        /// 이미 같은 클립이 루프로 재생 중이면 다시 시작하지 않아서 소리가 겹치지 않습니다.
        /// </summary>
        public void PlayLoopClip(AudioClip clip)
        {
            if (clip == null || audioSource == null)
            {
                return;
            }

            if (_loopClip == clip && audioSource.isPlaying && audioSource.loop)
            {
                return;
            }

            ApplyRandomPitch();
            _loopClip = clip;
            audioSource.clip = clip;
            audioSource.volume = volumeScale;
            audioSource.loop = true;
            audioSource.Play();
        }

        /// <summary>
        /// PlayLoopClip으로 재생 중인 반복 사운드를 멈춥니다.
        /// UnityEvent에서 드리프트 종료 이벤트에 연결하면 짧게 드리프트했을 때 소리도 바로 끊깁니다.
        /// </summary>
        public void StopLoopClip()
        {
            if (audioSource == null || _loopClip == null)
            {
                return;
            }

            audioSource.Stop();
            audioSource.clip = null;
            _loopClip = null;

            if (restoreLoopSettingOnStop)
            {
                audioSource.loop = _defaultLoop;
            }

            audioSource.pitch = _defaultPitch;
        }

        /// <summary>
        /// randomClips 배열 안에서 하나를 뽑아 즉시 재생합니다.
        /// </summary>
        public void PlayRandomClip()
        {
            AudioClip clip = GetRandomClip();
            PlayClip(clip);
        }

        /// <summary>
        /// 정해진 시간마다 랜덤 클립을 재생하는 루프를 시작합니다.
        /// </summary>
        public void StartRandomLoop()
        {
            if (_randomLoopCoroutine != null || !isActiveAndEnabled)
            {
                return;
            }

            _randomLoopCoroutine = StartCoroutine(RandomLoopRoutine());
        }

        /// <summary>
        /// 랜덤 클립 재생 루프를 멈춥니다.
        /// </summary>
        public void StopRandomLoop()
        {
            if (_randomLoopCoroutine == null)
            {
                return;
            }

            StopCoroutine(_randomLoopCoroutine);
            _randomLoopCoroutine = null;
        }

        private IEnumerator RandomLoopRoutine()
        {
            if (playImmediatelyOnEnable)
            {
                PlayRandomClip();
            }

            while (true)
            {
                yield return WaitForSecondsBySetting(playInterval);
                PlayRandomClip();
            }
        }

        private IEnumerator WaitForSecondsBySetting(float seconds)
        {
            float timer = 0f;
            while (timer < seconds)
            {
                timer += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
                yield return null;
            }
        }

        private AudioClip GetRandomClip()
        {
            if (randomClips == null || randomClips.Length == 0)
            {
                return null;
            }

            int index = Random.Range(0, randomClips.Length);
            return randomClips[index];
        }

        private void ApplyRandomPitch()
        {
            if (audioSource == null)
            {
                return;
            }

            if (!useRandomPitch)
            {
                audioSource.pitch = _defaultPitch;
                return;
            }

            float minPitch = Mathf.Min(randomPitchRange.x, randomPitchRange.y);
            float maxPitch = Mathf.Max(randomPitchRange.x, randomPitchRange.y);
            audioSource.pitch = Random.Range(minPitch, maxPitch);
        }

        private void ConfigureAudioSource()
        {
            if (audioSource == null)
            {
                return;
            }

            audioSource.playOnAwake = false;
        }

        private void ConfigureEngineAudioSource()
        {
            if (engineAudioSource == null)
            {
                return;
            }

            engineAudioSource.playOnAwake = false;
            engineAudioSource.loop = true;
            engineAudioSource.volume = engineVolume;
            _engineDefaultPitch = engineAudioSource.pitch;
        }

        private void UpdateEngineSound()
        {
            if (!playEngineBySpeed || playerController == null || engineAudioSource == null || engineClip == null)
            {
                StopEngineSound();
                return;
            }

            float speed = playerController.Speed;
            if (speed < engineStartSpeed)
            {
                StopEngineSound();
                return;
            }

            float fullSpeed = Mathf.Max(engineFullSpeed, engineStartSpeed + 0.01f);
            float speedRatio = Mathf.InverseLerp(engineStartSpeed, fullSpeed, speed);
            float targetPitch = Mathf.Lerp(engineMinPitch, engineMaxPitch, speedRatio);
            float smoothAmount = 1f - Mathf.Exp(-enginePitchSmoothing * Time.deltaTime);

            engineAudioSource.clip = engineClip;
            engineAudioSource.loop = true;
            engineAudioSource.volume = engineVolume;
            engineAudioSource.pitch = Mathf.Lerp(engineAudioSource.pitch, targetPitch, smoothAmount);

            if (!engineAudioSource.isPlaying)
            {
                engineAudioSource.Play();
            }
        }

        private void StopEngineSound()
        {
            if (engineAudioSource == null || !engineAudioSource.isPlaying)
            {
                return;
            }

            engineAudioSource.Stop();
            engineAudioSource.pitch = _engineDefaultPitch;
        }
    }
}
