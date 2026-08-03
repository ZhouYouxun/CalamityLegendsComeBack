using CalamityMod.Buffs.StatDebuffs;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SeasSearing
{
    internal sealed class SeasSearingNuclearDetonation : ModProjectile, ILocalizedModType
    {
        // Keep the original nuke's three visual phases, but let the fallout zone occupy the battlefield five times longer.
        private const int PhaseScale = 5;
        private const int Lifetime = 150 * PhaseScale;
        private const int DamageEndFrames = 45 * PhaseScale;
        private const int RainStartFrames = 45 * PhaseScale;
        private const int RainInterval = 10;
        private const int FalloutMissilesPerWave = 3;
        private const int PollutionJuicePerWave = 2;
        private bool initialized;

        public new string LocalizationCategory => "Projectiles.SeasSearing";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetDefaults()
        {
            Projectile.width          = 1200;
            Projectile.height         = 1200;
            Projectile.friendly       = true;
            Projectile.DamageType     = DamageClass.Ranged;
            Projectile.penetrate      = -1;
            Projectile.timeLeft       = Lifetime;
            Projectile.tileCollide    = false;
            Projectile.ignoreWater    = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown  = 18;
            Projectile.netImportant         = true;
        }

        public override bool? CanDamage() => Projectile.timeLeft > DamageEndFrames;

        public override void AI()
        {
            if (!initialized)
                InitializeNuke();

            int age = Lifetime - Projectile.timeLeft;
            Lighting.AddLight(Projectile.Center, Color.Lerp(SeasSearingPalette.WarningOrange, SeasSearingPalette.RadioactiveCyan, MathHelper.Clamp(age / (80f * PhaseScale), 0f, 1f)).ToVector3() * 1.4f);

            if (age % 7 == 0)
                SpawnNuclearDust(age);

            if (Main.myPlayer == Projectile.owner && age > RainStartFrames && age % RainInterval == 0)
                SpawnFalloutRain();
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            int amount = target.boss || target.realLife >= 0 ? 92 : 55;
            target.GetGlobalNPC<SeasSearingPollutionNPC>().ApplyPollution(target, Projectile.owner, amount, 24 * 60);
            target.AddBuff(BuffID.Venom, 600);
            target.AddBuff(ModContent.BuffType<Irradiated>(), 720);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D bloom  = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Texture2D ring   = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomRing").Value;
            float age        = Lifetime - Projectile.timeLeft;
            float shock      = MathHelper.Clamp(age / (42f * PhaseScale), 0f, 1f);
            float collapse   = MathHelper.Clamp((age - 42f * PhaseScale) / (38f * PhaseScale), 0f, 1f);
            float fade       = MathHelper.Clamp(Projectile.timeLeft / (34f * PhaseScale), 0f, 1f);
            Vector2 center   = Projectile.Center - Main.screenPosition;

            Color white  = (Color.White  with { A = 0 }) * fade;
            Color cyan   = (SeasSearingPalette.RadioactiveCyan with { A = 0 }) * fade;
            Color deep   = (SeasSearingPalette.AbyssBlack      with { A = 0 }) * fade;

            float baseScale = 1200f / bloom.Width;
            Main.EntitySpriteDraw(bloom, center, null, white * (1f - shock) * 1.8f, 0f, bloom.Size() * 0.5f, baseScale * (0.8f + shock * 1.3f), SpriteEffects.None, 0);
            Main.EntitySpriteDraw(ring,  center, null, cyan  * (0.8f + shock * 0.6f), Main.GlobalTimeWrappedHourly * 1.2f, ring.Size() * 0.5f, 1.1f + shock * 7.5f, SpriteEffects.None, 0);

            if (collapse > 0f)
            {
                Main.EntitySpriteDraw(bloom, center, null, deep  * collapse * 1.25f, 0f, bloom.Size() * 0.5f, baseScale * (0.32f + collapse * 0.28f), SpriteEffects.None, 0);
                Main.EntitySpriteDraw(ring,  center, null, cyan  * collapse * 0.9f, -Main.GlobalTimeWrappedHourly * 3.6f, ring.Size() * 0.5f, 0.9f + collapse * 1.7f, SpriteEffects.None, 0);
            }

            return false;
        }

        private void InitializeNuke()
        {
            initialized = true;
            Vector2 center        = Projectile.Center;
            Projectile.width      = Projectile.height = 1200;
            Projectile.Center     = center;
            SeasSearingVisualUtility.ShakeAt(center, 22f, 3200f);
            SeasSearingVisualUtility.SpawnAbyssDust(center, 150, 18f, 90f, 1.8f);
            SeasSearingVisualUtility.SpawnPressureRing(center, 12f, 24f, 90, Color.White);
            SoundEngine.PlaySound(SoundID.Item14 with { Volume = 1f,    Pitch = -0.65f }, center);
            SoundEngine.PlaySound(SoundID.Item62 with { Volume = 0.95f, Pitch = -0.35f }, center);
        }

        private void SpawnNuclearDust(int age)
        {
            int   count  = age < 45 * PhaseScale ? 18 : 11;
            float radius = age < 45 * PhaseScale ? 620f : 760f;

            for (int i = 0; i < count; i++)
            {
                Vector2 offset   = Main.rand.NextVector2CircularEdge(radius, radius * Main.rand.NextFloat(0.45f, 1f));
                Vector2 velocity = offset.SafeNormalize(Vector2.UnitY) * Main.rand.NextFloat(2.2f, 7.5f);
                if (age > 50 * PhaseScale) velocity = -Vector2.UnitY.RotatedByRandom(0.65f) * Main.rand.NextFloat(0.8f, 3.6f);

                Dust dust = Dust.NewDustPerfect(
                    Projectile.Center + offset * Main.rand.NextFloat(0.12f, 0.9f),
                    Main.rand.NextBool(3) ? DustID.Smoke : DustID.GemEmerald,
                    velocity, 140,
                    Main.rand.NextBool(4) ? SeasSearingPalette.WarningOrange : SeasSearingPalette.PollutionColor(Main.rand.NextFloat()),
                    Main.rand.NextFloat(0.8f, 1.8f));
                dust.noGravity = true;
            }
        }

        private void SpawnFalloutRain()
        {
            // Triple the original fallout-missile density, then interleave lower-damage liquid rain.
            for (int i = 0; i < FalloutMissilesPerWave; i++)
            {
                Vector2 spawn = Projectile.Center + new Vector2(Main.rand.NextFloat(-760f, 760f), Main.rand.NextFloat(-780f, -650f));
                Vector2 velocity = new(Main.rand.NextFloat(-1.8f, 1.8f), Main.rand.NextFloat(9f, 16f));
                Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(), spawn, velocity,
                    ModContent.ProjectileType<SeasSearingFalloutRain>(),
                    Math.Max(1, Projectile.damage / 9), 1.5f, Projectile.owner);
            }

            for (int i = 0; i < PollutionJuicePerWave; i++)
            {
                Vector2 spawn = Projectile.Center + new Vector2(Main.rand.NextFloat(-800f, 800f), Main.rand.NextFloat(-760f, -620f));
                Vector2 velocity = new(Main.rand.NextFloat(-2.8f, 2.8f), Main.rand.NextFloat(10f, 15f));
                int index = Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(), spawn, velocity,
                    ModContent.ProjectileType<SeasSearingPollutionJuice>(),
                    Math.Max(1, Projectile.damage / 14), 0.75f, Projectile.owner);

                if (Main.projectile.IndexInRange(index))
                    Main.projectile[index].scale = Main.rand.NextFloat(0.72f, 1.04f);
            }
        }
    }
}
