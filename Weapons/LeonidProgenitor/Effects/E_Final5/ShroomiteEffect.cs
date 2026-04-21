using CalamityLegendsComeBack.Weapons.LeonidProgenitor.Helpers;
using Terraria;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.LeonidProgenitor.Effects.E_Final5
{
    public class ShroomiteEffect : LeonidMetalEffect
    {
        public override int EffectID => 24;

        public override void AI(LeonidCometSmall meteor, Player owner)
        {
            int timer = (int)meteor.GetState("shroom_timer") + 1;
            meteor.SetState("shroom_timer", timer);
            if (timer % 8 != 0 || Main.myPlayer != meteor.Projectile.owner)
                return;

            Projectile.NewProjectile(meteor.Projectile.GetSource_FromThis(), meteor.Projectile.Center, meteor.Projectile.velocity * 0.12f, ModContent.ProjectileType<LeonidTrailMushroom>(), 0, 0f, meteor.Projectile.owner);
        }
    }
}
