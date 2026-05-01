using UnityEngine;
using UnityEngine.AI;
using NeoFPS;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Animator))]
public class EnemyAI : MonoBehaviour
{
    enum State { Idle, Chase, Attack, Dead }

    [Header("References")]
    [SerializeField] Transform muzzle;

    [Header("Body Aim (Optional)")]
    [Tooltip("상체를 플레이어 쪽으로 기울일 본 (mixamorig_Spine1 권장)")]
    [SerializeField] Transform aimBone;
    [Tooltip("상체 조준 세기 (0 = 꺼짐, 1 = 완전히 플레이어 쪽)")]
    [Range(0f, 1f)][SerializeField] float aimBoneWeight = 0.5f;
    [Tooltip("본의 로컬 'forward'가 실제로 가리키는 방향 보정 (Euler 각도)")]
    [SerializeField] Vector3 aimBoneLocalForwardFix = new Vector3(0f, 0f, 0f);

    [Header("Hand IK")]
    [Tooltip("왼손이 고정될 위치 (무기의 앞쪽 그립)")]
    [SerializeField] Transform leftHandGrip;
    [Tooltip("왼손 IK 세기 (0 = 꺼짐, 1 = 완전히 그립 위치)")]
    [Range(0f, 1f)][SerializeField] float leftHandIKWeight = 1f;

    [Header("Detection")]
    [SerializeField] float detectRange = 20f;
    [SerializeField] float attackRange = 10f;
    [SerializeField] float loseTargetRange = 25f;

    [Header("Movement")]
    [SerializeField] float moveSpeed = 3.5f;
    [SerializeField] float turnSpeed = 8f;

    [Header("Combat")]
    [SerializeField] float fireRate = 0.4f;
    [SerializeField] float damage = 10f;
    [SerializeField] float maxHp = 100f;
    [SerializeField] LayerMask shootLayerMask = ~0;
    [SerializeField] float aimHeightOffset = 1.0f;

    [Header("Visual Effects")]
    [Tooltip("총구 화염 프리팹 (사격 시 muzzle 위치에 생성)")]
    [SerializeField] GameObject muzzleFlashPrefab;
    [Tooltip("총구 화염 지속 시간 (초)")]
    [SerializeField] float muzzleFlashDuration = 0.1f;

    [Header("Bullet (Visual Only)")]
    [Tooltip("발사할 총알 프리팹 (DuNguyn 등)")]
    [SerializeField] GameObject bulletPrefab;
    [Tooltip("총알 비행 속도 (m/s)")]
    [SerializeField] float bulletSpeed = 80f;
    [Tooltip("총알 자동 제거 시간 (초)")]
    [SerializeField] float bulletLifetime = 0.5f;

    [Header("Death")]
    [Tooltip("사망 후 시체 유지 시간 (0 이하 = 영원히, 양수 = 그 시간 후 사라짐)")]
    [SerializeField] float corpseLifetime = -1f;

    // --- internal ---
    NavMeshAgent agent;
    Animator anim;
    Transform target;
    State state = State.Idle;
    float hp;
    float lastFireTime;
    BasicHealthManager healthManager;

    static readonly int HashSpeed = Animator.StringToHash("Speed");
    static readonly int HashIsShooting = Animator.StringToHash("IsShooting");
    static readonly int HashDie = Animator.StringToHash("Die");

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        anim = GetComponent<Animator>();
        agent.speed = moveSpeed;
        agent.updateRotation = false;
        hp = maxHp;

        // NeoFPS HealthManager 연동
        healthManager = GetComponent<BasicHealthManager>();
        if (healthManager != null)
        {
            healthManager.onIsAliveChanged += OnAliveChanged;
        }
    }

    void OnDestroy()
    {
        if (healthManager != null)
        {
            healthManager.onIsAliveChanged -= OnAliveChanged;
        }
    }

    void OnAliveChanged(bool isAlive)
    {
        if (!isAlive && state != State.Dead)
        {
            Die();
        }
    }

    void Start()
    {
        var playerGo = GameObject.FindGameObjectWithTag("Player");
        if (playerGo != null) target = playerGo.transform;
    }

    void Update()
    {
        if (state == State.Dead) return;

        // 플레이어를 아직 못 찾았으면 매 프레임 재시도 (NeoFPS는 런타임에 플레이어 스폰)
        if (target == null)
        {
            var playerGo = GameObject.FindGameObjectWithTag("Player");
            if (playerGo != null) target = playerGo.transform;
            else return;
        }

        float dist = Vector3.Distance(transform.position, target.position);

        switch (state)
        {
            case State.Idle:
                anim.SetFloat(HashSpeed, 0f);
                anim.SetBool(HashIsShooting, false);
                if (dist < detectRange) state = State.Chase;
                break;

            case State.Chase:
                agent.isStopped = false;
                agent.SetDestination(target.position);
                anim.SetFloat(HashSpeed, agent.velocity.magnitude);
                anim.SetBool(HashIsShooting, false);
                FaceDirection(agent.velocity);

                if (dist < attackRange) state = State.Attack;
                else if (dist > loseTargetRange) state = State.Idle;
                break;

            case State.Attack:
                agent.isStopped = true;
                anim.SetFloat(HashSpeed, 0f);
                anim.SetBool(HashIsShooting, true);
                FaceDirection(target.position - transform.position);

                if (Time.time >= lastFireTime + fireRate)
                {
                    Fire();
                    lastFireTime = Time.time;
                }

                if (dist > attackRange) state = State.Chase;
                break;
        }
    }

    // Animator가 본을 계산한 뒤에 상체를 플레이어 쪽으로 보정
    void LateUpdate()
    {
        if (state != State.Attack || aimBone == null || target == null) return;

        Vector3 aimPoint = target.position + Vector3.up * aimHeightOffset;
        Vector3 dir = aimPoint - aimBone.position;
        if (dir.sqrMagnitude < 0.01f) return;

        Quaternion lookRot = Quaternion.LookRotation(dir, Vector3.up);
        lookRot *= Quaternion.Euler(aimBoneLocalForwardFix);

        aimBone.rotation = Quaternion.Slerp(aimBone.rotation, lookRot, aimBoneWeight);
    }

    // IK 처리 — Animator Controller의 Layer에 'IK Pass' 체크 필수
    void OnAnimatorIK(int layerIndex)
    {
        if (anim == null) return;
        if (state == State.Dead) return;
        if (leftHandGrip == null) return;

        float weight = (state == State.Attack) ? leftHandIKWeight : 0f;
        anim.SetIKPositionWeight(AvatarIKGoal.LeftHand, weight);
        anim.SetIKRotationWeight(AvatarIKGoal.LeftHand, weight);
        anim.SetIKPosition(AvatarIKGoal.LeftHand, leftHandGrip.position);
        anim.SetIKRotation(AvatarIKGoal.LeftHand, leftHandGrip.rotation);
    }

    void FaceDirection(Vector3 dir)
    {
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.01f) return;
        Quaternion rot = Quaternion.LookRotation(dir);
        transform.rotation = Quaternion.Slerp(transform.rotation, rot, Time.deltaTime * turnSpeed);
    }

    void Fire()
    {
        if (muzzle == null || target == null) return;

        Vector3 aimPoint = target.position + Vector3.up * aimHeightOffset;
        Vector3 dir = (aimPoint - muzzle.position).normalized;

        // 시각효과 (머즐 플래시 + 총알)
        SpawnMuzzleFlash();
        SpawnBullet(muzzle.position, dir);

        // 데미지는 즉시 Raycast로 처리
        if (Physics.Raycast(muzzle.position, dir, out RaycastHit hit, 100f, shootLayerMask))
        {
            DealDamage(hit);
        }
    }

    void SpawnMuzzleFlash()
    {
        if (muzzleFlashPrefab == null) return;
        // muzzle의 자식으로 생성 → 무기와 함께 움직임
        var flash = Instantiate(muzzleFlashPrefab, muzzle.position, muzzle.rotation, muzzle);
        Destroy(flash, muzzleFlashDuration);
    }

    void SpawnBullet(Vector3 startPos, Vector3 direction)
    {
        if (bulletPrefab == null) return;

        // 총알 생성, 발사 방향으로 회전
        var bullet = Instantiate(bulletPrefab, startPos, Quaternion.LookRotation(direction));

        // BulletMover가 직선으로 이동시킴 (Kinematic Rigidbody와 무관, 충돌 무시)
        var mover = bullet.AddComponent<BulletMover>();
        mover.Init(direction * bulletSpeed, bulletLifetime);
        Destroy(bullet, bulletLifetime);
    }

    void DealDamage(RaycastHit hit)
    {
        var idh = hit.collider.GetComponentInParent<IDamageHandler>();
        if (idh != null)
        {
            idh.AddDamage(damage);
            return;
        }
        // NeoFPS DamageHandler 없으면 일반 SendMessage 폴백
        hit.collider.SendMessageUpwards("TakeDamage", damage, SendMessageOptions.DontRequireReceiver);
    }

    public void TakeDamage(float amount)
    {
        // 외부에서 직접 데미지 줄 때 사용 (디버그/특수 상황용)
        // 일반적으로는 BasicHealthManager가 데미지 처리하고 onIsAliveChanged로 사망 알림
        if (state == State.Dead) return;
        hp -= amount;
        if (hp <= 0f && healthManager == null)
        {
            Die();
        }
    }

    void Die()
    {
        if (state == State.Dead) return;

        state = State.Dead;
        anim.SetTrigger(HashDie);
        anim.SetBool(HashIsShooting, false);
        anim.SetFloat(HashSpeed, 0f);

        // AI 비활성화
        if (agent != null)
        {
            agent.isStopped = true;
            agent.enabled = false;
        }

        // 콜라이더 비활성화 (시체에 계속 부딪히지 않게)
        var col = GetComponent<Collider>();
        if (col != null) col.enabled = false;

        // 바닥에 시체 정렬 (NavMeshAgent 꺼지면 캐릭터가 공중에 뜨는 문제 해결)
        SnapCorpseToGround();

        // 시체 자동 제거 (옵션)
        if (corpseLifetime > 0f)
        {
            Destroy(gameObject, corpseLifetime);
        }
    }

    void SnapCorpseToGround()
    {
        Vector3 rayStart = transform.position + Vector3.up * 5f;
        if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, 20f, ~0, QueryTriggerInteraction.Ignore))
        {
            transform.position = hit.point;
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectRange);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
        Gizmos.color = new Color(1f, 0.5f, 0f);
        Gizmos.DrawWireSphere(transform.position, loseTargetRange);
    }
}