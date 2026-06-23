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

        public override void SetDefaults()
        {
            Projectile.width = 220;
            Projectile.height = 220;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 58;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.DamageType = DamageClass.MeleeNoSpeed;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 8;
        }

        public override bool ShouldUpdatePosition() => false;

        public override bool? CanDamage() => Time <= 12f || Projectile.timeLeft % 7 == 0;

        public override void AI()
        {
            Time++;
            if (Radius <= 0f)
                Radius = 130f;

            Projectile.Resize((int)(Radius * 2f), (int)(Radius * 2f));
            Projectile.Opacity = Utils.GetLerpValue(0f, 5f, Time, true) * Utils.GetLerpValue(0f, 18f, Projectile.timeLeft, true);
            Lighting.AddLight(Projectile.Center, CosmicDischargeCommon.DoGSpecialColor.ToVector3() * 0.55f * Projectile.Opacity);

            if (Time == 1f)
            {
                SoundEngine.PlaySound(new SoundStyle("CalamityMod/Sounds/Custom/DoGLaserWallBigAttack") { Volume = 0.48f, Pitch = 0.18f, MaxInstances = 3 }, Projectile.Center);
                CosmicDischargeCommon.SpawnRiftCrackProjectiles(Projectile.GetSource_FromThis(), Projectile.Center, Projectile.owner, 6, 3f, 8f, 14f, 22f);

                if (!Main.dedServ)
                {
                    CosmicDischargeCommon.SpawnDistortionBurst(Projectile.Center, 8, 4, 48f, 30f);
                    CosmicDischargeCommon.SpawnCustomPulse(Projectile.Center, CosmicDischargeCommon.DoGWhiteColor, 0.3f, 2.4f, "CalamityMod/Particles/PlasmaExplosion", 20);
                    GeneralParticleHandler.SpawnParticle(new DetailedExplosion(Projectile.Center, Vector2.Zero, CosmicDischargeCommon.DoGCyanColor, new Vector2(1.1f, 0.75f), 0f, 0.25f * 0.3f, 1.55f * 0.3f, 20));
                    GeneralParticleHandler.SpawnParticle(new DetailedExplosion(Projectile.Center, Vector2.Zero, CosmicDischargeCommon.DoGFuchsiaColor, new Vector2(0.75f, 1.1f), MathHelper.PiOver4, 0.25f * 0.3f, 1.35f * 0.3f, 18));
                    GeneralParticleHandler.SpawnParticle(new StrongBloom(Projectile.Center, Vector2.Zero, CosmicDischargeCommon.DoGWhiteColor, 1.8f * 0.3f, 20));
                }
            }

            if (!Main.dedServ && Main.rand.NextBool(2))
            {
                Vector2 direction = Main.rand.NextVector2CircularEdge(1f, 1f);
                GeneralParticleHandler.SpawnParticle(new StaticGlowLine(
                    Projectile.Center,
                    Projectile.Center + direction * Main.rand.NextFloat(60f, Radius),
                    direction * 0.4f,
                    14,
                    0.07f * 0.3f,
                    0.9f,
                    Main.rand.NextBool() ? CosmicDischargeCommon.DoGCyanColor : CosmicDischargeCommon.DoGFuchsiaColor));
                GeneralParticleHandler.SpawnParticle(new NanoParticle(
                    Projectile.Center + Main.rand.NextVector2Circular(Radius * 0.7f, Radius * 0.7f),
                    Main.rand.NextVector2Circular(2f, 2f),
                    CosmicDischargeCommon.DoGSpecialColor,
                    0.35f * 0.3f,
                    18,
                    emitsLight: true));
            }
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            Vector2 closest = Vector2.Clamp(targetHitbox.Center.ToVector2(), targetHitbox.TopLeft(), targetHitbox.BottomRight());
            float pulseRadius = Radius * MathHelper.Lerp(0.62f, 1f, Utils.GetLerpValue(0f, 12f, Time, true));
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
            Main.EntitySpriteDraw(bloom, drawPosition, null, CosmicDischargeCommon.Transparent(CosmicDischargeCommon.DoGFuchsiaColor) * 0.22f * Projectile.Opacity, 0f, bloomOrigin, scale * 1.35f * pulse, SpriteEffects.None);
            Main.EntitySpriteDraw(portal, drawPosition, null, Color.Black * 0.42f * Projectile.Opacity, rotation, portalOrigin, scale * 0.8f, SpriteEffects.None);
            Main.EntitySpriteDraw(portal, drawPosition, null, CosmicDischargeCommon.Transparent(CosmicDischargeCommon.DoGCyanColor) * 0.55f * Projectile.Opacity, rotation * 0.6f, portalOrigin, scale * 0.8f, SpriteEffects.None);
            Main.EntitySpriteDraw(portal, drawPosition, null, CosmicDischargeCommon.Transparent(CosmicDischargeCommon.DoGFuchsiaColor) * 0.55f * Projectile.Opacity, -rotation * 0.7f, portalOrigin, scale * 0.8f, SpriteEffects.None);
            Main.spriteBatch.SetBlendState(BlendState.AlphaBlend);
            return false;
        }
    }
}
