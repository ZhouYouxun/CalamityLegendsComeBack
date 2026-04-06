using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using CalamityLegendsComeBack.Weapons.YharimsCrystal;

namespace CalamityLegendsComeBack.Weapons.YharimsCrystal.EXSkill
{
    public class YC_EX_Drone : ModProjectile, ILocalizedModType
    {
        private const float BaseOrbitRadius = 92f;
        private const float OrbitSpinSpeed = 1.35f;
        private const float OrbitFollowLerp = 0.28f;
        private bool positionInitialized;
        private bool readyBurstPlayed;
        private bool laserSpawned;
        private int previousVipState = -1;
        private float selfSpin;

        public new string LocalizationCategory => "Projectiles";
        public override string Texture => "CalamityLegendsComeBack/Weapons/YharimsCrystal/EXSkill/YC_EX_Drone";

        public int SlotIndex => (int)Projectile.ai[0];
        public int ColorIndex => (int)Projectile.ai[1];

        public static readonly Color[] RainbowPalette =
        {
            new(255, 96, 96),
            new(255, 154, 78),
            new(255, 220, 94),
            new(116, 235, 126),
            new(108, 196, 255),
            new(120, 138, 255),
            new(208, 118, 255)
        };

        public static Color GetDroneColor(int index) => RainbowPalette[Utils.Clamp(index, 0, RainbowPalette.Length - 1)];

        public override void SetDefaults()
        {
            Projectile.width = 34;
            Projectile.height = 34;
            Projectile.friendly = false;
            Projectile.hostile = false;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.hide = false;
            Projectile.netImportant = true;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.timeLeft = 2;
        }

        public override bool ShouldUpdatePosition() => false;
        public override bool? CanDamage() => false;

        public override void AI()
        {
            Player owner = Main.player[Projectile.owner];
            if (!owner.active || owner.dead)
            {
                Projectile.Kill();
                return;
            }

            if (!TryGetActiveVip(owner, out _, out YC_EX_VIP vip))
            {
                Projectile.Kill();
                return;
            }

            Projectile.timeLeft = 2;
            UpdateOrbit(owner, vip);
            HandleVipState(owner, vip);

            Lighting.AddLight(Projectile.Center, GetDroneColor(ColorIndex).ToVector3() * 0.45f);
        }

        public override void OnKill(int timeLeft)
        {
            KillOwnedLaser();

            if (Main.dedServ)
                return;

            Color color = GetDroneColor(ColorIndex);
            Vector2 outward = Projectile.velocity.SafeNormalize(Vector2.UnitY);

            for (int i = 0; i < 6; i++)
            {
                GlowOrbParticle glow = new GlowOrbParticle(
                    Projectile.Center,
                    outward.RotatedByRandom(0.8f) * Main.rand.NextFloat(1.4f, 3.4f),
                    false,
                    Main.rand.Next(10, 16),
                    Main.rand.NextFloat(0.32f, 0.54f),
                    Color.Lerp(color, Color.White, Main.rand.NextFloat(0.2f, 0.65f)),
                    true,
                    false,
                    true);
                GeneralParticleHandler.SpawnParticle(glow);
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Projectile.type].Value;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            Vector2 origin = texture.Size() * 0.5f;
            Color tint = GetDroneColor(ColorIndex);

            Main.EntitySpriteDraw(
                texture,
                drawPosition,
                null,
                Color.White,
                Projectile.rotation,
                origin,
                Projectile.scale,
                SpriteEffects.None,
                0);

            Main.EntitySpriteDraw(
                texture,
                drawPosition,
                null,
                tint * 0.7f,
                Projectile.rotation,
                origin,
                Projectile.scale * 1.18f,
                SpriteEffects.None,
                0);

            return false;
        }

        private void UpdateOrbit(Player owner, YC_EX_VIP vip)
        {
            float slotOffset = MathHelper.TwoPi * SlotIndex / YC_EX_VIP.DroneTotal;
            float pulse = 0f;

            if (vip.CurrentState == YC_EX_VIP.EXVipState.DroneCharge)
                pulse = (float)System.Math.Sin(vip.CurrentStateTimer / 12f) * 4f;
            else if (vip.CurrentState == YC_EX_VIP.EXVipState.Firing)
                pulse = (float)System.Math.Sin(Main.GlobalTimeWrappedHourly * 6f + SlotIndex * 0.4f) * 5f;

            float orbitRadius = BaseOrbitRadius + pulse;
            float orbitAngle = Main.GlobalTimeWrappedHourly * OrbitSpinSpeed + slotOffset;
            Vector2 desiredCenter = owner.Center + orbitAngle.ToRotationVector2() * orbitRadius;
            Vector2 outward = (desiredCenter - owner.Center).SafeNormalize(Vector2.UnitY);

            if (!positionInitialized)
            {
                Projectile.Center = desiredCenter;
                positionInitialized = true;
            }
            else
            {
                Projectile.Center = Vector2.Lerp(Projectile.Center, desiredCenter, OrbitFollowLerp);
            }

            Projectile.velocity = outward;
            selfSpin += 0.06f;
            Projectile.rotation = outward.ToRotation() + MathHelper.PiOver2 + selfSpin;
            Projectile.scale = 0.92f + 0.05f * (float)System.Math.Sin(Main.GlobalTimeWrappedHourly * 4.2f + SlotIndex * 0.8f);
        }

        private void HandleVipState(Player owner, YC_EX_VIP vip)
        {
            int currentState = (int)vip.CurrentState;
            if (currentState != previousVipState)
            {
                OnVipStateChanged(vip.CurrentState);
                previousVipState = currentState;
            }

            switch (vip.CurrentState)
            {
                case YC_EX_VIP.EXVipState.Summoning:
                    EmitSummonFX();
                    break;
                case YC_EX_VIP.EXVipState.DroneCharge:
                    EmitChargeFX(vip.CurrentStateTimer / (float)YC_EX_VIP.DroneChargeTime);
                    break;
                case YC_EX_VIP.EXVipState.AwaitingFireCommand:
                    EmitReadyFX();
                    break;
                case YC_EX_VIP.EXVipState.Firing:
                    HandleFiringState(owner, vip.CurrentStateTimer);
                    break;
                case YC_EX_VIP.EXVipState.Cleanup:
                    EmitCleanupFX();
                    break;
            }
        }

        private void OnVipStateChanged(YC_EX_VIP.EXVipState newState)
        {
            if (newState == YC_EX_VIP.EXVipState.AwaitingFireCommand && !readyBurstPlayed)
            {
                readyBurstPlayed = true;
                EmitReadyBurst();
                SoundEngine.PlaySound(SoundID.Item29 with { Volume = 0.2f, Pitch = -0.25f + SlotIndex * 0.04f }, Projectile.Center);
            }

            if (newState != YC_EX_VIP.EXVipState.Firing)
                laserSpawned = false;
        }

        private void HandleFiringState(Player owner, int timer)
        {
            if (timer < YC_EX_VIP.LaserChargeTime)
            {
                float chargeProgress = timer / (float)YC_EX_VIP.LaserChargeTime;
                EmitLaserChargeFX(chargeProgress);
                return;
            }

            if (!laserSpawned && Projectile.owner == Main.myPlayer)
            {
                laserSpawned = true;
                YC_CBeam.SpawnBeam(
                    Projectile.GetSource_FromThis(),
                    Projectile.Center + Projectile.velocity * 24f,
                    Projectile.velocity.SafeNormalize(Vector2.UnitY),
                    Projectile.damage,
                    Projectile.knockBack,
                    Projectile.owner,
                    Projectile.whoAmI,
                    YC_CBeam.BeamAnchorKind.ExDrone,
                    1600f,
                    16f,
                    YC_EX_VIP.LaserFireTime,
                    false,
                    false,
                    GetDroneColor(ColorIndex),
                    Color.White,
                    24f,
                    0.04f,
                    -1,
                    12);
                SoundEngine.PlaySound(SoundID.Item68 with { Volume = 0.4f, Pitch = -0.2f + ColorIndex * 0.03f }, Projectile.Center);
            }

            EmitBeamAnchorFX(owner, timer);
        }

        private void KillOwnedLaser()
        {
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile other = Main.projectile[i];
                if (!other.active || other.owner != Projectile.owner)
                    continue;

                if (other.type == ModContent.ProjectileType<YC_CBeam>() &&
                    (int)other.ai[0] == Projectile.whoAmI &&
                    (YC_CBeam.BeamAnchorKind)(int)other.ai[1] == YC_CBeam.BeamAnchorKind.ExDrone)
                {
                    other.Kill();
                }
            }
        }

        private bool TryGetActiveVip(Player owner, out Projectile vipProjectile, out YC_EX_VIP vip)
        {
            vipProjectile = null;
            vip = null;

            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile other = Main.projectile[i];
                if (!other.active || other.owner != owner.whoAmI || other.type != ModContent.ProjectileType<YC_EX_VIP>())
                    continue;

                if (other.ModProjectile is YC_EX_VIP vipProjectileMod)
                {
                    vipProjectile = other;
                    vip = vipProjectileMod;
                    return true;
                }
            }

            return false;
        }

        private void EmitSummonFX()
        {
            if (Main.dedServ || Main.GameUpdateCount % 10 != 0)
                return;

            Color color = GetDroneColor(ColorIndex);
            GlowOrbParticle glow = new GlowOrbParticle(
                Projectile.Center + Main.rand.NextVector2Circular(8f, 8f),
                Main.rand.NextVector2Circular(0.4f, 0.4f),
                false,
                10,
                0.28f,
                color,
                true,
                false,
                true);
            GeneralParticleHandler.SpawnParticle(glow);
        }

        private void EmitChargeFX(float progress)
        {
            if (Main.dedServ)
                return;

            if (Main.GameUpdateCount % 4 == 0)
            {
                Color color = GetDroneColor(ColorIndex);
                Vector2 inward = -Projectile.velocity.SafeNormalize(Vector2.UnitY);
                float ringRadius = 18f + progress * 10f;
                Vector2 spawnPosition = Projectile.Center + Main.rand.NextVector2CircularEdge(ringRadius, ringRadius);

                GlowOrbParticle glow = new GlowOrbParticle(
                    spawnPosition,
                    inward.RotatedByRandom(0.35f) * Main.rand.NextFloat(1.2f, 2.4f),
                    false,
                    Main.rand.Next(12, 18),
                    MathHelper.Lerp(0.25f, 0.48f, progress),
                    Color.Lerp(color, Color.White, 0.3f + progress * 0.35f),
                    true,
                    false,
                    true);
                GeneralParticleHandler.SpawnParticle(glow);
            }
        }

        private void EmitReadyFX()
        {
            if (Main.dedServ || Main.GameUpdateCount % 18 != 0)
                return;

            Color color = GetDroneColor(ColorIndex) * 0.85f;
            DirectionalPulseRing pulse = new DirectionalPulseRing(
                Projectile.Center,
                Projectile.velocity.SafeNormalize(Vector2.UnitY),
                color,
                new Vector2(1f, 1.35f),
                Projectile.rotation,
                0.08f,
                0.03f,
                14);
            GeneralParticleHandler.SpawnParticle(pulse);
        }

        private void EmitReadyBurst()
        {
            if (Main.dedServ)
                return;

            Color color = GetDroneColor(ColorIndex);
            Vector2 outward = Projectile.velocity.SafeNormalize(Vector2.UnitY);
            Vector2 tangent = outward.RotatedBy(MathHelper.PiOver2);

            DirectionalPulseRing pulse = new DirectionalPulseRing(
                Projectile.Center,
                outward,
                color,
                new Vector2(1f, 1.9f),
                outward.ToRotation(),
                0.14f,
                0.04f,
                18);
            GeneralParticleHandler.SpawnParticle(pulse);

            for (int i = 0; i < 7; i++)
            {
                float spread = i / 6f - 0.5f;
                Vector2 velocity = outward * Main.rand.NextFloat(2.6f, 4.2f) + tangent * spread * 3.8f;

                SquareParticle square = new SquareParticle(
                    Projectile.Center,
                    velocity,
                    false,
                    Main.rand.Next(18, 24),
                    Main.rand.NextFloat(0.85f, 1.15f),
                    Color.Lerp(color, Color.White, Main.rand.NextFloat(0.25f, 0.6f)));
                GeneralParticleHandler.SpawnParticle(square);
            }
        }

        private void EmitLaserChargeFX(float progress)
        {
            if (Main.dedServ)
                return;

            if (Main.GameUpdateCount % 3 == 0)
            {
                Color color = GetDroneColor(ColorIndex);
                Vector2 forward = Projectile.velocity.SafeNormalize(Vector2.UnitY);
                Vector2 spawnPosition = Projectile.Center + Main.rand.NextVector2Circular(6f, 6f);

                SquishyLightParticle spark = new SquishyLightParticle(
                    spawnPosition,
                    forward.RotatedByRandom(0.25f) * MathHelper.Lerp(1.1f, 3.1f, progress),
                    MathHelper.Lerp(0.18f, 0.32f, progress),
                    Color.Lerp(color, Color.White, 0.25f + progress * 0.45f),
                    Main.rand.Next(12, 18));
                GeneralParticleHandler.SpawnParticle(spark);
            }
        }

        private void EmitBeamAnchorFX(Player owner, int timer)
        {
            if (Main.dedServ || timer % 8 != 0)
                return;

            Color color = GetDroneColor(ColorIndex);
            Vector2 forward = Projectile.velocity.SafeNormalize((Projectile.Center - owner.Center).SafeNormalize(Vector2.UnitY));

            GlowOrbParticle glow = new GlowOrbParticle(
                Projectile.Center + forward * 12f,
                forward * Main.rand.NextFloat(0.5f, 1.2f),
                false,
                9,
                0.3f,
                Color.Lerp(color, Color.White, 0.45f),
                true,
                false,
                true);
            GeneralParticleHandler.SpawnParticle(glow);
        }

        private void EmitCleanupFX()
        {
            if (Main.dedServ || Main.GameUpdateCount % 12 != 0)
                return;

            Color color = GetDroneColor(ColorIndex);
            GlowOrbParticle glow = new GlowOrbParticle(
                Projectile.Center,
                Main.rand.NextVector2Circular(0.35f, 0.35f),
                false,
                8,
                0.22f,
                color * 0.8f,
                true,
                false,
                true);
            GeneralParticleHandler.SpawnParticle(glow);
        }
    }
}
