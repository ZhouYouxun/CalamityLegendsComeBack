using CalamityMod;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Dev.M4A1
{
    /// <summary>
    /// 大招锁定框：收束锁定动画后向目标发射一枚追踪炮弹，随后淡出。
    /// ai[0] = 目标 NPC 索引，ai[1] = 起始延迟（错开齐射节奏），ai[2] = 炮弹伤害。
    /// </summary>
    public class M4A1LockOnBox : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private const int LockFrames = 16;
        private const int FadeFrames = 12;
        private const float ShellSpeed = 24f;

        private int Delay => (int)Projectile.ai[1];
        private int ShellDamage => (int)Projectile.ai[2];

        private int timer;
        private bool fired;
        private Vector2 lastTargetCenter;
        private Vector2 boxSize = new(40f);

        private Player Owner => Main.player[Projectile.owner];

        public override void SetDefaults()
        {
            Projectile.width = 2;
            Projectile.height = 2;
            Projectile.friendly = false;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 240;
            Projectile.netImportant = true;
        }

        public override bool? CanDamage() => false;
        public override bool ShouldUpdatePosition() => false;

        public override void AI()
        {
            NPC target = GetTarget();
            if (target != null)
            {
                lastTargetCenter = target.Center;
                boxSize = target.Size + new Vector2(10f);
                Projectile.Center = target.Center;
            }
            else
            {
                Projectile.Center = lastTargetCenter;
            }

            if (timer < Delay)
            {
                timer++;
                Projectile.timeLeft = 2;
                return;
            }

            int localTimer = timer - Delay;

            if (localTimer == 0)
                SoundEngine.PlaySound(SoundID.Item41 with { Volume = 0.4f, Pitch = 0.5f }, Projectile.Center);

            if (!fired && localTimer >= LockFrames)
            {
                fired = true;
                FireShell(target);
            }

            if (localTimer >= LockFrames + FadeFrames)
            {
                Projectile.Kill();
                return;
            }

            timer++;
            Projectile.timeLeft = 2;
        }

        private NPC GetTarget()
        {
            int idx = (int)Projectile.ai[0];
            if (idx < 0 || idx >= Main.maxNPCs)
                return null;
            NPC npc = Main.npc[idx];
            return npc.active && !npc.friendly ? npc : null;
        }

        private void FireShell(NPC target)
        {
            SoundEngine.PlaySound(SoundID.Item61 with { Volume = 0.7f, Pitch = 0.1f }, Projectile.Center);

            if (Projectile.owner != Main.myPlayer)
                return;

            Vector2 spawnPos = Owner.MountedCenter;
            Vector2 aim = ((target != null ? target.Center : lastTargetCenter) - spawnPos).SafeNormalize(Vector2.UnitY * -1f);
            int targetIndex = target != null ? target.whoAmI : -1;

            int index = Projectile.NewProjectile(
                Owner.GetSource_Misc("M4A1Ultimate"),
                spawnPos,
                aim * ShellSpeed,
                ModContent.ProjectileType<M4A1UltimateShell>(),
                ShellDamage,
                8f,
                Projectile.owner,
                targetIndex);

            if (Main.projectile.IndexInRange(index))
            {
                Main.projectile[index].DamageType = DamageClass.Ranged;
                Main.projectile[index].CritChance = Owner.GetWeaponCrit(Owner.HeldItem);
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (timer < Delay)
                return false;

            int localTimer = timer - Delay;
            Texture2D pixel = TextureAssets.MagicPixel.Value;

            float lockProgress = MathHelper.Clamp(localTimer / (float)LockFrames, 0f, 1f);
            float ease = 1f - (1f - lockProgress) * (1f - lockProgress);
            float fade = localTimer > LockFrames ? 1f - (localTimer - LockFrames) / (float)FadeFrames : 1f;
            float lockFlash = fired && localTimer < LockFrames + 4 ? 1f : 0f;

            // 收束：从大框缩到贴合目标
            float expand = MathHelper.Lerp(1.9f, 1f, ease);
            Vector2 half = boxSize * 0.5f * expand;
            Vector2 center = Projectile.Center - Main.screenPosition;
            Color color = Color.Lerp(M4A1Visuals.MarkColor, Color.White, lockFlash * 0.7f) * fade;

            Rectangle box = new((int)(center.X - half.X), (int)(center.Y - half.Y), (int)(half.X * 2f), (int)(half.Y * 2f));
            int corner = Math.Clamp((int)Math.Min(half.X, half.Y) / 2, 6, 22);
            DrawCorners(pixel, box, corner, 2, color);

            // 十字准星
            Main.spriteBatch.Draw(pixel, new Rectangle((int)center.X - 1, (int)center.Y - 6, 2, 12), color * 0.9f);
            Main.spriteBatch.Draw(pixel, new Rectangle((int)center.X - 6, (int)center.Y - 1, 12, 2), color * 0.9f);

            // 旋转外圈四点
            float ringRot = localTimer * 0.2f;
            float ringR = half.Length() * 0.7f + 6f;
            for (int i = 0; i < 4; i++)
            {
                Vector2 p = center + (ringRot + MathHelper.PiOver2 * i).ToRotationVector2() * ringR;
                Main.spriteBatch.Draw(pixel, p, new Rectangle(0, 0, 1, 1), color, MathHelper.PiOver4, new Vector2(0.5f), 3f, SpriteEffects.None, 0f);
            }

            return false;
        }

        private static void DrawCorners(Texture2D pixel, Rectangle box, int len, int thick, Color color)
        {
            void H(int x, int y, int w) => Main.spriteBatch.Draw(pixel, new Rectangle(x, y, w, thick), color);
            void V(int x, int y, int h) => Main.spriteBatch.Draw(pixel, new Rectangle(x, y, thick, h), color);

            H(box.Left, box.Top, len); V(box.Left, box.Top, len);
            H(box.Right - len, box.Top, len); V(box.Right - thick, box.Top, len);
            H(box.Left, box.Bottom - thick, len); V(box.Left, box.Bottom - len, len);
            H(box.Right - len, box.Bottom - thick, len); V(box.Right - thick, box.Bottom - len, len);
        }
    }
}
