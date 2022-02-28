using BepInEx.Configuration;
using DifficultyPlus.CoreModules;
using R2API;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using static R2API.RecalculateStatsAPI;

namespace DifficultyPlus.Items
{
    class Mocha2 : ItemBase<Mocha2>
    {
        public static BuffDef mochaBuffActive;
        public static BuffDef mochaBuffInactive;
        public static Sprite mochaCustomSprite = Resources.Load<Sprite>("textures/bufficons/texmovespeedbufficon");

        public static int buffDuration = 90;

        public static float aspdBoostBase = 0.20f;
        public static float aspdBoostStack = 0.0f;
        public static float mspdBoostBase = 0.25f;
        public static float mspdBoostStack = 0.25f;
        public override string ItemName => "Mocha";

        public override string ItemLangTokenName => "BORBOREWORKEDCOFFEELOL";

        public override string ItemPickupDesc => "Gain a temporary speed boost after beginning a stage.";

        public override string ItemFullDescription => $"After entering any stage, increase " +
            $"<style=cIsDamage>attack speed</style> by <style=cIsDamage>{Tools.ConvertDecimal(aspdBoostBase)}</style> and " +
            $"<style=cIsDamage>movement speed</style> by <style=cIsDamage>{Tools.ConvertDecimal(mspdBoostBase)}</style> " +
            $"<style=cStack>(+{Tools.ConvertDecimal(mspdBoostStack)} per stack)</style>, " +
            $"and reduce <style=cIsUtility>skill cooldowns</style> by <style=cIsUtility>{Tools.ConvertDecimal(aspdBoostBase)}</style>. " +
            $"Lasts <style=cIsUtility>{buffDuration}</style> seconds.";

        public override string ItemLore => "This is Hopoo's job!";

        public override ItemTier Tier => ItemTier.Tier1;
        public override ItemTag[] ItemTags { get; set; } = new ItemTag[] { ItemTag.Utility };

        public override GameObject ItemModel => Resources.Load<GameObject>("prefabs/NullModel");

        public override Sprite ItemIcon => Resources.Load<Sprite>("textures/miscicons/texWIPIcon");

        public override ItemDisplayRuleDict CreateItemDisplayRules()
        {
            return null;
        }

        public override void Hooks()
        {
            On.RoR2.CharacterBody.OnInventoryChanged += AddItemBehavior;
            On.RoR2.CharacterBody.OnBuffFinalStackLost += MochaExpiredBuff;
            On.RoR2.CharacterBody.RecalculateStats += MochaCDR;
            GetStatCoefficients += MochaSpeed;
        }

        private void MochaExpiredBuff(On.RoR2.CharacterBody.orig_OnBuffFinalStackLost orig, CharacterBody self, BuffDef buffDef)
        {
            if (buffDef == mochaBuffActive)
            {
                self.AddBuff(mochaBuffInactive);
            }
            orig(self, buffDef);
        }

        private void AddItemBehavior(On.RoR2.CharacterBody.orig_OnInventoryChanged orig, RoR2.CharacterBody self)
        {
            orig(self);
            if (NetworkServer.active)
            {
                if (self.master)
                {
                    BorboMochaBehavior mochaBehavior = self.AddItemBehavior<BorboMochaBehavior>(GetCount(self));
                }
            }
        }

        private void MochaSpeed(CharacterBody sender, StatHookEventArgs args)
        {
            int mochaCount = GetCount(sender);
            if(mochaCount > 0 && sender.HasBuff(mochaBuffActive))
            {
                args.moveSpeedMultAdd += mspdBoostBase + mspdBoostStack * (mochaCount - 1);

                float aspdBoost = aspdBoostBase + aspdBoostStack * (mochaCount - 1);
                args.attackSpeedMultAdd += aspdBoost;
            }
        }

        private void MochaCDR(On.RoR2.CharacterBody.orig_RecalculateStats orig, CharacterBody self)
        {
            orig(self);
            int mochaCount = GetCount(self);
            if (mochaCount > 0 && self.HasBuff(mochaBuffActive))
            {
                //float cdrBoost = 1 / (1 + aspdBoostBase + aspdBoostStack * (mochaCount - 1));
                float cdrBoost = 1 - aspdBoostBase;
                if(mochaCount > 1)
                    cdrBoost = Mathf.Pow(1 - aspdBoostStack, mochaCount - 1);

                SkillLocator skillLocator = self.skillLocator;
                if (skillLocator != null)
                {
                    if (skillLocator.primary != null)
                        skillLocator.primary.cooldownScale *= cdrBoost;

                    if (skillLocator.secondary != null)
                        skillLocator.secondary.cooldownScale *= cdrBoost;

                    if (skillLocator.utility != null)
                        skillLocator.utility.cooldownScale *= cdrBoost;

                    if (skillLocator.special != null)
                        skillLocator.special.cooldownScale *= cdrBoost;
                }
            }
        }

        public override void Init(ConfigFile config)
        {
            CreateItem();
            CreateLang();
            CreateBuff();
            Hooks();
        }

        private void CreateBuff()
        {
            string baseName = "BorboMochaBoost";
            mochaBuffActive = ScriptableObject.CreateInstance<BuffDef>();
            {
                mochaBuffActive.name = baseName + "Active";
                mochaBuffActive.iconSprite = mochaCustomSprite;
                mochaBuffActive.buffColor = new Color(0.6f,0.3f,0.1f);
                mochaBuffActive.canStack = true;
                mochaBuffActive.isDebuff = false;
            }
            mochaBuffInactive = ScriptableObject.CreateInstance<BuffDef>();
            {
                mochaBuffInactive.name = baseName + "Active";
                mochaBuffInactive.iconSprite = mochaCustomSprite;
                mochaBuffInactive.buffColor = new Color(0.1f,0.1f,0.2f);
                mochaBuffInactive.canStack = false;
                mochaBuffInactive.isDebuff = false;
            };
            Assets.buffDefs.Add(mochaBuffActive);
            Assets.buffDefs.Add(mochaBuffInactive);
        }
    }

    public class BorboMochaBehavior : CharacterBody.ItemBehavior
    {
        float remainingTime;
        float durationPerBuff = 1; //in seconds

        private void Start()
        {
            remainingTime = Mocha2.buffDuration;

            float startingBuffCount = Mocha2.buffDuration / durationPerBuff;
            for (int i = 0; i < startingBuffCount; i++)
                AddMochaBuff((i + 1) * durationPerBuff);
        }

        public void AddMochaBuff(float duration)
        {
            body.AddTimedBuff(Mocha2.mochaBuffActive, duration);
        }
    }
}
