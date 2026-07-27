using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Upgrade.Nadir.RightClick
{
    /// <summary>
    /// 右键三连投掷的隐形节奏控制器。
    /// 摁住右键期间持续存在：每一轮以 5 帧间隔投出 3 发虚空投矛，轮与轮之间间隔 35 帧。
    /// 松开右键、切换武器或改用左键时立即结束。控制器自身不造成伤害。
    /// </summary>
    public class UmbralNadirThrowController : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.Nadir";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        /// <summary>整轮循环计时（0..RoundPeriod-1）。</summary>
        public ref float CycleTimer => ref Projectile.ai[0];

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 20;
            Projectile.friendly = false;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 2;
            Projectile.DamageType = DamageClass.Melee;
        }

        public override bool? CanDamage() => false;

        public override void AI()
        {
            Player owner = Main.player[Projectile.owner];
            bool holdingWeapon = owner.active && !owner.dead && !owner.CCed && !owner.noItems &&
                                  owner.HeldItem.type == ModContent.ItemType<UmbralNadir>();

            // 当前处于一轮三连的投掷段内（t 在 0..(3-1)*5 之间）时，即使松开右键也要打完这一轮。
            int t = (int)(CycleTimer % UmbralNadirBalance.RoundPeriod);
            bool midRound = t <= (UmbralNadirBalance.ThrowsPerRound - 1) * UmbralNadirBalance.ThrowInterval;

            // 只有本地玩家掌控存活与投掷；松开右键（且不在投掷段中）/ 改按左键 / 换武器即结束。
            if (Projectile.owner == Main.myPlayer)
            {
                bool keepAlive = holdingWeapon && !Main.mapFullscreen && !Main.blockMouse &&
                                 ((Main.mouseRight && !Main.mouseLeft) || midRound);
                if (keepAlive)
                    Projectile.timeLeft = 2;
            }
            else if (holdingWeapon)
            {
                Projectile.timeLeft = 2;
            }

            if (!holdingWeapon)
            {
                Projectile.Kill();
                return;
            }

            // 维持投掷持握姿态
            Vector2 aim = (Main.MouseWorld - owner.MountedCenter).SafeNormalize(Vector2.UnitX * owner.direction);
            owner.heldProj = Projectile.whoAmI;
            owner.itemTime = 2;
            owner.itemAnimation = 2;
            owner.ChangeDir(aim.X >= 0f ? 1 : -1);
            owner.itemRotation = aim.ToRotation();
            if (owner.direction != 1)
                owner.itemRotation -= MathHelper.Pi;
            owner.itemRotation = MathHelper.WrapAngle(owner.itemRotation);
            Projectile.Center = owner.MountedCenter;

            // 节奏调度：轮内 3 发（第 0/5/10 帧），随后 35 帧空档 → 下一轮
            for (int shot = 0; shot < UmbralNadirBalance.ThrowsPerRound; shot++)
            {
                if (t == shot * UmbralNadirBalance.ThrowInterval)
                {
                    ThrowJavelin(owner, aim, shot);
                    break;
                }
            }

            CycleTimer++;
        }

        private void ThrowJavelin(Player owner, Vector2 aim, int shotIndex)
        {
            SoundEngine.PlaySound(SoundID.DD2_JavelinThrowersAttack with { Volume = 0.72f, Pitch = Main.rand.NextFloat(-0.05f, 0.22f) }, owner.Center);

            if (Projectile.owner != Main.myPlayer)
                return;

            // 三发各不重样，但收紧到"有活力而非散射飞镖"：速度提升到上限的 120%，角度 ±4°，横向偏移 ±14。
            // 投矛 extraUpdates=3（每帧移动 4 次），速度是每次位移量。
            float spread = MathHelper.ToRadians(Main.rand.NextFloat(-UmbralNadirBalance.JavelinSpreadDegrees, UmbralNadirBalance.JavelinSpreadDegrees));
            float speed = Main.rand.NextFloat(UmbralNadirBalance.JavelinSpeedMin, UmbralNadirBalance.JavelinSpeedMax);
            Vector2 velocity = aim.RotatedBy(spread) * speed;

            Vector2 perp = aim.RotatedBy(MathHelper.PiOver2);
            float lateral = Main.rand.NextFloat(-UmbralNadirBalance.JavelinLateralOffset, UmbralNadirBalance.JavelinLateralOffset);
            float forward = Main.rand.NextFloat(UmbralNadirBalance.JavelinForwardMin, UmbralNadirBalance.JavelinForwardMax);
            Vector2 spawnPos = owner.MountedCenter + aim * forward + perp * lateral;

            // ai[0] = 投掷序号（0/1 引信、2 终结）
            Projectile.NewProjectile(Projectile.GetSource_FromThis(), spawnPos, velocity,
                ModContent.ProjectileType<UmbralNadirJavelin>(),
                Math.Max(1, (int)(Projectile.damage * UmbralNadirBalance.JavelinDamageMult)),
                Projectile.knockBack, Projectile.owner, shotIndex);
        }
    }
}
