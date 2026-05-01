using UnityEngine;
using UnityEngine.AI;
using NeoFPS;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Animator))]
public class EnemyAI : MonoBehaviour
{
    enum State { Idle, Chase, Attack, Search, Dead }

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

    [Header("Vision")]
    [Tooltip("시야각 (도). 360 = 전방위, 120 = 정면 시야")]
    [Range(30f, 360f)][SerializeField] float viewAngle = 120f;
    [Tooltip("시야 시작 높이 (적 발 기준, 머리 높이로 설정)")]
    [SerializeField] float eyeHeight = 1.6f;
    [Tooltip("시야 차단 검사할 Layer (벽/지형만 체크. CharacterControllers는 빼야 함)")]
    [SerializeField] LayerMask sightBlockingMask = ~0;

    [Header("Search Behavior")]
    [Tooltip("시야 잃은 후 마지막 위치에서 두리번거리는 시간 (초)")]
    [SerializeField] float searchDuration = 5f;
    [Tooltip("두리번거릴 때 좌우 회전 속도 (도/초)")]
    [SerializeField] float searchRotationSpeed = 60f;
    [Tooltip("마지막 본 위치 도달 판정 거리 (m)")]
    [SerializeField] float lastSeenReachThreshold = 1.5f;

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

    // 시야/Search 관련 내부 변수
    Vector3 lastKnownPosition;
    float searchTimer;
    float searchRotationDir = 1f;
    float searchRotationFlipTimer;

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
        bool canSee = HasLineOfSight();

        switch (state)
        {
            case State.Idle:
                anim.SetFloat(HashSpeed, 0f);
                anim.SetBool(HashIsShooting, false);
                if (dist < detectRange && canSee) state = State.Chase;
                break;

            case State.Chase:
                agent.isStopped = false;
                agent.SetDestination(target.position);
                anim.SetFloat(HashSpeed, agent.velocity.magnitude);
                anim.SetBool(HashIsShooting, false);
                FaceDirection(agent.velocity);

                // 시야 잃으면 즉시 Search (마지막 본 위치 저장)
                if (!canSee)
                {
                    EnterSearch();
                    break;
                }

                if (dist < attackRange) state = State.Attack;
                else if (dist > loseTargetRange) state = State.Idle;
                break;

            case State.Attack:
                agent.isStopped = true;
                anim.SetFloat(HashSpeed, 0f);
                anim.SetBool(HashIsShooting, true);
                FaceDirection(target.position - transform.position);

                // 시야 잃으면 즉시 사격 멈추고 Search
                if (!canSee)
                {
                    EnterSearch();
                    break;
                }

                if (Time.time >= lastFireTime + fireRate)
                {
                    Fire();
                    lastFireTime = Time.time;
                }

                if (dist > attackRange) state = State.Chase;
                break;

            case State.Search:
                anim.SetBool(HashIsShooting, false);

                // 추적 중에 시야 회복 → 즉시 다시 Chase/Attack
                if (canSee && dist < detectRange)
                {
                    state = (dist < attackRange) ? State.Attack : State.Chase;
                    break;
                }

                // 마지막 본 위치까지 이동
                float distToLastSeen = Vector3.Distance(transform.position, lastKnownPosition);
                if (distToLastSeen > lastSeenReachThreshold)
                {
                    // 이동 중
                    agent.isStopped = false;
                    agent.SetDestination(lastKnownPosition);
                    anim.SetFloat(HashSpeed, agent.velocity.magnitude);
                    FaceDirection(agent.velocity);
                }
                else
                {
                    // 도착 → 두리번거리기
                    agent.isStopped = true;
                    anim.SetFloat(HashSpeed, 0f);

                    // 좌우 회전
                    transform.Rotate(Vector3.up, searchRotationDir * searchRotationSpeed * Time.deltaTime);

                    // 1.5초마다 회전 방향 뒤집기
                    searchRotationFlipTimer += Time.deltaTime;
                    if (searchRotationFlipTimer >= 1.5f)
                    {
                        searchRotationDir = -searchRotationDir;
                        searchRotationFlipTimer = 0f;
                    }

                    // 타이머 진행
                    searchTimer += Time.deltaTime;
                    if (searchTimer >= searchDuration)
                    {
                        // 못 찾음 → Idle 복귀
                        state = State.Idle;
                    }
                }
                break;
        }
    }

    void EnterSearch()
    {
        state = State.Search;
        lastKnownPosition = target.position;
        searchTimer = 0f;
        searchRotationFlipTimer = 0f;
        searchRotationDir = 1f;
    }

    /// <summary>
    /// 시야선 검사: 시야각 + Raycast 통합. 벽 차단 시 false 리턴.
    /// </summary>
    bool HasLineOfSight()
    {
        if (target == null) return false;

        Vector3 eyePos = transform.position + Vector3.up * eyeHeight;
        Vector3 aimPos = target.position + Vector3.up * aimHeightOffset;
        Vector3 toTarget = aimPos - eyePos;

        // 1. 시야각 검사 (수평 기준)
        Vector3 toTargetFlat = toTarget;
        toTargetFlat.y = 0f;
        if (toTargetFlat.sqrMagnitude > 0.01f)
        {
            float angle = Vector3.Angle(transform.forward, toTargetFlat);
            if (angle > viewAngle * 0.5f) return false;
        }

        // 2. Raycast로 벽 차단 검사
        float distance = toTarget.magnitude;
        if (Physics.Raycast(eyePos, toTarget.normalized, out RaycastHit hit, distance, sightBlockingMask, QueryTriggerInteraction.Ignore))
        {
            // 무언가에 막힘. 그게 플레이어 본인이면 통과, 다른 거면 차단
            if (hit.collider.transform != target && !hit.collider.transform.IsChildOf(target))
            {
                return false;
            }
        }

        return true;
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

        // 시야각 표시
        Gizmos.color = Color.cyan;
        Vector3 eyePosG = transform.position + Vector3.up * eyeHeight;
        Vector3 leftDirG = Quaternion.Euler(0, -viewAngle * 0.5f, 0) * transform.forward;
        Vector3 rightDirG = Quaternion.Euler(0, viewAngle * 0.5f, 0) * transform.forward;
        Gizmos.DrawLine(eyePosG, eyePosG + leftDirG * detectRange);
        Gizmos.DrawLine(eyePosG, eyePosG + rightDirG * detectRange);
    }
}