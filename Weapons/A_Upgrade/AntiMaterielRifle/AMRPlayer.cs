using System;
using System.Collections.Generic;
using CalamityMod;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using CalamityLegendsComeBack.Weapons.A_Upgrade.AntiMaterielRifle.Proj;

namespace CalamityLegendsComeBack.Weapons.A_Upgrade.AntiMaterielRifle
{
    internal sealed class AMRPlayer : ModPlayer
    {
        private bool holdingWeapon;
        private bool scopeRequested;
        private float requestedScopeCompletion;

        private int calibrationTarget = -1;
        private int calibrationStacks;
        private int calibrationTimer;
        private bool nextOnyxRoundIsMarker = true;

        internal bool IsHoldingWeapon => holdingWeapon;

        public override void Initialize()
        {
            nextOnyxRoundIsMarker = true;
        }

        public override void ResetEffects()
        {
            holdingWeapon = false;
            scopeRequested = false;
            requestedScopeCompletion = 0f;
        }

        internal void SetHoldingWeapon() => holdingWeapon = true;

        internal void RequestScope(float completion, bool zoomEnabled)
        {
            if (!zoomEnabled)
                return;

            scopeRequested = true;
            requestedScopeCompletion = MathHelper.Clamp(completion, 0f, 1f);
            Player.scope = true;
        }

        public override void PostUpdate()
        {
            if (calibrationTimer > 0)
            {
                calibrationTimer--;
                if (calibrationTimer <= 0)
                    ResetCalibration();
            }

            // 当到达困难模式且手持步枪时，检索周围敌怪生成边缘准心
            if (Main.myPlayer == Player.whoAmI && holdingWeapon && AMRBalance.BullseyeUnlocked)
            {
                int bullseyeType = ModContent.ProjectileType<AMRBullseye>();
                List<int> targetedNPCs = new();

                foreach (Projectile p in Main.ActiveProjectiles)
                {
                    if (p.type == bullseyeType && p.owner == Player.whoAmI)
                    {
                        targetedNPCs.Add((int)p.ai[0]);
                    }
                }

                foreach (NPC target in Main.ActiveNPCs)
                {
                    if (target.friendly || target.lifeMax < 5 || targetedNPCs.Contains(target.whoAmI) || target.realLife >= 0 ||
                        target.dontTakeDamage || target.immortal || target.townNPC || NPCID.Sets.ActsLikeTownNPC[target.type] || NPCID.Sets.CountsAsCritter[target.type])
                        continue;

                    if (target.WithinRange(Player.Center, 2000f))
                    {
                        Projectile.NewProjectile(
                            Player.GetSource_FromThis(),
                            target.Center,
                            Vector2.Zero,
                            bullseyeType,
                            0,
                            0f,
                            Player.whoAmI,
                            target.whoAmI);
                    }
                }
            }

            if (!holdingWeapon && calibrationTimer <= 0)
                ResetCalibration();
        }

        public override void ModifyZoom(ref float zoom)
        {
            if (!scopeRequested || zoom < 0f)
                return;

            float completion = requestedScopeCompletion;
            float easedCompletion = completion * completion * (3f - 2f * completion);
            zoom *= easedCompletion;
        }

        public override void UpdateDead()
        {
            ResetCalibration();
        }

        internal float GetCalibrationMultiplier(int targetIndex)
        {
            if (!AMRBalance.CalibrationUnlocked || calibrationTarget != targetIndex || calibrationStacks < 2)
                return 1f;

            return 1.45f;
        }

        internal void RegisterCalibrationHit(int targetIndex)
        {
            if (!AMRBalance.CalibrationUnlocked)
                return;

            if (calibrationTarget == targetIndex)
            {
                if (calibrationStacks >= 2)
                    calibrationStacks = 0;
                else
                    calibrationStacks++;
            }
            else
            {
                calibrationTarget = targetIndex;
                calibrationStacks = 1;
            }

            calibrationTimer = 3 * 60;
        }

        internal void ResetCalibration()
        {
            calibrationTarget = -1;
            calibrationStacks = 0;
            calibrationTimer = 0;
        }

        internal bool ConsumeOnyxRoundType()
        {
            if (!AMRBalance.OnyxSequenceUnlocked)
                return false;

            bool marker = nextOnyxRoundIsMarker;
            nextOnyxRoundIsMarker = !nextOnyxRoundIsMarker;
            return marker;
        }
    }
}
