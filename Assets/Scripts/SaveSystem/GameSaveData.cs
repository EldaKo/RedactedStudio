using System;
using System.Collections.Generic;

[Serializable]
public class GameSaveData
{
    public string version = "1";
    public int armorLevel = 1;
    public List<WeaponLevelSave> weaponLevels = new List<WeaponLevelSave>();
    public List<FacilitySave> facilities = new List<FacilitySave>();
    public List<InventorySlotSave> inventorySlots = new List<InventorySlotSave>();
    public List<string> clearedStages = new List<string>();
    public List<string> unlockedWeapons = new List<string>();
    public string equippedWeapon = "";
}

[Serializable]
public class WeaponLevelSave
{
    public string weaponName;
    public int level;
}

[Serializable]
public class FacilitySave
{
    public string facilityName;
    public int currentLevel;
}

[Serializable]
public class InventorySlotSave
{
    public int slotIndex;
    public string itemDataName;
    public int count;
}
