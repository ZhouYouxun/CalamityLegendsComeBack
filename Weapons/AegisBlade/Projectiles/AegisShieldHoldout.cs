using System;
using CalamityLegendsComeBack.Weapons.AegisBlade.Visuals;
using CalamityMod;
using CalamityMod.Graphics.Primitives;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.Graphics.Effects;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.AegisBlade.Projectiles
{
    // The shield sprite itself is deliberately not drawn here. AegisBladePlayer registers the supplied
    // 40-frame sheet as a native raisable shield, which is the same rendering path used by Stygian Shield.
    public class AegisShieldHoldout : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private const int MinimumDashCharge = BalanceAegisBlade.ShieldRaiseFrames;
        private const int ChargeStartTime = BalanceAegisBlade.ShieldRaiseFrames + BalanceAegisBlade.ChargeHoldDelay;
        private const int FullChargeTime = ChargeStartTime + BalanceAegisBlade.ChargeDuration;
        private const int DashDuration = BalanceAegisBlade.ShieldDashDuration;
        private const int BashDamageFrames = DashDuration;

        private Player Owner => Main.player[Projectile.owner];
        private AegisBladePlayer BladePlayer => Owner.GetModPlayer<AegisBladePlayer>();
        private ref float Charge => ref Projectile.ai[0];
        private ref float DashTime => ref Projectile.ai[1];
        private bool IsDashing => DashDestination != Vector2.Zero;
        private Vector2 DashDirection => Projectile.velocity.SafeNormalize(Vector2.UnitX * Owner.direction);
        private bool ChargeUnlocked => BalanceAegisBlade.ChargeUnlocked();
        private float MaximumGuardCharge => ChargeUnlocked ? FullChargeTime : ChargeStartTime;
        private float ChargeRatio => ChargeUnlocked
            ? Utils.GetLerpValue(ChargeStartTime, FullChargeTime, Charge, true)
            : 0f;
        private float DashPower => Utils.GetLerpValue(MinimumDashCharge, FullChargeTime, Charge, true);

        // DashDestination stores the bash direction (unit vector) once bashing starts.
        // Non-zero = in bash mode. Not synced over network (owner-driven).
        private Vector2 DashDestination;
        private float dashDistance;
        private float dashSpeed;
        private bool perfectParryFired;
        private bool fullChargeFired;
        private bool shieldRaisedFired;

        /// <summary>完美格挡瞬间的一次性白闪，仅用于绘制。</summary>
        private float parryFlash;

        private const string ShieldTexturePath = "CalamityLegendsComeBack/Weapons/AegisBlade/庇护盾牌";

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 14;
            ProjectileID.Sets.TrailingMode[Type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 2;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
            Projectile.noEnchantmentVisuals = true;
            Projectile.timeLeft = 4;
        }

        public override bool ShouldUpdatePosition() => false;

        public override void AI()
        {
            if (!Owner.active || Owner.dead || Owner.HeldItem.type != ModContent.ItemType<AegisBlade>())
            {
                Projectile.Kill();
                return;
            }

            if (parryFlash > 0f)
                parryFlash = MathHelper.Max(0f, parryFlash - 0.055f);

            Owner.heldProj = Projectile.whoAmI;
            if (IsDashing)
            {
                DoDash();
                return;
            }

            if (BladePlayer.IsChargingBarrier)
            {
                Projectile.Kill();
                return;
            }

            Projectile.Center = Owner.MountedCenter;
            Projectile.timeLeft = 4;

            bool rightHeld = IsRightHeld();

            if (!rightHeld)
            {
                TryStartDash();
                return;
            }

            Charge = Math.Min(Charge + 1f, MaximumGuardCharge);
            UpdateGuardState();
            UpdateFacing();

            if (!shieldRaisedFired && BladePlayer.ShieldRaised)
            {
                shieldRaisedFired = true;
                OnShieldRaised();
            }

            if (BladePlayer.WasHurtDuringRaise && !perfectParryFired)
            {
                BladePlayer.WasHurtDuringRaise = false;
                perfectParryFired = true;
                OnPerfectParry();
            }

            if (ChargeUnlocked && !fullChargeFired && Charge >= FullChargeTime)
            {
                fullChargeFired = true;
                OnFullCharge();
            }

            AegisVisuals.Light(ShieldWorldPosition, 0.45f + ChargeRatio * 0.9f);
            EmitGuardFlames();
        }

        private Vector2 ShieldWorldPosition =>
            Owner.Center + new Vector2(Owner.direction * Owner.width * 0.42f, -4f);

        private bool IsRightHeld()
        {
            if (Main.myPlayer != Projectile.owner)
                return true;

            return Owner.Calamity().mouseRight && !Main.mapFullscreen && !Main.blockMouse && !Owner.mouseInterface;
        }

        private void UpdateGuardState()
        {
            BladePlayer.ShieldRaising = Charge <= BalanceAegisBlade.ShieldRaiseFrames;
            BladePlayer.ShieldRaised = Charge > BalanceAegisBlade.ShieldRaiseFrames;
            BladePlayer.ShieldCharging = ChargeUnlocked && Charge >= ChargeStartTime && Charge < FullChargeTime;
            BladePlayer.ShieldFullyCharged = ChargeUnlocked && Charge >= FullChargeTime;
        }

        private void UpdateFacing()
        {
            Vector2 aimDirection = Owner.MountedCenter.DirectionTo(AegisBlade.GetMouseWorld(Owner))
                .SafeNormalize(Vector2.UnitX * Owner.direction);
            Owner.ChangeDir(aimDirection.X >= 0f ? 1 : -1);
            Owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full,
                aimDirection.ToRotation() - MathHelper.PiOver2);
        }

        // ── 盾牌猛击：给予33%蓄力速度的冲刺力，然后交由物理自然减速 ──────
        private void TryStartDash()
        {
            if (Charge < MinimumDashCharge || Owner.noItems || Owner.CCed)
            {
                Projectile.Kill();
                return;
            }

            Vector2 dashDirection = Owner.MountedCenter.DirectionTo(AegisBlade.GetMouseWorld(Owner))
                .SafeNormalize(Vector2.UnitX * Owner.direction);
            dashDistance = MathHelper.Lerp(BalanceAegisBlade.ShieldDashMinimumDistance,
                BalanceAegisBlade.ShieldDashMaximumDistance, DashPower);

            Vector2 destination = Owner.MountedCenter + dashDirection * dashDistance;
            bool destinationOutOfBounds = destination.X < 660f || destination.Y < 660f ||
                destination.X > Main.maxTilesX * 16f - 680f || destination.Y > Main.maxTilesY * 16f - 680f;
            if (destinationOutOfBounds)
            {
                Projectile.Kill();
                return;
            }

            // Charge selects distance; the bash then maintains a Stygian-style fixed speed.
            dashSpeed = dashDistance / DashDuration;
            Projectile.Center = Owner.MountedCenter;
            Projectile.velocity = dashDirection * dashSpeed;
            Owner.velocity = Vector2.Zero;

            // DashDestination 存储方向单位向量（非零即代表处于冲刺中）
            DashDestination = destination;

            float damageMultiplier = MathHelper.Lerp(1f, BalanceAegisBlade.ShieldDashMaxDamageMultiplier, DashPower);
            if (perfectParryFired)
                damageMultiplier *= BalanceAegisBlade.PerfectParryDashDamageMultiplier;
            Projectile.damage = Math.Max(1, (int)(Projectile.damage * damageMultiplier));
            Projectile.ExpandHitboxBy(BalanceAegisBlade.ShieldDashHitboxExpansion);
            Projectile.netUpdate = true;

            Owner.immune = true;
            Owner.immuneNoBlink = true;
            Owner.immuneTime = BashDamageFrames;
            for (int i = 0; i < Owner.hurtCooldowns.Length; i++)
                Owner.hurtCooldowns[i] = Owner.immuneTime;

            for (int i = 0; i < Projectile.oldPos.Length; i++)
            {
                Projectile.oldPos[i] = Vector2.Zero;
                Projectile.oldRot[i] = 0f;
                Projectile.oldSpriteDirection[i] = 0;
            }

            SoundEngine.PlaySound(SoundID.Item74 with { Volume = 1.05f, Pitch = -0.2f + DashPower * 0.22f }, Owner.Center);
            SpawnDashFlash(Owner.Center, dashDirection, 1f + DashPower * 0.35f);
        }

        // 猛击持续：跟随玩家，给予短暂伤害窗口后消亡。不强制移动，物理自然减速。
        private void DoDash()
        {
            DashTime++;
            Projectile.timeLeft = 4;

            Vector2 nextCenter = Projectile.Center + Projectile.velocity;
            Vector2 nextTopLeft = nextCenter - Projectile.Size * 0.5f;
            if (Collision.SolidCollision(nextTopLeft, Projectile.width, Projectile.height) || DashTime > DashDuration)
            {
                Projectile.Kill();
                return;
            }

            Projectile.Center = nextCenter;
            Owner.Center = nextCenter;
            Owner.velocity = Vector2.Zero;

            Vector2 direction = DashDestination;  // 已是单位方向向量
            direction = DashDirection;
            Owner.ChangeDir(direction.X >= 0f ? 1 : -1);
            Owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full,
                direction.ToRotation() - MathHelper.PiOver2);

            Owner.velocity = Vector2.Zero;

            AegisVisuals.Light(Projectile.Center, 1.3f);

            if (!Main.dedServ)
                EmitDashFlames(direction);

            if (DashTime >= DashDuration || Vector2.DistanceSquared(Projectile.Center, DashDestination) <= 1f)
            {
                Owner.velocity = direction * (dashSpeed * 0.45f);
                Projectile.Kill();
            }
        }

        public override bool TileCollideStyle(ref int width, ref int height, ref bool fallThrough, ref Vector2 hitboxCenterFrac)
        {
            width = height = 32;
            return true;
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            return CalamityUtils.CircularHitboxCollision(Owner.Center,
                BalanceAegisBlade.ShieldDashHitRadius, targetHitbox);
        }

        public override bool? CanDamage() => IsDashing && DashTime > 0f ? null : false;

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            Projectile.damage = Math.Max(1, (int)(Projectile.damage * BalanceAegisBlade.ShieldDashPiercingDamageMultiplier));
            SoundEngine.PlaySound(SoundID.Item14 with { Volume = 0.9f, Pitch = -0.15f }, target.Center);

            if (Main.dedServ)
                return;

            Vector2 direction = DashDestination != Vector2.Zero
                ? DashDirection
                : Owner.velocity.SafeNormalize(Vector2.UnitX * Owner.direction);

            // 盾撞：正前方压出一记扁平冲击，而不是一个圆环
            AegisVisuals.DirectionalImpact(target.Center, direction, 1.35f);
            AegisVisuals.EmberJet(target.Center, direction, 9, 1.25f, 0.4f);
            AegisVisuals.HolyDetonation(target.Center, 1.1f, true, direction.ToRotation());
            AegisVisuals.WarbannerConverge(target.Center, direction, 1.9f, 4,
                1f + target.Hitbox.Width / 360f);
            AegisVisuals.Screenshake(target.Center, 2.6f, 800f);
        }

        private void OnPerfectParry()
        {
            SoundEngine.PlaySound(SoundID.DD2_WitherBeastCrystalImpact with { Volume = 1f, Pitch = 0.25f }, Owner.Center);
            SoundEngine.PlaySound(SoundID.Item100 with { Volume = 0.75f, Pitch = 0.55f }, Owner.Center);
            if (Main.dedServ)
                return;

            parryFlash = 1f;

            // 完美格挡是这把武器最重要的一次成功反馈：给它整套里最强的一次爆闪
            AegisVisuals.HolyDetonation(Owner.Center, 2.2f, false);
            AegisVisuals.CoronaRing(Owner.Center, 22, 1.6f);
            AegisVisuals.EmberJet(Owner.Center, new Vector2(Owner.direction, -0.35f), 14, 1.4f, 1.2f);

            // 金红双环：内圈白金急速外扩，外圈余烬慢一拍跟上
            GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(Owner.Center, Vector2.Zero,
                AegisVisuals.Add(AegisVisuals.Core, 1f), Vector2.One, 0f, 0.05f, 1.6f, 16));
            GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(Owner.Center, Vector2.Zero,
                AegisVisuals.Add(AegisVisuals.Ember, 0.95f), Vector2.One, 0f, 0.06f, 2.4f, 26));

            // 火花冠：12 点均分，读起来像"挡下来的力被弹开成一圈"
            for (int i = 0; i < 12; i++)
            {
                float angle = MathHelper.TwoPi * i / 12f;
                GeneralParticleHandler.SpawnParticle(new SparkleParticle(
                    Owner.Center + angle.ToRotationVector2() * 42f, angle.ToRotationVector2() * 1.6f,
                    AegisVisuals.Add(AegisVisuals.Core, 1f), AegisVisuals.Add(AegisVisuals.Flame, 1f),
                    1.15f, 16, 0.04f, 1.9f));
            }

            AegisVisuals.Screenshake(Owner.Center, 4.2f, 1000f);
        }

        private void OnShieldRaised()
        {
            SoundEngine.PlaySound(SoundID.Item68 with { Volume = 0.65f, Pitch = 0.12f }, Owner.Center);
            if (Main.dedServ)
                return;

            Vector2 shieldPosition = ShieldWorldPosition;
            AegisVisuals.HolyDetonation(shieldPosition, 0.55f, false);
            AegisVisuals.CoronaRing(shieldPosition, 9, 0.55f);
            GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(shieldPosition, Vector2.Zero,
                AegisVisuals.Add(AegisVisuals.Gold, 0.9f), new Vector2(0.75f, 1.35f),
                new Vector2(Owner.direction, 0f).ToRotation(), 0.05f, 0.9f, 16));
        }

        private void OnFullCharge()
        {
            SoundEngine.PlaySound(SoundID.Item67 with { Volume = 0.95f, Pitch = 0.12f }, Owner.Center);
            if (Main.dedServ)
                return;

            // 蓄满：符文环"咬合"，向前压出一记冲击光锥
            AegisVisuals.HolyDetonation(Owner.Center, 1.3f, false);
            AegisVisuals.CoronaRing(Owner.Center, 16, 1.1f);

            Vector2 aim = Owner.MountedCenter.DirectionTo(AegisBlade.GetMouseWorld(Owner))
                .SafeNormalize(Vector2.UnitX * Owner.direction);
            GeneralParticleHandler.SpawnParticle(new CustomPulse(ShieldWorldPosition, Vector2.Zero,
                AegisVisuals.Add(AegisVisuals.Gold, 0.9f), AegisVisuals.TexBlastCone,
                new Vector2(3.6f, 1.4f), aim.ToRotation(), 0.9f, 0f, 26));
            AegisVisuals.Screenshake(Owner.Center, 2f, 700f);
        }

        /// <summary>举盾期间的持续特效：分三档强度，越蓄力火越旺、越往前压。</summary>
        private void EmitGuardFlames()
        {
            if (Main.dedServ)
                return;

            Vector2 shieldPosition = ShieldWorldPosition;
            Vector2 outward = new Vector2(Owner.direction, 0f);

            if (BladePlayer.ShieldRaising)
            {
                // ① 举盾过渡（同时是完美格挡窗口）：火迅速在盾面点燃
                float raiseRatio = MathHelper.Clamp(Charge / BalanceAegisBlade.ShieldRaiseFrames, 0f, 1f);
                for (int i = 0; i < 2; i++)
                {
                    GeneralParticleHandler.SpawnParticle(new GlowOrbParticle(
                        shieldPosition + Main.rand.NextVector2Circular(9f, 15f),
                        outward.RotatedByRandom(0.7f) * Main.rand.NextFloat(0.6f, 2f) * (0.4f + raiseRatio),
                        false, Main.rand.Next(11, 19), Main.rand.NextFloat(0.12f, 0.24f) * (0.5f + raiseRatio),
                        AegisVisuals.RandomFlameColor(), true, false, true));
                }

                if (Main.rand.NextBool(2))
                {
                    Dust ember = Dust.NewDustPerfect(shieldPosition + Main.rand.NextVector2Circular(8f, 14f),
                        AegisVisuals.ProfanedFireDust,
                        outward.RotatedByRandom(0.6f) * Main.rand.NextFloat(0.5f, 1.8f),
                        0, Color.White, Main.rand.NextFloat(0.8f, 1.35f));
                    ember.noGravity = true;
                }
                return;
            }

            if (!ChargeUnlocked || Charge < ChargeStartTime)
            {
                // ② 举盾持续：只留下沿盾缘往下滴的余烬，克制、不抢戏
                if (Main.rand.NextBool(3))
                    AegisVisuals.EmberDrip(shieldPosition + new Vector2(0f, 8f), 8f, 10f, 0.85f);

                if (Main.rand.NextBool(6))
                {
                    GeneralParticleHandler.SpawnParticle(new MediumMistParticle(
                        shieldPosition + Main.rand.NextVector2Circular(8f, 12f),
                        outward * Main.rand.NextFloat(0.15f, 0.6f) - Vector2.UnitY * 0.3f,
                        Color.Lerp(AegisVisuals.Charred, Color.DarkSlateGray, Main.rand.NextFloat(0.4f, 0.9f)),
                        Color.Transparent, Main.rand.NextFloat(0.16f, 0.3f), Main.rand.Next(18, 30),
                        Main.rand.NextFloat(-0.05f, 0.05f)));
                }
                return;
            }

            // ③ 蓄力：火星被反向吸进盾面，蓄力越满吸得越猛
            if (Main.rand.NextBool(2))
            {
                Vector2 inward = outward.RotatedByRandom(1.0f);
                GeneralParticleHandler.SpawnParticle(new SparkParticle(
                    shieldPosition + inward * Main.rand.NextFloat(38f, 80f),
                    -inward * Main.rand.NextFloat(1.8f, 5f) * (0.5f + ChargeRatio), false,
                    Main.rand.Next(12, 22), Main.rand.NextFloat(0.4f, 0.9f),
                    AegisVisuals.Gradient(Main.rand.NextFloat(0.1f, 0.7f))));
            }

            if (Main.rand.NextBool(2))
            {
                GeneralParticleHandler.SpawnParticle(new GlowOrbParticle(
                    shieldPosition + Main.rand.NextVector2Circular(10f, 16f),
                    outward.RotatedByRandom(0.7f) * Main.rand.NextFloat(0.4f, 1.8f) * ChargeRatio,
                    false, Main.rand.Next(12, 20), MathHelper.Lerp(0.14f, 0.3f, ChargeRatio),
                    AegisVisuals.RandomFlameColor(), true, false, true));
            }

            if (Main.rand.NextBool(3))
                AegisVisuals.EmberDrip(shieldPosition + new Vector2(0f, 8f), 9f, 11f, 1f + ChargeRatio * 0.5f);
        }

        /// <summary>冲刺途中的尾流：后方火舌 + 侧向火星 + 每 3 帧一圈定向环。</summary>
        private void EmitDashFlames(Vector2 direction)
        {
            if (Main.dedServ)
                return;

            Vector2 rear = Projectile.Center - direction * Main.rand.NextFloat(16f, 42f);
            for (int i = 0; i < 3; i++)
            {
                Vector2 trailVelocity = -direction.RotatedByRandom(0.34f) * Main.rand.NextFloat(2f, 5f);
                GeneralParticleHandler.SpawnParticle(new MediumMistParticle(
                    rear + Main.rand.NextVector2Circular(10f, 10f), trailVelocity,
                    Color.Lerp(AegisVisuals.Charred, Color.DarkSlateGray, Main.rand.NextFloat(0.25f, 0.8f)),
                    Color.Transparent, Main.rand.NextFloat(0.4f, 0.68f), Main.rand.Next(18, 28),
                    Main.rand.NextFloat(-0.08f, 0.08f)));

                GeneralParticleHandler.SpawnParticle(new GlowSparkParticle(
                    Projectile.Center + direction * Main.rand.NextFloat(-10f, 28f) + Main.rand.NextVector2Circular(12f, 12f),
                    trailVelocity * Main.rand.NextFloat(0.9f, 1.5f), false, Main.rand.Next(8, 14),
                    Main.rand.NextFloat(0.08f, 0.14f),
                    AegisVisuals.Add(AegisVisuals.Gradient(Main.rand.NextFloat(0f, 0.6f)), 0.9f),
                    new Vector2(2.4f, 0.48f), true, false, 1f));
            }

            if ((int)DashTime % 2 == 0)
            {
                Dust ember = Dust.NewDustPerfect(rear, AegisVisuals.ProfanedFireDust,
                    -direction.RotatedByRandom(0.5f) * Main.rand.NextFloat(1.5f, 5f),
                    0, Color.White, Main.rand.NextFloat(1f, 1.7f));
                ember.noGravity = true;
            }

            if ((int)DashTime % 3 == 0)
            {
                GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(
                    Projectile.Center - direction * 18f, -direction * 0.8f,
                    AegisVisuals.Add(AegisVisuals.Gold, 0.9f),
                    new Vector2(0.52f, 1.4f), direction.ToRotation(), 0.1f, 0.52f, 13));
            }
        }

        /// <summary>起手/收尾的冲刺闪光。</summary>
        private void SpawnDashFlash(Vector2 position, Vector2 direction, float strength)
        {
            if (Main.dedServ)
                return;

            AegisVisuals.DirectionalImpact(position, direction, 1.1f * strength);
            AegisVisuals.EmberJet(position, direction, 10, 1.15f * strength, 0.3f);

            // 起步的新月拖抹：盾牌"劈"开空气的那一下
            GeneralParticleHandler.SpawnParticle(new CustomPulse(position, Vector2.Zero,
                AegisVisuals.Add(AegisVisuals.Gold, 0.9f), AegisVisuals.TexSmearFire1,
                new Vector2(1f, 1f), direction.ToRotation() + MathHelper.PiOver2,
                0.1f, 0.85f * strength, 15));
            GeneralParticleHandler.SpawnParticle(new CustomPulse(position, Vector2.Zero,
                AegisVisuals.Add(AegisVisuals.Ember, 0.8f), AegisVisuals.TexBlastCone,
                new Vector2(3.2f, 1.4f), direction.ToRotation(), 0.85f, 0f, 20));
        }

        // ── 冲刺拖尾：三层（余烬外焰 / 圣金主焰 / 白金内芯） ─────────────
        // 旧版的 OffsetFunction 会额外加上 Main.screenPosition，等于把世界坐标当屏幕坐标用，
        // 拖尾实际被画到了屏幕外 —— 玩家从来没看见过这条拖尾。这里一并修正。
        private Vector2 TrailOffsetFunction(float _, Vector2 __) => Projectile.Size * 0.5f;

        private Color TrailColorFunction(float completionRatio, Vector2 vertexPosition) =>
            AegisVisuals.TrailColor(completionRatio, 1, Projectile.Opacity);

        private float TrailWidthFunction(float completionRatio, Vector2 vertexPosition) =>
            MathHelper.Lerp(26f, 5f, completionRatio) * (0.7f + DashPower * 0.45f);

        private Color TrailOuterColorFunction(float completionRatio, Vector2 vertexPosition) =>
            AegisVisuals.TrailColor(completionRatio, 0, Projectile.Opacity * 0.65f);

        private float TrailOuterWidthFunction(float completionRatio, Vector2 vertexPosition) =>
            TrailWidthFunction(completionRatio, vertexPosition) * 1.55f;

        private Color TrailCoreColorFunction(float completionRatio, Vector2 vertexPosition) =>
            AegisVisuals.TrailColor(completionRatio, 2, Projectile.Opacity);

        private float TrailCoreWidthFunction(float completionRatio, Vector2 vertexPosition) =>
            TrailWidthFunction(completionRatio, vertexPosition) * 0.4f;

        // 头顶蓄力条（独立于大招能量条，大招显示在左上角CooldownHandler）
        private void DrawChargeBar()
        {
            if (Main.myPlayer != Projectile.owner) return;

            Texture2D barBG = ModContent.Request<Texture2D>("CalamityMod/UI/MiscTextures/GenericBarBack").Value;
            Texture2D barFG = ModContent.Request<Texture2D>("CalamityMod/UI/MiscTextures/GenericBarFront").Value;

            float progress = Math.Clamp(Charge / MaximumGuardCharge, 0f, 1f);
            Color col = Color.Lerp(AegisVisuals.Gold, AegisVisuals.Core, ChargeRatio);
            float pulseAlpha = 0.75f + 0.15f * MathF.Sin(Main.GlobalTimeWrappedHourly * 6f);

            // -70f：与大招能量条（左上角CooldownHandler）位置不同，此条显示在玩家头顶
            Vector2 barPos = Owner.Center - Main.screenPosition + new Vector2(-barBG.Width * 0.75f, -70f);
            Rectangle frame = new Rectangle(0, 0, (int)(progress * barFG.Width), barFG.Height);

            // 蓄满时在条后压一层圣火余晖，让"满了"这件事在余光里也看得见
            if (ChargeRatio >= 1f)
            {
                Texture2D bloom = AegisVisuals.Tex(AegisVisuals.TexBloom);
                Main.spriteBatch.Draw(bloom,
                    barPos + new Vector2(barBG.Width * 0.5f, barBG.Height * 0.5f), null,
                    AegisVisuals.Add(AegisVisuals.Gold, 0.4f * pulseAlpha), 0f, bloom.Size() * 0.5f,
                    new Vector2(barBG.Width / 130f, barBG.Height / 42f), SpriteEffects.None, 0f);
            }

            Main.spriteBatch.Draw(barBG, barPos, Color.Lerp(AegisVisuals.Charred, col, 0.35f) * 0.9f);
            Main.spriteBatch.Draw(barFG, barPos, frame, col * pulseAlpha);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.dedServ)
                return false;

            if (IsDashing && DashTime > 0f)
            {
                DrawDash();
                return false;
            }

            DrawGuard();
            DrawChargeBar();
            DrawDashTelegraph();
            return false;
        }

        /// <summary>冲刺形态：三层火焰拖尾 + 烧红的盾面 + 前推的日核。</summary>
        private void DrawDash()
        {
            var trailShader = GameShaders.Misc["CalamityMod:ImpFlameTrail"];

            trailShader.SetShaderTexture(
                ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/Trails/ScarletDevilStreak"));
            PrimitiveRenderer.RenderTrail(Projectile.oldPos,
                new PrimitiveSettings(TrailOuterWidthFunction, TrailOuterColorFunction, TrailOffsetFunction,
                    shader: trailShader), 12);
            PrimitiveRenderer.RenderTrail(Projectile.oldPos,
                new PrimitiveSettings(TrailWidthFunction, TrailColorFunction, TrailOffsetFunction,
                    shader: trailShader), 12);

            trailShader.SetShaderTexture(
                ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/Trails/SylvestaffStreak"));
            PrimitiveRenderer.RenderTrail(Projectile.oldPos,
                new PrimitiveSettings(TrailCoreWidthFunction, TrailCoreColorFunction, TrailOffsetFunction,
                    shader: trailShader), 12);

            float dashProgress = DashTime / DashDuration;
            float fade = 1f - dashProgress * 0.55f;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            Texture2D shieldTex = ModContent.Request<Texture2D>(ShieldTexturePath).Value;
            float shieldRotation = DashDirection.ToRotation();

            Main.spriteBatch.EnterShaderRegion(BlendState.Additive);

            // 前方压出的日核：冲刺的"矛头"
            AegisVisuals.DrawSolarCore(drawPosition + DashDirection * 18f,
                26f * (1f + DashPower * 0.35f), fade,
                Main.GlobalTimeWrappedHourly * 6f);

            // 盾面本体：先暗红背光再亮金
            AegisVisuals.ProfanedBackglow(shieldTex, drawPosition, null, shieldRotation,
                shieldTex.Size() * 0.5f, new Vector2(1.35f + DashPower * 0.4f), fade, 4f, 6);
            Main.EntitySpriteDraw(shieldTex, drawPosition, null,
                AegisVisuals.Add(AegisVisuals.Core, 0.95f * fade),
                shieldRotation, shieldTex.Size() * 0.5f, 1.3f + DashPower * 0.35f, SpriteEffects.None, 0);

            // 侧向压扁的冲击环
            Texture2D ring = AegisVisuals.Tex(AegisVisuals.TexRingThick);
            Main.EntitySpriteDraw(ring, drawPosition, null,
                AegisVisuals.Add(AegisVisuals.Ember, 0.55f * fade),
                shieldRotation, ring.Size() * 0.5f,
                new Vector2(AegisVisuals.RadiusScale(ring, 20f), AegisVisuals.RadiusScale(ring, 62f)) *
                (1f + DashPower * 0.3f), SpriteEffects.None, 0);

            Main.spriteBatch.ExitShaderRegion();
        }

        /// <summary>举盾形态：护罩壳 + 烧红的盾面 + 随蓄力收紧的符文环。</summary>
        private void DrawGuard()
        {
            float chargeProgress = Charge / MaximumGuardCharge;
            float chargeGlow = MathHelper.Lerp(0.12f, 1f, chargeProgress);
            Vector2 shieldPosition = ShieldWorldPosition - Main.screenPosition;
            Texture2D shieldTex = ModContent.Request<Texture2D>(ShieldTexturePath).Value;
            Vector2 shieldOrigin = shieldTex.Size() * 0.5f;
            float shieldRotation = Owner.MountedCenter.DirectionTo(AegisBlade.GetMouseWorld(Owner)).ToRotation();
            Vector2 aimDirection = shieldRotation.ToRotationVector2();
            float pulse = 0.86f + 0.14f * MathF.Sin(Main.GlobalTimeWrappedHourly * 9f);

            Main.spriteBatch.EnterShaderRegion(BlendState.Additive);

            // ① 圣火护罩壳：从盾面往前撑开的一层裂纹光壳，蓄力越满越实
            Texture2D barrier = AegisVisuals.Tex(AegisVisuals.TexBarrierShell);
            float barrierOpacity = MathHelper.Lerp(0.16f, 0.5f, chargeProgress) * pulse;
            Vector2 barrierCenter = shieldPosition + aimDirection * 16f;
            Main.EntitySpriteDraw(barrier, barrierCenter, null,
                AegisVisuals.Add(AegisVisuals.Ember, barrierOpacity),
                shieldRotation + Main.GlobalTimeWrappedHourly * 0.35f, barrier.Size() * 0.5f,
                new Vector2(AegisVisuals.RadiusScale(barrier, 30f), AegisVisuals.RadiusScale(barrier, 42f)),
                SpriteEffects.None, 0);
            Main.EntitySpriteDraw(barrier, barrierCenter, null,
                AegisVisuals.Add(AegisVisuals.Gold, barrierOpacity * 0.62f),
                shieldRotation - Main.GlobalTimeWrappedHourly * 0.55f, barrier.Size() * 0.5f,
                new Vector2(AegisVisuals.RadiusScale(barrier, 24f), AegisVisuals.RadiusScale(barrier, 34f)),
                SpriteEffects.None, 0);

            // ② 随蓄力收紧的符文环：环越小 = 蓄力越满，是世界内的蓄力读数
            if (ChargeUnlocked && Charge >= BalanceAegisBlade.ShieldRaiseFrames)
            {
                float sigilRadius = MathHelper.Lerp(72f, 34f, ChargeRatio);
                AegisVisuals.DrawRuneSigil(shieldPosition, sigilRadius,
                    Main.GlobalTimeWrappedHourly * (1.2f + ChargeRatio * 5.5f),
                    MathHelper.Lerp(0.25f, 0.9f, chargeProgress), Vector2.One,
                    0.85f + ChargeRatio * 0.7f);
            }

            // ③ 亵渎背光 + 盾面本体
            AegisVisuals.ProfanedBackglow(shieldTex, shieldPosition, null, shieldRotation, shieldOrigin,
                new Vector2(1.12f), 0.55f + chargeGlow * 0.45f, 3.2f, 6);
            Main.EntitySpriteDraw(shieldTex, shieldPosition, null,
                AegisVisuals.Add(AegisVisuals.Flame, 0.55f + chargeGlow * 0.35f),
                shieldRotation, shieldOrigin, 1.16f, SpriteEffects.None, 0);
            Main.EntitySpriteDraw(shieldTex, shieldPosition, null,
                AegisVisuals.Add(AegisVisuals.Core, 0.4f + chargeGlow * 0.5f),
                shieldRotation, shieldOrigin, 1.02f, SpriteEffects.None, 0);

            // ④ 盾心炉光
            Texture2D bloom = AegisVisuals.Tex(AegisVisuals.TexBloom);
            Main.EntitySpriteDraw(bloom, shieldPosition, null,
                AegisVisuals.Add(AegisVisuals.Gold, (0.28f + chargeGlow * 0.42f) * pulse),
                0f, bloom.Size() * 0.5f,
                new Vector2(AegisVisuals.RadiusScale(bloom, 16f + chargeGlow * 12f)), SpriteEffects.None, 0);

            // ⑤ 完美格挡的一次性白闪
            if (parryFlash > 0.01f)
            {
                float flash = MathF.Pow(parryFlash, 0.6f);
                Vector2 playerPosition = Owner.Center - Main.screenPosition;
                Main.EntitySpriteDraw(bloom, playerPosition, null,
                    AegisVisuals.Add(AegisVisuals.Core, 0.85f * flash),
                    0f, bloom.Size() * 0.5f,
                    new Vector2(AegisVisuals.RadiusScale(bloom, 40f + 130f * (1f - parryFlash))),
                    SpriteEffects.None, 0);
                AegisVisuals.DrawRuneSigil(playerPosition, 40f + 120f * (1f - parryFlash),
                    Main.GlobalTimeWrappedHourly * 5f, flash * 0.9f, Vector2.One, 1.5f);
            }

            Main.spriteBatch.ExitShaderRegion();
        }

        /// <summary>冲刺落点预示。沿用 StygianShield 的 SpreadTelegraph 着色器，只是换成本武器配色。</summary>
        private void DrawDashTelegraph()
        {
            if (Charge < MinimumDashCharge)
                return;

            Texture2D arrowTex = ModContent.Request<Texture2D>(Texture).Value;
            Effect arrowEffect = Filters.Scene["CalamityMod:SpreadTelegraph"].GetShader().Shader;
            arrowEffect.Parameters["centerOpacity"].SetValue(1f);
            arrowEffect.Parameters["mainOpacity"].SetValue(1f);
            arrowEffect.Parameters["edgeBlendLength"].SetValue(0.07f);
            arrowEffect.Parameters["edgeBlendStrength"].SetValue(8f);

            Vector2 mouseWorld = AegisBlade.GetMouseWorld(Owner);
            Vector2 dashDir = Projectile.SafeDirectionTo(mouseWorld);
            float dashDist = MathHelper.Lerp(BalanceAegisBlade.ShieldDashMinimumDistance, BalanceAegisBlade.ShieldDashMaximumDistance, DashPower);
            Vector2 dashVec = dashDir * dashDist;
            Vector2 destination = Projectile.Center + dashVec;

            bool oob = destination.X < 660f || destination.Y < 660f ||
                       destination.X > Main.maxTilesX * 16f - 680f || destination.Y > Main.maxTilesY * 16f - 680f;

            // 可冲 = 圣金（与整把武器同色系）；不可冲 = 冷灰蓝，
            // 在这套全暖色的视觉里，"冷掉了"比"变红"更能一眼读出"这一下发不出去"。
            Color telegraphColor = oob ? new Color(96, 104, 128) : AegisVisuals.Gold;

            arrowEffect.Parameters["centerOpacity"].SetValue(0.6f);
            arrowEffect.Parameters["halfSpreadAngle"].SetValue(MathHelper.ToRadians(64f));
            arrowEffect.Parameters["edgeColor"].SetValue(telegraphColor.ToVector3());
            arrowEffect.Parameters["centerColor"].SetValue(
                (oob ? telegraphColor : AegisVisuals.Core).ToVector3());

            Main.spriteBatch.EnterShaderRegion(BlendState.Additive, arrowEffect);
            Main.EntitySpriteDraw(arrowTex, destination - Main.screenPosition, null, Color.White,
                dashDir.ToRotation() - MathHelper.Pi, arrowTex.Size() / 2f, 135f, SpriteEffects.None);
            Main.spriteBatch.ExitShaderRegion();

            for (int i = -1; i <= 1; i += 2)
            {
                Vector2 sideOrigin = Projectile.Center + dashDir.RotatedBy(90f * i) * 60f;
                Vector2 convergence = Projectile.Center + dashVec * 0.1f;
                Vector2 sideLineStart = sideOrigin + dashVec * 0.1f;
                Vector2 sideLineEnd = sideOrigin + dashVec;
                Color lineColor = telegraphColor * 0.3f;
                Main.spriteBatch.DrawLineBetter(sideLineStart, convergence, lineColor, 2f);
                Main.spriteBatch.DrawLineBetter(sideLineStart, sideLineEnd, lineColor, 4f);
                Main.spriteBatch.DrawLineBetter(sideLineEnd, destination, lineColor, 2f);
            }
        }

        public override void OnKill(int timeLeft)
        {
            if (!IsDashing)
            {
                // 放下盾牌：火在盾面上熄灭，只留几缕圣灰
                if (!Main.dedServ && shieldRaisedFired)
                {
                    Vector2 shieldPos = ShieldWorldPosition;
                    for (int i = 0; i < 3; i++)
                    {
                        GeneralParticleHandler.SpawnParticle(new MediumMistParticle(
                            shieldPos + Main.rand.NextVector2Circular(8f, 12f),
                            new Vector2(Owner.direction * Main.rand.NextFloat(0.2f, 0.7f), -Main.rand.NextFloat(0.3f, 1.1f)),
                            Color.Lerp(AegisVisuals.Charred, Color.DarkSlateGray, Main.rand.NextFloat(0.35f, 0.9f)),
                            Color.Transparent, Main.rand.NextFloat(0.14f, 0.26f), Main.rand.Next(16, 26), 0f));
                    }
                    AegisVisuals.EmberDrip(shieldPos, 7f, 9f, 0.9f);
                }
                return;
            }

            Vector2 direction = DashDestination != Vector2.Zero
                ? DashDirection
                : Owner.velocity.SafeNormalize(Vector2.UnitX * Owner.direction);
            Owner.velocity = direction * (dashSpeed * 0.45f);
            SpawnDashFlash(Owner.Center, direction, 1.15f + DashPower * 0.35f);
        }
    }
}
