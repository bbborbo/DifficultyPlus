using BepInEx;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using R2API.Utils;
using RoR2;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using static RoR2.GivePickupsOnStart;

namespace DifficultyPlus
{
    internal partial class DifficultyPlusPlugin : BaseUnityPlugin
    {
        #region State of Difficulty
        void FixMoneyAndExpRewards()
        {
            On.RoR2.DeathRewards.Awake += FixMoneyAndExpRewards;
        }

        private void FixMoneyAndExpRewards(On.RoR2.DeathRewards.orig_Awake orig, RoR2.DeathRewards self)
        {
            orig(self);
            float boost = GetAmbientLevelBoost();
            float ambientLevel = Run.instance.ambientLevel;
            float forgiveness = 0.85f;

            float actualLevelStat = 1 + (0.3f * ambientLevel);
            float intendedLevelStat = 1 + (0.3f * (ambientLevel - boost * forgiveness));
            float rewardMult = intendedLevelStat / actualLevelStat;

            self.goldReward = (uint)((float)self.expReward * rewardMult);
            self.expReward = (uint)((float)self.expReward * rewardMult);
        }
        #endregion
    }
}
