using System;
using CalamityLegendsComeBack.Weapons.A_Upgrade.Pisces.Shared;
using CalamityMod;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Upgrade.Pisces.RightClick
{
    /// <summary>
    /// 右键的冷静光学持械——只管理右键，从不扫描全图。玩家按住时始终朝鼠标旋转、可自由移动，
    /// 蓄力逐级提纯弹体质量：I 校准 → II 聚焦 → III 北辰锁定 → 满蓄双束神圣激光。
    /// 松开时按当前 ChargeTier 发射对应光弹；蓄到满级松开则停火一拍短前摇后放两道短激光。
    /// 持械弹幕自持（读 <see cref="Main.mouseRight"/>），松手后短暂空置以便连续点射复用，不占用 itemAnimation（切武器 / 滚轮不卡手）。
    /// </summary>
    public sealed class PiscesOpticHoldout : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.Pisces";
        public override string Texture => "CalamityMod/Items/Weapons/Ranged/PolarisParrotfish";

        public Player Owner => Main.player[Projectile.owner];

        // 同步：ai[0] = 蓄力 tick，ai[1] = 满蓄前摇剩余（>0 表示进入满蓄释放）。
        public ref float ChargeTicks => ref Projectile.ai[0];
        public ref float WindupTimer => ref Projectile.ai[1];

        private const float BarrelLength = 40f;
        private const float IdleOffset = 22f;
        // Polaris Parrotfish 也是右下斜向原图；仅修正物品绘制，瞄准/枪口仍保持真实射击方向。
        private const float FishArtRotation = -MathHelper.PiOver4;

        private float offsetLength = IdleOffset;
        private float recoilOffset;
        private int recoilTimer;
        private int muzzleFlashTimer;
        private int idleTicks;
        private int lastTierCue = -1;
        private int sustainedShotTimer;
        private int rapidLaserTimer;

        private Vector2 AimDirection => Projectile.velocity.SafeNormalize(Vector2.UnitX * Owner.direction);
        public Vector2 GunTipPosition => Owner.MountedCenter + AimDirection * (BarrelLength - recoilOffset) + new Vector2(0f, -4f * Owner.gravDir);

        private int Tier => PiscesBalance.ChargeTier((int)ChargeTicks);
        private float ChargeFraction => MathHelper.Clamp(ChargeTicks / PiscesBalance.MaxChargeTicks, 0f, 1f);
        private bool InWindup => WindupTimer > 0f;

        public override void SetDefaults()
        {
            Projectile.width = 40;
            Projectile.height = 40;
            Projectile.friendly = false;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.DamageType = DamageClass.Ranged;
        }

        public override bool? CanDamage() => false;
        public override bool ShouldUpdatePosition() => false;

        public override void AI()
        {
            Projectile.timeLeft = 2;

            if (Owner.HeldItem.type != ModContent.ItemType<Pisces>() || Owner.dead || !Owner.active || Owner.CCed)
            {
                Projectile.Kill();
                return;
            }

            if (recoilTimer > 0) recoilTimer--;
            if (muzzleFlashTimer > 0) muzzleFlashTimer--;
            recoilOffset = MathHelper.Lerp(recoilOffset, 0f, 0.2f);

            Owner.Calamity().mouseWorldListener = true;
            if (Main.myPlayer == Projectile.owner)
            {
                Owner.Calamity().rightClickListener = true;
                HandleInput();
            }

            UpdatePose();
            Color glow = InWindup ? PiscesVisuals.AuroraWhite : PiscesVisuals.AuroraLerp(ChargeFraction);
            Lighting.AddLight(GunTipPosition, glow.ToVector3() * (0.25f + ChargeFraction * 0.5f));
        }

        private void HandleInput()
        {
            bool canInput = CanUseWorldInput();
            bool rightHeld = (Main.mouseRight || Owner.Calamity().mouseRight) && canInput;

            // 满蓄释放：短前摇后放双束激光
            if (InWindup)
            {
                WindupTimer--;
                SpawnWindupGather();
                if (WindupTimer <= 0f)
                    FireHolyBeams();
                return;
            }

            if (rightHeld)
            {
                if (ChargeTicks == 0)
                    SoundEngine.PlaySound(SoundID.Item15 with { Volume = 0.35f, Pitch = -0.4f }, GunTipPosition);
                ChargeTicks = Math.Min(ChargeTicks + 1, PiscesBalance.ChargeCap);
                idleTicks = 0;
                PlayTierCues();
                SpawnChargeConvergence();
                FireWhileHeld();
                return;
            }

            // 松开
            if (ChargeTicks > 0)
            {
                ReleaseCharge();
                return;
            }

            // 空置：短暂保留以便连续点射复用；若玩家转去左键喷吐则立刻收起，超时也收起。
            idleTicks++;
            if (Main.mouseLeft || idleTicks > PiscesBalance.HoldoutIdleLinger)
                Projectile.Kill();
        }

        private void ReleaseCharge()
        {
            int tier = Tier;
            if (tier >= 3)
            {
                // 满蓄：进入短前摇（停火一拍），不立即发射。
                WindupTimer = PiscesBalance.MaxChargeWindup;
                SoundEngine.PlaySound(SoundID.Item15 with { Volume = 0.5f, Pitch = -0.8f }, GunTipPosition);
                SoundEngine.PlaySound(SoundID.Item82 with { Volume = 0.4f, Pitch = 0.3f }, GunTipPosition);
                lastTierCue = -1;
                return;
            }

            // 非满蓄阶段的弹幕已经在按住期间持续射出；松手只结束本轮蓄力。
            ChargeTicks = 0;
            sustainedShotTimer = 0;
            rapidLaserTimer = 0;
            lastTierCue = -1;
        }

        private void FireWhileHeld()
        {
            int shotTier = Math.Min(Tier, 2);
            sustainedShotTimer++;
            if (sustainedShotTimer >= PiscesBalance.SustainedShotInterval(shotTier))
            {
                sustainedShotTimer = 0;
                FirePolarShot(shotTier);
            }

            // 满蓄后仍持续投射北辰重弹，并间歇打出短促快速激光；松手才保留双束终结。
            if (Tier >= 3)
            {
                rapidLaserTimer++;
                if (rapidLaserTimer >= PiscesBalance.FullChargeRapidLaserInterval)
                {
                    rapidLaserTimer = 0;
                    FireRapidLaser();
                }
            }
            else
                rapidLaserTimer = 0;
        }

        private void FirePolarShot(int tier)
        {
            tier = Math.Min(tier, 2);
            Vector2 aim = AimDirection;
            float speed = PiscesBalance.PolarShotBaseSpeed * PiscesBalance.TierSpeedMult(tier);
            int weaponDamage = Owner.GetWeaponDamage(Owner.HeldItem);
            int damage = Math.Max(1, (int)(weaponDamage * PiscesBalance.TierDamageMult(tier)));

            if (Main.myPlayer == Projectile.owner)
            {
                int shot = Projectile.NewProjectile(Projectile.GetSource_FromThis(), GunTipPosition, aim * speed,
                    ModContent.ProjectileType<PiscesPolarShot>(), damage, PiscesBalance.KnockBack, Projectile.owner, tier, weaponDamage);
                if (Main.projectile.IndexInRange(shot))
                {
                    Main.projectile[shot].CritChance = Owner.GetWeaponCrit(Owner.HeldItem);
                    Main.projectile[shot].netUpdate = true;
                }
            }

            // 每级清晰的音高与枪口形态差异
            switch (tier)
            {
                case 0:
                    SoundEngine.PlaySound(SoundID.Item12 with { Volume = 0.5f, Pitch = 0.35f }, GunTipPosition);
                    break;
                case 1:
                    SoundEngine.PlaySound(SoundID.Item12 with { Volume = 0.5f, Pitch = 0.55f }, GunTipPosition);
                    SoundEngine.PlaySound(SoundID.Item12 with { Volume = 0.35f, Pitch = 0.85f }, GunTipPosition);
                    break;
                default:
                    SoundEngine.PlaySound(SoundID.Item72 with { Volume = 0.55f, Pitch = 0.2f }, GunTipPosition);
                    SoundEngine.PlaySound(SoundID.Item68 with { Volume = 0.35f, Pitch = 0.6f }, GunTipPosition);
                    break;
            }

            recoilOffset = 6f + tier * 3f;
            recoilTimer = 8;
            muzzleFlashTimer = 8 + tier * 2;
            SpawnMuzzleBurst(aim, tier);
        }

        private void FireRapidLaser()
        {
            Vector2 aim = AimDirection;
            int weaponDamage = Owner.GetWeaponDamage(Owner.HeldItem);
            int damage = Math.Max(1, (int)(weaponDamage * PiscesBalance.RapidLaserDamageMult));
            if (Main.myPlayer == Projectile.owner)
            {
                int beam = Projectile.NewProjectile(Projectile.GetSource_FromThis(), GunTipPosition, aim,
                    ModContent.ProjectileType<PiscesHolyBeam>(), damage, PiscesBalance.KnockBack, Projectile.owner,
                    0f, Projectile.whoAmI, -weaponDamage);
                if (Main.projectile.IndexInRange(beam))
                {
                    Main.projectile[beam].CritChance = Owner.GetWeaponCrit(Owner.HeldItem);
                    Main.projectile[beam].netUpdate = true;
                }
            }

            SoundEngine.PlaySound(SoundID.Item12 with { Volume = 0.35f, Pitch = 0.9f }, GunTipPosition);
            recoilOffset = Math.Max(recoilOffset, 9f);
            recoilTimer = 5;
            muzzleFlashTimer = 7;
            SpawnRapidLaserFlash(aim);
        }

        private void FireHolyBeams()
        {
            Vector2 aim = AimDirection;
            int weaponDamage = Owner.GetWeaponDamage(Owner.HeldItem);
            int beamDamage = Math.Max(1, (int)(weaponDamage * PiscesBalance.HolyBeamDamageMult));

            if (Main.myPlayer == Projectile.owner)
            {
                for (int side = -1; side <= 1; side += 2)
                {
                    int beam = Projectile.NewProjectile(Projectile.GetSource_FromThis(), GunTipPosition, aim,
                        ModContent.ProjectileType<PiscesHolyBeam>(), beamDamage, PiscesBalance.KnockBack, Projectile.owner, side, Projectile.whoAmI, weaponDamage);
                    if (Main.projectile.IndexInRange(beam))
                    {
                        Main.projectile[beam].CritChance = Owner.GetWeaponCrit(Owner.HeldItem);
                        Main.projectile[beam].netUpdate = true;
                    }
                }
            }

            SoundEngine.PlaySound(SoundID.Item72 with { Volume = 0.7f, Pitch = -0.2f }, GunTipPosition);
            SoundEngine.PlaySound(SoundID.Item122 with { Volume = 0.6f, Pitch = 0.3f }, GunTipPosition);
            Owner.Calamity().GeneralScreenShakePower = Math.Max(Owner.Calamity().GeneralScreenShakePower, 3.5f);

            recoilOffset = 16f;
            recoilTimer = 10;
            muzzleFlashTimer = 14;
            SpawnMuzzleBurst(aim, 3);

            ChargeTicks = 0;
            WindupTimer = 0f;
            idleTicks = 0;
        }

        private void PlayTierCues()
        {
            if (Tier != lastTierCue)
            {
                lastTierCue = Tier;
                if (Tier == 1)
                    SoundEngine.PlaySound(SoundID.Item25 with { Volume = 0.3f, Pitch = 0.4f }, GunTipPosition);
                else if (Tier == 2)
                    SoundEngine.PlaySound(SoundID.Item29 with { Volume = 0.3f, Pitch = 0.7f }, GunTipPosition); // 锁定提示
            }
        }

        private bool CanUseWorldInput()
        {
            if (Owner.noItems || Owner.CCed || Main.mapFullscreen || Main.blockMouse || Owner.mouseInterface)
                return false;
            if (Main.playerInventory && !Main.HoverItem.IsAir)
                return false;
            return true;
        }

        private void UpdatePose()
        {
            Vector2 armPos = Owner.RotatedRelativePoint(Owner.MountedCenter, true);
            Vector2 desiredAim = (Main.MouseWorld - armPos).SafeNormalize(Vector2.UnitX * Owner.direction);

            if (Projectile.owner == Main.myPlayer)
            {
                Vector2 newVel = Projectile.velocity == Vector2.Zero ? desiredAim
                    : Vector2.Lerp(Projectile.velocity, desiredAim, 0.35f).SafeNormalize(desiredAim);
                if (Vector2.DistanceSquared(newVel, Projectile.velocity) > 0.0001f)
                    Projectile.netUpdate = true;
                Projectile.velocity = newVel;
            }

            offsetLength = MathHelper.Lerp(offsetLength, IdleOffset - recoilOffset, 0.3f);

            Vector2 aim = AimDirection;
            int dir = aim.X >= 0f ? 1 : -1;
            Projectile.spriteDirection = Projectile.direction = dir;
            Projectile.rotation = aim.ToRotation();
            Projectile.Center = armPos + aim * offsetLength + new Vector2(0f, -6f * Owner.gravDir);

            Owner.ChangeDir(dir);
            Owner.heldProj = Projectile.whoAmI;
            // 不设置 itemTime / itemAnimation —— 那会锁死滚轮切换。
            float armRot = (Projectile.rotation - MathHelper.PiOver2) * Owner.gravDir;
            if (Owner.gravDir == -1f) armRot += MathHelper.Pi;
            Owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, armRot);
            Owner.SetCompositeArmBack(true, Player.CompositeArmStretchAmount.ThreeQuarters, armRot + MathHelper.ToRadians(8f) * dir);
        }

        // ===== 视觉 =====
        private void SpawnChargeConvergence()
        {
            if (Main.dedServ)
                return;
            float charge = ChargeFraction;
            Vector2 tip = GunTipPosition;
            Vector2 aim = AimDirection;

            int every = Tier >= 2 ? 2 : (Tier >= 1 ? 3 : 5);
            if ((int)ChargeTicks % every == 0)
            {
                float dist = MathHelper.Lerp(90f, 20f, charge);
                Vector2 edge = tip + aim.RotatedByRandom(0.9f) * Main.rand.NextFloat(dist * 0.7f, dist);
                Vector2 inward = (tip - edge).SafeNormalize(Vector2.Zero) * Main.rand.NextFloat(1.6f, 3.4f);
                Dust d = Dust.NewDustPerfect(edge, PiscesVisuals.HolyDust, inward, 60, PiscesVisuals.AuroraLerp(charge), Main.rand.NextFloat(0.7f, 1.1f) * (0.7f + charge));
                d.noGravity = true;
            }
        }

        private void SpawnWindupGather()
        {
            if (Main.dedServ)
                return;
            Vector2 tip = GunTipPosition;
            for (int i = 0; i < 2; i++)
            {
                Vector2 edge = tip + Main.rand.NextVector2CircularEdge(30f, 30f);
                Dust d = Dust.NewDustPerfect(edge, PiscesVisuals.HolyDust, (tip - edge).SafeNormalize(Vector2.Zero) * 5f, 40, PiscesVisuals.AuroraWhite, Main.rand.NextFloat(0.9f, 1.4f));
                d.noGravity = true;
            }
        }

        private void SpawnMuzzleBurst(Vector2 aim, int tier)
        {
            if (Main.dedServ)
                return;
            Vector2 muzzle = GunTipPosition + aim * 4f;
            int count = 4 + tier * 3;
            Color band = tier >= 3 ? PiscesVisuals.AuroraWhite : PiscesVisuals.AuroraLerp(0.3f + tier * 0.2f);
            for (int i = 0; i < count; i++)
            {
                Vector2 vel = aim.RotatedByRandom(0.35f) * Main.rand.NextFloat(3f, 8f + tier * 2f);
                Dust d = Dust.NewDustPerfect(muzzle, PiscesVisuals.HolyDust, vel, 40, band, Main.rand.NextFloat(0.8f, 1.3f));
                d.noGravity = true;
            }
        }

        // 满蓄期间的周期快激光使用短促、定向的四点闪光；它是补拍，不应复用终结技的重型枪口爆发。
        private void SpawnRapidLaserFlash(Vector2 aim)
        {
            if (Main.dedServ)
                return;

            Vector2 muzzle = GunTipPosition + aim * 4f;
            for (int i = 0; i < 4; i++)
            {
                Vector2 velocity = aim.RotatedByRandom(0.16f) * Main.rand.NextFloat(3.5f, 6f);
                Dust dust = Dust.NewDustPerfect(muzzle, PiscesVisuals.HolyDust, velocity, 50,
                    PiscesVisuals.AuroraWhite, Main.rand.NextFloat(0.65f, 0.9f));
                dust.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Type].Value;
            Vector2 drawPos = Projectile.Center - Main.screenPosition;
            float drawRot = Projectile.rotation + FishArtRotation + (Projectile.spriteDirection == -1 ? MathHelper.Pi : 0f);
            Vector2 origin = texture.Size() * 0.5f;
            SpriteEffects flip = (Projectile.spriteDirection * Owner.gravDir == -1) ? SpriteEffects.FlipHorizontally : SpriteEffects.None;

            Main.EntitySpriteDraw(texture, drawPos, null, Projectile.GetAlpha(lightColor), drawRot, origin, Projectile.scale * Owner.gravDir, flip, 0);

            DrawMuzzleCore();
            return false;
        }

        /// <summary>枪口从小亮点(I) → 稳定环(II) → 有明确中心的镜片核(III/满蓄)——用 2-3 层小方/圆 bloom 表“折射镜片锁定”。</summary>
        private void DrawMuzzleCore()
        {
            if (Main.dedServ)
                return;
            float charge = ChargeFraction;
            float windup = InWindup ? 1f - WindupTimer / PiscesBalance.MaxChargeWindup : 0f;
            float power = MathHelper.Clamp(charge + windup, 0f, 1.4f);
            if (power <= 0.01f && muzzleFlashTimer <= 0)
                return;

            Vector2 tip = GunTipPosition;
            float flash = muzzleFlashTimer / 16f;
            PiscesVisuals.BeginAdditive(Main.spriteBatch);

            // 小亮点（常驻，随蓄力增强）
            PiscesVisuals.DrawBloom(Main.spriteBatch, tip, 0.06f + power * 0.08f + flash * 0.08f, PiscesVisuals.AuroraWhite, 0.5f + power * 0.4f);
            PiscesVisuals.DrawBloom(Main.spriteBatch, tip, 0.12f + power * 0.14f, PiscesVisuals.AuroraCyan, 0.35f + power * 0.35f);

            // II+ 稳定极光环
            if (Tier >= 1 || InWindup)
                PiscesVisuals.DrawRing(Main.spriteBatch, tip, 12f + power * 8f, Main.GlobalTimeWrappedHourly * 2.2f, PiscesVisuals.AuroraCyan, 0.4f + power * 0.35f);

            // III/满蓄 折射镜片锁定：2-3 层小方 bloom 层叠
            if (Tier >= 2 || InWindup)
            {
                Texture2D bloom = PiscesVisuals.BloomCircle.Value;
                for (int i = 0; i < 3; i++)
                {
                    float a = Main.GlobalTimeWrappedHourly * (1.2f + i * 0.4f) + i * MathHelper.TwoPi / 3f;
                    Vector2 p = tip + a.ToRotationVector2() * (10f - i * 2f);
                    Main.spriteBatch.Draw(bloom, p - Main.screenPosition, null, PiscesVisuals.GoldWhite with { A = 0 } * (0.3f + power * 0.3f),
                        a, bloom.Size() * 0.5f, 0.04f + power * 0.02f, SpriteEffects.None, 0f);
                }
            }
            PiscesVisuals.EndAdditive(Main.spriteBatch);
        }
    }
}
