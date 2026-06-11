using CalamityLegendsComeBack.Weapons.SHPC.Effects.AAARules;
using CalamityMod.Items.LoreItems;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SHPC.Effects.EAfterDog.Cynosure
{
    /// <summary>
    /// SHPC 的 Cynosure 入口。普通能量球出生后会立即转换为重型穿甲弹。
    /// </summary>
    internal class CynosureEffect : DefaultEffect
    {
        public const int CynosureEffectID = 43;

        public override int EffectID => CynosureEffectID;
        public override int AmmoType => ModContent.ItemType<LoreCynosure>();
        public override int ShotsPerAmmo => 999;
        public override Color ThemeColor => new(38, 172, 255);
        public override Color StartColor => Color.White;
        public override Color EndColor => new(12, 92, 255);
        public override float SquishyLightParticleFactor => 0f;
        public override float ExplosionPulseFactor => 0f;
        public override float GlowScaleFactor => 0f;
        public override float GlowIntensityFactor => 0f;
        public override bool EnableDefaultSlowdown => false;
        public override bool PlayDefaultLeftClickFireSound => false;

        public override void OnSpawn(Projectile projectile, Player owner)
        {
            if (projectile.owner == Main.myPlayer)
            {
                Vector2 direction = projectile.velocity.SafeNormalize(Vector2.UnitX * owner.direction);
                Projectile.NewProjectile(projectile.GetSource_FromThis(), projectile.Center, direction * 27.2f,
                    ModContent.ProjectileType<CynosureArmorPiercingRound>(), projectile.damage, projectile.knockBack,
                    projectile.owner);
            }

            // 普通 SHPC 光球只承担一次转换，不参与后续爆炸。
            projectile.Kill();
        }

        public override void OnKill(Projectile projectile, Player owner, int timeLeft)
        {
        }
    }
}
