using CalamityLegendsComeBack.Weapons.A_Upgrade.AethersWhisper.Shared;
using CalamityMod;
using CalamityMod.Projectiles.BaseProjectiles;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Upgrade.AethersWhisper.Holdout
{
    /// <summary>
    /// 以太之低语的唯一常驻持械弹幕（嘉登军械库持械范式，继承 <see cref="BaseIdleHoldoutProjectile"/>）。
    /// 只要手持该武器就存在，读鼠标自行分流左右键；不占用 itemAnimation，所以切武器 / 滚轮不卡手。
    /// 左键蓄力坍缩炮见 .LeftCharge，右键四连折射扫射见 .RightSweep，武器发光与星芒见 .CoreVisuals。
    /// </summary>
    internal sealed partial class AethersWhisperHoldout : BaseIdleHoldoutProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.AethersWhisper";
        public override string Texture => "CalamityMod/Items/Weapons/Magic/AethersWhisper";

        public override int AssociatedItemID => ModContent.ItemType<AethersWhisper>();
        public override int IntendedProjectileType => ModContent.ProjectileType<AethersWhisperHoldout>();

        private const float BarrelLength = 56f;
        private const float IdleOffset = 26f;

        // 姿态
        private float offsetLength = IdleOffset;
        private float recoilOffset;
        private int recoilTimer;

        // 左键蓄力
        private int chargeTicks;
        private int muzzleFlashTimer;
        private int lastPulseStep = -1;
        private bool playedFullReady;

        // 右键扫射
        private bool rightActive;
        private int roundTick;
        private int beamsFiredThisRound;
        private int rightFlashTimer;

        // 星芒相位（每次开火推进，让核心像会转动的能量星）
        private float starPhaseKick;

        private Vector2 AimDirection => Projectile.velocity.SafeNormalize(Vector2.UnitX * Owner.direction);
        private Vector2 GunTip => Owner.MountedCenter + AimDirection * (BarrelLength - recoilOffset) + new Vector2(0f, -6f * Owner.gravDir);

        private bool IsCharging => chargeTicks > 0;
        private bool IsFullCharge => chargeTicks >= AethersWhisperBalance.FullChargeTicks;
        private float ChargeFraction => MathHelper.Clamp(chargeTicks / (float)AethersWhisperBalance.FullChargeTicks, 0f, 1f);

        public override void SetDefaults()
        {
            Projectile.width = 134;
            Projectile.height = 44;
            Projectile.friendly = false;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.DamageType = DamageClass.Magic;
        }

        public override bool? CanDamage() => false;

        public override void SafeAI()
        {
            Projectile.timeLeft = 2;

            if (recoilTimer > 0) recoilTimer--;
            if (muzzleFlashTimer > 0) muzzleFlashTimer--;
            if (rightFlashTimer > 0) rightFlashTimer--;
            recoilOffset = MathHelper.Lerp(recoilOffset, 0f, 0.2f);

            Owner.Calamity().mouseWorldListener = true;
            if (Main.myPlayer == Projectile.owner)
                Owner.Calamity().rightClickListener = true;

            rightActive = false;
            if (Main.myPlayer == Projectile.owner)
                HandleInput();

            // 视觉同步：拥有者把蓄力进度写入 ai[0]，其它客户端只读它画环/核心。
            if (Main.myPlayer == Projectile.owner)
                Projectile.ai[0] = chargeTicks;
            else
                chargeTicks = (int)Projectile.ai[0];

            UpdatePose();
            Lighting.AddLight(GunTip, AethersWhisperVisuals.Lerp(ChargeFraction).ToVector3() * (0.35f + ChargeFraction * 0.5f));
        }

        private void HandleInput()
        {
            bool canInput = CanUseWorldInput();
            bool rightHeld = (Main.mouseRight || Owner.Calamity().mouseRight) && canInput;
            bool leftHeld = Main.mouseLeft && canInput;

            if (rightHeld)
            {
                // 右键优先：中断左键蓄力（不发射），执行四连扫射。
                if (IsCharging) CancelLeftCharge();
                rightActive = true;
                RunRightSweep();
                return;
            }

            ResetRightRound();

            if (leftHeld)
                AdvanceLeftCharge();
            else if (IsCharging)
                ReleaseLeftCharge();
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
                Vector2 newVel = Projectile.velocity == Vector2.Zero ? desiredAim : Vector2.Lerp(Projectile.velocity, desiredAim, 0.4f).SafeNormalize(desiredAim);
                if (Vector2.DistanceSquared(newVel, Projectile.velocity) > 0.0001f)
                    Projectile.netUpdate = true;
                Projectile.velocity = newVel;
            }

            // 蓄力时枪体略微后仰、贴近；后坐时被推远。
            float targetOffset = IdleOffset - ChargeFraction * 6f - recoilOffset;
            offsetLength = MathHelper.Lerp(offsetLength, targetOffset, 0.3f);

            Vector2 aim = AimDirection;
            int dir = aim.X >= 0f ? 1 : -1;
            Projectile.spriteDirection = Projectile.direction = dir;
            Projectile.rotation = aim.ToRotation();
            Projectile.Center = armPos + aim * offsetLength + new Vector2(0f, -8f * Owner.gravDir);

            Owner.ChangeDir(dir);
            Owner.heldProj = Projectile.whoAmI;
            // 关键：不设置 itemTime / itemAnimation —— 那会锁死滚轮切换（BF/AMR 都不设）。
            float armRot = (Projectile.rotation - MathHelper.PiOver2) * Owner.gravDir;
            if (Owner.gravDir == -1f) armRot += MathHelper.Pi;
            Owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, armRot);
            Owner.SetCompositeArmBack(true, Player.CompositeArmStretchAmount.ThreeQuarters, armRot + MathHelper.ToRadians(9f) * dir);
        }

        private Vector2 GetSafeMuzzle(Vector2 aim)
        {
            Vector2 muzzle = GunTip + aim * 6f;
            return Collision.SolidCollision(muzzle, 2, 2) ? Owner.MountedCenter : muzzle;
        }
    }
}
