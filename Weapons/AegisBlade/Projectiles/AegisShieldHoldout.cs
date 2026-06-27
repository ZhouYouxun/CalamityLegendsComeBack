using System;
using CalamityMod;
using CalamityMod.Graphics.Primitives;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
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
        private const int BashDamageFrames = 15;

        private Player Owner => Main.player[Projectile.owner];
        private AegisBladePlayer BladePlayer => Owner.GetModPlayer<AegisBladePlayer>();
        private ref float Charge => ref Projectile.ai[0];
        private ref float DashTime => ref Projectile.ai[1];
        private bool IsDashing => DashDestination != Vector2.Zero;
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
        private bool perfectParryFired;
        private bool fullChargeFired;
        private bool previousLeftDown;
        private bool shieldRaisedFired;

        private static readonly Color ShieldGold = new(255, 200, 60);
        private static readonly Color ShieldLight = new(255, 238, 160);
        private static readonly Color ShieldFire = new(255, 145, 55);

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

        public override void AI()
        {
            if (!Owner.active || Owner.dead || Owner.HeldItem.type != ModContent.ItemType<AegisBlade>())
            {
                Projectile.Kill();
                return;
            }

            Owner.heldProj = Projectile.whoAmI;
            if (IsDashing)
            {
                DoDash();
                return;
            }

            Projectile.Center = Owner.MountedCenter;
            Projectile.timeLeft = 4;

            bool rightHeld = IsRightHeld();
            bool leftHeld = IsLeftHeld();
            bool leftJustPressed = leftHeld && !previousLeftDown;
            previousLeftDown = leftHeld;

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

            if (leftJustPressed && Main.myPlayer == Projectile.owner)
            {
                TriggerBladePlunge();
                Projectile.Kill();
                return;
            }

            EmitGuardFlames();
        }

        private bool IsRightHeld()
        {
            if (Main.myPlayer != Projectile.owner)
                return true;

            return Owner.Calamity().mouseRight && !Main.mapFullscreen && !Main.blockMouse && !Owner.mouseInterface;
        }

        private bool IsLeftHeld()
        {
            if (Main.myPlayer != Projectile.owner)
                return false;

            return Main.mouseLeft && !Main.mapFullscreen && !Main.blockMouse && !Owner.mouseInterface;
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

            // 速度 = 原冲刺速度的33%，不强制停止，交由物理减速
            Owner.velocity = dashDirection * dashDistance / DashDuration * 0.33f;

            // DashDestination 存储方向单位向量（非零即代表处于冲刺中）
            DashDestination = dashDirection;

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
            Projectile.Center = Owner.MountedCenter;
            Projectile.timeLeft = 4;

            Vector2 direction = DashDestination;  // 已是单位方向向量
            Owner.ChangeDir(direction.X >= 0f ? 1 : -1);
            Owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full,
                direction.ToRotation() - MathHelper.PiOver2);

            if (!Main.dedServ && (int)DashTime % 2 == 0)
                EmitDashFlames(direction);

            if (DashTime >= BashDamageFrames)
                Projectile.Kill();
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
                ? DashDestination
                : Owner.velocity.SafeNormalize(Vector2.UnitX * Owner.direction);
            GeneralParticleHandler.SpawnParticle(new DetailedExplosion(target.Center, Vector2.Zero,
                ShieldFire, new Vector2(1.15f, 0.72f), direction.ToRotation(), 0f, 0.72f, 20));
            GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(target.Center, direction * 1.4f,
                ShieldLight, new Vector2(1.2f, 2.6f), direction.ToRotation(), 0.08f, 0.08f, 18));
            for (int i = 0; i < 4; i++)
            {
                GeneralParticleHandler.SpawnParticle(new MediumMistParticle(
                    target.Center + Main.rand.NextVector2Circular(16f, 16f),
                    direction.RotatedByRandom(0.42f) * Main.rand.NextFloat(1.6f, 5f),
                    Color.Lerp(ShieldFire, ShieldLight, Main.rand.NextFloat()), Color.Transparent,
                    Main.rand.NextFloat(0.42f, 0.7f), Main.rand.Next(16, 24), Main.rand.NextFloat(-0.07f, 0.07f)));
            }
        }

        private void OnPerfectParry()
        {
            SoundEngine.PlaySound(SoundID.DD2_WitherBeastCrystalImpact with { Volume = 1f, Pitch = 0.25f }, Owner.Center);
            if (Main.dedServ)
                return;

            GeneralParticleHandler.SpawnParticle(new DetailedExplosion(Owner.Center, Vector2.Zero,
                ShieldLight, Vector2.One, 0f, 0f, 0.65f, 18));
            GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(Owner.Center, Vector2.Zero,
                ShieldGold, Vector2.One, 0f, 0.08f, 1.65f, 20));
        }

        private void OnShieldRaised()
        {
            SoundEngine.PlaySound(SoundID.Item68 with { Volume = 0.65f, Pitch = 0.12f }, Owner.Center);
            if (Main.dedServ)
                return;

            Vector2 shieldPos = Owner.Center + new Vector2(Owner.direction * Owner.width * 0.42f, -4f);
            GeneralParticleHandler.SpawnParticle(new DetailedExplosion(shieldPos, Vector2.Zero,
                ShieldLight, Vector2.One, 0f, 0f, 0.44f, 15));
            GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(shieldPos, Vector2.Zero,
                ShieldGold, Vector2.One, 0f, 0.06f, 1.1f, 16));
        }

        private void OnFullCharge()
        {
            SoundEngine.PlaySound(SoundID.Item67 with { Volume = 0.95f, Pitch = 0.12f }, Owner.Center);
            if (Main.dedServ)
                return;

            GeneralParticleHandler.SpawnParticle(new DetailedExplosion(Owner.Center, Vector2.Zero,
                ShieldLight, Vector2.One, 0f, 0f, 0.78f, 22));
            GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(Owner.Center, Vector2.Zero,
                ShieldGold, new Vector2(1.35f, 1.35f), MathHelper.PiOver4, 0.1f, 1.9f, 24));
        }

        private void TriggerBladePlunge()
        {
            int damage = new BalanceAegisBlade().GetBladePlungeDamage();
            Vector2 throwDirection = Owner.MountedCenter.DirectionTo(AegisBlade.GetMouseWorld(Owner))
                .SafeNormalize(Vector2.UnitX * Owner.direction);
            Projectile.NewProjectile(Projectile.GetSource_FromThis(), Owner.MountedCenter, throwDirection * 18f,
                ModContent.ProjectileType<AegisBladeThrown>(), damage, 6f, Projectile.owner);
            SoundEngine.PlaySound(SoundID.Item74 with { Volume = 1f, Pitch = -0.35f }, Owner.Center);
        }

        private void EmitGuardFlames()
        {
            if (Main.dedServ || !Main.rand.NextBool(2))
                return;

            Vector2 shieldPosition = Owner.Center + new Vector2(Owner.direction * Owner.width * 0.42f, -4f);

            if (BladePlayer.ShieldRaising)
            {
                // 举盾动画阶段：随进度增强的金色粒子
                float raiseRatio = MathHelper.Clamp(Charge / BalanceAegisBlade.ShieldRaiseFrames, 0f, 1f);
                Vector2 outward = new Vector2(Owner.direction, -0.6f).RotatedByRandom(0.65f);
                GeneralParticleHandler.SpawnParticle(new MediumMistParticle(
                    shieldPosition + Main.rand.NextVector2Circular(10f, 14f),
                    outward * Main.rand.NextFloat(0.5f, 1.6f) * (0.5f + raiseRatio),
                    Color.Lerp(ShieldGold, ShieldLight, Main.rand.NextFloat()), Color.Transparent,
                    MathHelper.Lerp(0.16f, 0.28f, raiseRatio), Main.rand.Next(12, 20), Main.rand.NextFloat(-0.05f, 0.05f)));
            }
            else if (!ChargeUnlocked || Charge < ChargeStartTime)
            {
                // 举盾持续阶段：微弱稀疏粒子
                if (!Main.rand.NextBool(3))
                    return;

                float holdRatio = MathHelper.Clamp(Charge / ChargeStartTime, 0f, 1f);
                Vector2 outward = new Vector2(Owner.direction, 0f).RotatedByRandom(0.55f);
                GeneralParticleHandler.SpawnParticle(new MediumMistParticle(
                    shieldPosition + Main.rand.NextVector2Circular(8f, 12f),
                    outward * Main.rand.NextFloat(0.15f, 0.65f),
                    Color.Lerp(ShieldGold, ShieldLight, Main.rand.NextFloat()), Color.Transparent,
                    MathHelper.Lerp(0.12f, 0.22f, holdRatio), Main.rand.Next(10, 18), Main.rand.NextFloat(-0.05f, 0.05f)));
            }
            else
            {
                // 蓄力阶段：火焰粒子
                Vector2 outward = new Vector2(Owner.direction, 0f).RotatedByRandom(0.7f);
                GeneralParticleHandler.SpawnParticle(new MediumMistParticle(
                    shieldPosition + Main.rand.NextVector2Circular(10f, 16f),
                    outward * Main.rand.NextFloat(0.3f, 1.5f),
                    Color.Lerp(ShieldGold, ShieldLight, ChargeRatio), Color.Transparent,
                    MathHelper.Lerp(0.18f, 0.38f, ChargeRatio), Main.rand.Next(12, 20), Main.rand.NextFloat(-0.05f, 0.05f)));
            }
        }

        private void EmitDashFlames(Vector2 direction)
        {
            Vector2 rear = Projectile.Center - direction * Main.rand.NextFloat(16f, 42f);
            GeneralParticleHandler.SpawnParticle(new MediumMistParticle(
                rear + Main.rand.NextVector2Circular(10f, 10f), -direction.RotatedByRandom(0.34f) * Main.rand.NextFloat(2f, 5f),
                Color.Lerp(ShieldFire, ShieldLight, Main.rand.NextFloat(0.15f, 0.7f)), Color.Transparent,
                Main.rand.NextFloat(0.4f, 0.68f), Main.rand.Next(18, 28), Main.rand.NextFloat(-0.08f, 0.08f)));
        }

        private void SpawnDashFlash(Vector2 position, Vector2 direction, float strength)
        {
            if (Main.dedServ)
                return;

            GeneralParticleHandler.SpawnParticle(new DetailedExplosion(position, Vector2.Zero,
                ShieldLight, Vector2.One, direction.ToRotation(), 0f, 0.58f * strength, 18));
            GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(position, direction * 1.8f,
                ShieldGold, new Vector2(0.9f, 2.2f) * strength, direction.ToRotation(), 0.09f, 0.08f, 18));
        }

        private Color TrailColorFunction(float completionRatio, Vector2 vertexPosition)
        {
            float fade = Utils.GetLerpValue(1f, 0.12f, completionRatio, true) * Projectile.Opacity;
            return Color.Lerp(ShieldLight, ShieldFire, completionRatio) * fade;
        }

        private float TrailWidthFunction(float completionRatio, Vector2 vertexPosition)
        {
            return MathHelper.Lerp(22f, 5f, completionRatio) * (0.7f + DashPower * 0.45f);
        }

        // 头顶蓄力条（独立于大招能量条，大招显示在左上角CooldownHandler）
        private void DrawChargeBar()
        {
            if (Main.myPlayer != Projectile.owner) return;

            Texture2D barBG = ModContent.Request<Texture2D>("CalamityMod/UI/MiscTextures/GenericBarBack").Value;
            Texture2D barFG = ModContent.Request<Texture2D>("CalamityMod/UI/MiscTextures/GenericBarFront").Value;

            float progress = Math.Clamp(Charge / MaximumGuardCharge, 0f, 1f);
            Color col = Color.Lerp(ShieldLight, ShieldGold, ChargeRatio);
            float pulseAlpha = 0.75f + 0.15f * MathF.Sin(Main.GlobalTimeWrappedHourly * 6f);

            // -70f：与大招能量条（左上角CooldownHandler）位置不同，此条显示在玩家头顶
            Vector2 barPos = Owner.Center - Main.screenPosition + new Vector2(-barBG.Width * 0.75f, -70f);
            Rectangle frame = new Rectangle(0, 0, (int)(progress * barFG.Width), barFG.Height);

            Main.spriteBatch.Draw(barBG, barPos, col * 0.9f);
            Main.spriteBatch.Draw(barFG, barPos, frame, col * pulseAlpha);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.dedServ)
                return false;

            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Texture2D ring = ModContent.Request<Texture2D>("CalamityMod/Particles/HollowCircleHardEdge").Value;

            if (IsDashing && DashTime > 0f)
            {
                GameShaders.Misc["CalamityMod:TrailStreak"].SetShaderTexture(
                    ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/Trails/ScarletDevilStreak"));
                PrimitiveRenderer.RenderTrail(Projectile.oldPos,
                    new PrimitiveSettings(TrailWidthFunction, TrailColorFunction,
                        (_, _) => Projectile.Size * 0.5f + Main.screenPosition,
                        shader: GameShaders.Misc["CalamityMod:TrailStreak"]), 12);

                float dashProgress = DashTime / DashDuration;
                Vector2 drawPosition = Projectile.Center - Main.screenPosition;
                Main.spriteBatch.EnterShaderRegion(BlendState.Additive);
                Main.EntitySpriteDraw(bloom, drawPosition, null, ShieldFire with { A = 0 } * (1f - dashProgress) * 0.8f,
                    0f, bloom.Size() * 0.5f, 1.15f + DashPower * 0.45f, SpriteEffects.None);
                Main.EntitySpriteDraw(ring, drawPosition, null, ShieldLight with { A = 0 } * (1f - dashProgress) * 0.45f,
                    DashDestination.ToRotation(), ring.Size() * 0.5f,
                    new Vector2(0.34f, 1.55f) * (1f + DashPower * 0.3f), SpriteEffects.None);
                Main.spriteBatch.ExitShaderRegion();
                return false;
            }

            // 盾牌举起全程均有随蓄力增强的包边光晕（缩小以适应小盾）
            float chargeProgress = Charge / MaximumGuardCharge;
            float pulse = 0.22f + 0.1f * MathF.Sin(Main.GlobalTimeWrappedHourly * 5f);
            float chargeGlow = MathHelper.Lerp(0.04f, 0.72f, chargeProgress);
            Vector2 shieldPosition = Owner.Center - Main.screenPosition + new Vector2(Owner.direction * Owner.width * 0.42f, -4f);

            Main.spriteBatch.EnterShaderRegion(BlendState.Additive);
            Main.EntitySpriteDraw(bloom, shieldPosition, null, ShieldGold with { A = 0 } * (pulse + chargeGlow * 0.32f),
                0f, bloom.Size() * 0.5f, 0.22f + chargeGlow * 0.28f, SpriteEffects.None);

            if (perfectParryFired || Charge >= FullChargeTime)
            {
                float ringPulse = 0.76f + 0.24f * MathF.Sin(Main.GlobalTimeWrappedHourly * 8f);
                Main.EntitySpriteDraw(ring, shieldPosition, null, ShieldLight with { A = 0 } * ringPulse * 0.4f,
                    Main.GlobalTimeWrappedHourly * 1.8f, ring.Size() * 0.5f, 0.24f + chargeGlow * 0.22f, SpriteEffects.None);
            }

            // 蓄力阶段：额外旋转光圈（较小）
            if (BladePlayer.ShieldCharging || BladePlayer.ShieldFullyCharged)
            {
                Main.EntitySpriteDraw(ring, shieldPosition, null, ShieldGold with { A = 0 } * ChargeRatio * 0.28f,
                    -Main.GlobalTimeWrappedHourly * 2.4f, ring.Size() * 0.5f,
                    0.16f + ChargeRatio * 0.12f, SpriteEffects.None);
            }

            Main.spriteBatch.ExitShaderRegion();

            DrawChargeBar();
            return false;
        }

        public override void OnKill(int timeLeft)
        {
            if (!IsDashing)
            {
                // 放下盾牌：极小的消散粒子（质感"很低"）
                if (!Main.dedServ && shieldRaisedFired)
                {
                    Vector2 shieldPos = Owner.Center + new Vector2(Owner.direction * Owner.width * 0.42f, -4f);
                    GeneralParticleHandler.SpawnParticle(new MediumMistParticle(
                        shieldPos, new Vector2(Owner.direction * 0.6f, -0.4f),
                        ShieldGold, Color.Transparent, 0.10f, 12, 0f));
                }
                return;
            }

            Vector2 direction = DashDestination != Vector2.Zero
                ? DashDestination
                : Owner.velocity.SafeNormalize(Vector2.UnitX * Owner.direction);
            SpawnDashFlash(Owner.Center, direction, 1.15f + DashPower * 0.35f);
        }
    }
}
