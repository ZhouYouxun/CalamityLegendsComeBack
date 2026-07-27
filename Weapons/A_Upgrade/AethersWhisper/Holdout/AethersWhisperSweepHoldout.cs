using System;
using CalamityMod;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

using CalamityLegendsComeBack.Weapons.A_Upgrade.AethersWhisper.Shared;
using CalamityLegendsComeBack.Weapons.A_Upgrade.AethersWhisper.RightClick;

namespace CalamityLegendsComeBack.Weapons.A_Upgrade.AethersWhisper.Holdout
{
    /// <summary>
    /// 右键：微光折返扫射的持握 / 节奏控制器（文档第 4.1 节）。
    /// 只负责：跟随鼠标持枪、以固定 0/7/14/21 tick 发出四束伪激光、第 36 tick 起下一小节、逐束扣魔。
    /// 每束的锁定准星、反射、终点判定与分解由 <see cref="AethersWhisperRefractionBeam"/> 自理。
    /// </summary>
    internal sealed class AethersWhisperSweepHoldout : ModProjectile, ILocalizedModType
    {
        private const float HoldoutDistance = 30f;
        private const float BarrelLength = 62f;

        public new string LocalizationCategory => "Projectiles.AethersWhisper";
        public override string Texture => "CalamityMod/Items/Weapons/Magic/AethersWhisper";

        private Player Owner => Main.player[Projectile.owner];
        private Vector2 AimDirection => Projectile.velocity.SafeNormalize(Vector2.UnitX * Owner.direction);
        private Vector2 GunTip => Owner.MountedCenter + AimDirection * BarrelLength + new Vector2(0f, -6f * Owner.gravDir);

        private int roundTick;
        private int beamsFiredThisRound;
        private int muzzleFlashTicks;

        public override void SetDefaults()
        {
            Projectile.width = 134;
            Projectile.height = 44;
            Projectile.friendly = false;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.netImportant = true;
        }

        public override bool ShouldUpdatePosition() => false;
        public override bool? CanDamage() => false;

        public override void AI()
        {
            bool localRightHeld = Main.mouseRight || Owner.Calamity().mouseRight;

            if (!Owner.active || Owner.dead || Owner.noItems || Owner.CCed ||
                Owner.HeldItem.type != ModContent.ItemType<AethersWhisper>() ||
                (Main.myPlayer == Projectile.owner && !localRightHeld && roundTick > 0))
            {
                Projectile.Kill();
                return;
            }

            Projectile.timeLeft = 2;
            if (muzzleFlashTicks > 0)
                muzzleFlashTicks--;

            Owner.Calamity().mouseWorldListener = true;
            if (Main.myPlayer == Projectile.owner)
                Owner.Calamity().rightClickListener = true;

            UpdatePose();

            if (Main.myPlayer == Projectile.owner)
                RunBurstRhythm(localRightHeld);

            Lighting.AddLight(GunTip, AethersWhisperVisuals.ShimmerCyan.ToVector3() * 0.35f);
        }

        private void RunBurstRhythm(bool rightHeld)
        {
            // 到达本束的发射 tick 就射出（每小节严格 4 束，无第五束、无随机补射）。
            for (int i = beamsFiredThisRound; i < AethersWhisperBalance.BeamsPerRound; i++)
            {
                if (roundTick != AethersWhisperBalance.BeamFireTicks[i])
                    continue;

                // 逐束扣魔；任一束扣魔失败立刻结束本轮，不补发、不透支。
                if (!Owner.CheckMana(AethersWhisperBalance.RightManaPerBeam, true))
                {
                    SoundEngine.PlaySound(SoundID.MenuTick with { Volume = 0.25f, Pitch = -0.5f }, Owner.Center);
                    Projectile.Kill();
                    return;
                }

                FireBeam(i);
                beamsFiredThisRound++;
                break;
            }

            roundTick++;

            // 第 36 tick：仍按住且魔力足够则开始下一小节；否则收束控制器。
            if (roundTick >= AethersWhisperBalance.RoundRestartTick)
            {
                if (rightHeld)
                {
                    roundTick = 0;
                    beamsFiredThisRound = 0;
                }
                else
                {
                    Projectile.Kill();
                }
            }
        }

        private void FireBeam(int beamIndex)
        {
            Vector2 tip = GunTip;
            Vector2 aimWorld = Main.MouseWorld; // 每束出生时锁定当时鼠标世界坐标
            Vector2 dir = (aimWorld - tip).SafeNormalize(AimDirection);

            int weaponDamage = Owner.GetWeaponDamage(Owner.HeldItem);
            int damage = Math.Max(1, (int)(weaponDamage * AethersWhisperBalance.BeamDamageMult));

            int beam = Projectile.NewProjectile(
                Projectile.GetSource_FromThis(),
                tip,
                dir * AethersWhisperBalance.BeamSpeed,
                ModContent.ProjectileType<AethersWhisperRefractionBeam>(),
                damage,
                AethersWhisperBalance.KnockBack,
                Projectile.owner,
                aimWorld.X,
                aimWorld.Y);
            if (Main.projectile.IndexInRange(beam))
            {
                Main.projectile[beam].CritChance = Owner.GetWeaponCrit(Owner.HeldItem);
                Main.projectile[beam].netUpdate = true;
            }

            muzzleFlashTicks = 6;
            // 四束音高逐渐提高 0.03（文档 5.5）。
            SoundEngine.PlaySound(SoundID.Item91 with { Volume = 0.4f, Pitch = 0.15f + beamIndex * 0.03f, MaxInstances = 4 }, tip);

            if (!Main.dedServ)
            {
                GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(
                    tip, dir * 0.5f, AethersWhisperVisuals.ShimmerCyan,
                    new Vector2(0.22f, 1.1f), dir.ToRotation(), 0.04f, 0.55f, 12));
            }
        }

        private void UpdatePose()
        {
            Vector2 desiredAim = (Main.MouseWorld - Owner.MountedCenter).SafeNormalize(Vector2.UnitX * Owner.direction);
            if (Projectile.owner == Main.myPlayer)
            {
                Projectile.velocity = Vector2.Lerp(AimDirection, desiredAim, 0.5f).SafeNormalize(desiredAim);
                Projectile.netUpdate = true;
            }

            Vector2 aim = AimDirection;
            int dir = aim.X >= 0f ? 1 : -1;
            Projectile.spriteDirection = Projectile.direction = dir;
            Projectile.rotation = aim.ToRotation();
            Projectile.Center = Owner.MountedCenter + aim * HoldoutDistance + new Vector2(0f, -10f * Owner.gravDir);

            Owner.ChangeDir(dir);
            Owner.heldProj = Projectile.whoAmI;
            Owner.itemRotation = (aim * dir).ToRotation();
            Owner.itemTime = Owner.itemAnimation = 2;

            float armRot = (Projectile.rotation - MathHelper.PiOver2) * Owner.gravDir;
            if (Owner.gravDir == -1f)
                armRot += MathHelper.Pi;
            Owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, armRot);
            Owner.SetCompositeArmBack(true, Player.CompositeArmStretchAmount.ThreeQuarters, armRot + MathHelper.ToRadians(9f) * dir);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D gun = TextureAssets.Projectile[Type].Value;
            Vector2 drawPos = Projectile.Center - Main.screenPosition;
            Vector2 origin = gun.Size() * 0.5f;
            SpriteEffects fx = Projectile.spriteDirection == -1 ? SpriteEffects.FlipVertically : SpriteEffects.None;

            Main.EntitySpriteDraw(gun, drawPos, null, AethersWhisperVisuals.AetherPurple with { A = 0 } * 0.25f,
                Projectile.rotation, origin, Projectile.scale * 1.04f, fx, 0);
            Main.EntitySpriteDraw(gun, drawPos, null, Projectile.GetAlpha(lightColor),
                Projectile.rotation, origin, Projectile.scale, fx, 0);

            if (muzzleFlashTicks > 0)
            {
                SpriteBatch sb = Main.spriteBatch;
                AethersWhisperVisuals.BeginAdditive(sb);
                Texture2D bloom = AethersWhisperVisuals.BloomCircle.Value;
                float p = muzzleFlashTicks / 6f;
                Vector2 aim = AimDirection;
                sb.Draw(bloom, GunTip - Main.screenPosition, null, AethersWhisperVisuals.ShimmerCyan with { A = 0 } * p,
                    aim.ToRotation(), bloom.Size() * 0.5f, new Vector2(0.3f, 0.14f) * (1f + p), SpriteEffects.None, 0f);
                AethersWhisperVisuals.EndAdditive(sb);
            }

            Owner.heldProj = Projectile.whoAmI;
            return false;
        }
    }
}
