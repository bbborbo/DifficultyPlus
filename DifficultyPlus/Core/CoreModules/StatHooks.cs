using Mono.Cecil.Cil;
using MonoMod.Cil;
using R2API.Utils;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace DifficultyPlus.CoreModules
{
    public class StatHooks : CoreModule
    {
        public class BorboStatHookEventArgs : EventArgs
        {
            /// <summary>Added to the direct multiplier to attack speed. ATTACK_SPEED ~ (BASE_ATTACK_SPEED + baseAttackSpeedAdd) * (ATTACK_SPEED_MULT + attackSpeedMultAdd).</summary>
            public float attackSpeedMultAdd = 1f;
            public float attackSpeedDivAdd = 1f;

            /// <summary>Added to the direct multiplier to crit damage. CRIT_DAMAGE ~ DAMAGE * (2 + critDamageAdd) * (critDamageMul).</summary>
            public float critDamageMultAdd = 0f;
        }

        public override void Init()
        {
            IL.RoR2.HealthComponent.TakeDamage += TakeDamage_CritDamage;

            On.RoR2.CharacterBody.RecalculateStats += AttackSpeedBs;
            On.RoR2.GlobalEventManager.OnHitEnemy += GlobalEventManager_OnHitEnemy;
        }

        private void AttackSpeedBs(On.RoR2.CharacterBody.orig_RecalculateStats orig, CharacterBody self)
        {
            orig(self);
            BorboStatHookEventArgs attackerStatMods = new BorboStatHookEventArgs();
            BorboStatCoefficients?.Invoke(self, attackerStatMods);

            float attackSpeedModifier = attackerStatMods.attackSpeedMultAdd / attackerStatMods.attackSpeedDivAdd;
            self.attackSpeed *= attackSpeedModifier;
        }

        /// <summary>
        /// Used as the delegate type for the GetStatCoefficients event.
        /// </summary>
        /// <param name="sender">The CharacterBody which RecalculateStats is being called for.</param>
        /// <param name="args">An instance of StatHookEventArgs, passed to each subscriber to this event in turn for modification.</param>
        public delegate void HitHookEventHandler(CharacterBody body, DamageInfo damageInfo, GameObject victim);

        /// <summary>
        /// Subscribe to this event to modify one of the stat hooks which TILER2.StatHooks covers (see StatHookEventArgs). Fired during CharacterBody.RecalculateStats.
        /// </summary>
        public static event HitHookEventHandler GetHitBehavior;
        private void GlobalEventManager_OnHitEnemy(On.RoR2.GlobalEventManager.orig_OnHitEnemy orig, GlobalEventManager self, DamageInfo damageInfo, GameObject victim)
        {
            if(damageInfo.attacker && damageInfo.procCoefficient > 0f)
            {
                CharacterBody attackerBody = damageInfo.attacker.GetComponent<CharacterBody>();
                if(attackerBody != null)
                {
                    CharacterMaster attackerMaster = attackerBody.master;
                    if(attackerMaster != null)
                    {
                        GetHitBehavior?.Invoke(attackerBody, damageInfo, victim);
                    }
                }
            }
            orig(self, damageInfo, victim);
        }

        /// <summary>
        /// Used as the delegate type for the GetStatCoefficients event.
        /// </summary>
        /// <param name="sender">The CharacterBody which RecalculateStats is being called for.</param>
        /// <param name="args">An instance of StatHookEventArgs, passed to each subscriber to this event in turn for modification.</param>
        public delegate void StatHookEventHandler(CharacterBody sender, BorboStatHookEventArgs args);

        /// <summary>
        /// Subscribe to this event to modify one of the stat hooks which TILER2.StatHooks covers (see StatHookEventArgs). Fired during CharacterBody.RecalculateStats.
        /// </summary>
        public static event StatHookEventHandler BorboStatCoefficients;

        private void TakeDamage_CritDamage(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            BorboStatHookEventArgs attackerStatMods = null;
            c.Emit(OpCodes.Ldarg_1); //arg 0 is HC, arg 1 is DI
            c.EmitDelegate<Action<DamageInfo>>((di) =>
            {
                if(di.attacker != null)
                {
                    CharacterBody cb = di.attacker.GetComponent<CharacterBody>();
                    if (cb)
                    {
                        attackerStatMods = new BorboStatHookEventArgs();
                        BorboStatCoefficients?.Invoke(cb, attackerStatMods);
                    }
                }
            });

            c.GotoNext(MoveType.After,
                x => x.MatchLdfld("RoR2.DamageInfo", "crit")
                );

            c.GotoNext(MoveType.After,
                x => x.MatchLdcR4(out _)
                );

            c.EmitDelegate<Func<float, float>>((critDamage) =>
            {
                float critMultiplierAdd = attackerStatMods.critDamageMultAdd;

                float finalCritDamageMultiplier = (critDamage + critMultiplierAdd);

                return finalCritDamageMultiplier;
            });
        }
    }
}
