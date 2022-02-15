using BepInEx;
using BepInEx.Configuration;
using DifficultyPlus.CoreModules;
using DifficultyPlus.Equipment;
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
    [R2APISubmoduleDependency(nameof(LanguageAPI), nameof(BuffAPI), nameof(PrefabAPI),
        nameof(EffectAPI), nameof(ResourcesAPI), nameof(DirectorAPI),
        nameof(ItemAPI), nameof(RecalculateStatsAPI), nameof(EliteAPI))]
    internal partial class DifficultyPlusPlugin : BaseUnityPlugin
    {
        public const string guid = "com.Borbo." + modName;
        public const string modName = "DifficultyPLUS";
        public const string version = "0.1.0";

        public static AssetBundle assetBundle = Tools.LoadAssetBundle(Properties.Resources.difficultyplus);
        public static string assetsPath = "Assets/BorboItemIcons/";
        public static string modelsPath = "Assets/Models/Prefabs/";
        public static string iconsPath = "Assets/Textures/Icons/";
        public static bool isAELoaded = Tools.isLoaded("com.Borbo.ArtificerExtended");
        public static bool isDSTLoaded = Tools.isLoaded("com.Borbo.DuckSurvivorTweaks");
        public static bool isHBULoaded = Tools.isLoaded("com.Borbo.HuntressBuffULTIMATE");

        internal static ConfigFile CustomConfigFile { get; set; }

        void Awake()
        {
            InitializeConfig();
            //InitializeItems();
            //InitializeEquipment();
            InitializeEliteEquipment();
            //InitializeScavengers();

            ChangeElites();
            ChangeEliteBehavior();
            DifficultyDependentChanges();
            FixMoneyAndExpRewards();

            LanguageAPI.Add("DIFFICULTY_EASY_DESCRIPTION", $"Simplifies difficulty for players new to the game. Weeping and gnashing is replaced by laughter and tickles." +
                $"<style=cStack>\n\n>Player Health Regeneration: <style=cIsHealing>+50%</style> " +
                $"\n>Difficulty Scaling: <style=cIsHealing>-50%</style> " +
                $"\n>Teleporter Visuals: <style=cIsHealing>+{Tools.ConvertDecimal(easyTeleParticleRadius / normalTeleParticleRadius - 1)}</style> " +
                $"\n>{Tier2EliteName} Elites appear starting on <style=cIsHealing>Stage {Tier2EliteMinimumStageDrizzle + 1}</style> " +
                $"\n>Player Damage Reduction: <style=cIsHealing>+38%</style></style>");
            // " + $"\n>Most Bosses have <style=cIsHealing>reduced skill sets</style>

            LanguageAPI.Add("DIFFICULTY_NORMAL_DESCRIPTION", $"This is the way the game is meant to be played! Test your abilities and skills against formidable foes." +
                $"<style=cStack>\n\n>Player Health Regeneration: +0% " +
                $"\n>Difficulty Scaling: +0% " +
                $"\n>Teleporter Visuals: +0% " +
                $"\n>{Tier2EliteName} Elites appear starting on Stage {Tier2EliteMinimumStageRainstorm + 1}</style></style>");

            LanguageAPI.Add("DIFFICULTY_HARD_DESCRIPTION", $"For hardcore players. Every bend introduces pain and horrors of the planet. You will die." +
                $"<style=cStack>\n\n>Player Health Regeneration: <style=cIsHealth>-40%</style> " +
                $"\n>Difficulty Scaling: <style=cIsHealth>+50%</style>" +
                $"\n>Teleporter Visuals: <style=cIsHealth>{Tools.ConvertDecimal(1 - hardTeleParticleRadius / normalTeleParticleRadius)}</style> " +
                $"\n>{Tier2EliteName} Elites appear starting on <style=cIsHealth>Stage {Tier2EliteMinimumStageMonsoon + 1}</style>" +
                $"\n>Most Enemies have <style=cIsHealth>unique scaling</style></style>");

            InitializeCoreModules();
            new ContentPacks().Initialize();
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

        /*public List<TwistedScavengerBase> Scavs = new List<TwistedScavengerBase>();
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
            BalanceCategory category = scav.Category;

            bool enabled = true;

            if (category != BalanceCategory.None && category != BalanceCategory.Count)
            {
                string name = scav.ScavName.Replace("'", "");
                enabled = IsCategoryEnabled(category) &&
                CustomConfigFile.Bind<bool>(category.ToString(), $"Enable Twisted Scavenger: {name}", true, "Should this scavenger appear in A Moment, Whole?").Value;
            }
            else
            {
                Debug.Log($"{scav.ScavLangTokenName} initializing into Balance Category: {category}!!");
            }

            //ItemStatusDictionary.Add(item, itemEnabled);

            if (enabled)
            {
                scavList.Add(scav);
            }
            return enabled;
        }*/
        #endregion

        #region items

        /*public List<ItemBase> Items = new List<ItemBase>();
        public static Dictionary<ItemBase, bool> ItemStatusDictionary = new Dictionary<ItemBase, bool>();

        void InitializeItems()
        {
            var ItemTypes = Assembly.GetExecutingAssembly().GetTypes().Where(type => !type.IsAbstract && type.IsSubclassOf(typeof(ItemBase)));

            foreach (var itemType in ItemTypes)
            {
                ItemBase item = (ItemBase)System.Activator.CreateInstance(itemType);
                if (item.IsHidden)
                    return;

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

        bool ValidateItem(ItemBase item, List<ItemBase> itemList)
        {
            BalanceCategory category = item.Category;

            var itemEnabled = true;

            if (category != BalanceCategory.None && category != BalanceCategory.Count)
            {
                string name = item.ItemName.Replace("'", "");
                itemEnabled = IsCategoryEnabled(category) &&
                CustomConfigFile.Bind<bool>(category.ToString(), $"Enable Item: {name}", true, "Should this item appear in runs?").Value;
            }
            else
            {
                Debug.Log($"{item.ItemName} item initializing into Balance Category: {category}!!");
            }

            //ItemStatusDictionary.Add(item, itemEnabled);

            if (itemEnabled)
            {
                itemList.Add(item);
            }
            return itemEnabled;
        }*/
        #endregion

        #region equips

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
                CustomConfigFile.Bind<bool>("Equipment", "Enable Equipment: " + equipment.EliteEquipmentName, true, "Should this item appear in runs?").Value;

            EliteEquipmentStatusDictionary.Add(equipment, itemEnabled);
            return itemEnabled;
        }
        #endregion
    }
}
