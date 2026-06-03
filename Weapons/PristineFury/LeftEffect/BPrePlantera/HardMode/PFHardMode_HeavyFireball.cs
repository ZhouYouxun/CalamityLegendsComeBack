using CalamityMod;
using CalamityMod.Buffs.DamageOverTime;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.PristineFury.LeftEffect
{
    internal sealed class PFHardMode_HeavyFireball : ModProjectile, ILocalizedModType
    {
        private Color ThemeColor => PFLeftEffectRules.GetThemeColor(Projectile, new Color(255, 224, 92));

        public new string LocalizationCategory => "Projectiles.PristineFury";
        public override string Texture => "CalamityMod/Projectiles/FireProj";

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 12;
            ProjectileID.Sets.TrailingMode[Type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 54;
            Projectile.scale = 1.35f;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 240;
            Projectile.tileCollide = true;
            Projectile.ignoreWater = true;
        }

        public override void AI()
        {
            Projectile.velocity.Y = MathHelper.Clamp(Projectile.velocity.Y + 0.2f, -18f, 15f);

            Projectile.rotation += Projectile.velocity.X * 0.025f;

            Lighting.AddLight(Projectile.Center, ThemeColor.ToVector3() * 1.05f);

            if (Main.dedServ)
                return;

            Vector2 forward = Projectile.velocity.SafeNormalize(Vector2.UnitY);
            Vector2 backward = -forward;

            // =========================
            // 核心高亮能量粒子
            // =========================

            if (Main.rand.NextBool(2))
            {
                GeneralParticleHandler.SpawnParticle(new GlowOrbParticle(
                    Projectile.Center +
                    Main.rand.NextVector2Circular(8f, 8f),

                    backward * Main.rand.NextFloat(0.5f, 2.4f),

                    false,
                    Main.rand.Next(14, 24),
                    Main.rand.NextFloat(0.35f, 0.72f),

                    Color.Lerp(
                        ThemeColor,
                        Color.White,
                        Main.rand.NextFloat(0.18f, 0.52f)),

                    true,
                    false,
                    true));
            }

            // =========================
            // 重型尾焰
            // =========================

            if (Main.rand.NextBool(2))
            {
                GeneralParticleHandler.SpawnParticle(new MediumMistParticle(
                    Projectile.Center -
                    forward * Main.rand.NextFloat(10f, 28f),

                    backward * Main.rand.NextFloat(1.4f, 5.2f),

                    Color.Lerp(
                        ThemeColor,
                        Color.Goldenrod,
                        Main.rand.NextFloat(0.08f, 0.28f)),

                    Color.Transparent,

                    Main.rand.NextFloat(0.42f, 0.95f),

                    Main.rand.Next(18, 34),

                    Main.rand.NextFloat(-0.04f, 0.04f)));
            }

            // =========================
            // 高速火花喷射
            // =========================

            if (Main.rand.NextBool(2))
            {
                Vector2 sparkVelocity =
                    backward.RotatedByRandom(0.42f) *
                    Main.rand.NextFloat(3f, 10f);

                GeneralParticleHandler.SpawnParticle(new SparkParticle(
                    Projectile.Center,
                    sparkVelocity,
                    false,
                    Main.rand.Next(14, 24),
                    Main.rand.NextFloat(0.9f, 1.8f),

                    Color.Lerp(
                        ThemeColor,
                        Color.White,
                        Main.rand.NextFloat(0.12f, 0.5f))));
            }

            // =========================
            // 高频火焰 Dust
            // =========================

            if (Main.rand.NextBool(2))
            {
                Dust dust = Dust.NewDustPerfect(
                    Projectile.Center +
                    Main.rand.NextVector2Circular(10f, 10f),

                    Main.rand.NextBool(3)
                        ? DustID.GoldFlame
                        : DustID.YellowTorch,

                    backward.RotatedByRandom(0.32f) *
                    Main.rand.NextFloat(1f, 5f),

                    70,

                    Color.Lerp(
                        ThemeColor,
                        Color.White,
                        Main.rand.NextFloat(0.06f, 0.3f)),

                    Main.rand.NextFloat(1f, 1.8f));

                dust.noGravity = true;
            }

            // =========================
            // 低频重型烟雾
            // 烟雾现在只是辅助
            // =========================

            if (Main.rand.NextBool(6))
            {
                GeneralParticleHandler.SpawnParticle(new HeavySmokeParticle(
                    Projectile.Center -
                    Projectile.velocity * 0.35f +
                    Main.rand.NextVector2Circular(10f, 10f),

                    backward * Main.rand.NextFloat(0.4f, 1.8f),

                    Color.Lerp(
                        ThemeColor,
                        Color.DarkGoldenrod,
                        0.42f),

                    Main.rand.Next(18, 28),

                    Main.rand.NextFloat(0.65f, 1.05f),

                    0.55f,

                    Main.rand.NextFloat(-0.03f, 0.03f),

                    glowing: true));
            }

            // =========================
            // 节奏型能量环
            // =========================

            if (Projectile.timeLeft % 10 == 0)
            {
                GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(
                    Projectile.Center,
                    Vector2.Zero,

                    Color.Lerp(
                        ThemeColor,
                        Color.White,
                        0.14f) * 0.55f,

                    new Vector2(0.75f, 0.75f),

                    Projectile.rotation,

                    0.02f,
                    0.24f,
                    14));
            }
        }
        public override void OnKill(int timeLeft)
        {
            if (Projectile.owner == Main.myPlayer)
            {
                int oldWidth = Projectile.width;
                int oldHeight = Projectile.height;
                Vector2 center = Projectile.Center;
                Projectile.width = Projectile.height = 170;
                Projectile.Center = center;
                Projectile.penetrate = -1;
                Projectile.Damage();
                Projectile.width = oldWidth;
                Projectile.height = oldHeight;
                Projectile.Center = center;

                for (int i = 0; i < 12; i++)
                {
                    float angle = MathHelper.Lerp(-MathHelper.Pi * 0.92f, -MathHelper.Pi * 0.08f, i / 11f);
                    Vector2 velocity = angle.ToRotationVector2() * Main.rand.NextFloat(4.8f, 10.6f);
                    int fragment = Projectile.NewProjectile(
                        Projectile.GetSource_FromThis(),
                        center,
                        velocity,
                        ModContent.ProjectileType<PFHardMode_TotalityFire>(),
                        Math.Max(1, (int)(Projectile.damage * 0.54f)),
                        Projectile.knockBack * 0.25f,
                        Projectile.owner,
                        0f,
                        Main.rand.Next(8, 18));
                    PFLeftEffectRules.ApplyTheme(fragment, (PristineFuryMark)(int)Projectile.ai[2]);
                }
            }

            if (!Main.dedServ)
            {
                PFHardMode_TotalityFire.SpawnBurstEffects(Projectile.Center, ThemeColor, 1.6f);
                SoundEngine.PlaySound(SoundID.Item14 with { Volume = 0.78f, Pitch = -0.18f }, Projectile.Center);
            }
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) =>
            CalamityUtils.CircularHitboxCollision(Projectile.Center, 34f * Projectile.scale, targetHitbox);

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) =>
            target.AddBuff(ModContent.BuffType<HolyFlames>(), 360);

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = Terraria.GameContent.TextureAssets.Projectile[Type].Value;

            Texture2D bloom =
                ModContent.Request<Texture2D>(
                    "CalamityMod/Particles/BloomCircle").Value;

            Texture2D ring =
                ModContent.Request<Texture2D>(
                    "CalamityMod/Particles/BloomRing").Value;

            Texture2D line =
                ModContent.Request<Texture2D>(
                    "CalamityMod/Particles/BloomLineSoftEdge").Value;

            Texture2D magic =
                ModContent.Request<Texture2D>(
                    "CalamityLegendsComeBack/Texture/KsTexture/magic_03").Value;

            Vector2 center =
                Projectile.Center -
                Main.screenPosition;

            Vector2 direction =
                Projectile.velocity.SafeNormalize(Vector2.UnitX);

            // 不要再 A = 0
            Color theme =
                Color.Lerp(
                    ThemeColor,
                    Color.White,
                    0.12f);

            PFLeftEffectRules.BeginAdditive();

            // =========================
            // 重型拖尾
            // =========================

            for (int i = 1; i < Projectile.oldPos.Length; i++)
            {
                if (Projectile.oldPos[i] == Vector2.Zero ||
                    Projectile.oldPos[i - 1] == Vector2.Zero)
                    continue;

                float completion =
                    i / (float)Projectile.oldPos.Length;

                Vector2 current =
                    Projectile.oldPos[i] +
                    Projectile.Size * 0.5f;

                Vector2 previous =
                    Projectile.oldPos[i - 1] +
                    Projectile.Size * 0.5f;

                Vector2 between =
                    previous - current;

                float length =
                    between.Length();

                if (length <= 1f)
                    continue;

                Main.EntitySpriteDraw(
                    line,

                    (current + previous) * 0.5f -
                    Main.screenPosition,

                    null,

                    Color.Lerp(
                        theme,
                        Color.White,
                        0.24f)

                    * (0.55f * (1f - completion)),

                    between.ToRotation() +
                    MathHelper.PiOver2,

                    line.Size() * 0.5f,

                    new Vector2(
                        0.28f * (1f - completion),
                        length / line.Height),

                    SpriteEffects.None,
                    0f);
            }

            // =========================
            // 外层火焰辉光
            // =========================

            Main.EntitySpriteDraw(
                bloom,
                center,
                null,
                theme * 0.85f,
                Projectile.rotation,
                bloom.Size() * 0.5f,
                Projectile.scale * 0.92f,
                SpriteEffects.None,
                0);

            // =========================
            // 核心压缩高亮
            // =========================

            Main.EntitySpriteDraw(
                bloom,
                center - direction * 10f,
                null,

                Color.Lerp(
                    theme,
                    Color.White,
                    0.42f) * 0.75f,

                Projectile.rotation,

                bloom.Size() * 0.5f,

                Projectile.scale *
                new Vector2(0.82f, 0.42f),

                SpriteEffects.None,
                0);

            // =========================
            // 能量旋转环
            // =========================

            Main.EntitySpriteDraw(
                ring,
                center,
                null,
                theme * 0.58f,

                Projectile.rotation +
                Main.GlobalTimeWrappedHourly * 1.2f,

                ring.Size() * 0.5f,

                Projectile.scale * 0.72f,

                SpriteEffects.None,
                0);

            // =========================
            // 魔法能量层
            // =========================

            Main.EntitySpriteDraw(
                magic,
                center,
                null,

                Color.Lerp(
                    theme,
                    Color.White,
                    0.22f) * 0.48f,

                -Projectile.rotation +
                Main.GlobalTimeWrappedHourly * 1.8f,

                magic.Size() * 0.5f,

                Projectile.scale * 0.22f,

                SpriteEffects.None,
                0);

            // =========================
            // 本体
            // =========================

            Main.EntitySpriteDraw(
                texture,
                center,
                null,

                Color.Lerp(
                    theme,
                    Color.White,
                    0.35f),

                Projectile.rotation,

                texture.Size() * 0.5f,

                Projectile.scale * 1.42f,

                SpriteEffects.None,
                0);

            PFLeftEffectRules.EndAdditive();

            return false;
        }







    }
}
