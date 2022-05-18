using Mono.Cecil.Cil;
using MonoMod.Cil;
using R2API;
using RoR2;
using RoR2.Skills;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace DifficultyPlus.CoreModules
{
    class Assets : CoreModule
    {
        public static EffectDef CreateEffect(GameObject effect)
        {
            if (effect == null)
            {
                Debug.LogError("Effect prefab was null");
                return null;
            }

            var effectComp = effect.GetComponent<EffectComponent>();
            if (effectComp == null)
            {
                Debug.LogErrorFormat("Effect prefab: \"{0}\" does not have an EffectComponent.", effect.name);
                return null;
            }

            var vfxAttrib = effect.GetComponent<VFXAttributes>();
            if (vfxAttrib == null)
            {
                Debug.LogErrorFormat("Effect prefab: \"{0}\" does not have a VFXAttributes component.", effect.name);
                return null;
            }

            var def = new EffectDef
            {
                prefab = effect,
                prefabEffectComponent = effectComp,
                prefabVfxAttributes = vfxAttrib,
                prefabName = effect.name,
                spawnSoundEventName = effectComp.soundName
            };

            effectDefs.Add(def);
            return def;
        }

        public static List<ArtifactDef> artifactDefs = new List<ArtifactDef>();
        public static List<BuffDef> buffDefs = new List<BuffDef>();
        public static List<EffectDef> effectDefs = new List<EffectDef>();
        public static List<SkillFamily> skillFamilies = new List<SkillFamily>();
        public static List<SkillDef> skillDefs = new List<SkillDef>();
        public static List<GameObject> projectilePrefabs = new List<GameObject>();
        public static List<GameObject> networkedObjectPrefabs = new List<GameObject>();
        public static List<Type> entityStates = new List<Type>();

        public static List<ItemDef> itemDefs = new List<ItemDef>();
        public static List<EquipmentDef> equipDefs = new List<EquipmentDef>();
        public static List<EliteDef> eliteDefs = new List<EliteDef>();

        public static List<GameObject> masterPrefabs = new List<GameObject>();
        public static List<GameObject> bodyPrefabs = new List<GameObject>();

        public override void Init()
        {
            AddTrophyHunterDebuffs();

            IL.RoR2.HealthComponent.TakeDamage += AddExecutionThreshold;
            On.RoR2.HealthComponent.GetHealthBarValues += DisplayExecutionThreshold;
        }

        public static BuffDef bossHunterDebuff;
        public static BuffDef bossHunterDebuffWithScalpel;
        private void AddTrophyHunterDebuffs()
        {
            bossHunterDebuff = ScriptableObject.CreateInstance<BuffDef>();

            bossHunterDebuff.buffColor = new Color(0.2f, 0.9f, 0.8f, 1);
            bossHunterDebuff.canStack = false;
            bossHunterDebuff.isDebuff = true;
            bossHunterDebuff.name = "TrophyHunterDebuff";
            bossHunterDebuff.iconSprite = LegacyResourcesAPI.Load<Sprite>("textures/bufficons/texBuffLunarDetonatorIcon");

            buffDefs.Add(bossHunterDebuff);

            bossHunterDebuffWithScalpel = ScriptableObject.CreateInstance<BuffDef>();

            bossHunterDebuffWithScalpel.buffColor = new Color(0.2f, 0.9f, 0.8f, 1);
            bossHunterDebuffWithScalpel.canStack = false;
            bossHunterDebuffWithScalpel.isDebuff = true;
            bossHunterDebuffWithScalpel.name = "TrophyHunterScalpelDebuff";
            bossHunterDebuffWithScalpel.iconSprite = LegacyResourcesAPI.Load<Sprite>("textures/bufficons/texBuffLunarDetonatorIcon");

            buffDefs.Add(bossHunterDebuffWithScalpel);
        }

        private void AddExecutionThreshold(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            int thresholdPosition = 0;

            c.GotoNext(MoveType.After,
                x => x.MatchLdcR4(float.NegativeInfinity),
                x => x.MatchStloc(out thresholdPosition)
                );
            
            c.GotoNext(MoveType.Before,
                x => x.MatchLdarg(0),
                x => x.MatchCallOrCallvirt<HealthComponent>("get_isInFrozenState")
                );

            c.Emit(OpCodes.Ldloc, thresholdPosition);
            c.Emit(OpCodes.Ldarg, 0);
            c.EmitDelegate<Func<float, HealthComponent, float>>((currentThreshold, hc) =>
            {
                float newThreshold = currentThreshold;

                newThreshold = GetExecutionThreshold(currentThreshold, hc);

                return newThreshold;
            });
            c.Emit(OpCodes.Stloc, thresholdPosition);
        }

        static float GetExecutionThreshold(float currentThreshold, HealthComponent healthComponent)
        {
            float newThreshold = currentThreshold;
            CharacterBody body = healthComponent.body;

            if (body != null)
            {
                if (!body.bodyFlags.HasFlag(CharacterBody.BodyFlags.ImmuneToExecutes))
                {
                    float threshold = 0;
                    if(currentThreshold < threshold)
                    {
                        newThreshold = threshold;
                    }
                }
            }

            return newThreshold;
        }

        private HealthComponent.HealthBarValues DisplayExecutionThreshold(On.RoR2.HealthComponent.orig_GetHealthBarValues orig, HealthComponent self)
        {
            HealthComponent.HealthBarValues values = orig(self);

            values.cullFraction = Mathf.Clamp01(GetExecutionThreshold(values.cullFraction, self));

            return values;
        }
    }
}
