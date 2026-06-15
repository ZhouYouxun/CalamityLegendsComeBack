using System;
using CalamityMod;
using Microsoft.Xna.Framework;
using Terraria.DataStructures;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Dev.HyperdimensionalMatrixCore
{
    internal static class MatrixModuleNumbers
    {
        // Module cooldowns (frames)
        public const int DataGridCooldown    = 280;
        public const int GeoBurstCooldown    = 220;
        public const int ShaderOrbsCooldown  = 340;
        public const int FusionCooldown      = 400;
        public const int SpaceWarpCooldown   = 560;

        public const int CompileStormInterval   = 900;
        public const float TargetingRange       = 2400f;
        public const float SpaceWarpMinNPCSize  = 80f;

        // Damage multipliers
        public const float DataGridDamage   = 0.18f;
        public const float GeoBurstDamage   = 0.52f;
        public const float ShaderOrbDamage  = 0.38f;
        public const float FusionDamage     = 1.85f;
        public const float SpaceWarpDamage  = 0.26f;
        public const float CompileStormDmg  = 0.20f;
    }

    public sealed class HyperdimensionalMatrixCoreProjectile : ModProjectile, ILocalizedModType
    {
        private const int ModuleCount = 5;

        private readonly int[] moduleCooldowns = new int[ModuleCount];
        private int compileStormTimer;
        private int lastFiredModule = -1;
        private int targetRefreshTimer;
        private int idleTimer;

        public new string LocalizationCategory => "Projectiles.HyperdimensionalMatrixCore";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        // ai[1] encodes the target index + 1 (0 = no target)
        public int TargetIndex => (int)Projectile.ai[1] - 1;

        public override void SetStaticDefaults()
        {
            Main.projPet[Type] = true;
            ProjectileID.Sets.MinionSacrificable[Type] = false;
            ProjectileID.Sets.MinionTargettingFeature[Type] = true;
            ProjectileID.Sets.DrawScreenCheckFluff[Type] = 2800;
        }

        public override void SetDefaults()
        {
            Projectile.width = 48;
            Projectile.height = 48;
            Projectile.netImportant = true;
            Projectile.friendly = true;
            Projectile.ignoreWater = true;
            Projectile.minion = true;
            Projectile.minionSlots = 1f;
            Projectile.timeLeft = 18000;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.DamageType = DamageClass.Summon;
        }

        public override bool? CanDamage() => false;

        public override void AI()
        {
            Player owner = Main.player[Projectile.owner];
            if (!owner.active || owner.dead || !owner.HasBuff<HyperdimensionalMatrixCoreBuff>())
            {
                Projectile.Kill();
                return;
            }

            Projectile.timeLeft = 2;
            Projectile.minionSlots = Math.Max(1f, owner.maxMinions);

            if (Main.myPlayer == Projectile.owner && (Main.GameUpdateCount + Projectile.identity) % 30 == 0)
                HyperdimensionalMatrixCore.RemoveOtherSlotConsumingMinions(owner, Type);

            UpdatePosition(owner);
            UpdateDamage(owner);
            UpdateTarget(owner);
            TickCooldowns();

            idleTimer++;

            NPC target = GetTarget();
            if (Main.myPlayer == Projectile.owner)
                TryFireModules(target);

            Lighting.AddLight(Projectile.Center, HyperdimensionalMatrixVisuals.GetDataColor(0.25f).ToVector3() * 0.5f);
        }

        public NPC GetTarget()
        {
            if (!Main.npc.IndexInRange(TargetIndex))
                return null;

            NPC npc = Main.npc[TargetIndex];
            return npc.CanBeChasedBy(Projectile, false) ? npc : null;
        }

        public static float GetSlotDamageMultiplier(Player owner)
        {
            int slots = Math.Max(1, owner.maxMinions);
            return 1f + (slots - 1) * 0.5f;
        }

        private void UpdatePosition(Player owner)
        {
            float t = Main.GlobalTimeWrappedHourly;
            Vector2 hover = owner.Top + new Vector2(
                (float)Math.Sin(t * 1.5f + Projectile.identity * 0.1f) * 9f,
                -72f + (float)Math.Sin(t * 2.2f) * 5f);

            if (Vector2.DistanceSquared(Projectile.Center, hover) > 900f * 900f)
            {
                Projectile.Center = hover;
                Projectile.netUpdate = true;
            }
            else
            {
                Projectile.Center = Vector2.Lerp(Projectile.Center, hover, 0.24f);
            }

            Projectile.velocity = Vector2.Zero;
            Projectile.rotation = t;
        }

        private void UpdateDamage(Player owner)
        {
            int base_ = Projectile.originalDamage > 0
                ? Projectile.originalDamage
                : HyperdimensionalMatrixCore.BaseDamage;
            Projectile.damage = Math.Max(1, (int)owner
                .GetTotalDamage(DamageClass.Summon)
                .ApplyTo(base_ * GetSlotDamageMultiplier(owner)));
        }

        private void UpdateTarget(Player owner)
        {
            if (Main.myPlayer != Projectile.owner)
                return;

            targetRefreshTimer--;
            if (targetRefreshTimer > 0 && GetTarget() != null)
                return;

            targetRefreshTimer = 12;
            NPC found = FindTarget(owner);
            int encoded = found?.whoAmI + 1 ?? 0;
            if ((int)Projectile.ai[1] != encoded)
            {
                Projectile.ai[1] = encoded;
                Projectile.netUpdate = true;
            }
        }

        private NPC FindTarget(Player owner)
        {
            if (owner.HasMinionAttackTargetNPC && Main.npc.IndexInRange(owner.MinionAttackTargetNPC))
            {
                NPC designated = Main.npc[owner.MinionAttackTargetNPC];
                if (designated.CanBeChasedBy(Projectile, false) &&
                    Vector2.Distance(owner.Center, designated.Center) <= MatrixModuleNumbers.TargetingRange * 1.35f)
                    return designated;
            }

            NPC closest = null;
            float closestDist = MatrixModuleNumbers.TargetingRange;
            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!npc.CanBeChasedBy(Projectile, false))
                    continue;

                float dist = Vector2.Distance(owner.Center, npc.Center);
                if (dist < closestDist)
                {
                    closestDist = dist;
                    closest = npc;
                }
            }

            return closest;
        }

        private void TickCooldowns()
        {
            for (int i = 0; i < ModuleCount; i++)
                if (moduleCooldowns[i] > 0) moduleCooldowns[i]--;

            compileStormTimer++;
        }

        private void TryFireModules(NPC target)
        {
            // Compile Storm fires independently on a long interval
            if (compileStormTimer >= MatrixModuleNumbers.CompileStormInterval && target != null)
            {
                FireCompileStorm(target);
                compileStormTimer = 0;
                // Reset all modules so storm carries uninterrupted momentum
                for (int i = 0; i < ModuleCount; i++)
                    moduleCooldowns[i] = 240;
                return;
            }

            if (target == null)
                return;

            // Rotate through modules; fire the first ready one
            for (int offset = 0; offset < ModuleCount; offset++)
            {
                int m = (lastFiredModule + 1 + offset) % ModuleCount;
                if (!ModuleReady(m, target))
                    continue;

                FireModule(m, target);
                lastFiredModule = m;
                break;
            }
        }

        private bool ModuleReady(int m, NPC target)
        {
            if (moduleCooldowns[m] > 0)
                return false;

            // Space Warp only fires vs boss-sized enemies
            if (m == 4 && Math.Max(target.width, target.height) < MatrixModuleNumbers.SpaceWarpMinNPCSize)
                return false;

            return true;
        }

        private void FireModule(int m, NPC target)
        {
            IEntitySource src = Projectile.GetSource_FromThis();
            int dmg = Projectile.damage;

            switch (m)
            {
                case 0: // Data Grid
                    Projectile.NewProjectile(src, target.Center, Vector2.Zero,
                        ModContent.ProjectileType<MatrixDataGridPanel>(),
                        Math.Max(1, (int)(dmg * MatrixModuleNumbers.DataGridDamage)),
                        Projectile.knockBack, Projectile.owner,
                        Projectile.whoAmI, target.whoAmI);
                    moduleCooldowns[0] = MatrixModuleNumbers.DataGridCooldown;
                    SoundEngine.PlaySound(SoundID.Item117 with { Volume = 0.36f, Pitch = 0.24f, MaxInstances = 3 }, Projectile.Center);
                    break;

                case 1: // Geometry Burst
                    Projectile.NewProjectile(src, Projectile.Center, Vector2.Zero,
                        ModContent.ProjectileType<MatrixGeoBurst>(),
                        Math.Max(1, (int)(dmg * MatrixModuleNumbers.GeoBurstDamage)),
                        Projectile.knockBack, Projectile.owner,
                        Projectile.whoAmI, target.whoAmI);
                    moduleCooldowns[1] = MatrixModuleNumbers.GeoBurstCooldown;
                    SoundEngine.PlaySound(SoundID.Item29 with { Volume = 0.30f, Pitch = 0.08f, MaxInstances = 3 }, Projectile.Center);
                    break;

                case 2: // Shader Orbs (5 types)
                    for (int i = 0; i < 5; i++)
                    {
                        Projectile.NewProjectile(src, target.Center, Vector2.Zero,
                            ModContent.ProjectileType<MatrixShaderOrb>(),
                            Math.Max(1, (int)(dmg * MatrixModuleNumbers.ShaderOrbDamage)),
                            Projectile.knockBack, Projectile.owner,
                            i, target.whoAmI);
                    }
                    moduleCooldowns[2] = MatrixModuleNumbers.ShaderOrbsCooldown;
                    SoundEngine.PlaySound(SoundID.Item103 with { Volume = 0.30f, Pitch = -0.12f, MaxInstances = 3 }, target.Center);
                    break;

                case 3: // Metaball Fusion
                    Projectile.NewProjectile(src, target.Center, Vector2.Zero,
                        ModContent.ProjectileType<MatrixFusionController>(),
                        Math.Max(1, (int)(dmg * MatrixModuleNumbers.FusionDamage)),
                        Projectile.knockBack, Projectile.owner,
                        Projectile.whoAmI, target.whoAmI);
                    moduleCooldowns[3] = MatrixModuleNumbers.FusionCooldown;
                    SoundEngine.PlaySound(SoundID.Item20 with { Volume = 0.26f, Pitch = -0.32f, MaxInstances = 3 }, target.Center);
                    break;

                case 4: // Space Warp
                    Projectile.NewProjectile(src, target.Center, Vector2.Zero,
                        ModContent.ProjectileType<MatrixSpaceWarpField>(),
                        Math.Max(1, (int)(dmg * MatrixModuleNumbers.SpaceWarpDamage)),
                        Projectile.knockBack, Projectile.owner,
                        Projectile.whoAmI, target.whoAmI);
                    moduleCooldowns[4] = MatrixModuleNumbers.SpaceWarpCooldown;
                    SoundEngine.PlaySound(SoundID.Item122 with { Volume = 0.22f, Pitch = -0.52f, MaxInstances = 2 }, target.Center);
                    break;
            }
        }

        private void FireCompileStorm(NPC target)
        {
            Projectile.NewProjectile(
                Projectile.GetSource_FromThis(),
                Projectile.Center, Vector2.Zero,
                ModContent.ProjectileType<MatrixCompileStorm>(),
                Projectile.damage,
                Projectile.knockBack,
                Projectile.owner,
                Projectile.whoAmI, target.whoAmI);

            SoundEngine.PlaySound(SoundID.Item88 with { Volume = 0.6f, Pitch = -0.4f, MaxInstances = 1 }, Projectile.Center);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            float t = Main.GlobalTimeWrappedHourly;
            Player owner = Main.player[Projectile.owner];

            // Layer 1: player orbit shield
            HyperdimensionalMatrixVisuals.DrawShield(owner, 0.60f);

            // Layer 2: icosahedron wire-frame rotating around core
            HyperdimensionalMatrixVisuals.DrawGeometry(
                Projectile.Center, MatrixGeometryShape.Icosahedron,
                54f, t * 0.80f, 0.55f, Projectile.identity);

            // Layer 3: cube rotating opposite direction (smaller)
            HyperdimensionalMatrixVisuals.DrawGeometry(
                Projectile.Center, MatrixGeometryShape.Cube,
                34f, -t * 1.18f + 0.42f, 0.42f, Projectile.identity + 7, false);

            // Layer 4: hypercube at the core center
            HyperdimensionalMatrixVisuals.DrawHypercube(Projectile.Center, 26f, t, 0.88f);

            // Rune rings
            HyperdimensionalMatrixVisuals.DrawScanRing(
                Projectile.Center, 74f, t * 0.48f,
                HyperdimensionalMatrixVisuals.GetDataColor(0.18f, 0.36f), 32, 1.4f);
            HyperdimensionalMatrixVisuals.DrawScanRing(
                Projectile.Center, 90f, -t * 0.32f,
                HyperdimensionalMatrixVisuals.GetDataColor(0.62f, 0.26f), 24, 1.0f);

            // Targeting line
            NPC target = GetTarget();
            if (target != null)
                HyperdimensionalMatrixVisuals.DrawTargetingLine(Projectile.Center, target.Center, 0.28f);

            // Idle orbiting data blocks
            for (int i = 0; i < 14; i++)
            {
                float angle = MathHelper.TwoPi * i / 14f + t * (0.55f + i % 3 * 0.07f);
                float r = 60f + (float)Math.Sin(t * 2.6f + i * 0.72f) * 10f;
                Vector2 pos = Projectile.Center + angle.ToRotationVector2() * r;
                float blink = 0.45f + 0.55f * (float)Math.Sin(t * 5f + i * 1.27f);
                HyperdimensionalMatrixVisuals.DrawNode(pos,
                    HyperdimensionalMatrixVisuals.GetDataColor(i * 0.071f, 0.52f * blink),
                    3f + i % 3);
            }

            return false;
        }
    }
}
