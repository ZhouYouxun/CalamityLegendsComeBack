using CalamityMod.Buffs.DamageOverTime;
using CalamityLegendsComeBack.Weapons.PristineFury;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Accssory.PF
{
    // Per-NPC purification level tracking for Pristine Fury accessories.
    // Level advances by continuously attacking with PF left-click projectiles.
    // Regular enemies: flat 3 seconds (180 frames) per level.
    // Bosses: level × 3 seconds per level (3s→6s→9s...).
    internal sealed class PFPurificationGlobalNPC : GlobalNPC
    {
        public override bool InstancePerEntity => true;

        // How long after the last hit before accumulated progress starts decaying.
        private const int HitRefreshFrames = 90;
        // Frames of progress decay per frame when not being hit.
        private const int DecayPerFrame = 2;

        public int PurificationLevel;
        private int hitAccumulator;
        private int hitRefreshTimer;
        private int periodicEffectTimer;

        // Called from PFAccessoryGlobalProjectile when a PF left-click projectile hits this NPC.
        internal void RegisterPFHit(int ownerIndex)
        {
            hitRefreshTimer = HitRefreshFrames;
        }

        public override void PostAI(NPC npc)
        {
            if (hitRefreshTimer > 0)
            {
                hitRefreshTimer--;
                hitAccumulator++;
            }
            else if (hitAccumulator > 0)
            {
                hitAccumulator = Math.Max(0, hitAccumulator - DecayPerFrame);
            }

            TryAdvanceLevel(npc);
            TickPeriodicEffects(npc);
        }

        private void TryAdvanceLevel(NPC npc)
        {
            int cap = GetPlayerPurificationCap();
            if (PurificationLevel >= cap)
                return;

            int threshold = GetThreshold(npc, PurificationLevel);
            if (hitAccumulator >= threshold)
            {
                hitAccumulator = 0;
                PurificationLevel++;
                OnLevelUp(npc);
            }
        }

        private static int GetThreshold(NPC npc, int currentLevel)
        {
            // Next level costs: flat 180 for regular, (currentLevel+1)*180 for bosses.
            if (npc.boss)
                return (currentLevel + 1) * 180;
            return 180;
        }

        // Returns the max purification level any PF-wielding player currently allows.
        private static int GetPlayerPurificationCap()
        {
            int best = 2;
            for (int i = 0; i < Main.maxPlayers; i++)
            {
                Player p = Main.player[i];
                if (!p.active || p.dead)
                    continue;
                if (p.HeldItem?.type != ModContent.ItemType<NewLegendPristineFury>())
                    continue;
                int cap = p.GetModPlayer<PFAccessoryPlayer>().PurificationCap;
                if (cap > best)
                    best = cap;
            }
            return best;
        }

        private void OnLevelUp(NPC npc)
        {
            if (Main.dedServ) return;

            Color levelColor = GetLevelColor(PurificationLevel);
            CombatText.NewText(npc.Hitbox, levelColor, $"纯化 Lv{PurificationLevel}", dramatic: false);
            npc.AddBuff(ModContent.BuffType<HolyFlames>(), 180 + PurificationLevel * 60);

            // Lv4+: check for explosion accessory effects.
            if (PurificationLevel >= 4)
                TrySpawnPurificationCombustion(npc, false);
        }

        private void TickPeriodicEffects(NPC npc)
        {
            if (!npc.active || !npc.boss || PurificationLevel < 4) return;

            periodicEffectTimer++;
            int interval = PurificationLevel switch
            {
                6 => 90,
                5 => 120,
                _ => 180   // lv4
            };

            if (periodicEffectTimer >= interval)
            {
                periodicEffectTimer = 0;
                TrySpawnPurificationCombustion(npc, true);
            }
        }

        // Spawns purification combustion if the attacker has the right lily equipped.
        // isPeriodic = true for recurring boss effect, false for one-shot level-up.
        private void TrySpawnPurificationCombustion(NPC npc, bool isPeriodic)
        {
            if (Main.dedServ) return;

            int cap = GetPlayerPurificationCap();
            if (cap < 4) return;

            // Visual: a burst of HolyFlames dust from the NPC center.
            Color sparkColor = GetLevelColor(PurificationLevel);
            int sparkCount = PurificationLevel switch
            {
                6 => 28,
                5 => 18,
                _ => 10
            };
            for (int k = 0; k < sparkCount; k++)
            {
                float angle = Main.rand.NextFloat(MathHelper.TwoPi);
                float speed = Main.rand.NextFloat(2f, 5f + PurificationLevel);
                Terraria.Dust d = Terraria.Dust.NewDustPerfect(
                    npc.Center + Main.rand.NextVector2Circular(npc.width * 0.4f, npc.height * 0.4f),
                    Terraria.ID.DustID.AncientLight,
                    angle.ToRotationVector2() * speed,
                    0,
                    sparkColor,
                    Main.rand.NextFloat(0.8f, 1.6f));
                d.noGravity = true;
            }
        }

        public override void OnKill(NPC npc)
        {
            if (PurificationLevel < 4 || Main.dedServ) return;

            int cap = GetPlayerPurificationCap();
            if (cap < 4) return;

            // Combustion explosion on death at lv4+.
            int spreadLevel = PurificationLevel switch
            {
                6 => 3,
                5 => 2,
                _ => 0
            };

            Color combColor = GetLevelColor(PurificationLevel);
            int dustCount = PurificationLevel switch { 6 => 48, 5 => 30, _ => 16 };
            for (int k = 0; k < dustCount; k++)
            {
                float angle = MathHelper.TwoPi * k / dustCount;
                float speed = Main.rand.NextFloat(3f, 8f);
                Terraria.Dust d = Terraria.Dust.NewDustPerfect(
                    npc.Center,
                    Terraria.ID.DustID.AncientLight,
                    angle.ToRotationVector2() * speed,
                    0,
                    combColor,
                    Main.rand.NextFloat(1f, 2f));
                d.noGravity = true;
            }

            // Spread purification to nearby enemies at lv5/6.
            if (spreadLevel > 0)
            {
                foreach (NPC other in Main.ActiveNPCs)
                {
                    if (other.whoAmI == npc.whoAmI || !other.active || other.friendly || other.dontTakeDamage)
                        continue;
                    float dist = Vector2.Distance(npc.Center, other.Center);
                    if (dist > 400f) continue;

                    PFPurificationGlobalNPC otherPurif = other.GetGlobalNPC<PFPurificationGlobalNPC>();
                    if (otherPurif.PurificationLevel < spreadLevel)
                    {
                        otherPurif.PurificationLevel = spreadLevel;
                        other.AddBuff(ModContent.BuffType<HolyFlames>(), 300);
                    }
                }
            }
        }

        // Damage modifier: PF projectiles deal bonus damage based on NPC purification level.
        public override void ModifyHitByProjectile(NPC npc, Projectile projectile, ref NPC.HitModifiers modifiers)
        {
            if (PurificationLevel < 3 || !IsAnyPFProjectile(projectile))
                return;

            float bonus = PurificationLevel switch
            {
                6 => 0.60f,
                5 => 0.40f,
                4 => 0.25f,
                _ => 0.15f   // lv3
            };
            modifiers.SourceDamage *= 1f + bonus;
        }

        private static bool IsAnyPFProjectile(Projectile proj)
        {
            if (!Main.player.IndexInRange(proj.owner) || !proj.friendly || proj.hostile)
                return false;
            Player owner = Main.player[proj.owner];
            return owner.active && owner.HeldItem?.type == ModContent.ItemType<NewLegendPristineFury>();
        }

        internal static Color GetLevelColor(int level) => level switch
        {
            1 => new Color(255, 240, 200),
            2 => new Color(255, 210, 80),
            3 => new Color(255, 160, 40),
            4 => new Color(255, 100, 20),
            5 => new Color(220, 50, 10),
            6 => new Color(180, 20, 80),
            _ => Color.White
        };
    }
}
