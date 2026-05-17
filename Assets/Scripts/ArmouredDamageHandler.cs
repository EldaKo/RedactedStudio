using NeoFPS.Constants;
using UnityEngine;

namespace NeoFPS
{
    [HelpURL("https://docs.neofps.com/manual/healthref-mb-armoureddamagehandler.html")]
    public class ArmouredDamageHandler : BasicDamageHandler
    {
        [Header("Armour Settings (방탄복 설정)")]

        [SerializeField, FpsInventoryKey, Tooltip("인벤토리에서 방탄복으로 인식할 아이템의 ID입니다.")]
        private int m_InventoryID = 0;

        [SerializeField, Range(0, 5), Tooltip("방탄복의 등급(레벨)입니다. 등급이 높을수록 기본 방어력이 상승합니다.")]
        private int m_ArmourLevel = 1;

        [SerializeField, Range(0f, 1f), Tooltip("레벨 0일 때의 기본 데미지 감소율입니다. (0.5 = 50% 방어)")]
        private float m_BaseDamageMitigation = 0.5f;

        [SerializeField, Range(0f, 0.2f), Tooltip("레벨이 1 오를 때마다 추가되는 데미지 감소율입니다.")]
        private float m_MitigationPerLevel = 0.1f;

        [SerializeField, Tooltip("방탄복 내구도가 깎이는 양을 조절하는 배율입니다. 값이 낮을수록 방탄복이 오래갑니다.")]
        private float m_ArmourDamageMultiplier = 0.5f;

        [SerializeField, Tooltip("방탄복이 방어할 수 있는 데미지 종류를 선택합니다. (예: 총알, 폭발 등)")]
        private DamageType m_ArmourDamageFilter = DamageType.All;

        private IInventory m_Inventory = null;

        // 인스펙터에서 값이 변경될 때 호출되어 범위를 제한합니다.
#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            m_ArmourDamageMultiplier = Mathf.Clamp(m_ArmourDamageMultiplier, 0f, 100f);
        }
#endif

        protected override void Awake()
        {
            base.Awake();
            // 캐릭터의 부모 오브젝트로부터 인벤토리 시스템을 가져옵니다.
            m_Inventory = GetComponentInParent<IInventory>();
        }

        /// <summary>
        /// 현재 레벨과 기본 설정값을 합쳐 최종 데미지 감소율을 계산합니다.
        /// </summary>
        private float GetFinalMitigation()
        {
            // 기본값 + (레벨 * 레벨당 보너스)를 계산하고 0~1(0%~100%) 사이로 고정합니다.
            return Mathf.Clamp01(m_BaseDamageMitigation + (m_ArmourLevel * m_MitigationPerLevel));
        }

        /// <summary>
        /// 방탄복 로직을 적용하여 실제 체력에 가해질 데미지를 계산하는 핵심 함수입니다.
        /// </summary>
        bool GetDamageAfterArmour(ref float damage, DamageType t)
        {
            // 1. 인벤토리가 없거나, 방탄복이 방어할 수 없는 데미지 타입인 경우 방어 없이 통과
            if (m_Inventory == null || (m_ArmourDamageFilter & t) == DamageType.None)
                return true;

            // 2. 인벤토리에서 지정된 ID의 방탄복 아이템을 찾습니다.
            var item = m_Inventory.GetItem(m_InventoryID);
            
            // 3. 방탄복 아이템이 없거나 내구도(quantity)가 0이면 방어 불가능
            if (item == null || item.quantity == 0)
                return true;
            
            // 4. 레벨 시스템이 적용된 최종 데미지 감소량 계산
            float finalMitigationRate = GetFinalMitigation();
            float mitigated = damage * finalMitigationRate;

            // 5. 방탄복 내구도 소모량 계산 (막아낸 데미지 * 소모 배율)
            int armourDamage = Mathf.CeilToInt(mitigated * m_ArmourDamageMultiplier);

            // 6. 만약 소모될 내구도가 남은 내구도보다 많다면, 남은 양만큼만 소모
            if (armourDamage > item.quantity)
            {
                armourDamage = item.quantity;
               
                if (m_ArmourDamageMultiplier > 0f)
                    mitigated = armourDamage / m_ArmourDamageMultiplier;
                else
                    mitigated = damage * finalMitigationRate; // 배율이 0이면 내구도 부족 상황이 올 수 없지만 안전장치로 원상복구
            }
          
            // 7. 실제 방탄복 아이템의 수치(내구도)를 차감
            item.quantity -= armourDamage;

            // 9. 원래 데미지에서 방탄복이 막아낸 만큼을 제외합니다.
            damage -= mitigated;

            // 남은 데미지가 0보다 크면 true를 반환하여 체력을 깎고, 0이면 Blocked 처리합니다.
            return damage > 0f;
        }

        #region IDamageHandler 상속 구현부
        // 외부 데미지 소스(IDamageSource)가 있는 경우의 데미지 처리
        public override DamageResult AddDamage(float damage, RaycastHit hit, IDamageSource source)
        {
            if (GetDamageAfterArmour(ref damage, source.outDamageFilter.GetDamageType()))
                return base.AddDamage(damage, hit, source);
            else
                return DamageResult.Blocked;
        }

        // 레이캐스트 충돌 지점 정보만 있는 경우 (기본 데미지 타입 적용)
        public override DamageResult AddDamage(float damage, RaycastHit hit)
        {
            if (GetDamageAfterArmour(ref damage, DamageType.Default))
                return base.AddDamage(damage, hit);
            else
                return DamageResult.Blocked;
        }

        // 데미지 소스만 있는 경우 (범위 데미지 등)
        public override DamageResult AddDamage(float damage, IDamageSource source)
        {
            if (GetDamageAfterArmour(ref damage, source.outDamageFilter.GetDamageType()))
                return base.AddDamage(damage, source);
            else
                return DamageResult.Blocked;
        }

        // 데미지 양만 전달된 경우
        public override DamageResult AddDamage(float damage)
        {
            if (GetDamageAfterArmour(ref damage, DamageType.Default))
                return base.AddDamage(damage);
            else
                return DamageResult.Blocked;
        }
        #endregion

        // ==========================================
        // [새로 추가된 부분] 외부 스크립트에서 방탄복 레벨을 설정하는 함수
        // ==========================================
        public void SetArmourLevel(int newLevel)
        {
            m_ArmourLevel = newLevel;
            // 필요하다면 여기에 레벨업 이펙트나 사운드 코드를 추가할 수도 있습니다.
        }
    }
}