using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;

namespace CalamityLegendsComeBack.Weapons.SHPC.Effects.AAARules
{
    public class DefaultEffect : RulesOfEffect
    {
        public override int EffectID => -1;

        public override int AmmoType => 0;

        //// 默认灰白主题色
        //public override Color ThemeColor => new Color(205, 205, 205);

        //// 默认拖尾起始色
        //public override Color StartColor => new Color(245, 245, 245);

        //// 默认拖尾末尾色
        //public override Color EndColor => new Color(125, 125, 125);

        public override Color ThemeColor => Color.Transparent;
        public override Color StartColor => Color.Transparent;
        public override Color EndColor => Color.Transparent;

        // 基础数值（白卷，不改）
        public override void SetDefaults(Projectile projectile)
        {


        }

        // 出生时（白卷）
        public override void OnSpawn(Projectile projectile, Player owner)
        {


        }
        public override bool EnableDefaultSlowdown => true;
        // 每帧AI（白卷）
        public override void AI(Projectile projectile, Player owner)
        {


        }

        // 命中前（白卷）
        public override void ModifyHitNPC(Projectile projectile, Player owner, NPC target, ref NPC.HitModifiers modifiers)
        {


        }

        // 命中后（白卷）
        public override void OnHitNPC(Projectile projectile, Player owner, NPC target, NPC.HitInfo hit, int damageDone)
        {


        }
        public override float SquishyLightParticleFactor => 1f;
        public override float ExplosionPulseFactor => 1f;
        // 死亡时（白卷）
        public override void OnKill(Projectile projectile, Player owner, int timeLeft)
        {


        }

        // 绘制前（白卷）
        public override void PreDraw(Projectile projectile, Player owner, SpriteBatch spriteBatch)
        {


        }

        // 绘制后（白卷）
        public override void PostDraw(Projectile projectile, Player owner, SpriteBatch spriteBatch)
        {


        }
    }
}