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
    public class IceWedgeProj : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetDefaults()
        {
            Projectile.width = 16;
            Projectile.height = 16;
            Projectile.friendly = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = true;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 120;
            Projectile.DamageType = DamageClass.Summon;
        }

        public override void AI()
        {
            // 稍许寻敌自动微调追踪
            NPC target = CalamityUtils.MinionHoming(Projectile.Center, 500f, Main.player[Projectile.owner]);
            if (target != null)
            {
                Vector2 targetDir = (target.Center - Projectile.Center).SafeNormalize(Vector2.UnitX);
                Projectile.velocity = (Projectile.velocity * 15f + targetDir * 14f) / 16f;
            }

            // 粒子尾迹
            for (int i = 0; i < 2; i++)
            {
                int dType = Main.rand.NextBool(2) ? DustID.Frost : DustID.Electric;
                Dust d = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, dType);
                d.velocity = -Projectile.velocity * 0.3f;
                d.scale = Main.rand.NextFloat(0.9f, 1.4f);
                d.noGravity = true;
            }

            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            Player player = Main.player[Projectile.owner];
            var modPlayer = player.GetModPlayer<GlacialEmbracePlayer>();

            // 给敌人上冻伤 Debuff
            target.AddBuff(BuffID.Frostburn2, 300);

            // 增加 4 点终结技充能
            modPlayer.UltimateCharge = Math.Min(GlacialEmbracePlayer.MaxUltimateCharge, modPlayer.UltimateCharge + 4);
            modPlayer.OnSpecialHitNPC();

            // 寻找所有钉在该敌人身上的冰刺，激活它们进行全贯穿打击！
            int spikeType = ModContent.ProjectileType<IceSpikeMinion>();
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile p = Main.projectile[i];
                if (p.active && p.type == spikeType && p.owner == player.whoAmI)
                {
                    var spike = p.ModProjectile as IceSpikeMinion;
                    if (spike != null && spike.embedded && spike.embedNPCIndex == target.whoAmI)
                    {
                        // 贯穿方向为钉入偏移矢量的朝向
                        Vector2 thrustDir = spike.embedOffset.SafeNormalize(Vector2.UnitY);
                        spike.PierceThrust(thrustDir);
                    }
                }
            }

            SoundEngine.PlaySound(SoundID.Item27 with { Pitch = 0.5f, Volume = 0.8f }, Projectile.Center); // 冰块碎裂声
        }

        public override void OnKill(int timeLeft)
        {
            // 碎裂粒子
            for (int i = 0; i < 20; i++)
            {
                int dType = Main.rand.NextBool(2) ? DustID.Frost : DustID.Electric;
                Dust d = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, dType);
                d.velocity = Main.rand.NextVector2Circular(5f, 5f);
                d.scale = Main.rand.NextFloat(1.1f, 1.7f);
                d.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            // 绘制水晶冰楔
            Texture2D crystalTex = ModContent.Request<Texture2D>("CalamityLegendsComeBack/Texture/KsTexture/spark_03").Value;
            Vector2 drawPos = Projectile.Center - Main.screenPosition;
            Color drawCol = Color.Lerp(Color.Cyan, Color.White, 0.4f);

            // 水平拉伸使之呈现长楔状
            Main.spriteBatch.Draw(crystalTex, drawPos, null, drawCol, Projectile.rotation - MathHelper.PiOver2, crystalTex.Size() * 0.5f, new Vector2(1.2f, 0.5f), SpriteEffects.None, 0f);
            return false;
        }
    }
}
