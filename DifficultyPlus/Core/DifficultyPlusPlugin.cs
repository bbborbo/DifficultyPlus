using BepInEx;
using BepInEx.Configuration;
using DifficultyPlus.CoreModules;
using DifficultyPlus.Equipment;
using DifficultyPlus.Items;
using DifficultyPlus.Scavengers;
using R2API;
using R2API.Utils;
using RoR2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security;
using System.Security.Permissions;
using UnityEngine;

#pragma warning disable CS0618 // Type or member is obsolete
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
#pragma warning restore CS0618 // Type or member is obsolete
[module: UnverifiableCode]
#pragma warning disable 

namespace DifficultyPlus
{
    [BepInDependency("com.bepis.r2api", BepInDependency.DependencyFlags.HardDependency)]

    [BepInDependency("com.Borbo.HuntressBuffULTIMATE", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("com.Borbo.SpeedyBeetles", BepInDependency.DependencyFlags.HardDependency)]

    [NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.EveryoneNeedSameModVersion)]
    [BepInPlugin(guid, modName, version)]
    [R2APISubmoduleDependency(nameof(LanguageAPI), nameof(PrefabAPI), nameof(DirectorAPI),
        nameof(ItemAPI), nameof(RecalculateStatsAPI), nameof(EliteAPI))]
    internal partial class DifficultyPlusPlugin : BaseUnityPlugin
    {
        public const string guid = "com.Borbo." + modName;
        public const string modName = "DifficultyPLUS";
        public const string version = "0.1.0";

        public static AssetBundle assetBundle = Tools.LoadAssetBundle(Properties.Resources.difficultyplusbundle);
        public static string assetsPath = "Assets/BorboItemIcons/";
        public static string modelsPath = "Assets/Models/Prefabs/";
        public static string iconsPath = "Assets/Textures/Icons/";
        public static bool isAELoaded = Tools.isLoaded("com.Borbo.ArtificerExtended");
        public static bool isDSTLoaded = Tools.isLoaded("com.Borbo.DuckSurvivorTweaks");
        public static bool isHBULoaded = Tools.isLoaded("com.Borbo.HuntressBuffULTIMATE");

        internal static ConfigFile CustomConfigFile { get; set; }

        public static string drizzleDesc = $"Simplifies difficulty for players new to the game. Weeping and gnashing is replaced by laughter and tickles." +
                $"<style=cStack>\n\n>Player Health Regeneration: <style=cIsHealing>+50%</style> " +
                $"\n>Difficulty Scaling: <style=cIsHealing>-50%</style> " +
                $"\n>Player Damage Reduction: <style=cIsHealing>+38%</style>";
        public static string rainstormDesc = $"This is the way the game is meant to be played! Test your abilities and skills against formidable foes." +
                $"<style=cStack>\n\n>Player Health Regeneration: +0% " +
                $"\n>Difficulty Scaling: +0% ";
        public static string monsoonDesc = $"For hardcore players. Every bend introduces pain and horrors of the planet. You will die." +
                $"<style=cStack>\n\n>Player Health Regeneration: <style=cIsHealth>-40%</style> " +
                $"\n>Difficulty Scaling: <style=cIsHealth>+50%</style>";

        void Awake()
        {
            InitializeConfig();
            InitializeItems();
            //InitializeEquipment();
            //InitializeEliteEquipment();
            //InitializeScavengers();

            RoR2Application.onLoad += InitializeEverything;

            InitializeCoreModules();
            new ContentPacks().Initialize();
        }

        private void InitializeEverything()
        {
            #region difficulty dependent difficulty
            //ambient level
            if (GetConfigBool(true, "Difficulty: Difficulty Dependent Ambient Difficulty Boost"))
            {
                AmbientLevelDifficulty();
                FixMoneyAndExpRewards(); //related to ambient difficulty boost
            }

            //elite stats
            if (GetConfigBool(true, "Elite: Elite Stats and Ocurrences"))
            {
                ChangeEliteStats();
            }

            //teleporter particle
            if (GetConfigBool(true, "Difficulty: Teleporter Particle Radius"))
            {
                DifficultyDependentTeleParticles();
            }

            //monsoon stat boost
            if (GetConfigBool(true, "Difficulty: Monsoon Stat Booster"))
            {
                //MonsoonStatBoost();
            }
            #endregion

            #region packets
            //economy

            // boss item drop
            if (GetConfigBool(true, "Boss: Boss Item Drops"))
            {
                BossesDropBossItems();
                TricornRework();
                DirectorAPI.InteractableActions += DeleteYellowPrinters;
            }

            //overloading elite
            if (GetConfigBool(true, "Elite: Overloading Elite Rework"))
            {
                OverloadingEliteChanges();
            }

            //blazing elite
            //BlazingEliteChanges();

            //newt shrine
            if (GetConfigBool(true, "Lunar: Newt Shrine"))
            {
                NerfBazaarStuff();
            }

            On.RoR2.Run.BeginStage += GetChestCostForStage;

            if (GetConfigBool(true, "Economy: Gold Gain and Chest Scaling"))
            {
                FixMoneyScaling();
            }

            //elite gold
            if (GetConfigBool(true, "Economy: Elite Gold Rewards"))
            {
                EliteGoldReward();
            }

            //printer
            if (GetConfigBool(true, "Economy: Printer"))
            {
                DirectorAPI.InteractableActions += PrinterOccurrenceHook;
            }

            //scrapper
            if (GetConfigBool(true, "Economy: Scrapper"))
            {
                DirectorAPI.InteractableActions += ScrapperOccurrenceHook;
            }

            //equipment barrels and shops
            if (GetConfigBool(true, "Economy: Equipment Barrel/Shop"))
            {
                DirectorAPI.InteractableActions += EquipBarrelOccurrenceHook;
            }

            //blood shrine
            if (GetConfigBool(true, "Economy: Blood Shrine"))
            {
                BloodShrineRewardRework();
            }
            #endregion


            LanguageAPI.Add("DIFFICULTY_EASY_DESCRIPTION", drizzleDesc + "</style>");
            // " + $"\n>Most Bosses have <style=cIsHealing>reduced skill sets</style>

            LanguageAPI.Add("DIFFICULTY_NORMAL_DESCRIPTION", rainstormDesc + "</style>");

            LanguageAPI.Add("DIFFICULTY_HARD_DESCRIPTION", monsoonDesc + "</style>");
        }

        private bool GetConfigBool(bool defaultValue, string entryName, string desc = "")
        {
            if (desc != "")
            {
                return CustomConfigFile.Bind<bool>("DifficultyPlus Packets - See README For Details.",
                    entryName + "Packet", defaultValue,
                    $"The changes in this Packet will be enabled if set to true.").Value;
            }
            return CustomConfigFile.Bind<bool>("DifficultyPlus Packets",
                entryName + " Packet", defaultValue,
                "(The following changes will be enabled if set to true) " + desc).Value;
        }

        private void InitializeConfig()
        {
            CustomConfigFile = new ConfigFile(Paths.ConfigPath + $"\\{modName}.cfg", true);
        }
        void InitializeCoreModules()
        {
            var CoreModuleTypes = Assembly.GetExecutingAssembly().GetTypes().Where(type => !type.IsAbstract && type.IsSubclassOf(typeof(CoreModule)));

            foreach (var coreModuleType in CoreModuleTypes)
            {
                CoreModule coreModule = (CoreModule)Activator.CreateInstance(coreModuleType);

                coreModule.Init();

                Debug.Log("Core Module: " + coreModule + " Initialized!");
            }
        }
        #region twisted scavs

        public List<TwistedScavengerBase> Scavs = new List<TwistedScavengerBase>();
        public static Dictionary<TwistedScavengerBase, bool> ScavStatusDictionary = new Dictionary<TwistedScavengerBase, bool>();
        private void InitializeScavengers()
        {
            var ScavTypes = Assembly.GetExecutingAssembly().GetTypes().Where(type => !type.IsAbstract && type.IsSubclassOf(typeof(TwistedScavengerBase)));

            foreach (var scavType in ScavTypes)
            {
                TwistedScavengerBase scav = (TwistedScavengerBase)System.Activator.CreateInstance(scavType);

                if (ValidateScav(scav, Scavs))
                {
                    scav.PopulateItemInfos(CustomConfigFile);
                    scav.Init(CustomConfigFile);
                }
                else
                {
                    Debug.Log("Scavenger: " + scav.ScavLangTokenName + " did not initialize!");
                }
            }
        }

        bool ValidateScav(TwistedScavengerBase scav, List<TwistedScavengerBase> scavList)
        {
            bool enabled = 
                CustomConfigFile.Bind<bool>("Twisted Scavengers", $"Enable Twisted Scavenger: {scav.ScavName} the {scav.ScavTitle}", true, "Should this scavenger appear in A Moment, Whole?").Value;

            //ItemStatusDictionary.Add(item, itemEnabled);

            if (enabled)
            {
                scavList.Add(scav);
            }
            return enabled;
        }
        #endregion

        #region items

        public List<ItemBase> Items = new List<ItemBase>();
        public static Dictionary<ItemBase, bool> ItemStatusDictionary = new Dictionary<ItemBase, bool>();

        void InitializeItems()
        {
            var ItemTypes = Assembly.GetExecutingAssembly().GetTypes().Where(type => !type.IsAbstract && type.IsSubclassOf(typeof(ItemBase)));

            foreach (var itemType in ItemTypes)
            {
                ItemBase item = (ItemBase)System.Activator.CreateInstance(itemType);
                if (!item.IsHidden)
                {
                    if (ValidateItem(item, Items))
                    {
                        item.Init(CustomConfigFile);
                    }
                    else
                    {
                        Debug.Log("Item: " + item.ItemName + " Did not initialize!");
                    }
                }
            }
        }

        bool ValidateItem(ItemBase item, List<ItemBase> itemList)
        {
            bool itemEnabled = item.Tier == ItemTier.NoTier;

            if (!itemEnabled)
            {
                itemEnabled = CustomConfigFile.Bind<bool>("Items", $"Enable Item: {item.ItemName}", true, "Should this item appear in runs?").Value;
            }

            //ItemStatusDictionary.Add(item, itemEnabled);

            if (itemEnabled)
            {
                itemList.Add(item);
            }
            return itemEnabled;
        }
        #endregion

        #region equips

        public static List<EquipmentDef> EliteEquipments = new List<EquipmentDef>();
        public static Dictionary<EliteEquipmentBase, bool> EliteEquipmentStatusDictionary = new Dictionary<EliteEquipmentBase, bool>();
        void InitializeEliteEquipment()
        {
            var EquipmentTypes = Assembly.GetExecutingAssembly().GetTypes().Where(type => !type.IsAbstract && type.IsSubclassOf(typeof(EliteEquipmentBase)));

            foreach (var equipmentType in EquipmentTypes)
            {
                EliteEquipmentBase equipment = (EliteEquipmentBase)System.Activator.CreateInstance(equipmentType);

                if (ValidateEliteEquipment(equipment))
                {
                    equipment.Init(Config);
                    EliteEquipments.Add(equipment.EliteEquipmentDef);
                }
            }
        }
        public bool ValidateEliteEquipment(EliteEquipmentBase equipment)
        {
            var itemEnabled =
                CustomConfigFile.Bind<bool>("Elite Aspects", $"Enable Aspect: {equipment.EliteEquipmentName} ({equipment.EliteModifier} Elite)" , true, "Should these elites appear in runs?").Value;

            EliteEquipmentStatusDictionary.Add(equipment, itemEnabled);
            return itemEnabled;
        }

        /* public List<EquipmentBase> Equipments = new List<EquipmentBase>();
         public static Dictionary<EquipmentBase, bool> EquipmentStatusDictionary = new Dictionary<EquipmentBase, bool>();
         void InitializeEquipment()
         {
             var EquipmentTypes = Assembly.GetExecutingAssembly().GetTypes().Where(type => !type.IsAbstract && type.IsSubclassOf(typeof(EquipmentBase)));

             foreach (var equipmentType in EquipmentTypes)
             {
                 EquipmentBase equipment = (EquipmentBase)System.Activator.CreateInstance(equipmentType);
                 if (equipment.IsHidden)
                     return;

                 if (ValidateEquipment(equipment, Equipments))
                 {
                     equipment.Init(Config);
                 }
             }
         }
         public bool ValidateEquipment(EquipmentBase equipment, List<EquipmentBase> equipmentList)
         {
             BalanceCategory category = equipment.Category;

             var itemEnabled = true;

             if (category != BalanceCategory.None && category != BalanceCategory.Count)
             {
                 itemEnabled = IsCategoryEnabled(category) &&
                 CustomConfigFile.Bind<bool>(category.ToString(), "Enable Equipment: " + equipment.EquipmentName, true, "Should this item appear in runs?").Value;
             }
             else
             {
                 Debug.Log($"{equipment.EquipmentName} equipment initializing into Balance Category: {category}!!");
             }

             EquipmentStatusDictionary.Add(equipment, itemEnabled);

             if (itemEnabled)
             {
                 equipmentList.Add(equipment);
             }
             return itemEnabled;
         }*/
        #endregion
    }
}
