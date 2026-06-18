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
    public class CosmicDischargeDoGRiftBomb : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.Melee";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private ref float DetonateDelay => ref Projectile.ai[0];
        private ref float Time => ref Projectile.ai[1];
        private bool detonated;

        public override void SetDefaults()
        {
            Projectile.width = 28;
            Projectile.height = 28;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 80;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.DamageType = DamageClass.MeleeNoSpeed;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }

        public override bool ShouldUpdatePosition() => !detonated;

        public override bool? CanDamage() => detonated && Time <= DetonateDelay + 3f;

        public override void AI()
        {
            if (DetonateDelay <= 0f)
                DetonateDelay = 22f;

            Time++;
            Projectile.velocity *= 0.94f;
            Projectile.rotation += 0.08f;
            Lighting.AddLight(Projectile.Center, CosmicDischargeCommon.DoGSpecialColor.ToVector3() * 0.3f);

            if (!detonated && Time >= DetonateDelay)
                Detonate();

            if (detonated)
            {
                Projectile.velocity = Vector2.Zero;
                Projectile.Opacity = Utils.GetLerpValue(DetonateDelay + 15f, DetonateDelay, Time, true);
                if (Time >= DetonateDelay + 16f)
                    Projectile.Kill();
            }
            else if (!Main.dedServ && Main.rand.NextBool(3))
            {
                GeneralParticleHandler.SpawnParticle(new GlowSquareParticle(
                    Projectile.Center + Main.rand.NextVector2Circular(10f, 10f),
                    Main.rand.NextVector2Circular(1.4f, 1.4f),
                    false,
                    12,
                    Main.rand.NextFloat(0.05f, 0.1f),
                    CosmicDischargeCommon.ThreeColorSpark,
                    rotation: Main.rand.NextFloat(0.05f, 0.12f)));
                GeneralParticleHandler.SpawnParticle(new ElectricSpark(
                    Projectile.Center + Main.rand.NextVector2Circular(12f, 12f),
                    Projectile.velocity.RotatedByRandom(0.7f) * -0.35f,
                    CosmicDischargeCommon.DoGCyanColor,
                    CosmicDischargeCommon.DoGFuchsiaColor,
                    0.45f,
                    10,
                    MathHelper.PiOver4,
                    5f));
            }
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            if (!detonated)
                return false;

            Vector2 closest = Vector2.Clamp(targetHitbox.Center.ToVector2(), targetHitbox.TopLeft(), targetHitbox.BottomRight());
            return Vector2.DistanceSquared(closest, Projectile.Center) <= 92f * 92f;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            CosmicDischargeCommon.ApplyDoGDebuffs(target, 240);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Vector2 origin = bloom.Size() * 0.5f;

            Main.spriteBatch.SetBlendState(BlendState.Additive);

            if (!detonated)
            {
                float pulse = 0.7f + 0.18f * MathF.Sin(Time * 0.35f);
                Main.EntitySpriteDraw(
                    bloom,
                    Projectile.Center - Main.screenPosition,
                    null,
                    CosmicDischargeCommon.Transparent(CosmicDischargeCommon.DoGSpecialColor) * 0.32f,
                    Projectile.rotation,
                    origin,
                    0.18f * pulse,
                    SpriteEffects.None);
                Main.spriteBatch.SetBlendState(BlendState.AlphaBlend);
                return false;
            }

            float progress = Utils.GetLerpValue(DetonateDelay, DetonateDelay + 16f, Time, true);
            float fade = Utils.GetLerpValue(DetonateDelay + 16f, DetonateDelay + 4f, Time, true);
            Main.EntitySpriteDraw(
                bloom,
                Projectile.Center - Main.screenPosition,
                null,
                CosmicDischargeCommon.Transparent(CosmicDischargeCommon.DoGFuchsiaColor) * 0.3f * fade,
                0f,
                origin,
                MathHelper.Lerp(0.35f, 1.45f, progress),
                SpriteEffects.None);

            Main.spriteBatch.SetBlendState(BlendState.AlphaBlend);
            return false;
        }

        private void Detonate()
        {
            detonated = true;
            Projectile.Resize(184, 184);
            Projectile.Damage();
            SoundEngine.PlaySound(new SoundStyle("CalamityMod/Sounds/Custom/DevourerRiftOpen") { Volume = 0.42f, Pitch = 0.25f, MaxInstances = 4 }, Projectile.Center);
            ApplyScreenShake(4.4f);

            if (Main.dedServ)
                return;

            GeneralParticleHandler.SpawnParticle(new PulseRing(
                Projectile.Center,
                Vector2.Zero,
                CosmicDischargeCommon.Transparent(CosmicDischargeCommon.DoGSpecialColor) * 0.58f,
                0.05f,
                1.2f,
                18));
            GeneralParticleHandler.SpawnParticle(new StrongBloom(
                Projectile.Center,
                Vector2.Zero,
                CosmicDischargeCommon.Transparent(CosmicDischargeCommon.DoGFuchsiaColor) * 0.42f,
                0.5f,
                16));
            GeneralParticleHandler.SpawnParticle(new DetailedExplosion(
                Projectile.Center,
                Vector2.Zero,
                CosmicDischargeCommon.DoGCyanColor,
                new Vector2(1.2f, 0.8f),
                0f,
                0.15f,
                0.95f,
                16));
            GeneralParticleHandler.SpawnParticle(new DetailedExplosion(
                Projectile.Center,
                Vector2.Zero,
                CosmicDischargeCommon.DoGFuchsiaColor * 0.8f,
                new Vector2(0.8f, 1.25f),
                MathHelper.Pi / 3f,
                0.12f,
                0.78f,
                14));
            CosmicDischargeCommon.SpawnRiftCrackProjectiles(Projectile.GetSource_FromThis(), Projectile.Center, Projectile.owner, 5, 3f, 8f, 14f, 22f);
            CosmicDischargeCommon.SpawnDistortionBurst(Projectile.Center, 6, 3, 38f, 25f);

            for (int i = 0; i < 12; i++)
            {
                Vector2 velocity = (MathHelper.TwoPi * i / 12f).ToRotationVector2() * Main.rand.NextFloat(2.4f, 6.2f);
                GeneralParticleHandler.SpawnParticle(new GlowSparkParticle(
                    Projectile.Center + Main.rand.NextVector2Circular(8f, 8f),
                    velocity,
                    true,
                    Main.rand.Next(12, 20),
                    Main.rand.NextFloat(0.55f, 0.95f),
                    CosmicDischargeCommon.ThreeColorSpark,
                    new Vector2(0.25f, 1.7f),
                    true));
            }
        }

        private void ApplyScreenShake(float power)
        {
            if (Main.dedServ)
                return;

            float distanceFactor = Utils.GetLerpValue(1100f, 100f, Vector2.Distance(Main.LocalPlayer.Center, Projectile.Center), true);
            Main.LocalPlayer.Calamity().GeneralScreenShakePower = Math.Max(Main.LocalPlayer.Calamity().GeneralScreenShakePower, power * distanceFactor);
        }
    }
}
