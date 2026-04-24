using UnityEngine;
using UnityEngine.AI;

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

    // --- internal ---
    NavMeshAgent agent;
    Animator anim;
    Transform target;
    State state = State.Idle;
    float hp;
    float lastFireTime;

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

    void Start()
    {
        var playerGo = GameObject.FindGameObjectWithTag("Player");
        if (playerGo != null) target = playerGo.transform;
        else Debug.LogWarning("[EnemyAI] Player 태그를 가진 오브젝트를 찾지 못했습니다.");
    }

    void Update()
    {
        if (state == State.Dead || target == null) return;

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

        // 월드 공간에서 목표 방향을 바라보는 회전
        Quaternion lookRot = Quaternion.LookRotation(dir, Vector3.up);
        // 본의 축이 실제 forward와 다를 경우 보정
        lookRot *= Quaternion.Euler(aimBoneLocalForwardFix);

        aimBone.rotation = Quaternion.Slerp(aimBone.rotation, lookRot, aimBoneWeight);
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

    void DealDamage(RaycastHit hit)
    {
        var idh = hit.collider.GetComponentInParent(
            System.Type.GetType("NeoFPS.IDamageHandler, NeoFPS") ?? typeof(MonoBehaviour));
        if (idh != null && idh.GetType().GetMethod("AddDamage", new[] { typeof(float) }) != null)
        {
            idh.GetType().GetMethod("AddDamage", new[] { typeof(float) })
               .Invoke(idh, new object[] { damage });
            return;
        }
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