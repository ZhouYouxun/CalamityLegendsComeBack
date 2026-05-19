using CalamityMod;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SHPC.Effects.DPreDog.SZPC
{
    public class TwistingNether_Blade : ModProjectile, ILocalizedModType
    {
        private static readonly Color BladePurple = new(185, 35, 255);
        private static readonly Color BladeBlood = new(135, 0, 42);
        private static readonly Color BladeDark = new(12, 0, 20);

        public new string LocalizationCategory => "Projectiles.SHPC";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        // 飞行计时器
        private int flightTimer;

        // 螺旋尾迹角度
        private float helixAngle;

        // 脉冲光效角度
        private float pulseAngle;

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 24;
            ProjectileID.Sets.TrailingMode[Type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 170;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = 5;
            Projectile.timeLeft = 260;
            Projectile.extraUpdates = 2;
            Projectile.Opacity = 1f;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 12;
        }

        public override bool? CanCutTiles() => false;

        public override void OnSpawn(IEntitySource source)
        {
            // 保证生成后必定立刻进入直线飞行
            if (Projectile.velocity.LengthSquared() < 0.001f)
                Projectile.velocity = -Vector2.UnitY * 24f;

            Projectile.Opacity = 1f;
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;

            // 给每一发一点点不同的初始相位，避免视觉完全重叠
            helixAngle = Projectile.identity * 0.37f;
            pulseAngle = Projectile.identity * 0.21f;

            SoundEngine.PlaySound(SoundID.Item104 with { Pitch = -0.25f, Volume = 0.55f }, Projectile.Center);
        }

        public override void AI()
        {
            bool firstSubstep = Projectile.numUpdates == 0;

            if (firstSubstep)
            {
                flightTimer++;
                helixAngle += 0.28f;
                pulseAngle += 0.16f;
            }

            // 永远只保留二阶段：不减速、不追踪、不制导，纯直线飞行
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
            Projectile.Opacity = 1f;

            if (!Main.dedServ)
                SpawnFlightEffects(firstSubstep);
        }

        private void SpawnFlightEffects(bool firstSubstep)
        {
            if (!firstSubstep)
                return;

            Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitY);
            Vector2 side = direction.RotatedBy(MathHelper.PiOver2);

            // 二阶段原本的螺旋拖尾
            Vector2 center = Projectile.Center + side * ((float)Math.Sin(helixAngle) * 9f);
            float intensity = 0.9f + 0.22f * (float)Math.Sin(pulseAngle * 1.4f);
            Color mainColor = BladePurple;
            Color acidColor = BladeBlood;
            Vector2 velocity =
                -direction * Main.rand.NextFloat(4.6f, 8.2f) +
                side * (float)Math.Cos(helixAngle * 1.25f) * Main.rand.NextFloat(0.8f, 2.1f);

            GeneralParticleHandler.SpawnParticle(new CustomSpark(
                center + Main.rand.NextVector2Circular(8f, 8f),
                velocity,
                "CalamityMod/Particles/VerticalSmear",
                false,
                Main.rand.Next(15, 23),
                Main.rand.NextFloat(1.8f, 2.8f) * intensity,
                Color.Lerp(mainColor, acidColor, Main.rand.NextFloat(0.25f, 0.8f)),
                new Vector2(0.18f, 1f)));

            if (firstSubstep)
            {
                // 把原一阶段的“蓄能感”脉冲并到现在的直线飞行里
                if (flightTimer % 4 == 0)
                {
                    GeneralParticleHandler.SpawnParticle(new CustomSpark(
                        center,
                        Vector2.Zero,
                        "CalamityMod/Particles/BloomCircle",
                        false,
                        10,
                        0.055f,
                        Color.Lerp(BladePurple, Color.White, 0.12f),
                        new Vector2(1f, 1f),
                        true,
                        false,
                        Main.rand.NextFloat(-0.12f, 0.12f),
                        false,
                        false,
                        0.65f));
                }
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (Projectile.owner == Main.myPlayer)
            {
                Vector2 slashVelocity = Projectile.velocity.SafeNormalize(Vector2.UnitX) * 8f;
                Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    target.Center,
                    slashVelocity,
                    ModContent.ProjectileType<TwistingNether_BlackSLASH>(),
                    (int)(Projectile.damage * 1.1f),
                    Projectile.knockBack,
                    Projectile.owner);
            }

            if (!Main.dedServ)
            {
                for (int i = 0; i < 5; i++)
                {
                    GeneralParticleHandler.SpawnParticle(new CustomPulse(
                        target.Center,
                        Vector2.Zero,
                        i == 0 ? BladeDark : (i % 2 == 0 ? BladePurple : BladeBlood),
                        i == 0 ? "CalamityMod/Particles/LargeBloom" : "CalamityMod/Particles/BloomCircle",
                        i == 0 ? new Vector2(1.8f, 0.8f) : Vector2.One,
                        Main.rand.NextFloat(-0.2f, 0.2f),
                        MathHelper.Max(0.12f, 0.78f - i * 0.1f) * 0.05f,
                        0f,
                        18,
                        true));
                }

                for (int i = 0; i < 48; i++)
                {
                    Dust dust = Dust.NewDustPerfect(
                        target.Center,
                        Main.rand.NextBool() ? DustID.Shadowflame : DustID.PurpleTorch,
                        Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(2.5f, 11.5f),
                        0,
                        Color.Lerp(BladePurple, BladeBlood, Main.rand.NextFloat()),
                        Main.rand.NextFloat(1.25f, 2.4f));
                    dust.noGravity = true;
                }

                for (int i = 0; i < 10; i++)
                {
                    GeneralParticleHandler.SpawnParticle(new HeavySmokeParticle(
                        target.Center + Main.rand.NextVector2Circular(18f, 18f),
                        Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(1.5f, 5f),
                        Main.rand.NextBool(3) ? BladeDark : new Color(28, 0, 38),
                        Main.rand.Next(24, 42),
                        Main.rand.NextFloat(0.8f, 1.55f),
                        0.45f,
                        Main.rand.NextFloat(-0.12f, 0.12f),
                        false));
                }
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Vector2 drawPos = Projectile.Center - Main.screenPosition;
            const float magicDrawScale = 2.5f;

            // === 主颜色 ===
            Color mainColor = BladePurple;
            Color fadeColor = BladeBlood * 0.65f;

            // === 尾迹（稳定 oldPos 版本）===
            /*
            Texture2D pixel = TextureAssets.MagicPixel.Value;

            for (int i = 1; i < Projectile.oldPos.Length; i++)
            {
                if (Projectile.oldPos[i] == Vector2.Zero)
                    continue;

                Vector2 pos = Projectile.oldPos[i] + Projectile.Size / 2f - Main.screenPosition;
                float progress = i / (float)Projectile.oldPos.Length;

                float width = MathHelper.Lerp(1.2f, 0.1f, progress);
                Color color = Color.Lerp(mainColor, Color.Transparent, progress) * 1.25f;

                Vector2 dir = Projectile.oldPos[i - 1] - Projectile.oldPos[i];
                float rot = dir.ToRotation();

                Main.EntitySpriteDraw(
                    pixel,
                    pos,
                    null,
                    color,
                    rot,
                    new Vector2(0f, 0.5f),
                    new Vector2(dir.Length(), width),
                    SpriteEffects.None,
                    0
                );
            }

            // === BloomCircle（彻底修黑块）===
            */

            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;

            Main.spriteBatch.SetBlendState(BlendState.Additive);

            Main.EntitySpriteDraw(
                bloom,
                drawPos,
                null,
                BladeDark * 1.15f,
                0f,
                bloom.Size() / 2f,
                0.0625f * magicDrawScale,
                SpriteEffects.None
            );

            Main.EntitySpriteDraw(
                bloom,
                drawPos,
                null,
                mainColor * 1.2f,
                0f,
                bloom.Size() / 2f,
                0.036f * magicDrawScale,
                SpriteEffects.None
            );

            // === VerticalSmearRagged（修前伸过长）===
            Texture2D smear = ModContent.Request<Texture2D>("CalamityMod/Particles/VerticalSmear").Value;

            Vector2 forward = Projectile.velocity.SafeNormalize(Vector2.UnitX);

            // ❗ 原本是 full velocity 推进，这里削减 50%
            Vector2 smearPos = drawPos + forward * Projectile.velocity.Length() * 0.5f;
            smearPos = drawPos; // 直接用弹幕中心，不往前推

            float twistSpin = Main.GlobalTimeWrappedHourly * 8.8f + (float)Math.Sin(pulseAngle * 1.7f) * 0.42f;
            for (int i = 0; i < 9; i++)
            {
                float completion = i / 8f;
                float twist = (float)Math.Sin(twistSpin * 1.6f + i * 0.74f) * 0.26f;
                float rotation = Projectile.rotation + twistSpin * (i % 2 == 0 ? 0.42f : -0.32f) + i * MathHelper.TwoPi / 9f + twist;
                Color smearColor = i % 3 == 0 ? BladeDark : Color.Lerp(mainColor, BladeBlood, completion);
                smearColor.A = 0;

                Main.EntitySpriteDraw(
                    smear,
                    smearPos,
                    null,
                    smearColor * (0.68f - i * 0.045f),
                    rotation,
                    smear.Size() / 2f,
                    new Vector2(0.18f + i * 0.014f, 1.0f + i * 0.08f) * 0.08f * magicDrawScale,
                    SpriteEffects.None
                );
            }

            Texture2D star = ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/SimpleStar").Value;
            for (int i = 0; i < 4; i++)
            {
                Main.EntitySpriteDraw(
                    star,
                    drawPos,
                    null,
                    Color.Lerp(Color.White, mainColor, 0.45f) * (0.54f - i * 0.07f),
                    Projectile.rotation + i * MathHelper.PiOver2,
                    star.Size() / 2f,
                    new Vector2(0.55f, 1.55f + i * 0.24f) * 0.05f * magicDrawScale,
                    SpriteEffects.None
                );
            }

            Main.spriteBatch.SetBlendState(BlendState.AlphaBlend);

            return false;
        }

    }
}
