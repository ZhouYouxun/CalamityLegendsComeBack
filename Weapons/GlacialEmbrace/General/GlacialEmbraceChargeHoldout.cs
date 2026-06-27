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

        private const int MaxCharge = 45;
        private const int PerfectMin = 30; // 蓄力30帧以上为完美释放
        private const int ReleaseCooldown = 15;

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

            if (!player.active || player.dead || player.HeldItem.type != ModContent.ItemType<GlacialEmbrace>())
            {
                Projectile.Kill();
                return;
            }

            player.itemAnimation = 2;
            player.itemTime = 2;
            player.ChangeDir(player.Calamity().mouseWorld.X > player.Center.X ? 1 : -1);

            if (Projectile.ai[0] < MaxCharge)
            {
                Projectile.ai[0]++;
                if (Projectile.ai[0] == MaxCharge)
                    SoundEngine.PlaySound(SoundID.Item29 with { Pitch = 0.5f, Volume = 0.5f }, player.Center);
            }

            Projectile.timeLeft = 2;

            if (!player.channel && Main.myPlayer == Projectile.owner)
            {
                ReleaseChargedAttack(player, modPlayer);
                Projectile.Kill();
            }
        }

        private void ReleaseChargedAttack(Player player, GlacialEmbracePlayer modPlayer)
        {
            bool perfect = Projectile.ai[0] >= PerfectMin;
            float chargeRatio = MathHelper.Clamp(Projectile.ai[0] / PerfectMin, 0f, 1f);
            float releasePower = MathHelper.Lerp(0.10f, 1f, chargeRatio);
            int damage = Math.Max(1, (int)(player.HeldItem.damage * releasePower));
            modPlayer.LeftSpecialCooldown = ReleaseCooldown;

            if (perfect)
                SoundEngine.PlaySound(SoundID.Item30 with { Pitch = 0.6f, Volume = 0.85f }, player.Center);

            var source = player.GetSource_ItemUse(player.HeldItem);
            Vector2 targetDir = (player.Calamity().mouseWorld - player.Center)
                .SafeNormalize(Vector2.UnitX * player.direction);

            switch (modPlayer.CurrentMode)
            {
                case 0:
                {
                    Vector2 mouseOffset = player.Calamity().mouseWorld - player.Center;
                    Vector2 bladeDir = mouseOffset.SafeNormalize(Vector2.UnitX * player.direction);
                    float bladeRadius = MathHelper.Clamp(mouseOffset.Length(), 100f, 260f);
                    Projectile.NewProjectile(source, player.Center + bladeDir * bladeRadius, Vector2.Zero,
                        ModContent.ProjectileType<IceBladeProj>(), damage, player.HeldItem.knockBack,
                        player.whoAmI, bladeDir.ToRotation(), bladeRadius);
                    break;
                }
                case 1:
                    Projectile.NewProjectile(source, player.Center, targetDir * 14f,
                        ModContent.ProjectileType<IceWedgeProj>(), damage, player.HeldItem.knockBack, player.whoAmI);
                    break;
                default:
                    Projectile.NewProjectile(source, player.Center, targetDir * 12f,
                        ModContent.ProjectileType<IcePillarProj>(),
                        damage, player.HeldItem.knockBack * 1.8f, player.whoAmI);
                    break;
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

            float progress = Projectile.ai[0] / MaxCharge;
            float time = Main.GlobalTimeWrappedHourly;
            Texture2D pixel = TextureAssets.MagicPixel.Value;
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;

            // ── 形态蓄力视觉效果 ──
            if (modPlayer.CurrentMode == 0)
            {
                Vector2 mousePos = player.Calamity().mouseWorld - Main.screenPosition;
                Color bladeCol = new Color(0, 195, 255, 0) * progress * 0.55f;
                float ringRad = (1f - progress) * 70f + 10f;
                Main.spriteBatch.Draw(bloom, mousePos, null, bladeCol * 0.4f, 0f,
                    bloom.Size() * 0.5f, ringRad / bloom.Width, SpriteEffects.None, 0f);
                Vector2 lineStart = player.Center - Main.screenPosition;
                Vector2 lineEnd = mousePos;
                Main.spriteBatch.Draw(pixel, lineStart, new Rectangle(0, 0, 1, 1), bladeCol * 0.3f,
                    (lineEnd - lineStart).ToRotation(), new Vector2(0f, 0.5f),
                    new Vector2((lineEnd - lineStart).Length(), 1.5f), SpriteEffects.None, 0f);
            }
            else if (modPlayer.CurrentMode == 1)
            {
                Vector2 headPos = player.Top + new Vector2(0f, -40f) - Main.screenPosition;
                Color wedgeCol = new Color(130, 230, 255, 0) * progress * 0.6f;
                Main.spriteBatch.Draw(bloom, headPos, null, wedgeCol, time * 2f,
                    bloom.Size() * 0.5f, 0.25f * progress, SpriteEffects.None, 0f);
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
                Vector2 frontPos = player.Center
                    + (player.Calamity().mouseWorld - player.Center)
                        .SafeNormalize(Vector2.UnitX * player.direction) * 40f
                    - Main.screenPosition;
                Color pillarCol = Color.Lerp(Color.Cyan, Color.OrangeRed,
                    0.2f * MathF.Sin(time * 5f)) * progress * 0.7f;
                Main.spriteBatch.Draw(bloom, frontPos, null, pillarCol, 0f,
                    bloom.Size() * 0.5f, 0.4f * progress, SpriteEffects.None, 0f);
            }

            // ── 左键蓄力进度条（头顶）──
            Vector2 barTopLeft = player.Top - Main.screenPosition + new Vector2(-50f, -28f);
            int barW = 100;
            int barH = 9;

            Main.spriteBatch.Draw(pixel, new Rectangle((int)barTopLeft.X, (int)barTopLeft.Y, barW, barH),
                new Color(20, 30, 50, 200));

            // 完美区高亮
            int perfectStartPx = (int)(barW * (PerfectMin / (float)MaxCharge));
            Main.spriteBatch.Draw(pixel,
                new Rectangle((int)barTopLeft.X + perfectStartPx, (int)barTopLeft.Y,
                    barW - perfectStartPx, barH),
                new Color(0, 200, 100, 80));

            // 蓄力填充
            bool isPerfect = Projectile.ai[0] >= PerfectMin;
            Color fillCol = isPerfect
                ? Color.Lerp(new Color(0, 220, 120), new Color(0, 255, 200),
                    0.5f + 0.5f * MathF.Sin(time * 10f))
                : new Color(50, 170, 255);
            int fillW = (int)(progress * barW);
            if (fillW > 0)
                Main.spriteBatch.Draw(pixel,
                    new Rectangle((int)barTopLeft.X, (int)barTopLeft.Y, fillW, barH),
                    fillCol * 0.9f);

            // 游标
            int cursorX = (int)(barTopLeft.X + progress * barW);
            Main.spriteBatch.Draw(pixel,
                new Rectangle(cursorX - 1, (int)barTopLeft.Y - 1, 2, barH + 2),
                Color.White * 0.9f);

            // 标签文字
            string label = isPerfect ? "PERFECT!" : "HOLD";
            Color labelCol = isPerfect ? new Color(0, 255, 180) : new Color(160, 220, 255);
            Utils.DrawBorderStringFourWay(Main.spriteBatch, FontAssets.MouseText.Value, label,
                barTopLeft.X + barW * 0.5f - 18f, barTopLeft.Y - 16f, labelCol, Color.Black,
                Vector2.Zero, 0.72f);

            return false;
        }
    }
}
