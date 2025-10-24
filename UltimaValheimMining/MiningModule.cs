using System;
using UnityEngine;
using UltimaValheim.Core;
using HarmonyLib;

namespace UltimaValheim.Mining
{
    /// <summary>
    /// Mining module for Ultima Valheim.
    /// Implements UO-style mining system with skill-based ore drops.
    /// </summary>
    public class MiningModule : ICoreModule
    {
        public string ModuleID => "UltimaValheim.Mining";
        public System.Version ModuleVersion
        {
            get { return new System.Version(1, 0, 0, 0); }
        }

        private MiningSystem _miningSystem;
        private Harmony _harmony;
        private AssetBundle _assetBundle;
        private BepInEx.Configuration.ConfigEntry<bool> _enableMiningSystem;
        private BepInEx.Configuration.ConfigEntry<float> _skillGainRate;

        public void OnCoreReady()
        {
            CoreAPI.Log.LogInfo($"[{ModuleID}] Initializing Mining module...");

            // Load configuration
            _enableMiningSystem = CoreAPI.Config.Bind(
                ModuleID,
                "EnableMiningSystem",
                true,
                "Enable the Ultima Online mining system"
            );

            _skillGainRate = CoreAPI.Config.Bind(
                ModuleID,
                "SkillGainRate",
                1.0f,
                "Multiplier for mining skill gain rate (will be used when Skills module is added)"
            );

            if (!_enableMiningSystem.Value)
            {
                CoreAPI.Log.LogInfo($"[{ModuleID}] Mining system is disabled in config.");
                return;
            }

            // Load asset bundle
            LoadAssets();

            // Initialize mining system
            _miningSystem = new MiningSystem();

            // Register ore items with Jotunn if asset bundle loaded
            if (_assetBundle != null)
            {
                RegisterOreItems();
            }

            // Apply Harmony patches to intercept rock mining
            _harmony = new Harmony("com.valheim.ultima.mining");
            ApplyHarmonyPatches();

            CoreAPI.Log.LogInfo($"[{ModuleID}] Mining module initialized successfully!");
        }

        private void LoadAssets()
        {
            try
            {
                // Log all embedded resources for debugging
                CoreAPI.Log.LogInfo($"[{ModuleID}] Embedded resources: {string.Join(", ", typeof(MiningModule).Assembly.GetManifestResourceNames())}");

                // Load asset bundle from embedded resources
                _assetBundle = Jotunn.Utils.AssetUtils.LoadAssetBundleFromResources("uomining");

                if (_assetBundle != null)
                {
                    CoreAPI.Log.LogInfo($"[{ModuleID}] Successfully loaded uomining asset bundle from embedded resources");

                    // Log all assets in the bundle for debugging
                    string[] assetNames = _assetBundle.GetAllAssetNames();
                    CoreAPI.Log.LogInfo($"[{ModuleID}] Bundle contains {assetNames.Length} assets:");
                    foreach (string assetName in assetNames)
                    {
                        CoreAPI.Log.LogInfo($"[{ModuleID}]   - {assetName}");
                    }
                }
                else
                {
                    CoreAPI.Log.LogError($"[{ModuleID}] Failed to load uomining asset bundle from embedded resources");
                }
            }
            catch (Exception ex)
            {
                CoreAPI.Log.LogError($"[{ModuleID}] Exception while loading asset bundle: {ex}");
            }
        }

        private void RegisterOreItems()
        {
            try
            {
                // Wait for Jotunn's ItemManager to be ready
                Jotunn.Managers.PrefabManager.OnVanillaPrefabsAvailable += () =>
                {
                    RegisterOres();
                    RegisterIngots();
                };
            }
            catch (Exception ex)
            {
                CoreAPI.Log.LogError($"[{ModuleID}] Failed to register ore items: {ex}");
            }
        }

        private void RegisterOres()
        {
            // Ore list from CSV
            string[] ores = new string[]
            {
                "UOiron_ore",
                "UOshadow_ore",
                "UOgold_ore",
                "UOagapite_ore",
                "UOverite_ore",
                "UOsnow_ore",
                "UOice_ore",
                "UObloodrock_ore",
                "UOvalorite_ore",
                "UOblackrock_ore"
            };

            foreach (string oreID in ores)
            {
                try
                {
                    var orePrefab = _assetBundle.LoadAsset<GameObject>(oreID);
                    if (orePrefab != null)
                    {
                        // Generate sprite from the prefab
                        var oreItem = orePrefab.GetComponent<ItemDrop>();
                        if (oreItem != null)
                        {
                            oreItem.m_itemData.m_shared.m_icons = new Sprite[] {
                                Jotunn.Managers.RenderManager.Instance.Render(orePrefab, Jotunn.Managers.RenderManager.IsometricRotation)
                            };
                        }

                        // Create custom item (no recipe - obtained from mining only)
                        Jotunn.Entities.CustomItem customOre = new Jotunn.Entities.CustomItem(orePrefab, fixReference: true);
                        Jotunn.Managers.ItemManager.Instance.AddItem(customOre);

                        CoreAPI.Log.LogInfo($"[{ModuleID}] Registered ore: {oreID}");
                    }
                    else
                    {
                        CoreAPI.Log.LogWarning($"[{ModuleID}] Could not find ore prefab: {oreID}");
                    }
                }
                catch (Exception ex)
                {
                    CoreAPI.Log.LogError($"[{ModuleID}] Failed to register ore {oreID}: {ex}");
                }
            }
        }

        private void RegisterIngots()
        {
            // Ingot list - these will need smelting recipes later
            string[] ingots = new string[]
            {
                "UOiron_ingot",
                "UOshadow_ingot",
                "UOgold_ingot",
                "UOagapite_ingot",
                "UOverite_ingot",
                "UOsnow_ingot",
                "UOice_ingot",
                "UObloodrock_ingot",
                "UOvalorite_ingot",
                "UOblackrock_ingot"
            };

            foreach (string ingotID in ingots)
            {
                try
                {
                    var ingotPrefab = _assetBundle.LoadAsset<GameObject>(ingotID);
                    if (ingotPrefab != null)
                    {
                        // Generate sprite from the prefab
                        var ingotItem = ingotPrefab.GetComponent<ItemDrop>();
                        if (ingotItem != null)
                        {
                            ingotItem.m_itemData.m_shared.m_icons = new Sprite[] {
                                Jotunn.Managers.RenderManager.Instance.Render(ingotPrefab, Jotunn.Managers.RenderManager.IsometricRotation)
                            };
                        }

                        // Create custom item (no recipe yet - will add smelting later)
                        Jotunn.Entities.CustomItem customIngot = new Jotunn.Entities.CustomItem(ingotPrefab, fixReference: true);
                        Jotunn.Managers.ItemManager.Instance.AddItem(customIngot);

                        CoreAPI.Log.LogInfo($"[{ModuleID}] Registered ingot: {ingotID}");
                    }
                    else
                    {
                        CoreAPI.Log.LogWarning($"[{ModuleID}] Could not find ingot prefab: {ingotID}");
                    }
                }
                catch (Exception ex)
                {
                    CoreAPI.Log.LogError($"[{ModuleID}] Failed to register ingot {ingotID}: {ex}");
                }
            }
        }

        private void ApplyHarmonyPatches()
        {
            try
            {
                // Patch Destructible (rocks) when they take damage
                _harmony.Patch(
                    AccessTools.Method(typeof(Destructible), nameof(Destructible.Damage)),
                    postfix: new HarmonyMethod(typeof(MiningModule), nameof(OnDestructibleDamaged_Postfix))
                );

                CoreAPI.Log.LogInfo($"[{ModuleID}] Applied Harmony patches for mining.");
            }
            catch (Exception ex)
            {
                CoreAPI.Log.LogError($"[{ModuleID}] Failed to apply Harmony patches: {ex}");
            }
        }

        /// <summary>
        /// Harmony postfix patch for when a Destructible (rock) takes damage.
        /// This is where we intercept pickaxe hits on rocks.
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch(typeof(Destructible), nameof(Destructible.Damage))]
        private static void OnDestructibleDamaged_Postfix(Destructible __instance, HitData hit)
        {
            try
            {
                // Only process on server
                if (!ZNet.instance.IsServer())
                    return;

                // Only process if it's a rock (MineRock5 or similar)
                if (__instance == null || __instance.gameObject == null)
                    return;

                string objectName = __instance.gameObject.name.ToLower();
                if (!objectName.Contains("rock") && !objectName.Contains("mine"))
                    return;

                // Get the player who hit the rock
                if (hit == null || hit.GetAttacker() == null)
                    return;

                Player player = hit.GetAttacker() as Player;
                if (player == null)
                    return;

                // Check if player is using a pickaxe
                if (!IsUsingPickaxe(player))
                    return;

                // Get the Mining module instance from Core
                var miningModule = CoreLifecycle.GetModule("UltimaValheim.Mining") as MiningModule;
                if (miningModule == null || miningModule._miningSystem == null)
                    return;

                // Process the mining attempt
                miningModule._miningSystem.OnRockMined(player, __instance.gameObject);
            }
            catch (Exception ex)
            {
                CoreAPI.Log.LogError($"[UltimaValheim.Mining] Error in OnDestructibleDamaged_Postfix: {ex}");
            }
        }

        /// <summary>
        /// Check if player is currently using a pickaxe.
        /// </summary>
        private static bool IsUsingPickaxe(Player player)
        {
            if (player == null)
                return false;

            var currentWeapon = player.GetCurrentWeapon();
            if (currentWeapon == null)
                return false;

            string weaponName = currentWeapon.m_shared.m_name.ToLower();
            return weaponName.Contains("pickaxe") || weaponName.Contains("pick");
        }

        public void OnPlayerJoin(Player player)
        {
            // Nothing needed on player join for mining
        }

        public void OnPlayerLeave(Player player)
        {
            // Nothing needed on player leave for mining
        }

        public void OnSave()
        {
            // Player mining skills are saved via ServerCharacters (PlayerDataManager)
            // No additional save logic needed
        }

        public void OnShutdown()
        {
            CoreAPI.Log.LogInfo($"[{ModuleID}] Shutting down Mining module...");

            // Unpatch Harmony
            _harmony?.UnpatchSelf();
        }

        #region Public API (for admin commands/debugging)

        /// <summary>
        /// Set a player's mining skill (for admin commands).
        /// </summary>
        public void SetPlayerMiningSkill(Player player, float skill)
        {
            _miningSystem?.SetPlayerMiningSkill(player, skill);
        }

        /// <summary>
        /// Get a player's current mining skill.
        /// </summary>
        public float GetPlayerMiningSkill(Player player)
        {
            if (player == null)
                return 0f;

            return CoreAPI.PlayerData.GetFloat(player, "Mining_Skill", 50f);
        }

        #endregion
    }
}
