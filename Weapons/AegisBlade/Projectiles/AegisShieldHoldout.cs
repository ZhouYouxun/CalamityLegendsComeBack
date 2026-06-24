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

        private Vector2 DashDestination;
        private float dashDistance;
        private bool perfectParryFired;
        private bool fullChargeFired;
        private bool previousLeftDown;

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

            // Fully charged guard retains the existing sword-plunge / earth-wall action.
            if (ChargeUnlocked && leftJustPressed && Charge >= FullChargeTime && Main.myPlayer == Projectile.owner)
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
            Vector2 intendedDestination = Projectile.Center + dashDirection * dashDistance;

            if (intendedDestination.X < 660f || intendedDestination.Y < 660f ||
                intendedDestination.X > Main.maxTilesX * 16f - 680f ||
                intendedDestination.Y > Main.maxTilesY * 16f - 680f)
            {
                Projectile.Kill();
                return;
            }

            DashDestination = intendedDestination;
            Projectile.velocity = dashDirection * dashDistance / DashDuration;
            float damageMultiplier = MathHelper.Lerp(1f, BalanceAegisBlade.ShieldDashMaxDamageMultiplier, DashPower);
            if (perfectParryFired)
                damageMultiplier *= BalanceAegisBlade.PerfectParryDashDamageMultiplier;
            Projectile.damage = Math.Max(1, (int)(Projectile.damage * damageMultiplier));
            Projectile.ExpandHitboxBy(BalanceAegisBlade.ShieldDashHitboxExpansion);
            Projectile.tileCollide = true;
            Projectile.netUpdate = true;

            Owner.immune = true;
            Owner.immuneNoBlink = true;
            Owner.immuneTime = DashDuration;
            for (int i = 0; i < Owner.hurtCooldowns.Length; i++)
                Owner.hurtCooldowns[i] = Owner.immuneTime;

            for (int i = 0; i < Projectile.oldPos.Length; i++)
            {
                Projectile.oldPos[i] = Vector2.Zero;
                Projectile.oldRot[i] = 0f;
                Projectile.oldSpriteDirection[i] = 0;
            }

            SoundEngine.PlaySound(SoundID.Item74 with { Volume = 1.05f, Pitch = -0.2f + DashPower * 0.22f }, Owner.Center);
            SpawnDashFlash(Projectile.Center, dashDirection, 1f + DashPower * 0.35f);
        }

        private void DoDash()
        {
            DetachOwnerFromHooksAndMounts();

            DashTime++;
            Vector2 direction = Projectile.SafeDirectionTo(DashDestination);
            Projectile.velocity = direction * dashDistance / DashDuration;

            if (Vector2.Distance(Projectile.Center, DashDestination) < 6f || DashTime > DashDuration ||
                Collision.SolidCollision(Projectile.Center, 1, 1, false))
            {
                Projectile.Kill();
                return;
            }

            Owner.Center = Projectile.Center;
            Owner.ChangeDir(direction.X >= 0f ? 1 : -1);
            Owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full,
                direction.ToRotation() - MathHelper.PiOver2);

            if (!Main.dedServ && (int)DashTime % 2 == 0)
                EmitDashFlames(direction);
        }

        private void DetachOwnerFromHooksAndMounts()
        {
            if (Owner.mount != null)
                Owner.mount.Dismount(Owner);

            foreach (Projectile projectile in Main.ActiveProjectiles)
            {
                if (projectile.owner == Owner.whoAmI && projectile.aiStyle == ProjAIStyleID.Hook)
                    projectile.Kill();
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

            Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitX * Owner.direction);
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
            Projectile.NewProjectile(Projectile.GetSource_FromThis(), Owner.Center, Vector2.UnitY * 26f,
                ModContent.ProjectileType<AegisBladeThrown>(), damage, 6f, Projectile.owner);
            SoundEngine.PlaySound(SoundID.Item74 with { Volume = 1f, Pitch = -0.35f }, Owner.Center);
        }

        private void EmitGuardFlames()
        {
            if (Main.dedServ || !ChargeUnlocked || Charge < ChargeStartTime || !Main.rand.NextBool(2))
                return;

            Vector2 shieldPosition = Owner.Center + new Vector2(Owner.direction * Owner.width * 0.42f, -4f);
            Vector2 outward = new Vector2(Owner.direction, 0f).RotatedByRandom(0.7f);
            GeneralParticleHandler.SpawnParticle(new MediumMistParticle(
                shieldPosition + Main.rand.NextVector2Circular(14f, 22f), outward * Main.rand.NextFloat(0.4f, 1.9f),
                Color.Lerp(ShieldGold, ShieldLight, ChargeRatio), Color.Transparent,
                MathHelper.Lerp(0.28f, 0.55f, ChargeRatio), Main.rand.Next(16, 25), Main.rand.NextFloat(-0.06f, 0.06f)));
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
                    Projectile.velocity.ToRotation(), ring.Size() * 0.5f,
                    new Vector2(0.34f, 1.55f) * (1f + DashPower * 0.3f), SpriteEffects.None);
                Main.spriteBatch.ExitShaderRegion();
                return false;
            }

            // Native shield frames provide the actual silhouette. This additive layer only supplies the
            // right-click glow, so the sheet remains crisp instead of being washed out by a second sprite.
            float pulse = 0.22f + 0.1f * MathF.Sin(Main.GlobalTimeWrappedHourly * 5f);
            float chargeGlow = MathHelper.Lerp(0.1f, 0.8f, ChargeRatio);
            Vector2 shieldPosition = Owner.Center - Main.screenPosition + new Vector2(Owner.direction * Owner.width * 0.42f, -4f);
            Main.spriteBatch.EnterShaderRegion(BlendState.Additive);
            Main.EntitySpriteDraw(bloom, shieldPosition, null, ShieldGold with { A = 0 } * (pulse + chargeGlow * 0.35f),
                0f, bloom.Size() * 0.5f, 0.36f + chargeGlow * 0.34f, SpriteEffects.None);
            if (perfectParryFired || Charge >= FullChargeTime)
            {
                float ringPulse = 0.76f + 0.24f * MathF.Sin(Main.GlobalTimeWrappedHourly * 8f);
                Main.EntitySpriteDraw(ring, shieldPosition, null, ShieldLight with { A = 0 } * ringPulse * 0.4f,
                    Main.GlobalTimeWrappedHourly * 1.8f, ring.Size() * 0.5f, 0.34f + chargeGlow * 0.28f, SpriteEffects.None);
            }
            Main.spriteBatch.ExitShaderRegion();
            return false;
        }

        public override void OnKill(int timeLeft)
        {
            if (!IsDashing)
                return;

            Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitX * Owner.direction);
            SpawnDashFlash(Projectile.Center, direction, 1.15f + DashPower * 0.35f);
        }
    }
}
