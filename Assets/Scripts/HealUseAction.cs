using UnityEngine;
using NeoFPS;

[CreateAssetMenu(fileName = "HealAction", menuName = "Inventory/Use Action/Heal")]
public class HealUseAction : ItemUseAction
{
    [Tooltip("회복량")]
    public float healAmount = 25f;

    [Tooltip("체력이 가득 차 있어도 사용 가능하게 할지")]
    public bool allowAtFullHealth = false;

    [Header("Messages")]
    public string successFormat = "HP +{0:0}";
    public string failNoTarget = "No target";
    public string failNoHealth = "No HealthManager";
    public string failDead = "Cannot use while dead";
    public string failFullHealth = "Health is full";

    public override bool CanUse(GameObject user)
    {
        if (user == null) return false;
        var hm = user.GetComponentInChildren<IHealthManager>();
        if (hm == null) return false;
        if (!hm.isAlive) return false;
        if (!allowAtFullHealth && hm.health >= hm.healthMax) return false;
        return true;
    }

    public override bool TryUse(GameObject user)
    {
        if (!CanUse(user)) return false;
        var hm = user.GetComponentInChildren<IHealthManager>();
        hm.AddHealth(healAmount);
        return true;
    }

    public override string GetSuccessMessage(GameObject user) => string.Format(successFormat, healAmount);

    public override string GetFailMessage(GameObject user)
    {
        if (user == null) return failNoTarget;
        var hm = user.GetComponentInChildren<IHealthManager>();
        if (hm == null) return failNoHealth;
        if (!hm.isAlive) return failDead;
        if (hm.health >= hm.healthMax) return failFullHealth;
        return base.GetFailMessage(user);
    }
}
