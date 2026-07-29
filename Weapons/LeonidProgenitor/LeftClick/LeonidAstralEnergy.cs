using System;
using CalamityLegendsComeBack.Weapons.LeonidProgenitor.Core;
using CalamityLegendsComeBack.Weapons.LeonidProgenitor.Effects;
using CalamityMod;
using CalamityMod.Dusts;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.LeonidProgenitor
{
    public class LeonidAstralEnergy : ModProjectile, ILocalizedModType
    {
        private const int DamageDelay = 30;
        private const int HomingDelay = 42;

        public new string LocalizationCategory => "Projectiles.LeonidProgenitor";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public ref float Timer => ref Projectile.ai[0];

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 32;
            Projectile.friendly = true;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 240;
            Projectile.DamageType = ModContent.GetInstance<RogueDamageClass>();
            Projectile.tileCollide = false;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 15;
        }

        public override void AI()
        {
            Timer++;

            // First travel harmlessly, then ease into a wide soft homing curve.
            if (Timer >= HomingDelay)
            {
                CalamityUtils.HomeInOnNPC(Projectile, true, 540f, 11f, 44f);
            }

            // 保留兵匠之傲星辉能量束的原版正弦双螺旋 Dust 特效
            Projectile.ai[1] += 0.18f;
            float angle = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
            float pulse = (float)Math.Sin(Projectile.ai[1]);
            float radius = 9f;
            Vector2 offset = angle.ToRotationVector2() * pulse * radius;
            
            Dust d1 = Dust.NewDustPerfect(Projectile.Center + offset, ModContent.DustType<AstralOrange>(), Vector2.Zero, Scale: 0.75f);
            if (d1 != null) d1.noGravity = true;
            
            Dust d2 = Dust.NewDustPerfect(Projectile.Center - offset, ModContent.DustType<AstralBlue>(), Vector2.Zero, Scale: 0.75f);
            if (d2 != null) d2.noGravity = true;

            // 兵匠之傲原版 GlowOrbParticle 渐变发光球粒子
            Color partColor = Color.Lerp(Color.Orange, Color.Blue, (float)Math.Sin(Main.GlobalTimeWrappedHourly * 3.5f));
            GlowOrbParticle obligatory = new(Projectile.Center, Vector2.Zero, false, 2, 0.8f, partColor);
            GeneralParticleHandler.SpawnParticle(obligatory);

            // 狮子座 (LeonidProgenitor) 专属的附加星光特效
            if (Main.rand.NextBool(10))
            {
                LeonidStarlight.Shed(
                    Projectile.Center,
                    Projectile.velocity * 0.4f,
                    Color.Lerp(LeonidVisualUtils.StratusBlue, LeonidVisualUtils.StarGold, 0.5f),
                    LeonidStarlightShape.Mote,
                    0.65f,
                    hoverTime: 14,
                    lifetime: 90
                );
            }
        }

        public override bool? CanDamage() => Timer < DamageDelay ? false : null;

        public override void OnKill(int timeLeft)
        {
            // 消散时的星辉与星光爆裂特效
            Color burstColor = Color.Lerp(Color.Orange, Color.Cyan, (float)Math.Sin(Main.GlobalTimeWrappedHourly * 2f));
            LeonidVisualUtils.SpawnDustBurst(Projectile.Center, burstColor, 14, 5f, 1.2f);
            LeonidStarlight.Burst(
                Projectile.Center,
                6,
                burstColor,
                LeonidStarlightShape.Mote,
                speed: 4f,
                scale: 0.75f,
                hoverTime: 18,
                lifetime: 100
            );
        }

        public override bool PreDraw(ref Color lightColor)
        {
            // 绘制额外的星芒核心
            Vector2 drawPos = Projectile.Center - Main.screenPosition;
            Color coreColor = Color.Lerp(Color.Orange, Color.Cyan, (float)Math.Sin(Main.GlobalTimeWrappedHourly * 3.5f));

            LeonidVisualUtils.BeginAdditiveSpriteBatch();
            LeonidVisualUtils.DrawBloom(Projectile.Center, coreColor * 0.4f, 0.35f);
            LeonidVisualUtils.DrawSparkle(Projectile.Center, Color.White, 0.6f, 0.45f, Projectile.velocity.ToRotation());
            LeonidVisualUtils.BeginAlphaBlendSpriteBatch();

            return false;
        }
    }
}
