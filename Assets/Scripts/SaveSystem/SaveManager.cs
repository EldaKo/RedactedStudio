using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class SaveManager
{
    private const string FileName = "save.json";
    private const string HideoutScene = "Hideout";

    public static GameSaveData PendingLoad;
    public static bool SaveOnHideoutLoad;

    private static string SavePath => Path.Combine(Application.persistentDataPath, FileName);

    public static bool HasSave() => File.Exists(SavePath);

    public static void Save(GameSaveData data)
    {
        try
        {
            var json = JsonUtility.ToJson(data, true);
            File.WriteAllText(SavePath, json);
            Debug.Log($"[Save] Saved to {SavePath}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[Save] Failed: {e.Message}");
        }
    }

    public static GameSaveData Load()
    {
        if (!File.Exists(SavePath)) return null;
        try
        {
            var json = File.ReadAllText(SavePath);
            return JsonUtility.FromJson<GameSaveData>(json);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[Save] Load failed: {e.Message}");
            return null;
        }
    }

    public static void DeleteSave()
    {
        if (File.Exists(SavePath)) File.Delete(SavePath);
    }

    public static GameSaveData CaptureCurrentState()
    {
        var data = new GameSaveData();

        if (PlayerUpgradeManager.Instance != null)
        {
            data.armorLevel = PlayerUpgradeManager.Instance.armorLevel;
            data.healthLevel = PlayerUpgradeManager.Instance.healthLevel;
            foreach (var wl in PlayerUpgradeManager.Instance.weaponLevelList)
                data.weaponLevels.Add(new WeaponLevelSave { weaponName = wl.weaponName, level = wl.level });
        }

        foreach (var kv in FacilityLevelTracker.All)
            data.facilities.Add(new FacilitySave { facilityName = kv.Key, currentLevel = kv.Value });

        data.clearedStages = new System.Collections.Generic.List<string>(StageProgress.All);
        data.unlockedWeapons = new System.Collections.Generic.List<string>(PlayerLoadout.AllUnlocked);
        data.equippedWeapon = PlayerLoadout.EquippedWeapon;

        foreach (var f in Object.FindObjectsOfType<FacilityScript>(true))
        {
            if (GetSavedFacility(data, f.facilityName) != null) continue;
            data.facilities.Add(new FacilitySave { facilityName = f.facilityName, currentLevel = f.currentLevel });
        }

        if (Inventory.HasInstance)
        {
            for (int i = 0; i < Inventory.Instance.SlotCount; i++)
            {
                var slot = Inventory.Instance.GetSlot(i);
                if (slot == null || slot.data == null || slot.count <= 0) continue;
                data.inventorySlots.Add(new InventorySlotSave
                {
                    slotIndex = i,
                    itemDataName = slot.data.itemName,
                    count = slot.count
                });
            }
        }

        return data;
    }

    private static FacilitySave GetSavedFacility(GameSaveData data, string name)
    {
        foreach (var f in data.facilities) if (f.facilityName == name) return f;
        return null;
    }

    public static void ContinueGame()
    {
        var data = Load();
        if (data == null)
        {
            Debug.LogWarning("[Save] No save file. Starting new game instead.");
            StartNewGame();
            return;
        }
        PendingLoad = data;
        SaveOnHideoutLoad = false;
        ResetPersistentSingletons();
        SceneManager.LoadScene(HideoutScene);
    }

    public static void StartNewGame()
    {
        DeleteSave();
        PendingLoad = null;
        SaveOnHideoutLoad = false;
        FacilityLevelTracker.Clear();
        StageProgress.Clear();
        PlayerLoadout.Clear();

        // 첫 무기는 기본 해금 + 장착
        var db = WeaponData.Get();
        if (db != null && db.weapons.Count > 0 && db.weapons[0] != null)
        {
            PlayerLoadout.Unlock(db.weapons[0].id);
            PlayerLoadout.Equip(db.weapons[0].id);
        }

        ResetPersistentSingletons();
        SceneManager.LoadScene(HideoutScene);
    }

    private static void ResetPersistentSingletons()
    {
        if (PlayerUpgradeManager.Instance != null)
        {
            PlayerUpgradeManager.Instance.armorLevel = 1;
            PlayerUpgradeManager.Instance.healthLevel = 1;
            PlayerUpgradeManager.Instance.weaponLevelList.Clear();
        }
        if (Inventory.HasInstance) Inventory.Instance.ClearAll();
    }
}
