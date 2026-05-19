using CalamityMod;
using CalamityMod.Buffs.DamageOverTime;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.PristineFury.LeftEffect
{
    internal sealed class PFHardMode_Flame : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.PristineFury";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private const int Lifetime = 108;
        private ref float Timer => ref Projectile.localAI[0];
        private ref float HueDrift => ref Projectile.localAI[1];

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 18;
            ProjectileID.Sets.TrailingMode[Type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 52;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.penetrate = 5;
            Projectile.timeLeft = Lifetime;
            Projectile.extraUpdates = 4;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }

        public override void AI()
        {
            Timer++;
            HueDrift += 0.022f;

            Projectile.scale = 1.75f * Utils.GetLerpValue(4f, 30f, Timer, true) * Utils.GetLerpValue(0f, 20f, Projectile.timeLeft, true);
            Projectile.rotation = Projectile.velocity.ToRotation();
            Projectile.velocity = Projectile.velocity.RotatedBy((float)Math.Sin(Timer * 0.1f + Projectile.ai[1]) * 0.01f) * 0.993f;

            EmitChromaticBody();
        }

        private Color RandomChromaticColor() => Main.rand.Next(4) switch
        {
            0 => Color.DeepSkyBlue,
            1 => Color.MediumSpringGreen,
            2 => Color.DarkOrange,
            _ => Color.Violet
        };

        private void EmitChromaticBody()
        {
            Color color = RandomChromaticColor();

            if (Timer == 1f && !Main.dedServ)
            {
                for (int i = 0; i < 12; i++)
                {
                    float rotMulti = Main.rand.NextFloat(0.7f, 1.1f);
                    Dust dust = Dust.NewDustPerfect(Projectile.Center, Main.rand.NextBool() ? 66 : 247);
                    dust.scale = Main.rand.NextFloat(1.8f, 2.5f) - rotMulti;
                    dust.noGravity = true;
                    dust.velocity = Projectile.velocity.RotatedByRandom(0.5f * rotMulti) * Main.rand.NextFloat(0.5f, 1.8f) * rotMulti;
                    dust.alpha = Main.rand.Next(90, 150);
                    dust.color = color;
                }
            }

            if (Main.dedServ)
                return;

            if (Timer > 9f)
            {
                float dustArea = Main.rand.NextFloat(0.1f, 1.7f);
                Dust dust = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(9f, 9f) + Projectile.velocity * Main.rand.NextFloat(-1.8f, 1.8f), Main.rand.NextBool() ? 66 : 247);
                dust.scale = (1.8f - dustArea) * 0.65f;
                dust.noGravity = true;
                dust.velocity = new Vector2(4f, 4f).RotatedByRandom(100f) * dustArea;
                dust.alpha = Main.rand.Next(90, 150);
                dust.color = color;
            }

            float hue = 0.5f * (HueDrift % 1f) + 0.5f * Utils.GetLerpValue(28f, Lifetime, Timer, true) * MathF.Sin(Main.GlobalTimeWrappedHourly * 5f);
            Color smokeColor = Main.hslToRgb(hue, 1f, 0.7f);
            float smokeRot = MathHelper.ToRadians(3f);
            Particle smoke = new HeavySmokeParticle(Projectile.Center, Projectile.velocity * 0.5f, smokeColor, 12, Projectile.scale * Main.rand.NextFloat(0.6f, 1.2f), 0.45f, smokeRot, true, required: true);
            GeneralParticleHandler.SpawnParticle(smoke);

            if (Main.rand.NextBool(5))
            {
                Particle glow = new HeavySmokeParticle(Projectile.Center, Projectile.velocity * 0.5f, Color.Lerp(smokeColor, Color.White, 0.3f), 9, Projectile.scale * Main.rand.NextFloat(0.4f, 0.7f), 0.2f, smokeRot, true, 0.005f);
                GeneralParticleHandler.SpawnParticle(glow);
            }

            if (Timer % 18f == 0f)
                GeneralParticleHandler.SpawnParticle(new MediumMistParticle(Projectile.Center, Main.rand.NextVector2Circular(14f, 14f), Main.hslToRgb(Main.rand.NextFloat(), 1f, 0.8f), Color.Black, Main.rand.NextFloat(0.6f, 1.4f), 150, 0.1f));

            Lighting.AddLight(Projectile.Center, smokeColor.ToVector3() * Projectile.scale * 0.34f);
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) => CalamityUtils.CircularHitboxCollision(Projectile.Center, 52f * Projectile.scale * 0.5f, targetHitbox);

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) => target.AddBuff(ModContent.BuffType<ElementalMix>(), 900);

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D flare = ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/SmallGreyscaleCircle").Value;

            PFLeftEffectRules.BeginAdditive();
            for (int i = Projectile.oldPos.Length - 1; i >= 0; i--)
            {
                float completion = i / (float)Projectile.oldPos.Length;
                float hue = 0.5f + 0.5f * completion * MathF.Sin(Main.GlobalTimeWrappedHourly * 5f);
                Color color = Main.hslToRgb(hue, 1f, 0.6f) * (1f - completion) * 1.6f;
                Vector2 drawPosition = Projectile.oldPos[i] + Projectile.Size * 0.5f - Main.screenPosition;
                Main.EntitySpriteDraw(flare, drawPosition, null, color, Projectile.rotation, flare.Size() * 0.5f, Projectile.scale * MathHelper.Lerp(0.12f, 0.7f, 1f - completion), SpriteEffects.None, 0);
            }
            PFLeftEffectRules.EndAdditive();

            return false;
        }
    }
}
