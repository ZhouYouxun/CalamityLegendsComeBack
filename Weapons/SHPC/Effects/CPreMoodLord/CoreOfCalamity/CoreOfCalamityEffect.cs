using CalamityLegendsComeBack.Weapons.SHPC.Effects.AAARules;
using CalamityMod.Items.Materials;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SHPC.Effects.CPreMoodLord.CoreOfCalamity
{
    /// <summary>
    /// 灾劫核心的 SHPC 入口。
    /// 通用 SHPC 光球只负责承接伤害和发射方向，生成后立刻转换为专用灰色能量弹。
    /// </summary>
    internal sealed class CoreOfCalamityEffect : DefaultEffect
    {
        public const int CoreOfCalamityEffectID = 44;

        public override int EffectID => CoreOfCalamityEffectID;
        public override int AmmoType => ModContent.ItemType<CoreofCalamity>();
        public override Color ThemeColor => new(132, 138, 148);
        public override Color StartColor => new(228, 232, 238);
        public override Color EndColor => new(54, 58, 68);
        public override float SquishyLightParticleFactor => 0f;
        public override float ExplosionPulseFactor => 0f;
        public override float GlowScaleFactor => 0f;
        public override float GlowIntensityFactor => 0f;
        public override bool SuppressDefaultOnKillEffects => true;
        public override bool EnableDefaultSlowdown => false;
        public override bool PlayDefaultLeftClickFireSound => false;
        public override int LeftClickBurstCount => 4;

        public override void OnSpawn(Projectile projectile, Player owner)
        {
            if (projectile.owner == Main.myPlayer)
            {
                Vector2 direction = projectile.velocity.SafeNormalize(Vector2.UnitX * owner.direction);
                Projectile.NewProjectile(
                    projectile.GetSource_FromThis(),
                    projectile.Center + direction * 14f,
                    direction * 16.5f,
                    ModContent.ProjectileType<CoreOfCalamityEnergyOrb>(),
                    projectile.damage,
                    projectile.knockBack,
                    projectile.owner);
            }

            projectile.Kill();
        }

        public override void OnKill(Projectile projectile, Player owner, int timeLeft)
        {
        }
    }
}
