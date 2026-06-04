using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;

namespace CalamityLegendsComeBack.Weapons.LeonidProgenitor.Effects
{
    public abstract class LeonidMetalEffect
    {
        public abstract int EffectID { get; }

        public virtual void OnSpawn(LeonidCometSmall meteor, Player owner)
        {
        }

        public virtual void AI(LeonidCometSmall meteor, Player owner)
        {
        }

        public virtual void ModifyHitNPC(LeonidCometSmall meteor, Player owner, NPC target, ref NPC.HitModifiers modifiers)
        {
        }

        public virtual void OnHitNPC(LeonidCometSmall meteor, Player owner, NPC target, NPC.HitInfo hit, int damageDone)
        {
        }

        public virtual bool OnTileCollide(LeonidCometSmall meteor, Player owner, Vector2 oldVelocity)
        {
            return true;
        }

        public virtual void OnKill(LeonidCometSmall meteor, Player owner, int timeLeft)
        {
        }

        public virtual void PostDraw(LeonidCometSmall meteor, Player owner, SpriteBatch spriteBatch)
        {
        }
    }
}
