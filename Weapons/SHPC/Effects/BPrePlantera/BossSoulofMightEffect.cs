using CalamityLegendsComeBack.Weapons.SHPC.Effects.AAARules;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;


namespace CalamityLegendsComeBack.Weapons.SHPC.Effects.BPrePlantera
{
    internal class BossSoulofMightEffect : DefaultEffect
    {
        public override int EffectID => 13;

        public override int AmmoType => ItemID.SoulofMight;

        public override Color ThemeColor => new Color(70, 110, 255);   // 力量核心蓝（高能电蓝）
        public override Color StartColor => new Color(150, 190, 255);  // 高亮（发光边缘）
        public override Color EndColor => new Color(20, 40, 120);      // 深蓝（阴影/核心）

        public override float SquishyLightParticleFactor => 0f;
        public override float ExplosionPulseFactor => 0f;
        public override float GlowScaleFactor => 0f;
        public override float GlowIntensityFactor => 0f;
        public override bool EnableDefaultSlowdown => false;
        public override void OnSpawn(Projectile projectile, Player owner)
        {
            projectile.GetGlobalProjectile<BossSoulofMight_GP>().firstFrame = true;
            projectile.timeLeft = 2;
            projectile.penetrate = -1;
            projectile.tileCollide = false;
        }

        public override void AI(Projectile projectile, Player owner)
        {
            BossSoulofMight_GP gp = projectile.GetGlobalProjectile<BossSoulofMight_GP>();
            if (!gp.firstFrame)
                return;

            gp.firstFrame = false;
            projectile.Kill();
        }

        public override void ModifyHitNPC(Projectile projectile, Player owner, NPC target, ref NPC.HitModifiers modifiers)
        {

        }

        public override void OnHitNPC(Projectile projectile, Player owner, NPC target, NPC.HitInfo hit, int damageDone)
        {

        }

        public override void OnKill(Projectile projectile, Player owner, int timeLeft)
        {
            Vector2 baseDirection = projectile.velocity.SafeNormalize(Vector2.UnitX);
            float baseSpeed = 12f;
            float centerAngle = baseDirection.ToRotation();
            float totalSpread = Main.rand.NextFloat(MathHelper.ToRadians(7f), MathHelper.ToRadians(10f));
            float halfSpread = totalSpread * 0.5f;

            for (int i = 0; i < 3; i++)
            {
                float offset = i switch
                {
                    0 => -halfSpread + Main.rand.NextFloat(0f, halfSpread * 0.25f),
                    1 => Main.rand.NextFloat(-halfSpread * 0.16f, halfSpread * 0.16f),
                    _ => halfSpread - Main.rand.NextFloat(0f, halfSpread * 0.25f)
                };

                float angle = centerAngle + offset;
                Vector2 velocity = angle.ToRotationVector2() * Main.rand.NextFloat(baseSpeed * 0.94f, baseSpeed * 1.08f);

                Projectile.NewProjectile(
                    projectile.GetSource_FromThis(),
                    projectile.Center,
                    velocity,
                    ModContent.ProjectileType<BossSoulofMight_Ball>(),
                    (int)(projectile.damage * 0.5f), // 50%伤害
                    projectile.knockBack,
                    projectile.owner
                );
            }
        }













    }

    internal class BossSoulofMight_GP : GlobalProjectile
    {
        public override bool InstancePerEntity => true;
        public bool firstFrame;
    }
}
