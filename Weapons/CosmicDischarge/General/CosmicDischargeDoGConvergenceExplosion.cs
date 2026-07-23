using System;
using System.Collections.Generic;
using CalamityMod;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.CosmicDischarge
{
    public class CosmicDischargeDoGConvergenceExplosion : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.Melee";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private ref float Time => ref Projectile.ai[0];
        private ref float Radius => ref Projectile.ai[1];
        private bool IsUltimateBurst => Projectile.ai[2] == 1f;

        public override void SetDefaults()
        {
            Projectile.width = 220;
            Projectile.height = 220;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 120;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.DamageType = DamageClass.MeleeNoSpeed;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 6;
        }

        public override bool ShouldUpdatePosition() => false;

        public override bool? CanDamage() => IsUltimateBurst ? Time <= 2f : Time <= 16f || Projectile.timeLeft % 6 == 0;

        public override void AI()
        {
            Time++;
            if (Radius <= 0f)
                Radius = 145f;
            if (IsUltimateBurst && Time == 1f)
                Projectile.timeLeft = 54;

            Projectile.Resize((int)(Radius * 2f), (int)(Radius * 2f));
            // Fade in over 8 frames, fade out over last 30 frames of the 120-frame lifetime.
            Projectile.Opacity = Utils.GetLerpValue(0f, 8f, Time, true) * Utils.GetLerpValue(0f, 30f, Projectile.timeLeft, true);
            Lighting.AddLight(Projectile.Center, CosmicDischargeCommon.DoGSpecialColor.ToVector3() * 0.72f * Projectile.Opacity);

            // 起爆：一次 Heavy 档爆发 + DoG 原生裂缝弹幕 + 扭曲元球。
            // 观感主体来自下面 PreDraw 的传送门层，粒子只负责起爆那一瞬。
            if (Time == 1f)
            {
                SoundEngine.PlaySound(new SoundStyle("CalamityMod/Sounds/Custom/DoGLaserWallBigAttack") { Volume = 0.58f, Pitch = 0.18f, MaxInstances = 3 }, Projectile.Center);
                CosmicDischargeCommon.SpawnRiftCrackProjectiles(Projectile.GetSource_FromThis(), Projectile.Center, Projectile.owner, 8, 20f, Radius, 14f, 24f);
                CosmicDischargeCommon.SpawnDistortionBurst(Projectile.Center, 10, 28f, Radius * 0.3f);
                CosmicDischargeCommon.SpawnRiftBurst(Projectile.Center, RiftTier.Heavy, default, CosmicDischargeCommon.DoGSpecialColor);
            }

            // 持续期只保留呼吸感的低频脉冲：每 28 帧一记，不再逐帧撒碎片。
            if (!Main.dedServ && Time > 1f && Time % 28 == 0)
            {
                SoundEngine.PlaySound(new SoundStyle("CalamityMod/Sounds/Custom/DevourerAttack") { Volume = 0.30f, Pitch = Main.rand.NextFloat(-0.35f, 0.25f), MaxInstances = 6 }, Projectile.Center);

                GeneralParticleHandler.SpawnParticle(new PulseRing(
                    Projectile.Center,
                    Vector2.Zero,
                    CosmicDischargeCommon.Transparent(CosmicDischargeCommon.DoGSpecialColor) * 0.5f,
                    0.04f,
                    Radius / 130f,
                    20));

                for (int i = 0; i < 4; i++)
                    GeneralParticleHandler.SpawnParticle(new SquishyLightParticle(
                        Projectile.Center + Main.rand.NextVector2Circular(Radius * 0.5f, Radius * 0.5f),
                        Main.rand.NextVector2Unit() * Main.rand.NextFloat(4f, 7f),
                        Main.rand.NextFloat(0.6f, 0.85f),
                        CosmicDischargeCommon.RiftColor(),
                        Main.rand.Next(25, 35)));
            }
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            Vector2 closest = Vector2.Clamp(targetHitbox.Center.ToVector2(), targetHitbox.TopLeft(), targetHitbox.BottomRight());
            // Expand from 40% to full radius over first 18 frames for a dramatic initial shockwave.
            float pulseRadius = Radius * MathHelper.Lerp(0.4f, 1f, Utils.GetLerpValue(0f, 18f, Time, true));
            return Vector2.DistanceSquared(closest, Projectile.Center) <= pulseRadius * pulseRadius;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            CosmicDischargeCommon.ApplyDoGDebuffs(target, 300);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Texture2D portal = ModContent.Request<Texture2D>("CalamityMod/Projectiles/Melee/StreamGougePortal").Value;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            Vector2 bloomOrigin = bloom.Size() * 0.5f;
            Vector2 portalOrigin = portal.Size() * 0.5f;
            float pulse = 0.82f + 0.18f * MathF.Sin(Time * 0.55f);
            float scale = Radius / 110f * Projectile.Opacity;
            float rotation = Main.GlobalTimeWrappedHourly * 7.5f + Projectile.identity * 0.18f;

            Main.spriteBatch.SetBlendState(BlendState.Additive);
            Main.EntitySpriteDraw(bloom, drawPosition, null, CosmicDischargeCommon.Transparent(CosmicDischargeCommon.RiftMagenta) * 0.22f * Projectile.Opacity, 0f, bloomOrigin, scale * 1.35f * pulse, SpriteEffects.None);
            Main.EntitySpriteDraw(portal, drawPosition, null, Color.Black * 0.42f * Projectile.Opacity, rotation, portalOrigin, scale * 0.8f, SpriteEffects.None);
            Main.EntitySpriteDraw(portal, drawPosition, null, CosmicDischargeCommon.Transparent(CosmicDischargeCommon.RiftLightBlue) * 0.55f * Projectile.Opacity, rotation * 0.6f, portalOrigin, scale * 0.8f, SpriteEffects.None);
            Main.EntitySpriteDraw(portal, drawPosition, null, CosmicDischargeCommon.Transparent(CosmicDischargeCommon.RiftMagenta) * 0.55f * Projectile.Opacity, -rotation * 0.7f, portalOrigin, scale * 0.8f, SpriteEffects.None);
            Main.spriteBatch.SetBlendState(BlendState.AlphaBlend);
            return false;
        }
    }
}
