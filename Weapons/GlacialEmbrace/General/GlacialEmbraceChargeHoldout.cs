using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using CalamityMod;
using CalamityLegendsComeBack.Weapons.GlacialEmbrace.LeftClick;

namespace CalamityLegendsComeBack.Weapons.GlacialEmbrace.General
{
    public class GlacialEmbraceChargeHoldout : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetDefaults()
        {
            Projectile.width = 2;
            Projectile.height = 2;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 2;
        }

        public override bool? CanDamage() => false;
        public override bool ShouldUpdatePosition() => false;

        public override void AI()
        {
            Player player = Main.player[Projectile.owner];
            var modPlayer = player.GetModPlayer<GlacialEmbracePlayer>();

            // 维持周期，锁定玩家使用动作
            if (!player.active || player.dead || player.HeldItem.type != ModContent.ItemType<GlacialEmbrace>())
            {
                Projectile.Kill();
                return;
            }
            
            // 锁定玩家手持动作
            player.itemAnimation = 2;
            player.itemTime = 2;
            player.ChangeDir(player.Calamity().mouseWorld.X > player.Center.X ? 1 : -1);

            // 更新 QTE 状态至 Player
            modPlayer.QteActive = true;
            modPlayer.QteTimer = (int)Projectile.ai[0];
            modPlayer.QteMax = 45;
            modPlayer.QteType = 1; // 蓄力释放 QTE

            // 自增蓄力时间
            if (Projectile.ai[0] < 45)
            {
                Projectile.ai[0]++;
                if (Projectile.ai[0] == 45)
                {
                    // 蓄满提示音
                    SoundEngine.PlaySound(SoundID.Item29 with { Pitch = 0.5f, Volume = 0.5f }, player.Center);
                }
            }

            // 维持弹幕存活
            Projectile.timeLeft = 2;

            // 监测按键释放：若松开左键，触发释放判定
            if (!player.channel && Main.myPlayer == Projectile.owner)
            {
                ReleaseChargedAttack(player, modPlayer);
                Projectile.Kill();
            }
        }

        private void ReleaseChargedAttack(Player player, GlacialEmbracePlayer modPlayer)
        {
            modPlayer.QteActive = false;

            bool perfect = false;
            if (Projectile.ai[0] >= 30)
            {
                // 只有在进行了实质性蓄力（大于等于30帧）时，才触发 QTE 判定
                perfect = Projectile.ai[0] >= 35 && Projectile.ai[0] <= 45;
                if (perfect)
                {
                    modPlayer.TriggerQteSuccess();
                }
                else
                {
                    modPlayer.TriggerQteFail();
                }
            }

            // 根据不同模式，生成对应的特殊攻击弹幕
            var source = player.GetSource_ItemUse(player.HeldItem);
            Vector2 targetDir = (player.Calamity().mouseWorld - player.Center).SafeNormalize(Vector2.UnitX * player.direction);
            int damage = (int)(player.HeldItem.damage * (perfect ? 1.5f : 1.0f));

            if (modPlayer.CurrentMode == 0) // 斩击形态：冰刀
            {
                Projectile.NewProjectile(source, player.Center, Vector2.Zero, ModContent.ProjectileType<IceBladeProj>(), damage, player.HeldItem.knockBack, player.whoAmI);
            }
            else if (modPlayer.CurrentMode == 1) // 突刺形态：冰楔
            {
                Projectile.NewProjectile(source, player.Center, targetDir * 14f, ModContent.ProjectileType<IceWedgeProj>(), damage, player.HeldItem.knockBack, player.whoAmI);
            }
            else // 打击形态：冰柱
            {
                Projectile.NewProjectile(source, player.Center, targetDir * 12f, ModContent.ProjectileType<IcePillarProj>(), (int)(damage * 1.8f), player.HeldItem.knockBack * 1.8f, player.whoAmI);
            }
        }

        public override void OnKill(int timeLeft)
        {
            Player player = Main.player[Projectile.owner];
            player.GetModPlayer<GlacialEmbracePlayer>().QteActive = false;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Player player = Main.player[Projectile.owner];
            var modPlayer = player.GetModPlayer<GlacialEmbracePlayer>();

            float progress = Projectile.ai[0] / 45f;
            float time = Main.GlobalTimeWrappedHourly;
            Texture2D pixel = TextureAssets.MagicPixel.Value;
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;

            // 绘制蓄力特质：不同形态展现不同发光体积以及网格线条收缩
            if (modPlayer.CurrentMode == 0)
            {
                // 斩击模式：在鼠标位置绘制逐渐凝聚的冰刀轮廓与缩紧的辅助矩阵线
                Vector2 mousePos = player.Calamity().mouseWorld - Main.screenPosition;
                Color bladeCol = new Color(0, 195, 255, 0) * progress * 0.5f;

                // 绘制同心圆缩紧线
                float ringRad = (1f - progress) * 80f + 10f;
                Main.spriteBatch.Draw(bloom, mousePos, null, bladeCol * 0.4f, 0f, bloom.Size() * 0.5f, ringRad / bloom.Width, SpriteEffects.None, 0f);

                // 绘制一条指向刀尖的矩阵线
                Vector2 lineStart = player.Center - Main.screenPosition;
                Vector2 lineEnd = player.Calamity().mouseWorld - Main.screenPosition;
                Main.spriteBatch.Draw(pixel, lineStart, new Rectangle(0, 0, 1, 1), bladeCol * 0.3f, 
                    (lineEnd - lineStart).ToRotation(), new Vector2(0f, 0.5f), new Vector2((lineEnd - lineStart).Length(), 1.5f), SpriteEffects.None, 0f);
            }
            else if (modPlayer.CurrentMode == 1)
            {
                // 突刺模式：在玩家头顶绘制旋转冰楔与聚能线条
                Vector2 headPos = player.Top + new Vector2(0f, -40f) - Main.screenPosition;
                Color wedgeCol = new Color(130, 230, 255, 0) * progress * 0.6f;
                Main.spriteBatch.Draw(bloom, headPos, null, wedgeCol, time * 2f, bloom.Size() * 0.5f, 0.25f * progress, SpriteEffects.None, 0f);

                // 放射状收缩网格线
                for (int i = 0; i < 4; i++)
                {
                    float angle = time * 0.8f + MathHelper.TwoPi * i / 4f;
                    Vector2 offset = angle.ToRotationVector2() * (40f * (1f - progress));
                    Main.spriteBatch.Draw(pixel, headPos + offset, new Rectangle(0, 0, 1, 1), wedgeCol * 0.4f,
                        angle, new Vector2(0f, 0.5f), new Vector2(15f, 1f), SpriteEffects.None, 0f);
                }
            }
            else
            {
                // 打击模式：在玩家身前生成巨大冰柱光影
                Vector2 frontPos = player.Center + (player.Calamity().mouseWorld - player.Center).SafeNormalize(Vector2.UnitX * player.direction) * 40f - Main.screenPosition;
                Color pillarCol = Color.Lerp(Color.Cyan, Color.OrangeRed, 0.2f * MathF.Sin(time * 5f)) * progress * 0.7f;
                Main.spriteBatch.Draw(bloom, frontPos, null, pillarCol, 0f, bloom.Size() * 0.5f, 0.4f * progress, SpriteEffects.None, 0f);
            }

            return false;
        }
    }
}
