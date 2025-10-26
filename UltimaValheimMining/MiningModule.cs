using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UltimaValheim.Core;
using HarmonyLib;

namespace UltimaValheim.Mining
{
    /// <summary>
    /// Mining module for Ultima Valheim.
    /// Implements UO-style mining system with skill-based ore drops.
    /// OPTIMIZED VERSION with performance improvements and smelting support.
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

            // Initialize mining system with optimizations
            _miningSystem = new MiningSystem();

            // Register ore items with Jotunn if asset bundle loaded
            if (_assetBundle != null)
            {
                RegisterOreItems();
                RegisterSmeltingRecipes();
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
#if DEBUG
                // Log all embedded resources for debugging
                CoreAPI.Log.LogInfo($"[{ModuleID}] Embedded resources: {string.Join(", ", typeof(MiningModule).Assembly.GetManifestResourceNames())}");
#endif

                // Load asset bundle from embedded resources
                _assetBundle = Jotunn.Utils.AssetUtils.LoadAssetBundleFromResources("uomining");

                if (_assetBundle != null)
                {
                    CoreAPI.Log.LogInfo($"[{ModuleID}] Successfully loaded uomining asset bundle from embedded resources");

#if DEBUG
                    // Log all assets in the bundle for debugging
                    string[] assetNames = _assetBundle.GetAllAssetNames();
                    CoreAPI.Log.LogInfo($"[{ModuleID}] Bundle contains {assetNames.Length} assets:");
                    foreach (string assetName in assetNames)
                    {
                        CoreAPI.Log.LogInfo($"[{ModuleID}]   - {assetName}");
                    }
#endif
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
            // Ore list - removed UOiron_ore as we'll use vanilla IronOre
            string[] ores = new string[]
            {
                // "UOiron_ore", // Removed - using vanilla IronOre instead
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
            // Ingot list - removed UOiron_ingot as we'll use vanilla Iron
            string[] ingots = new string[]
            {
                // "UOiron_ingot", // Removed - using vanilla Iron instead
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

        private void RegisterSmeltingRecipes()
        {
            try
            {
                // Wait for vanilla prefabs to be available before adding conversions
                Jotunn.Managers.PrefabManager.OnVanillaPrefabsAvailable += () =>
                {
                    CoreAPI.Log.LogInfo($"[{ModuleID}] Registering smelting recipes...");

                    // Define ore-to-ingot conversions
                    var conversions = new Dictionary<string, string>
                    {
                        // Vanilla iron ore is handled by vanilla smelting already
                        
                        // Custom ores to custom ingots
                        { "UOshadow_ore", "UOshadow_ingot" },
                        { "UOgold_ore", "UOgold_ingot" },
                        { "UOagapite_ore", "UOagapite_ingot" },
                        { "UOverite_ore", "UOverite_ingot" },
                        { "UOsnow_ore", "UOsnow_ingot" },
                        { "UOice_ore", "UOice_ingot" },
                        { "UObloodrock_ore", "UObloodrock_ingot" },
                        { "UOvalorite_ore", "UOvalorite_ingot" },
                        { "UOblackrock_ore", "UOblackrock_ingot" }
                    };

                    foreach (var kvp in conversions)
                    {
                        try
                        {
                            // Create smelter conversion config
                            var smelterConfig = new Jotunn.Configs.SmelterConversionConfig
                            {
                                // Station defaults to "smelter" if not specified
                                Station = "smelter", // Can also use: "blastfurnace", "eitrrefinery", etc.
                                FromItem = kvp.Key,  // Input item (ore)
                                ToItem = kvp.Value   // Output item (ingot)
                                // Note: Cook time is NOT a property of SmelterConversionConfig
                                // Smelting uses the station's default cook time (30 seconds for smelter)
                            };

                            // Add the conversion to the ItemManager
                            var customConversion = new Jotunn.Entities.CustomItemConversion(smelterConfig);
                            Jotunn.Managers.ItemManager.Instance.AddItemConversion(customConversion);

                            CoreAPI.Log.LogInfo($"[{ModuleID}] Added smelting recipe: {kvp.Key} -> {kvp.Value}");
                        }
                        catch (Exception ex)
                        {
                            CoreAPI.Log.LogError($"[{ModuleID}] Failed to add smelting recipe for {kvp.Key}: {ex}");
                        }
                    }

                    // Optionally add blast furnace conversions for higher-tier ores
                    RegisterBlastFurnaceRecipes();
                };
            }
            catch (Exception ex)
            {
                CoreAPI.Log.LogError($"[{ModuleID}] Failed to register smelting recipes: {ex}");
            }
        }

        private void RegisterBlastFurnaceRecipes()
        {
            try
            {
                // Optional: Add blast furnace recipes for higher-tier ores
                // These are in addition to regular smelting - player can use either

                var blastFurnaceConversions = new Dictionary<string, string>
                {
                    // Higher tier ores can also use blast furnace for faster smelting
                    { "UObloodrock_ore", "UObloodrock_ingot" },
                    { "UOvalorite_ore", "UOvalorite_ingot" },
                    { "UOblackrock_ore", "UOblackrock_ingot" }
                };

                foreach (var kvp in blastFurnaceConversions)
                {
                    try
                    {
                        var blastConfig = new Jotunn.Configs.SmelterConversionConfig
                        {
                            Station = "blastfurnace", // Using blast furnace for faster smelting
                            FromItem = kvp.Key,
                            ToItem = kvp.Value
                        };

                        var customConversion = new Jotunn.Entities.CustomItemConversion(blastConfig);
                        Jotunn.Managers.ItemManager.Instance.AddItemConversion(customConversion);

                        CoreAPI.Log.LogInfo($"[{ModuleID}] Added blast furnace recipe: {kvp.Key} -> {kvp.Value}");
                    }
                    catch (Exception ex)
                    {
                        CoreAPI.Log.LogWarning($"[{ModuleID}] Could not add blast furnace recipe for {kvp.Key}: {ex}");
                        // Not critical if blast furnace recipes fail - they're optional
                    }
                }
            }
            catch (Exception ex)
            {
                CoreAPI.Log.LogWarning($"[{ModuleID}] Failed to register blast furnace recipes: {ex}");
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
        /// OPTIMIZED: Early exit checks moved to front for performance.
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch(typeof(Destructible), nameof(Destructible.Damage))]
        private static void OnDestructibleDamaged_Postfix(Destructible __instance, HitData hit)
        {
            try
            {
                // OPTIMIZATION: Early exit checks ordered by likelihood

                // 1. Null checks first (fastest)
                if (__instance == null || __instance.gameObject == null || hit == null)
                    return;

                // 2. Server check (very fast)
                if (!ZNet.instance.IsServer())
                    return;

                // 3. Get attacker and check if it's a player (fast)
                var attacker = hit.GetAttacker();
                if (attacker == null)
                    return;

                Player player = attacker as Player;
                if (player == null)
                    return;

                // 4. OPTIMIZATION: Check pickaxe EARLY (filters out 90%+ of calls)
                if (!IsUsingPickaxe(player))
                    return;

                // 5. Finally check if it's a rock (only after pickaxe confirmed)
                string objectName = __instance.gameObject.name.ToLower();
                if (!objectName.Contains("rock") && !objectName.Contains("mine"))
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