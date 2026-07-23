using System;
using CalamityLegendsComeBack.Weapons.AegisBlade.Visuals;
using CalamityMod;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.AegisBlade.Projectiles
{
    /// <summary>
    /// 左键命中时在敌人身上炸开的圣火爆点。
    /// 伤害窗口仍然只有起始 3 帧，其余全部是视觉余辉：焦痕贴花 + 收缩日核 + 符文闪。
    /// </summary>
    public class AegisSparkExplosion : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private const int Lifetime = 22;
        private const int DamageFrames = 3;   // 与旧版一致：只有最初 3 帧能造成伤害

        /// <summary>1 → 0 的余辉进度。</summary>
        private float Fade => Projectile.timeLeft / (float)Lifetime;

        /// <summary>0 → 1 的展开进度，用于让焦痕"烧开"。</summary>
        private float Bloom => Utils.GetLerpValue(Lifetime, Lifetime - 7f, Projectile.timeLeft, true);

        public override void SetDefaults()
        {
            Projectile.width = 75;
            Projectile.height = 75;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = Lifetime;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }

        public override bool ShouldUpdatePosition() => false;

        public override bool? CanDamage() => Projectile.timeLeft >= Lifetime - DamageFrames ? null : false;

        public override void AI()
        {
            AegisVisuals.Light(Projectile.Center, 1.1f * Fade);

            if (Projectile.localAI[0]++ > 0f)
                return;

            SoundEngine.PlaySound(SoundID.Item14 with { Volume = 0.62f, Pitch = -0.25f }, Projectile.Center);
            SoundEngine.PlaySound(SoundID.Item100 with { Volume = 0.45f, Pitch = 0.4f }, Projectile.Center);

            if (Main.dedServ)
                return;

            // ── 统一的三重圣火爆闪 ──
            AegisVisuals.HolyDetonation(Projectile.Center, 1.15f);
            AegisVisuals.CoronaRing(Projectile.Center, 12, 0.95f, Main.rand.NextFloat(MathHelper.TwoPi));

            // ── 正义旗式：火从四面被"吸"进伤口 ──
            AegisVisuals.WarbannerConverge(Projectile.Center,
                Main.rand.NextFloat(MathHelper.TwoPi).ToRotationVector2(), 1.8f, 5, 1.05f);

            // ── 圣火尘 ──
            for (int i = 0; i < 16; i++)
            {
                Vector2 velocity = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(2.6f, 9.2f);
                Dust ember = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(18f, 18f),
                    AegisVisuals.ProfanedFireDust, velocity, 0, Color.White, Main.rand.NextFloat(1f, 1.8f));
                ember.noGravity = true;
            }

            // ── 被震下来的碎屑：呼应这把武器的"庇护土墙"土系身份，
            //    但配色改走余烬/焦黑，不再是一撮金沙 ──
            for (int i = 0; i < 10; i++)
            {
                Vector2 velocity = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(1.4f, 5.8f);
                Dust debris = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(20f, 20f),
                    DustID.Dirt, velocity, 0,
                    Color.Lerp(AegisVisuals.Charred, AegisVisuals.Ember, Main.rand.NextFloat(0.2f, 0.8f)),
                    Main.rand.NextFloat(0.85f, 1.35f));
                debris.noGravity = Main.rand.NextBool(3);
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.dedServ)
                return false;

            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            float fade = Fade;
            float bloom = Bloom;

            Main.spriteBatch.EnterShaderRegion(BlendState.Additive);

            // ① 焦痕：先"烧开"再慢慢冷却
            AegisVisuals.DrawScorchDecal(drawPosition, Projectile.identity * 1.31f,
                MathHelper.Lerp(18f, 58f, bloom), fade);

            // ② 命中符文闪：只在最初几帧出现，是"打中了"的强信号
            if (bloom < 1f)
            {
                AegisVisuals.DrawRuneSigil(drawPosition, MathHelper.Lerp(26f, 74f, bloom),
                    Projectile.identity + bloom * 3.4f, (1f - bloom) * 0.9f, Vector2.One, 1.2f);
            }

            // ③ 收缩的日核
            AegisVisuals.DrawSolarCore(drawPosition, MathHelper.Lerp(34f, 12f, 1f - fade), fade,
                Main.GlobalTimeWrappedHourly * 6f + Projectile.identity);

            Main.spriteBatch.ExitShaderRegion();
            return false;
        }
    }
}
