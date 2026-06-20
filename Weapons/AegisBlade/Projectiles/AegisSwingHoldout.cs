using System;
using CalamityMod;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.AegisBlade.Projectiles
{
    // 左键挥舞holdout。在swing状态内部检测右键，触发剑盾联动投掷。
    public class AegisSwingHoldout : ModProjectile
    {
        public override string Texture => "CalamityLegendsComeBack/Weapons/AegisBlade/AegisBlade";

        private Player Owner => Main.player[Projectile.owner];

        // ── 状态 ──────────────────────────────────────────────────────────
        private const int StateSwing = 0;
        private const int StateComboRaise = 1;
        private const int StateComboHold = 2;

        private int state = StateSwing;
        private int stateTimer = 0;
        private int swingCount = 0;           // 偶数/奇数决定挥舞方向
        private int swingDir = 1;             // 当前挥舞方向
        private bool comboRequested = false;
        private bool fireballSpawned = false;
        private float currentAngle = 0f;
        private float startAngle = 0f;
        private float endAngle = 0f;
        private Vector2 lockedMouseDir = Vector2.UnitX;
        private float scale = 1f;

        private const int SwingDuration = 26;     // 单次挥舞帧数
        private const int ComboRaiseDuration = 30; // 举剑过渡帧数
        private static readonly float SwingArc = MathHelper.ToRadians(220f);
        private const float FireballSpawnProgress = 0.65f;

        private static readonly Color TrailColor = new(255, 200, 80);

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 60;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = SwingDuration;
            Projectile.timeLeft = 4;
            Projectile.noEnchantmentVisuals = true;
            Projectile.ContinuouslyUpdateDamageStats = true;
        }

        private bool IsLeftHeld()
            => Owner.channel && (Main.myPlayer != Projectile.owner || Main.mouseLeft)
               && !Main.mapFullscreen && !Main.blockMouse && !Owner.mouseInterface;

        private bool IsRightHeld()
            => (Main.mouseRight || Owner.controlUseTile)
               && !Main.mapFullscreen && !Main.blockMouse && !Owner.mouseInterface;

        private Vector2 GetMouseDir()
        {
            Vector2 mouse = Owner.Calamity().mouseWorld;
            if (mouse == Vector2.Zero) mouse = Main.MouseWorld;
            return Owner.MountedCenter.DirectionTo(mouse).SafeNormalize(Vector2.UnitX);
        }

        public override void AI()
        {
            if (!Owner.active || Owner.dead || Owner.HeldItem.type != ModContent.ItemType<AegisBlade>())
            {
                Projectile.Kill();
                return;
            }

            scale = Owner.GetMeleeScale();
            Owner.heldProj = Projectile.whoAmI;
            Owner.GetModPlayer<AegisBladePlayer>().IsSwinging = true;

            // 保持 item 动画激活
            Owner.itemTime = Math.Max(Owner.itemTime, 2);
            Owner.itemAnimation = Math.Max(Owner.itemAnimation, 2);
            Projectile.timeLeft = 4;
            Projectile.Center = Owner.MountedCenter;

            switch (state)
            {
                case StateSwing:      DoSwing(); break;
                case StateComboRaise: DoComboRaise(); break;
                case StateComboHold:  DoComboHold(); break;
            }

            // 手臂跟随（世界空间角度，左右对称 −130° 偏移）
            float armAngle = currentAngle - MathHelper.ToRadians(130f);
            Owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, armAngle);
            Owner.SetCompositeArmBack(true, Player.CompositeArmStretchAmount.Full, armAngle);
            Owner.itemLocation = Owner.Center;
            Owner.itemRotation = currentAngle;
        }

        private void StartSwing()
        {
            stateTimer = 0;
            fireballSpawned = false;
            lockedMouseDir = GetMouseDir();
            swingDir = -Math.Sign(Owner.Center.X - Owner.Calamity().mouseWorld.X);
            if (swingDir == 0) swingDir = Owner.direction;
            Owner.direction = swingDir;

            // 起始角 / 终止角（相对于指向鼠标方向的旋转偏移）
            float baseAngle = lockedMouseDir.ToRotation();
            startAngle = baseAngle + MathHelper.ToRadians(-110f * swingDir * (swingCount % 2 == 0 ? 1 : -1));
            endAngle   = baseAngle + MathHelper.ToRadians(-110f * swingDir * (swingCount % 2 == 0 ? -1 : 1));
            currentAngle = startAngle;
        }

        private void DoSwing()
        {
            if (stateTimer == 0)
                StartSwing();

            stateTimer++;
            float progress = (float)stateTimer / SwingDuration;
            float easedProgress = CalamityUtils.EaseInOutExp(progress, 5f, 2f);
            currentAngle = MathHelper.Lerp(startAngle, endAngle, easedProgress);

            // 挥舞到65%时生成火球
            if (!fireballSpawned && progress >= FireballSpawnProgress && Main.myPlayer == Projectile.owner)
            {
                SpawnFireballs();
                fireballSpawned = true;
                SoundEngine.PlaySound(SoundID.Item71 with { Volume = 0.8f, Pitch = Main.rand.NextFloat(-0.1f, 0.2f) }, Owner.Center);
            }

            // 检测右键：触发剑盾联动
            if (IsLeftHeld() && IsRightHeld())
                comboRequested = true;

            // 摆动粒子
            EmitSwingTrail(progress);

            if (stateTimer >= SwingDuration)
            {
                if (comboRequested)
                {
                    EnterComboRaise();
                    return;
                }

                if (IsLeftHeld())
                {
                    // 继续下一次挥舞
                    swingCount++;
                    Projectile.ResetLocalNPCHitImmunity();
                    stateTimer = 0;
                    fireballSpawned = false;
                }
                else
                {
                    Projectile.Kill();
                }
            }
        }

        private void EnterComboRaise()
        {
            state = StateComboRaise;
            stateTimer = 0;
            comboRequested = false;
            SoundEngine.PlaySound(SoundID.Item74 with { Volume = 0.75f, Pitch = -0.2f }, Owner.Center);
        }

        private void DoComboRaise()
        {
            stateTimer++;

            // 必须同时按住左右键，否则取消
            if (!IsLeftHeld() || !IsRightHeld())
            {
                // 取消，恢复挥舞
                state = StateSwing;
                stateTimer = 0;
                fireballSpawned = false;
                return;
            }

            float progress = (float)stateTimer / ComboRaiseDuration;
            // 目标：剑指向正上方（-π/2）
            float targetAngle = -MathHelper.PiOver2;
            currentAngle = MathHelper.Lerp(currentAngle, targetAngle, CalamityUtils.SineInOutEasing(progress, 1));

            if (!Main.dedServ && stateTimer % 3 == 0)
            {
                Vector2 tip = Owner.Center - Vector2.UnitY * 120f * scale;
                Vector2 vel = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(2f, 5f);
                GeneralParticleHandler.SpawnParticle(new CustomSpark(tip + Main.rand.NextVector2Circular(20f, 20f),
                    vel, "CalamityMod/Particles/Sparkle", false, Main.rand.Next(10, 18),
                    Main.rand.NextFloat(0.5f, 0.9f), TrailColor, new Vector2(0.3f, 1.1f), true, true, shrinkSpeed: 0.2f));
            }

            if (stateTimer >= ComboRaiseDuration)
            {
                state = StateComboHold;
                stateTimer = 0;
            }
        }

        private void DoComboHold()
        {
            // 剑悬在玩家正上方，等待任意按键松开
            currentAngle = MathHelper.Lerp(currentAngle, -MathHelper.PiOver2, 0.15f);

            // 蓄力粒子
            if (!Main.dedServ && Main.rand.NextBool(3))
            {
                Vector2 tip = Owner.Center - Vector2.UnitY * 130f * scale;
                Vector2 orbit = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(40f, 100f);
                Vector2 vel = (tip - (tip + orbit)).SafeNormalize(Vector2.UnitY) * Main.rand.NextFloat(3f, 7f);
                GeneralParticleHandler.SpawnParticle(new CustomSpark(tip + orbit + Main.rand.NextVector2Circular(10f, 10f),
                    vel, "CalamityMod/Particles/Sparkle", false, Main.rand.Next(12, 22),
                    Main.rand.NextFloat(0.6f, 1.2f), TrailColor, new Vector2(0.3f, 1.3f), true, true, shrinkSpeed: 0.16f));
            }

            // 松开任意键 → 投掷
            if (!IsLeftHeld() || !IsRightHeld())
            {
                if (Main.myPlayer == Projectile.owner)
                    ThrowBlade();
                Projectile.Kill();
            }
        }

        private void SpawnFireballs()
        {
            int fireballDamage = (int)(Projectile.damage * 0.6f);
            float speed = 14f;

            // 垂直于刀锋方向向两侧喷射，带轻微向上偏移
            Vector2 bladeDir = currentAngle.ToRotationVector2();
            Vector2 perp = bladeDir.RotatedBy(MathHelper.PiOver2);
            Vector2 upBias = new Vector2(0f, -1.5f);

            Projectile.NewProjectile(Projectile.GetSource_FromThis(), Owner.Center,
                perp * speed + upBias,
                ModContent.ProjectileType<AegisFireball>(), fireballDamage, 2f, Projectile.owner);

            Projectile.NewProjectile(Projectile.GetSource_FromThis(), Owner.Center,
                -perp * speed + upBias,
                ModContent.ProjectileType<AegisFireball>(), fireballDamage, 2f, Projectile.owner);
        }

        private void ThrowBlade()
        {
            Vector2 mouse = Owner.Calamity().mouseWorld;
            if (mouse == Vector2.Zero) mouse = Main.MouseWorld;
            Vector2 dir = Owner.Center.DirectionTo(mouse).SafeNormalize(Vector2.UnitX * Owner.direction);

            Vector2 spawnPos = Owner.Center - Vector2.UnitY * 60f;
            int thrown = Projectile.NewProjectile(Projectile.GetSource_FromThis(), spawnPos,
                dir * 25f,
                ModContent.ProjectileType<AegisBladeThrown>(),
                (int)(Projectile.damage * 1.5f),
                Projectile.knockBack * 1.2f,
                Projectile.owner);

            SoundEngine.PlaySound(SoundID.Item84 with { Volume = 1f, Pitch = -0.3f }, Owner.Center);
        }

        // ── 伤害 ──────────────────────────────────────────────────────────

        public override bool? CanDamage()
        {
            float progress = (float)stateTimer / SwingDuration;
            return state == StateSwing && progress >= 0.25f && progress <= 0.85f ? null : false;
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            float _ = 0f;
            Vector2 bladeDir = currentAngle.ToRotationVector2();
            Vector2 bladeTip = Owner.Center + bladeDir * 140f * scale;
            return Collision.CheckAABBvLineCollision(
                targetHitbox.TopLeft(), targetHitbox.Size(),
                Owner.Center, bladeTip, 28f * scale, ref _) ? null : false;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            Owner.GetModPlayer<AegisBladePlayer>().GainEnergyOnHit();
        }

        // ── 绘制 ──────────────────────────────────────────────────────────

        private void EmitSwingTrail(float progress)
        {
            if (Main.dedServ || !(progress > 0.2f && progress < 0.9f))
                return;

            Vector2 bladeDir = currentAngle.ToRotationVector2();
            for (int i = 0; i < 3; i++)
            {
                Vector2 pos = Owner.Center + bladeDir * Main.rand.NextFloat(40f, 130f) * scale;
                Vector2 vel = bladeDir.RotatedBy(MathHelper.PiOver2 * Math.Sign(endAngle - startAngle)) * Main.rand.NextFloat(2f, 5f);
                GeneralParticleHandler.SpawnParticle(new GlowSparkParticle(pos, vel, false,
                    Main.rand.Next(6, 12), Main.rand.NextFloat(0.04f, 0.07f) * scale,
                    Main.rand.NextBool(3) ? Color.White : TrailColor,
                    new Vector2(1.4f, 0.2f), true, false, 0.75f));
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.dedServ) return false;

            Texture2D tex = ModContent.Request<Texture2D>(Texture).Value;
            bool facingLeft = Owner.direction == -1;
            // origin 始终在纹理左下角（即剑柄）。FlipHorizontally 下 MonoGame 的 origin 计算
            // 将该点映射到渲染图像的右下角并放置在 drawPos，因此剑柄仍在玩家中心。
            // 翻转后刀刃自然角从 -45° 变为 225°，需把旋转补偿量从 +45° 改为 -225°
            // 以保证视觉刀刃方向 = currentAngle。
            Vector2 origin = new Vector2(0f, tex.Height);
            SpriteEffects effects = facingLeft ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
            float drawRotation = facingLeft
                ? (currentAngle - MathHelper.Pi - MathHelper.PiOver4)
                : (currentAngle + MathHelper.PiOver4);
            Vector2 drawPos = Owner.Center - Main.screenPosition + new Vector2(0f, Owner.gfxOffY);

            // 光晕
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Vector2 bladeTip = Owner.Center + currentAngle.ToRotationVector2() * 120f * scale - Main.screenPosition;
            Main.spriteBatch.SetBlendState(BlendState.Additive);
            Main.EntitySpriteDraw(bloom, bladeTip, null, TrailColor with { A = 0 } * 0.35f,
                0f, bloom.Size() * 0.5f, scale * 0.6f, SpriteEffects.None, 0);
            Main.spriteBatch.SetBlendState(BlendState.AlphaBlend);

            Main.EntitySpriteDraw(tex, drawPos, null, lightColor, drawRotation, origin, scale, effects, 0);
            return false;
        }
    }
}
