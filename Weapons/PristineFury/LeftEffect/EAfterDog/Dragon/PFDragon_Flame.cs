using CalamityMod;
using CalamityMod.Buffs.DamageOverTime;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.PristineFury.LeftEffect
{
    internal sealed class PFDragon_Flame : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.PristineFury";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private int time;
        private Color ThemeColor => PFLeftEffectRules.GetThemeColor(Projectile, new Color(255, 224, 92));

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 24;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.penetrate = 10;
            Projectile.timeLeft = 84;
            Projectile.extraUpdates = 4;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }

        public override void AI()
        {
            Projectile.rotation = Projectile.velocity.ToRotation();
            Lighting.AddLight(Projectile.Center, ThemeColor.ToVector3() * 0.86f);

            if (time == 0)
                SpawnMuzzleWash();

            EmitDragonBreath();
            if (time < 70)
                Projectile.velocity *= 1.01f;

            time++;
        }

        private void SpawnMuzzleWash()
        {
            if (Main.dedServ)
                return;

            Vector2 forward = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            for (int i = 0; i < 8; i++)
            {
                Dust dust = Dust.NewDustPerfect(Projectile.Center, Main.rand.NextBool(3) ? DustID.Torch : DustID.GoldFlame);
                dust.velocity = forward.RotatedByRandom(0.45f) * Main.rand.NextFloat(2.4f, 8.4f);
                dust.scale = Main.rand.NextFloat(0.8f, 1.55f);
                dust.noGravity = true;
                dust.color = Main.rand.NextBool() ? ThemeColor : Color.Lerp(ThemeColor, Color.White, 0.4f);
            }

            for (int i = 0; i < 5; i++)
            {
                Vector2 smokeVelocity = forward.RotatedByRandom(0.45f) * Main.rand.NextFloat(2.1f, 5.8f);
                GeneralParticleHandler.SpawnParticle(new SmallSmokeParticle(Projectile.Center, smokeVelocity, Color.DimGray, Main.rand.NextBool() ? Color.SlateGray : Color.Black, Main.rand.NextFloat(0.45f, 1.05f), 100f));
            }
        }

        private void EmitDragonBreath()
        {
            if (Main.dedServ)
                return;

            Vector2 forward = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            float pulse = MathHelper.Clamp(System.MathF.Abs(System.MathF.Sin((time + Projectile.ai[1]) * 0.575f / MathHelper.Pi)), 0.45f + time * 0.0015f, 1f);

            GeneralParticleHandler.SpawnParticle(new CustomSpark(
                Projectile.Center,
                forward,
                "CalamityMod/Particles/SmallBloom",
                false,
                10,
                0.09f + time * 0.0014f,
                ThemeColor * 0.62f,
                Vector2.One,
                true,
                false,
                glowOpacity: 0.55f));

            GeneralParticleHandler.SpawnParticle(new CustomSpark(
                Projectile.Center,
                forward,
                "CalamityMod/Particles/SmallBloom",
                false,
                10,
                0.04f + time * 0.0012f,
                Color.Lerp(ThemeColor, Color.White, 0.32f) * 0.55f,
                Vector2.One,
                true,
                false,
                glowOpacity: 0.52f));

            if (time % 5 == 0)
            {
                Vector2 offset = Main.rand.NextVector2Circular(1f + time * 0.14f, 1f + time * 0.14f);
                GeneralParticleHandler.SpawnParticle(new SparkParticle(Projectile.Center + offset, -Projectile.velocity * 0.45f, false, 15, Main.rand.NextFloat(0.38f, 0.68f) * pulse, Main.rand.NextBool() ? ThemeColor : Color.Lerp(ThemeColor, Color.White, 0.35f)));
            }

            if (time % 7 == 0)
            {
                Vector2 smokeVelocity = forward.RotatedByRandom(0.52f) * Main.rand.NextFloat(2.6f, 6.6f);
                GeneralParticleHandler.SpawnParticle(new SmallSmokeParticle(Projectile.Center + Main.rand.NextVector2Circular(7f, 7f), smokeVelocity, Color.DimGray, Main.rand.NextBool() ? Color.SlateGray : Color.Black, Main.rand.NextFloat(0.4f, 0.9f), 90f));
            }
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) =>
            CalamityUtils.CircularHitboxCollision(Projectile.Center, Projectile.width + time * 0.12f, targetHitbox);

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            if (Projectile.numHits > 0)
                Projectile.damage = System.Math.Max(1, (int)(Projectile.damage * 0.92f));
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(ModContent.BuffType<Dragonfire>(), 420);
            SpawnHitEffects(target.Center);

            if (Main.myPlayer != Projectile.owner)
                return;

            int burst = Projectile.NewProjectile(
                Projectile.GetSource_FromThis(),
                target.Center,
                Vector2.Zero,
                ModContent.ProjectileType<PFDragon_Burst>(),
                System.Math.Max(1, (int)(Projectile.damage * 0.55f)),
                Projectile.knockBack * 0.25f,
                Projectile.owner);

            PFLeftEffectRules.ApplyTheme(burst, (PristineFuryMark)(int)Projectile.ai[2]);
        }

        private void SpawnHitEffects(Vector2 center)
        {
            if (Main.dedServ)
                return;

            Vector2 backward = -Projectile.velocity.SafeNormalize(Vector2.UnitY);
            GeneralParticleHandler.SpawnParticle(new CustomSpark(Projectile.Center, Vector2.Zero, "CalamityMod/Particles/SmallBloom", false, 7, Main.rand.NextFloat(0.54f, 0.68f), ThemeColor, Vector2.One, true, false));
            GeneralParticleHandler.SpawnParticle(new CustomPulse(center, Vector2.Zero, Color.Lerp(ThemeColor, Color.White, 0.2f) * 0.68f, "CalamityMod/Particles/BloomRing", Vector2.One, Main.rand.NextFloat(-10f, 10f), Main.rand.NextFloat(0.2f, 1.2f), 2.2f, 15, true));

            for (int i = 0; i < 6; i++)
            {
                Vector2 velocity = backward.RotatedByRandom(0.55f) * Main.rand.NextFloat(9f, 22f);
                GeneralParticleHandler.SpawnParticle(new SparkParticle(center + Main.rand.NextVector2Circular(10f, 10f), velocity, true, Main.rand.Next(18, 30), Main.rand.NextFloat(0.75f, 1.25f), Main.rand.NextBool(4) ? ThemeColor : Color.Lerp(ThemeColor, Color.White, 0.35f)));
            }
        }

        public override bool PreDraw(ref Color lightColor) => false;
    }

    internal sealed class PFDragon_Burst : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.PristineFury";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private Color ThemeColor => PFLeftEffectRules.GetThemeColor(Projectile, new Color(255, 224, 92));

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 260;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 8;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }

        public override void AI()
        {
            if (Projectile.localAI[0]++ == 0f)
                SpawnBurstEffects();
        }

        private void SpawnBurstEffects()
        {
            if (Main.dedServ)
                return;

            for (int i = 0; i < 12; i++)
            {
                Vector2 velocity = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(5f, 18f);
                GeneralParticleHandler.SpawnParticle(new SparkParticle(Projectile.Center + Main.rand.NextVector2Circular(24f, 24f), velocity, true, Main.rand.Next(18, 32), Main.rand.NextFloat(0.85f, 1.45f), Main.rand.NextBool(4) ? ThemeColor : Color.Lerp(ThemeColor, Color.White, 0.38f)));
            }

            for (int i = 0; i < 8; i++)
            {
                Vector2 velocity = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(3f, 10f);
                GeneralParticleHandler.SpawnParticle(new SmallSmokeParticle(Projectile.Center + Main.rand.NextVector2Circular(44f, 44f), velocity, Color.DimGray, Main.rand.NextBool() ? Color.SlateGray : Color.Black, Main.rand.NextFloat(0.7f, 1.5f), 110f));
            }

            GeneralParticleHandler.SpawnParticle(new CustomPulse(Projectile.Center, Vector2.Zero, (ThemeColor with { A = 0 }) * 0.75f, "CalamityMod/Particles/BloomRing", Vector2.One, Main.rand.NextFloat(-10f, 10f), 0.42f, 3.0f, 16, true));
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) =>
            CalamityUtils.CircularHitboxCollision(Projectile.Center, 130f, targetHitbox);

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            target.AddBuff(ModContent.BuffType<Dragonfire>(), 420);
            if (Projectile.numHits > 0)
                Projectile.damage = System.Math.Max(1, (int)(Projectile.damage * 0.95f));
        }

        public override bool PreDraw(ref Color lightColor) => false;
    }
}
