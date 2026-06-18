using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using CalamityLegendsComeBack.Weapons.SHPC;

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
        public virtual int ShotsPerAmmo => SHPCAmmoCapacity.GetCapacity(EffectID);
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

        public virtual bool EnableProximityExplosion => true;

        // 是否播放 SHPC 左键默认开火音效 AnomalysNanogunMPFBShot。只影响左键开火声音。
        public virtual bool PlayDefaultLeftClickFireSound => true;

        public virtual string LeftClickReplacementFireSoundPath => EffectID switch
        {
            4 => "CalamityLegendsComeBack/Sound/Other/Helldiver2/榴弹发射器-开火",
            8 => "CalamityLegendsComeBack/Sound/Other/Helldiver2/反器材狙击步枪开火",
            10 => "CalamityLegendsComeBack/Sound/Other/Helldiver2/轨道烟雾攻击",
            13 => "CalamityLegendsComeBack/Sound/Other/Helldiver2/机炮开火",
            17 => "CalamityLegendsComeBack/Sound/Other/Helldiver2/Wasp单次开火",
            18 => "CalamityLegendsComeBack/Sound/Other/Helldiver2/Wasp连续开火",
            19 => "CalamityLegendsComeBack/Sound/Other/Helldiver2/电弧发射器-发射",
            21 => "CalamityLegendsComeBack/Sound/Other/Helldiver2/最后通牒开火",
            23 => "CalamityLegendsComeBack/Sound/Other/Helldiver2/电弧发射器-发射",
            25 => "CalamityLegendsComeBack/Sound/Other/Helldiver2/轨道空爆攻击",
            26 => "CalamityLegendsComeBack/Sound/Other/Helldiver2/焦土-开枪",
            33 => "CalamityLegendsComeBack/Sound/Other/Helldiver2/磁轨炮-开火",
            34 => "CalamityLegendsComeBack/Sound/Other/Helldiver2/激光大炮开火",
            35 => "CalamityLegendsComeBack/Sound/Other/Helldiver2/焦土-开枪",
            36 => "CalamityLegendsComeBack/Sound/Other/Helldiver2/轨道炮攻击-仅开火",
            37 => "CalamityLegendsComeBack/Sound/Other/Helldiver2/焦土-开枪",
            38 => "CalamityLegendsComeBack/Sound/Other/Helldiver2/磁轨炮-开火",
            39 => "CalamityLegendsComeBack/Sound/Other/Helldiver2/爆裂铳开火",
            40 => "CalamityLegendsComeBack/Sound/SHPC/雷霆开火与换弹",
            41 => "CalamityLegendsComeBack/Sound/Other/Helldiver2/反器材狙击步枪开火",
            43 => "CalamityLegendsComeBack/Sound/Other/Helldiver2/反坦克炮-开火与换弹",
            44 => "CalamityLegendsComeBack/Sound/Other/Helldiver2/榴弹发射器-开火",
            _ => "CalamityLegendsComeBack/Sound/Other/Helldiver2/爆裂铳开火"
        };

        public virtual int LeftClickReplacementFireSoundMaxInstances => 4;

        public virtual void PlayLeftClickReplacementFireSound(Projectile projectile, Player owner)
        {
            if (PlayDefaultLeftClickFireSound || Main.dedServ || projectile.owner != Main.myPlayer)
                return;

            SoundEngine.PlaySound(new SoundStyle(LeftClickReplacementFireSoundPath)
            {
                MaxInstances = LeftClickReplacementFireSoundMaxInstances
            }, projectile.Center);
        }

        // 每帧AI附加逻辑
        public virtual void AI(Projectile projectile, Player owner)
        {


        }

        // 命中前数值修改
        public virtual void ModifyHitNPC(Projectile projectile, Player owner, NPC target, ref NPC.HitModifiers modifiers)
        {


        }

        public virtual bool? CanDamage(Projectile projectile, Player owner)
        {
            return null;
        }

        public virtual bool? CanHitNPC(Projectile projectile, Player owner, NPC target)
        {
            return null;
        }

        // 命中后逻辑
        public virtual void ModifyDamageHitbox(Projectile projectile, Player owner, ref Rectangle hitbox)
        {
        }

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

        public virtual bool SuppressDefaultOnKillEffects => false;

        // 死亡/爆炸逻辑 
        public virtual void OnKill(Projectile projectile, Player owner, int timeLeft)
        {


        }
        // 光芒大小控制（默认与原行为一致）
        public virtual float GlowScaleFactor => 1f;

        // 光芒亮度控制（默认与原行为一致）
        public virtual float GlowIntensityFactor => 0.9f;

        // 左键一次触发的视觉爆发脉冲次数（普通=1，连射材料=N）
        // 用于左键手持弹幕决定做几次小后坐力与星芒闪烁
        public virtual int LeftClickBurstCount => 1;
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
