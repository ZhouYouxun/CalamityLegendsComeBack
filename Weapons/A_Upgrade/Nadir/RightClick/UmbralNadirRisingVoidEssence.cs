using System;
using CalamityLegendsComeBack.Weapons.A_Upgrade.Nadir.General;
using CalamityMod;
using CalamityMod.Buffs.DamageOverTime;
using CalamityMod.Dusts;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Upgrade.Nadir.RightClick
{
    /// <summary>
    /// 从旧版 NadirJavVoidEssence 本地移植的上升虚空核。
    /// 它自命中点正下方的大范围阴影中向上飞来，先无伤害，随后才开始锁敌。
    /// </summary>
    public class UmbralNadirRisingVoidEssence : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.Nadir";
        public override string Texture => "CalamityLegendsComeBack/Weapons/A_Upgrade/Nadir/RightClick/UmbralNadirRisingVoidEssence";

        private const int FrameCount = 4;
        private const int FrameTime = 12;
        private const float DamageStartTime = 20f;
        private const float HomingStartTime = 50f;
        private static readonly Color VoidWhite = new(216, 246, 255);

        private ref float Time => ref Projectile.ai[1];
        private bool StartFading;

        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = FrameCount;
            ProjectileID.Sets.TrailCacheLength[Type] = 6;
            ProjectileID.Sets.TrailingMode[Type] = 0;
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 24;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.timeLeft = 350;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.alpha = 80;
            Projectile.penetrate = 2;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 4;
            Projectile.extraUpdates = 1;
        }

        public override bool? CanDamage() => Time >= DamageStartTime;

        public override void AI()
        {
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
            DrawOffsetX = 1;
            DrawOriginOffsetY = 4;

            if (++Projectile.frameCounter > FrameTime)
            {
                Projectile.frame = (Projectile.frame + 1) % FrameCount;
                Projectile.frameCounter = 0;
            }

            Lighting.AddLight(Projectile.Center, VoidWhite.ToVector3() * 0.35f);
            if (Main.rand.NextBool(2))
            {
                Dust dust = Dust.NewDustPerfect(Projectile.Center, ModContent.DustType<VoidDustInverted>(),
                    -Projectile.velocity * 0.12f, 0, VoidWhite, Main.rand.NextFloat(0.65f, 1.05f));
                dust.noGravity = true;
            }

            // 保留旧版节奏：前段只是从下方穿出，约 25 个真实帧后才开始主动锁定。
            if (Time > HomingStartTime)
            {
                NPC target = Projectile.Center.ClosestNPCAt(500f);
                if (target != null)
                    SpiralHome(target);
            }

            if (StartFading)
                Projectile.alpha += 12;
            Time++;
        }

        private void SpiralHome(NPC target)
        {
            Vector2 toTarget = target.Center - Projectile.Center;
            float distance = toTarget.Length();
            float homingCompletion = MathHelper.Clamp((Time - HomingStartTime) / 100f, 0f, 1f);

            // 每颗虚空精华用黄金角错开初相，并按不同方向绕目标收束；靠近时螺旋半径会压缩到命中尺度。
            float spiralDirection = (Projectile.identity & 1) == 0 ? 1f : -1f;
            float phase = Projectile.identity * 2.39996323f + spiralDirection * Time * MathHelper.Lerp(0.18f, 0.32f, homingCompletion);
            float orbitRadius = MathHelper.Clamp(distance * MathHelper.Lerp(0.4f, 0.08f, homingCompletion), 8f, 135f);
            Vector2 orbitPoint = target.Center + phase.ToRotationVector2() * orbitRadius;
            float speed = MathHelper.Lerp(12f, 17f, homingCompletion);
            Vector2 desiredVelocity = (orbitPoint - Projectile.Center).SafeNormalize(toTarget.SafeNormalize(Vector2.UnitY)) * speed;

            Projectile.velocity = Vector2.Lerp(Projectile.velocity, desiredVelocity, MathHelper.Lerp(0.13f, 0.26f, homingCompletion));
            if (Projectile.velocity.Length() > speed)
                Projectile.velocity = Projectile.velocity.SafeNormalize(Vector2.UnitY) * speed;

            if (Main.rand.NextBool(2))
            {
                Vector2 orbitDustPosition = Projectile.Center + phase.ToRotationVector2().RotatedBy(MathHelper.PiOver2) * MathF.Min(orbitRadius, 48f);
                Dust dust = Dust.NewDustPerfect(orbitDustPosition, ModContent.DustType<VoidDustInverted>(),
                    -Projectile.velocity * 0.06f, 0, VoidWhite, Main.rand.NextFloat(0.5f, 0.82f));
                dust.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            CalamityUtils.DrawAfterimagesCentered(Projectile, ProjectileID.Sets.TrailingMode[Type], lightColor, 1);
            return false;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(ModContent.BuffType<Voidfrost>(), 90);
            UmbralCorrosionGlobalNPC.AddStacks(target, 1);
            Projectile.velocity *= 0.4f;
            StartFading = true;

            for (int i = 0; i < 8; i++)
            {
                Dust dust = Dust.NewDustPerfect(Projectile.Center, ModContent.DustType<VoidDustInverted>(),
                    Main.rand.NextVector2Unit() * Main.rand.NextFloat(1.5f, 4.2f), 0,
                    i % 2 == 0 ? Color.Black : VoidWhite, Main.rand.NextFloat(0.8f, 1.3f));
                dust.noGravity = true;
            }
        }

        public override void OnKill(int timeLeft)
        {
            for (int i = 0; i < 18; i++)
            {
                Dust dust = Dust.NewDustPerfect(Projectile.Center, ModContent.DustType<VoidDustInverted>(),
                    Main.rand.NextVector2Unit() * Main.rand.NextFloat(1.6f, 4.8f), 0,
                    i % 2 == 0 ? Color.Black : VoidWhite, Main.rand.NextFloat(0.7f, 1.25f));
                dust.noGravity = true;
            }
        }
    }
}
