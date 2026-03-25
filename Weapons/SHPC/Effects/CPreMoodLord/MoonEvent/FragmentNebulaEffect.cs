using CalamityLegendsComeBack.Weapons.SHPC.Effects.AAARules;
using CalamityMod;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SHPC.Effects.CPreMoodLord.MoonEvent
{
    public class FragmentNebulaEffect : DefaultEffect
    {
        public override int EffectID => 23;

        // 占位
        public override int AmmoType => ItemID.FragmentNebula;

        // ===== 星云主题色 =====
        public override Color ThemeColor => new Color(180, 80, 255);
        public override Color StartColor => new Color(220, 140, 255);
        public override Color EndColor => new Color(120, 40, 200);

        public override float SquishyLightParticleFactor => 1.55f;
        public override float ExplosionPulseFactor => 1.55f;

        // ================= OnSpawn =================
        public override void OnSpawn(Projectile projectile, Player owner)
        {
        }

        // ================= AI =================
        public override void AI(Projectile projectile, Player owner)
        {
            // ===== 简单追踪 =====
            NPC target = projectile.Center.ClosestNPCAt(900f);

            if (target != null)
            {
                Vector2 desiredDir = (target.Center - projectile.Center).SafeNormalize(Vector2.UnitX);

                float currentRot = projectile.velocity.ToRotation();
                float targetRot = desiredDir.ToRotation();

                float newRot = currentRot.AngleTowards(targetRot, MathHelper.ToRadians(4f));

                float speed = projectile.velocity.Length();

                // 稍微拉到稳定速度
                speed = MathHelper.Lerp(speed, 14f, 0.08f);

                projectile.velocity = newRot.ToRotationVector2() * speed;
            }

            // 抵消默认减速
            projectile.velocity *= 1.020408f;

            // ===== 星云拖尾 =====
            if (Main.rand.NextBool(2))
            {
                Dust d = Dust.NewDustPerfect(
                    projectile.Center,
                    DustID.PurpleCrystalShard,
                    Main.rand.NextVector2Circular(1.5f, 1.5f),
                    0,
                    default,
                    Main.rand.NextFloat(1.2f, 1.8f)
                );
                d.noGravity = true;
            }

            if (Main.rand.NextBool(3))
            {
                Dust d = Dust.NewDustPerfect(
                    projectile.Center,
                    DustID.PurpleTorch,
                    Main.rand.NextVector2Circular(1.2f, 1.2f),
                    0,
                    Color.MediumPurple,
                    Main.rand.NextFloat(1.0f, 1.5f)
                );
                d.noGravity = true;
            }
        }

        // ================= ModifyHitNPC =================
        public override void ModifyHitNPC(Projectile projectile, Player owner, NPC target, ref NPC.HitModifiers modifiers)
        {
        }

        // ================= OnHitNPC =================
        public override void OnHitNPC(Projectile projectile, Player owner, NPC target, NPC.HitInfo hit, int damageDone)
        {
        }

        // ================= OnKill =================
        public override void OnKill(Projectile projectile, Player owner, int timeLeft)
        {
            Vector2 center = projectile.Center;

            // ===== 外层：规则紫色环 =====
            int count = 24;
            for (int i = 0; i < count; i++)
            {
                float angle = MathHelper.TwoPi * i / count;
                Vector2 dir = angle.ToRotationVector2();

                Dust d = Dust.NewDustPerfect(
                    center + dir * 8f,
                    DustID.PurpleCrystalShard,
                    dir * 5f,
                    0,
                    default,
                    1.8f
                );
                d.noGravity = true;
            }

            // ===== 中层：能量爆散 =====
            for (int i = 0; i < 36; i++)
            {
                Dust d = Dust.NewDustPerfect(
                    center + Main.rand.NextVector2Circular(16f, 16f),
                    DustID.PurpleTorch,
                    Main.rand.NextVector2Circular(6f, 6f),
                    0,
                    Color.MediumPurple,
                    Main.rand.NextFloat(1.3f, 2.0f)
                );
                d.noGravity = true;
                d.fadeIn = 0.4f;
            }

            // ===== 内核：闪光核心 =====
            for (int i = 0; i < 12; i++)
            {
                Vector2 vel = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(3f, 6f);

                Dust d = Dust.NewDustPerfect(
                    center,
                    DustID.MagicMirror,
                    vel,
                    0,
                    Color.White,
                    1.4f
                );
                d.noGravity = true;
            }

            // ===== 光照 =====
            Lighting.AddLight(center, ThemeColor.ToVector3() * 0.8f);
        }











    }
}