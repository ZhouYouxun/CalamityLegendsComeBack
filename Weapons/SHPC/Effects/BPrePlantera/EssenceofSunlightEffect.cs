using CalamityLegendsComeBack.Weapons.SHPC.Effects.AAARules;
using CalamityMod.Items.Materials;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SHPC.Effects.BPrePlantera
{
    public class EssenceofSunlightEffect : DefaultEffect
    {
        public override int EffectID => 7;

        public override int AmmoType => ModContent.ItemType<EssenceofSunlight>();

        // 日光金色
        public override Color ThemeColor => new Color(255, 220, 90);
        public override Color StartColor => new Color(255, 255, 160);
        public override Color EndColor => new Color(255, 180, 60);

        public override float SquishyLightParticleFactor => 1.35f;
        public override float ExplosionPulseFactor => 1.35f;

        public override void OnSpawn(Projectile projectile, Player owner)
        {
            // 生命周期
            projectile.timeLeft = 160;
        }

        public override void AI(Projectile projectile, Player owner)
        {
            // 持续加速
            projectile.velocity *= 1.035f;
        }

        public override void OnHitNPC(Projectile projectile, Player owner, NPC target, NPC.HitInfo hit, int damageDone)
        {
            // 传递标记（不刷新计时）
            var gnpc = target.GetGlobalNPC<EssenceofSunlight_GNPC>();

            if (!gnpc.marked)
            {
                gnpc.marked = true;
                gnpc.timer = 0;
                gnpc.owner = projectile.owner;
            }
        }





    }
}