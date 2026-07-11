using System;
using CalamityMod;
using CalamityMod.Particles;
using CalamityLegendsComeBack;
using CalamityLegendsComeBack.Systems;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Dev.HyperdimensionalMatrixCore
{
    // ──────────────────────────────────────────────────────
    // SHARED · DATA LASER (telegraphed piercing beam)
    // ──────────────────────────────────────────────────────

    /// <summary>
    /// 数据激光：多个模组共用的蓄力直射光束弹幕。
    /// 生成时朝目标方向的单位向量存入 velocity 并冻结（自身不再位移），
    /// 蓄力阶段显示细线预警，随后炸开为一道贯穿式光束，短暂延续后消散。
    /// </summary>
    public sealed class MatrixDataLaser : ModProjectile, ILocalizedModType
    {
        private const int ChargeEnd = 18;
        private const int ActiveEnd = 34;
        private const int Lifetime  = 40;
        public const float BeamLength = 450f;
        private const float BeamWidth  = 15f;

        public new string LocalizationCategory => "Projectiles.HyperdimensionalMatrixCore";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private int Age => Lifetime - Projectile.timeLeft;
        private Vector2 Direction => Projectile.velocity.SafeNormalize(Vector2.UnitX);

        /// <summary>
        /// 以目标为激光的中点，反推出生成原点：沿 direction 方向命中目标后
        /// 再继续飞行半程，命中前也有半程可见——即"居中重置"。
        /// </summary>
        public static Vector2 GetCenteredOrigin(Vector2 targetCenter, Vector2 direction)
        {
            Vector2 dir = direction.SafeNormalize(Vector2.UnitX);
            return targetCenter - dir * (BeamLength * 0.5f);
        }

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.MinionShot[Type] = true;
            ProjectileID.Sets.DrawScreenCheckFluff[Type] = 900;
        }

        public override void SetDefaults()
        {
            Projectile.width = 10;
            Projectile.height = 10;
            Projectile.friendly = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = Lifetime;
            Projectile.DamageType = DamageClass.Summon;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 8;
        }

        public override bool ShouldUpdatePosition() => false;

        public override void OnSpawn(IEntitySource source)
        {
            if (Main.dedServ)
                return;

            Color c = HyperdimensionalMatrixVisuals.GetDataColor(0.3f);
            for (int i = 0; i < 10; i++)
            {
                Vector2 vel = Main.rand.NextVector2Unit() * Main.rand.NextFloat(0.5f, 2f);
                GeneralParticleHandler.SpawnParticle(new GlowOrbParticle(
                    Projectile.Center, vel, false, 10 + Main.rand.Next(8), 0.4f, c, true, false, false));
            }
        }

        public override bool? CanDamage() => Age >= ChargeEnd && Age < ActiveEnd ? null : false;

        public override void AI()
        {
            int age = Age;
            Projectile.rotation = Direction.ToRotation();

            if (age == ChargeEnd && !Main.dedServ)
            {
                Color c = HyperdimensionalMatrixVisuals.GetDataColor(0.35f);
                CLCBLightingBoltsSystem.Spawn_MatrixDataBurst(Projectile.Center, c, 0.55f);

                if (Main.LocalPlayer.active)
                {
                    float sd = Vector2.Distance(Main.LocalPlayer.Center, Projectile.Center);
                    if (sd < 500f)
                        Main.LocalPlayer.Calamity().GeneralScreenShakePower = Math.Max(
                            Main.LocalPlayer.Calamity().GeneralScreenShakePower, 1.4f * (1f - sd / 500f));
                }
            }

            float glowStrength = age >= ChargeEnd && age < ActiveEnd ? 0.7f : 0.25f;
            Lighting.AddLight(Projectile.Center, HyperdimensionalMatrixVisuals.GetDataColor(age * 0.02f).ToVector3() * glowStrength);
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            int age = Age;
            if (age < ChargeEnd || age >= ActiveEnd)
                return false;

            Vector2 start = Projectile.Center;
            Vector2 end = start + Direction * BeamLength;
            float collisionPoint = 0f;
            return Collision.CheckAABBvLineCollision(
                targetHitbox.TopLeft(), targetHitbox.Size(), start, end, BeamWidth, ref collisionPoint);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            int age = Age;
            Vector2 dir = Direction;
            Vector2 start = Projectile.Center;
            Color baseColor = HyperdimensionalMatrixVisuals.GetDataColor(0.3f);
            float t = Main.GlobalTimeWrappedHourly;

            if (age < ChargeEnd)
            {
                // Telegraph: thin warning line, brightening as the charge builds
                float chargePct = age / (float)ChargeEnd;
                Vector2 end = start + dir * BeamLength;
                Color warnColor = baseColor * (0.15f + chargePct * 0.45f);
                Main.spriteBatch.DrawLineBetter(start, end, warnColor, 0.6f + chargePct * 1.2f);

                float pulse = 3f + 2f * chargePct * (float)Math.Sin(t * 14f);
                HyperdimensionalMatrixVisuals.DrawNode(start, baseColor * (0.4f + chargePct * 0.5f), pulse);
                HyperdimensionalMatrixVisuals.DrawScanRing(start, 14f + chargePct * 10f, t * 3f,
                    baseColor * (0.3f + chargePct * 0.4f), 16, 1.2f);
            }
            else if (age < ActiveEnd)
            {
                // Active beam: layered bright core + glow + flowing data ticks along its length
                float activePct = (age - ChargeEnd) / (float)(ActiveEnd - ChargeEnd);
                float fade = activePct < 0.15f ? activePct / 0.15f : 1f;
                Vector2 end = start + dir * BeamLength;

                Main.spriteBatch.DrawLineBetter(start, end, baseColor * (0.25f * fade), BeamWidth * 1.8f);
                Main.spriteBatch.DrawLineBetter(start, end, baseColor * fade, BeamWidth * 0.55f);
                Main.spriteBatch.DrawLineBetter(start, end, Color.White with { A = 0 } * (fade * 0.85f), BeamWidth * 0.16f);

                for (int i = 0; i < 6; i++)
                {
                    float flow = (t * 2.4f + i / 6f) % 1f;
                    Color tickColor = HyperdimensionalMatrixVisuals.GetDataColor(i * 0.12f, fade);
                    HyperdimensionalMatrixVisuals.DrawNode(Vector2.Lerp(start, end, flow), tickColor, 4f);
                }

                HyperdimensionalMatrixVisuals.DrawNode(start, Color.White with { A = 0 } * fade, 7f);
                HyperdimensionalMatrixVisuals.DrawScanRing(start, 20f, t * 3f, baseColor * (fade * 0.6f), 16, 1.5f);
            }
            else
            {
                // Quick fade-out
                float fadePct = (age - ActiveEnd) / (float)(Lifetime - ActiveEnd);
                Vector2 end = start + dir * BeamLength;
                Main.spriteBatch.DrawLineBetter(start, end, baseColor * (0.4f * (1f - fadePct)), BeamWidth * 0.3f * (1f - fadePct));
            }

            return false;
        }

        public override void OnKill(int timeLeft)
        {
            if (Main.dedServ)
                return;

            Color c = HyperdimensionalMatrixVisuals.GetDataColor(0.3f);
            for (int i = 0; i < 6; i++)
            {
                Vector2 vel = Main.rand.NextVector2Unit() * Main.rand.NextFloat(1f, 3f);
                GeneralParticleHandler.SpawnParticle(new GlowOrbParticle(
                    Projectile.Center, vel, false, 8 + Main.rand.Next(6), 0.4f, c, true, false, false));
            }
        }
    }
}
