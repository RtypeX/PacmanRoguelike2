using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Debug helper for applying chosen upgrade tiers without buying them from the shop.
/// Attach to any scene object, assign upgrade assets, and set desired tiers in the Inspector.
/// </summary>
[DefaultExecutionOrder(-1000)]
public class UpgradeTestHarness : MonoBehaviour
{
    [System.Serializable]
    public class UpgradeTestEntry
    {
        public UpgradeData upgrade;
        [Min(0)] public int tiersToApply = 1;
    }

    [Header("Behavior")]
    public bool applyOnStart = true;
    public bool reloadSceneAfterApply = false;
    public bool verboseLogging = true;

    [Header("Upgrades To Simulate")]
    public List<UpgradeTestEntry> upgradesToApply = new List<UpgradeTestEntry>();

    private bool appliedThisPlaySession;

    private void Start()
    {
        if (!applyOnStart || appliedThisPlaySession)
            return;

        ApplyConfiguredUpgrades();
    }

    [ContextMenu("Apply Configured Upgrades")]
    public void ApplyConfiguredUpgrades()
    {
        GameManager.EnsureInstance();

        if (PlayerUpgrades.Instance == null)
        {
            Debug.LogError("UpgradeTestHarness could not find PlayerUpgrades.Instance.");
            return;
        }

        appliedThisPlaySession = true;

        foreach (UpgradeTestEntry entry in upgradesToApply)
        {
            if (entry == null || entry.upgrade == null || entry.tiersToApply <= 0)
                continue;

            for (int i = 0; i < entry.tiersToApply; i++)
            {
                PlayerUpgrades.Instance.ApplyUpgrade(entry.upgrade, 0, entry.upgrade.upgradeValue);
            }

            if (verboseLogging)
            {
                Debug.Log($"UpgradeTestHarness applied {entry.upgrade.upgradeName} x{entry.tiersToApply}. Final tier: {PlayerUpgrades.Instance.GetTier(entry.upgrade)}");
            }
        }

        if (reloadSceneAfterApply && Application.isPlaying)
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }

    [ContextMenu("Clear Configured Upgrade State")]
    public void ClearConfiguredUpgradeState()
    {
        GameManager.EnsureInstance();

        if (PlayerUpgrades.Instance == null)
        {
            Debug.LogError("UpgradeTestHarness could not find PlayerUpgrades.Instance.");
            return;
        }

        PlayerUpgrades.Instance.ResetUpgrades();
        appliedThisPlaySession = false;

        if (verboseLogging)
        {
            Debug.Log("UpgradeTestHarness cleared PlayerUpgrades state.");
        }
    }
}
