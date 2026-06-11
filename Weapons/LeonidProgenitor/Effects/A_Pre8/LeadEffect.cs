using CalamityLegendsComeBack.Weapons.LeonidProgenitor.Effects.Shared;
using Terraria;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.LeonidProgenitor.Effects.A_Pre8
{
    public class LeadEffect : LeonidMetalEffect
    {
        public override int EffectID => 4;

        protected override int EnergyVariant => 3;
        protected override float EnergySizeFactor => 1.04f;
        protected override int EnergyMoteCount => 2;
        protected override int EnergyDustInterval => 18;
        protected override float EnergyOpacity => 0.24f;

        public override void OnHitNPC(LeonidCometSmall meteor, Player owner, NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (Main.myPlayer != meteor.Projectile.owner)
                return;

            int shockwave = Projectile.NewProjectile(meteor.Projectile.GetSource_FromThis(), target.Center, Microsoft.Xna.Framework.Vector2.Zero, ModContent.ProjectileType<Shared_Shockwave>(), meteor.Projectile.damage / 2, 0f, meteor.Projectile.owner);
            if (shockwave >= 0 && shockwave < Main.maxProjectiles)
                Main.projectile[shockwave].DamageType = meteor.Projectile.DamageType;
        }
    }
}
