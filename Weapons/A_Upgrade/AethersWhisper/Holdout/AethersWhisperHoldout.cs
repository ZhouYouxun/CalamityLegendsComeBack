using System;
using CalamityMod;
using CalamityMod.Enums;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

using CalamityLegendsComeBack.Weapons.A_Upgrade.AethersWhisper.Shared;
using CalamityLegendsComeBack.Weapons.A_Upgrade.AethersWhisper.LeftClick;

namespace CalamityLegendsComeBack.Weapons.A_Upgrade.AethersWhisper.Holdout
{
    /// <summary>
    /// 左键：微光坍缩炮的持握 / 蓄力控制器（文档第 3 节）。
    /// 只负责：跟随鼠标持枪、0→90 tick 蓄力、预瞄薄膜收束、松开发射晶核、满蓄后坐与声音断点。
    /// 不负责：右键热量 / 右键弹幕 / 无限持续光束（那些在 <see cref="AethersWhisperSweepHoldout"/>）。
    /// </summary>
    internal sealed class AethersWhisperHoldout : ModProjectile, ILocalizedModType
    {
        private const float HoldoutDistance = 30f;
        private const float BarrelLength = 62f;

        public new string LocalizationCategory => "Projectiles.AethersWhisper";
        public override string Texture => "CalamityMod/Items/Weapons/Magic/AethersWhisper";

        private Player Owner => Main.player[Projectile.owner];

        /// <summary>当前蓄力 tick（同时镜像到 ai[0] 供其它客户端做视觉）。</summary>
        private int chargeTicks;
        private int recoilTicks;
        private float recoilOffset;
        private int muzzleFlashTicks;
        private int lastPulseStep = -1;
        private bool playedFullReadySound;

        private Vector2 AimDirection => Projectile.velocity.SafeNormalize(Vector2.UnitX * Owner.direction);
        private Vector2 GunTip => Owner.MountedCenter + AimDirection * (BarrelLength - recoilOffset) + new Vector2(0f, -6f * Owner.gravDir);

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
            // 结束条件：玩家失效 / 切走武器 / 右键扫射接管。
            if (!Owner.active || Owner.dead || Owner.noItems || Owner.CCed ||
                Owner.HeldItem.type != ModContent.ItemType<AethersWhisper>() ||
                Owner.ownedProjectileCounts[ModContent.ProjectileType<AethersWhisperSweepHoldout>()] > 0)
            {
                Projectile.Kill();
                return;
            }

            Projectile.timeLeft = 2;

            if (recoilTicks > 0)
                recoilTicks--;
            if (muzzleFlashTicks > 0)
                muzzleFlashTicks--;
            recoilOffset = MathHelper.Lerp(recoilOffset, 0f, 0.2f);

            UpdatePose();

            if (Main.myPlayer == Projectile.owner)
                HandleInput();

            // 视觉同步：只有拥有者把蓄力进度写入 ai[0]，其它客户端只读它来画环
            // （若在非拥有者上也写，会把同步过来的进度覆盖为 0，导致他人看不到蓄力环）。
            if (Main.myPlayer == Projectile.owner)
                Projectile.ai[0] = chargeTicks;

            ApplyMovePenalty();
            PlayChargeSounds();
            Lighting.AddLight(GunTip, AethersWhisperVisuals.ShimmerCyan.ToVector3() * 0.4f * ChargeFraction01());
        }

        private float ChargeFraction01() => MathHelper.Clamp(chargeTicks / (float)AethersWhisperBalance.FullChargeTicks, 0f, 1f);
        private bool IsFull => chargeTicks >= AethersWhisperBalance.FullChargeTicks;

        private void HandleInput()
        {
            bool rightHeld = Main.mouseRight || Owner.Calamity().mouseRight;
            bool leftHeld = Main.mouseLeft && !rightHeld && CanUseWorldInput();

            if (leftHeld)
            {
                if (chargeTicks == 0)
                    SoundEngine.PlaySound(SoundID.Item15 with { Volume = 0.35f, Pitch = -0.6f }, GunTip);
                chargeTicks = Math.Min(chargeTicks + 1, AethersWhisperBalance.FullChargeTicks);
            }
            else if (chargeTicks > 0)
            {
                ReleaseShot();
                chargeTicks = 0;
                lastPulseStep = -1;
                playedFullReadySound = false;
                Projectile.netUpdate = true;
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

        private void ReleaseShot()
        {
            // 未达最小蓄力：取消，不耗魔、不发射。
            if (chargeTicks < AethersWhisperBalance.MinChargeTicks)
                return;

            // 仅在真正放出炮弹时扣魔；魔力不足则不发射。
            if (!Owner.CheckMana(AethersWhisperBalance.LeftManaCost, true))
            {
                SoundEngine.PlaySound(SoundID.MenuTick with { Volume = 0.3f, Pitch = -0.5f }, Owner.Center);
                return;
            }

            float charge = AethersWhisperBalance.ChargeProgress(chargeTicks);
            bool full = IsFull;
            Vector2 aim = AimDirection;
            float speed = AethersWhisperBalance.ChargedShotSpeed(charge);
            int weaponDamage = Owner.GetWeaponDamage(Owner.HeldItem);
            int damage = Math.Max(1, (int)(weaponDamage * AethersWhisperBalance.ChargeDamageMultiplier(charge)));

            Vector2 spawn = GetSafeMuzzle(aim);
            int shot = Projectile.NewProjectile(
                Projectile.GetSource_FromThis(),
                spawn,
                aim * speed,
                ModContent.ProjectileType<AethersWhisperChargedShot>(),
                damage,
                AethersWhisperBalance.KnockBack,
                Projectile.owner,
                charge,
                full ? 1f : 0f);
            if (Main.projectile.IndexInRange(shot))
            {
                Main.projectile[shot].CritChance = Owner.GetWeaponCrit(Owner.HeldItem);
                Main.projectile[shot].netUpdate = true;
            }

            // 后坐 + 枪口动画。满蓄有真正的重量感（更强后坐 + 屏震）。
            recoilOffset = full ? 20f : 10f + charge * 6f;
            muzzleFlashTicks = full ? 12 : 8;
            recoilTicks = AethersWhisperBalance.FullChargeRecoilTicks;
            Owner.velocity -= aim * (full ? AethersWhisperBalance.FullChargeRecoilSpeed : 1.4f + charge * 1.8f);

            if (full)
            {
                Owner.Calamity().GeneralScreenShakePower = Math.Max(
                    Owner.Calamity().GeneralScreenShakePower, AethersWhisperBalance.FullChargeScreenShake);
                // 先真空抽离，再厚实低频炮声——中间一拍近乎静音（文档 5.5）。
                SoundEngine.PlaySound(SoundID.Item15 with { Volume = 0.5f, Pitch = -0.9f }, GunTip);
                SoundEngine.PlaySound(SoundID.DD2_BetsysWrathImpact with { Volume = 0.7f, Pitch = -0.35f }, GunTip);
            }
            else
            {
                SoundEngine.PlaySound(SoundID.Item72 with { Volume = 0.55f, Pitch = -0.2f - charge * 0.2f }, GunTip);
            }

            SpawnMuzzleBurst(aim, charge, full);
        }

        private Vector2 GetSafeMuzzle(Vector2 aim)
        {
            Vector2 muzzle = GunTip + aim * 6f;
            return Collision.SolidCollision(muzzle, 2, 2) ? Owner.MountedCenter : muzzle;
        }

        private void ApplyMovePenalty()
        {
            if (chargeTicks >= AethersWhisperBalance.TierCriticalTicks)
            {
                float m = AethersWhisperBalance.CriticalMoveSpeedMult;
                Owner.moveSpeed *= m; Owner.maxRunSpeed *= m; Owner.accRunSpeed *= m;
            }
            else if (chargeTicks >= AethersWhisperBalance.TierStableTicks)
            {
                float m = AethersWhisperBalance.StableMoveSpeedMult;
                Owner.moveSpeed *= m; Owner.maxRunSpeed *= m; Owner.accRunSpeed *= m;
            }
        }

        private void PlayChargeSounds()
        {
            // 满蓄后每 20 tick 一次极轻的晶体脉冲（文档 5.5）。
            if (!IsFull)
                return;
            if (!playedFullReadySound)
            {
                playedFullReadySound = true;
                SoundEngine.PlaySound(SoundID.Item82 with { Volume = 0.4f, Pitch = 0.35f }, GunTip);
            }
            int step = (int)(Main.GameUpdateCount / 20);
            if (step != lastPulseStep)
            {
                lastPulseStep = step;
                SoundEngine.PlaySound(SoundID.Item25 with { Volume = 0.22f, Pitch = 0.5f }, GunTip);
            }
        }

        private void UpdatePose()
        {
            Vector2 desiredAim = (Main.MouseWorld - Owner.MountedCenter).SafeNormalize(Vector2.UnitX * Owner.direction);
            if (Projectile.owner == Main.myPlayer)
            {
                Projectile.velocity = Vector2.Lerp(AimDirection, desiredAim, 0.4f).SafeNormalize(desiredAim);
                Projectile.netUpdate = true;
            }

            Vector2 aim = AimDirection;
            int dir = aim.X >= 0f ? 1 : -1;
            Projectile.spriteDirection = Projectile.direction = dir;
            Projectile.rotation = aim.ToRotation();
            Projectile.Center = Owner.MountedCenter + aim * (HoldoutDistance - recoilOffset * 0.5f) + new Vector2(0f, -10f * Owner.gravDir);

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

        private void SpawnMuzzleBurst(Vector2 aim, float charge, bool full)
        {
            if (Main.dedServ)
                return;

            // 冷青定向环 + 珠白核心（收束一拍后被推出）。
            GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(
                GunTip, -aim * 0.4f, AethersWhisperVisuals.ShimmerCyan,
                new Vector2(0.35f, 1.5f), aim.ToRotation(), 0.05f, (0.7f + charge * 0.7f), full ? 22 : 16));
            GeneralParticleHandler.SpawnParticle(new GenericBloom(
                GunTip, Vector2.Zero, AethersWhisperVisuals.PearlWhite with { A = 0 },
                (0.4f + charge * 0.6f), full ? 14 : 10, false, true), false, GeneralDrawLayer.AfterEverything);

            int sparks = full ? 10 : 5;
            for (int i = 0; i < sparks; i++)
            {
                Vector2 vel = aim.RotatedByRandom(0.25f) * Main.rand.NextFloat(2f, full ? 7f : 4.5f);
                Dust d = Dust.NewDustPerfect(GunTip, DustID.PurpleTorch, vel, 60, AethersWhisperVisuals.AetherPurple, Main.rand.NextFloat(0.9f, 1.5f));
                d.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D gun = TextureAssets.Projectile[Type].Value;
            Vector2 drawPos = Projectile.Center - Main.screenPosition;
            Vector2 origin = gun.Size() * 0.5f;
            SpriteEffects fx = Projectile.spriteDirection == -1 ? SpriteEffects.FlipVertically : SpriteEffects.None;

            // 枪体：深紫剪影垫底 + 本体。
            Main.EntitySpriteDraw(gun, drawPos, null, AethersWhisperVisuals.AetherPurple with { A = 0 } * 0.25f,
                Projectile.rotation, origin, Projectile.scale * 1.04f, fx, 0);
            Main.EntitySpriteDraw(gun, drawPos, null, Projectile.GetAlpha(lightColor),
                Projectile.rotation, origin, Projectile.scale, fx, 0);

            DrawChargeVisual();
            Owner.heldProj = Projectile.whoAmI;
            return false;
        }

        private void DrawChargeVisual()
        {
            int ticks = (int)Projectile.ai[0];
            if (ticks <= 0 && muzzleFlashTicks <= 0)
                return;

            float charge = AethersWhisperBalance.ChargeProgress(ticks);
            bool full = ticks >= AethersWhisperBalance.FullChargeTicks;
            Vector2 aim = AimDirection;
            Vector2 tip = GunTip;
            SpriteBatch sb = Main.spriteBatch;

            AethersWhisperVisuals.BeginAdditive(sb);

            // 枪口深紫环芯（始终亮起）。
            AethersWhisperVisuals.DrawShimmerRing(sb, tip, 20f, Main.GlobalTimeWrappedHourly * 1.5f, 0.5f + charge * 0.4f);

            // 冷青薄膜由准星方向被吸回枪口——蓄得越满、环越靠近枪口越小。
            if (ticks > 0 && !full)
            {
                int rings = 2 + (int)(charge * 2f);
                for (int i = 0; i < rings; i++)
                {
                    float phase = (i + 1f) / (rings + 1f);
                    float dist = MathHelper.Lerp(120f, 16f, charge) * phase;
                    float radius = MathHelper.Lerp(34f, 12f, charge) * (1.1f - phase * 0.3f);
                    Vector2 pos = tip + aim * dist;
                    AethersWhisperVisuals.DrawShimmerRing(sb, pos, radius, -Main.GlobalTimeWrappedHourly * (1f + i), 0.35f + charge * 0.35f);
                }
            }

            // 满蓄：所有可见特效收成一个珠白点（文档 3.3）。
            if (full)
            {
                Texture2D bloom = AethersWhisperVisuals.BloomCircle.Value;
                float pulse = 0.10f + 0.02f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 8f);
                sb.Draw(bloom, tip - Main.screenPosition, null, AethersWhisperVisuals.PearlWhite with { A = 0 },
                    0f, bloom.Size() * 0.5f, pulse, SpriteEffects.None, 0f);
            }

            // 发射瞬间的 1 tick 小型 bloom。
            if (muzzleFlashTicks > 0)
            {
                Texture2D bloom = AethersWhisperVisuals.BloomCircle.Value;
                float p = muzzleFlashTicks / 12f;
                sb.Draw(bloom, tip - Main.screenPosition, null, AethersWhisperVisuals.ShimmerCyan with { A = 0 } * p,
                    aim.ToRotation(), bloom.Size() * 0.5f, new Vector2(0.35f, 0.18f) * (1f + p), SpriteEffects.None, 0f);
            }

            AethersWhisperVisuals.EndAdditive(sb);
        }
    }
}
