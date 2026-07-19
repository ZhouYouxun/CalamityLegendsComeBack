using CalamityLegendsComeBack.Accssory.BF.SeedOfSilva;
using CalamityLegendsComeBack.Weapons.BlossomFlux;
using CalamityLegendsComeBack.Weapons.BlossomFlux.LeftClick;
using CalamityLegendsComeBack.Weapons.BlossomFlux.RightClick;
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

            return false;
        }

    }

    internal sealed class BFAccessoryGlobalProjectile : GlobalProjectile
    {
        public override bool InstancePerEntity => true;

        public bool BlossomFluxArrow;
        public BlossomFluxChloroplastPresetType Preset;
        private bool sunflowerEmpowered;
        private bool sunflowerContactTriggered;
        private bool badSeedAttributesApplied;

        public override void AI(Projectile projectile)
        {
            if (!projectile.friendly || !BFAccessorySystem.TryGetBlossomFluxPreset(projectile, out BlossomFluxChloroplastPresetType preset))
                return;

            Preset = preset;
            if (!BFArrowCommon.InBounds(projectile.owner, Main.maxPlayers))
                return;

            Player owner = Main.player[projectile.owner];
            BFAccessoryPlayer accessoryPlayer = owner.GetModPlayer<BFAccessoryPlayer>();

            if (accessoryPlayer.HasBadSeedAttributes && !badSeedAttributesApplied)
            {
                badSeedAttributesApplied = true;
                projectile.CritChance += (int)(BFAccessoryPlayer.BadSeedBlossomFluxCritBonus * accessoryPlayer.BadSeedAttributeMultiplier);
            }

            if (accessoryPlayer.DominationQuiverEquipped)
                projectile.tileCollide = false;

            if (!accessoryPlayer.SeedOfSilvaEquipped)
                return;

            if (!accessoryPlayer.HoldingBlossomFlux)
                return;

            foreach (Projectile seedProjectile in Main.ActiveProjectiles)
            {
                if (!seedProjectile.active || seedProjectile.owner != owner.whoAmI)
                    continue;

                if (seedProjectile.ModProjectile is not SeedOfSilvaFlowerProjectile seed)
                    continue;

                float distanceSquared = Vector2.DistanceSquared(projectile.Center, seedProjectile.Center);
                if (preset == BlossomFluxChloroplastPresetType.Chlo_ABreak &&
                    projectile.GetGlobalProjectile<BFArrow_CDetecEffect>().BlossomFluxLeftArrow &&
                    seed.IsBlooming &&
                    seed.CurrentPreset == BlossomFluxChloroplastPresetType.Chlo_ABreak &&
                    distanceSquared <= SeedOfSilvaSunflower.SunflowerRingRadius * SeedOfSilvaSunflower.SunflowerRingRadius)
                {
                    sunflowerEmpowered = true;
                    if (!sunflowerContactTriggered && seed is SeedOfSilvaSunflower sunflower)
                    {
                        sunflowerContactTriggered = true;
                        sunflower.TriggerAssaultImpact(projectile);
                    }
                }

                if (preset == BlossomFluxChloroplastPresetType.Chlo_DBomb &&
                    projectile.damage > 0 &&
                    projectile.type != ModContent.ProjectileType<FuckYou>() &&
                    seed.IsBlooming &&
                    seed.CurrentPreset == BlossomFluxChloroplastPresetType.Chlo_DBomb &&
                    distanceSquared <= 72f * 72f)
                    seed.TryTriggerTorchflowerExplosion(projectile);
            }
        }

        public override void ModifyHitNPC(Projectile projectile, NPC target, ref NPC.HitModifiers modifiers)
        {
            if (!BFAccessorySystem.TryGetBlossomFluxPreset(projectile, out BlossomFluxChloroplastPresetType preset))
                return;

            if (BFArrowCommon.InBounds(projectile.owner, Main.maxPlayers) && Main.player[projectile.owner].GetModPlayer<BFAccessoryPlayer>().DominationQuiverEquipped)
                modifiers.DefenseEffectiveness *= 0f;

            if (BFArrowCommon.InBounds(projectile.owner, Main.maxPlayers))
            {
                BFAccessoryPlayer accessoryPlayer = Main.player[projectile.owner].GetModPlayer<BFAccessoryPlayer>();
                if (accessoryPlayer.HasBadSeedAttributes)
                    modifiers.FinalDamage *= 1f + BFAccessoryPlayer.BadSeedBlossomFluxDamageBonus * accessoryPlayer.BadSeedAttributeMultiplier;
            }

            if (sunflowerEmpowered && preset == BlossomFluxChloroplastPresetType.Chlo_ABreak)
            {
                modifiers.FinalDamage *= 1.35f;
                if (Main.rand.NextFloat() < 0.25f)
                    modifiers.FinalDamage *= 1.5f;
            }
        }

        public override void OnHitNPC(Projectile projectile, NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (!BFAccessorySystem.TryGetBlossomFluxPreset(projectile, out _) || !BFArrowCommon.InBounds(projectile.owner, Main.maxPlayers))
                return;

            Main.player[projectile.owner].GetModPlayer<BFAccessoryPlayer>().RegisterBlossomFluxHit();
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
