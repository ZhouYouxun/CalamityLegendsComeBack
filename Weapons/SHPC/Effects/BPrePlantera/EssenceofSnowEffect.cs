using CalamityLegendsComeBack.Weapons.SHPC.Effects.AAARules;
using CalamityMod.Items.Materials;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SHPC.Effects.BPrePlantera
{
    public class EssenceofSnowEffect : DefaultEffect
    {
        public override int EffectID => 6;

        public override int AmmoType => ModContent.ItemType<EssenceofEleum>();

        // 冰蓝主题（从图提取）
        public override Color ThemeColor => new Color(120, 220, 255);
        public override Color StartColor => new Color(200, 240, 255);
        public override Color EndColor => new Color(80, 160, 255);

        public override float SquishyLightParticleFactor => 1.35f;
        public override float ExplosionPulseFactor => 1.35f;

        public override void OnSpawn(Projectile projectile, Player owner)
        {
            // 初速度强化
            projectile.velocity *= 1.5f;

            // 生命周期缩短
            projectile.timeLeft = 40;

            // 提高更新频率（更丝滑）
            projectile.extraUpdates = 1;
        }

        public override void OnHitNPC(Projectile projectile, Player owner, NPC target, NPC.HitInfo hit, int damageDone)
        {
            // 霜冻效果（原版）
            target.AddBuff(BuffID.Frostburn, 180); // 3秒
        }

        public override void OnKill(Projectile projectile, Player owner, int timeLeft)
        {
            // ===== 计算前进方向 =====
            Vector2 forward = projectile.velocity.SafeNormalize(Vector2.UnitX);

            // ===== 生成液氮区域弹幕（先生成占位）=====
            Projectile.NewProjectile(
                projectile.GetSource_FromThis(),
                projectile.Center,
                forward * projectile.velocity.Length() * 2f, // 速度 = 原速度 ×2
                ModContent.ProjectileType<EssenceofSnow_N2>(), // ⚠️ 你后面实现
                (int)(projectile.damage * 1.25f),
                projectile.knockBack,
                projectile.owner
            );
        }
    }
}