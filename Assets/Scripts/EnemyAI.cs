using UnityEngine;
using UnityEngine.AI;
using NeoFPS;  // ← 추가

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

    [Header("Target Acquisition")]
    [Tooltip("플레이어 재탐색 주기 (초). 스폰 시스템 사용 중이라면 0.5초가 무난")]
    [SerializeField] float retargetInterval = 0.5f;

    // --- internal ---
    NavMeshAgent agent;
    Animator anim;
    Transform target;
    State state = State.Idle;
    float hp;
    float lastFireTime;
    float lastRetargetTime;

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
    }

    // Start의 1회 검색 제거. Update에서 폴링으로 대체.
    void TryAcquireTarget()
    {
        if (target != null) return;
        if (Time.time < lastRetargetTime + retargetInterval) return;

        lastRetargetTime = Time.time;
        var playerGo = GameObject.FindGameObjectWithTag("Player");
        if (playerGo != null) target = playerGo.transform;
    }

    void Update()
    {
        if (state == State.Dead) return;

        // 플레이어가 아직 없으면 주기적 재탐색
        if (target == null)
        {
            TryAcquireTarget();
            return;
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

        if (Physics.Raycast(muzzle.position, dir, out RaycastHit hit, 100f, shootLayerMask))
        {
            Debug.DrawLine(muzzle.position, hit.point, Color.red, 0.1f);
            DealDamage(hit);
        }
    }

    // NeoFPS IDamageHandler 직접 사용. 리플렉션 제거.
    // RaycastHit을 같이 넘겨서 헤드샷 디텍트/데미지 마커가 정상 동작.
    void DealDamage(RaycastHit hit)
    {
        var damageHandler = hit.collider.GetComponentInParent<IDamageHandler>();
        if (damageHandler != null)
        {
            damageHandler.AddDamage(damage, hit);
            return;
        }

        // NeoFPS 외부 오브젝트(자체 적 등) 호환용 fallback
        hit.collider.SendMessageUpwards("TakeDamage", damage, SendMessageOptions.DontRequireReceiver);
    }

    public void TakeDamage(float amount)
    {
        if (state == State.Dead) return;
        hp -= amount;
        if (hp <= 0f) Die();
    }

    void Die()
    {
        state = State.Dead;
        anim.SetTrigger(HashDie);
        anim.SetBool(HashIsShooting, false);
        anim.SetFloat(HashSpeed, 0f);
        if (agent != null) { agent.isStopped = true; agent.enabled = false; }
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