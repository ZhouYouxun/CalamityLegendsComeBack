using CalamityLegendsComeBack.Weapons.SHPC.Effects.AAARules;
using Terraria;
using Terraria.ID;
using Microsoft.Xna.Framework;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SHPC.Effects.BPrePlantera
{
    internal class SoulofLightEffect : DefaultEffect
    {
        public override int EffectID => 9;
        public override int AmmoType => ItemID.SoulofLight;

        // ===== 三段粉色 =====
        public override Color ThemeColor => new Color(255, 120, 200);
        public override Color StartColor => new Color(255, 180, 230);
        public override Color EndColor => new Color(255, 80, 160);

        public override float SquishyLightParticleFactor => 1.1f;
        public override float ExplosionPulseFactor => 1.1f;
        public override bool EnableDefaultSlowdown => false;

        public override void OnSpawn(Projectile projectile, Player owner)
        {
            // ===== 穿透设置 =====
            projectile.tileCollide = false;
            projectile.penetrate = 3;

            // ===== 初速度提升 =====
            projectile.velocity *= 1.1f;

            // ===== 生成3个伴随弹幕 =====
            for (int i = 0; i < 3; i++)
            {
                int id = Projectile.NewProjectile(
                    projectile.GetSource_FromThis(),
                    projectile.Center,
                    Vector2.Zero,
                    ModContent.ProjectileType<NewSHPS>(),
                    projectile.damage,
                    projectile.knockBack,
                    projectile.owner,
                    0,                      // presetIndex = 0
                    projectile.whoAmI       // 绑定主弹幕
                );
            }
        }

        public override void AI(Projectile projectile, Player owner)
        {
            // ===== 屏幕反弹 =====
            Rectangle screenRect = new Rectangle(0, 0, Main.screenWidth, Main.screenHeight);
            Vector2 screenPosition = projectile.Center - Main.screenPosition;

            if (!screenRect.Contains(screenPosition.ToPoint()))
            {
                if (screenPosition.X <= 0 || screenPosition.X >= Main.screenWidth)
                    projectile.velocity.X *= -1;

                if (screenPosition.Y <= 0 || screenPosition.Y >= Main.screenHeight)
                    projectile.velocity.Y *= -1;
            }
        }

        public override void ModifyHitNPC(Projectile projectile, Player owner, NPC target, ref NPC.HitModifiers modifiers) { }
        public override void OnHitNPC(Projectile projectile, Player owner, NPC target, NPC.HitInfo hit, int damageDone) { }
        public override void OnKill(Projectile projectile, Player owner, int timeLeft) { }
    }
}