using CalamityLegendsComeBack.Accssory.BF.SeedOfSilva;
using CalamityLegendsComeBack.Weapons.BlossomFlux;
using CalamityLegendsComeBack.Weapons.BlossomFlux.Chloroplast;
using CalamityLegendsComeBack.Weapons.BlossomFlux.LeafProj;
using CalamityLegendsComeBack.Weapons.BlossomFlux.SpecialArrow;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Accssory.BF.Common
{
    internal static class BFAccessorySystem
    {
        public static bool TryGetBlossomFluxPreset(Projectile projectile, out BlossomFluxChloroplastPresetType preset)
        {
            preset = BlossomFluxChloroplastPresetType.Chlo_ABreak;

            if (projectile.GetGlobalProjectile<BFAccessoryGlobalProjectile>().BlossomFluxArrow)
            {
                preset = projectile.GetGlobalProjectile<BFAccessoryGlobalProjectile>().Preset;
                return true;
            }

            BFArrow_CDetecEffect leftArrow = projectile.GetGlobalProjectile<BFArrow_CDetecEffect>();
            if (leftArrow.BlossomFluxLeftArrow)
            {
                preset = leftArrow.Preset;
                return true;
            }

            int type = projectile.type;
            if (type == ModContent.ProjectileType<BFLeafProj>())
            {
                preset = projectile.ai[0] switch
                {
                    1f => BlossomFluxChloroplastPresetType.Chlo_BRecov,
                    2f => BlossomFluxChloroplastPresetType.Chlo_CDetec,
                    3f => BlossomFluxChloroplastPresetType.Chlo_DBomb,
                    _ => BlossomFluxChloroplastPresetType.Chlo_ABreak
                };
                return true;
            }

            if (type == ModContent.ProjectileType<BFArrow_ABreak>())
            {
                preset = BlossomFluxChloroplastPresetType.Chlo_ABreak;
                return true;
            }

            if (type == ModContent.ProjectileType<BFArrow_BRecov>() || type == ModContent.ProjectileType<BFArrow_BRecovTransfer>())
            {
                preset = BlossomFluxChloroplastPresetType.Chlo_BRecov;
                return true;
            }

            if (type == ModContent.ProjectileType<BFArrow_CDetec>())
            {
                preset = BlossomFluxChloroplastPresetType.Chlo_CDetec;
                return true;
            }

            if (type == ModContent.ProjectileType<BFArrow_DBomb>() || type == ModContent.ProjectileType<BFArrow_DBombVisual>() || type == ModContent.ProjectileType<FuckYou>())
            {
                preset = BlossomFluxChloroplastPresetType.Chlo_DBomb;
                return true;
            }

            if (type == ModContent.ProjectileType<BFArrow_EPlague>() || type == ModContent.ProjectileType<BFLeftPlagueReaper>() || type == ModContent.ProjectileType<BFArrow_EPlagueGas>())
            {
                preset = BlossomFluxChloroplastPresetType.Chlo_EPlague;
                return true;
            }

            if (type == ModContent.ProjectileType<BlossomFluxChloroplast>())
            {
                preset = projectile.ai[0] switch
                {
                    1f => BlossomFluxChloroplastPresetType.Chlo_BRecov,
                    2f => BlossomFluxChloroplastPresetType.Chlo_CDetec,
                    3f => BlossomFluxChloroplastPresetType.Chlo_DBomb,
                    4f => BlossomFluxChloroplastPresetType.Chlo_EPlague,
                    _ => BlossomFluxChloroplastPresetType.Chlo_ABreak
                };
                return true;
            }

            return false;
        }

    }

    internal sealed class BFAccessoryGlobalProjectile : GlobalProjectile
    {
        public override bool InstancePerEntity => true;

        public bool BlossomFluxArrow;
        public BlossomFluxChloroplastPresetType Preset;
        public bool PastLingeringArrow;
        private bool sunflowerEmpowered;
        private int torchflowerExplosionCooldown;

        public override void AI(Projectile projectile)
        {
            if (!projectile.friendly || !BFAccessorySystem.TryGetBlossomFluxPreset(projectile, out BlossomFluxChloroplastPresetType preset))
                return;

            Preset = preset;
            if (!BFArrowCommon.InBounds(projectile.owner, Main.maxPlayers))
                return;

            Player owner = Main.player[projectile.owner];
            BFAccessoryPlayer accessoryPlayer = owner.GetModPlayer<BFAccessoryPlayer>();

            if (accessoryPlayer.DominationQuiverEquipped)
                projectile.tileCollide = false;

            if (!accessoryPlayer.SeedOfSilvaEquipped)
                return;

            if (torchflowerExplosionCooldown > 0)
                torchflowerExplosionCooldown--;

            foreach (Projectile seedProjectile in Main.ActiveProjectiles)
            {
                if (!seedProjectile.active || seedProjectile.owner != owner.whoAmI || seedProjectile.type != ModContent.ProjectileType<SeedOfSilvaSeed>())
                    continue;

                if (seedProjectile.ModProjectile is not SeedOfSilvaSeed seed)
                    continue;

                float distanceSquared = Vector2.DistanceSquared(projectile.Center, seedProjectile.Center);
                if (preset == BlossomFluxChloroplastPresetType.Chlo_ABreak && seed.CurrentPreset == BlossomFluxChloroplastPresetType.Chlo_ABreak && distanceSquared <= 92f * 92f)
                {
                    sunflowerEmpowered = true;
                    if (!Main.dedServ && Main.rand.NextBool(10))
                    {
                        Dust dust = Dust.NewDustPerfect(projectile.Center, DustID.YellowTorch, Main.rand.NextVector2Circular(0.5f, 0.5f), 100, new Color(255, 238, 92), 0.82f);
                        dust.noGravity = true;
                    }
                }

                if (preset == BlossomFluxChloroplastPresetType.Chlo_DBomb && seed.CurrentPreset == BlossomFluxChloroplastPresetType.Chlo_DBomb && torchflowerExplosionCooldown <= 0 && distanceSquared <= 72f * 72f)
                {
                    torchflowerExplosionCooldown = 36;
                    if (projectile.owner == Main.myPlayer)
                    {
                        Projectile.NewProjectile(
                            projectile.GetSource_FromThis(),
                            seedProjectile.Center,
                            Vector2.Zero,
                            ModContent.ProjectileType<FuckYou>(),
                            System.Math.Max(1, (int)(projectile.damage * 0.55f)),
                            projectile.knockBack * 0.4f,
                            projectile.owner,
                            104f);
                    }
                }
            }
        }

        public override void ModifyHitNPC(Projectile projectile, NPC target, ref NPC.HitModifiers modifiers)
        {
            if (!BFAccessorySystem.TryGetBlossomFluxPreset(projectile, out BlossomFluxChloroplastPresetType preset))
                return;

            if (BFArrowCommon.InBounds(projectile.owner, Main.maxPlayers) && Main.player[projectile.owner].GetModPlayer<BFAccessoryPlayer>().DominationQuiverEquipped)
                modifiers.DefenseEffectiveness *= 0f;

            if (sunflowerEmpowered && preset == BlossomFluxChloroplastPresetType.Chlo_ABreak)
            {
                modifiers.FinalDamage *= 1.3f;
                if (Main.rand.NextFloat() < 0.15f)
                    modifiers.FinalDamage *= 1.5f;
            }
        }

        public override void OnHitNPC(Projectile projectile, NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (!PastLingeringArrow || projectile.owner != Main.myPlayer)
                return;

            int shardCount = Main.rand.Next(1, 3);
            for (int i = 0; i < shardCount; i++)
            {
                Vector2 velocity = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(7.5f, 11.5f);
                Projectile.NewProjectile(
                    projectile.GetSource_FromThis(),
                    projectile.Center + Main.rand.NextVector2Circular(8f, 8f),
                    velocity,
                    ModContent.ProjectileType<PastLingeringShard>(),
                    System.Math.Max(1, (int)(projectile.damage * 0.42f)),
                    projectile.knockBack * 0.35f,
                    projectile.owner,
                    target.whoAmI);
            }
        }
    }

    internal sealed class BFAccessoryGlobalNPC : GlobalNPC
    {
        private readonly int[] delphiniumStunTimers = new int[Main.maxPlayers];
        private readonly int[] mandrakeSlowTimers = new int[Main.maxPlayers];

        public override bool InstancePerEntity => true;

        public void ApplyDelphiniumStun(int owner, int time)
        {
            if (BFArrowCommon.InBounds(owner, Main.maxPlayers))
                delphiniumStunTimers[owner] = System.Math.Max(delphiniumStunTimers[owner], time);
        }

        public void ApplyMandrakeSlow(int owner, int time)
        {
            if (BFArrowCommon.InBounds(owner, Main.maxPlayers))
                mandrakeSlowTimers[owner] = System.Math.Max(mandrakeSlowTimers[owner], time);
        }

        public override void PostAI(NPC npc)
        {
            bool hardStunned = false;
            bool mandrakeSlowed = false;

            for (int i = 0; i < Main.maxPlayers; i++)
            {
                if (delphiniumStunTimers[i] > 0)
                {
                    delphiniumStunTimers[i]--;
                    hardStunned = true;
                }

                if (mandrakeSlowTimers[i] > 0)
                {
                    mandrakeSlowTimers[i]--;
                    mandrakeSlowed = true;
                }
            }

            if (hardStunned && npc.knockBackResist > 0f)
                npc.velocity *= 0.18f;
            else if (mandrakeSlowed && npc.knockBackResist > 0f)
                npc.velocity *= 0.82f;
        }

        public override void ModifyHitByProjectile(NPC npc, Projectile projectile, ref NPC.HitModifiers modifiers)
        {
            if (!BFAccessorySystem.TryGetBlossomFluxPreset(projectile, out BlossomFluxChloroplastPresetType preset) || preset != BlossomFluxChloroplastPresetType.Chlo_CDetec)
                return;

            if (!BFArrowCommon.InBounds(projectile.owner, Main.maxPlayers) || delphiniumStunTimers[projectile.owner] <= 0)
                return;

            modifiers.FinalDamage *= npc.knockBackResist > 0f ? 2f : 1.15f;
        }

        public override void ModifyHitPlayer(NPC npc, Player target, ref Player.HurtModifiers modifiers)
        {
            for (int i = 0; i < Main.maxPlayers; i++)
            {
                if (mandrakeSlowTimers[i] <= 0)
                    continue;

                modifiers.FinalDamage *= 0.85f;
                return;
            }
        }

        public override void DrawEffects(NPC npc, ref Color drawColor)
        {
            bool stunned = false;
            bool slowed = false;
            for (int i = 0; i < Main.maxPlayers; i++)
            {
                stunned |= delphiniumStunTimers[i] > 0;
                slowed |= mandrakeSlowTimers[i] > 0;
            }

            if (stunned)
            {
                drawColor = Color.Lerp(drawColor, new Color(114, 172, 255), 0.42f);
                Lighting.AddLight(npc.Center, new Vector3(0.06f, 0.12f, 0.32f));
            }
            else if (slowed)
            {
                drawColor = Color.Lerp(drawColor, new Color(170, 90, 208), 0.28f);
                Lighting.AddLight(npc.Center, new Vector3(0.12f, 0.04f, 0.18f));
            }
        }
    }
}
