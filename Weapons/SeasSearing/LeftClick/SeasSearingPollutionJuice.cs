using CalamityMod.Buffs.StatDebuffs;
using CalamityMod.Dusts.WaterSplash;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SeasSearing
{
    // A physical glob of fluorescent waste. The projectile is invisible; sparks and liquid
    // particles form its body, copying the lively, granular silhouette of Slagfire.
    internal sealed class SeasSearingPollutionJuice : ModProjectile, ILocalizedModType
    {
        private static readonly Color OuterColor = new(76, 238, 72);
        private static readonly Color InnerColor = new(205, 255, 142);

        private bool impactEffectsPlayed;

        public new string LocalizationCategory => "Projectiles.SeasSearing";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetDefaults()
        {
            Projectile.width = 18;
            Projectile.height = 18;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 240;
            Projectile.extraUpdates = 1;
            Projectile.tileCollide = true;
            Projectile.ignoreWater = true;
            Projectile.usesIDStaticNPCImmunity = true;
            Projectile.idStaticNPCHitCooldown = 8;
            Projectile.ArmorPenetration = 10;
        }

        public override void AI()
        {
            Projectile.localAI[0]++;
            Projectile.rotation += MathF.Sign(Projectile.velocity.X) * 0.08f;

            // A short straight spray followed by a pronounced liquid arc.
            if (Projectile.localAI[0] >= 7f)
                Projectile.velocity.Y = Math.Min(Projectile.velocity.Y + 0.16f, 16f);

            Projectile.velocity.X *= 0.997f;
            Lighting.AddLight(Projectile.Center, SeasSearingPalette.BiohazardLime.ToVector3() * 0.32f);

            if (Main.dedServ || Projectile.localAI[0] <= 2f)
                return;

            Vector2 backward = -Projectile.velocity * Main.rand.NextFloat(0.008f, 0.025f);
            for (int i = 0; i < 2; i++)
            {
                Vector2 position = Projectile.Center - Projectile.velocity * (i * 0.28f);
                Color color = i == 0 ? OuterColor : InnerColor;
                GeneralParticleHandler.SpawnParticle(new CustomSpark(
                    position,
                    backward,
                    "CalamityMod/Particles/BloomLineFade",
                    false,
                    7,
                    i == 0 ? 0.035f : 0.022f,
                    color * (i == 0 ? 0.85f : 0.7f),
                    new Vector2(0.5f, 0.95f),
                    shrinkSpeed: 0.38f));
            }

            if (Main.rand.NextBool(16))
            {
                Vector2 dripPosition = Projectile.Bottom + Main.rand.NextVector2Circular(4f, 2f);
                Vector2 dripVelocity = new(Main.rand.NextFloat(-0.8f, 0.8f), Main.rand.NextFloat(1.2f, 3f));
                GeneralParticleHandler.SpawnParticle(new WaterFlavoredParticle(
                    dripPosition,
                    dripVelocity,
                    true,
                    Main.rand.Next(24, 40),
                    Main.rand.NextFloat(0.28f, 0.48f),
                    Color.Lerp(OuterColor, InnerColor, Main.rand.NextFloat(0.15f, 0.65f))));
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.GetGlobalNPC<SeasSearingPollutionNPC>().ApplyPollution(target, Projectile.owner, 3, 8 * 60, fromSpread: true);
            target.AddBuff(BuffID.Venom, 150);
            target.AddBuff(ModContent.BuffType<Irradiated>(), 150);
            SpawnLiquidSplash(Projectile.velocity.SafeNormalize(Vector2.UnitY));
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            SpawnLiquidSplash(oldVelocity.SafeNormalize(Vector2.UnitY));
            Projectile.Kill();
            return false;
        }

        public override void OnKill(int timeLeft)
        {
            if (!impactEffectsPlayed)
                SpawnLiquidSplash(Projectile.velocity.SafeNormalize(Vector2.UnitY));
        }

        private void SpawnLiquidSplash(Vector2 impactDirection)
        {
            if (impactEffectsPlayed)
                return;

            impactEffectsPlayed = true;
            if (Main.dedServ)
                return;

            Vector2 burstDirection = -impactDirection.SafeNormalize(-Vector2.UnitY);
            for (int i = 0; i < 5; i++)
            {
                Vector2 velocity = burstDirection.RotatedByRandom(MathHelper.ToRadians(68f)) * Main.rand.NextFloat(3.5f, 8.5f);
                float scale = Main.rand.NextFloat(0.28f, 0.58f);

                GeneralParticleHandler.SpawnParticle(new WaterFlavoredParticle(
                    Projectile.Center,
                    velocity,
                    true,
                    Main.rand.Next(18, 34),
                    scale * 1.15f,
                    OuterColor));
                GeneralParticleHandler.SpawnParticle(new WaterFlavoredParticle(
                    Projectile.Center,
                    velocity,
                    true,
                    Main.rand.Next(18, 34),
                    scale * 0.68f,
                    InnerColor));
            }

            SeasSearingVisualUtility.SpawnPressureRing(Projectile.Center, 1.1f, 4f, 7, SeasSearingPalette.BiohazardLime);
            SoundEngine.PlaySound(SoundID.SplashWeak with
            {
                Volume = 0.25f,
                Pitch = 0.35f,
                PitchVariance = 0.16f,
                MaxInstances = 8
            }, Projectile.Center);
        }

        public static void SpawnCone(
            IEntitySource source,
            Vector2 position,
            Vector2 direction,
            int count,
            float minSpeed,
            float maxSpeed,
            float spread,
            int damage,
            float knockback,
            int owner)
        {
            direction = direction.SafeNormalize(Vector2.UnitX);
            for (int i = 0; i < count; i++)
            {
                Vector2 velocity = direction.RotatedByRandom(spread) * Main.rand.NextFloat(minSpeed, maxSpeed);
                velocity.Y -= Main.rand.NextFloat(0.4f, 1.8f);
                Spawn(source, position + Main.rand.NextVector2Circular(3f, 3f), velocity, damage, knockback, owner);
            }
        }

        public static void SpawnRadial(
            IEntitySource source,
            Vector2 position,
            int count,
            float minSpeed,
            float maxSpeed,
            int damage,
            float knockback,
            int owner)
        {
            float offset = Main.rand.NextFloat(MathHelper.TwoPi);
            for (int i = 0; i < count; i++)
            {
                float angle = offset + MathHelper.TwoPi * i / count + Main.rand.NextFloat(-0.22f, 0.22f);
                Vector2 velocity = angle.ToRotationVector2() * Main.rand.NextFloat(minSpeed, maxSpeed);
                velocity.Y -= Main.rand.NextFloat(1f, 3.5f);
                Spawn(source, position + Main.rand.NextVector2Circular(4f, 4f), velocity, damage, knockback, owner);
            }
        }

        private static void Spawn(IEntitySource source, Vector2 position, Vector2 velocity, int damage, float knockback, int owner)
        {
            int index = Projectile.NewProjectile(
                source,
                position,
                velocity,
                ModContent.ProjectileType<SeasSearingPollutionJuice>(),
                damage,
                knockback,
                owner);

            if (Main.projectile.IndexInRange(index))
                Main.projectile[index].scale = Main.rand.NextFloat(0.82f, 1.18f);
        }
    }
}
