using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using CalamityMod;

namespace CalamityLegendsComeBack.Weapons.GlacialEmbrace
{
    public class IceBladeProj : ModProjectile
    {
        public override string Texture => "CalamityLegendsComeBack/Texture/KsTexture/slash_01"; // 使用弧形斜斩贴图

        private float rotationOffset = 0f;
        private int attackTimer = 0;
        private const int PrepareDelay = 10; // 蓄力准备延迟
        private const int SpinDuration = 30; // 旋转持续帧数

        public override void SetDefaults()
        {
            Projectile.width = 160;
            Projectile.height = 160;
            Projectile.friendly = true;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = PrepareDelay + SpinDuration;
            Projectile.DamageType = DamageClass.Summon;
            
            // 启用静态无敌帧，以防多次撞击重叠
            Projectile.usesIDStaticNPCImmunity = true;
            Projectile.idStaticNPCHitCooldown = 8;
        }

        public override bool? CanDamage()
        {
            // 在准备蓄力期不造成伤害
            return attackTimer >= PrepareDelay ? null : false;
        }

        public override void AI()
        {
            Player player = Main.player[Projectile.owner];
            if (!player.active || player.dead)
            {
                Projectile.Kill();
                return;
            }

            // 锁死位置在玩家中心
            Projectile.Center = player.Center;

            attackTimer++;
            if (attackTimer < PrepareDelay)
            {
                // 1. 准备期：冰刀在鼠标方向颤抖蓄能
                Vector2 toMouse = player.Calamity().mouseWorld - player.Center;
                rotationOffset = toMouse.ToRotation();
                
                // 粒子蓄能
                if (Main.rand.NextBool(3))
                {
                    Dust d = Dust.NewDustDirect(player.Center + toMouse.SafeNormalize(Vector2.UnitX) * 40f, 0, 0, DustID.Ice);
                    d.velocity = -toMouse.SafeNormalize(Vector2.Zero) * 2f;
                    d.noGravity = true;
                }
            }
            else
            {
                // 2. 旋转爆发期：以极快速度围绕玩家旋转两周 (720度 / 30帧 = 24度每帧)
                if (attackTimer == PrepareDelay)
                {
                    // 挥击音效
                    SoundEngine.PlaySound(SoundID.Item71 with { Pitch = -0.1f, Volume = 1f }, Projectile.Center);
                    
                    // 增加轻微屏幕震动
                    player.Calamity().GeneralScreenShakePower = Math.Max(player.Calamity().GeneralScreenShakePower, 2.2f);
                }

                float progress = (float)(attackTimer - PrepareDelay) / SpinDuration;
                float angle = progress * MathHelper.TwoPi * 2f; // 旋转两周
                Projectile.rotation = rotationOffset + angle;
                
                // 沿刀刃长度产生密集的极光与矩阵粒子
                for (int i = 0; i < 4; i++)
                {
                    float dist = 20f + i * 25f;
                    Vector2 pos = Projectile.Center + Projectile.rotation.ToRotationVector2() * dist;
                    int dType = Main.rand.NextBool(2) ? DustID.Frost : DustID.Electric;
                    Dust dDust = Dust.NewDustDirect(pos, 0, 0, dType);
                    dDust.velocity = Projectile.rotation.ToRotationVector2().RotatedBy(MathHelper.PiOver2) * (3f + i);
                    dDust.scale = 1.0f + Main.rand.NextFloat(0.6f);
                    dDust.noGravity = true;
                }
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.Frostburn2, 300);
            
            // 每次成功劈砍命中，会为玩家的终结技充能槽增加 4 点能量
            Player player = Main.player[Projectile.owner];
            var modPlayer = player.GetModPlayer<GlacialEmbracePlayer>();
            modPlayer.UltimateCharge = Math.Min(GlacialEmbracePlayer.MaxUltimateCharge, modPlayer.UltimateCharge + 4);
            modPlayer.OnSpecialHitNPC();
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (attackTimer < PrepareDelay) return false; // 准备期不画刀芒

            Texture2D slashTex = TextureAssets.Projectile[Type].Value;
            Vector2 drawPos = Projectile.Center - Main.screenPosition;
            float time = Main.GlobalTimeWrappedHourly;

            // 绘制极光绚丽的环形刀芒
            Color slashCol = Color.Lerp(new Color(0, 195, 255, 0), new Color(150, 50, 255, 0), 0.5f + 0.5f * MathF.Sin(time * 8f));
            
            // 双向发光叠加
            Main.spriteBatch.Draw(slashTex, drawPos, null, slashCol * 0.9f, Projectile.rotation, slashTex.Size() * 0.5f, 1.2f, SpriteEffects.None, 0f);
            Main.spriteBatch.Draw(slashTex, drawPos, null, Color.White * 0.4f, Projectile.rotation, slashTex.Size() * 0.5f, 1.0f, SpriteEffects.None, 0f);

            return false;
        }
    }
}
