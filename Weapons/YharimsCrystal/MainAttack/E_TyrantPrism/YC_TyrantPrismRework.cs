using CalamityMod;
using CalamityMod.Buffs.DamageOverTime;
using CalamityMod.Particles;
using CalamityMod.Projectiles.Magic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.Enums;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.YharimsCrystal.MainAttack.E_TyrantPrism
{
    public class YC_TyrantPrismHoldout : YC_BaseHoldout
    {
        public enum TyrantPrismState
        {
            Converging,
            Combat,
            HeavyRest
        }

        public const int DroneCount = 6;
        public const int ConvergenceFrames = 156;
        public const int SpawnInterval = 13;
        public const int MainBeamFadeFrames = 46;
        public const int HeavyRestFrames = 96;
        private const int ManaDrainInterval = 10;

        private int manaDrainTimer;
        private bool convergenceReadySoundPlayed;

        private ref float StateRaw => ref Projectile.ai[0];
        private ref float StateTimerRaw => ref Projectile.ai[1];
        private ref float CommandSerialRaw => ref Projectile.ai[2];

        public TyrantPrismState CurrentState => (TyrantPrismState)(int)StateRaw;
        public int StateTimer => (int)StateTimerRaw;
        public int CommandSerial => (int)CommandSerialRaw;
        public float HoldFrames => HoldFrameCounter;
        public float ConvergenceRatio => MathHelper.Clamp(HoldFrameCounter / ConvergenceFrames, 0f, 1f);
        public float MainBeamStrength => CurrentState == TyrantPrismState.Converging
            ? Utils.GetLerpValue(ConvergenceFrames - MainBeamFadeFrames, ConvergenceFrames, HoldFrameCounter, true)
            : 1f;
        public bool MainBeamCanDamage => CurrentState != TyrantPrismState.Converging || MainBeamStrength > 0.34f;
        public bool DroneCombatOnline => CurrentState != TyrantPrismState.Converging;
        public Vector2 MainMuzzle => Projectile.Center + ForwardDirection * 24f;
        public Vector2 BeamFocusPoint => Projectile.Center + ForwardDirection * MathHelper.Lerp(720f, 1320f, MainBeamStrength);

        protected override float HoldoutDistance => 4f;
        protected override float SoundPitch => 0.14f;

        protected override void OnHoldoutAI()
        {
            Projectile.damage = Owner.GetWeaponDamage(Owner.HeldItem);

            if (Main.myPlayer == Projectile.owner)
                Owner.Calamity().rightClickListener = true;

            EnsureMainBeam();
            EnsureDrones();
            DrainManaOrReset();

            StateTimerRaw++;

            if (CurrentState == TyrantPrismState.Converging)
            {
                if (HoldFrameCounter >= ConvergenceFrames && CountOwnedDrones() >= DroneCount)
                    EnterCombatState();

                EmitConvergenceFX();
                return;
            }

            if (CurrentState == TyrantPrismState.Combat)
            {
                TryIssueHeavyCommand();
                EmitCombatFX();
                return;
            }

            if (StateTimer >= HeavyRestFrames)
                SetState(TyrantPrismState.Combat);

            EmitRestFX();
        }

        public override void OnKill(int timeLeft)
        {
            KillOwnedPrismProjectiles();
        }

        public override bool PreDraw(ref Color lightColor)
        {
            base.PreDraw(ref lightColor);

            if (Main.dedServ)
                return false;

            Texture2D glow = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Vector2 muzzle = MainMuzzle - Main.screenPosition;
            float strength = MathHelper.Clamp(0.18f + MainBeamStrength, 0f, 1f);
            float pulse = 1f + 0.08f * (float)System.Math.Sin(HoldFrameCounter * 0.18f);
            Color gold = new Color(255, 216, 104, 0);

            Main.EntitySpriteDraw(glow, muzzle, null, gold * (0.38f * strength), Projectile.rotation, glow.Size() * 0.5f, (0.13f + MainBeamStrength * 0.2f) * pulse, SpriteEffects.None, 0f);
            Main.EntitySpriteDraw(glow, muzzle, null, (Color.White with { A = 0 }) * (0.18f + MainBeamStrength * 0.32f), Projectile.rotation, glow.Size() * 0.5f, (0.055f + MainBeamStrength * 0.08f) * pulse, SpriteEffects.None, 0f);

            return false;
        }

        public bool TryGetDrone(int slot, out Projectile droneProjectile, out YC_TyrantPrismDrone drone)
        {
            droneProjectile = null;
            drone = null;

            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile other = Main.projectile[i];
                if (!other.active ||
                    other.owner != Projectile.owner ||
                    other.type != ModContent.ProjectileType<YC_TyrantPrismDrone>() ||
                    (int)other.ai[0] != slot ||
                    (int)other.ai[1] != Projectile.whoAmI)
                {
                    continue;
                }

                if (other.ModProjectile is YC_TyrantPrismDrone droneMod)
                {
                    droneProjectile = other;
                    drone = droneMod;
                    return true;
                }
            }

            return false;
        }

        private void EnsureMainBeam()
        {
            if (HasMainBeam())
                return;

            if (Projectile.owner != Main.myPlayer)
                return;

            Projectile.NewProjectile(
                Projectile.GetSource_FromThis(),
                MainMuzzle,
                ForwardDirection,
                ModContent.ProjectileType<YC_TyrantPrismMainBeam>(),
                Projectile.damage,
                Projectile.knockBack,
                Projectile.owner,
                Projectile.whoAmI);
        }

        private bool HasMainBeam()
        {
            int beamType = ModContent.ProjectileType<YC_TyrantPrismMainBeam>();
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile other = Main.projectile[i];
                if (other.active && other.owner == Projectile.owner && other.type == beamType && (int)other.ai[0] == Projectile.whoAmI)
                    return true;
            }

            return false;
        }

        private void EnsureDrones()
        {
            if (Projectile.owner != Main.myPlayer)
                return;

            for (int slot = 0; slot < DroneCount; slot++)
            {
                if (HoldFrameCounter < slot * SpawnInterval)
                    break;

                if (TryGetDrone(slot, out _, out _))
                    continue;

                Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    Projectile.Center,
                    ForwardDirection,
                    ModContent.ProjectileType<YC_TyrantPrismDrone>(),
                    Projectile.damage,
                    Projectile.knockBack,
                    Projectile.owner,
                    slot,
                    Projectile.whoAmI);

                SoundEngine.PlaySound(SoundID.Item15 with { Volume = 0.16f, Pitch = 0.08f + slot * 0.025f }, Projectile.Center);
            }
        }

        private int CountOwnedDrones()
        {
            int count = 0;
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile other = Main.projectile[i];
                if (other.active &&
                    other.owner == Projectile.owner &&
                    other.type == ModContent.ProjectileType<YC_TyrantPrismDrone>() &&
                    (int)other.ai[1] == Projectile.whoAmI)
                {
                    count++;
                }
            }

            return count;
        }

        private void DrainManaOrReset()
        {
            if (Projectile.owner != Main.myPlayer)
                return;

            manaDrainTimer++;
            if (manaDrainTimer < ManaDrainInterval)
                return;

            manaDrainTimer = 0;
            if (Owner.CheckMana(Owner.HeldItem, -1, true))
                return;

            SoundEngine.PlaySound(SoundID.MaxMana with { Pitch = -0.45f }, Owner.Center);
            Projectile.Kill();
        }

        private void EnterCombatState()
        {
            SetState(TyrantPrismState.Combat);

            if (!convergenceReadySoundPlayed)
            {
                convergenceReadySoundPlayed = true;
                SoundEngine.PlaySound(SoundID.Item122 with { Volume = 0.48f, Pitch = -0.12f }, Projectile.Center);
                Owner.Calamity().GeneralScreenShakePower = System.Math.Max(Owner.Calamity().GeneralScreenShakePower, 4.5f);
            }
        }

        private void TryIssueHeavyCommand()
        {
            if (Projectile.owner != Main.myPlayer)
                return;

            if (!Owner.Calamity().mouseRight || !Main.mouseRightRelease || Main.mapFullscreen || Main.blockMouse)
                return;

            int heavyManaCost = System.Math.Max(1, (int)(Owner.HeldItem.mana * Owner.manaCost * 8f));
            if (!Owner.CheckMana(Owner.HeldItem, heavyManaCost, true))
            {
                SoundEngine.PlaySound(SoundID.MaxMana with { Pitch = -0.45f }, Owner.Center);
                Projectile.Kill();
                return;
            }

            CommandSerialRaw++;
            SetState(TyrantPrismState.HeavyRest);
            SoundEngine.PlaySound(SoundID.Item92 with { Volume = 0.5f, Pitch = -0.2f }, Projectile.Center);
            Owner.Calamity().GeneralScreenShakePower = System.Math.Max(Owner.Calamity().GeneralScreenShakePower, 5.8f);
        }

        private void SetState(TyrantPrismState state)
        {
            StateRaw = (int)state;
            StateTimerRaw = 0f;
            Projectile.netUpdate = true;
        }

        private void KillOwnedPrismProjectiles()
        {
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile other = Main.projectile[i];
                if (!other.active || other.owner != Projectile.owner)
                    continue;

                if (other.type == ModContent.ProjectileType<YC_TyrantPrismDrone>() && (int)other.ai[1] == Projectile.whoAmI)
                    other.Kill();
                else if (other.type == ModContent.ProjectileType<YC_TyrantPrismMainBeam>() && (int)other.ai[0] == Projectile.whoAmI)
                    other.Kill();
                else if (other.type == ModContent.ProjectileType<YC_TyrantPrismConvergeBeam>() && (int)other.ai[1] == Projectile.whoAmI)
                    other.Kill();
            }
        }

        private void EmitConvergenceFX()
        {
            if (Main.dedServ || Main.GameUpdateCount % 4 != 0)
                return;

            Vector2 normal = ForwardDirection.RotatedBy(MathHelper.PiOver2);
            float radius = MathHelper.Lerp(42f, 8f, ConvergenceRatio);
            Vector2 position = MainMuzzle + normal * Main.rand.NextFloat(-radius, radius) - ForwardDirection * Main.rand.NextFloat(4f, 30f);
            EmitDust(
                position,
                ForwardDirection.RotatedByRandom(0.28f) * Main.rand.NextFloat(0.8f, 2.4f),
                Color.Lerp(new Color(255, 188, 86), Color.White, Main.rand.NextFloat(0.18f, 0.58f)),
                Main.rand.NextFloat(0.7f, 1.1f),
                DustID.GoldFlame);
        }

        private void EmitCombatFX()
        {
            if (Main.dedServ || Main.GameUpdateCount % 8 != 0)
                return;

            GlowOrbParticle glow = new(
                MainMuzzle + Main.rand.NextVector2Circular(5f, 5f),
                ForwardDirection * Main.rand.NextFloat(0.4f, 1.3f),
                false,
                Main.rand.Next(8, 12),
                Main.rand.NextFloat(0.22f, 0.34f),
                Color.Lerp(new Color(255, 214, 108), Color.White, Main.rand.NextFloat(0.25f, 0.65f)),
                true,
                false,
                true);
            GeneralParticleHandler.SpawnParticle(glow);
        }

        private void EmitRestFX()
        {
            if (Main.dedServ || Main.GameUpdateCount % 10 != 0)
                return;

            Dust dust = Dust.NewDustPerfect(
                MainMuzzle + Main.rand.NextVector2Circular(8f, 8f),
                DustID.SteampunkSteam,
                -Vector2.UnitY * Main.rand.NextFloat(0.4f, 1.2f),
                80,
                default,
                Main.rand.NextFloat(0.45f, 0.85f));
            dust.noGravity = false;
            dust.color = new Color(255, 210, 110);
        }
    }

public class YC_TyrantPrismDrone : ModProjectile, ILocalizedModType
    {
        private static readonly Vector2[] FleetOffsets =
        {
            new(-72f, -18f),
            new(-118f, -54f),
            new(-52f, -96f),
            new(72f, -18f),
            new(118f, -54f),
            new(52f, -96f)
        };

        private bool positionInitialized;
        private bool combatAttackInitialized;
        private int attackTimer;
        private int burstShotsRemaining;
        private int lastCommandSerial = -1;
        private int lastRhythmStep = -1;

        public new string LocalizationCategory => "Projectiles.YharimsCrystal";
        public override string Texture => "CalamityLegendsComeBack/Weapons/YharimsCrystal/YCRight/YC_Right_Drone";

        public int SlotIndex => (int)Projectile.ai[0];
        public int ParentHoldoutIndex => (int)Projectile.ai[1];
        public Vector2 CurrentForwardDirection { get; private set; } = Vector2.UnitX;

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 10;
            ProjectileID.Sets.TrailingMode[Type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = 34;
            Projectile.height = 34;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.friendly = false;
            Projectile.hostile = false;
            Projectile.netImportant = true;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.timeLeft = 2;
        }

        public override bool ShouldUpdatePosition() => false;
        public override bool? CanDamage() => false;

        public override void AI()
        {
            if (!TryGetHoldout(out Projectile holdoutProjectile, out YC_TyrantPrismHoldout holdout))
            {
                Projectile.Kill();
                return;
            }

            Player owner = Main.player[Projectile.owner];
            if (!owner.active || owner.dead)
            {
                Projectile.Kill();
                return;
            }

            Projectile.timeLeft = 2;
            Projectile.damage = holdoutProjectile.damage;

            UpdateMovement(owner, holdoutProjectile, holdout);
            UpdateFacing(owner, holdout);
            EnsureConvergenceBeam(holdout);
            UpdateAttacks(owner, holdout);
            EmitIdleFX(holdout);

            Lighting.AddLight(Projectile.Center, new Color(255, 213, 116).ToVector3() * 0.42f);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Type].Value;
            Vector2 origin = texture.Size() * 0.5f;
            SpriteEffects effects = Projectile.spriteDirection == -1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;

            for (int i = Projectile.oldPos.Length - 1; i >= 0; i--)
            {
                if (Projectile.oldPos[i] == Vector2.Zero)
                    continue;

                float completion = 1f - i / (float)Projectile.oldPos.Length;
                Vector2 oldDraw = Projectile.oldPos[i] + Projectile.Size * 0.5f - Main.screenPosition;
                Main.EntitySpriteDraw(texture, oldDraw, null, new Color(255, 212, 104, 0) * (0.06f + completion * 0.12f), Projectile.rotation, origin, Projectile.scale, effects, 0);
            }

            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            Main.EntitySpriteDraw(texture, drawPosition, null, Color.White, Projectile.rotation, origin, Projectile.scale, effects, 0);
            Main.EntitySpriteDraw(texture, drawPosition, null, new Color(255, 232, 160, 0) * 0.32f, Projectile.rotation, origin, Projectile.scale * 1.08f, effects, 0);
            return false;
        }

        private bool TryGetHoldout(out Projectile holdoutProjectile, out YC_TyrantPrismHoldout holdout)
        {
            holdoutProjectile = null;
            holdout = null;

            if (ParentHoldoutIndex < 0 || ParentHoldoutIndex >= Main.maxProjectiles)
                return false;

            Projectile candidate = Main.projectile[ParentHoldoutIndex];
            if (!candidate.active ||
                candidate.owner != Projectile.owner ||
                candidate.type != ModContent.ProjectileType<YC_TyrantPrismHoldout>() ||
                candidate.ModProjectile is not YC_TyrantPrismHoldout holdoutMod)
            {
                return false;
            }

            holdoutProjectile = candidate;
            holdout = holdoutMod;
            return true;
        }

        private void UpdateMovement(Player owner, Projectile holdoutProjectile, YC_TyrantPrismHoldout holdout)
        {
            Vector2 desiredCenter = GetDesiredCenter(owner, holdoutProjectile, holdout);
            if (!positionInitialized)
            {
                Projectile.Center = desiredCenter;
                positionInitialized = true;
                return;
            }

            float response = holdout.CurrentState == YC_TyrantPrismHoldout.TyrantPrismState.Converging ? 0.16f : 0.105f;
            float maxSpeed = holdout.CurrentState == YC_TyrantPrismHoldout.TyrantPrismState.Converging ? 18f : 13f;
            Vector2 desiredVelocity = (desiredCenter - Projectile.Center) * response;
            if (desiredVelocity.Length() > maxSpeed)
                desiredVelocity = desiredVelocity.SafeNormalize(Vector2.Zero) * maxSpeed;

            Projectile.velocity = Vector2.Lerp(Projectile.velocity, desiredVelocity, 0.34f);
            Projectile.Center += Projectile.velocity;
            Projectile.velocity *= 0.92f;
        }

        private Vector2 GetDesiredCenter(Player owner, Projectile holdoutProjectile, YC_TyrantPrismHoldout holdout)
        {
            Vector2 forward = holdout.ForwardDirection;
            Vector2 right = forward.RotatedBy(MathHelper.PiOver2);

            if (holdout.CurrentState == YC_TyrantPrismHoldout.TyrantPrismState.Converging)
            {
                float phase = holdout.HoldFrames * 0.105f + SlotIndex * MathHelper.TwoPi / YC_TyrantPrismHoldout.DroneCount;
                float spiralRadius = MathHelper.Lerp(34f, 14f, holdout.ConvergenceRatio);
                Vector2 spiralOffset = right * ((float)System.Math.Cos(phase) * spiralRadius) +
                    forward * ((float)System.Math.Sin(phase * 1.35f) * spiralRadius * 0.42f);
                return holdout.MainMuzzle - forward * 20f + spiralOffset;
            }

            Vector2 local = FleetOffsets[Utils.Clamp(SlotIndex, 0, FleetOffsets.Length - 1)];
            float bob = (float)System.Math.Sin(Main.GlobalTimeWrappedHourly * 4.1f + SlotIndex * 0.72f) * 4.5f;
            return owner.MountedCenter + right * (local.X + bob * 0.45f) + forward * (local.Y + bob);
        }

        private void UpdateFacing(Player owner, YC_TyrantPrismHoldout holdout)
        {
            Vector2 defaultDirection = holdout.ForwardDirection;
            if (holdout.CurrentState == YC_TyrantPrismHoldout.TyrantPrismState.Converging)
            {
                CurrentForwardDirection = (holdout.BeamFocusPoint - Projectile.Center).SafeNormalize(defaultDirection);
            }
            else
            {
                NPC target = FindTarget(owner, 1750f);
                CurrentForwardDirection = target != null
                    ? (target.Center - Projectile.Center).SafeNormalize(defaultDirection)
                    : defaultDirection;
            }

            Projectile.rotation = CurrentForwardDirection.ToRotation() + MathHelper.PiOver2;
            Projectile.direction = Projectile.spriteDirection = CurrentForwardDirection.X >= 0f ? 1 : -1;
            Projectile.scale = 0.86f + 0.035f * (float)System.Math.Sin(Main.GlobalTimeWrappedHourly * 5f + SlotIndex * 0.8f);
        }

        private void EnsureConvergenceBeam(YC_TyrantPrismHoldout holdout)
        {
            if (holdout.CurrentState != YC_TyrantPrismHoldout.TyrantPrismState.Converging || Projectile.owner != Main.myPlayer || HasConvergenceBeam())
                return;

            Projectile.NewProjectile(
                Projectile.GetSource_FromThis(),
                Projectile.Center,
                CurrentForwardDirection,
                ModContent.ProjectileType<YC_TyrantPrismConvergeBeam>(),
                (int)(Projectile.damage * 0.42f),
                Projectile.knockBack * 0.2f,
                Projectile.owner,
                Projectile.whoAmI,
                ParentHoldoutIndex);
        }

        private bool HasConvergenceBeam()
        {
            int beamType = ModContent.ProjectileType<YC_TyrantPrismConvergeBeam>();
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile other = Main.projectile[i];
                if (other.active && other.owner == Projectile.owner && other.type == beamType && (int)other.ai[0] == Projectile.whoAmI)
                    return true;
            }

            return false;
        }

        private void UpdateAttacks(Player owner, YC_TyrantPrismHoldout holdout)
        {
            if (!holdout.DroneCombatOnline)
                return;

            if (lastCommandSerial < 0)
                lastCommandSerial = holdout.CommandSerial;

            if (holdout.CommandSerial != lastCommandSerial)
            {
                lastCommandSerial = holdout.CommandSerial;
                burstShotsRemaining = 0;
                attackTimer = 48 + SlotIndex * 4;
                lastRhythmStep = -1;

                if (Projectile.owner == Main.myPlayer)
                    FireHeavySalvo();
            }

            if (holdout.CurrentState == YC_TyrantPrismHoldout.TyrantPrismState.HeavyRest)
                return;

            int rhythmStep = holdout.StateTimer / 18;
            if (rhythmStep == lastRhythmStep)
                return;

            lastRhythmStep = rhythmStep;
            if (!IsRhythmPair(rhythmStep % 3))
                return;

            if (Projectile.owner == Main.myPlayer)
                FireRhythmVolley(rhythmStep);
        }

        private bool IsRhythmPair(int pair) => pair switch
        {
            0 => SlotIndex is 0 or 3,
            1 => SlotIndex is 1 or 4,
            _ => SlotIndex is 2 or 5
        };

        private void FireRhythmVolley(int rhythmStep)
        {
            Vector2 direction = CurrentForwardDirection.SafeNormalize(Vector2.UnitX);
            Vector2 muzzle = Projectile.Center + direction * 18f;
            Vector2 side = direction.RotatedBy(MathHelper.PiOver2);

            Projectile.NewProjectile(
                Projectile.GetSource_FromThis(),
                muzzle,
                direction,
                ModContent.ProjectileType<YC_TyrantPrismLaserLance>(),
                (int)(Projectile.damage * 0.58f),
                Projectile.knockBack * 0.25f,
                Projectile.owner,
                620f + (rhythmStep % 3) * 60f,
                SlotIndex);

            for (int i = 0; i < 2; i++)
            {
                float spread = i == 0 ? -0.025f : 0.025f;
                Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    muzzle + side * (i == 0 ? -3.5f : 3.5f),
                    direction.RotatedBy(spread + Main.rand.NextFloat(-0.012f, 0.012f)) * Main.rand.NextFloat(12.4f, 14.2f),
                    ModContent.ProjectileType<YC_TyrantPrismBolt>(),
                    (int)(Projectile.damage * 0.42f),
                    Projectile.knockBack * 0.18f,
                    Projectile.owner,
                    SlotIndex + Main.rand.NextFloat(),
                    0.9f);
            }

            EmitMuzzleBurst(direction, 6, 4.8f);
            SoundEngine.PlaySound(SoundID.Item12 with { Volume = 0.16f, Pitch = 0.18f + SlotIndex * 0.02f }, Projectile.Center);
        }

        private void FireHeavySalvo()
        {
            Vector2 direction = CurrentForwardDirection.SafeNormalize(Vector2.UnitX);
            Vector2 muzzle = Projectile.Center + direction * 18f;
            Vector2 missileVelocity = direction.RotatedBy(Main.rand.NextFloat(-0.06f, 0.06f)) * 12.5f;

            Projectile.NewProjectile(
                Projectile.GetSource_FromThis(),
                muzzle,
                missileVelocity,
                ModContent.ProjectileType<YC_TyrantPrismMissile>(),
                (int)(Projectile.damage * 2.65f),
                Projectile.knockBack * 2.2f,
                Projectile.owner,
                SlotIndex);

            EmitHeavyMuzzleFX(muzzle, direction);

            SoundStyle missileSound = new("CalamityMod/Sounds/Item/MagnaCannonShot");
            SoundEngine.PlaySound(missileSound with { Volume = 0.22f, Pitch = -0.18f + SlotIndex * 0.02f, PitchVariance = 0.12f }, Projectile.Center);
        }

        private static void EmitHeavyMuzzleFX(Vector2 muzzle, Vector2 direction)
        {
            if (Main.dedServ)
                return;

            Color effectColor = new(255, 214, 92);
            for (int i = 0; i < 7; i++)
            {
                Vector2 dustVelocity = (direction * 10f).RotatedByRandom(0.5f) * Main.rand.NextFloat(0.1f, 1.6f);
                Dust dust = Dust.NewDustPerfect(muzzle, Main.rand.NextBool(4) ? DustID.YellowTorch : DustID.GoldFlame, dustVelocity);
                dust.scale = Main.rand.NextFloat(1.05f, 1.35f);
                dust.noGravity = true;
                dust.color = Main.rand.NextBool() ? Color.Lerp(effectColor, Color.White, 0.5f) : effectColor;
            }

            GlowSparkParticle pulse = new(
                muzzle - direction * 10f,
                direction * 20f,
                false,
                Main.rand.Next(7, 12),
                0.045f,
                effectColor,
                new Vector2(1.5f, 0.9f),
                true);
            GeneralParticleHandler.SpawnParticle(pulse);
        }

        private NPC FindTarget(Player owner, float range)
        {
            NPC nearest = null;
            float maxDistanceSquared = range * range;

            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];
                if (!npc.CanBeChasedBy(Projectile))
                    continue;

                float distanceSquared = Vector2.DistanceSquared(Projectile.Center, npc.Center);
                if (distanceSquared > maxDistanceSquared)
                    continue;

                if (!Collision.CanHitLine(Projectile.Center, 1, 1, npc.Center, 1, 1))
                    continue;

                maxDistanceSquared = distanceSquared;
                nearest = npc;
            }

            return nearest;
        }

        private void EmitMuzzleBurst(Vector2 direction, int dustCount, float speed)
        {
            if (Main.dedServ)
                return;

            for (int i = 0; i < dustCount; i++)
            {
                Dust dust = Dust.NewDustPerfect(
                    Projectile.Center + direction * 12f,
                    DustID.GoldFlame,
                    direction.RotatedByRandom(0.3f) * Main.rand.NextFloat(speed * 0.45f, speed),
                    0,
                    Color.Lerp(new Color(255, 199, 92), Color.White, Main.rand.NextFloat(0.18f, 0.58f)),
                    Main.rand.NextFloat(0.75f, 1.15f));
                dust.noGravity = true;
            }
        }

        private void EmitIdleFX(YC_TyrantPrismHoldout holdout)
        {
            if (Main.dedServ || Main.GameUpdateCount % (holdout.DroneCombatOnline ? 10 : 5) != 0)
                return;

            Dust dust = Dust.NewDustPerfect(
                Projectile.Center + Main.rand.NextVector2Circular(4f, 4f),
                DustID.GoldFlame,
                CurrentForwardDirection.RotatedByRandom(0.25f) * Main.rand.NextFloat(0.25f, 0.9f),
                0,
                Color.Lerp(new Color(255, 204, 100), Color.White, Main.rand.NextFloat(0.16f, 0.5f)),
                Main.rand.NextFloat(0.55f, 0.9f));
            dust.noGravity = true;
        }
}

public class YC_TyrantPrismBolt : ModProjectile, ILocalizedModType
{
    public new string LocalizationCategory => "Projectiles.YharimsCrystal";

    public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

    public ref float Time => ref Projectile.ai[0];

    public override void SetDefaults()
    {
        Projectile.width = 16;
        Projectile.height = 16;
        Projectile.friendly = true;
        Projectile.DamageType = DamageClass.Magic;
        Projectile.penetrate = 2;
        Projectile.timeLeft = 100;
        Projectile.extraUpdates = 1;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = 12;
    }

    public override void AI()
    {
        Time++;
        Projectile.rotation = Projectile.velocity.ToRotation();
        Projectile.velocity *= 1.006f;

        Color glowColor = Color.Lerp(new Color(255, 198, 74), new Color(255, 242, 188), 0.48f);
        Lighting.AddLight(Projectile.Center, glowColor.ToVector3() * 0.85f);

        if (Main.rand.NextBool(2))
        {
            Dust dust = Dust.NewDustPerfect(
                Projectile.Center + Main.rand.NextVector2Circular(4f, 4f),
                DustID.GoldFlame,
                -Projectile.velocity * Main.rand.NextFloat(0.04f, 0.1f),
                80,
                glowColor,
                Main.rand.NextFloat(0.8f, 1.18f));
            dust.noGravity = true;
        }

        if (Main.rand.NextBool(6))
        {
            GlowOrbParticle glow = new(
                Projectile.Center + Main.rand.NextVector2Circular(3f, 3f),
                -Projectile.velocity * Main.rand.NextFloat(0.015f, 0.05f),
                false,
                Main.rand.Next(7, 11),
                Main.rand.NextFloat(0.2f, 0.29f),
                glowColor,
                true,
                false,
                true);
            GeneralParticleHandler.SpawnParticle(glow);
        }

        if (Main.rand.NextBool(8))
        {
            SquishyLightParticle softGlow = new(
                Projectile.Center + Main.rand.NextVector2Circular(2.6f, 2.6f),
                -Projectile.velocity * Main.rand.NextFloat(0.01f, 0.035f),
                Main.rand.NextFloat(0.16f, 0.24f),
                Color.Lerp(glowColor, Color.White, Main.rand.NextFloat(0.15f, 0.45f)),
                Main.rand.Next(7, 11));
            GeneralParticleHandler.SpawnParticle(softGlow);
        }

        if (Main.rand.NextBool(3))
        {
            PointParticle point = new(
                Projectile.Center + Main.rand.NextVector2Circular(3f, 3f),
                -Projectile.velocity * Main.rand.NextFloat(0.025f, 0.08f),
                false,
                Main.rand.Next(6, 10),
                Main.rand.NextFloat(0.42f, 0.78f),
                Color.Lerp(glowColor, Color.White, Main.rand.NextFloat(0.2f, 0.62f)));
            GeneralParticleHandler.SpawnParticle(point);
        }
    }

    public override bool PreDraw(ref Color lightColor)
    {
        Texture2D bloomCircle = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
        Vector2 circleOrigin = bloomCircle.Size() * 0.5f;
        Color outerColor = new Color(255, 186, 66, 0);
        Color innerColor = new Color(255, 248, 214, 0);

        Main.EntitySpriteDraw(
            bloomCircle,
            Projectile.Center - Main.screenPosition,
            null,
            outerColor,
            0f,
            circleOrigin,
            0.23f,
            SpriteEffects.None);
        Main.EntitySpriteDraw(
            bloomCircle,
            Projectile.Center - Main.screenPosition,
            null,
            innerColor,
            0f,
            circleOrigin,
            0.11f,
            SpriteEffects.None);
        return false;
    }

    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        target.AddBuff(ModContent.BuffType<Dragonfire>(), 90);
    }

    public override void OnKill(int timeLeft)
    {
        Color glowColor = new Color(255, 211, 100);
        for (int i = 0; i < 6; i++)
        {
            Dust dust = Dust.NewDustPerfect(
                Projectile.Center,
                DustID.GoldFlame,
                Main.rand.NextVector2CircularEdge(2.6f, 2.6f),
                80,
                glowColor,
                Main.rand.NextFloat(0.9f, 1.28f));
            dust.noGravity = true;
        }
    }
}

public class YC_TyrantPrismLaserLance : ModProjectile, ILocalizedModType
{
    public new string LocalizationCategory => "Projectiles.YharimsCrystal";
    public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

    private ref float BeamLength => ref Projectile.ai[0];
    private ref float SlotIndex => ref Projectile.ai[1];
    private ref float Timer => ref Projectile.localAI[0];

    public override void SetDefaults()
    {
        Projectile.width = 14;
        Projectile.height = 14;
        Projectile.friendly = true;
        Projectile.DamageType = DamageClass.Magic;
        Projectile.penetrate = -1;
        Projectile.timeLeft = 14;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = 8;
    }

    public override bool ShouldUpdatePosition() => false;

    public override void AI()
    {
        Timer++;
        Projectile.velocity = Projectile.velocity.SafeNormalize(Vector2.UnitX);
        Projectile.rotation = Projectile.velocity.ToRotation();
        if (BeamLength <= 0f)
            BeamLength = 620f;

        Lighting.AddLight(Projectile.Center, new Color(255, 224, 112).ToVector3() * 0.72f);
        if (Main.dedServ || Timer % 3f != 0f)
            return;

        Vector2 direction = Projectile.velocity;
        Vector2 position = Projectile.Center + direction * Main.rand.NextFloat(24f, BeamLength) + direction.RotatedBy(MathHelper.PiOver2) * Main.rand.NextFloat(-5f, 5f);
        GlowOrbParticle glow = new(
            position,
            -direction * Main.rand.NextFloat(0.15f, 0.55f),
            false,
            Main.rand.Next(7, 12),
            Main.rand.NextFloat(0.16f, 0.28f),
            Color.Lerp(new Color(255, 204, 92), Color.White, Main.rand.NextFloat(0.18f, 0.52f)),
            true,
            false,
            true);
        GeneralParticleHandler.SpawnParticle(glow);
    }

    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        float collisionPoint = 0f;
        return Collision.CheckAABBvLineCollision(
            targetHitbox.TopLeft(),
            targetHitbox.Size(),
            Projectile.Center,
            Projectile.Center + Projectile.velocity.SafeNormalize(Vector2.UnitX) * BeamLength,
            12f,
            ref collisionPoint);
    }

    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        target.AddBuff(ModContent.BuffType<Dragonfire>(), 120);
    }

    public override bool PreDraw(ref Color lightColor)
    {
        Texture2D line = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomLineSoftEdge").Value;
        Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
        Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitX);
        Vector2 start = Projectile.Center - Main.screenPosition;
        Vector2 end = Projectile.Center + direction * BeamLength - Main.screenPosition;
        Vector2 mid = (start + end) * 0.5f;
        float fadeIn = Utils.GetLerpValue(0f, 3f, Timer, true);
        float fadeOut = Utils.GetLerpValue(0f, 8f, Projectile.timeLeft, true);
        float opacity = fadeIn * fadeOut;
        float pulse = 0.9f + 0.1f * (float)System.Math.Sin(Timer * 0.7f + SlotIndex);
        Color gold = new Color(255, 208, 86, 0);
        Color white = Color.White with { A = 0 };

        Main.spriteBatch.SetBlendState(BlendState.Additive);
        Main.EntitySpriteDraw(line, mid, null, gold * 0.82f * opacity, direction.ToRotation() + MathHelper.PiOver2, line.Size() * 0.5f, new Vector2(0.18f * pulse, BeamLength / line.Height), SpriteEffects.None, 0f);
        Main.EntitySpriteDraw(line, mid, null, white * 0.28f * opacity, direction.ToRotation() + MathHelper.PiOver2, line.Size() * 0.5f, new Vector2(0.075f, BeamLength / line.Height), SpriteEffects.None, 0f);
        Main.EntitySpriteDraw(bloom, start, null, gold * 0.58f * opacity, Projectile.rotation, bloom.Size() * 0.5f, 0.15f * pulse, SpriteEffects.None, 0f);
        Main.spriteBatch.SetBlendState(BlendState.AlphaBlend);
        return false;
    }
}

public class YC_TyrantPrismMissile : ModProjectile, ILocalizedModType
{
    public new string LocalizationCategory => "Projectiles.YharimsCrystal";
    public override string Texture => "CalamityMod/Projectiles/Ranged/ThePackMissile";

    private ref float SlotIndex => ref Projectile.ai[0];
    private ref float Timer => ref Projectile.localAI[0];

    public override void SetStaticDefaults()
    {
        Main.projFrames[Type] = 9;
        ProjectileID.Sets.TrailCacheLength[Type] = 8;
        ProjectileID.Sets.TrailingMode[Type] = 0;
    }

    public override void SetDefaults()
    {
        Projectile.width = 40;
        Projectile.height = 40;
        Projectile.friendly = true;
        Projectile.DamageType = DamageClass.Magic;
        Projectile.penetrate = 1;
        Projectile.timeLeft = 260;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = -1;
    }

    public override void AI()
    {
        Timer++;
        Projectile.rotation = Projectile.velocity.ToRotation();
        Projectile.frameCounter++;
        Projectile.frame = Projectile.frameCounter / 4 % Main.projFrames[Type];

        if (Timer > 18f)
            HomeTowardTarget();
        else
            Projectile.velocity *= 1.012f;

        Color glow = Color.Lerp(new Color(255, 196, 72), Color.White, 0.28f);
        Lighting.AddLight(Projectile.Center, glow.ToVector3() * 0.85f);
        EmitMissileTrail(glow);
    }

    private void HomeTowardTarget()
    {
        NPC target = Projectile.Center.ClosestNPCAt(1500f);
        Vector2 currentDirection = Projectile.velocity.SafeNormalize(Vector2.UnitX);
        float speed = MathHelper.Clamp(Projectile.velocity.Length(), 12f, 24f);
        if (target is null)
        {
            Projectile.velocity = Vector2.Lerp(Projectile.velocity, currentDirection * MathHelper.Min(speed + 0.08f, 20f), 0.08f);
            return;
        }

        Vector2 predicted = target.Center + target.velocity * MathHelper.Clamp(Projectile.Distance(target.Center) / System.Math.Max(speed, 1f), 6f, 26f);
        Vector2 desiredDirection = (predicted - Projectile.Center).SafeNormalize(currentDirection);
        float lockIn = Utils.GetLerpValue(18f, 70f, Timer, true);
        float maxTurn = MathHelper.Lerp(MathHelper.ToRadians(4f), MathHelper.ToRadians(18f), lockIn);
        Vector2 newDirection = currentDirection.ToRotation().AngleTowards(desiredDirection.ToRotation(), maxTurn).ToRotationVector2();
        Projectile.velocity = Vector2.Lerp(Projectile.velocity, newDirection * MathHelper.Lerp(speed, 23f, 0.12f), 0.12f + lockIn * 0.08f);
    }

    private void EmitMissileTrail(Color glow)
    {
        if (Main.dedServ)
            return;

        Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitX);
        Vector2 normal = direction.RotatedBy(MathHelper.PiOver2);
        if (Main.rand.NextBool(2))
        {
            Dust dust = Dust.NewDustPerfect(
                Projectile.Center - direction * Main.rand.NextFloat(16f, 34f) + normal * Main.rand.NextFloat(-5f, 5f),
                Main.rand.NextBool(3) ? DustID.YellowTorch : DustID.GoldFlame,
                -direction * Main.rand.NextFloat(0.9f, 2.8f) + normal * Main.rand.NextFloat(-0.35f, 0.35f),
                0,
                glow,
                Main.rand.NextFloat(0.78f, 1.25f));
            dust.noGravity = true;
        }

        if (Timer % 3f == 0f)
        {
            GeneralParticleHandler.SpawnParticle(new CustomSpark(
                Projectile.Center - direction * 24f + normal * Main.rand.NextFloat(-7f, 7f),
                -direction * Main.rand.NextFloat(0.5f, 1.6f),
                "CalamityMod/Particles/BloomCircle",
                false,
                Main.rand.Next(10, 16),
                Main.rand.NextFloat(0.24f, 0.42f),
                Main.rand.NextBool(4) ? Color.White : glow,
                new Vector2(0.28f, 1.5f),
                true,
                true,
                extraRotation: -MathHelper.PiOver2,
                shrinkSpeed: 0.28f,
                glowOpacity: 0.78f));
        }
    }

    public override void OnKill(int timeLeft)
    {
        if (Projectile.owner == Main.myPlayer)
        {
            int oldWidth = Projectile.width;
            int oldHeight = Projectile.height;
            Vector2 center = Projectile.Center;
            Projectile.width = Projectile.height = 184;
            Projectile.Center = center;
            Projectile.penetrate = -1;
            Projectile.Damage();
            Projectile.width = oldWidth;
            Projectile.height = oldHeight;
            Projectile.Center = center;
        }

        if (Main.dedServ)
            return;

        Color gold = new(255, 205, 78);
        SoundEngine.PlaySound(new SoundStyle("CalamityMod/Sounds/Item/DeadSunExplosion") { Volume = 0.32f, Pitch = -0.15f, PitchVariance = 0.12f }, Projectile.Center);
        GeneralParticleHandler.SpawnParticle(new CustomPulse(Projectile.Center, Vector2.Zero, gold * 0.78f, "CalamityMod/Particles/FlameExplosion", Vector2.One, Main.rand.NextFloat(-10f, 10f), 0.05f, 0.56f, 18, true));
        GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(Projectile.Center, Vector2.Zero, Color.Lerp(gold, Color.White, 0.24f), Vector2.One, Projectile.rotation, 0.14f, 1.18f, 22));

        for (int i = 0; i < 28; i++)
        {
            Vector2 velocity = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(2.8f, 10.5f);
            Dust dust = Dust.NewDustPerfect(Projectile.Center, Main.rand.NextBool(4) ? DustID.YellowTorch : DustID.GoldFlame, velocity, 0, Main.rand.NextBool(4) ? Color.White : gold, Main.rand.NextFloat(0.9f, 1.65f));
            dust.noGravity = true;
        }

        for (int i = 0; i < 12; i++)
        {
            Vector2 velocity = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(2.2f, 7.8f);
            GeneralParticleHandler.SpawnParticle(new SparkParticle(Projectile.Center, velocity, false, Main.rand.Next(16, 28), Main.rand.NextFloat(0.7f, 1.3f), Color.Lerp(gold, Color.White, Main.rand.NextFloat(0.1f, 0.45f))));
        }
    }

    public override bool PreDraw(ref Color lightColor)
    {
        Texture2D texture = TextureAssets.Projectile[Type].Value;
        Rectangle frame = texture.Frame(verticalFrames: Main.projFrames[Type], frameY: Projectile.frame);
        Vector2 origin = frame.Size() * 0.5f;
        Color afterimageColor = new Color(255, 205, 86, 0) * 0.5f;

        CalamityUtils.DrawAfterimagesCentered(Projectile, ProjectileID.Sets.TrailingMode[Type], afterimageColor, 1);
        Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, frame, Color.Lerp(lightColor, Color.White, 0.48f), Projectile.rotation, origin, Projectile.scale, SpriteEffects.None, 0f);
        return false;
    }
}

public class YC_TyrantPrismMainBeam : ModProjectile, ILocalizedModType
    {
        private const float MaxBeamLength = 2600f;
        private const int SampleCount = 3;

        public new string LocalizationCategory => "Projectiles.YharimsCrystal";
        public override string Texture => "CalamityMod/Projectiles/Magic/YharimsCrystalBeam";

        private int HoldoutIndex => (int)Projectile.ai[0];
        private ref float BeamLength => ref Projectile.localAI[0];
        private ref float Timer => ref Projectile.localAI[1];

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.DrawScreenCheckFluff[Type] = 10000;
        }

        public override void SetDefaults()
        {
            Projectile.width = 18;
            Projectile.height = 18;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.hide = true;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 6;
            Projectile.timeLeft = 2;
        }

        public override bool ShouldUpdatePosition() => false;

        public override bool? CanDamage()
        {
            return TryGetHoldout(out _, out YC_TyrantPrismHoldout holdout) && holdout.MainBeamCanDamage ? null : false;
        }

        public override void DrawBehind(
            int index,
            System.Collections.Generic.List<int> behindNPCsAndTiles,
            System.Collections.Generic.List<int> behindNPCs,
            System.Collections.Generic.List<int> behindProjectiles,
            System.Collections.Generic.List<int> overPlayers,
            System.Collections.Generic.List<int> overWiresUI)
        {
            overPlayers.Add(index);
        }

        public override void AI()
        {
            if (!TryGetHoldout(out Projectile holdoutProjectile, out YC_TyrantPrismHoldout holdout))
            {
                Projectile.Kill();
                return;
            }

            Timer++;
            Projectile.timeLeft = 2;
            Projectile.Center = holdout.MainMuzzle;
            Projectile.velocity = holdout.ForwardDirection;
            Projectile.rotation = Projectile.velocity.ToRotation();
            Projectile.scale = MathHelper.Lerp(0.42f, 1.65f, holdout.MainBeamStrength);
            Projectile.damage = (int)(holdoutProjectile.damage * MathHelper.Lerp(0.82f, 2.35f, holdout.MainBeamStrength));

            UpdateBeamLength();
            EmitBeamFX(holdout);
            DelegateMethods.v3_1 = new Color(255, 214, 95).ToVector3() * (0.28f + holdout.MainBeamStrength * 0.42f);
            Utils.PlotTileLine(Projectile.Center, Projectile.Center + Projectile.velocity * BeamLength, 24f * Projectile.scale, DelegateMethods.CastLight);
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            if (Projectile.velocity == Vector2.Zero || BeamLength <= 0f)
                return false;

            float collisionPoint = 0f;
            float width = MathHelper.Lerp(10f, 42f, Projectile.scale / 1.65f);
            return Collision.CheckAABBvLineCollision(
                targetHitbox.TopLeft(),
                targetHitbox.Size(),
                Projectile.Center,
                Projectile.Center + Projectile.velocity * BeamLength,
                width,
                ref collisionPoint);
        }

        public override void CutTiles()
        {
            if (Projectile.velocity == Vector2.Zero)
                return;

            DelegateMethods.tilecut_0 = TileCuttingContext.AttackProjectile;
            Utils.PlotTileLine(Projectile.Center, Projectile.Center + Projectile.velocity * BeamLength, 30f * Projectile.scale, DelegateMethods.CutTiles);
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(ModContent.BuffType<Dragonfire>(), 180);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (!TryGetHoldout(out _, out YC_TyrantPrismHoldout holdout) || Projectile.velocity == Vector2.Zero || BeamLength <= 0f)
                return false;

            Texture2D beamTexture = ModContent.Request<Texture2D>(Texture).Value;
            Texture2D glow = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Vector2 unit = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            Vector2 start = Projectile.Center + unit * 8f * Projectile.scale - Main.screenPosition;
            Vector2 end = start + unit * MathHelper.Max(8f, BeamLength - 12f * Projectile.scale);
            float strength = holdout.MainBeamStrength;
            float opacity = Utils.GetLerpValue(0f, 0.08f, strength, true);
            float pulse = 1f + 0.055f * (float)System.Math.Sin(Timer * 0.21f);
            Color gold = Color.Lerp(new Color(255, 182, 76), new Color(255, 234, 150), strength);

            DelegateMethods.f_1 = 1f;
            Utils.LaserLineFraming framing = new(DelegateMethods.RainbowLaserDraw);

            DelegateMethods.c_1 = gold * (0.84f * opacity);
            Utils.DrawLaser(Main.spriteBatch, beamTexture, start, end, new Vector2(Projectile.scale * 0.42f * pulse), framing);

            DelegateMethods.c_1 = Color.White * (0.34f * opacity);
            Utils.DrawLaser(Main.spriteBatch, beamTexture, start, end, new Vector2(Projectile.scale * 0.16f), framing);

            Main.EntitySpriteDraw(glow, start, null, (gold with { A = 0 }) * (0.42f * opacity), Projectile.rotation, glow.Size() * 0.5f, 0.13f * Projectile.scale, SpriteEffects.None, 0);
            return false;
        }

        private bool TryGetHoldout(out Projectile holdoutProjectile, out YC_TyrantPrismHoldout holdout)
        {
            holdoutProjectile = null;
            holdout = null;

            if (HoldoutIndex < 0 || HoldoutIndex >= Main.maxProjectiles)
                return false;

            Projectile candidate = Main.projectile[HoldoutIndex];
            if (!candidate.active ||
                candidate.owner != Projectile.owner ||
                candidate.type != ModContent.ProjectileType<YC_TyrantPrismHoldout>() ||
                candidate.ModProjectile is not YC_TyrantPrismHoldout holdoutMod)
            {
                return false;
            }

            holdoutProjectile = candidate;
            holdout = holdoutMod;
            return true;
        }

        private void UpdateBeamLength()
        {
            float[] samples = new float[SampleCount];
            Collision.LaserScan(Projectile.Center, Projectile.velocity.SafeNormalize(Vector2.UnitX), 3f * Projectile.scale, MaxBeamLength, samples);

            float average = 0f;
            for (int i = 0; i < samples.Length; i++)
                average += samples[i];

            average /= samples.Length;
            if (average <= 0f)
                average = MaxBeamLength;

            BeamLength = MathHelper.Lerp(BeamLength <= 0f ? average : BeamLength, average, 0.66f);
        }

        private void EmitBeamFX(YC_TyrantPrismHoldout holdout)
        {
            if (Main.dedServ || holdout.MainBeamStrength < 0.12f || Main.GameUpdateCount % 5 != 0)
                return;

            Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            Vector2 normal = direction.RotatedBy(MathHelper.PiOver2);
            float distance = BeamLength > 44f ? Main.rand.NextFloat(18f, BeamLength - 18f) : BeamLength * 0.5f;
            Vector2 position = Projectile.Center + direction * distance + normal * Main.rand.NextFloat(-8f, 8f);
            Dust dust = Dust.NewDustPerfect(
                position,
                DustID.GoldFlame,
                normal * Main.rand.NextFloat(-0.8f, 0.8f) - direction * Main.rand.NextFloat(0.05f, 0.35f),
                0,
                Color.Lerp(new Color(255, 200, 88), Color.White, Main.rand.NextFloat(0.2f, 0.58f)),
                Main.rand.NextFloat(0.55f, 0.95f) * Projectile.scale);
            dust.noGravity = true;
        }
    }

    public class YC_TyrantPrismConvergeBeam : ModProjectile, ILocalizedModType
    {
        private const float MaxBeamLength = 1850f;

        public new string LocalizationCategory => "Projectiles.YharimsCrystal";
        public override string Texture => "CalamityMod/Projectiles/Magic/YharimsCrystalBeam";

        private int DroneIndex => (int)Projectile.ai[0];
        private int HoldoutIndex => (int)Projectile.ai[1];
        private ref float BeamLength => ref Projectile.localAI[0];
        private ref float Timer => ref Projectile.localAI[1];

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.DrawScreenCheckFluff[Type] = 8000;
        }

        public override void SetDefaults()
        {
            Projectile.width = 10;
            Projectile.height = 10;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.hide = true;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 8;
            Projectile.timeLeft = 2;
        }

        public override bool ShouldUpdatePosition() => false;

        public override bool? CanDamage()
        {
            if (!TryGetSources(out _, out _, out YC_TyrantPrismHoldout holdout))
                return false;

            return holdout.HoldFrames > 18f && holdout.MainBeamStrength < 0.84f ? null : false;
        }

        public override void AI()
        {
            if (!TryGetSources(out Projectile droneProjectile, out Projectile holdoutProjectile, out YC_TyrantPrismHoldout holdout) ||
                holdout.CurrentState != YC_TyrantPrismHoldout.TyrantPrismState.Converging)
            {
                Projectile.Kill();
                return;
            }

            Timer++;
            Projectile.timeLeft = 2;
            Vector2 focusOffset = holdout.ForwardDirection.RotatedBy(MathHelper.PiOver2) *
                ((DroneIndex - (YC_TyrantPrismHoldout.DroneCount - 1f) * 0.5f) * MathHelper.Lerp(72f, 0f, holdout.ConvergenceRatio));
            Vector2 targetPoint = holdout.BeamFocusPoint + focusOffset;
            Vector2 direction = (targetPoint - droneProjectile.Center).SafeNormalize(holdout.ForwardDirection);

            Projectile.Center = droneProjectile.Center + direction * 14f;
            Projectile.velocity = direction;
            Projectile.rotation = direction.ToRotation();
            Projectile.scale = MathHelper.Lerp(0.72f, 1.2f, holdout.ConvergenceRatio) * Utils.GetLerpValue(0.98f, 0.42f, holdout.MainBeamStrength, true);
            Projectile.damage = (int)(holdoutProjectile.damage * MathHelper.Lerp(0.35f, 0.52f, holdout.ConvergenceRatio));

            UpdateBeamLength();
            Lighting.AddLight(Projectile.Center, new Color(255, 206, 104).ToVector3() * (0.18f + Projectile.scale * 0.2f));
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            if (Projectile.velocity == Vector2.Zero || BeamLength <= 0f || Projectile.scale <= 0.08f)
                return false;

            float collisionPoint = 0f;
            return Collision.CheckAABBvLineCollision(
                targetHitbox.TopLeft(),
                targetHitbox.Size(),
                Projectile.Center,
                Projectile.Center + Projectile.velocity * BeamLength,
                18f * Projectile.scale,
                ref collisionPoint);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Projectile.velocity == Vector2.Zero || BeamLength <= 0f || Projectile.scale <= 0.08f)
                return false;

            Texture2D beamTexture = ModContent.Request<Texture2D>(Texture).Value;
            Texture2D glow = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Vector2 unit = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            Vector2 start = Projectile.Center + unit * 6f - Main.screenPosition;
            Vector2 end = start + unit * MathHelper.Max(8f, BeamLength - 10f);
            float opacity = MathHelper.Clamp(Projectile.scale / 1.2f, 0f, 1f);
            Color gold = Color.Lerp(new Color(255, 166, 78), new Color(255, 238, 172), 0.35f);

            DelegateMethods.f_1 = 1f;
            Utils.LaserLineFraming framing = new(DelegateMethods.RainbowLaserDraw);
            DelegateMethods.c_1 = gold * (0.84f * opacity);
            Utils.DrawLaser(Main.spriteBatch, beamTexture, start, end, new Vector2(Projectile.scale * 0.4f), framing);
            DelegateMethods.c_1 = Color.White * (0.32f * opacity);
            Utils.DrawLaser(Main.spriteBatch, beamTexture, start, end, new Vector2(Projectile.scale * 0.16f), framing);
            Main.EntitySpriteDraw(glow, start, null, (gold with { A = 0 }) * (0.46f * opacity), Projectile.rotation, glow.Size() * 0.5f, 0.16f * Projectile.scale, SpriteEffects.None, 0);
            return false;
        }

        private bool TryGetSources(out Projectile droneProjectile, out Projectile holdoutProjectile, out YC_TyrantPrismHoldout holdout)
        {
            droneProjectile = null;
            holdoutProjectile = null;
            holdout = null;

            if (DroneIndex < 0 || DroneIndex >= Main.maxProjectiles || HoldoutIndex < 0 || HoldoutIndex >= Main.maxProjectiles)
                return false;

            Projectile drone = Main.projectile[DroneIndex];
            Projectile candidateHoldout = Main.projectile[HoldoutIndex];

            if (!drone.active ||
                drone.owner != Projectile.owner ||
                drone.type != ModContent.ProjectileType<YC_TyrantPrismDrone>() ||
                !candidateHoldout.active ||
                candidateHoldout.owner != Projectile.owner ||
                candidateHoldout.type != ModContent.ProjectileType<YC_TyrantPrismHoldout>() ||
                candidateHoldout.ModProjectile is not YC_TyrantPrismHoldout holdoutMod)
            {
                return false;
            }

            droneProjectile = drone;
            holdoutProjectile = candidateHoldout;
            holdout = holdoutMod;
            return true;
        }

        private void UpdateBeamLength()
        {
            float[] samples = new float[3];
            Collision.LaserScan(Projectile.Center, Projectile.velocity.SafeNormalize(Vector2.UnitX), 2f * Projectile.scale, MaxBeamLength, samples);

            float average = 0f;
            for (int i = 0; i < samples.Length; i++)
                average += samples[i];

            average /= samples.Length;
            if (average <= 0f)
                average = MaxBeamLength;

            BeamLength = MathHelper.Lerp(BeamLength <= 0f ? average : BeamLength, average, 0.72f);
        }
    }

}
