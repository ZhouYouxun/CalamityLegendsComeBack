using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
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

            int mushroom = Projectile.NewProjectile(meteor.Projectile.GetSource_FromThis(), meteor.Projectile.Center, meteor.Projectile.velocity * 0.08f, ProjectileID.Mushroom, 0, 0f, meteor.Projectile.owner); // ProjectileID.Mushroom = 131.
            if (mushroom >= 0 && mushroom < Main.maxProjectiles)
            {
                Main.projectile[mushroom].friendly = false;
                Main.projectile[mushroom].hostile = false;
                Main.projectile[mushroom].tileCollide = false;
                Main.projectile[mushroom].timeLeft = 42;
                Main.projectile[mushroom].alpha = 80;
                Main.projectile[mushroom].scale = 0.45f;
                Main.projectile[mushroom].rotation = Main.rand.NextFloat(MathHelper.TwoPi);
            }
        }
    }
}
