using System;
using CalamityLegendsComeBack.Weapons.A_Upgrade.Pisces.Shared;
using CalamityMod;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Upgrade.Pisces.LeftClick
{
    /// <summary>
    /// 左键硫火喷吐的手持鱼。它只负责把 Dragoon Drizzlefish 作为可见手持弹幕画出来，
    /// 并用 SHPC 同款“快 kick - 缓回位”后坐让每次喷吐有明确动作；实际火球仍由 Pisces.Shoot 生成。
    /// </summary>
    public sealed class PiscesBrimstoneHoldout : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.Pisces";
        public override string Texture => "CalamityMod/Items/Fishing/BrimstoneCragCatches/DragoonDrizzlefish";

        private const int KickFrames = 4;
        private const int ReturnFrames = 12;
        private const float MaxRecoil = 13f;
        // Dragoon Drizzlefish 原图的嘴朝右下（相对水平约 +45°）；手持时要先逆转这 45° 才会朝准星。
        private const float FishArtRotation = -MathHelper.PiOver4;

        private int timer;
        private float recoilOffset;
        private Vector2 aimDirection;

        private Player Owner => Main.player[Projectile.owner];

        public override void SetDefaults()
        {
            Projectile.width = 2;
            Projectile.height = 2;
            Projectile.friendly = false;
            Projectile.hostile = false;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 30;
        }

        public override bool? CanDamage() => false;
        public override bool ShouldUpdatePosition() => false;

        public override void AI()
        {
            if (!Owner.active || Owner.dead || Owner.HeldItem.type != ModContent.ItemType<Pisces>() ||
                Owner.ownedProjectileCounts[ModContent.ProjectileType<RightClick.PiscesOpticHoldout>()] > 0)
            {
                Projectile.Kill();
                return;
            }

            Projectile.timeLeft = 2;
            UpdateAimAndPose();
            UpdateRecoil();
            SpawnMuzzleEmbers();
            timer++;
        }

        private void UpdateAimAndPose()
        {
            Vector2 arm = Owner.RotatedRelativePoint(Owner.MountedCenter, true);
            Vector2 target = Projectile.owner == Main.myPlayer ? Main.MouseWorld : Owner.Calamity().mouseWorld;
            aimDirection = (target - arm).SafeNormalize(Vector2.UnitX * Owner.direction);
            int dir = aimDirection.X >= 0f ? 1 : -1;

            Owner.ChangeDir(dir);
            Owner.heldProj = Projectile.whoAmI;
            Projectile.direction = Projectile.spriteDirection = dir;
            Projectile.rotation = aimDirection.ToRotation();
            Projectile.Center = arm + aimDirection * 30f;

            float armRotation = (Projectile.rotation - MathHelper.PiOver2) * Owner.gravDir;
            if (Owner.gravDir == -1f)
                armRotation += MathHelper.Pi;
            Owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, armRotation);
        }

        private void UpdateRecoil()
        {
            if (timer < KickFrames)
                recoilOffset = MathF.Sin(timer / (float)KickFrames * MathHelper.PiOver2) * MaxRecoil;
            else if (timer < ReturnFrames)
                recoilOffset = MathF.Cos((timer - KickFrames) / (float)(ReturnFrames - KickFrames) * MathHelper.PiOver2) * MaxRecoil;
            else
                Projectile.Kill();
        }

        private void SpawnMuzzleEmbers()
        {
            if (Main.dedServ || timer > 6 || !Main.rand.NextBool(2))
                return;

            Vector2 muzzle = Projectile.Center + aimDirection * (22f - recoilOffset);
            Dust dust = Dust.NewDustPerfect(muzzle, Main.rand.NextBool() ? 183 : 90,
                aimDirection.RotatedByRandom(0.3f) * Main.rand.NextFloat(2f, 5f), 20, PiscesVisuals.EmberOrange,
                Main.rand.NextFloat(0.8f, 1.2f));
            dust.noGravity = true;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (aimDirection == Vector2.Zero)
                return false;

            Texture2D texture = TextureAssets.Projectile[Type].Value;
            Vector2 drawCenter = Projectile.Center - aimDirection * recoilOffset - Main.screenPosition;
            float drawRotation = Projectile.rotation + FishArtRotation + (Projectile.spriteDirection == -1 ? MathHelper.Pi : 0f);
            SpriteEffects flip = (Projectile.spriteDirection * Owner.gravDir == -1)
                ? SpriteEffects.FlipHorizontally
                : SpriteEffects.None;

            // 物品本体 + 一层近距离硫火描边，保留“举起那条鱼”而不是普通枪械的读感。
            for (int i = 0; i < 6; i++)
            {
                Vector2 offset = (MathHelper.TwoPi * i / 6f).ToRotationVector2() * 1.7f;
                Main.EntitySpriteDraw(texture, drawCenter + offset, null, PiscesVisuals.BrimstoneRed with { A = 0 } * 0.32f,
                    drawRotation, texture.Size() * 0.5f, Projectile.scale, flip, 0f);
            }
            Main.EntitySpriteDraw(texture, drawCenter, null, Color.White, drawRotation, texture.Size() * 0.5f,
                Projectile.scale, flip, 0f);
            return false;
        }

        public static void KillOwnedBy(int playerIndex)
        {
            int type = ModContent.ProjectileType<PiscesBrimstoneHoldout>();
            foreach (Projectile projectile in Main.ActiveProjectiles)
            {
                if (projectile.active && projectile.owner == playerIndex && projectile.type == type)
                    projectile.Kill();
            }
        }
    }
}
