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
        private static readonly Color BladePurple = new(145, 90, 220);
        private static readonly Color BladeDark = new(60, 12, 95);

        public new string LocalizationCategory => "Projectiles.SHPC";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        // 飞行计时器
        private int flightTimer;

        // 螺旋尾迹角度
        private float helixAngle;

        // 脉冲光效角度
        private float pulseAngle;

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 150;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = 5;
            Projectile.timeLeft = 240;
            Projectile.extraUpdates = 2;
            Projectile.Opacity = 1f;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 18;
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
            Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitY);
            Vector2 side = direction.RotatedBy(MathHelper.PiOver2);

            // 二阶段原本的螺旋拖尾
            float spiralOffsetAmount = 12f + (float)Math.Sin(helixAngle) * 6f;
            Vector2 spiralOffset = side * spiralOffsetAmount;

            GeneralParticleHandler.SpawnParticle(new CustomSpark(
                Projectile.Center + spiralOffset,
                -Projectile.velocity * 0.28f + side * 0.4f,
                "CalamityMod/Particles/BloomCircle",
                false,
                10,
                0.4f,
                Color.Lerp(BladePurple, Color.White, 0.15f),
                new Vector2(0.35f, 0.9f),
                true,
                false,
                Main.rand.NextFloat(-0.1f, 0.1f),
                false,
                false,
                0.94f));

            if (firstSubstep)
            {
                Dust dust = Dust.NewDustPerfect(
                    Projectile.Center + spiralOffset,
                    Main.rand.NextBool() ? DustID.Shadowflame : DustID.PurpleTorch,
                    -Projectile.velocity * 0.08f + side * Main.rand.NextFloat(0.2f, 0.6f),
                    0,
                    Color.Lerp(BladePurple, BladeDark, Main.rand.NextFloat(0.1f, 0.55f)),
                    Main.rand.NextFloat(1f, 1.4f));
                dust.noGravity = true;

                if (Main.rand.NextBool(2))
                {
                    GeneralParticleHandler.SpawnParticle(new AltSparkParticle(
                        Projectile.Center + Main.rand.NextVector2Circular(5f, 5f),
                        (-direction).RotatedByRandom(0.45f) * Main.rand.NextFloat(1.2f, 3.6f),
                        false,
                        Main.rand.Next(10, 16),
                        Main.rand.NextFloat(0.65f, 1f),
                        Color.Lerp(BladePurple, BladeDark, Main.rand.NextFloat(0.2f, 0.7f))));
                }

                // 把原一阶段的“蓄能感”脉冲并到现在的直线飞行里
                if (flightTimer % 4 == 0)
                {
                    float chargePulse = 0.5f + 0.5f * (float)Math.Sin(pulseAngle);
                    Vector2 ringOffset = (pulseAngle * 1.55f).ToRotationVector2() * (8f + chargePulse * 10f);

                    GeneralParticleHandler.SpawnParticle(new CustomPulse(
                        Projectile.Center,
                        Vector2.Zero,
                        Color.Lerp(BladeDark, BladePurple, chargePulse) * 0.45f,
                        "CalamityMod/Particles/LargeBloom",
                        new Vector2(0.8f, 1.4f),
                        Main.rand.NextFloat(-0.15f, 0.15f),
                        0.18f + chargePulse * 0.08f,
                        0f,
                        8,
                        false));

                    Dust warningDust = Dust.NewDustPerfect(
                        Projectile.Center + ringOffset,
                        Main.rand.NextBool() ? DustID.Shadowflame : DustID.PurpleTorch,
                        (-ringOffset).SafeNormalize(Vector2.UnitY) * Main.rand.NextFloat(0.3f, 1.2f),
                        0,
                        Color.Lerp(BladePurple, Color.White, 0.2f),
                        Main.rand.NextFloat(0.9f, 1.3f));
                    warningDust.noGravity = true;
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
                for (int i = 0; i < 2; i++)
                {
                    GeneralParticleHandler.SpawnParticle(new CustomPulse(
                        target.Center,
                        Vector2.Zero,
                        i == 0 ? BladePurple : Color.White * 0.45f,
                        "CalamityMod/Particles/BloomCircle",
                        Vector2.One,
                        Main.rand.NextFloat(-0.2f, 0.2f),
                        0.45f - i * 0.12f,
                        0f,
                        12,
                        true));
                }

                for (int i = 0; i < 14; i++)
                {
                    Dust dust = Dust.NewDustPerfect(
                        target.Center,
                        Main.rand.NextBool() ? DustID.Shadowflame : DustID.PurpleTorch,
                        Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(1.8f, 6.4f),
                        0,
                        Color.Lerp(BladePurple, BladeDark, Main.rand.NextFloat()),
                        Main.rand.NextFloat(1f, 1.45f));
                    dust.noGravity = true;
                }
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Vector2 drawPos = Projectile.Center - Main.screenPosition;

            // === 主颜色 ===
            Color mainColor = new Color(120, 255, 200);
            Color fadeColor = mainColor * 0.5f;

            // === 尾迹（稳定 oldPos 版本）===
            Texture2D pixel = TextureAssets.MagicPixel.Value;

            for (int i = 1; i < Projectile.oldPos.Length; i++)
            {
                if (Projectile.oldPos[i] == Vector2.Zero)
                    continue;

                Vector2 pos = Projectile.oldPos[i] + Projectile.Size / 2f - Main.screenPosition;
                float progress = i / (float)Projectile.oldPos.Length;

                float width = MathHelper.Lerp(6f, 1f, progress);
                Color color = Color.Lerp(mainColor, Color.Transparent, progress) * 0.8f;

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
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;

            Main.spriteBatch.SetBlendState(BlendState.Additive);

            Main.EntitySpriteDraw(
                bloom,
                drawPos,
                null,
                mainColor * 0.8f,
                0f,
                bloom.Size() / 2f,
                0.6f,
                SpriteEffects.None
            );

            Main.EntitySpriteDraw(
                bloom,
                drawPos,
                null,
                Color.White * 0.6f,
                0f,
                bloom.Size() / 2f,
                0.3f,
                SpriteEffects.None
            );

            // === VerticalSmearRagged（修前伸过长）===
            Texture2D smear = ModContent.Request<Texture2D>("CalamityMod/Particles/VerticalSmearRagged").Value;

            Vector2 forward = Projectile.velocity.SafeNormalize(Vector2.UnitX);

            // ❗ 原本是 full velocity 推进，这里削减 50%
            Vector2 smearPos = drawPos + forward * Projectile.velocity.Length() * 0.5f;
            smearPos = drawPos; // 直接用弹幕中心，不往前推

            Main.EntitySpriteDraw(
                smear,
                smearPos,
                null,
                mainColor * 0.5f,
                Projectile.rotation,
                smear.Size() / 2f,
                0.27f,
                SpriteEffects.None
            );

            Main.spriteBatch.SetBlendState(BlendState.AlphaBlend);

            return false;
        }

    }
}