using System;
using CalamityLegendsComeBack.Weapons.YharimsCrystal.LeftGeneral;
using CalamityLegendsComeBack.Weapons.YharimsCrystal.Passive;
using CalamityMod;
using CalamityMod.Particles;
using CalamityMod.Projectiles.Magic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.YharimsCrystal.RightGeneral
{
    internal sealed class YC_RightCrystalHoldout : YC_BaseHoldout
    {
        private static readonly Color HoldoutGold = new(255, 218, 88);
        private static readonly Color HoldoutOrange = new(255, 104, 36);

        private readonly BalanceYharimsCrystal balance = new();
        private int shardIndex = -1;
        private int laserIndex = -1;
        private readonly int[] droneIndices = new int[] { -1, -1, -1, -1, -1, -1 };

        // Charge state stored in the main beam: 0 = charging, 1 = charged/converged.
        // Focus is now a real cursor lock, so it retains the converged state instead of scattering.
        private const float LaserStateNormal = 0f;
        private const float LaserStateConverged = 1f;

        public float ChargeRatio => MathHelper.Clamp(HoldFrameCounter / balance.GetRightChargeFrames(), 0f, 1f);
        public bool Charged => HoldFrameCounter >= balance.GetRightChargeFrames();
        public Vector2 Muzzle => Projectile.Center + ForwardDirection * 32f;
        public bool IsFocusMode => IsLeftHeld();

        protected override float HoldoutDistance => 12f;
        protected override float SoundPitch => 0.14f;

        public override void OnSpawn(Terraria.DataStructures.IEntitySource source)
        {
            Owner.GetModPlayer<YharimsCrystalStatePlayer>().SetLastWeapon(YCWeaponForm.Crystal);
            Projectile.scale = 1.06f;
            SoundEngine.PlaySound(SoundID.Item117 with { Volume = 0.5f, Pitch = -0.25f }, Owner.Center);

            // Kill any background blade passive (it moves to background when crystal is active)
            int bgBladeType = ModContent.ProjectileType<YC_BackgroundBlade>();
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile p = Main.projectile[i];
                if (p.active && p.owner == Projectile.owner && p.type == bgBladeType)
                    p.Kill();
            }
        }

        protected override void OnHoldoutAI()
        {
            Projectile.damage = Owner.HeldItem.ModItem is NewLegendYharimsCrystal item
                ? item.GetScaledDamage(Owner, balance.GetRightClickBaseDamage())
                : Projectile.damage;

            EmitChargeFX();
            MaintainEmpoweredShard();
            MaintainMainLaser();
            MaintainDrones();

            // Update laser state based on charge and left-hold
            UpdateLaserChargeState();
        }

        public override void OnKill(int timeLeft)
        {
            KillProjectile(shardIndex);
            KillProjectile(laserIndex);
            for (int i = 0; i < droneIndices.Length; i++)
            {
                KillProjectile(droneIndices[i]);
                droneIndices[i] = -1;
            }

            // Right-click holdout ended: briefly block left-click holdout to prevent autoReuse immediately re-spawning it
            if (Projectile.owner == Main.myPlayer)
            {
                YharimsCrystalStatePlayer state = Main.player[Projectile.owner].GetModPlayer<YharimsCrystalStatePlayer>();
                state.LeftClickCooldown = Math.Max(state.LeftClickCooldown, 20);
            }

            SoundEngine.PlaySound(SoundID.Item14 with { Volume = 0.42f, Pitch = -0.15f }, Projectile.Center);
        }

        protected override bool IsRightHeld()
        {
            return (Main.mouseRight || Owner.Calamity().mouseRight || Owner.controlUseTile) &&
                !Main.mapFullscreen &&
                !Main.blockMouse &&
                !Owner.mouseInterface;
        }

        public bool IsLeftHeld()
        {
            return Main.mouseLeft &&
                !Main.mapFullscreen &&
                !Main.blockMouse &&
                !Owner.mouseInterface;
        }

        private void UpdateLaserChargeState()
        {
            if (Projectile.owner != Main.myPlayer)
                return;

            int laserType = ModContent.ProjectileType<YC_RightScorchingLaser>();
            if (!IsProjectileActive(laserIndex, laserType))
                return;

            Projectile laser = Main.projectile[laserIndex];

            float desiredState;
            if (Charged)
                desiredState = LaserStateConverged;
            else
                desiredState = LaserStateNormal;

            if (Math.Abs(laser.ai[1] - desiredState) > 0.01f)
            {
                laser.ai[1] = desiredState;
                laser.netUpdate = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            base.PreDraw(ref lightColor);

            if (Main.dedServ)
                return false;

            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Texture2D ring = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomRing").Value;
            Vector2 drawPosition = Muzzle - Main.screenPosition;
            Color orange = new Color(255, 112, 34, 0);
            Color gold = new Color(255, 218, 94, 0);
            float charge = ChargeRatio;
            float pulse = 1f + (float)Math.Sin(HoldFrameCounter * 0.22f) * 0.08f;

            Main.EntitySpriteDraw(bloom, drawPosition, null, orange * (0.28f + charge * 0.56f), Projectile.rotation, bloom.Size() * 0.5f, (0.12f + charge * 0.24f) * pulse, SpriteEffects.None);
            Main.EntitySpriteDraw(bloom, drawPosition, null, Color.White with { A = 0 } * (0.08f + charge * 0.28f), Projectile.rotation, bloom.Size() * 0.5f, (0.04f + charge * 0.08f) * pulse, SpriteEffects.None);
            Main.EntitySpriteDraw(ring, drawPosition, null, gold * charge * 0.58f, Main.GlobalTimeWrappedHourly * 1.7f, ring.Size() * 0.5f, (0.12f + charge * 0.22f) * pulse, SpriteEffects.None);

            // When fully charged, add extra converged/scattered visual indicator
            if (charge >= 1f)
            {
                bool scattered = IsLeftHeld();
                Color stateColor = scattered ? new Color(180, 220, 255, 0) : new Color(255, 240, 150, 0);
                float statePulse = 1f + (float)Math.Sin(HoldFrameCounter * 0.45f) * 0.15f;
                Main.EntitySpriteDraw(ring, drawPosition, null, stateColor * 0.72f, -Main.GlobalTimeWrappedHourly * 2.4f, ring.Size() * 0.5f, 0.28f * statePulse, SpriteEffects.None);
            }

            return false;
        }

        // Only maintain the laser while the right-click holdout is alive
        private void MaintainMainLaser()
        {
            if (Projectile.owner != Main.myPlayer)
                return;

            int laserType = ModContent.ProjectileType<YC_RightScorchingLaser>();
            if (IsProjectileActive(laserIndex, laserType))
                return;

            laserIndex = Projectile.NewProjectile(
                Projectile.GetSource_FromThis(),
                Muzzle,
                ForwardDirection,
                laserType,
                Projectile.damage,
                Projectile.knockBack,
                Projectile.owner,
                Projectile.whoAmI); // ai[0] >= 0: attached mode

            if (Main.projectile.IndexInRange(laserIndex))
            {
                YharimsCrystalHellBladeGlobalProjectile.Mark(Main.projectile[laserIndex], YCWeaponForm.Crystal);
                Main.projectile[laserIndex].CritChance = Projectile.CritChance;
            }
        }

        private void MaintainEmpoweredShard()
        {
            bool empowered = Owner.GetModPlayer<YharimsCrystalStatePlayer>().CrystalEmpowered;
            int shardType = ModContent.ProjectileType<YC_BurningShard>();

            if (!empowered)
            {
                KillProjectile(shardIndex);
                shardIndex = -1;
                return;
            }

            if (Projectile.owner != Main.myPlayer || IsProjectileActive(shardIndex, shardType))
                return;

            shardIndex = Projectile.NewProjectile(
                Projectile.GetSource_FromThis(),
                Muzzle + ForwardDirection * 22f,
                Vector2.Zero,
                shardType,
                Math.Max(1, (int)(Projectile.damage * 0.38f)),
                Projectile.knockBack * 0.2f,
                Projectile.owner,
                1f,
                Projectile.whoAmI);

            if (Main.projectile.IndexInRange(shardIndex))
            {
                YharimsCrystalHellBladeGlobalProjectile.Mark(Main.projectile[shardIndex], YCWeaponForm.Crystal);
                Main.projectile[shardIndex].CritChance = Projectile.CritChance;
            }
        }

        private void MaintainDrones()
        {
            if (Projectile.owner != Main.myPlayer)
                return;

            int droneType = ModContent.ProjectileType<YC_RightDrone>();
            for (int slot = 0; slot < 6; slot++)
            {
                if (IsProjectileActive(droneIndices[slot], droneType))
                    continue;

                droneIndices[slot] = Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    Owner.Center,
                    Vector2.Zero,
                    droneType,
                    Projectile.damage,
                    Projectile.knockBack,
                    Projectile.owner,
                    slot,
                    Projectile.whoAmI);

                if (Main.projectile.IndexInRange(droneIndices[slot]))
                {
                    YharimsCrystalHellBladeGlobalProjectile.Mark(Main.projectile[droneIndices[slot]], YCWeaponForm.Crystal);
                    Main.projectile[droneIndices[slot]].CritChance = Projectile.CritChance;
                }
            }
        }

        private void EmitChargeFX()
        {
            if (Main.dedServ)
                return;

            float charge = ChargeRatio;
            int interval = Math.Max(2, (int)MathHelper.Lerp(8f, 2f, charge));
            if (Main.GameUpdateCount % interval == 0)
            {
                Vector2 direction = ForwardDirection;
                Vector2 normal = direction.RotatedBy(MathHelper.PiOver2);
                Vector2 spawn = Muzzle + normal * Main.rand.NextFloat(-36f, 36f) - direction * Main.rand.NextFloat(18f, 74f);
                Vector2 velocity = direction * Main.rand.NextFloat(2f, 6f + charge * 6f) + normal * Main.rand.NextFloat(-1.1f, 1.1f);
                Color color = Main.rand.NextBool(4) ? Color.White : Color.Lerp(new Color(255, 72, 34), new Color(255, 222, 94), Main.rand.NextFloat(0.1f, 0.9f));

                GeneralParticleHandler.SpawnParticle(new CustomSpark(
                    spawn,
                    velocity,
                    "CalamityMod/Particles/BloomCircle",
                    false,
                    Main.rand.Next(10, 18),
                    Main.rand.NextFloat(0.22f, 0.46f) * (0.55f + charge),
                    color,
                    new Vector2(0.72f, 1.15f),
                    shrinkSpeed: 0.85f));
            }

            if ((int)HoldFrameCounter % 24 == 0)
                SoundEngine.PlaySound(SoundID.Item34 with { Volume = 0.16f + charge * 0.14f, Pitch = -0.28f, PitchVariance = 0.18f, MaxInstances = 5 }, Muzzle);
        }

        private static bool IsProjectileActive(int index, int type)
        {
            return index >= 0 &&
                index < Main.maxProjectiles &&
                Main.projectile[index].active &&
                Main.projectile[index].type == type;
        }

        private static void KillProjectile(int index)
        {
            if (index >= 0 && index < Main.maxProjectiles && Main.projectile[index].active)
                Main.projectile[index].Kill();
        }

        protected override void UpdateHoldout()
        {
            Vector2 holdoutCenter = Owner.RotatedRelativePoint(Owner.MountedCenter, true);

            if (Projectile.owner == Main.myPlayer)
            {
                Vector2 targetAim = (NewLegendYharimsCrystal.GetMouseWorld(Owner) - holdoutCenter).SafeNormalize(Vector2.UnitX * Owner.direction);

                if (Projectile.velocity == Vector2.Zero)
                {
                    Projectile.velocity = targetAim;
                }
                else
                {
                    // A focus command is a true cursor lock. The core beam and every drone round
                    // therefore share the same point instead of merely turning toward it at different speeds.
                    if (IsFocusMode)
                    {
                        if (Projectile.velocity != targetAim)
                            Projectile.netUpdate = true;

                        Projectile.velocity = targetAim;
                    }
                    else
                    {
                    // Turn rate: normal = 2 deg/frame, charged = 0.8 deg/frame.
                    float maxTurnDeg;
                    if (Charged)
                        maxTurnDeg = 0.8f;
                    else
                        maxTurnDeg = 2f;

                    float maxTurn = MathHelper.ToRadians(maxTurnDeg);
                    float currentAngle = Projectile.velocity.ToRotation();
                    float targetAngle = targetAim.ToRotation();
                    float newAngle = currentAngle.AngleTowards(targetAngle, maxTurn);
                    Vector2 newAim = newAngle.ToRotationVector2();

                    if (newAim != Projectile.velocity)
                        Projectile.netUpdate = true;

                    Projectile.velocity = newAim;
                    }
                }
            }

            Projectile.Center = holdoutCenter + ForwardDirection * HoldoutDistance;
            Projectile.rotation = ForwardDirection.ToRotation() + MathHelper.PiOver2;
            Projectile.direction = Projectile.velocity.X >= 0f ? 1 : -1;
            Projectile.spriteDirection = Projectile.direction;

            Owner.ChangeDir(Projectile.direction);
            Owner.heldProj = Projectile.whoAmI;
            Owner.itemTime = 2;
            Owner.itemAnimation = 2;
            Owner.itemRotation = (Projectile.velocity * Projectile.direction).ToRotation();

            float armRotation = (Projectile.rotation - MathHelper.PiOver2) * Owner.gravDir;
            if (Owner.gravDir == -1f)
                armRotation += MathHelper.Pi;

            Owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, armRotation);
            Projectile.timeLeft = 2;

            if (Projectile.soundDelay <= 0 && HoldFrameCounter > 1f)
            {
                Projectile.soundDelay = 22;
                SoundEngine.PlaySound(SoundID.Item15 with { Volume = 0.13f, Pitch = SoundPitch, MaxInstances = 4 }, Projectile.Center);
            }
        }
    }
}
