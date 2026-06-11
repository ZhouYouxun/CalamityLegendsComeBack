using CalamityLegendsComeBack.Weapons.LeonidProgenitor.Effects.Shared;
using Terraria;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.LeonidProgenitor.Effects.E_Final5
{
    public class SpectreEffect : LeonidMetalEffect
    {
        public override int EffectID => 25;

        protected override int EnergyVariant => 7;
        protected override float EnergySizeFactor => 0.92f;
        protected override int EnergyMoteCount => 4;
        protected override int EnergyDustInterval => 12;
        protected override float EnergyOpacity => 0.24f;

        public override void OnSpawn(LeonidCometSmall meteor, Player owner)
        {
            if (meteor.HasFlag("spectre_clone") || Main.myPlayer != meteor.Projectile.owner)
                return;

            meteor.SetFlag("spectre_clone");
            int clone = Projectile.NewProjectile(meteor.Projectile.GetSource_FromThis(), meteor.InitialCenter, meteor.Projectile.velocity * 2f, ModContent.ProjectileType<Shared_SplitMeteor>(), meteor.Projectile.damage / 2, meteor.Projectile.knockBack, meteor.Projectile.owner, 1f);
            if (clone >= 0 && clone < Main.maxProjectiles)
            {
                Main.projectile[clone].DamageType = meteor.Projectile.DamageType;
                Main.projectile[clone].netUpdate = true;
            }
        }
    }
}
