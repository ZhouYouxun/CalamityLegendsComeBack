using CalamityMod.Buffs.DamageOverTime;
using CalamityMod.Particles;
using CalamityLegendsComeBack.Weapons.PristineFury;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
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

            // CritSpark burst on level-up
            int burstCount = 4 + PurificationLevel * 2;
            for (int k = 0; k < burstCount; k++)
            {
                Vector2 pos = npc.Center + Main.rand.NextVector2Circular(npc.width * 0.4f, npc.height * 0.4f);
                float angle = Main.rand.NextFloat(MathHelper.TwoPi);
                Vector2 vel = angle.ToRotationVector2() * Main.rand.NextFloat(2f, 5f + PurificationLevel);
                Particle spark = new CritSpark(pos, vel,
                    Color.Lerp(levelColor, Color.Gold, 0.4f), Color.White,
                    0.9f + PurificationLevel * 0.1f, 18 + PurificationLevel * 3);
                GeneralParticleHandler.SpawnParticle(spark);
            }

            // Spinning square flash on level-up
            int squareCount = Math.Min(PurificationLevel, 3);
            for (int k = 0; k < squareCount; k++)
                SpawnSquareEffect(npc);

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
                if (!Main.dedServ)
                {
                    int sqCount = PurificationLevel switch { 6 => 3, 5 => 2, _ => 1 };
                    for (int k = 0; k < sqCount; k++)
                        SpawnSquareEffect(npc);
                }
            }
        }

        // Continuous on-screen visual effects based on purification level, called each draw frame.
        public override void DrawEffects(NPC npc, ref Color drawColor)
        {
            if (PurificationLevel <= 0) return;

            Color levelColor = GetLevelColor(PurificationLevel);

            // How often to fire the main CritSpark: 1/N chance per frame
            int sparkChance = PurificationLevel switch
            {
                1 => 14,
                2 => 9,
                3 => 5,
                4 => 3,
                5 => 2,
                _ => 1   // Lv6: every frame
            };

            // Main CritSpark (Holy Flames style)
            if (Main.rand.NextBool(sparkChance))
            {
                Vector2 pos = npc.Center + Main.rand.NextVector2Circular(npc.width * 0.45f, npc.height * 0.45f);
                Vector2 vel = new Vector2(0f, Main.rand.NextBool(4) ? -5f : -9f)
                    .RotatedByRandom(MathHelper.ToRadians(25f)) * Main.rand.NextFloat(0.1f, 1.9f)
                    + npc.velocity * 0.25f;
                float scale = 0.7f + PurificationLevel * 0.12f;
                Particle spark = new CritSpark(pos, vel,
                    Color.Lerp(levelColor, Color.Gold, 0.35f),
                    Color.White, scale, 12 + PurificationLevel * 2);
                GeneralParticleHandler.SpawnParticle(spark);
            }

            // GemTopaz dust from Lv2
            if (PurificationLevel >= 2 && Main.rand.NextBool(sparkChance))
            {
                Dust d = Dust.NewDustDirect(npc.position - new Vector2(2f, 2f), npc.width + 4, npc.height + 4, DustID.GemTopaz);
                d.velocity = npc.velocity + new Vector2(0f, Main.rand.NextFloat(-5f, -1f));
                d.noGravity = true;
                d.alpha = 235;
                d.scale = Main.rand.NextFloat(0.7f, 1.2f) * (1f + PurificationLevel * 0.06f);
            }

            // Extra scattered CritSparks from Lv4
            if (PurificationLevel >= 4 && Main.rand.NextBool(4))
            {
                Vector2 pos2 = npc.Center + Main.rand.NextVector2Circular(npc.width * 0.4f, npc.height * 0.4f);
                Vector2 vel2 = Main.rand.NextVector2Circular(3.5f, 5f) - Vector2.UnitY * 1.8f;
                Particle spark2 = new CritSpark(pos2, vel2, levelColor,
                    Color.Lerp(levelColor, Color.White, 0.5f),
                    0.5f + PurificationLevel * 0.07f, 8 + PurificationLevel);
                GeneralParticleHandler.SpawnParticle(spark2);
            }

            // Occasional shrinking glowing square (Nidhogg-style) from Lv3
            int squareChance = PurificationLevel switch
            {
                6 => 18,
                5 => 28,
                4 => 45,
                3 => 70,
                _ => 0
            };
            if (squareChance > 0 && Main.rand.NextBool(squareChance))
                SpawnSquareEffect(npc);

            // Ambient light from Lv2
            if (PurificationLevel >= 2)
            {
                float intensity = 0.07f + PurificationLevel * 0.03f;
                Lighting.AddLight(npc.Center,
                    levelColor.R / 255f * intensity,
                    levelColor.G / 255f * intensity,
                    levelColor.B / 255f * intensity);
            }
        }

        private void SpawnSquareEffect(NPC npc)
        {
            Color levelColor = GetLevelColor(PurificationLevel);
            int count = PurificationLevel >= 5 ? 2 : 1;
            for (int i = 0; i < count; i++)
            {
                Vector2 pos = npc.Center + Main.rand.NextVector2Circular(npc.width * 0.35f, npc.height * 0.35f);
                float scale = Main.rand.NextFloat(0.25f, 0.6f) * (1f + PurificationLevel * 0.1f);
                float spinSpeed = Main.rand.NextFloat(0.05f, 0.16f) * (Main.rand.NextBool() ? 1f : -1f);
                Vector2 vel = Main.rand.NextVector2Circular(1.2f, 1.8f) - Vector2.UnitY * 0.8f;
                Particle sq = new CustomSpark(
                    pos, vel,
                    "CalamityMod/Particles/GlowSquareParticle",
                    false,
                    35 + PurificationLevel * 6,
                    scale,
                    levelColor,
                    Vector2.One,
                    true,
                    true,
                    MathHelper.PiOver4,
                    spin: spinSpeed);
                GeneralParticleHandler.SpawnParticle(sq);
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
