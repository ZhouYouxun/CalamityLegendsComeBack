using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SHPC.Effects.BPrePlantera
{
    public class BossSoulofMight_Ball : ModProjectile, ILocalizedModType
    {
        //public override string Texture => "CalamityMod/Projectiles/InvisibleProj";
        public override string Texture => "Terraria/Images/Projectile_106";

        public new string LocalizationCategory => "Projectiles";



        // 定义初始速度、减速速度和减速时间
        public const float InitialSpeed = 68f;
        public const float SlowdownSpeed = 2f;
        public const int SlowdownTime = 175;
        public static readonly float SlowdownFactor = (float)Math.Pow(SlowdownSpeed / InitialSpeed, 1f / SlowdownTime);

        // 使用 ai[0] 来记录时间
        public ref float Time => ref Projectile.ai[0];
        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 8;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 2;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            // 模仿SeraphimProjectile的PreDraw方法
            Texture2D texture = Terraria.GameContent.TextureAssets.Projectile[Projectile.type].Value;
            Vector2 origin = texture.Size() * 0.5f;
            Vector2 baseDrawPosition = Projectile.Center - Main.screenPosition;

            // 逐渐消失的光效
            float endFade = Utils.GetLerpValue(0f, 12f, Projectile.timeLeft, true);
            Color mainColor = Color.White * Projectile.Opacity * endFade * 1.5f;
            mainColor.A = (byte)(255 - Projectile.alpha);

            Color afterimageLightColor = Color.White * endFade;
            afterimageLightColor.A = (byte)(255 - Projectile.alpha);

            // 绘制多个逐渐淡出的光影
            for (int i = 0; i < 18; i++)
            {
                Vector2 drawPosition = baseDrawPosition + (MathHelper.TwoPi * i / 18f).ToRotationVector2() * (1f - Projectile.Opacity) * 16f;
                Main.EntitySpriteDraw(texture, drawPosition, null, afterimageLightColor, Projectile.rotation, origin, Projectile.scale, SpriteEffects.None, 0);
            }

            // 绘制特殊的残影效果
            for (int i = 0; i < 8; i++)
            {
                Vector2 drawPosition = baseDrawPosition - Projectile.velocity * i * 0.3f;
                Color afterimageColor = mainColor * (1f - i / 8f);
                Main.EntitySpriteDraw(texture, drawPosition, null, afterimageColor, Projectile.rotation, origin, Projectile.scale, SpriteEffects.None, 0);
            }

            return false; // 不使用默认绘制

        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 14;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = 1;
            Projectile.light = 0.5f;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = true; // 允许与方块碰撞
            Projectile.extraUpdates = 1; // 额外更新次数
            Projectile.usesLocalNPCImmunity = true; // 弹幕使用本地无敌帧
            Projectile.localNPCHitCooldown = 14; // 无敌帧冷却时间为14帧
            Projectile.timeLeft = 100;

        }

        public override void AI()
        {
            // 保持弹幕旋转（对于倾斜走向的弹幕而言）
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver4;

            // Lighting - 添加深橙色光源，光照强度为 0.55
            Lighting.AddLight(Projectile.Center, Color.Orange.ToVector3() * 0.55f);

            // 弹幕保持直线运动并逐渐加速
            //Projectile.velocity *= 1.01f;

            // Very rapidly slow down and fade out, transforming into light.
            if (Time <= SlowdownTime)
            {
                Projectile.Opacity = (float)Math.Pow(1f - Time / SlowdownTime, 2D);
                Projectile.velocity *= SlowdownFactor;

                int lightDustCount = (int)MathHelper.Lerp(8f, 1f, Projectile.Opacity);
                for (int i = 0; i < lightDustCount; i++)
                {
                    Vector2 dustSpawnPosition = Projectile.Center + Main.rand.NextVector2Unit() * (1f - Projectile.Opacity) * 45f;
                    Dust light = Dust.NewDustPerfect(dustSpawnPosition, 267);
                    light.color = Color.Lerp(Color.Gold, Color.White, Main.rand.NextFloat(0.5f, 1f));
                    light.velocity = Main.rand.NextVector2Circular(10f, 10f);
                    light.scale = MathHelper.Lerp(1.3f, 0.8f, Projectile.Opacity) * Main.rand.NextFloat(0.8f, 1.2f);
                    light.noGravity = true;
                }

            }

            Time++;

        }



        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.Electrified, 300); // 原版的带电效果
            //target.AddBuff(ModContent.BuffType<GalvanicCorrosion>(), 300); // 电偶腐蚀
        }
        public override void OnKill(int timeLeft)
        {
            // 在弹幕消失时，释放SHPExplosion
            Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, Vector2.Zero, ModContent.ProjectileType<BossSoulofMight_EXP>(), Projectile.damage, Projectile.knockBack, Projectile.owner);
        }
    }
}
