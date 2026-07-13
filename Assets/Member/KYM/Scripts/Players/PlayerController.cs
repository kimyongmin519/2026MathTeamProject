using Member.KYM.Scripts.CoreSystems;
using UnityEngine;
using UnityEngine.Events;

namespace Member.KYM.Scripts.Players
{
    [RequireComponent(typeof(Rigidbody))]
    public class PlayerController : MonoBehaviour
    {
        // 플레이어의 이동 방향과 드리프트 버튼 상태를 전달하는 입력 데이터
        [field: SerializeField] public PlayerInputSO PlayerInput { get; private set; }

        [Header("Speed")]
        // 전진할 때 차량을 밀어주는 가속력. 높을수록 최고속도에 빨리 도달한다.
        [SerializeField, Min(0f)] private float acceleration = 32f;
        // 후진할 때 차량을 밀어주는 가속력
        [SerializeField, Min(0f)] private float reverseAcceleration = 18f;
        // 현재 진행 방향과 반대 방향을 입력했을 때 적용되는 제동력
        [SerializeField, Min(0f)] private float brakePower = 45f;
        // 차량이 낼 수 있는 최대 전진 속도(m/s)
        [SerializeField, Min(0.1f)] private float maxForwardSpeed = 24f;
        // 차량이 낼 수 있는 최대 후진 속도(m/s)
        [SerializeField, Min(0.1f)] private float maxReverseSpeed = 9f;
        // 가속 입력을 놓았을 때 자연스럽게 차량을 감속시키는 저항
        [SerializeField, Min(0f)] private float coastingDrag = 2.5f;

        [Header("Steering")]
        // 저속에서 1초 동안 회전할 수 있는 최대 각도
        [SerializeField, Min(0f)] private float steeringDegreesPerSecond = 105f;
        // 최고속도에서 유지할 조향력 비율. 0.42면 기본 조향력의 42%를 사용한다.
        [SerializeField, Range(0.1f, 1f)] private float minimumSteeringAtTopSpeed = 0.42f;
        // 제자리 회전을 막기 위해 조향을 허용하기 시작하는 최소 속도
        [SerializeField, Min(0f)] private float minimumSpeedToSteer = 0.4f;
        // 드리프트를 시작할 수 있는 속도에서 적용되는 최소 회전 배율
        [SerializeField, Min(1f)] private float driftSteeringMultiplier = 1.8f;
        // 최고 속도에 가까운 드리프트에서 적용되는 최대 회전 배율
        [SerializeField, Min(1f)] private float driftSteeringAtTopSpeedMultiplier = 3.2f;
        // 속도에 따른 드리프트 회전력 증가 곡선. 1은 선형, 높을수록 고속 구간에서 빠르게 증가한다.
        [SerializeField, Min(0.1f)] private float driftSteeringSpeedResponse = 1.2f;

        [Header("Grip & Drift")]
        // 일반 주행 중 옆으로 미끄러지는 속도를 줄이는 접지력
        [SerializeField, Min(0f)] private float normalGrip = 10f;
        // 드리프트 중 적용되는 접지력. 낮을수록 옆으로 더 오래 미끄러진다.
        [SerializeField, Min(0f)] private float driftGrip = 2.2f;
        // 드리프트를 시작할 수 있는 최소 전진 또는 후진 속도
        [SerializeField, Min(0f)] private float minimumDriftSpeed = 5f;
        // 드리프트를 시작하기 위해 필요한 최소 좌우 입력 크기
        [SerializeField, Range(0f, 1f)] private float minimumDriftSteeringInput = 0.15f;
        // 드리프트 중 차량을 조향 방향 바깥쪽으로 밀어 미끄러짐을 만드는 힘
        [SerializeField, Min(0f)] private float driftOutwardPush = 3f;

        [Header("Ground")]
        // 접지 판정에서 지면으로 인식할 레이어
        [SerializeField] private LayerMask groundLayers = ~0;
        // 차량 아래 방향으로 지면을 검사하는 Ray의 길이
        [SerializeField, Min(0.1f)] private float groundCheckDistance = 1.2f;
        // 차량이 지면에서 뜨지 않도록 아래 방향으로 계속 누르는 힘
        [SerializeField, Min(0f)] private float downforce = 18f;
        // Rigidbody의 무게중심 위치. Y를 낮추면 차량이 덜 뒤집힌다.
        [SerializeField] private Vector3 centerOfMass = new Vector3(0f, -0.45f, 0f);

        [Header("Wheelchair Visual Feedback")]
        // 흔들림을 적용할 휠체어 외형. 비어 있으면 자식 오브젝트 "Visual"을 자동으로 찾는다.
        [SerializeField] private Transform visualRoot;
        // 가속과 제동에 따라 휠체어가 앞뒤로 기울어지는 최대 각도
        [SerializeField, Min(0f)] private float maxPitchAngle = 7f;
        // 좌우 조향과 횡이동에 따라 휠체어가 좌우로 기울어지는 최대 각도
        [SerializeField, Min(0f)] private float maxRollAngle = 9f;
        // 전후 가속도가 앞뒤 기울기에 반영되는 강도
        [SerializeField, Min(0f)] private float accelerationTilt = 0.32f;
        // 조향 입력이 좌우 기울기에 반영되는 강도
        [SerializeField, Min(0f)] private float steeringTilt = 6f;
        // 주행 중 발생하는 규칙적인 상하 움직임의 크기
        [SerializeField, Min(0f)] private float rollingBobAmount = 0.025f;
        // 속도에 따라 상하 움직임이 반복되는 빠르기
        [SerializeField, Min(0f)] private float rollingBobFrequency = 8f;
        // 노면의 잔진동처럼 보이는 불규칙한 위치 흔들림의 크기
        [SerializeField, Min(0f)] private float roadShakeAmount = 0.012f;
        // 외형이 목표 기울기와 위치를 따라가는 부드러움
        [SerializeField, Min(0f)] private float visualSmoothing = 10f;

        [Header("Boost")]
        // 일반 주행 중 1초마다 충전되는 부스터 게이지 양
        [SerializeField, Min(0f)] private float boostChargePerSecond = 0.12f;
        // 드리프트 중 일반 충전량에 추가되는 초당 게이지 양
        [SerializeField, Min(0f)] private float driftChargeBonusPerSecond = 0.18f;
        // 게이지가 충전되기 시작하는 최소 주행 속도
        [SerializeField, Min(0f)] private float minimumBoostChargeSpeed = 2f;
        // 완충 후 부스터를 발동시키기 위해 눌러야 하는 Shift 횟수
        [SerializeField, Min(1)] private int requiredBoostMashCount = 5;
        // Shift 연타를 완료해야 하는 제한 시간
        [SerializeField, Min(0.1f)] private float boostMashTimeWindow = 1.2f;
        // 부스터가 유지되는 시간
        [SerializeField, Min(0.1f)] private float boostDuration = 2.5f;
        // 부스터 중 차량을 앞으로 밀어주는 추가 가속력
        [SerializeField, Min(0f)] private float boostAcceleration = 55f;
        // 부스터 중 적용되는 최대 전진 속도
        [SerializeField, Min(0.1f)] private float boostMaxSpeed = 38f;

        [Header("Boost Collision")]
        // 부스트 충돌 판정에 사용할 장애물 레이어. 바닥 충돌은 접촉면 각도로 별도 제외합니다.
        [SerializeField] private LayerMask boostCrashObstacleLayers = ~0;
        // 장애물에 부딪혔을 때 충돌 지점 반대 방향으로 튕겨 나가는 속도입니다.
        [SerializeField, Min(0f)] private float boostCrashBounceSpeed = 5f;
        // 충돌 시 살짝 들리는 느낌을 주는 위쪽 속도입니다.
        [SerializeField, Min(0f)] private float boostCrashUpwardSpeed = 0.8f;
        // 충돌 직후 드리프트 조향이 다시 적용되어 빙글 도는 것을 막는 짧은 조작 잠금 시간입니다.
        [SerializeField, Min(0f)] private float boostCrashSteeringLockDuration = 0.2f;
        // 이 값보다 위를 향한 접촉면은 바닥이나 경사로로 보고 부스트 충돌에서 제외합니다.
        [SerializeField, Range(0f, 1f)] private float boostCrashGroundNormalThreshold = 0.55f;

        [Header("Particle Effects")]
        // 부스터가 유지되는 동안 재생할 파티클. 자식 파티클도 함께 재생한다.
        [SerializeField] private ParticleSystem boostParticle;
        // 드리프트가 유지되는 동안 재생할 파티클. 자식 파티클도 함께 재생한다.
        [SerializeField] private ParticleSystem driftParticle;

        [Header("Camera Speed Effect")]
        // 일정 속도 이상에서 카메라에 속도감을 표현할 파티클
        [SerializeField] private ParticleSystem speedParticle;
        // 속도 파티클이 나타나기 시작하는 속도
        [SerializeField, Min(0f)] private float speedEffectStartSpeed = 40f;
        // 속도 파티클이 최대 강도에 도달하는 속도
        [SerializeField, Min(0.1f)] private float speedEffectFullSpeed = 70f;
        // 최대 속도에서 기존 파티클 방출량에 곱할 배율
        [SerializeField, Min(0f)] private float speedEffectEmissionMultiplier = 2f;
        // 속도에 따라 파티클 자체의 움직임이 빨라지는 최대 배율
        [SerializeField, Min(0.1f)] private float speedEffectSimulationMultiplier = 1.6f;

        [Header("Animation")]
        // 부스터 상태를 IsBoost 파라미터로 전달할 플레이어 Animator
        [SerializeField] private Animator playerAnimator;

        [Header("Sound Events")]
        // 부스트가 시작되는 순간 한 번 호출됩니다. PlayerSoundPlayer.PlayClip(AudioClip)을 연결해 부스트 사운드를 재생할 수 있습니다.
        [SerializeField] private UnityEvent onBoostStarted;
        // 드리프트 상태로 진입하는 순간 한 번 호출됩니다. PlayerSoundPlayer.PlayClip(AudioClip)을 연결해 드리프트 사운드를 재생할 수 있습니다.
        [SerializeField] private UnityEvent onDriftStarted;
        // 드리프트 상태가 끝나는 순간 한 번 호출됩니다. PlayerSoundPlayer.StopLoopClip()을 연결해 드리프트 루프 사운드를 멈출 수 있습니다.
        [SerializeField] private UnityEvent onDriftEnded;

        public float ForwardSpeed { get; private set; }
        public float Speed => _rigidbody == null ? 0f : _rigidbody.linearVelocity.magnitude;
        public bool IsGrounded { get; private set; }
        public bool IsDrifting { get; private set; }
        public float BoostGauge01 { get; private set; }
        public bool IsBoostReady => BoostGauge01 >= 1f;
        public bool IsBoosting => _boostTimeRemaining > 0f;
        public float BoostTimeRemaining => _boostTimeRemaining;
        public float BoostTime01 => boostDuration <= 0f ? 0f : _boostTimeRemaining / boostDuration;
        public float BoostMashProgress => requiredBoostMashCount <= 0
            ? 0f
            : (float)_boostMashCount / requiredBoostMashCount;
        public float SpeedEffectAmount { get; private set; }

        private Rigidbody _rigidbody;
        private Vector3 _visualStartLocalPosition;
        private Quaternion _visualStartLocalRotation;
        private float _previousForwardSpeed;
        private float _forwardAcceleration;
        private float _sidewaysSpeed;
        private float _steeringInput;
        private float _rollingTime;
        private uint _lastBoostPressVersion;
        private int _boostMashCount;
        private float _boostMashTimeRemaining;
        private float _boostTimeRemaining;
        private float _boostCrashSteeringLockRemaining;
        private bool _wasBoostParticlePlaying;
        private bool _wasDriftParticlePlaying;
        private bool _wasBoostAnimationActive;
        private float _speedParticleBaseEmission = 1f;

        private static readonly int IsBoostHash = Animator.StringToHash("IsBoost");

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();
            _rigidbody.centerOfMass = centerOfMass;
            _rigidbody.interpolation = RigidbodyInterpolation.Interpolate;

            if (visualRoot == null)
            {
                visualRoot = transform.Find("Visual");
            }

            if (visualRoot != null)
            {
                _visualStartLocalPosition = visualRoot.localPosition;
                _visualStartLocalRotation = visualRoot.localRotation;
            }

            if (PlayerInput != null)
            {
                _lastBoostPressVersion = PlayerInput.BoostPressVersion;
            }

            if (playerAnimator == null)
            {
                playerAnimator = GetComponentInChildren<Animator>(true);
            }

            if (playerAnimator != null)
            {
                playerAnimator.SetBool(IsBoostHash, false);
            }

            StopParticleImmediately(boostParticle);
            StopParticleImmediately(driftParticle);
            StopParticleImmediately(speedParticle);

            if (speedParticle != null)
            {
                _speedParticleBaseEmission = speedParticle.emission.rateOverTimeMultiplier;
            }
        }

        private void FixedUpdate()
        {
            if (PlayerInput == null)
            {
                return;
            }

            Vector2 input = Vector2.ClampMagnitude(PlayerInput.InputDirection, 1f);
            _boostCrashSteeringLockRemaining = Mathf.Max(
                0f,
                _boostCrashSteeringLockRemaining - Time.fixedDeltaTime);
            UpdateGroundState();
            UpdateLocalSpeed();
            UpdateVisualValues(input.x);

            bool wasDrifting = IsDrifting;
            IsDrifting = IsGrounded
                && _boostCrashSteeringLockRemaining <= 0f
                && PlayerInput.IsDrifting
                && Mathf.Abs(ForwardSpeed) >= minimumDriftSpeed
                && Mathf.Abs(input.x) >= minimumDriftSteeringInput;

            if (IsDrifting && !wasDrifting)
            {
                onDriftStarted?.Invoke();
            }
            else if (!IsDrifting && wasDrifting)
            {
                onDriftEnded?.Invoke();
            }

            UpdateBoostSystem();
            UpdateParticleEffects();
            UpdateBoostAnimation();

            if (!IsGrounded)
            {
                return;
            }

            ApplyAcceleration(input.y);
            ApplySteering(input.x);
            ApplyLateralGrip();
            ApplyDownforce();
            LimitSpeed();
        }

        private void OnDisable()
        {
            if (IsDrifting)
            {
                onDriftEnded?.Invoke();
                IsDrifting = false;
            }

            StopParticleImmediately(boostParticle);
            StopParticleImmediately(driftParticle);
            StopParticleImmediately(speedParticle);
            _wasBoostParticlePlaying = false;
            _wasDriftParticlePlaying = false;
            _wasBoostAnimationActive = false;

            if (playerAnimator != null)
            {
                playerAnimator.SetBool(IsBoostHash, false);
            }
        }

        private void LateUpdate()
        {
            ApplyWheelchairVisualFeedback();
            UpdateCameraSpeedEffect();
        }

        private void UpdateGroundState()
        {
            Vector3 origin = transform.position + transform.up * 0.1f;
            IsGrounded = Physics.Raycast(
                origin,
                -transform.up,
                groundCheckDistance,
                groundLayers,
                QueryTriggerInteraction.Ignore);
        }

        private void UpdateLocalSpeed()
        {
            ForwardSpeed = Vector3.Dot(_rigidbody.linearVelocity, transform.forward);
        }

        private void UpdateVisualValues(float steering)
        {
            _forwardAcceleration = (ForwardSpeed - _previousForwardSpeed) / Time.fixedDeltaTime;
            _previousForwardSpeed = ForwardSpeed;
            _sidewaysSpeed = Vector3.Dot(_rigidbody.linearVelocity, transform.right);
            _steeringInput = steering;
        }

        private void ApplyWheelchairVisualFeedback()
        {
            if (visualRoot == null)
            {
                return;
            }

            float speedRatio = Mathf.Clamp01(Speed / maxForwardSpeed);
            float groundedAmount = IsGrounded ? 1f : 0f;

            float pitch = Mathf.Clamp(
                -_forwardAcceleration * accelerationTilt,
                -maxPitchAngle,
                maxPitchAngle);

            float roll = Mathf.Clamp(
                -(_steeringInput * steeringTilt + _sidewaysSpeed),
                -maxRollAngle,
                maxRollAngle);

            _rollingTime += Time.deltaTime * rollingBobFrequency * Mathf.Lerp(0.35f, 1.5f, speedRatio);
            float bob = Mathf.Sin(_rollingTime) * rollingBobAmount * speedRatio * groundedAmount;

            float shakeX = (Mathf.PerlinNoise(Time.time * 13.7f, 0.2f) - 0.5f) * 2f;
            float shakeY = (Mathf.PerlinNoise(0.4f, Time.time * 17.3f) - 0.5f) * 2f;
            Vector3 shakeOffset = new Vector3(shakeX, shakeY, 0f)
                                  * roadShakeAmount * speedRatio * groundedAmount;

            Quaternion targetRotation = _visualStartLocalRotation * Quaternion.Euler(pitch, 0f, roll);
            Vector3 targetPosition = _visualStartLocalPosition + Vector3.up * bob + shakeOffset;
            float smoothAmount = 1f - Mathf.Exp(-visualSmoothing * Time.deltaTime);

            visualRoot.localRotation = Quaternion.Slerp(visualRoot.localRotation, targetRotation, smoothAmount);
            visualRoot.localPosition = Vector3.Lerp(visualRoot.localPosition, targetPosition, smoothAmount);
        }

        private void ApplyAcceleration(float throttle)
        {
            if (Mathf.Abs(throttle) < 0.01f)
            {
                Vector3 horizontalVelocity = Vector3.ProjectOnPlane(_rigidbody.linearVelocity, transform.up);
                _rigidbody.AddForce(-horizontalVelocity * coastingDrag, ForceMode.Acceleration);
                return;
            }

            bool isTryingToChangeDirection = Mathf.Abs(ForwardSpeed) > 0.5f
                && Mathf.Sign(throttle) != Mathf.Sign(ForwardSpeed);

            if (isTryingToChangeDirection)
            {
                _rigidbody.AddForce(-transform.forward * Mathf.Sign(ForwardSpeed) * brakePower,
                    ForceMode.Acceleration);
                return;
            }

            float currentAcceleration = throttle > 0f ? acceleration : reverseAcceleration;
            float speedLimit = throttle > 0f ? maxForwardSpeed : maxReverseSpeed;
            float speedRatio = Mathf.Clamp01(Mathf.Abs(ForwardSpeed) / speedLimit);
            float accelerationBySpeed = Mathf.Lerp(1f, 0.15f, speedRatio);

            _rigidbody.AddForce(transform.forward * (throttle * currentAcceleration * accelerationBySpeed),
                ForceMode.Acceleration);
        }

        private void UpdateBoostSystem()
        {
            if (IsBoosting)
            {
                _boostTimeRemaining = Mathf.Max(0f, _boostTimeRemaining - Time.fixedDeltaTime);
                if (IsGrounded)
                {
                    _rigidbody.AddForce(transform.forward * boostAcceleration, ForceMode.Acceleration);
                }
                return;
            }

            if (!IsGrounded)
            {
                _boostMashCount = 0;
                _boostMashTimeRemaining = 0f;
                _lastBoostPressVersion = PlayerInput.BoostPressVersion;
                return;
            }

            ProcessBoostMashInput();

            if (Mathf.Abs(ForwardSpeed) < minimumBoostChargeSpeed || IsBoostReady)
            {
                return;
            }

            float chargeRate = boostChargePerSecond;
            if (IsDrifting)
            {
                chargeRate += driftChargeBonusPerSecond;
            }

            BoostGauge01 = Mathf.Clamp01(BoostGauge01 + chargeRate * Time.fixedDeltaTime);
        }

        private void ProcessBoostMashInput()
        {
            if (!IsBoostReady || IsBoosting)
            {
                _boostMashCount = 0;
                _boostMashTimeRemaining = 0f;
                _lastBoostPressVersion = PlayerInput.BoostPressVersion;
                return;
            }

            if (_boostMashCount > 0)
            {
                _boostMashTimeRemaining -= Time.fixedDeltaTime;
                if (_boostMashTimeRemaining <= 0f)
                {
                    _boostMashCount = 0;
                }
            }

            if (_lastBoostPressVersion == PlayerInput.BoostPressVersion)
            {
                return;
            }

            _lastBoostPressVersion = PlayerInput.BoostPressVersion;
            _boostMashCount++;
            _boostMashTimeRemaining = boostMashTimeWindow;

            if (_boostMashCount >= requiredBoostMashCount)
            {
                StartBoost();
            }
        }

        private void StartBoost()
        {
            BoostGauge01 = 0f;
            _boostMashCount = 0;
            _boostMashTimeRemaining = 0f;
            _boostTimeRemaining = boostDuration;
            onBoostStarted?.Invoke();
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (!IsBoosting || collision.contactCount == 0)
            {
                return;
            }

            int collisionLayerMask = 1 << collision.gameObject.layer;
            if ((boostCrashObstacleLayers.value & collisionLayerMask) == 0)
            {
                return;
            }

            ContactPoint contact = collision.GetContact(0);
            float upwardFacingAmount = Mathf.Abs(Vector3.Dot(contact.normal, transform.up));
            if (upwardFacingAmount > boostCrashGroundNormalThreshold)
            {
                return;
            }

            StopBoostFromCrash(contact.point);
        }

        private void StopBoostFromCrash(Vector3 hitPoint)
        {
            _boostTimeRemaining = 0f;
            _boostMashCount = 0;
            _boostMashTimeRemaining = 0f;
            _boostCrashSteeringLockRemaining = boostCrashSteeringLockDuration;

            // 드리프트와 충돌로 남은 회전 관성을 제거해 충돌 직후 빙글 도는 현상을 막습니다.
            _rigidbody.angularVelocity = Vector3.zero;

            Vector3 bounceDirection = Vector3.ProjectOnPlane(
                _rigidbody.worldCenterOfMass - hitPoint,
                transform.up).normalized;

            if (bounceDirection.sqrMagnitude < 0.001f)
            {
                bounceDirection = -transform.forward;
            }

            float verticalSpeed = Vector3.Dot(_rigidbody.linearVelocity, transform.up);
            _rigidbody.linearVelocity = bounceDirection * boostCrashBounceSpeed
                                        + transform.up * Mathf.Max(verticalSpeed, boostCrashUpwardSpeed);

            UpdateParticleEffects();
            UpdateBoostAnimation();
        }

        private void UpdateParticleEffects()
        {
            SetParticleState(boostParticle, IsBoosting, ref _wasBoostParticlePlaying);
            SetParticleState(driftParticle, IsDrifting, ref _wasDriftParticlePlaying);
        }

        private void SetParticleState(ParticleSystem particle, bool shouldPlay, ref bool wasPlaying)
        {
            if (particle == null || shouldPlay == wasPlaying)
            {
                return;
            }

            wasPlaying = shouldPlay;
            if (shouldPlay)
            {
                particle.Play(true);
            }
            else
            {
                particle.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }
        }

        private void StopParticleImmediately(ParticleSystem particle)
        {
            if (particle != null)
            {
                particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
        }

        private void UpdateCameraSpeedEffect()
        {
            if (speedParticle == null)
            {
                SpeedEffectAmount = 0f;
                return;
            }

            float fullSpeed = Mathf.Max(speedEffectFullSpeed, speedEffectStartSpeed + 0.01f);
            SpeedEffectAmount = Mathf.InverseLerp(speedEffectStartSpeed, fullSpeed, Speed);

            if (SpeedEffectAmount <= 0.001f)
            {
                if (speedParticle.isPlaying)
                {
                    speedParticle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                }

                return;
            }

            ParticleSystem.EmissionModule emission = speedParticle.emission;
            emission.rateOverTimeMultiplier = _speedParticleBaseEmission
                                              * Mathf.Lerp(0.25f, speedEffectEmissionMultiplier, SpeedEffectAmount);

            ParticleSystem.MainModule main = speedParticle.main;
            main.simulationSpeed = Mathf.Lerp(0.8f, speedEffectSimulationMultiplier, SpeedEffectAmount);

            if (!speedParticle.isPlaying)
            {
                speedParticle.Play(true);
            }
        }

        private void UpdateBoostAnimation()
        {
            bool shouldPlayBoostAnimation = IsBoosting;
            if (playerAnimator == null || shouldPlayBoostAnimation == _wasBoostAnimationActive)
            {
                return;
            }

            _wasBoostAnimationActive = shouldPlayBoostAnimation;
            playerAnimator.SetBool(IsBoostHash, shouldPlayBoostAnimation);
        }

        private void ApplySteering(float steering)
        {
            if (_boostCrashSteeringLockRemaining > 0f)
            {
                return;
            }

            float absoluteSpeed = Mathf.Abs(ForwardSpeed);
            if (absoluteSpeed < minimumSpeedToSteer || Mathf.Abs(steering) < 0.01f)
            {
                return;
            }

            float steeringStrength;
            if (IsDrifting)
            {
                float driftSpeedRatio = Mathf.InverseLerp(
                    minimumDriftSpeed,
                    Mathf.Max(minimumDriftSpeed + 0.01f, boostMaxSpeed),
                    Speed);
                float shapedDriftSpeedRatio = Mathf.Pow(driftSpeedRatio, driftSteeringSpeedResponse);
                steeringStrength = Mathf.Lerp(
                    driftSteeringMultiplier,
                    driftSteeringAtTopSpeedMultiplier,
                    shapedDriftSpeedRatio);
            }
            else
            {
                float speedRatio = Mathf.Clamp01(absoluteSpeed / maxForwardSpeed);
                steeringStrength = Mathf.Lerp(1f, minimumSteeringAtTopSpeed, speedRatio);
            }

            float reverseDirection = ForwardSpeed >= 0f ? 1f : -1f;
            float yaw = steering * steeringDegreesPerSecond * steeringStrength
                        * reverseDirection * Time.fixedDeltaTime;

            _rigidbody.MoveRotation(_rigidbody.rotation * Quaternion.Euler(0f, yaw, 0f));

            if (IsDrifting)
            {
                _rigidbody.AddForce(transform.right * (steering * driftOutwardPush), ForceMode.Acceleration);
            }
        }

        private void ApplyLateralGrip()
        {
            Vector3 verticalVelocity = Vector3.Project(_rigidbody.linearVelocity, transform.up);
            Vector3 planarVelocity = _rigidbody.linearVelocity - verticalVelocity;

            if (IsDrifting)
            {
                // 드리프트 중에는 횡속도를 일부만 제거해 옆으로 미끄러지게 한다.
                float sidewaysSpeed = Vector3.Dot(planarVelocity, transform.right);
                _rigidbody.AddForce(-transform.right * (sidewaysSpeed * driftGrip), ForceMode.Acceleration);
                return;
            }

            if (planarVelocity.sqrMagnitude < 0.001f)
            {
                return;
            }

            // 속력을 없애지 않고 속도 방향만 차체의 전진/후진 방향으로 정렬한다.
            float travelDirection = ForwardSpeed >= 0f ? 1f : -1f;
            Vector3 alignedVelocity = transform.forward * (planarVelocity.magnitude * travelDirection);
            float gripAmount = 1f - Mathf.Exp(-normalGrip * Time.fixedDeltaTime);
            Vector3 grippedVelocity = Vector3.Lerp(planarVelocity, alignedVelocity, gripAmount);

            _rigidbody.linearVelocity = grippedVelocity + verticalVelocity;
        }

        private void ApplyDownforce()
        {
            _rigidbody.AddForce(-transform.up * (downforce + (Speed / 2)), ForceMode.Acceleration);
        }

        private void LimitSpeed()
        {
            Vector3 localVelocity = transform.InverseTransformDirection(_rigidbody.linearVelocity);
            float currentMaxForwardSpeed = IsBoosting ? boostMaxSpeed : maxForwardSpeed;
            localVelocity.z = Mathf.Clamp(localVelocity.z, -maxReverseSpeed, currentMaxForwardSpeed);
            _rigidbody.linearVelocity = transform.TransformDirection(localVelocity);
            ForwardSpeed = localVelocity.z;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = IsGrounded ? Color.green : Color.red;
            Vector3 origin = transform.position + transform.up * 0.1f;
            Gizmos.DrawLine(origin, origin - transform.up * groundCheckDistance);
        }
    }
}
