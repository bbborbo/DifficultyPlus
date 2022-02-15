using BepInEx;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using R2API;
using RoR2;
using RoR2.Projectile;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace DifficultyPlus
{
    internal partial class DifficultyPlusPlugin : BaseUnityPlugin
    {
        public static float overloadingBombDamage = 1.5f; //0.5f

        public static int Tier2EliteMinimumStageDrizzle = 10;
        public static int Tier2EliteMinimumStageRainstorm = 5;
        public static int Tier2EliteMinimumStageMonsoon = 3;
        public static int Tier2EliteMinimumStageEclipse = 0;

        static string Tier2EliteName = "Tier 2";

        void ChangeElites()
        {
            CombatDirector.baseEliteHealthBoostCoefficient = 3f;
            CombatDirector.baseEliteDamageBoostCoefficient = 1.5f;
            On.RoR2.CombatDirector.Init += EliteTierChanges;
        }

        private void EliteTierChanges(On.RoR2.CombatDirector.orig_Init orig)
        {
            orig();

            foreach(CombatDirector.EliteTierDef etd in CombatDirector.eliteTiers)
            {
                EliteDef[] eliteTypes = new EliteDef[] { RoR2Content.Elites.Poison, RoR2Content.Elites.Haunted };

                if (etd.eliteTypes == eliteTypes)
                {
                    etd.healthBoostCoefficient = Mathf.Pow(CombatDirector.baseEliteHealthBoostCoefficient, 2);
                    etd.damageBoostCoefficient = 4.5f;

                    etd.isAvailable = (SpawnCard.EliteRules rules) =>
                    (Run.instance.stageClearCount > Tier2EliteMinimumStageDrizzle && rules == SpawnCard.EliteRules.Default && Run.instance.selectedDifficulty <= DifficultyIndex.Easy)
                    || (Run.instance.stageClearCount > Tier2EliteMinimumStageRainstorm && rules == SpawnCard.EliteRules.Default && Run.instance.selectedDifficulty == DifficultyIndex.Normal)
                    || (Run.instance.stageClearCount > Tier2EliteMinimumStageMonsoon && rules == SpawnCard.EliteRules.Default && Run.instance.selectedDifficulty == DifficultyIndex.Hard)
                    || (Run.instance.stageClearCount > Tier2EliteMinimumStageEclipse && rules == SpawnCard.EliteRules.Default && Run.instance.selectedDifficulty > DifficultyIndex.Hard);
                }
            }
        }

        void ChangeEliteBehavior()
        {
            #region overload
            //Debug.Log("Modifying Overloading Elite bombs!");
            GameObject overloadingBomb = Resources.Load<GameObject>("Prefabs/Projectiles/LightningStake");

            ProjectileStickOnImpact bombStick = overloadingBomb.GetComponent<ProjectileStickOnImpact>();
            bombStick.ignoreCharacters = true;
            bombStick.ignoreWorld = false;

            ProjectileImpactExplosion bombPie = overloadingBomb.GetComponent<ProjectileImpactExplosion>();
            bombPie.blastRadius = 9;
            bombPie.lifetime = 1.2f;

            On.RoR2.HealthComponent.TakeDamage += OverloadingKnockbackFix;
            IL.RoR2.GlobalEventManager.OnHitAll += OverloadingBombDamage;
            #endregion

            #region blazing
            On.RoR2.CharacterBody.UpdateFireTrail += BlazingFireTrailChanges;
            #endregion
        }

        public static float fireTrailDPS = 0.5f; //1.5f
        public static float fireTrailBaseRadius = 6f; //3f
        public static float fireTrailLifetime = 100f; //3f
        private void BlazingFireTrailChanges(On.RoR2.CharacterBody.orig_UpdateFireTrail orig, CharacterBody self)
        {
            orig(self);
            return;

            if (self.fireTrail)
            {
                self.fireTrail.radius = fireTrailBaseRadius * self.radius;
                self.fireTrail.damagePerSecond = self.damage * fireTrailDPS;
                //self.fireTrail.pointLifetime = fireTrailLifetime;
            }
        }

        private void OverloadingBombDamage(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            c.GotoNext(MoveType.After,
                x => x.MatchLdsfld("RoR2.RoR2Content/Buffs", "AffixBlue")
                );

            c.GotoNext(MoveType.Before,
                x => x.MatchCallOrCallvirt("RoR2.Util", nameof(RoR2.Util.OnHitProcDamage))
                );
            c.Index--;
            c.Remove();
            c.Emit(OpCodes.Ldc_R4, overloadingBombDamage);
        }

        private void OverloadingKnockbackFix(On.RoR2.HealthComponent.orig_TakeDamage orig, RoR2.HealthComponent self, RoR2.DamageInfo damageInfo)
        {
            if (damageInfo.attacker)
            {
                CharacterBody aBody = damageInfo.attacker.GetComponent<CharacterBody>();
                if (aBody)
                {
                    if (aBody.HasBuff(RoR2Content.Buffs.AffixBlue))
                    {
                        damageInfo.force *= 0.25f;
                    }
                }
            }
            orig(self, damageInfo);
        }
    }
}
