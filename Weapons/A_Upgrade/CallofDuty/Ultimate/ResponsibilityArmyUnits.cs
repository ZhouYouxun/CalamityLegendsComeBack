using System;
using System.IO;
using CalamityMod;
using CalamityMod.Items.Accessories;
using CalamityMod.Projectiles.Ranged;
using CalamityMod.Projectiles.Summon;
using CalamityMod.World;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Upgrade.CallofDuty.Ultimate
{
    internal sealed class ResponsibilityArmyAmplifier : ResponsibilityArmyUnitBase
    {
        protected override string SourceTexture => "CalamityMod/NPCs/NormalNPCs/WulfrumAmplifier";
        protected override int FrameCount => 6;
        protected override int Width => 44;
        protected override int Height => 44;
        protected override float LifeMultiplier => 1.1f;
        protected override int ChassisDefense => 16;
        protected override float DamageCapRatio => 0.2f;
        protected override bool Flying => false;
        protected override int NativeHitDustCount => 3;
        protected override int NativeDeathDustCount => 15;
        protected override string[] NativeGoreNames => new[] { "WulfrumAmplifierGore" };

        private Vector2 lockedCenter;
        private bool deployedSquad;
        private bool landed;
        private int landedTimer;
        private ref float VisualChargeRadius => ref NPC.ai[1];

        public bool FieldOnline => Initialized && landed && landedTimer >= 30 && !Dismissing;

        public override void SetDefaults()
        {
            base.SetDefaults();
            if (Main.zenithWorld)
                NPC.scale = 2f;
        }

        internal static bool SpawnFor(Player player, int generation, int snapshotDamage)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return false;

            // The Amplifier is dropped in at the caller and falls under its own weight instead of
            // being teleported onto whatever ground happens to be underneath the player.
            Vector2 spawnPosition = player.Center - Vector2.UnitY * 24f;
            IEntitySource source = player.GetSource_Misc("CallofDutyUltimate");
            int index = NPC.NewNPC(source, (int)spawnPosition.X, (int)spawnPosition.Y, ModContent.NPCType<ResponsibilityArmyAmplifier>());
            if (!Main.npc.IndexInRange(index) || Main.npc[index].ModNPC is not ResponsibilityArmyAmplifier amplifier)
                return false;

            amplifier.InitializeFromOwner(player, generation, 0, snapshotDamage, CallofDutyPlayer.UltimateDurationFrames);
            Main.npc[index].netUpdate = true;
            return true;
        }

        protected override void UpdateUnitAI()
        {
            if (!landed)
            {
                UpdateFreeFall();
                return;
            }

            NPC.Center = lockedCenter;
            NPC.velocity = Vector2.Zero;
            NPC.direction = 1;
            landedTimer++;

            if (landedTimer == 30)
            {
                EmitDeploymentShockwave();
                if (Main.netMode != NetmodeID.MultiplayerClient)
                    DeploySquad();
            }

            if (FieldOnline)
            {
                VisualChargeRadius = (int)MathHelper.Lerp(VisualChargeRadius, AmplifierRadius, 0.1f);
                if (!Main.dedServ && Main.rand.NextBool(4))
                {
                    float dustCount = MathHelper.TwoPi * VisualChargeRadius / 8f;
                    for (int i = 0; i < dustCount; i++)
                    {
                        float angle = MathHelper.TwoPi * i / dustCount;
                        Dust dust = Dust.NewDustPerfect(NPC.Center, DustID.Vortex);
                        dust.position = NPC.Center + angle.ToRotationVector2() * VisualChargeRadius;
                        dust.scale = 0.7f;
                        dust.noGravity = true;
                        dust.velocity = NPC.velocity;
                    }
                }
            }
        }

        /// <summary>
        /// The chassis drops from the caller and only anchors itself once it touches solid ground,
        /// so it can never end up hovering in mid air over a pit or a platform gap.
        /// </summary>
        private void UpdateFreeFall()
        {
            NPC.direction = 1;
            NPC.spriteDirection = -1;

            // Landing is read from the previous movement step: tile collision zeroes velocity.Y and
            // leaves the pre-collision speed in oldVelocity.
            bool touchedGround = NPC.collideY && NPC.oldVelocity.Y > 0.1f;
            bool buriedInTiles = Collision.SolidCollision(NPC.position, NPC.width, NPC.height);
            bool droppedTooLong = Age >= 300;

            if (touchedGround || buriedInTiles || droppedTooLong)
            {
                Land();
                return;
            }

            NPC.velocity.X *= 0.9f;
            NPC.velocity.Y = Math.Min(NPC.velocity.Y + 0.45f, 18f);
            NPC.StepUpBlocks();

            if (!Main.dedServ && Age % 3 == 0)
                EmitSmoke(2);
        }

        private void Land()
        {
            landed = true;
            landedTimer = 0;
            NPC.velocity = Vector2.Zero;
            lockedCenter = NPC.Center;
            NPC.netUpdate = true;

            if (Main.dedServ)
                return;

            SoundEngine.PlaySound(SoundID.Item14 with { Volume = 0.4f, Pitch = -0.45f }, NPC.Center);
            for (int i = 0; i < 24; i++)
            {
                Vector2 velocity = new Vector2(Main.rand.NextFloat(-4.5f, 4.5f), Main.rand.NextFloat(-3.5f, -0.5f));
                Dust dust = Dust.NewDustPerfect(NPC.Bottom + Main.rand.NextVector2Circular(NPC.width * 0.4f, 4f),
                    i % 4 == 0 ? DustID.Electric : DustID.Smoke, velocity, 110, new Color(75, 210, 255), Main.rand.NextFloat(0.8f, 1.3f));
                dust.noGravity = true;
            }
        }

        private void EmitDeploymentShockwave()
        {
            if (!Main.dedServ)
            {
                SoundEngine.PlaySound(SoundID.Item92 with { Volume = 0.75f, Pitch = -0.2f }, NPC.Center);
                for (int i = 0; i < 96; i++)
                {
                    Vector2 velocity = (MathHelper.TwoPi * i / 96f).ToRotationVector2() * Main.rand.NextFloat(5f, 11f);
                    Dust dust = Dust.NewDustPerfect(NPC.Center, i % 3 == 0 ? DustID.Vortex : DustID.Electric, velocity, 90, new Color(75, 210, 255), Main.rand.NextFloat(0.8f, 1.35f));
                    dust.noGravity = true;
                }
            }
        }

        private void DeploySquad()
        {
            if (deployedSquad || Owner == null)
                return;
            deployedSquad = true;

            int remaining = Math.Max(1, RemainingFrames);
            for (int i = 0; i < 6; i++)
            {
                float side = i % 2 == 0 ? -1f : 1f;
                int rank = i / 2;
                float x = NPC.Center.X + side * (95f + rank * 78f);
                SpawnUnit<ResponsibilityArmyRover>(ResponsibilityArmyGround.FindGround(new Vector2(x, NPC.Bottom.Y - 20f), 40, 40), i, remaining);
            }
            for (int i = 0; i < 4; i++)
            {
                float side = i % 2 == 0 ? -1f : 1f;
                int rank = i / 2;
                float x = NPC.Center.X + side * (335f + rank * 76f);
                SpawnUnit<ResponsibilityArmyGyrator>(ResponsibilityArmyGround.FindGround(new Vector2(x, NPC.Bottom.Y - 42f), 40, 40), i, remaining);
            }
            for (int i = 0; i < 4; i++)
            {
                Vector2 position = NPC.Center + new Vector2((i - 1.5f) * 105f, -125f - Math.Abs(i - 1.5f) * 25f);
                SpawnUnit<ResponsibilityArmyDrone>(position, i, remaining);
            }
            for (int i = 0; i < 2; i++)
            {
                Vector2 position = NPC.Center + new Vector2((i == 0 ? -1f : 1f) * 220f, -72f);
                SpawnUnit<ResponsibilityArmyHovercraft>(position, i, remaining);
            }
        }

        private void SpawnUnit<T>(Vector2 position, int slotIndex, int remaining) where T : ResponsibilityArmyUnitBase
        {
            int index = NPC.NewNPC(NPC.GetSource_FromAI(), (int)position.X, (int)position.Y, ModContent.NPCType<T>());
            if (!Main.npc.IndexInRange(index) || Main.npc[index].ModNPC is not T unit)
                return;
            unit.InitializeFromOwner(Owner, Generation, slotIndex, SnapshotDamage, remaining);
            Main.npc[index].netUpdate = true;
        }

        public override void FindFrame(int frameHeight)
        {
            NPC.frameCounter++;
            NPC.frame.Y = (int)(NPC.frameCounter / 8) % FrameCount * frameHeight;
        }

        protected override void WriteUnitExtraAI(BinaryWriter writer)
        {
            writer.WriteVector2(lockedCenter);
            writer.Write(deployedSquad);
            writer.Write(landed);
            writer.Write(landedTimer);
        }

        protected override void ReadUnitExtraAI(BinaryReader reader)
        {
            lockedCenter = reader.ReadVector2();
            deployedSquad = reader.ReadBoolean();
            landed = reader.ReadBoolean();
            landedTimer = reader.ReadInt32();
        }
    }

    internal sealed class ResponsibilityArmyRover : ResponsibilityArmyUnitBase
    {
        protected override string SourceTexture => "CalamityMod/NPCs/NormalNPCs/WulfrumRover";
        protected override int FrameCount => 16;
        protected override int Width => 40;
        protected override int Height => 40;
        protected override float LifeMultiplier => 1.25f;
        protected override int ChassisDefense => 22;
        protected override float DamageCapRatio => 0.25f;
        protected override bool Flying => false;
        protected override int NativeHitDustCount => 4;
        protected override int NativeDeathDustCount => 20;
        protected override string[] NativeGoreNames => new[] { "WulfrumRoverGore1", "WulfrumRoverGore2" };

        private int ramCooldown;
        private int ramRunTimer;
        private int cannonTimer;

        public override void SetDefaults()
        {
            base.SetDefaults();
            if (Main.zenithWorld)
                NPC.scale = 2f;
        }

        protected override void UpdateUnitAI()
        {
            if (ramCooldown > 0)
                ramCooldown--;

            NPC target = AcquireTarget();
            if (target == null)
            {
                float side = SlotIndex % 2 == 0 ? -1f : 1f;
                int rank = SlotIndex / 2;
                Vector2 anchor = GetIdleAnchor(new Vector2(side * (110f + rank * 78f), 24f));
                MoveGroundTowards(anchor, 6f, -4f, 0.0125f);
                cannonTimer = Math.Max(0, cannonTimer - 2);
                return;
            }

            float combatSide = SlotIndex % 2 == 0 ? -1f : 1f;
            int combatRank = SlotIndex / 2;
            if (ramRunTimer > 0)
                ramRunTimer--;
            else if (ramCooldown <= 0 && IsRamLaunchWindow())
                ramRunTimer = 24;

            // The Rovers may occupy the same space physically, but only the current time-slot
            // holder gets to break formation and ram. The rest wait in distinct approach lanes.
            bool charging = ramRunTimer > 0;
            Vector2 ramPoint = charging
                ? target.Center
                : target.Center + new Vector2(combatSide * (96f + combatRank * 42f), 12f);
            MoveGroundTowards(ramPoint, charging ? (Amplified ? 17f : 14f) : 10f, Amplified ? -8f : -7f, charging ? 0.22f : 0.14f);

            if (charging && NPC.Hitbox.Intersects(target.Hitbox) && ramCooldown <= 0)
            {
                SpawnRamHitbox(target, 0.42f, 54);
                ramCooldown = 30;
                ramRunTimer = 0;
                NPC.velocity.X = -NPC.direction * 5f;
            }

            UpdateCannon(target);
        }

        private void MoveGroundTowards(Vector2 destination, float maximumSpeed, float jumpSpeed, float responsiveness)
        {
            int direction = Math.Sign(destination.X - NPC.Center.X);
            if (direction != 0)
                NPC.direction = direction;
            NPC.spriteDirection = -NPC.direction;
            NPC.velocity.X = MathHelper.Lerp(NPC.velocity.X, direction * maximumSpeed, responsiveness);

            bool destinationAbove = destination.Y < NPC.Center.Y - 48f;
            if ((NPC.collideX || destinationAbove) && NPC.collideY && Math.Abs(NPC.velocity.Y) < 0.2f)
            {
                NPC.velocity.Y = jumpSpeed;
                NPC.netUpdate = true;
            }
            NPC.StepUpBlocks();
        }

        private void UpdateCannon(NPC target)
        {
            cannonTimer++;
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            if (cannonTimer == 1)
            {
                Vector2 direction = NPC.SafeDirectionTo(target.Center);
                for (int i = 0; i < 6; i++)
                {
                    Vector2 velocity = direction.RotatedByRandom(0.24f) * Main.rand.NextFloat(8.5f, 11.5f);
                    Projectile.NewProjectile(NPC.GetSource_FromAI(), NPC.Center + direction * 18f, velocity,
                        ModContent.ProjectileType<ResponsibilityRoverScrap>(), GetAttackDamage(0.1f), 1.4f, OwnerIndex);
                }
                SoundEngine.PlaySound(CalamityMod.Items.Weapons.Ranged.WulfrumBlunderbuss.ShootSound, NPC.Center);
            }
            else if (cannonTimer == 46 || cannonTimer == 50 || cannonTimer == 54)
            {
                Vector2 velocity = NPC.SafeDirectionTo(target.Center) * 9.5f;
                Projectile.NewProjectile(NPC.GetSource_FromAI(), NPC.Center + velocity.SafeNormalize(Vector2.UnitX) * 18f, velocity,
                    ModContent.ProjectileType<ResponsibilityRoverFusionBolt>(), GetAttackDamage(0.16f), 1.2f, OwnerIndex,
                    velocity.ToRotation(), target.whoAmI);
            }

            if (cannonTimer >= 129)
                cannonTimer = 0;
        }

        protected override int ModifyInterceptedProjectileDamage(Projectile projectile, int damage)
        {
            Vector2 front = Vector2.UnitX * (NPC.direction == 0 ? 1 : NPC.direction);
            Vector2 incomingPosition = NPC.SafeDirectionTo(projectile.Center);
            if (Vector2.Dot(front, incomingPosition) >= 0.5f)
                damage = Math.Max(1, (int)MathF.Ceiling(damage * 0.5f));
            return damage;
        }

        public override void FindFrame(int frameHeight)
        {
            NPC.frameCounter++;
            int half = FrameCount / 2;
            int frame = (int)(NPC.frameCounter / 5) % half;
            if (Amplified)
                frame += half;
            NPC.frame.Y = frame * frameHeight;
        }

        protected override void DrawUnitExtras(SpriteBatch spriteBatch, Vector2 screenPosition, Color drawColor)
        {
            if (!Amplified || Dismissing)
                return;

            float scale = (Main.zenithWorld ? 0.2f : 0.15f) + 0.03f * (0.5f + 0.5f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 0.5f + NPC.whoAmI * 0.2f));
            const float noiseScale = 0.6f;

            Effect shieldEffect = Terraria.Graphics.Effects.Filters.Scene["CalamityMod:RoverDriveShield"].GetShader().Shader;
            shieldEffect.Parameters["time"].SetValue(Main.GlobalTimeWrappedHourly * 0.24f);
            shieldEffect.Parameters["blowUpPower"].SetValue(2.5f);
            shieldEffect.Parameters["blowUpSize"].SetValue(0.5f);
            shieldEffect.Parameters["noiseScale"].SetValue(noiseScale);

            float baseShieldOpacity = 0.9f + 0.1f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 2f);
            shieldEffect.Parameters["shieldOpacity"].SetValue(baseShieldOpacity);
            shieldEffect.Parameters["shieldEdgeBlendStrenght"].SetValue(4f);

            Color blueTint = new(51, 102, 255);
            Color cyanTint = new(71, 202, 255);
            Color wulfGreen = new Color(194, 255, 67) * 0.8f;
            Color edgeColor = CalamityUtils.MulticolorLerp(Main.GlobalTimeWrappedHourly * 0.2f, blueTint, cyanTint, wulfGreen);
            shieldEffect.Parameters["shieldColor"].SetValue(blueTint.ToVector3());
            shieldEffect.Parameters["shieldEdgeColor"].SetValue(edgeColor.ToVector3());

            spriteBatch.End();
            spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, shieldEffect, Main.GameViewMatrix.TransformationMatrix);

            RoverDrive.NoiseTex ??= ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/GreyscaleGradients/TechyNoise");
            Texture2D texture = RoverDrive.NoiseTex.Value;
            Vector2 position = NPC.Center + NPC.gfxOffY * Vector2.UnitY - screenPosition;
            spriteBatch.Draw(texture, position, null, Color.White, 0f, texture.Size() / 2f, scale, SpriteEffects.None, 0f);

            spriteBatch.End();
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.Transform);
        }
    }

    internal sealed class ResponsibilityArmyGyrator : ResponsibilityArmyUnitBase
    {
        protected override string SourceTexture => "CalamityMod/NPCs/NormalNPCs/WulfrumGyrator";
        protected override int FrameCount => 10;
        protected override int Width => 40;
        protected override int Height => 40;
        protected override float LifeMultiplier => 0.85f;
        protected override int ChassisDefense => 12;
        protected override float DamageCapRatio => 0.28f;
        protected override bool Flying => false;
        protected override int NativeHitDustCount => 5;
        protected override int NativeDeathDustCount => 20;
        protected override string[] NativeGoreNames => new[] { "WulfrumGyratorGore" };

        private int warmupTimer;
        private int chargeTimer;
        private int ramCooldown;
        private int stuckTimer;
        private int framesSinceImpact = 9999;
        private Vector2 lastPosition;

        protected override void UpdateUnitAI()
        {
            if (ramCooldown > 0)
                ramCooldown--;
            if (framesSinceImpact < 9999)
                framesSinceImpact++;

            UpdateStuckRecovery();

            NPC target = AcquireTarget();
            if (target == null)
            {
                warmupTimer = 0;
                chargeTimer = 0;
                float side = SlotIndex % 2 == 0 ? -1f : 1f;
                int rank = SlotIndex / 2;
                Vector2 anchor = GetIdleAnchor(new Vector2(side * (335f + rank * 76f), 30f));
                MoveTowards(anchor, Amplified ? 12f : 10f, Amplified ? -15f : -12f);
                return;
            }

            if (chargeTimer <= 0)
            {
                float side = SlotIndex % 2 == 0 ? -1f : 1f;
                int rank = SlotIndex / 2;
                Vector2 stagingAnchor = target.Center + new Vector2(side * (120f + rank * 48f), 24f);
                MoveTowards(stagingAnchor, Amplified ? 12f : 10f, Amplified ? -15f : -12f);
                if (ramCooldown <= 0 && IsRamLaunchWindow())
                    warmupTimer++;
                else
                    warmupTimer = 0;

                if (warmupTimer >= 5)
                {
                    NPC.direction = Math.Sign(target.Center.X - NPC.Center.X);
                    if (NPC.direction == 0)
                        NPC.direction = Main.rand.NextBool() ? 1 : -1;
                    chargeTimer = 84;
                    warmupTimer = 0;
                    NPC.velocity.X = NPC.direction * (Amplified ? 20f : 16f);
                    NPC.netUpdate = true;
                }
            }
            else
            {
                chargeTimer--;
                int targetDirection = Math.Sign(target.Center.X - NPC.Center.X);
                if (targetDirection != 0)
                    NPC.direction = targetDirection;

                float speed = Amplified ? 20f : 16f;
                NPC.velocity.X = MathHelper.Lerp(NPC.velocity.X, NPC.direction * speed, 0.24f);
                bool targetAbove = target.Center.Y < NPC.Center.Y - 48f;
                if ((NPC.collideX || targetAbove) && NPC.collideY && Math.Abs(NPC.velocity.Y) < 0.2f)
                    NPC.velocity.Y = Amplified ? -15f : -12f;

                if (NPC.Hitbox.Intersects(target.Hitbox) && ramCooldown <= 0)
                {
                    SpawnRamHitbox(target, 0.48f, 48);
                    framesSinceImpact = 0;
                    ramCooldown = 36;
                    chargeTimer = 0;
                    NPC.velocity.X = -NPC.direction * 7f;
                    NPC.velocity.Y = -5f;
                }
            }
            NPC.StepUpBlocks();
        }

        /// <summary>
        /// A Gyrator spawned into a hillside can end up buried in solid tiles with nowhere to roll,
        /// which parks it there for the whole Ultimate. If it is pinned and is not landing hits, it
        /// gets recalled to the owner. A Gyrator that keeps connecting is doing its job wherever it
        /// happens to be wedged, so it is left alone.
        /// </summary>
        private void UpdateStuckRecovery()
        {
            bool buried = Collision.SolidCollision(NPC.position, NPC.width, NPC.height);
            bool pinned = Vector2.DistanceSquared(NPC.Center, lastPosition) < 4f;
            lastPosition = NPC.Center;

            // Still connecting with something: whatever it is wedged in, it is still a threat.
            if (framesSinceImpact <= 120)
            {
                stuckTimer = 0;
                return;
            }

            if (buried || pinned)
                stuckTimer++;
            else
                stuckTimer = 0;

            if (stuckTimer < 60)
                return;

            stuckTimer = 0;
            TeleportNearOwner();
        }

        private void TeleportNearOwner()
        {
            if (Owner == null || Main.netMode == NetmodeID.MultiplayerClient)
                return;

            float side = SlotIndex % 2 == 0 ? -1f : 1f;
            Vector2 destination = ResponsibilityArmyGround.FindGround(Owner.Bottom + new Vector2(side * (60f + SlotIndex * 24f), 0f), Width, Height);

            // Never trade one burial for another.
            if (Collision.SolidCollision(destination - new Vector2(Width, Height) * 0.5f, Width, Height))
                destination = Owner.Center - Vector2.UnitY * 24f;

            EmitSmoke(12);
            NPC.Center = destination;
            NPC.velocity = Vector2.Zero;
            chargeTimer = 0;
            warmupTimer = 0;
            lastPosition = NPC.Center;
            EmitSmoke(12);
            NPC.netUpdate = true;

            if (!Main.dedServ)
                SoundEngine.PlaySound(SoundID.Item8 with { Volume = 0.4f, Pitch = 0.3f }, NPC.Center);
        }

        private void MoveTowards(Vector2 destination, float speed, float jumpSpeed)
        {
            int direction = Math.Sign(destination.X - NPC.Center.X);
            if (direction != 0)
                NPC.direction = direction;
            NPC.velocity.X = MathHelper.Lerp(NPC.velocity.X, direction * speed, 0.18f);
            if ((NPC.collideX || destination.Y < NPC.Center.Y - 60f) && NPC.collideY && Math.Abs(NPC.velocity.Y) < 0.2f)
                NPC.velocity.Y = jumpSpeed;
            NPC.StepUpBlocks();
        }

        public override void FindFrame(int frameHeight)
        {
            NPC.frameCounter++;
            if (Main.zenithWorld)
                NPC.frameCounter++;

            int half = FrameCount / 2;
            int frame = (int)(NPC.frameCounter / 5) % half;
            if (Amplified)
                frame += half;
            NPC.frame.Y = frame * frameHeight;
        }
    }

    internal sealed class ResponsibilityArmyDrone : ResponsibilityArmyUnitBase
    {
        protected override string SourceTexture => "CalamityMod/NPCs/NormalNPCs/WulfrumDrone";
        protected override int FrameCount => 6;
        protected override int Width => 32;
        protected override int Height => 32;
        protected override float LifeMultiplier => 0.55f;
        protected override int ChassisDefense => 6;
        protected override float DamageCapRatio => 0.33f;
        protected override bool Flying => true;
        protected override int NativeHitDustCount => 3;
        protected override int NativeDeathDustCount => 15;
        protected override string[] NativeGoreNames => new[] { "WulfrumDroneGore1", "WulfrumDroneGore2" };

        protected override void UpdateUnitAI()
        {
            NPC target = AcquireTarget();
            Vector2 destination;
            if (target == null)
            {
                destination = GetIdleAnchor(new Vector2((SlotIndex - 1.5f) * 68f, -105f - MathF.Sin((Age + SlotIndex * 17) * 0.05f) * 10f));
            }
            else
            {
                Vector2 away = target.SafeDirectionTo(NPC.Center);
                if (away == Vector2.Zero)
                    away = Vector2.UnitX;
                float desiredDistance = 560f + (SlotIndex % 2 == 0 ? -55f : 55f);
                destination = target.Center + away * desiredDistance + new Vector2((SlotIndex - 1.5f) * 42f, -105f - (SlotIndex % 2) * 48f);

                if ((Age + SlotIndex * 7) % 30 == 0 && Main.netMode != NetmodeID.MultiplayerClient)
                {
                    // Fires the Wulfrum Controller droid's own energy burst rather than a custom bolt.
                    // That projectile drives its own ai[0]/ai[1] (launch rotation and homing target),
                    // so no extra AI arguments may be handed to it.
                    Vector2 velocity = NPC.SafeDirectionTo(target.Center) * 10f;
                    int damage = GetAttackDamage(0.18f);
                    int burst = Projectile.NewProjectile(NPC.GetSource_FromAI(), NPC.Center, velocity,
                        ModContent.ProjectileType<WulfrumEnergyBurst>(), damage, 0.6f, OwnerIndex);
                    if (Main.projectile.IndexInRange(burst))
                    {
                        Main.projectile[burst].originalDamage = damage;
                        Main.projectile[burst].netUpdate = true;
                    }
                    SoundEngine.PlaySound(SoundID.Item12 with { Volume = 0.28f, Pitch = 0.35f }, NPC.Center);
                }
            }

            float movementSpeed = CalamityWorld.death ? 14f : CalamityWorld.revenge ? 13f : Main.expertMode ? 12f : 10f;
            Vector2 desiredVelocity = NPC.SafeDirectionTo(destination) * Math.Min(movementSpeed, NPC.Distance(destination) * 0.11f);
            NPC.velocity = Vector2.Lerp(NPC.velocity, desiredVelocity, 0.24f);
            NPC.spriteDirection = (NPC.velocity.X < 0f).ToDirectionInt();
            NPC.rotation = NPC.velocity.X / 25f;

            if (!Main.dedServ)
            {
                Dust dust = Dust.NewDustPerfect(NPC.Bottom, DustID.Vortex);
                dust.color = Color.Green;
                dust.scale = 0.675f;
            }
        }

        public override void FindFrame(int frameHeight)
        {
            NPC.frameCounter++;
            int half = FrameCount / 2;
            int frame = (int)(NPC.frameCounter / 5) % half;
            if (Amplified)
                frame += half;
            NPC.frame.Y = frame * frameHeight;
        }
    }

    internal sealed class ResponsibilityArmyHovercraft : ResponsibilityArmyUnitBase
    {
        protected override string SourceTexture => "CalamityMod/NPCs/NormalNPCs/WulfrumHovercraft";
        protected override int FrameCount => 12;
        protected override int Width => 40;
        protected override int Height => 38;
        protected override float LifeMultiplier => 0.7f;
        protected override int ChassisDefense => 10;
        protected override float DamageCapRatio => 0.3f;
        protected override bool Flying => true;
        protected override int NativeHitDustCount => 5;
        protected override int NativeDeathDustCount => 20;
        protected override string[] NativeGoreNames => new[] { "WulfrumHovercraftGore1", "WulfrumHovercraftGore2" };

        protected override void UpdateUnitAI()
        {
            NPC target = AcquireTarget();
            Vector2 destination;
            if (target == null)
            {
                destination = GetIdleAnchor(new Vector2((SlotIndex == 0 ? -1f : 1f) * 150f, -52f));
            }
            else
            {
                // Every Hovercraft holds an assigned flank, standoff range, and hover altitude, so
                // the pair spreads into a firing line instead of piling onto one point behind the
                // target. The slow bob keeps them from looking welded to their anchors.
                float side = SlotIndex % 2 == 0 ? -1f : 1f;
                int rank = SlotIndex / 2;
                float standoff = 620f + rank * 130f;
                float altitude = 58f + (SlotIndex % 2 == 0 ? 0f : 52f) + rank * 34f;
                destination = target.Center + new Vector2(side * standoff, 0f);
                float groundY = ResponsibilityArmyGround.FindGroundY(destination.X, target.Center.Y, 340f);
                destination.Y = groundY - altitude - MathF.Sin((Age + SlotIndex * 40) * 0.04f) * 12f;

                if ((Age + SlotIndex * 45) % 90 == 0 && Main.netMode != NetmodeID.MultiplayerClient)
                    FireMissileSalvo(target);
            }

            float movementSpeed = Amplified ? 13f : 11f;
            Vector2 desiredVelocity = NPC.SafeDirectionTo(destination) * Math.Min(movementSpeed, NPC.Distance(destination) * 0.09f);
            NPC.velocity = Vector2.Lerp(NPC.velocity, desiredVelocity, 0.18f);
            NPC.rotation = NPC.velocity.X / 16f;
            NPC.spriteDirection = (NPC.velocity.X < 0f).ToDirectionInt();
            Lighting.AddLight(NPC.Center - Vector2.UnitY * 8f, Color.Lime.ToVector3() * 1.5f);
        }

        private void FireMissileSalvo(NPC target)
        {
            for (int i = 0; i < 6; i++)
            {
                Vector2 velocity = new Vector2(Main.rand.NextFloat(-2.8f, 2.8f), Main.rand.NextFloat(-10.5f, -7.5f));
                Projectile.NewProjectile(NPC.GetSource_FromAI(), NPC.Top, velocity,
                    ModContent.ProjectileType<ResponsibilityHovercraftMissile>(), GetAttackDamage(0.14f), 1f, OwnerIndex, target.whoAmI, SlotIndex);
            }
            SoundEngine.PlaySound(SoundID.Item61 with { Volume = 0.38f, Pitch = 0.25f }, NPC.Center);
        }

        public override void FindFrame(int frameHeight)
        {
            NPC.frameCounter++;
            int half = FrameCount / 2;
            int frame = (int)(NPC.frameCounter / 5) % half;
            if (Amplified)
                frame += half;
            NPC.frame.Y = frame * frameHeight;
        }
    }

    internal static class ResponsibilityArmyGround
    {
        internal static Vector2 FindGround(Vector2 origin, int width, int height)
        {
            int tileX = Math.Clamp((int)(origin.X / 16f), 10, Main.maxTilesX - 10);
            int startY = Math.Clamp((int)(origin.Y / 16f), 10, Main.maxTilesY - 20);

            for (int offset = -18; offset <= 70; offset++)
            {
                int tileY = Math.Clamp(startY + offset, 10, Main.maxTilesY - 10);
                Tile tile = Framing.GetTileSafely(tileX, tileY);
                if (tile.HasTile && Main.tileSolid[tile.TileType] && !Main.tileSolidTop[tile.TileType])
                    return new Vector2(tileX * 16f + 8f, tileY * 16f - height * 0.5f - 2f);
            }
            return origin - Vector2.UnitY * height;
        }

        internal static float FindGroundY(float worldX, float startWorldY, float searchDistance)
        {
            int tileX = Math.Clamp((int)(worldX / 16f), 10, Main.maxTilesX - 10);
            int startY = Math.Clamp((int)(startWorldY / 16f), 10, Main.maxTilesY - 20);
            int tiles = Math.Max(1, (int)(searchDistance / 16f));
            for (int offset = -8; offset <= tiles; offset++)
            {
                int tileY = Math.Clamp(startY + offset, 10, Main.maxTilesY - 10);
                Tile tile = Framing.GetTileSafely(tileX, tileY);
                if (tile.HasTile && Main.tileSolid[tile.TileType])
                    return tileY * 16f;
            }
            return startWorldY + 100f;
        }
    }

    internal sealed class ResponsibilityRoverScrap : WulfrumScrapBullet
    {
        public override void SetDefaults()
        {
            base.SetDefaults();
            Projectile.DamageType = DamageClass.Summon;
        }
    }

    internal sealed class ResponsibilityRoverFusionBolt : WulfrumFusionBolt
    {
        public override void SetDefaults()
        {
            base.SetDefaults();
            Projectile.DamageType = DamageClass.Summon;
        }
    }
}
