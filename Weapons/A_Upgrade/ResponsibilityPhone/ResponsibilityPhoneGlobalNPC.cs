using System;
using System.Collections.Generic;
using CalamityMod;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Upgrade.ResponsibilityPhone
{
    internal sealed class ResponsibilityPhoneGlobalNPC : GlobalNPC
    {
        private sealed class ConnectionState
        {
            public int Stacks;
            public int Timer;
            public int LastConnectionSequence = -1;
            public int LastResponseSequence = -1;
        }

        public override bool InstancePerEntity => true;

        private Dictionary<int, ConnectionState> connections;

        public override void PostAI(NPC npc)
        {
            if (connections == null || connections.Count == 0)
                return;

            List<int> expiredOwners = null;
            foreach (KeyValuePair<int, ConnectionState> pair in connections)
            {
                ConnectionState state = pair.Value;
                if (state.Timer > 0)
                    state.Timer--;
                if (state.Timer <= 0 && state.Stacks > 0)
                    state.Stacks = 0;

                if (state.Timer <= 0 && state.Stacks <= 0 && !HasHat(npc, pair.Key))
                {
                    expiredOwners ??= new List<int>();
                    expiredOwners.Add(pair.Key);
                }
            }

            if (expiredOwners != null)
            {
                foreach (int owner in expiredOwners)
                    connections.Remove(owner);
            }
        }

        public override void ModifyHitByProjectile(NPC npc, Projectile projectile, ref NPC.HitModifiers modifiers)
        {
            if (!Main.player.IndexInRange(projectile.owner) || !projectile.DamageType.CountsAsClass(DamageClass.Summon))
                return;
            if (HasHat(npc, projectile.owner))
                modifiers.SourceDamage *= 1.08f;
        }

        public override void OnKill(NPC npc)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient || !npc.boss || npc.friendly)
                return;
            if (npc.realLife >= 0 && npc.realLife != npc.whoAmI)
                return;

            foreach (Player player in Main.ActivePlayers)
            {
                if (!ResponsibilityPhone.HasPhoneInMainInventory(player))
                    continue;

                ResponsibilityPhonePlayer phonePlayer = player.GetModPlayer<ResponsibilityPhonePlayer>();
                phonePlayer.FillUltimate();
                ResponsibilityPhonePackets.SendState(player);
            }
        }

        internal static void RegisterSequenceHit(NPC target, int owner, int sequenceId, ResponsibilityLanguage language, int baseWeaponDamage)
        {
            if (!Main.player.IndexInRange(owner) || !target.active)
                return;

            ResponsibilityPhoneGlobalNPC global = target.GetGlobalNPC<ResponsibilityPhoneGlobalNPC>();
            global.connections ??= new Dictionary<int, ConnectionState>();
            if (!global.connections.TryGetValue(owner, out ConnectionState state))
            {
                state = new ConnectionState();
                global.connections[owner] = state;
            }

            ResponsibilityHatMarker hat = FindHat(target, owner);
            if (hat != null)
            {
                if (state.LastResponseSequence != sequenceId)
                {
                    state.LastResponseSequence = sequenceId;
                    hat.Respond(language, baseWeaponDamage);
                }
                return;
            }

            if (state.LastConnectionSequence == sequenceId)
                return;

            state.LastConnectionSequence = sequenceId;
            ResponsibilityLanguageDefinition definition = ResponsibilityLanguageRegistry.Get((int)language);
            state.Stacks = Math.Min(6, state.Stacks + (definition?.ConnectionValue ?? 1));
            state.Timer = 150;

            if (state.Stacks >= 6)
            {
                state.Stacks = 0;
                state.Timer = 0;
                if (owner == Main.myPlayer)
                    SpawnHat(target, owner, baseWeaponDamage);
            }
        }

        private static void SpawnHat(NPC target, int owner, int baseWeaponDamage)
        {
            if (FindHat(target, owner) != null)
                return;

            int markerType = ModContent.ProjectileType<ResponsibilityHatMarker>();
            List<Projectile> ownerHats = new();
            foreach (Projectile projectile in Main.ActiveProjectiles)
            {
                if (projectile.type == markerType && projectile.owner == owner)
                    ownerHats.Add(projectile);
            }

            if (ownerHats.Count >= 8)
            {
                Projectile replacement = null;
                foreach (Projectile candidate in ownerHats)
                {
                    int candidateTarget = (int)candidate.ai[0];
                    bool candidateBoss = Main.npc.IndexInRange(candidateTarget) && Main.npc[candidateTarget].active && Main.npc[candidateTarget].boss;
                    if (candidateBoss)
                        continue;
                    if (replacement == null || candidate.timeLeft < replacement.timeLeft)
                        replacement = candidate;
                }

                // A normal enemy is never allowed to evict one of eight protected boss hats.
                if (replacement == null && !target.boss)
                    return;
                replacement?.Kill();
            }

            int marker = Projectile.NewProjectile(
                target.GetSource_FromAI(),
                target.Top - Vector2.UnitY * 12f,
                Vector2.Zero,
                markerType,
                0,
                0f,
                owner,
                target.whoAmI);

            int impactDamage = Math.Max(1, (int)MathF.Round(baseWeaponDamage * 0.45f));
            Projectile.NewProjectile(
                target.GetSource_FromAI(),
                target.Center,
                Vector2.Zero,
                ModContent.ProjectileType<ResponsibilityHatImpact>(),
                impactDamage,
                2f,
                owner,
                target.whoAmI);

            if (Main.projectile.IndexInRange(marker))
                Main.projectile[marker].netUpdate = true;

            if (Main.dedServ)
                return;

            Vector2 effectCenter = target.Top - Vector2.UnitY * 4f;
            GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(
                effectCenter,
                Vector2.Zero,
                new Color(194, 255, 67),
                new Vector2(1.15f, 0.72f),
                0f,
                0.06f,
                0.72f,
                18));
            GeneralParticleHandler.SpawnParticle(new BloomParticle(
                effectCenter,
                Vector2.Zero,
                new Color(76, 202, 255),
                0.08f,
                0.42f,
                16));

            for (int i = 0; i < 10; i++)
            {
                Vector2 radial = (MathHelper.TwoPi * i / 10f).ToRotationVector2();
                Vector2 velocity = new Vector2(radial.X * Main.rand.NextFloat(1.2f, 3.2f), -Main.rand.NextFloat(0.8f, 3.4f));
                Color color = i % 3 == 0 ? Color.White : i % 2 == 0 ? new Color(194, 255, 67) : new Color(76, 202, 255);
                GeneralParticleHandler.SpawnParticle(new GlowOrbParticle(
                    effectCenter + radial * Main.rand.NextFloat(2f, 8f),
                    velocity,
                    false,
                    Main.rand.Next(13, 21),
                    Main.rand.NextFloat(0.28f, 0.48f),
                    color,
                    true,
                    false,
                    true));
            }
        }

        internal static bool HasHat(NPC npc, int owner) => FindHat(npc, owner) != null;

        internal static ResponsibilityHatMarker FindHat(NPC npc, int owner)
        {
            int markerType = ModContent.ProjectileType<ResponsibilityHatMarker>();
            foreach (Projectile projectile in Main.ActiveProjectiles)
            {
                if (projectile.type == markerType && projectile.owner == owner && (int)projectile.ai[0] == npc.whoAmI)
                    return projectile.ModProjectile as ResponsibilityHatMarker;
            }
            return null;
        }

        internal static NPC FindPriorityTarget(int owner, Vector2 origin, float maximumDistance, bool includeNearest = true)
        {
            if (!Main.player.IndexInRange(owner))
                return null;

            ResponsibilityPhonePlayer phonePlayer = Main.player[owner].GetModPlayer<ResponsibilityPhonePlayer>();
            if (phonePlayer.PriorityTargetTimer > 0 && Main.npc.IndexInRange(phonePlayer.PriorityTarget))
            {
                NPC forced = Main.npc[phonePlayer.PriorityTarget];
                if (forced.CanBeChasedBy() && forced.Distance(origin) <= maximumDistance)
                    return forced;
            }

            NPC bestPriorityHat = null;
            float bestPriority = -1f;
            NPC bestHat = null;
            float bestHatDistance = maximumDistance;
            int markerType = ModContent.ProjectileType<ResponsibilityHatMarker>();

            foreach (Projectile marker in Main.ActiveProjectiles)
            {
                if (marker.type != markerType || marker.owner != owner || !Main.npc.IndexInRange((int)marker.ai[0]))
                    continue;

                NPC candidate = Main.npc[(int)marker.ai[0]];
                if (!candidate.CanBeChasedBy())
                    continue;
                float distance = candidate.Distance(origin);
                if (distance > maximumDistance)
                    continue;

                if (marker.ai[1] > bestPriority)
                {
                    bestPriority = marker.ai[1];
                    bestPriorityHat = candidate;
                }
                if (distance < bestHatDistance)
                {
                    bestHatDistance = distance;
                    bestHat = candidate;
                }
            }

            if (bestPriority > 0f)
                return bestPriorityHat;
            if (bestHat != null)
                return bestHat;

            if (!includeNearest)
                return null;

            NPC nearest = null;
            float nearestDistance = maximumDistance;
            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!npc.CanBeChasedBy())
                    continue;
                float distance = npc.Distance(origin);
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearest = npc;
                }
            }
            return nearest;
        }

        internal static NPC FindDistributedTarget(int owner, int squadOrder, Vector2 origin, float maximumDistance)
        {
            if (!Main.player.IndexInRange(owner) || squadOrder < 0)
                return null;

            List<NPC> candidates = new();
            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (npc.CanBeChasedBy() && npc.Distance(origin) <= maximumDistance)
                    candidates.Add(npc);
            }

            if (candidates.Count == 0)
                return null;

            Player player = Main.player[owner];
            candidates.Sort((left, right) =>
            {
                int distanceComparison = Vector2.DistanceSquared(left.Center, player.Center).CompareTo(Vector2.DistanceSquared(right.Center, player.Center));
                return distanceComparison != 0 ? distanceComparison : left.whoAmI.CompareTo(right.whoAmI);
            });
            return candidates[squadOrder % candidates.Count];
        }
    }
}
