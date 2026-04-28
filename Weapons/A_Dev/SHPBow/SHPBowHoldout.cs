using CalamityMod;
using CalamityLegendsComeBack.Weapons.Visuals;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Dev.SHPBow
{
    internal sealed class SHPBowHoldout : ModProjectile, ILocalizedModType
    {
        private const float HoldoutLength = 24f;
        private const float MuzzleLength = 46f;
        private const int ReadyPulseFrames = 18;
        private const int NormalFireOutlineFrames = 16;
        private const int ChargedFireOutlineFrames = 32;

        private int leftFireTimer;
        private int chargeTimer;
        private int readyPulseTimer;
        private int rainbowOutlineTimer;
        private float recoilKick;
        private bool rightChargeActive;
        private bool readySoundPlayed;

        public override string Texture => SHPBow.TextureAssetPath;
        public new string LocalizationCategory => "Projectiles.A_Dev";

        private Player Owner => Main.player[Projectile.owner];
        private SHPBowPlayer BowPlayer => Owner.GetModPlayer<SHPBowPlayer>();
        private Vector2 AimDirection => Projectile.velocity.SafeNormalize(Vector2.UnitX * Owner.direction);
        private int CurrentChargeFrames => GetChargeFrames();
        private float ChargeCompletion => MathHelper.Clamp(chargeTimer / (float)CurrentChargeFrames, 0f, 1f);
        private bool FullyCharged => chargeTimer >= CurrentChargeFrames;

        private struct SequenceShot
        {
            public Vector2 Direction;
            public float SpeedMultiplier;
            public float DamageMultiplier;
            public float LateralOffset;

            public SequenceShot(Vector2 direction)
            {
                Direction = direction;
                SpeedMultiplier = 1f;
                DamageMultiplier = 1f;
                LateralOffset = 0f;
            }
        }

        public override void SetDefaults()
        {
            Projectile.width = 68;
            Projectile.height = 126;
            Projectile.friendly = false;
            Projectile.hostile = false;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.DamageType = DamageClass.Ranged;
        }

        public override bool ShouldUpdatePosition() => false;

        public override bool? CanDamage() => false;

        public override void AI()
        {
            if (!Owner.active || Owner.dead || Owner.noItems || Owner.CCed || Owner.HeldItem.type != ModContent.ItemType<SHPBow>())
            {
                Projectile.Kill();
                return;
            }

            Owner.Calamity().mouseWorldListener = true;
            if (Main.myPlayer == Owner.whoAmI)
                Owner.Calamity().rightClickListener = true;

            Owner.HeldItem.noUseGraphic = true;
            Projectile.damage = Owner.GetWeaponDamage(Owner.HeldItem);
            Projectile.knockBack = Owner.HeldItem.knockBack;

            BowPlayer.SetHoldingSHPBow();
            UpdateHeldProjectileVariables();

            if (Main.myPlayer == Projectile.owner)
                HandleOwnerInput();

            ManipulatePlayerVariables();
            UpdateTimersAndLighting();
        }

        private void HandleOwnerInput()
        {
            bool selectionPanelOpen = HasActiveSelectionPanel(Owner);
            BowPlayer.ProcessRightClickState(selectionPanelOpen);

            if (BowPlayer.ShortTapReleasedThisFrame && !rightChargeActive)
                ToggleSelectionPanel();

            if (BowPlayer.LongHoldReachedThisFrame && !rightChargeActive)
                BeginRightCharge();

            if (rightChargeActive)
            {
                UpdateRightChargeState();
                return;
            }

            BowPlayer.UpdateChargeBar(false, 0f);
            HandleLeftClickInput(selectionPanelOpen);
        }

        private void HandleLeftClickInput(bool selectionPanelOpen)
        {
            bool validLeftInput =
                Owner.HeldItem.type == ModContent.ItemType<SHPBow>() &&
                !Owner.noItems &&
                !Owner.CCed &&
                !selectionPanelOpen &&
                !BowPlayer.RightMouseHeld &&
                Main.mouseLeft &&
                !Main.mapFullscreen &&
                !Main.blockMouse &&
                !Owner.mouseInterface &&
                !(Main.playerInventory && Main.HoverItem.type == Owner.HeldItem.type);

            if (!validLeftInput)
            {
                leftFireTimer = 0;
                return;
            }

            if (leftFireTimer > 0)
            {
                leftFireTimer--;
                return;
            }

            FireSequenceShot(charged: false);
            leftFireTimer = GetPrimaryFireInterval();
        }

        private void BeginRightCharge()
        {
            CloseSelectionPanel();
            rightChargeActive = true;
            chargeTimer = 0;
            readyPulseTimer = 0;
            readySoundPlayed = false;
            leftFireTimer = 0;
            SoundEngine.PlaySound(SoundID.Item149 with { Volume = 0.55f, Pitch = -0.25f }, Projectile.Center);
        }

        private void UpdateRightChargeState()
        {
            if (BowPlayer.LongHoldReleasedThisFrame)
            {
                if (FullyCharged)
                    FireSequenceShot(charged: true);
                else
                    SoundEngine.PlaySound(SoundID.MenuClose with { Volume = 0.34f, Pitch = 0.2f }, Projectile.Center);

                CancelRightCharge();
                return;
            }

            chargeTimer = Math.Min(chargeTimer + 1, CurrentChargeFrames);
            BowPlayer.UpdateChargeBar(true, ChargeCompletion);

            if (FullyCharged && !readySoundPlayed)
            {
                readySoundPlayed = true;
                readyPulseTimer = ReadyPulseFrames;
                rainbowOutlineTimer = Math.Max(rainbowOutlineTimer, ReadyPulseFrames);
                SoundEngine.PlaySound(SoundID.Item29 with { Volume = 0.9f, Pitch = -0.18f }, Projectile.Center);
            }

            EmitChargeDust(GetMuzzlePosition(AimDirection));
        }

        private void CancelRightCharge()
        {
            rightChargeActive = false;
            chargeTimer = 0;
            readyPulseTimer = 0;
            readySoundPlayed = false;
            BowPlayer.UpdateChargeBar(false, 0f);
        }

        private void FireSequenceShot(bool charged)
        {
            if (!TryPickAmmoForSequence(charged, out float ammoSpeed, out int ammoDamage, out float knockback))
            {
                leftFireTimer = 6;
                return;
            }

            IEntitySource source = Projectile.GetSource_FromThis();
            int packedSequence = BowPlayer.PackedSequence;
            Vector2 aim = AimDirection;
            Vector2 muzzle = GetMuzzlePosition(aim);
            float speed = Math.Max(ammoSpeed, Owner.HeldItem.shootSpeed);
            List<SequenceShot> shots = BuildSequenceShots(aim, charged);
            float chargedDamageMultiplier = charged ? 1.55f + BowPlayer.SequenceLength * 0.22f : 1f;

            foreach (SequenceShot shot in shots)
            {
                Vector2 normal = shot.Direction.RotatedBy(MathHelper.PiOver2);
                Vector2 spawnPosition = muzzle + normal * shot.LateralOffset + Main.rand.NextVector2Circular(charged ? 1.8f : 0.8f, charged ? 1.8f : 0.8f);
                Vector2 velocity = shot.Direction * speed * shot.SpeedMultiplier;
                int damage = Math.Max(1, (int)(ammoDamage * shot.DamageMultiplier * chargedDamageMultiplier));

                SpawnArrow(
                    source,
                    spawnPosition,
                    velocity,
                    packedSequence,
                    damage,
                    charged ? knockback * 1.25f : knockback,
                    charged);
            }

            ApplyRecoil(charged ? 14f : 2.6f + shots.Count * 0.35f);
            rainbowOutlineTimer = charged ? ChargedFireOutlineFrames : NormalFireOutlineFrames;
            if (charged)
            {
                Owner.velocity -= aim * (4.4f + BowPlayer.SequenceLength * 0.5f);
                Owner.SetScreenshake(5.5f);
                SoundEngine.PlaySound(SoundID.Item92 with { Volume = 0.95f, Pitch = -0.18f }, Projectile.Center);
                SoundEngine.PlaySound(SoundID.Item72 with { Volume = 0.65f, Pitch = 0.05f }, Projectile.Center);
            }
            else
            {
                SoundEngine.PlaySound(SoundID.Item5 with { Volume = 0.54f + shots.Count * 0.025f, Pitch = Main.rand.NextFloat(-0.05f, 0.08f) }, Projectile.Center);
            }

            EmitMuzzleDust(muzzle, aim, packedSequence, charged);
        }

        private List<SequenceShot> BuildSequenceShots(Vector2 aim, bool charged)
        {
            List<SequenceShot> shots = new() { new SequenceShot(aim) };
            int scatterStep = 0;
            int maxShots = charged ? 11 : 7;

            for (int i = 0; i < BowPlayer.SequenceLength; i++)
            {
                SHPBowMode mode = BowPlayer.GetSequenceMode(i);

                switch (mode)
                {
                    case SHPBowMode.Pierce:
                        for (int s = 0; s < shots.Count; s++)
                        {
                            SequenceShot shot = shots[s];
                            shot.SpeedMultiplier *= charged ? 1.13f : 1.08f;
                            shot.DamageMultiplier *= charged ? 1.09f : 1.04f;
                            shots[s] = shot;
                        }
                        break;

                    case SHPBowMode.Ricochet:
                        for (int s = 0; s < shots.Count; s++)
                        {
                            SequenceShot shot = shots[s];
                            shot.SpeedMultiplier *= 1.03f;
                            shot.DamageMultiplier *= charged ? 1.04f : 1.02f;
                            shots[s] = shot;
                        }
                        break;

                    case SHPBowMode.Scatter:
                        scatterStep++;
                        shots = ApplyScatterTransform(shots, scatterStep, maxShots, charged);
                        break;

                    case SHPBowMode.Homing:
                        for (int s = 0; s < shots.Count; s++)
                        {
                            SequenceShot shot = shots[s];
                            shot.SpeedMultiplier *= charged ? 1.01f : 0.99f;
                            shot.DamageMultiplier *= charged ? 1.06f : 1.03f;
                            shots[s] = shot;
                        }
                        break;
                }
            }

            return shots;
        }

        private static List<SequenceShot> ApplyScatterTransform(List<SequenceShot> shots, int scatterStep, int maxShots, bool charged)
        {
            List<SequenceShot> transformed = new(maxShots);
            float baseAngle = 0.1f + scatterStep * 0.045f;

            for (int i = 0; i < shots.Count && transformed.Count < maxShots; i++)
            {
                SequenceShot baseShot = shots[i];
                baseShot.DamageMultiplier *= charged ? 0.93f : 0.88f;
                transformed.Add(baseShot);

                int side = (i + scatterStep) % 2 == 0 ? 1 : -1;
                AddScatterBranch(transformed, shots[i], side, baseAngle, scatterStep, charged, maxShots);

                if (charged)
                    AddScatterBranch(transformed, shots[i], -side, baseAngle * 1.25f, scatterStep, charged, maxShots);
            }

            return transformed;
        }

        private static void AddScatterBranch(List<SequenceShot> shots, SequenceShot source, int side, float angle, int scatterStep, bool charged, int maxShots)
        {
            if (shots.Count >= maxShots)
                return;

            SequenceShot branch = source;
            branch.Direction = branch.Direction.RotatedBy(angle * side).SafeNormalize(source.Direction);
            branch.SpeedMultiplier *= charged ? 0.98f : 0.94f;
            branch.DamageMultiplier *= charged ? 0.74f : 0.58f;
            branch.LateralOffset += side * (3.4f + scatterStep * 1.8f);
            shots.Add(branch);
        }

        private bool TryPickAmmoForSequence(bool chargedShot, out float speed, out int damage, out float knockback)
        {
            bool dontConsume = !chargedShot && BowPlayer.CountMode(SHPBowMode.Scatter) > 0 && Main.rand.NextBool(7);

            if (!Owner.PickAmmo(Owner.HeldItem, out _, out speed, out damage, out knockback, out _, dontConsume))
                return false;

            if (speed <= 0f)
                speed = Owner.HeldItem.shootSpeed;

            return true;
        }

        private void SpawnArrow(IEntitySource source, Vector2 position, Vector2 velocity, int packedSequence, int damage, float knockback, bool charged)
        {
            int projectileIndex = Projectile.NewProjectile(
                source,
                position,
                velocity,
                ModContent.ProjectileType<SHPBowArrow>(),
                damage,
                knockback,
                Owner.whoAmI,
                packedSequence,
                charged ? 1f : 0f);

            if (projectileIndex < 0 || projectileIndex >= Main.maxProjectiles)
                return;

            Projectile arrow = Main.projectile[projectileIndex];
            arrow.noDropItem = true;
            arrow.originalDamage = damage;
            arrow.netUpdate = true;
        }

        private void UpdateHeldProjectileVariables()
        {
            Vector2 armPosition = Owner.RotatedRelativePoint(Owner.MountedCenter, true);

            if (Main.myPlayer == Projectile.owner)
            {
                Vector2 desiredVelocity = (GetCurrentMouseWorld() - armPosition).SafeNormalize(Vector2.UnitX * Owner.direction);
                Projectile.velocity = Projectile.velocity == Vector2.Zero
                    ? desiredVelocity
                    : Vector2.Lerp(Projectile.velocity, desiredVelocity, rightChargeActive ? 0.22f : 0.38f);
                Projectile.netUpdate = true;
            }

            Vector2 aim = AimDirection;
            float holdLength = HoldoutLength + MathHelper.Lerp(0f, 8f, ChargeCompletion) - recoilKick;
            Projectile.Center = armPosition + aim * holdLength + new Vector2(0f, -3f * Owner.gravDir);
            Projectile.rotation = aim.ToRotation();
            Projectile.direction = aim.X >= 0f ? 1 : -1;
            Projectile.spriteDirection = Projectile.direction;
            Projectile.timeLeft = 2;
        }

        private void ManipulatePlayerVariables()
        {
            Owner.ChangeDir(Projectile.direction);
            Owner.heldProj = Projectile.whoAmI;
            Owner.itemRotation = (AimDirection * Projectile.direction).ToRotation();

            float pull = rightChargeActive ? ChargeCompletion : 0f;
            float armRotation = Projectile.rotation - MathHelper.PiOver2;
            Owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, armRotation + pull * 0.14f * Projectile.direction);
            Owner.SetCompositeArmBack(true, Player.CompositeArmStretchAmount.Full, armRotation - pull * 0.18f * Projectile.direction);
        }

        private void UpdateTimersAndLighting()
        {
            if (recoilKick > 0f)
                recoilKick = MathHelper.Lerp(recoilKick, 0f, 0.34f);

            if (readyPulseTimer > 0)
                readyPulseTimer--;

            if (rainbowOutlineTimer > 0)
                rainbowOutlineTimer--;

            Color color = SHPBowModeHelpers.SequenceColor(BowPlayer.PackedSequence, 0.65f);
            Lighting.AddLight(GetMuzzlePosition(AimDirection), color.ToVector3() * (0.2f + ChargeCompletion * 0.46f));
        }

        private void ApplyRecoil(float amount)
        {
            recoilKick = MathHelper.Clamp(recoilKick + amount, 0f, 18f);
        }

        private Vector2 GetMuzzlePosition(Vector2 aim)
        {
            Vector2 armPosition = Owner.RotatedRelativePoint(Owner.MountedCenter, true);
            Vector2 muzzle = armPosition + aim * MuzzleLength;

            if (Collision.CanHit(armPosition, 0, 0, muzzle, 0, 0))
                return muzzle;

            return armPosition + aim * 14f;
        }

        private Vector2 GetCurrentMouseWorld()
        {
            Vector2 mouseWorld = Owner.Calamity().mouseWorld;
            if (mouseWorld == Vector2.Zero)
                mouseWorld = Main.MouseWorld;

            return mouseWorld;
        }

        private int GetPrimaryFireInterval()
        {
            int interval = 9 + BowPlayer.SequenceLength * 2 + BowPlayer.CountMode(SHPBowMode.Scatter) * 2 + BowPlayer.CountMode(SHPBowMode.Homing);
            return Utils.Clamp(interval, 11, 28);
        }

        private int GetChargeFrames()
        {
            int frames = 42 + BowPlayer.SequenceLength * 9 + BowPlayer.CountMode(SHPBowMode.Scatter) * 5 + BowPlayer.CountMode(SHPBowMode.Homing) * 3;
            return Utils.Clamp(frames, 50, 96);
        }

        private void EmitMuzzleDust(Vector2 muzzle, Vector2 aim, int packedSequence, bool charged)
        {
            int length = SHPBowModeHelpers.SequenceLength(packedSequence);
            int count = charged ? 34 : 10 + length * 2;

            for (int i = 0; i < count; i++)
            {
                SHPBowMode mode = SHPBowModeHelpers.SequenceMode(packedSequence, i % length);
                Vector2 velocity = aim.RotatedByRandom(charged ? 0.76f : 0.32f) * Main.rand.NextFloat(charged ? 2.4f : 0.9f, charged ? 8.2f : 3.4f);
                Color color = Color.Lerp(SHPBowModeHelpers.MainColor(mode), SHPBowModeHelpers.AccentColor(mode), Main.rand.NextFloat());
                Dust dust = Dust.NewDustPerfect(
                    muzzle + Main.rand.NextVector2Circular(4f, 4f),
                    SHPBowModeHelpers.DustType(mode),
                    velocity,
                    100,
                    color,
                    Main.rand.NextFloat(charged ? 1.05f : 0.72f, charged ? 1.55f : 1.05f));
                dust.noGravity = true;
            }
        }

        private void EmitChargeDust(Vector2 muzzle)
        {
            if (!Main.rand.NextBool(FullyCharged ? 1 : 2))
                return;

            int packedSequence = BowPlayer.PackedSequence;
            int length = SHPBowModeHelpers.SequenceLength(packedSequence);
            SHPBowMode mode = SHPBowModeHelpers.SequenceMode(packedSequence, (chargeTimer / 5) % length);
            Color color = Color.Lerp(SHPBowModeHelpers.MainColor(mode), SHPBowModeHelpers.AccentColor(mode), ChargeCompletion);
            Vector2 pullDirection = -AimDirection;
            Dust dust = Dust.NewDustPerfect(
                muzzle + Main.rand.NextVector2Circular(8f, 8f),
                SHPBowModeHelpers.DustType(mode),
                pullDirection.RotatedByRandom(0.55f) * Main.rand.NextFloat(1.2f, 4f),
                100,
                color,
                Main.rand.NextFloat(0.8f, 1.25f) * (0.65f + ChargeCompletion));
            dust.noGravity = true;
        }

        private static bool HasActiveSelectionPanel(Player player) => FindOpenSelectionPanel(player) != null;

        private static Projectile FindOpenSelectionPanel(Player player)
        {
            int panelType = ModContent.ProjectileType<SHPBowSelectionPanel>();
            foreach (Projectile projectile in Main.ActiveProjectiles)
            {
                if (!projectile.active || projectile.owner != player.whoAmI || projectile.type != panelType)
                    continue;

                if (projectile.ai[0] == 1f || projectile.Opacity <= 0.02f)
                    continue;

                return projectile;
            }

            return null;
        }

        private void ToggleSelectionPanel()
        {
            Projectile openPanel = FindOpenSelectionPanel(Owner);
            if (openPanel != null)
            {
                openPanel.ai[0] = 1f;
                openPanel.netUpdate = true;
                SoundEngine.PlaySound(SoundID.MenuClose with { Volume = 0.5f }, Owner.Center);
                return;
            }

            Projectile.NewProjectile(
                Projectile.GetSource_FromThis(),
                Owner.Center,
                Vector2.Zero,
                ModContent.ProjectileType<SHPBowSelectionPanel>(),
                0,
                0f,
                Owner.whoAmI);

            SoundEngine.PlaySound(SoundID.MenuOpen with { Volume = 0.58f, Pitch = 0.08f }, Owner.Center);
        }

        private void CloseSelectionPanel()
        {
            int panelType = ModContent.ProjectileType<SHPBowSelectionPanel>();
            foreach (Projectile projectile in Main.ActiveProjectiles)
            {
                if (!projectile.active || projectile.owner != Owner.whoAmI || projectile.type != panelType)
                    continue;

                projectile.ai[0] = 1f;
                projectile.netUpdate = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Type].Value;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            Vector2 origin = texture.Size() * 0.5f;
            float drawRotation = Projectile.rotation;
            SpriteEffects effects = SpriteEffects.None;

            if (Math.Cos(drawRotation) < 0.0)
            {
                drawRotation += MathHelper.Pi;
                effects = SpriteEffects.FlipHorizontally;
            }

            int packedSequence = BowPlayer.PackedSequence;
            Color baseColor = SHPBowModeHelpers.SequenceColor(packedSequence, 0.2f);
            Color tipColor = SHPBowModeHelpers.SequenceColor(packedSequence, 1f);
            float pulse = 0.76f + 0.24f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 7f + Projectile.identity);
            float chargeGlow = rightChargeActive ? ChargeCompletion : 0f;
            float readyPulse = readyPulseTimer / (float)ReadyPulseFrames;
            float outlinePulse = rainbowOutlineTimer / (float)(rightChargeActive || rainbowOutlineTimer > NormalFireOutlineFrames ? ChargedFireOutlineFrames : NormalFireOutlineFrames);
            float rainbowOpacity = MathHelper.Clamp(0.52f + BowPlayer.SequenceLength * 0.07f + chargeGlow * 0.55f + readyPulse * 0.72f + outlinePulse * 1.05f, 0f, 1.65f);
            float rainbowRadius = 4.2f + chargeGlow * 6.2f + readyPulse * 5.2f + outlinePulse * 7.4f;

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(
                SpriteSortMode.Deferred,
                BlendState.Additive,
                SamplerState.PointClamp,
                DepthStencilState.None,
                Main.Rasterizer,
                null,
                Main.GameViewMatrix.TransformationMatrix);

            HoldoutOutlineHelper.DrawStarmadaRainbowOutline(
                texture,
                drawPosition,
                drawRotation,
                origin,
                Vector2.One * Projectile.scale * (1f + outlinePulse * 0.04f),
                effects,
                rainbowRadius,
                rainbowOpacity,
                Main.GlobalTimeWrappedHourly + Projectile.identity * 0.13f,
                24 + (int)(chargeGlow * 10f + outlinePulse * 10f),
                manageBlendState: false);

            for (int i = 0; i < 12; i++)
            {
                float completion = i / 11f;
                float angle = MathHelper.TwoPi * completion + Main.GlobalTimeWrappedHourly * (1.45f + chargeGlow);
                Vector2 offset = angle.ToRotationVector2() * (1.4f + chargeGlow * 4.6f + readyPulse * 5f) * pulse;
                Color glowColor = Color.Lerp(baseColor, tipColor, completion);
                glowColor.A = 0;
                Main.EntitySpriteDraw(
                    texture,
                    drawPosition + offset,
                    null,
                    glowColor * (0.2f + chargeGlow * 0.34f + readyPulse * 0.42f + outlinePulse * 0.26f),
                    drawRotation,
                    origin,
                    Projectile.scale,
                    effects,
                    0);
            }

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(
                SpriteSortMode.Deferred,
                BlendState.AlphaBlend,
                SamplerState.PointClamp,
                DepthStencilState.None,
                Main.Rasterizer,
                null,
                Main.GameViewMatrix.TransformationMatrix);

            DrawBowString(drawRotation);
            DrawSequenceBeads(drawRotation);

            Main.EntitySpriteDraw(
                texture,
                drawPosition,
                null,
                Projectile.GetAlpha(lightColor),
                drawRotation,
                origin,
                Projectile.scale,
                effects,
                0);

            return false;
        }

        private void DrawBowString(float drawRotation)
        {
            Vector2 bowCenter = Projectile.Center - Main.screenPosition;
            Vector2 top = bowCenter + new Vector2(-21f, -49f).RotatedBy(drawRotation);
            Vector2 bottom = bowCenter + new Vector2(-21f, 49f).RotatedBy(drawRotation);
            Vector2 pull = bowCenter - AimDirection * MathHelper.Lerp(8f, 30f, rightChargeActive ? ChargeCompletion : 0f);
            Color stringColor = Color.Lerp(SHPBowModeHelpers.SequenceColor(BowPlayer.PackedSequence, 0f), SHPBowModeHelpers.SequenceColor(BowPlayer.PackedSequence, 1f), 0.5f) * (0.75f + ChargeCompletion * 0.25f);

            DrawLine(Main.spriteBatch, top, pull, stringColor, 2f);
            DrawLine(Main.spriteBatch, bottom, pull, stringColor, 2f);
        }

        private void DrawSequenceBeads(float drawRotation)
        {
            Vector2 direction = AimDirection;
            Vector2 normal = direction.RotatedBy(MathHelper.PiOver2);
            float pullDistance = MathHelper.Lerp(9f, 28f, rightChargeActive ? ChargeCompletion : 0f);

            for (int i = 0; i < BowPlayer.SequenceLength; i++)
            {
                SHPBowMode mode = BowPlayer.GetSequenceMode(i);
                Texture2D icon = ModContent.Request<Texture2D>(SHPBowModeHelpers.IconTexturePath(mode)).Value;
                float offsetAlongString = -pullDistance + i * 8.5f;
                Vector2 beadPosition = Projectile.Center - direction * (13f + offsetAlongString) + normal * (i - (BowPlayer.SequenceLength - 1f) * 0.5f) * 3.2f - Main.screenPosition;
                float beadPulse = 0.94f + 0.06f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 8f + i);

                Main.EntitySpriteDraw(
                    icon,
                    beadPosition,
                    null,
                    Color.Lerp(SHPBowModeHelpers.MainColor(mode), SHPBowModeHelpers.AccentColor(mode), 0.35f) * (0.58f + ChargeCompletion * 0.35f),
                    -drawRotation * 0.28f,
                    icon.Size() * 0.5f,
                    (0.24f + ChargeCompletion * 0.08f) * beadPulse,
                    SpriteEffects.None,
                    0);
            }
        }

        private static void DrawLine(SpriteBatch spriteBatch, Vector2 start, Vector2 end, Color color, float width)
        {
            Texture2D pixel = TextureAssets.MagicPixel.Value;
            Vector2 edge = end - start;
            if (edge.LengthSquared() <= 0.01f)
                return;

            spriteBatch.Draw(
                pixel,
                start,
                new Rectangle(0, 0, 1, 1),
                color,
                edge.ToRotation(),
                Vector2.Zero,
                new Vector2(edge.Length(), width),
                SpriteEffects.None,
                0f);
        }
    }
}
