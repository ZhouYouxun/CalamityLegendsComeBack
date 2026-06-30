using CalamityLegendsComeBack.Weapons.A_Dev.DesertEagle.Slot;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;

namespace CalamityLegendsComeBack.Weapons.A_Dev.DesertEagle.LeftClick.Rules
{
    /// <summary>
    /// 沙漠之鹰左键弹幕行为基类。
    /// 每把装填进格子槽的手枪对应一个继承类，决定弹幕的全部表现。
    /// </summary>
    public abstract class DEBulletRule
    {
        // ──────────────────────────────────────────────────────
        //  标识
        // ──────────────────────────────────────────────────────

        /// <summary>激活本规则的枪的 ItemType；0 表示默认规则（无装填）。</summary>
        public abstract int GunItemType { get; }

        // ──────────────────────────────────────────────────────
        //  弹幕基础数值（默认与 LifeRound 相似，规则可覆盖）
        // ──────────────────────────────────────────────────────

        /// <summary>穿透数。默认 1。</summary>
        public virtual int Penetrate => 1;

        /// <summary>额外更新次数（决定弹速）。默认 4，与 LifeRound 相同。</summary>
        public virtual int ExtraUpdates => 4;

        /// <summary>护甲穿透。默认 0。</summary>
        public virtual int ArmorPenetration => 0;

        /// <summary>发射速度倍率（相对于武器 shootSpeed）。默认 1f。</summary>
        public virtual float SpeedMultiplier => 1f;

        /// <summary>伤害倍率（相对于武器伤害）。默认 1f。</summary>
        public virtual float DamageMultiplier => 1f;

        // ──────────────────────────────────────────────────────
        //  每发子弹的额外同步数据（存入 ai[1]）
        // ──────────────────────────────────────────────────────

        /// <summary>
        /// 在 Shoot() 中调用，返回存入 Projectile.ai[1] 的额外浮点数据。
        /// 用于：弹射剩余次数、牌型序号、冰/火切换状态、是否为强化弹等。
        /// </summary>
        public virtual float GetShotExtra(DesertEagleSlotPlayer slotPlayer) => 0f;

        // ──────────────────────────────────────────────────────
        //  生命周期钩子
        // ──────────────────────────────────────────────────────

        /// <summary>弹幕 SetDefaults() 时调用，可进一步覆盖默认值。</summary>
        public virtual void SetDefaults(Projectile projectile) { }

        /// <summary>弹幕生成（OnSpawn）时调用。</summary>
        public virtual void OnSpawn(Projectile projectile, Player owner) { }

        /// <summary>每帧 AI() 时调用。负责旋转、尾迹粒子、归巢等逻辑。</summary>
        public virtual void AI(Projectile projectile, Player owner) { }

        // ──────────────────────────────────────────────────────
        //  战斗钩子
        // ──────────────────────────────────────────────────────

        public virtual void ModifyHitNPC(Projectile projectile, Player owner, NPC target, ref NPC.HitModifiers modifiers) { }

        public virtual void OnHitNPC(Projectile projectile, Player owner, NPC target, NPC.HitInfo hit, int damageDone) { }

        public virtual bool? CanHitNPC(Projectile projectile, Player owner, NPC target) => null;

        /// <summary>撞墙时调用。返回 true 则继续走原版逻辑（销毁弹幕）。</summary>
        public virtual bool OnTileCollide(Projectile projectile, Player owner, Vector2 oldVelocity) => true;

        // ──────────────────────────────────────────────────────
        //  消亡钩子
        // ──────────────────────────────────────────────────────

        public virtual void OnKill(Projectile projectile, Player owner, int timeLeft) { }

        // ──────────────────────────────────────────────────────
        //  绘制钩子
        // ──────────────────────────────────────────────────────

        /// <summary>
        /// 替换 PreDraw。
        /// 返回 true → DELeftBullet 继续执行自己的后像绘制；
        /// 返回 false → 完全自定义（规则自行绘制，不再走默认后像）。
        /// </summary>
        public virtual bool PreDraw(Projectile projectile, Player owner, ref Color lightColor) => true;

        public virtual void PostDraw(Projectile projectile, Player owner, SpriteBatch spriteBatch) { }

        // ──────────────────────────────────────────────────────
        //  Tooltip 文本（按语言各一份）
        // ──────────────────────────────────────────────────────

        public virtual string TooltipEffectEN => "";
        public virtual string TooltipEffectZH => "";
    }
}
