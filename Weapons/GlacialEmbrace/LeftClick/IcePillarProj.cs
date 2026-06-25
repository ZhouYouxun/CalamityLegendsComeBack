using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using CalamityMod;
using CalamityLegendsComeBack.Weapons.GlacialEmbrace.General;

namespace CalamityLegendsComeBack.Weapons.GlacialEmbrace.LeftClick
{
    public class IcePillarProj : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetDefaults()
        {
            Projectile.width = 30;
            Projectile.height = 30;
            Projectile.friendly = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = true;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 100;
            Projectile.DamageType = DamageClass.Summon;
        }

        public override void AI()
        {
            // 冰柱受重力微调下坠，表现出厚重感
            Projectile.velocity.Y += 0.15f;
            
            // 冰柱旋转朝向移动速度方向
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;

            // 掉落大量冰尘与科技矩阵火花
            for (int i = 0; i < 2; i++)
            {
                int dType = Main.rand.NextBool(2) ? DustID.Frost : DustID.Electric;
                Dust d = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, dType);
                d.velocity = -Projectile.velocity * 0.25f;
                d.scale = Main.rand.NextFloat(1.1f, 1.7f);
                d.noGravity = true;
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            Player player = Main.player[Projectile.owner];
            var modPlayer = player.GetModPlayer<GlacialEmbracePlayer>();

            // 造成冻伤
            target.AddBuff(BuffID.Frostburn2, 300);

            // 充能 +4
            modPlayer.UltimateCharge = Math.Min(GlacialEmbracePlayer.MaxUltimateCharge, modPlayer.UltimateCharge + 4);
            modPlayer.OnSpecialHitNPC();

            // 1. 强制击退判定：普通敌人强击退，Boss 弱击退；肉山和 Boss 仆从不处理。
            bool wallOfFlesh = target.type == NPCID.WallofFlesh || target.type == NPCID.WallofFleshEye;
            bool bossServant = target.realLife >= 0 && !target.boss;
            if (!wallOfFlesh && !bossServant)
            {
                float pushStrength = target.boss ? 3.2f : 10.5f;
                Vector2 pushVel = Projectile.velocity.SafeNormalize(Vector2.UnitX) * pushStrength;
                target.velocity += pushVel;
            }

            // 2. 引爆玩家所有冰刺的冲击波并进入休眠
            int spikeType = ModContent.ProjectileType<IceSpikeMinion>();
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile p = Main.projectile[i];
                if (p.active && p.type == spikeType && p.owner == player.whoAmI)
                {
                    var spike = p.ModProjectile as IceSpikeMinion;
                    if (spike != null && !spike.hibernating)
                    {
                        spike.ForceShockwaveAndHibernate(player);
                    }
                }
            }

            // 3. 产生范围爆炸伤害
            var source = player.GetSource_ItemUse(player.HeldItem);
            int expl = Projectile.NewProjectile(source, Projectile.Center, Vector2.Zero, ProjectileID.SolarWhipSwordExplosion, (int)(Projectile.damage * 1.5f), 8f, player.whoAmI);
            if (Main.projectile.IndexInRange(expl))
            {
                Main.projectile[expl].friendly = true;
                Main.projectile[expl].hostile = false;
                Main.projectile[expl].DamageType = DamageClass.Summon;
            }

            SoundEngine.PlaySound(SoundID.Item62 with { Volume = 1f, Pitch = -0.2f }, Projectile.Center); // 巨大爆破声
            
            // 增加屏幕震动
            player.Calamity().GeneralScreenShakePower = Math.Max(player.Calamity().GeneralScreenShakePower, 4.5f);
        }

        public override void OnKill(int timeLeft)
        {
            // 冰尘与科技矩阵爆发
            for (int i = 0; i < 35; i++)
            {
                int dType = Main.rand.NextBool(2) ? DustID.Frost : DustID.Electric;
                Dust d = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, dType);
                d.velocity = Main.rand.NextVector2Circular(8f, 8f);
                d.scale = Main.rand.NextFloat(1.3f, 2.4f);
                d.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            // 绘制巨大的厚重冰柱 (冰花贴图进行拉伸)
            Texture2D pillarTex = ModContent.Request<Texture2D>("CalamityLegendsComeBack/Texture/KsTexture/spark_03").Value;
            Vector2 drawPos = Projectile.Center - Main.screenPosition;
            Color drawCol = Color.Lerp(Color.Cyan, Color.LightSkyBlue, 0.3f);

            // 纵向拉伸成粗大冰柱 shape
            Main.spriteBatch.Draw(pillarTex, drawPos, null, drawCol * 0.95f, Projectile.rotation - MathHelper.PiOver2, pillarTex.Size() * 0.5f, new Vector2(2.2f, 1.1f), SpriteEffects.None, 0f);
            return false;
        }
    }
}
