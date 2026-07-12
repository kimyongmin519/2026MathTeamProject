using UnityEngine;
using UnityEngine.Events;

namespace Member.KYM.Scripts.Npcs
{
    /// <summary>
    /// 플레이어와 닿으면 폭발 이펙트를 만들고, NPC를 래그돌로 전환해 날려 보내는 컴포넌트입니다.
    /// NPC 루트 오브젝트에 붙이고, 래그돌용 Rigidbody/Collider는 자식 본들에 세팅해두면 됩니다.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(CapsuleCollider))]
    public class ExplodingRagdollNpc : MonoBehaviour
    {
        [Header("Player Detection")]
        // 플레이어 오브젝트가 들어있는 레이어만 켜두면 플레이어 구현체를 직접 참조하지 않고 충돌 대상을 판단할 수 있습니다.
        [SerializeField] private LayerMask playerLayers = ~0;

        [Header("Explosion Effect")]
        // 폭발 순간 생성할 파티클 프리팹입니다. 씬 오브젝트가 아니라 Project의 프리팹을 넣는 걸 추천합니다.
        [SerializeField] private ParticleSystem explosionParticlePrefab;

        // 폭발 이펙트가 생성될 위치입니다. 비워두면 충돌 지점에서 생성됩니다.
        [SerializeField] private Transform explosionPoint;

        // 생성된 폭발 파티클 오브젝트를 몇 초 뒤 제거할지 정합니다.
        [SerializeField, Min(0.1f)] private float particleDestroyDelay = 4f;

        [Header("Ragdoll")]
        // NPC 루트나 자식에 있는 Animator입니다. 폭발 시 꺼져서 애니메이션 대신 물리가 몸을 움직입니다.
        [SerializeField] private Animator animator;

        // NPC 루트의 Rigidbody입니다. 비워두면 자동으로 찾습니다. 래그돌 전환 전에는 키네마틱으로 유지됩니다.
        [SerializeField] private Rigidbody rootRigidbody;

        // NPC 루트의 Collider입니다. 비워두면 자동으로 찾습니다. 래그돌 전환 후에는 꺼집니다.
        [SerializeField] private Collider rootCollider;

        // 래그돌 전환 순간 같이 꺼야 하는 이동/AI/애니메이션 보조 스크립트가 있으면 넣어주세요.
        [SerializeField] private Behaviour[] disableOnRagdoll;

        [Header("Launch Force")]
        // NPC가 플레이어 반대 방향으로 날아가는 힘입니다. 너무 약하면 쓰러지기만 하고, 너무 강하면 과하게 튑니다.
        [SerializeField, Min(0f)] private float explosionForce = 12f;

        // 위쪽으로 추가되는 힘입니다. 값이 높을수록 맞고 붕 뜨는 느낌이 강해집니다.
        [SerializeField, Min(0f)] private float upwardForce = 4f;

        // 폭발 힘이 퍼지는 반경입니다. 몸의 각 부위가 폭발 지점에서 얼마나 다르게 밀릴지에 영향을 줍니다.
        [SerializeField, Min(0.1f)] private float explosionRadius = 3f;

        // 몸이 날아갈 때 랜덤 회전이 붙는 정도입니다. 값이 높을수록 더 과장되게 빙글빙글 돕니다.
        [SerializeField, Min(0f)] private float torqueForce = 6f;

        // 플레이어가 빠르게 달려와 부딪혔을 때, 플레이어 속도를 NPC 날아가는 힘에 일부 반영할지 정합니다.
        [SerializeField] private bool inheritPlayerVelocity = true;

        // 플레이어 속도를 얼마나 많이 이어받을지 정합니다.
        [SerializeField, Min(0f)] private float playerVelocityMultiplier = 0.35f;

        // 플레이어 속도 반영값의 상한입니다. 부스터 상태에서 NPC가 너무 멀리 날아가는 걸 막습니다.
        [SerializeField, Min(0f)] private float maxInheritedSpeed = 18f;

        [Header("Cleanup")]
        // 폭발 후 NPC 오브젝트를 자동 삭제할지 정합니다. 시체를 남기고 싶으면 꺼두세요.
        [SerializeField] private bool destroyAfterExplosion;

        // destroyAfterExplosion이 켜져 있을 때, 폭발 후 몇 초 뒤 NPC 오브젝트를 삭제할지 정합니다.
        [SerializeField, Min(0f)] private float destroyDelay = 6f;

        [Header("Events")]
        // 폭발이 일어나는 순간 호출됩니다. 사운드 재생, 점수 증가 같은 추가 연출을 인스펙터에서 연결할 수 있습니다.
        [SerializeField] private UnityEvent onExploded;

        private Rigidbody[] _ragdollRigidbodies;
        private Collider[] _ragdollColliders;
        private bool _hasExploded;

        private void Reset()
        {
            animator = GetComponentInChildren<Animator>();
            rootRigidbody = GetComponent<Rigidbody>();
            rootCollider = GetComponent<Collider>();

            ConfigureRootRigidbody();
        }

        private void Awake()
        {
            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>();
            }

            if (rootRigidbody == null)
            {
                rootRigidbody = GetComponent<Rigidbody>();
            }

            if (rootCollider == null)
            {
                rootCollider = GetComponent<Collider>();
            }

            ConfigureRootRigidbody();
            CollectRagdollParts();
            SetRagdollEnabled(false);
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (_hasExploded || collision.collider == null)
            {
                return;
            }

            Vector3 hitPoint = collision.contactCount > 0
                ? collision.GetContact(0).point
                : collision.collider.ClosestPoint(transform.position);

            TryExplode(collision.collider, hitPoint);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_hasExploded || other == null)
            {
                return;
            }

            TryExplode(other, other.ClosestPoint(transform.position));
        }

        private void TryExplode(Collider other, Vector3 hitPoint)
        {
            if (!IsPlayerCollider(other, out Rigidbody playerRigidbody))
            {
                return;
            }

            Explode(playerRigidbody, hitPoint);
        }

        private bool IsPlayerCollider(Collider other, out Rigidbody playerRigidbody)
        {
            playerRigidbody = other.attachedRigidbody != null
                ? other.attachedRigidbody
                : other.GetComponentInParent<Rigidbody>();

            if ((playerLayers.value & (1 << other.gameObject.layer)) == 0)
            {
                return false;
            }

            return true;
        }

        private void Explode(Rigidbody playerRigidbody, Vector3 hitPoint)
        {
            _hasExploded = true;

            SpawnExplosionEffect(hitPoint);

            if (animator != null)
            {
                animator.enabled = false;
            }

            foreach (Behaviour behaviour in disableOnRagdoll)
            {
                if (behaviour != null && behaviour != this)
                {
                    behaviour.enabled = false;
                }
            }

            SetRagdollEnabled(true);
            ApplyLaunchForce(playerRigidbody, hitPoint);

            onExploded?.Invoke();

            if (destroyAfterExplosion)
            {
                Destroy(gameObject, destroyDelay);
            }
        }

        private void SpawnExplosionEffect(Vector3 hitPoint)
        {
            if (explosionParticlePrefab == null)
            {
                return;
            }

            Vector3 spawnPosition = explosionPoint != null ? explosionPoint.position : hitPoint;
            Quaternion spawnRotation = explosionPoint != null ? explosionPoint.rotation : Quaternion.identity;
            ParticleSystem particle = Instantiate(explosionParticlePrefab, spawnPosition, spawnRotation);

            particle.Play(true);
            Destroy(particle.gameObject, particleDestroyDelay);
        }

        private void CollectRagdollParts()
        {
            _ragdollRigidbodies = GetComponentsInChildren<Rigidbody>(true);
            _ragdollColliders = GetComponentsInChildren<Collider>(true);
        }

        private void SetRagdollEnabled(bool enabled)
        {
            if (rootCollider != null)
            {
                rootCollider.enabled = !enabled;
            }

            if (rootRigidbody != null)
            {
                rootRigidbody.isKinematic = true;
                rootRigidbody.detectCollisions = !enabled;
            }

            foreach (Collider ragdollCollider in _ragdollColliders)
            {
                if (ragdollCollider == null || ragdollCollider == rootCollider)
                {
                    continue;
                }

                ragdollCollider.enabled = enabled;
                ragdollCollider.isTrigger = false;
            }

            foreach (Rigidbody ragdollRigidbody in _ragdollRigidbodies)
            {
                if (ragdollRigidbody == null || ragdollRigidbody == rootRigidbody)
                {
                    continue;
                }

                ragdollRigidbody.isKinematic = !enabled;
                ragdollRigidbody.detectCollisions = enabled;
                ragdollRigidbody.interpolation = RigidbodyInterpolation.Interpolate;
                ragdollRigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            }
        }

        private void ConfigureRootRigidbody()
        {
            if (rootRigidbody == null)
            {
                return;
            }

            rootRigidbody.isKinematic = true;
            rootRigidbody.useGravity = false;
            rootRigidbody.interpolation = RigidbodyInterpolation.Interpolate;
        }

        private void ApplyLaunchForce(Rigidbody playerRigidbody, Vector3 hitPoint)
        {
            Vector3 explosionOrigin = playerRigidbody != null ? playerRigidbody.position : hitPoint;
            Vector3 inheritedVelocity = GetInheritedPlayerVelocity(playerRigidbody);

            foreach (Rigidbody ragdollRigidbody in _ragdollRigidbodies)
            {
                if (ragdollRigidbody == null || ragdollRigidbody == rootRigidbody)
                {
                    continue;
                }

                ragdollRigidbody.AddExplosionForce(
                    explosionForce,
                    explosionOrigin,
                    explosionRadius,
                    upwardForce,
                    ForceMode.VelocityChange);

                if (inheritedVelocity.sqrMagnitude > 0.001f)
                {
                    ragdollRigidbody.AddForce(inheritedVelocity, ForceMode.VelocityChange);
                }

                if (torqueForce > 0f)
                {
                    ragdollRigidbody.AddTorque(Random.insideUnitSphere * torqueForce, ForceMode.VelocityChange);
                }
            }
        }

        private Vector3 GetInheritedPlayerVelocity(Rigidbody playerRigidbody)
        {
            if (!inheritPlayerVelocity || playerRigidbody == null)
            {
                return Vector3.zero;
            }

            return Vector3.ClampMagnitude(
                playerRigidbody.linearVelocity * playerVelocityMultiplier,
                maxInheritedSpeed);
        }
    }
}
