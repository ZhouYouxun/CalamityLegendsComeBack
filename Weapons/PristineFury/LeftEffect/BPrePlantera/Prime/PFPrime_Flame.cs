using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.PristineFury.LeftEffect
{
    internal sealed class PFPrime_Flame : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.PristineFury";
        public override string Texture => "CalamityMod/Projectiles/FireProj";

        private const int Lifetime = 96;
        private const int Fadetime = 80;
        private int MistType = -1;
        private ref float Time => ref Projectile.localAI[0];
        private ref float BounceCount => ref Projectile.localAI[1];

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 12;
            ProjectileID.Sets.TrailingMode[Type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 10;
            Projectile.friendly = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.penetrate = 7;
            Projectile.MaxUpdates = 4;
            Projectile.timeLeft = Lifetime;
            Projectile.usesIDStaticNPCImmunity = true;
            Projectile.idStaticNPCHitCooldown = 3;
        }

        public override void AI()
        {
            Time++;

            if (MistType == -1)
                MistType = Main.rand.Next(3);

            Projectile.rotation = Projectile.velocity.ToRotation();
            Color theme = PFLeftEffectRules.GetThemeColor(Projectile, new Color(255, 206, 92));
            Lighting.AddLight(Projectile.Center, theme.ToVector3() * 0.66f);

            if (Time > Fadetime)
                Projectile.velocity *= 0.95f;

            if (Main.dedServ)
                return;

            if (Time > 6f && Time < Fadetime)
            {
                if (Main.rand.NextBool(16))
                {
                    Dust dust = Dust.NewDustDirect(
                        Projectile.Center + Main.rand.NextVector2Circular(60f, 60f) * Utils.Remap(Time, 0f, Fadetime, 0.5f, 1f),
                        4,
                        4,
                        Main.rand.NextBool() ? DustID.Torch : DustID.SolarFlare,
                        Projectile.velocity.X * 0.2f,
                        Projectile.velocity.Y * 0.2f,
                        100,
                        theme);

                    if (Main.rand.NextBool(5))
                    {
                        dust.noGravity = true;
                        dust.scale *= 2f;
                        dust.velocity *= 0.8f;
                    }

                    dust.velocity *= 1.1f;
                    dust.velocity += Projectile.velocity * Utils.Remap(Time, 0f, Fadetime * 0.75f, 1f, 0.1f) * Utils.Remap(Time, 0f, Fadetime * 0.1f, 0.1f, 1f);
                }

                if (Main.rand.NextBool(19))
                {
                    float size = Utils.Remap(Utils.GetLerpValue(0f, Lifetime, Time), 0.2f, 0.5f, 0.25f, 1f);
                    Particle trail = new CustomSpark(
                        Projectile.Center,
                        Projectile.velocity + Vector2.UnitY * Main.rand.NextFloat(-10f, -24f) * size,
                        "CalamityMod/Particles/BloomCircle",
                        false,
                        14,
                        0.9f * size,
                        Color.Lerp(theme, Color.White, 0.18f) * 0.5f,
                        new Vector2(Main.rand.NextFloat(2f, 3f), 1f),
                        true,
                        true,
                        shrinkSpeed: 0.3f,
                        glowOpacity: 0.5f);

                    GeneralParticleHandler.SpawnParticle(trail);
                }
            }
            else if (Time == 5f)
            {
                Dust dust = Dust.NewDustPerfect(
                    Projectile.Center,
                    Main.rand.NextBool(3) ? DustID.Torch : DustID.SolarFlare,
                    Projectile.velocity.RotatedByRandom(MathHelper.ToRadians(30f)) * Main.rand.NextFloat(0.5f, 1f),
                    0,
                    theme);
                dust.scale = Main.rand.NextFloat(0.8f, 1.8f);
                dust.noGravity = true;
                dust.fadeIn = 0.5f;
            }
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            SpawnBounceExplosion();
            BounceCount++;
            if (BounceCount >= 5f)
                return true;

            if (Projectile.velocity.X != oldVelocity.X)
                Projectile.velocity.X = -oldVelocity.X * 0.92f;
            if (Projectile.velocity.Y != oldVelocity.Y)
                Projectile.velocity.Y = -oldVelocity.Y * 0.92f;

            Projectile.timeLeft = Math.Max(Projectile.timeLeft, 42);
            Projectile.netUpdate = true;
            return false;
        }

        private void SpawnBounceExplosion()
        {
            if (Projectile.owner == Main.myPlayer)
            {
                int explosion = Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    Projectile.Center,
                    Vector2.Zero,
                    ModContent.ProjectileType<PFPrime_BounceExplosion>(),
                    Math.Max(1, (int)(Projectile.damage * 0.55f)),
                    Projectile.knockBack * 0.5f,
                    Projectile.owner);
                PFLeftEffectRules.ApplyTheme(explosion, (PristineFuryMark)(int)Projectile.ai[2]);
            }

            if (Main.dedServ)
                return;

            Color theme = PFLeftEffectRules.GetThemeColor(Projectile, new Color(255, 206, 92));
            for (int i = 0; i < 9; i++)
            {
                GeneralParticleHandler.SpawnParticle(new SparkParticle(
                    Projectile.Center,
                    Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(2f, 6f),
                    false,
                    Main.rand.Next(12, 20),
                    Main.rand.NextFloat(0.55f, 0.95f),
                    Color.Lerp(theme, Color.White, Main.rand.NextFloat(0.1f, 0.35f))));
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) => target.AddBuff(BuffID.OnFire3, 240);

        public override void ModifyDamageHitbox(ref Rectangle hitbox)
        {
            int size = (int)Utils.Remap(Time, 0f, Fadetime, 10f, 40f);
            if (Time > Fadetime)
                size = (int)Utils.Remap(Time, Fadetime, Lifetime, 40f, 0f);

            hitbox.Inflate(size, size);
        }

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            if (Projectile.numHits > 0)
                Projectile.damage = (int)(Projectile.damage * 0.75f);
            if (Projectile.damage < 1)
                Projectile.damage = 1;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D fire = Terraria.GameContent.TextureAssets.Projectile[Type].Value;
            Texture2D mist = ModContent.Request<Texture2D>("CalamityMod/Particles/MediumMist").Value;

            Color color1 = new(255, 210, 92, 220);
            Color color2 = new(255, 145, 36, 120);
            Color color3 = new(255, 86, 22, 140);
            Color color4 = new(150, 44, 10, 110);
            float length = Time > Fadetime - 10f ? 0.1f : 0.15f;
            float vOffset = Math.Min(Time, 20f);
            float timeRatio = Utils.GetLerpValue(0f, Lifetime, Time);
            float fireSize = Utils.Remap(timeRatio, 0.2f, 0.5f, 0.25f, 1f);

            if (timeRatio >= 1f)
                return false;

            for (float j = 1f; j >= 0f; j -= length)
            {
                Color fireColor = timeRatio < 0.1f
                    ? Color.Lerp(Color.Transparent, color1, Utils.GetLerpValue(0f, 0.1f, timeRatio))
                    : timeRatio < 0.2f
                        ? Color.Lerp(color1, color2, Utils.GetLerpValue(0.1f, 0.2f, timeRatio))
                        : timeRatio < 0.35f
                            ? color2
                            : timeRatio < 0.7f
                                ? Color.Lerp(color2, color3, Utils.GetLerpValue(0.35f, 0.7f, timeRatio))
                                : timeRatio < 0.85f
                                    ? Color.Lerp(color3, color4, Utils.GetLerpValue(0.7f, 0.85f, timeRatio))
                                    : Color.Lerp(color4, Color.Transparent, Utils.GetLerpValue(0.85f, 1f, timeRatio));

                fireColor *= (1f - j) * Utils.GetLerpValue(0f, 0.2f, timeRatio, true);
                Vector2 firePos = Projectile.Center - Main.screenPosition - Projectile.velocity * vOffset * j;
                float mainRot = (-j * MathHelper.PiOver2 - Main.GlobalTimeWrappedHourly * (j + 1f) * 2f / length) * Math.Sign(Projectile.velocity.X);
                float trailRot = MathHelper.PiOver4 - mainRot;
                Vector2 trailOffset = Projectile.velocity * vOffset * length * 0.5f;

                Main.EntitySpriteDraw(fire, firePos - trailOffset, null, fireColor * 0.25f, trailRot, fire.Size() * 0.5f, fireSize, SpriteEffects.None);
                Main.EntitySpriteDraw(fire, firePos, null, fireColor, mainRot, fire.Size() * 0.5f, fireSize, SpriteEffects.None);

                if (MistType > 2 || MistType < 0)
                    return false;

                Rectangle frame = mist.Frame(1, 3, 0, MistType);
                Main.EntitySpriteDraw(mist, firePos, frame, Color.Lerp(fireColor, Color.White, 0.3f) with { A = 0 }, mainRot, frame.Size() * 0.5f, fireSize, SpriteEffects.None);
                Main.EntitySpriteDraw(mist, firePos, frame, fireColor with { A = 0 }, mainRot, frame.Size() * 0.5f, fireSize * 3f, SpriteEffects.None);
            }

            return false;
        }
    }

    internal sealed class PFPrime_BounceExplosion : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.PristineFury";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 10;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 3;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }

        public override void AI()
        {
            if (Projectile.localAI[0] != 0f)
                return;

            Projectile.localAI[0] = 1f;
            Projectile.Resize(118, 118);
            Projectile.Damage();

            if (Main.dedServ)
                return;

            Color theme = PFLeftEffectRules.GetThemeColor(Projectile, Color.Cyan);
            Particle expandingPulse = new DirectionalPulseRing(
                Projectile.Center,
                Vector2.Zero,
                theme,
                new Vector2(1.2f, 1.2f),
                0f,
                0.5f,
                6.0f,
                20);
            GeneralParticleHandler.SpawnParticle(expandingPulse);

            GeneralParticleHandler.SpawnParticle(new CustomPulse(
                Projectile.Center,
                Vector2.Zero,
                Color.Lerp(theme, Color.White, 0.35f),
                "CalamityMod/Particles/BloomCircle",
                Vector2.One,
                Main.rand.NextFloat(-10f, 10f),
                0.18f,
                0.8f,
                14));
        }

        public override bool PreDraw(ref Color lightColor) => false;
    }
}
