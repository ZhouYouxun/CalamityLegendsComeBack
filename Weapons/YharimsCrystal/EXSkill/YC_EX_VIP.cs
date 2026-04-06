using System.Collections.Generic;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using CalamityLegendsComeBack.Weapons.YharimsCrystal;

namespace CalamityLegendsComeBack.Weapons.YharimsCrystal.EXSkill
{
    public class YC_EX_VIP : ModProjectile, ILocalizedModType
    {
        public enum EXVipState
        {
            Summoning,
            DroneCharge,
            AwaitingFireCommand,
            Firing,
            Cleanup
        }

        public const int DroneTotal = 7;
        public const int DroneChargeTime = 120;
        public const int LaserChargeTime = 10 * 60;
        public const int LaserFireTime = 15 * 60;
        private const int SpawnInterval = 15;
        private const int CleanupInterval = 18;

        private readonly int[] rainbowSpawnOrder = new int[DroneTotal];
        private bool spawnOrderInitialized;

        private ref float State => ref Projectile.ai[0];
        private ref float StateTimer => ref Projectile.ai[1];

        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";
        public new string LocalizationCategory => "Projectiles";

        public EXVipState CurrentState => (EXVipState)(int)State;
        public int CurrentStateTimer => (int)StateTimer;

        public override void SetDefaults()
        {
            Projectile.width = 18;
            Projectile.height = 18;
            Projectile.friendly = false;
            Projectile.hostile = false;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.hide = true;
            Projectile.netImportant = true;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.timeLeft = 2;
        }

        public override bool ShouldUpdatePosition() => false;
        public override bool? CanDamage() => false;

        public override void OnSpawn(IEntitySource source)
        {
            InitializeSpawnOrder();
            State = (int)EXVipState.Summoning;
            StateTimer = 0f;
        }

        public override void AI()
        {
            Player owner = Main.player[Projectile.owner];
            if (!owner.active || owner.dead)
            {
                Projectile.Kill();
                return;
            }

            if (!spawnOrderInitialized)
                InitializeSpawnOrder();

            Projectile.Center = owner.Center;
            Projectile.velocity = Vector2.Zero;
            Projectile.timeLeft = 2;

            switch (CurrentState)
            {
                case EXVipState.Summoning:
                    DoSummoningState(owner);
                    break;
                case EXVipState.DroneCharge:
                    DoDroneChargeState(owner);
                    break;
                case EXVipState.AwaitingFireCommand:
                    DoAwaitFireState(owner);
                    break;
                case EXVipState.Firing:
                    DoFiringState(owner);
                    break;
                case EXVipState.Cleanup:
                    DoCleanupState(owner);
                    break;
            }
        }

        public override void OnKill(int timeLeft)
        {
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile other = Main.projectile[i];
                if (!other.active || other.owner != Projectile.owner)
                    continue;

                if (other.type == ModContent.ProjectileType<YC_EX_Drone>())
                {
                    other.Kill();
                    continue;
                }

                if (other.type == ModContent.ProjectileType<YC_CBeam>() &&
                    (YC_CBeam.BeamAnchorKind)(int)other.ai[1] == YC_CBeam.BeamAnchorKind.ExDrone)
                {
                    other.Kill();
                }
            }
        }

        private void DoSummoningState(Player owner)
        {
            StateTimer++;
            EmitSummonFX(owner);

            if (Projectile.owner != Main.myPlayer)
                return;

            if (CurrentStateTimer % SpawnInterval == 0 && CountOwnedDrones() < DroneTotal)
                SpawnDrone(owner, CountOwnedDrones());

            if (CountOwnedDrones() >= DroneTotal)
                SetState(EXVipState.DroneCharge);
        }

        private void DoDroneChargeState(Player owner)
        {
            StateTimer++;
            EmitChargeFX(owner, CurrentStateTimer / (float)DroneChargeTime);

            if (Projectile.owner == Main.myPlayer && CurrentStateTimer >= DroneChargeTime)
            {
                SoundEngine.PlaySound(SoundID.Item122 with { Volume = 0.45f, Pitch = -0.1f }, owner.Center);
                SetState(EXVipState.AwaitingFireCommand);
            }
        }

        private void DoAwaitFireState(Player owner)
        {
            StateTimer++;
            EmitReadyFX(owner);

            if (Projectile.owner == Main.myPlayer &&
                Main.mouseLeft &&
                Main.mouseLeftRelease &&
                !Main.mapFullscreen &&
                !Main.blockMouse)
            {
                SetState(EXVipState.Firing);
            }
        }

        private void DoFiringState(Player owner)
        {
            StateTimer++;
            EmitFiringFX(owner, CurrentStateTimer);

            if (Projectile.owner == Main.myPlayer && CurrentStateTimer >= LaserChargeTime + LaserFireTime)
                SetState(EXVipState.Cleanup);
        }

        private void DoCleanupState(Player owner)
        {
            StateTimer++;

            if (Projectile.owner != Main.myPlayer)
                return;

            if (CurrentStateTimer % CleanupInterval == 0)
            {
                if (!TryKillNextDrone())
                    Projectile.Kill();
            }
        }

        private void InitializeSpawnOrder()
        {
            spawnOrderInitialized = true;
            for (int i = 0; i < DroneTotal; i++)
                rainbowSpawnOrder[i] = i;

            if (Projectile.owner != Main.myPlayer)
                return;

            for (int i = DroneTotal - 1; i > 0; i--)
            {
                int swapIndex = Main.rand.Next(i + 1);
                (rainbowSpawnOrder[i], rainbowSpawnOrder[swapIndex]) = (rainbowSpawnOrder[swapIndex], rainbowSpawnOrder[i]);
            }
        }

        private void SpawnDrone(Player owner, int slotIndex)
        {
            int colorIndex = rainbowSpawnOrder[slotIndex % DroneTotal];
            Projectile.NewProjectile(
                Projectile.GetSource_FromThis(),
                owner.Center,
                Vector2.Zero,
                ModContent.ProjectileType<YC_EX_Drone>(),
                Projectile.damage,
                Projectile.knockBack,
                Projectile.owner,
                slotIndex,
                colorIndex);

            SoundEngine.PlaySound(SoundID.Item15 with { Volume = 0.2f, Pitch = 0.15f + slotIndex * 0.02f }, owner.Center);
        }

        private int CountOwnedDrones()
        {
            int count = 0;
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile other = Main.projectile[i];
                if (other.active &&
                    other.owner == Projectile.owner &&
                    other.type == ModContent.ProjectileType<YC_EX_Drone>())
                {
                    count++;
                }
            }

            return count;
        }

        private bool TryKillNextDrone()
        {
            List<Projectile> drones = new();
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile other = Main.projectile[i];
                if (other.active &&
                    other.owner == Projectile.owner &&
                    other.type == ModContent.ProjectileType<YC_EX_Drone>())
                {
                    drones.Add(other);
                }
            }

            drones.Sort((a, b) => a.ai[0].CompareTo(b.ai[0]));
            if (drones.Count <= 0)
                return false;

            drones[0].Kill();
            return true;
        }

        private void SetState(EXVipState newState)
        {
            State = (int)newState;
            StateTimer = 0f;
            Projectile.netUpdate = true;
        }

        private void EmitSummonFX(Player owner)
        {
            if (Main.dedServ || Main.GameUpdateCount % 6 != 0)
                return;

            Vector2 spawnPos = owner.Center + Main.rand.NextVector2Circular(14f, 14f);
            Dust dust = Dust.NewDustPerfect(
                spawnPos,
                DustID.GoldFlame,
                Main.rand.NextVector2Circular(0.4f, 0.4f),
                0,
                new Color(255, 210, 130),
                Main.rand.NextFloat(0.9f, 1.2f));
            dust.noGravity = true;
        }

        private void EmitChargeFX(Player owner, float progress)
        {
            if (Main.dedServ)
                return;

            if (Main.GameUpdateCount % 5 == 0)
            {
                float radius = MathHelper.Lerp(22f, 38f, MathHelper.Clamp(progress, 0f, 1f));
                Vector2 orbitOffset = Main.rand.NextVector2CircularEdge(radius, radius);

                GlowOrbParticle glow = new GlowOrbParticle(
                    owner.Center + orbitOffset,
                    -orbitOffset.SafeNormalize(Vector2.UnitY) * Main.rand.NextFloat(1f, 2.2f),
                    false,
                    Main.rand.Next(16, 22),
                    Main.rand.NextFloat(0.45f, 0.7f),
                    Color.Lerp(new Color(255, 175, 90), Color.White, Main.rand.NextFloat(0.2f, 0.7f)),
                    true,
                    false,
                    true);
                GeneralParticleHandler.SpawnParticle(glow);
            }
        }

        private void EmitReadyFX(Player owner)
        {
            if (Main.dedServ || Main.GameUpdateCount % 16 != 0)
                return;

            DirectionalPulseRing pulse = new DirectionalPulseRing(
                owner.Center,
                Vector2.UnitY,
                new Color(255, 220, 150) * 0.9f,
                new Vector2(1f, 1.25f),
                0f,
                0.08f,
                0.03f,
                18);
            GeneralParticleHandler.SpawnParticle(pulse);
        }

        private void EmitFiringFX(Player owner, int timer)
        {
            if (Main.dedServ || timer % 10 != 0)
                return;

            for (int i = 0; i < 2; i++)
            {
                Vector2 direction = Main.rand.NextVector2Unit();
                SquishyLightParticle spark = new SquishyLightParticle(
                    owner.Center + direction * Main.rand.NextFloat(18f, 30f),
                    direction * Main.rand.NextFloat(0.8f, 1.8f),
                    Main.rand.NextFloat(0.22f, 0.32f),
                    Color.Lerp(new Color(255, 205, 120), Color.White, Main.rand.NextFloat(0.2f, 0.6f)),
                    Main.rand.Next(12, 18));
                GeneralParticleHandler.SpawnParticle(spark);
            }
        }
    }
}
