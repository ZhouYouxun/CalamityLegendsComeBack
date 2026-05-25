using System;
using CalamityLegendsComeBack.Accssory.TS;
using CalamityMod;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Dev.AzureThunder
{
    internal sealed class AzureThunderStrikeSequencer : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.AzureThunder";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private int TargetIndex => (int)Projectile.ai[0];
        private int StrikeCount => Math.Max(1, (int)Projectile.ai[1]);
        private int ConsumedCharge => Math.Max(0, (int)Projectile.ai[2] / 10);
        private bool HarmonyMode => (int)Projectile.ai[2] % 10 == 1;

        private int timer;
        private int strikesDone;
        private bool commandedSwords;
        private Vector2 focusPoint;

        public override void SetDefaults()
        {
            Projectile.width = 2;
            Projectile.height = 2;
            Projectile.friendly = false;
            Projectile.hostile = false;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 240;
        }

        public override bool? CanDamage() => false;
        public override bool ShouldUpdatePosition() => false;

        public override void AI()
        {
            timer++;

            NPC target = ResolveTarget();
            if (target == null)
            {
                Projectile.Kill();
                return;
            }

            focusPoint = target.Center;
            Projectile.Center = focusPoint;

            if (HarmonyMode && !commandedSwords)
            {
                AzureThunderSounds.PlayCommandPulse(focusPoint);
                CommandGroundSwordsToFocus();
                commandedSwords = true;
            }

            int firstStrikeFrame = HarmonyMode ? 26 : 12;
            int interval = 5;

            if (strikesDone < StrikeCount && timer >= firstStrikeFrame + strikesDone * interval)
            {
                PulseGroundSwords();
                SpawnStrike(target, strikesDone == StrikeCount - 1);
                strikesDone++;
            }

            if (strikesDone >= StrikeCount && timer > firstStrikeFrame + StrikeCount * interval + 20)
                Projectile.Kill();
        }

        private NPC ResolveTarget()
        {
            if (TargetIndex >= 0 && Main.npc.IndexInRange(TargetIndex))
            {
                NPC target = Main.npc[TargetIndex];
                if (target.active && target.CanBeChasedBy(Projectile))
                    return target;
            }

            return AzureThunderPlayer.FindNearestTarget(Projectile.Center, 1600f);
        }

        private void CommandGroundSwordsToFocus()
        {
            int groundType = ModContent.ProjectileType<AzureThunderGroundSword>();
            float maxDistance = AzureThunderAccessoryPlayer.GetGroundSwordEffectRadius(Main.player[Projectile.owner]);
            foreach (Projectile projectile in Main.ActiveProjectiles)
            {
                if (!projectile.active || projectile.owner != Projectile.owner || projectile.type != groundType)
                    continue;

                if (projectile.Distance(focusPoint) > maxDistance)
                    continue;

                if (projectile.ModProjectile is AzureThunderGroundSword sword)
                    sword.BeginDive(focusPoint + Main.rand.NextVector2Circular(60f, 60f), Math.Max(1, (int)(Projectile.damage * 0.9f)), Projectile.knockBack);
            }
        }

        private void PulseGroundSwords()
        {
            int groundType = ModContent.ProjectileType<AzureThunderGroundSword>();
            Player owner = Main.player[Projectile.owner];
            Vector2 pulseCenter = HarmonyMode ? focusPoint : owner.Center;
            float maxDistance = AzureThunderAccessoryPlayer.GetGroundSwordEffectRadius(owner);

            foreach (Projectile projectile in Main.ActiveProjectiles)
            {
                if (!projectile.active || projectile.owner != Projectile.owner || projectile.type != groundType)
                    continue;

                if (projectile.Distance(pulseCenter) > maxDistance)
                    continue;

                if (projectile.ModProjectile is AzureThunderGroundSword sword)
                    sword.PulseLightningOutline();
            }
        }

        private void SpawnStrike(NPC target, bool finalStrike)
        {
            float damageFactor;
            if (finalStrike)
                damageFactor = HarmonyMode ?
                    AzureThunderProgression.UltimateRightClickFinalDamageFactor + ConsumedCharge * AzureThunderProgression.UltimateRightClickChargeDamageBonus :
                    6f;
            else
                damageFactor = HarmonyMode ? 0.81f : 0.45f;

            Vector2 strikePoint = target.Center + Main.rand.NextVector2Circular(HarmonyMode ? 95f : 55f, HarmonyMode ? 55f : 35f);
            bool applyCrumbling = finalStrike &&
                !HarmonyMode &&
                AzureThunderProgression.DownedYharon &&
                AzureThunderPlayer.CountOwnedGroundSwords(Main.player[Projectile.owner]) >= AzureThunderGroundSword.MaxGroundSwords;

            AzureThunderPlayer.SpawnVerticalLightning(
                Projectile.GetSource_FromThis(),
                strikePoint,
                target,
                Math.Max(1, (int)(Projectile.damage * damageFactor)),
                Projectile.knockBack,
                Projectile.owner,
                gainCharge: false,
                applyStaticDischarge: finalStrike || HarmonyMode,
                big: finalStrike && HarmonyMode,
                ultimateEnergyGain: AzureThunderAccessoryPlayer.GetRightClickLightningEnergyGain(Main.player[Projectile.owner]),
                applyCrumbling: applyCrumbling);

            if (finalStrike && HarmonyMode)
            {
                for (int i = 0; i < 5; i++)
                {
                    AzureThunderPlayer.SpawnFlatLightning(
                        Projectile.GetSource_FromThis(),
                        strikePoint,
                        Vector2.UnitX.RotatedByRandom(MathHelper.TwoPi) * Main.rand.NextFloat(16f, 24f),
                        Math.Max(1, (int)(Projectile.damage * 0.45f)),
                        Projectile.knockBack,
                        Projectile.owner,
                        1.2f);
                }
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (!HarmonyMode || focusPoint == Vector2.Zero)
                return false;

            Texture2D circle = ModContent.Request<Texture2D>("CalamityLegendsComeBack/Texture/KsTexture/circle_04").Value;
            Vector2 drawPosition = focusPoint - Main.screenPosition;
            float opacity = Utils.GetLerpValue(0f, 22f, timer, true) * Utils.GetLerpValue(StrikeCount * 5 + 48f, StrikeCount * 5 + 18f, timer, true);
            float scale = 1.6f + (float)Math.Sin(Main.GlobalTimeWrappedHourly * 5f) * 0.08f;

            Main.spriteBatch.EnterShaderRegion(BlendState.Additive);
            Main.EntitySpriteDraw(circle, drawPosition, null, AzureThunderColors.Azure with { A = 0 } * opacity * 0.45f, Main.GlobalTimeWrappedHourly * 0.7f, circle.Size() * 0.5f, scale, SpriteEffects.None);
            Main.EntitySpriteDraw(circle, drawPosition, null, AzureThunderColors.PaleYellow with { A = 0 } * opacity * 0.36f, -Main.GlobalTimeWrappedHourly * 0.55f, circle.Size() * 0.5f, scale * 0.78f, SpriteEffects.None);
            Main.spriteBatch.ExitShaderRegion();

            return false;
        }
    }
}
