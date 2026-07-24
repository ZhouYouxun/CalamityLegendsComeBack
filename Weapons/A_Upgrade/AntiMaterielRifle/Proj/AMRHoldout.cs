using System;
using CalamityMod;
using CalamityMod.Particles;
using CalamityMod.Sounds;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Upgrade.AntiMaterielRifle.Proj
{
    internal sealed class AMRHoldout : ModProjectile, ILocalizedModType
    {
        private const float HoldoutDistance = 34f;
        private const float BarrelLength = 74f;

        private int leftFireTimer;
        private int chargeFrames;
        private int muzzleFlashTimer;
        private int useAnimationTimer;
        private float recoilOffset;
        private bool rightAiming;
        private bool suppressedRightUntilRelease;
        private bool rightInteractionLastFrame;
        private bool playedReadySound;

        public new string LocalizationCategory => "Projectiles.AntiMaterielRifle";
        public override string Texture => "CalamityMod/Items/Weapons/Ranged/AntiMaterielRifle";

        private Player Owner => Main.player[Projectile.owner];
        private AMRPlayer AMRPlayer => Owner.GetModPlayer<AMRPlayer>();
        internal Vector2 AimDirection => Projectile.velocity.SafeNormalize(Vector2.UnitX * Owner.direction);
        internal Vector2 GunTipPosition => Projectile.Center + AimDirection * BarrelLength;
        internal float ScopeChargeCompletion => MathHelper.Clamp(chargeFrames / (float)AMRBalance.ScopeChargeFrames, 0f, 1f);
        internal bool RightAiming => rightAiming;

        public override void SetDefaults()
        {
            Projectile.width = 154;
            Projectile.height = 40;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.netImportant = true;
        }

        public override bool ShouldUpdatePosition() => false;
        public override bool? CanDamage() => false;

        public override void AI()
        {
            if (!Owner.active || Owner.dead || Owner.noItems || Owner.CCed ||
                Owner.HeldItem.type != ModContent.ItemType<NewLegendAntiMaterielRifle>())
            {
                CancelAim();
                Projectile.Kill();
                return;
            }

            Projectile.damage = Owner.GetWeaponDamage(Owner.HeldItem);
            Projectile.knockBack = Owner.HeldItem.knockBack;
            Projectile.timeLeft = 2;
            AMRPlayer.SetHoldingWeapon();

            Owner.Calamity().mouseWorldListener = true;
            if (Main.myPlayer == Owner.whoAmI)
                Owner.Calamity().rightClickListener = true;

            UpdateTimers();
            UpdatePose();

            if (Main.myPlayer == Projectile.owner)
                HandleInputs();

            if (rightAiming)
                UpdateAimState();
        }

        private void UpdateTimers()
        {
            if (leftFireTimer > 0)
                leftFireTimer--;
            if (muzzleFlashTimer > 0)
                muzzleFlashTimer--;
            if (useAnimationTimer > 0)
                useAnimationTimer--;

            recoilOffset = MathHelper.Lerp(recoilOffset, 0f, 0.2f);
        }

        private void HandleInputs()
        {
            bool rawRightHeld = Main.mouseRight || Owner.Calamity().mouseRight;
            bool interactionActive = NewLegendAntiMaterielRifle.IsWorldRightClickInteractionActive(Owner);

            if (!rawRightHeld)
            {
                if (rightAiming)
                    ReleaseAimedShot(NewLegendAntiMaterielRifle.CanUseWorldInput(Owner));

                suppressedRightUntilRelease = false;
                rightInteractionLastFrame = interactionActive;
            }
            else
            {
                if (interactionActive || rightInteractionLastFrame)
                    suppressedRightUntilRelease = true;

                rightInteractionLastFrame = interactionActive;

                bool validRightHold = Owner.Calamity().mouseRight &&
                    !suppressedRightUntilRelease &&
                    !interactionActive &&
                    NewLegendAntiMaterielRifle.CanUseWorldInput(Owner);

                if (validRightHold)
                    StartOrContinueAim();
                else if (rightAiming)
                    CancelAim();
            }

            bool leftHeld = Main.mouseLeft &&
                !rawRightHeld &&
                !rightAiming &&
                NewLegendAntiMaterielRifle.CanUseWorldInput(Owner);

            if (leftHeld)
                TryFireLeft();
        }

        private void StartOrContinueAim()
        {
            if (!rightAiming)
            {
                rightAiming = true;
                chargeFrames = 0;
                playedReadySound = false;
                SpawnAimScope();
                SoundEngine.PlaySound(SoundID.Item15 with { Volume = 0.35f, Pitch = -0.35f }, Owner.Center);
            }

            chargeFrames = Math.Min(chargeFrames + 1, AMRBalance.ScopeChargeFrames);
            useAnimationTimer = 2;
        }

        private void UpdateAimState()
        {
            bool zoomEnabled = Owner.HeldItem.ModItem is NewLegendAntiMaterielRifle rifle && rifle.ScopeZoomEnabled;
            AMRPlayer.RequestScope(ScopeChargeCompletion, zoomEnabled);

            float charge = ScopeChargeCompletion;

            float moveMult = MathHelper.Clamp(1f - charge, 0f, 1f);
            Owner.moveSpeed *= moveMult;
            Owner.maxRunSpeed *= moveMult;
            Owner.accRunSpeed *= moveMult;
            Owner.velocity.X *= MathHelper.Lerp(1f, 0.4f, charge);

            float defMult = MathHelper.Clamp(1f - 0.5f * charge, 0.5f, 1f);
            Owner.statDefense *= defMult;

            if (!playedReadySound && chargeFrames >= AMRBalance.ScopeChargeFrames)
            {
                playedReadySound = true;
                SoundEngine.PlaySound(SoundID.Item82 with { Volume = 0.55f, Pitch = 0.25f }, GunTipPosition);
            }
        }

        private void ReleaseAimedShot(bool allowFire)
        {
            bool chargedEnough = chargeFrames >= AMRBalance.MinimumScopeChargeFrames;
            float charge = ScopeChargeCompletion;
            rightAiming = false;
            chargeFrames = 0;
            playedReadySound = false;

            if (allowFire && chargedEnough)
                FireShot(rightShot: true, charge);
        }

        private void CancelAim()
        {
            rightAiming = false;
            chargeFrames = 0;
            playedReadySound = false;
        }

        private void TryFireLeft()
        {
            if (leftFireTimer > 0 || AMRPlayer.IsSliding)
                return;

            if (!FireShot(rightShot: false, 0f))
                return;

            leftFireTimer = AMRBalance.FireInterval;
        }

        private bool FireShot(bool rightShot, float charge)
        {
            if (!Owner.PickAmmo(Owner.HeldItem, out _, out _, out int ammoDamage, out float ammoKnockback, out _, false))
            {
                SoundEngine.PlaySound(SoundID.MenuTick with { Volume = 0.35f, Pitch = -0.4f }, Owner.Center);
                return false;
            }

            Vector2 direction = AimDirection;
            float speed = rightShot ? AMRBalance.RightProjectileSpeed(charge) : AMRBalance.LeftProjectileSpeed;
            float slideMultiplier = rightShot ? AMRPlayer.ConsumeSlideEmpowerment() : 1f;
            int damage = rightShot
                ? (int)(ammoDamage * AMRBalance.RightDamageMultiplier(charge) * slideMultiplier)
                : ammoDamage;
            float knockback = Math.Max(Owner.HeldItem.knockBack, ammoKnockback) * (rightShot ? 1.35f : 1f);
            int projectileType = ModContent.ProjectileType<AMRRound>();

            bool markerRound = AMRPlayer.ConsumeOnyxRoundType();
            Vector2 spawnPosition = GetSafeMuzzlePosition(direction);
            int shotIndex = Projectile.NewProjectile(
                Projectile.GetSource_FromThis(),
                spawnPosition,
                direction * speed,
                projectileType,
                Math.Max(1, damage),
                knockback,
                Projectile.owner,
                charge,
                markerRound ? 1f : 0f);

            if (!Main.projectile.IndexInRange(shotIndex))
                return false;

            Projectile shot = Main.projectile[shotIndex];
            int baseCrit = Owner.GetWeaponCrit(Owner.HeldItem);
            int bonusCrit = rightShot ? (int)(100f * charge) : 0;
            shot.CritChance = baseCrit + bonusCrit;
            shot.netUpdate = true;

            recoilOffset = rightShot ? 17f : 11f;
            muzzleFlashTimer = rightShot ? 10 : 7;
            useAnimationTimer = rightShot ? 12 : 8;
            Owner.velocity -= direction * (rightShot ? 0.75f : 0.42f);
            Owner.Calamity().GeneralScreenShakePower = rightShot ? 8f : 5f;
            SpawnMuzzleBurst(direction, rightShot);
            EjectSpentCasing(direction, rightShot);

            SoundStyle fireSound = CommonCalamitySounds.LargeWeaponFireSound with
            {
                Volume = rightShot ? 1f : 0.78f,
                Pitch = rightShot ? -0.16f : -0.05f,
                MaxInstances = 4
            };
            SoundEngine.PlaySound(fireSound, GunTipPosition);
            return true;
        }

        private void EjectSpentCasing(Vector2 direction, bool rightShot)
        {
            if (Main.netMode == NetmodeID.Server)
                return;

            Vector2 breech = Projectile.Center + direction * (HoldoutDistance + 6f);
            Vector2 ejectVelocity = (-direction * 1.7f + new Vector2(0f, -3.1f)).RotatedByRandom(0.22f) *
                Main.rand.NextFloat(0.85f, 1.2f) * (rightShot ? 1.25f : 1f);

            Gore.NewGore(Projectile.GetSource_FromThis(), breech, ejectVelocity,
                ModContent.Find<ModGore>("CalamityMod/OmniSniperCasing").Type);
        }

        private Vector2 GetSafeMuzzlePosition(Vector2 direction)
        {
            Vector2 muzzle = GunTipPosition + direction * 5f;
            return Collision.SolidCollision(muzzle, 2, 2) ? Owner.Center : muzzle;
        }

        private void SpawnAimScope()
        {
            int scopeType = ModContent.ProjectileType<AMRAimScope>();
            if (Owner.ownedProjectileCounts[scopeType] > 0)
                return;

            Projectile.NewProjectile(
                Projectile.GetSource_FromThis(),
                GunTipPosition,
                AimDirection,
                scopeType,
                0,
                0f,
                Projectile.owner,
                Projectile.identity);
        }

        private void UpdatePose()
        {
            Vector2 desiredAim = (NewLegendAntiMaterielRifle.GetMouseWorld(Owner) - Owner.MountedCenter)
                .SafeNormalize(Vector2.UnitX * Owner.direction);

            if (Projectile.owner == Main.myPlayer)
            {
                float turnSpeed = rightAiming ? 0.22f : 0.42f;
                Projectile.velocity = Vector2.Lerp(AimDirection, desiredAim, turnSpeed).SafeNormalize(desiredAim);
                Projectile.netUpdate = true;
            }

            Vector2 aim = AimDirection;
            int direction = aim.X >= 0f ? 1 : -1;
            Projectile.spriteDirection = direction;
            Projectile.direction = direction;
            Projectile.rotation = aim.ToRotation();
            Projectile.Center = Owner.MountedCenter + aim * (HoldoutDistance - recoilOffset) + new Vector2(0f, -10f * Owner.gravDir);

            Owner.ChangeDir(direction);
            Owner.heldProj = Projectile.whoAmI;
            Owner.itemRotation = (aim * direction).ToRotation();
            Owner.HeldItem.noUseGraphic = true;

            if (useAnimationTimer > 0 || rightAiming)
            {
                Owner.itemTime = Math.Max(Owner.itemTime, 2);
                Owner.itemAnimation = Math.Max(Owner.itemAnimation, 2);
            }

            float armRotation = (Projectile.rotation - MathHelper.PiOver2) * Owner.gravDir;
            if (Owner.gravDir == -1f)
                armRotation += MathHelper.Pi;

            Owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, armRotation);
            Owner.SetCompositeArmBack(true, Player.CompositeArmStretchAmount.ThreeQuarters,
                armRotation + MathHelper.ToRadians(9f) * direction);
        }

        private void SpawnMuzzleBurst(Vector2 direction, bool rightShot)
        {
            if (Main.dedServ)
                return;

            Color mainColor = rightShot ? new Color(233, 102, 238) : new Color(255, 202, 81);
            Color pressureColor = rightShot ? new Color(157, 174, 218) : new Color(136, 164, 194);
            float strength = rightShot ? 1.28f : 1f;
            int count = rightShot ? 14 : 9;

            GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(
                GunTipPosition,
                -direction * 0.35f,
                pressureColor,
                new Vector2(0.28f, 1.42f),
                direction.ToRotation(),
                0.05f,
                1.02f * strength,
                18));
            GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(
                GunTipPosition + direction * 3f,
                -direction * 0.16f,
                Color.Lerp(mainColor, Color.White, 0.58f),
                new Vector2(0.17f, 0.78f),
                direction.ToRotation(),
                0.02f,
                0.48f * strength,
                12));

            for (int i = -2; i <= 2; i++)
            {
                float angle = MathHelper.ToRadians(10f) * i;
                Vector2 velocity = -direction.RotatedBy(angle) * (1.6f + Math.Abs(i) * 0.38f) * strength;
                GeneralParticleHandler.SpawnParticle(new CritSpark(
                    GunTipPosition,
                    velocity,
                    Color.White,
                    mainColor,
                    0.38f + Math.Abs(i) * 0.035f,
                    13 + Math.Abs(i) * 2,
                    i * 0.04f,
                    2.55f));
            }

            for (int i = 0; i < 6; i++)
            {
                float spread = i / 5f - 0.5f;
                GeneralParticleHandler.SpawnParticle(new GlowOrbParticle(
                    GunTipPosition + direction * 4f,
                    direction.RotatedBy(MathHelper.ToRadians(32f) * spread) * MathHelper.Lerp(2.8f, 6.4f, 1f - MathF.Abs(spread * 2f)),
                    false,
                    13,
                    0.32f * strength,
                    Color.Lerp(mainColor, Color.White, 0.34f),
                    true,
                    false,
                    true));
            }

            GeneralParticleHandler.SpawnParticle(new GenericBloom(
                GunTipPosition,
                Vector2.Zero,
                Color.Lerp(mainColor, Color.White, 0.65f),
                0.78f * strength,
                11,
                false));

            for (int i = 0; i < count; i++)
            {
                Dust spark = Dust.NewDustPerfect(
                    GunTipPosition + Main.rand.NextVector2Circular(3f, 3f),
                    Main.rand.NextBool() ? DustID.GoldFlame : DustID.PurpleTorch,
                    direction.RotatedByRandom(rightShot ? 0.18f : 0.3f) * Main.rand.NextFloat(2f, rightShot ? 9f : 6f),
                    50,
                    mainColor,
                    Main.rand.NextFloat(0.8f, rightShot ? 1.45f : 1.15f));
                spark.noGravity = true;
            }

            for (int i = 0; i < 4; i++)
            {
                Dust smoke = Dust.NewDustPerfect(
                    GunTipPosition - direction * Main.rand.NextFloat(2f, 14f),
                    DustID.Smoke,
                    -direction.RotatedByRandom(0.55f) * Main.rand.NextFloat(0.5f, 2.4f),
                    150,
                    Color.Gray,
                    Main.rand.NextFloat(0.7f, 1.15f));
                smoke.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Type].Value;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            Vector2 origin = texture.Size() * 0.5f;
            SpriteEffects effects = Projectile.spriteDirection == -1 ? SpriteEffects.FlipVertically : SpriteEffects.None;

            if (rightAiming || muzzleFlashTimer > 0)
            {
                Color glowColor = rightAiming
                    ? Color.Lerp(new Color(86, 108, 168, 0), new Color(233, 102, 238, 0), ScopeChargeCompletion)
                    : new Color(255, 202, 81, 0);
                float power = rightAiming ? 0.12f + ScopeChargeCompletion * 0.28f : muzzleFlashTimer / 20f;
                for (int i = 0; i < 6; i++)
                {
                    Vector2 offset = (MathHelper.TwoPi * i / 6f).ToRotationVector2() * (1.5f + power * 4f);
                    Main.EntitySpriteDraw(texture, drawPosition + offset, null, glowColor * power,
                        Projectile.rotation, origin, Projectile.scale, effects, 0f);
                }
            }

            Main.EntitySpriteDraw(texture, drawPosition, null, Projectile.GetAlpha(lightColor),
                Projectile.rotation, origin, Projectile.scale, effects, 0f);
            DrawMuzzleGlow();
            return false;
        }

        private void DrawMuzzleGlow()
        {
            float flash = muzzleFlashTimer / 10f;
            float scopePower = rightAiming ? ScopeChargeCompletion * 0.55f : 0f;
            float power = Math.Max(flash, scopePower);
            if (power <= 0.01f)
                return;

            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Color color = rightAiming ? new Color(233, 102, 238, 0) : new Color(255, 202, 81, 0);

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, Main.DefaultSamplerState,
                DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
            Main.EntitySpriteDraw(bloom, GunTipPosition - Main.screenPosition, null, color * power,
                Projectile.rotation, bloom.Size() * 0.5f,
                new Vector2(0.12f + power * 0.3f, 0.06f + power * 0.12f), SpriteEffects.None, 0f);
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState,
                DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
        }
    }
}
