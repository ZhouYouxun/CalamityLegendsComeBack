using Terraria;

namespace CalamityLegendsComeBack.Weapons.LeonidProgenitor.Effects.A_Pre8
{
    public class TinEffect : LeonidMetalEffect
    {
        public override int EffectID => 2;

        protected override int EnergyVariant => 0;
        protected override float EnergySizeFactor => 0.76f;
        protected override int EnergyMoteCount => 2;
        protected override int EnergyDustInterval => 13;
        protected override float EnergySpinOffset => 0.35f;

        public override bool OnTileCollide(LeonidCometSmall meteor, Player owner, Microsoft.Xna.Framework.Vector2 oldVelocity)
        {
            int bounceCount = (int)meteor.GetState("tin_bounce");
            if (bounceCount >= 2)
                return true;

            meteor.SetState("tin_bounce", bounceCount + 1);
            if (meteor.Projectile.velocity.X != oldVelocity.X)
                meteor.Projectile.velocity.X = -oldVelocity.X;

            if (meteor.Projectile.velocity.Y != oldVelocity.Y)
                meteor.Projectile.velocity.Y = -oldVelocity.Y;

            return false;
        }
    }
}
