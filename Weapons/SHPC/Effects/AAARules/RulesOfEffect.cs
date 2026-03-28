using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;

namespace CalamityLegendsComeBack.Weapons.SHPC.Effects.AAARules
{
    public abstract class RulesOfEffect
    {
        // 唯一ID + 对应弹药
        public abstract int EffectID { get; }
        public abstract int AmmoType { get; }

        // 三种颜色（主体 / 拖尾起点 / 拖尾终点）
        public virtual Color ThemeColor => Color.White;
        public virtual Color StartColor => Color.White;
        public virtual Color EndColor => Color.White;

        // 一个材料装填多少发？
        public virtual int ShotsPerAmmo => 50; // 默认50发
        // 基础数值修改（只允许改指定字段）
        public virtual void SetDefaults(Projectile projectile)
        {


        }

        // 弹幕生成时调用（统一入口）
        public virtual void OnSpawn(Projectile projectile, Player owner)
        {


        }
        // 是否启用默认减速（默认开启）
        public virtual bool EnableDefaultSlowdown => true;
        // 每帧AI附加逻辑
        public virtual void AI(Projectile projectile, Player owner)
        {


        }

        // 命中前数值修改
        public virtual void ModifyHitNPC(Projectile projectile, Player owner, NPC target, ref NPC.HitModifiers modifiers)
        {


        }

        // 命中后逻辑
        public virtual void OnHitNPC(Projectile projectile, Player owner, NPC target, NPC.HitInfo hit, int damageDone)
        {


        }

        // 撞墙逻辑【白卷内不用改，因为他本来就默认】
        public virtual bool OnTileCollide(Projectile projectile, Player owner, Vector2 oldVelocity)
        {
            return true; // 默认行为：不拦截，走原版
        }


        // SquishyLightParticle强度系数（默认1）
        public virtual float SquishyLightParticleFactor => 1f;

        public virtual float ExplosionPulseFactor => 1f;
        // 死亡/爆炸逻辑 
        public virtual void OnKill(Projectile projectile, Player owner, int timeLeft)
        {


        }

        // 绘制前附加
        public virtual void PreDraw(Projectile projectile, Player owner, SpriteBatch spriteBatch)
        {


        }

        // 绘制后附加
        public virtual void PostDraw(Projectile projectile, Player owner, SpriteBatch spriteBatch)
        {


        }
    }
}