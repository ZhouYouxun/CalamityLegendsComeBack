using System;
using CalamityMod;
using CalamityMod.Events;
using CalamityMod.NPCs;
using CalamityMod.World;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.Enums;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.BossAI.ReBack.Prime2041
{
    public static class Destroyer2041AI
    {
        public const float DRIncraeseTime = 600f;
        public const float DeathModeLaserBreathGateValue = 600f;
        public const float LaserTelegraphTime = 120f;
        public const float SparkTelegraphTime = 30f;
        public const float FlightPhaseGateValue = 900f;
        public const float FlightPhaseResetGateValue = FlightPhaseGateValue * 2f;
        private const float Phase4FlightPhaseTimerSetValue = FlightPhaseGateValue * 0.5f;
        private const float Phase5FlightPhaseTimerSetValue = FlightPhaseGateValue;
        public const float PhaseTransitionTelegraphTime = 180f;
        public const float GroundTelegraphStartGateValue = FlightPhaseResetGateValue - PhaseTransitionTelegraphTime;
        public const float FlightTelegraphStartGateValue = FlightPhaseGateValue - PhaseTransitionTelegraphTime;
        private const int OneInXChanceToFireLaser = 200;

        private static int HeadType => ModContent.NPCType<Destroyer2041Head>();
        private static int BodyType => ModContent.NPCType<Destroyer2041Body>();
        private static int TailType => ModContent.NPCType<Destroyer2041Tail>();

        private static bool IsSegmentType(int type) => type == HeadType || type == BodyType || type == TailType;

        public static bool BuffedDestroyerAI(NPC npc, Mod mod)
        {
            int mechdusaCurvedSpineSegmentIndex = 0;
            int mechdusaCurvedSpineSegments = 10;
            if (NPC.IsMechQueenUp && npc.type != HeadType)
            {
                int mechdusaIndex = (int)npc.ai[1];
                while (mechdusaIndex > 0 && mechdusaIndex < Main.maxNPCs)
                {
                    if (Main.npc[mechdusaIndex].active && IsSegmentType(Main.npc[mechdusaIndex].type))
                    {
                        mechdusaCurvedSpineSegmentIndex++;
                        if (Main.npc[mechdusaIndex].type == HeadType)
                            break;

                        if (mechdusaCurvedSpineSegmentIndex >= mechdusaCurvedSpineSegments)
                        {
                            mechdusaCurvedSpineSegmentIndex = 0;
                            break;
                        }

                        mechdusaIndex = (int)Main.npc[mechdusaIndex].ai[1];
                        continue;
                    }

                    mechdusaCurvedSpineSegmentIndex = 0;
                    break;
                }
            }

            CalamityGlobalNPC calamityGlobalNPC = npc.Calamity();

            bool bossRush = BossRushEvent.BossRushActive;
            bool masterMode = Main.masterMode || bossRush;
            bool death = CalamityWorld.death || bossRush;

            // 10 seconds of resistance to prevent spawn killing
            if (calamityGlobalNPC.newAI[1] < DRIncraeseTime)
                calamityGlobalNPC.newAI[1] += 1f;

            calamityGlobalNPC.CurrentlyIncreasingDefenseOrDR = calamityGlobalNPC.newAI[1] < DRIncraeseTime;

            // Percent life remaining
            float lifeRatio = npc.life / (float)npc.lifeMax;

            // Phases based on life percentage
            bool phase2 = lifeRatio < 0.85f || masterMode;
            bool phase3 = lifeRatio < 0.7f || masterMode;
            bool startFlightPhase = lifeRatio < 0.5f;
            bool phase4 = lifeRatio < (death ? 0.4f : 0.25f);
            bool phase5 = lifeRatio < (death ? 0.2f : 0.1f);

            // Flight timer
            if (startFlightPhase)
                calamityGlobalNPC.newAI[3] += 1f;

            // Force the timer to be at a certain value in later phases
            float flightPhaseTimerSetValue = phase5 ? Phase5FlightPhaseTimerSetValue : phase4 ? Phase4FlightPhaseTimerSetValue : 0f;
            if (calamityGlobalNPC.newAI[3] < flightPhaseTimerSetValue)
                calamityGlobalNPC.newAI[3] = flightPhaseTimerSetValue;

            // Return to ground phase, with less time spent in later phases
            if (calamityGlobalNPC.newAI[3] >= FlightPhaseResetGateValue)
            {
                calamityGlobalNPC.newAI[3] = flightPhaseTimerSetValue;
                npc.TargetClosest();
            }

            // Spawn DR check
            bool hasSpawnDR = calamityGlobalNPC.newAI[1] < DRIncraeseTime && calamityGlobalNPC.newAI[1] > 60f;

            // Gradual color transition from ground to flight and vice versa
            // 0f = Red, 1f = Purple
            float phaseTransitionColorAmount = (hasSpawnDR || phase5) ? 1f : 0f;
            if (!hasSpawnDR && !phase5)
            {
                if (calamityGlobalNPC.newAI[3] >= GroundTelegraphStartGateValue)
                    phaseTransitionColorAmount = MathHelper.Clamp(1f - (calamityGlobalNPC.newAI[3] - GroundTelegraphStartGateValue) / PhaseTransitionTelegraphTime, 0f, 1f);
                else if (calamityGlobalNPC.newAI[3] >= FlightTelegraphStartGateValue)
                    phaseTransitionColorAmount = MathHelper.Clamp((calamityGlobalNPC.newAI[3] - FlightTelegraphStartGateValue) / PhaseTransitionTelegraphTime, 0f, 1f);
            }

            // Set worm variable for worms
            if (npc.ai[3] > 0f)
                npc.realLife = (int)npc.ai[3];

            // Get a target
            if (npc.target < 0 || npc.target == Main.maxPlayers || Main.player[npc.target].dead || !Main.player[npc.target].active)
                npc.TargetClosest();

            Player player = Main.player[npc.target];

            bool increaseSpeed = Vector2.Distance(player.Center, npc.Center) > 3200f;
            bool increaseSpeedMore = Vector2.Distance(player.Center, npc.Center) > 5600f;

            // Get a new target if current target is too far away
            if (increaseSpeedMore && npc.type == HeadType)
                npc.TargetClosest();

            float enrageScale = bossRush ? 1f : 0f;
            if (Main.IsItDay() || bossRush)
            {
                calamityGlobalNPC.CurrentlyEnraged = !bossRush;
                enrageScale += 2f;
            }

            // Phase for flying at the player
            bool flyAtTarget = (calamityGlobalNPC.newAI[3] >= FlightPhaseGateValue && startFlightPhase) || hasSpawnDR;

            // Dust on spawn and alpha effects
            if (npc.type == HeadType || (npc.type != HeadType && Main.npc[(int)npc.ai[1]].alpha < 128))
            {
                if (npc.alpha != 0)
                {
                    for (int i = 0; i < 2; i++)
                    {
                        int spawnDust = Dust.NewDust(npc.position, npc.width, npc.height, DustID.TheDestroyer, 0f, 0f, 100, default, 2f);
                        Main.dust[spawnDust].noGravity = true;
                        Main.dust[spawnDust].noLight = true;
                    }
                }

                npc.alpha -= 42;
                if (npc.alpha < 0)
                    npc.alpha = 0;
            }

            // Check if other segments are still alive, if not, die
            // Check for Oblivion too, since having a max power Destroyer during that fight would be turbo cancer
            bool oblivionAlive = false;
            if (npc.type != HeadType)
            {
                bool shouldDespawn = true;
                for (int i = 0; i < Main.maxNPCs; i++)
                {
                    if (Main.npc[i].active && Main.npc[i].type == HeadType)
                    {
                        shouldDespawn = false;
                        break;
                    }
                }
                if (!shouldDespawn)
                {
                    if (npc.ai[1] <= 0f)
                        shouldDespawn = true;
                    else if (Main.npc[(int)npc.ai[1]].life <= 0)
                        shouldDespawn = true;
                }
                if (shouldDespawn)
                {
                    npc.life = 0;
                    npc.HitEffect(0, 10.0);
                    npc.checkDead();
                    npc.active = false;
                }
            }
            else
            {
                if (masterMode && !bossRush && npc.localAI[3] != -1f)
                {
                    for (int i = 0; i < Main.maxNPCs; i++)
                    {
                        if (Main.npc[i].active && (Main.npc[i].type == ModContent.NPCType<Prime2041SecondHead>() || Main.npc[i].type == ModContent.NPCType<Prime2041>()))
                        {
                            oblivionAlive = true;
                            break;
                        }
                    }
                }

                // Set variable to force despawn when Prime dies in Master Rev+
                // Set to -1f if Prime isn't alive when summoned
                if (npc.localAI[3] == 0f)
                {
                    if (oblivionAlive)
                        npc.localAI[3] = 1f;
                    else
                        npc.localAI[3] = -1f;

                    npc.SyncExtraAI();
                }
            }

            // Total segment variable
            int totalSegments = Main.getGoodWorld ? 100 : 80;

            // Calculate aggression based on how many broken segments there are
            float brokenSegmentAggressionMultiplier = 1f;
            if (npc.type == HeadType && !oblivionAlive)
            {
                int numProbeSegments = 0;
                for (int i = 0; i < Main.maxNPCs; i++)
                {
                    if (Main.npc[i].active && Main.npc[i].type == BodyType && Main.npc[i].ai[2] == 0f)
                        numProbeSegments++;
                }
                brokenSegmentAggressionMultiplier += (1f - MathHelper.Clamp(numProbeSegments / (float)totalSegments, 0f, 1f)) * 0.25f;
            }

            // Death Mode laser spit bool
            bool spitLaserSpreads = death;

            // Height of the box used to calculate whether The Destroyer should fly at its target or not
            int noFlyZoneBoxHeight = masterMode ? 1500 : 1800;

            // Speed and movement variables
            float speed = masterMode ? 0.2f : 0.1f;
            float turnSpeed = masterMode ? 0.3f : 0.15f;

            // Max velocity
            float segmentVelocity = flyAtTarget ? (masterMode ? 22.5f : 15f) : (masterMode ? 30f : 20f);

            // Increase velocity based on distance
            float velocityMultiplier = increaseSpeedMore ? 2f : increaseSpeed ? 1.5f : 1f;

            // If Oblivion is alive, don't fly, don't spit laser spreads, use the default vanilla no fly zone, reduce segment count to 60, use base speed and use base turn speed
            if (oblivionAlive)
            {
                calamityGlobalNPC.newAI[3] = 0f;
                totalSegments = Main.getGoodWorld ? 75 : 60;
                spitLaserSpreads = false;
                noFlyZoneBoxHeight = 2000;
            }
            else
            {
                noFlyZoneBoxHeight -= death ? 400 : (int)(400f * (1f - lifeRatio));

                float segmentVelocityBoost = death ? (flyAtTarget ? 4.5f : 6f) * (1f - lifeRatio) : (flyAtTarget ? 3f : 4f) * (1f - lifeRatio);
                float speedBoost = death ? (flyAtTarget ? 0.1125f : 0.15f) * (1f - lifeRatio) : (flyAtTarget ? 0.075f : 0.1f) * (1f - lifeRatio);
                float turnSpeedBoost = death ? 0.18f * (1f - lifeRatio) : 0.12f * (1f - lifeRatio);

                segmentVelocity += segmentVelocityBoost;
                speed += speedBoost;
                turnSpeed += turnSpeedBoost;

                segmentVelocity += 5f * enrageScale;
                speed += 0.05f * enrageScale;
                turnSpeed += 0.075f * enrageScale;

                if (flyAtTarget)
                {
                    float speedMultiplier = phase5 ? 1.8f : phase4 ? 1.65f : 1.5f;
                    speed *= speedMultiplier;
                }

                segmentVelocity *= velocityMultiplier;
                speed *= velocityMultiplier;
                turnSpeed *= velocityMultiplier;

                segmentVelocity *= brokenSegmentAggressionMultiplier;
                speed *= brokenSegmentAggressionMultiplier;
                turnSpeed *= brokenSegmentAggressionMultiplier;

                if (Main.getGoodWorld)
                {
                    segmentVelocity *= 1.2f;
                    speed *= 1.2f;
                    turnSpeed *= 1.2f;
                }
            }

            bool probeLaunched = npc.ai[2] == 1f;
            if (npc.type == BodyType)
            {
                // Enrage, fire more cyan lasers
                if (enrageScale > 0f && !bossRush)
                {
                    if (calamityGlobalNPC.newAI[2] < 480f)
                        calamityGlobalNPC.newAI[2] += 1f;
                }
                else
                {
                    if (calamityGlobalNPC.newAI[2] > 0f)
                        calamityGlobalNPC.newAI[2] -= 1f;
                }

                // Regenerate Probes in Master Mode if the number of Probes is less than 40 and the number of living NPCs is less than the segment count + 40 (this limit is here just in case)
                if (masterMode && probeLaunched)
                {
                    npc.localAI[2] += 1f;
                    if (npc.localAI[2] >= 600f)
                    {
                        int maxProbes = 40;
                        bool regenerateProbeSegment = NPC.CountNPCS(NPCID.Probe) < maxProbes;
                        if (regenerateProbeSegment)
                        {
                            int maxNPCs = totalSegments + maxProbes;
                            int numNPCs = 0;
                            for (int i = 0; i < Main.maxNPCs; i++)
                            {
                                if (Main.npc[i].active)
                                {
                                    numNPCs++;
                                    if (numNPCs >= maxNPCs)
                                    {
                                        regenerateProbeSegment = false;
                                        break;
                                    }
                                }
                            }
                        }

                        if (regenerateProbeSegment)
                        {
                            npc.ai[2] = 0f;
                            npc.netUpdate = true;
                        }

                        npc.localAI[2] = 0f;
                        npc.SyncVanillaLocalAI();
                    }
                }
            }

            if (npc.type == HeadType)
            {
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    // Spawn segments from head
                    if (npc.ai[0] == 0f)
                    {
                        npc.ai[3] = npc.whoAmI;
                        npc.realLife = npc.whoAmI;
                        int index = npc.whoAmI;
                        for (int j = 0; j <= totalSegments; j++)
                        {
                            int type = BodyType;
                            if (j == totalSegments)
                                type = TailType;

                            int segment = NPC.NewNPC(npc.GetSource_FromAI(), (int)(npc.Center.X), (int)(npc.position.Y + npc.height), type, npc.whoAmI);
                            Main.npc[segment].ai[3] = npc.whoAmI;
                            Main.npc[segment].realLife = npc.whoAmI;
                            Main.npc[segment].ai[1] = index;
                            Main.npc[index].ai[0] = segment;
                            NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, segment);
                            index = segment;
                        }
                    }
                }

                // Laser breath in Death Mode
                if (spitLaserSpreads)
                {
                    // Set laser color and type
                    if (npc.DestroyerLaserColor() == -1)
                    {
                        npc.SetDestroyerLaserColor(phase3 ? 3 : phase2 ? 2 : 1);
                        npc.SyncDestroyerLaserColor();
                    }

                    float laserBreathGateValue = DeathModeLaserBreathGateValue;
                    if (calamityGlobalNPC.newAI[0] < laserBreathGateValue)
                        calamityGlobalNPC.newAI[0] += 1f;

                    // Sync newAI every 20 frames for the new telegraph
                    if (calamityGlobalNPC.newAI[0] % 20f == 0f)
                        npc.SyncExtraAI();

                    if ((player.Center - npc.Center).SafeNormalize(Vector2.UnitY).ToRotation().AngleTowards(npc.velocity.ToRotation(), MathHelper.PiOver4) == npc.velocity.ToRotation() &&
                        calamityGlobalNPC.newAI[0] >= laserBreathGateValue && Vector2.Distance(npc.Center, player.Center) > 480f &&
                        Collision.CanHit(npc.position, npc.width, npc.height, player.position, player.width, player.height))
                    {
                        if (calamityGlobalNPC.newAI[0] % 30f == 0f)
                        {
                            float velocity = bossRush ? 6f : death ? 5.333f : 5f;
                            int type = ProjectileID.DeathLaser;
                            switch (npc.DestroyerLaserColor())
                            {
                                default:
                                case 0:
                                    break;

                                case 1:
                                    type = ProjectileID.DeathLaser;
                                    break;

                                case 2:
                                    type = ProjectileID.DeathLaser;
                                    break;
                            }
                            int damage = npc.GetProjectileDamage(type);

                            // Reduce mech boss projectile damage depending on the new ore progression changes
                            if (Prime2041Compat.EarlyHardmodeProgressionRework && !BossRushEvent.BossRushActive)
                            {
                                double firstMechMultiplier = Prime2041Compat.EarlyHardmodeProgressionReworkFirstMechStatMultiplierExpert;
                                double secondMechMultiplier = Prime2041Compat.EarlyHardmodeProgressionReworkSecondMechStatMultiplierExpert;
                                if (!NPC.downedMechBossAny)
                                    damage = (int)(damage * firstMechMultiplier);
                                else if ((!NPC.downedMechBoss1 && !NPC.downedMechBoss2) || (!NPC.downedMechBoss2 && !NPC.downedMechBoss3) || (!NPC.downedMechBoss3 && !NPC.downedMechBoss1))
                                    damage = (int)(damage * secondMechMultiplier);
                            }

                            Vector2 projectileVelocity = (player.Center - npc.Center).SafeNormalize(Vector2.UnitY) * velocity;
                            int numProj = calamityGlobalNPC.newAI[0] % 60f == 0f ? (masterMode ? 9 : 7) : (masterMode ? 6 : 4);
                            int spread = masterMode ? 38 : 26;
                            float rotation = MathHelper.ToRadians(spread);
                            for (int i = 0; i < numProj; i++)
                            {
                                Vector2 perturbedSpeed = projectileVelocity.RotatedBy(MathHelper.Lerp(-rotation, rotation, i / (float)(numProj - 1)));
                                if (Main.netMode != NetmodeID.MultiplayerClient)
                                {
                                    int proj = Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center + perturbedSpeed.SafeNormalize(Vector2.UnitY) * 100f, perturbedSpeed, type, damage, 0f, Main.myPlayer, 1f, 0f);
                                    Main.projectile[proj].timeLeft = 1200;
                                }
                            }
                        }

                        calamityGlobalNPC.newAI[0] += 1f;
                        if (calamityGlobalNPC.newAI[0] > laserBreathGateValue + 60f)
                        {
                            calamityGlobalNPC.newAI[0] = 0f;
                            npc.SetDestroyerLaserColor(-1);
                            npc.SyncDestroyerLaserColor();
                            npc.SyncExtraAI();
                        }
                    }
                }
            }

            // Fire lasers
            if (npc.type == BodyType)
            {
                bool ableToFireLaser = npc.DestroyerLaserColor() != -1;

                // Set laser color and type
                if (npc.DestroyerLaserColor() == -1 && !probeLaunched)
                {
                    if (Main.rand.NextBool(masterMode ? OneInXChanceToFireLaser / (phase5 ? 4 : phase4 ? 3 : 2) : OneInXChanceToFireLaser))
                    {
                        int random = phase3 ? 4 : phase2 ? 3 : 2;
                        switch (Main.rand.Next(random))
                        {
                            case 0:
                            case 1:
                                npc.SetDestroyerLaserColor(0);
                                break;
                            case 2:
                                npc.SetDestroyerLaserColor(1);
                                break;
                            case 3:
                                npc.SetDestroyerLaserColor(2);
                                break;
                        }

                        if (calamityGlobalNPC.newAI[2] > 0f || bossRush)
                            npc.SetDestroyerLaserColor(2);

                        npc.SyncDestroyerLaserColor();
                    }
                }

                if (probeLaunched && ableToFireLaser)
                {
                    npc.SetDestroyerLaserColor(-1);
                    npc.SyncDestroyerLaserColor();
                }

                // Laser rate of fire
                float shootProjectileTime = death ? (masterMode ? (phase5 ? 120f : phase4 ? 150f : 180f) : 270f) : (masterMode ? (phase5 ? 150f : phase4 ? 210f : 270f) : 450f);
                float bodySegmentTime = npc.ai[0] * (masterMode ? 20f : 30f);
                float shootProjectileGateValue = bodySegmentTime + shootProjectileTime;
                float laserTimerIncrement = (calamityGlobalNPC.newAI[0] > shootProjectileGateValue - LaserTelegraphTime) ? 1f : 2f;
                if (ableToFireLaser)
                    calamityGlobalNPC.newAI[0] += laserTimerIncrement;

                // Sync newAI every 20 frames for the new telegraph
                if (calamityGlobalNPC.newAI[0] % 20f == 0f && ableToFireLaser)
                    npc.SyncExtraAI();

                Color telegraphColor = Color.Transparent;
                switch (npc.DestroyerLaserColor())
                {
                    case 0:
                        telegraphColor = Color.Red;
                        break;
                    case 1:
                        telegraphColor = Color.Green;
                        break;
                    case 2:
                        telegraphColor = Color.Cyan;
                        break;
                }

                if (calamityGlobalNPC.newAI[0] == shootProjectileGateValue - LaserTelegraphTime)
                {
                    Particle telegraph = new DestroyerReticleTelegraph(
                        npc,
                        telegraphColor,
                        1.5f,
                        0.15f,
                        (int)LaserTelegraphTime);
                    GeneralParticleHandler.SpawnParticle(telegraph); 
                }

                if (calamityGlobalNPC.newAI[0] == shootProjectileGateValue - SparkTelegraphTime)
                {
                    Particle spark = new DestroyerSparkTelegraph(
                        npc,
                        telegraphColor * 2f,
                        Color.White,
                        3f,
                        30,
                        Main.rand.NextFloat(MathHelper.ToRadians(3f)) * Main.rand.NextBool().ToDirectionInt());
                    GeneralParticleHandler.SpawnParticle(spark);
                }

                // Shoot lasers
                // Shoot nothing if probe has been launched
                if (calamityGlobalNPC.newAI[0] >= shootProjectileGateValue && ableToFireLaser)
                {
                    if (!masterMode)
                    {
                        int numProbeSegments = 0;
                        for (int i = 0; i < Main.maxNPCs; i++)
                        {
                            if (Main.npc[i].active && Main.npc[i].type == npc.type && Main.npc[i].ai[2] == 0f)
                                numProbeSegments++;
                        }
                        float lerpAmount = MathHelper.Clamp(numProbeSegments / (float)totalSegments, 0f, 1f);
                        float laserShootTimeBonus = (int)MathHelper.Lerp(0f, (shootProjectileTime + bodySegmentTime * lerpAmount) - LaserTelegraphTime, 1f - lerpAmount);
                        calamityGlobalNPC.newAI[0] = laserShootTimeBonus;
                        npc.SyncExtraAI();
                        npc.TargetClosest();
                    }

                    if (Collision.CanHit(npc.position, npc.width, npc.height, player.position, player.width, player.height))
                    {
                        if (masterMode)
                        {
                            int numProbeSegments = 0;
                            for (int i = 0; i < Main.maxNPCs; i++)
                            {
                                if (Main.npc[i].active && Main.npc[i].type == npc.type && Main.npc[i].ai[2] == 0f)
                                    numProbeSegments++;
                            }
                            float lerpAmount = MathHelper.Clamp(numProbeSegments / (float)totalSegments, 0f, 1f);
                            float laserShootTimeBonus = (int)MathHelper.Lerp(0f, (shootProjectileTime + bodySegmentTime * lerpAmount) - LaserTelegraphTime, 1f - lerpAmount);
                            calamityGlobalNPC.newAI[0] = laserShootTimeBonus;
                            npc.SyncExtraAI();
                            npc.TargetClosest();
                        }

                        // Laser speed
                        float projectileSpeed = (masterMode ? 4.5f : 3.5f) + Main.rand.NextFloat() * 1.5f;
                        projectileSpeed += enrageScale;

                        // Set projectile damage and type
                        int projectileType = ProjectileID.DeathLaser;
                        switch (npc.DestroyerLaserColor())
                        {
                            default:
                            case 0:
                                break;

                            case 1:
                                projectileType = ProjectileID.DeathLaser;
                                break;

                            case 2:
                                projectileType = ProjectileID.DeathLaser;
                                break;
                        }

                        // Get target vector
                        Vector2 projectileVelocity = (player.Center - npc.Center).SafeNormalize(Vector2.UnitY) * projectileSpeed;
                        Vector2 projectileSpawn = npc.Center + projectileVelocity.SafeNormalize(Vector2.UnitY) * 100f;

                        // Shoot projectile
                        int damage = npc.GetProjectileDamage(projectileType);

                        // Reduce mech boss projectile damage depending on the new ore progression changes
                        if (Prime2041Compat.EarlyHardmodeProgressionRework && !BossRushEvent.BossRushActive)
                        {
                            double firstMechMultiplier = Prime2041Compat.EarlyHardmodeProgressionReworkFirstMechStatMultiplierExpert;
                            double secondMechMultiplier = Prime2041Compat.EarlyHardmodeProgressionReworkSecondMechStatMultiplierExpert;
                            if (!NPC.downedMechBossAny)
                                damage = (int)(damage * firstMechMultiplier);
                            else if ((!NPC.downedMechBoss1 && !NPC.downedMechBoss2) || (!NPC.downedMechBoss2 && !NPC.downedMechBoss3) || (!NPC.downedMechBoss3 && !NPC.downedMechBoss1))
                                damage = (int)(damage * secondMechMultiplier);
                        }

                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            int proj = Projectile.NewProjectile(npc.GetSource_FromAI(), projectileSpawn, projectileVelocity, projectileType, damage, 0f, Main.myPlayer, 1f, 0f);
                            Main.projectile[proj].timeLeft = 1200;
                        }

                        npc.netUpdate = true;

                        if (masterMode)
                        {
                            npc.SetDestroyerLaserColor(-1);
                            npc.SyncDestroyerLaserColor();
                        }
                    }

                    if (!masterMode)
                    {
                        npc.SetDestroyerLaserColor(-1);
                        npc.SyncDestroyerLaserColor();
                    }
                }
            }

            if (npc.type == HeadType)
            {
                if (npc.life > Main.npc[(int)npc.ai[0]].life)
                    npc.life = Main.npc[(int)npc.ai[0]].life;
            }
            else
            {
                if (npc.life > Main.npc[(int)npc.ai[1]].life)
                    npc.life = Main.npc[(int)npc.ai[1]].life;
            }

            int tilePosX = (int)(npc.position.X / 16f) - 1;
            int tileWidthPosX = (int)((npc.position.X + npc.width) / 16f) + 2;
            int tilePosY = (int)(npc.position.Y / 16f) - 1;
            int tileWidthPosY = (int)((npc.position.Y + npc.height) / 16f) + 2;

            if (tilePosX < 0)
                tilePosX = 0;
            if (tileWidthPosX > Main.maxTilesX)
                tileWidthPosX = Main.maxTilesX;
            if (tilePosY < 0)
                tilePosY = 0;
            if (tileWidthPosY > Main.maxTilesY)
                tileWidthPosY = Main.maxTilesY;

            // Fly or not
            bool shouldFly = flyAtTarget;
            if (!shouldFly)
            {
                for (int k = tilePosX; k < tileWidthPosX; k++)
                {
                    for (int l = tilePosY; l < tileWidthPosY; l++)
                    {
                        if (Main.tile[k, l] != null && ((Main.tile[k, l].HasUnactuatedTile && (Main.tileSolid[Main.tile[k, l].TileType] || (Main.tileSolidTop[Main.tile[k, l].TileType] && Main.tile[k, l].TileFrameY == 0))) || Main.tile[k, l].LiquidAmount > 64))
                        {
                            Vector2 tileConvertedPosition;
                            tileConvertedPosition.X = k * 16;
                            tileConvertedPosition.Y = l * 16;
                            if (npc.position.X + npc.width > tileConvertedPosition.X && npc.position.X < tileConvertedPosition.X + 16f && npc.position.Y + npc.height > tileConvertedPosition.Y && npc.position.Y < tileConvertedPosition.Y + 16f)
                            {
                                shouldFly = true;
                                break;
                            }
                        }
                    }
                }
            }

            // Start flying if target is not within a certain distance
            if (!shouldFly)
            {
                npc.localAI[1] = 1f;

                if (npc.type == HeadType)
                {
                    Rectangle rectangle = new Rectangle((int)npc.position.X, (int)npc.position.Y, npc.width, npc.height);
                    int noFlyZone = 1000;
                    bool outsideNoFlyZone = true;

                    if (npc.position.Y > player.position.Y)
                    {
                        for (int m = 0; m < Main.maxPlayers; m++)
                        {
                            if (Main.player[m].active)
                            {
                                Rectangle noFlyRectangle = new Rectangle((int)Main.player[m].position.X - noFlyZone, (int)Main.player[m].position.Y - noFlyZone, noFlyZone * 2, noFlyZoneBoxHeight);
                                if (rectangle.Intersects(noFlyRectangle))
                                {
                                    outsideNoFlyZone = false;
                                    break;
                                }
                            }
                        }

                        if (outsideNoFlyZone)
                            shouldFly = true;
                    }
                }
            }
            else
                npc.localAI[1] = 0f;

            if (npc.type != BodyType || !probeLaunched)
            {
                Vector3 lightColor = Color.Red.ToVector3();
                int x = (int)((npc.position.X - 8f) / 16f);
                int x2 = (int)((npc.position.X + npc.width + 8f) / 16f);
                int y = (int)((npc.position.Y - 8f) / 16f);
                int y2 = (int)((npc.position.Y + npc.height + 8f) / 16f);
                for (int l = x; l <= x2; l++)
                {
                    for (int m = y; m <= y2; m++)
                    {
                        if (Lighting.Brightness(l, m) == 0f)
                            lightColor = Color.Black.ToVector3();
                    }
                }

                if (lightColor != Color.Black.ToVector3())
                {
                    // Light colors
                    Vector3 groundColor = new Vector3(0.3f, 0.1f, 0.05f);
                    Vector3 flightColor = new Vector3(0.05f, 0.1f, 0.3f);
                    Vector3 segmentColor = Vector3.Lerp(groundColor, flightColor, phaseTransitionColorAmount);
                    Vector3 telegraphColor = groundColor;

                    // Telegraph for the laser breath and body lasers
                    float telegraphProgress = 0f;
                    if (npc.DestroyerLaserColor() != -1)
                    {
                        if (npc.type == HeadType && spitLaserSpreads)
                        {
                            float telegraphGateValue = DeathModeLaserBreathGateValue - LaserTelegraphTime;
                            if (calamityGlobalNPC.newAI[0] > telegraphGateValue)
                            {
                                switch (npc.DestroyerLaserColor())
                                {
                                    default:
                                    case 0:
                                        break;

                                    case 1:
                                        telegraphColor = new Vector3(0.1f, 0.3f, 0.05f);
                                        break;

                                    case 2:
                                        telegraphColor = new Vector3(0.05f, 0.2f, 0.2f);
                                        break;
                                }
                                telegraphProgress = MathHelper.Clamp((calamityGlobalNPC.newAI[0] - telegraphGateValue) / LaserTelegraphTime, 0f, 1f);
                            }
                        }
                        else if (npc.type == BodyType)
                        {
                            float shootProjectileTime = (CalamityWorld.death || BossRushEvent.BossRushActive) ? 270f : 450f;
                            float bodySegmentTime = npc.ai[0] * 30f;
                            float shootProjectileGateValue = bodySegmentTime + shootProjectileTime;
                            float telegraphGateValue = shootProjectileGateValue - LaserTelegraphTime;
                            if (calamityGlobalNPC.newAI[0] > telegraphGateValue)
                            {
                                switch (npc.DestroyerLaserColor())
                                {
                                    default:
                                    case 0:
                                        break;

                                    case 1:
                                        telegraphColor = new Vector3(0.1f, 0.3f, 0.05f);
                                        break;

                                    case 2:
                                        telegraphColor = new Vector3(0.05f, 0.2f, 0.2f);
                                        break;
                                }
                                telegraphProgress = MathHelper.Clamp((calamityGlobalNPC.newAI[0] - telegraphGateValue) / LaserTelegraphTime, 0f, 1f);
                            }
                        }
                    }

                    Lighting.AddLight(npc.Center, Vector3.Lerp(segmentColor, telegraphColor * 2f, telegraphProgress));
                }
            }

            // Despawn
            bool oblivionWasAlive = npc.localAI[3] == 1f && !oblivionAlive;
            bool oblivionFightDespawn = (oblivionAlive && lifeRatio < 0.75f) || oblivionWasAlive;
            if (player.dead || oblivionFightDespawn)
            {
                shouldFly = false;
                npc.velocity.Y += 2f;

                if (npc.position.Y > Main.worldSurface * 16D)
                {
                    npc.velocity.Y += 2f;
                    segmentVelocity *= 2f;
                }

                if (npc.position.Y > Main.rockLayer * 16D)
                {
                    for (int n = 0; n < Main.maxNPCs; n++)
                    {
                        if (Main.npc[n].aiStyle == npc.aiStyle)
                            Main.npc[n].active = false;
                    }
                }
            }

            Vector2 npcCenter = npc.Center;
            float targetTilePosX = player.Center.X;
            float targetTilePosY = player.Center.Y;
            targetTilePosX = (int)(targetTilePosX / 16f) * 16;
            targetTilePosY = (int)(targetTilePosY / 16f) * 16;
            npcCenter.X = (int)(npcCenter.X / 16f) * 16;
            npcCenter.Y = (int)(npcCenter.Y / 16f) * 16;
            targetTilePosX -= npcCenter.X;
            targetTilePosY -= npcCenter.Y;
            float targetTileDist = (float)Math.Sqrt(targetTilePosX * targetTilePosX + targetTilePosY * targetTilePosY);

            if (npc.ai[1] > 0f && npc.ai[1] < Main.npc.Length)
            {
                int mechdusaSegmentScale = (int)(44f * npc.scale);
                try
                {
                    npcCenter = npc.Center;
                    targetTilePosX = Main.npc[(int)npc.ai[1]].Center.X - npcCenter.X;
                    targetTilePosY = Main.npc[(int)npc.ai[1]].Center.Y - npcCenter.Y;
                }
                catch
                {
                }

                if (mechdusaCurvedSpineSegmentIndex > 0)
                {
                    float absoluteTilePosX = (float)mechdusaSegmentScale - (float)mechdusaSegmentScale * (((float)mechdusaCurvedSpineSegmentIndex - 1f) * 0.1f);
                    if (absoluteTilePosX < 0f)
                        absoluteTilePosX = 0f;

                    if (absoluteTilePosX > (float)mechdusaSegmentScale)
                        absoluteTilePosX = mechdusaSegmentScale;

                    targetTilePosY = Main.npc[(int)npc.ai[1]].Center.Y + absoluteTilePosX - npcCenter.Y;
                }

                npc.rotation = (float)Math.Atan2(targetTilePosY, targetTilePosX) + MathHelper.PiOver2;
                targetTileDist = (float)Math.Sqrt(targetTilePosX * targetTilePosX + targetTilePosY * targetTilePosY);
                if (mechdusaCurvedSpineSegmentIndex > 0)
                    mechdusaSegmentScale = mechdusaSegmentScale / mechdusaCurvedSpineSegments * mechdusaCurvedSpineSegmentIndex;

                targetTileDist = (targetTileDist - mechdusaSegmentScale) / targetTileDist;
                targetTilePosX *= targetTileDist;
                targetTilePosY *= targetTileDist;
                npc.velocity = Vector2.Zero;
                npc.position.X += targetTilePosX;
                npc.position.Y += targetTilePosY;
            }
            else
            {
                if (!shouldFly)
                {
                    npc.velocity.Y += 0.15f;
                    if (masterMode && npc.velocity.Y > 0f && Math.Abs(npc.Center.Y - player.Center.Y) > 360f)
                        npc.velocity.Y += 0.05f;

                    if (npc.velocity.Y > segmentVelocity)
                        npc.velocity.Y = segmentVelocity;

                    // This bool exists to stop the strange wiggle behavior when worms are falling down
                    bool slowXVelocity = Math.Abs(npc.velocity.X) > speed;
                    if ((Math.Abs(npc.velocity.X) + Math.Abs(npc.velocity.Y)) < segmentVelocity * 0.4)
                    {
                        if (npc.velocity.X < 0f)
                            npc.velocity.X -= speed * 1.1f;
                        else
                            npc.velocity.X += speed * 1.1f;
                    }
                    else if (npc.velocity.Y == segmentVelocity)
                    {
                        if (slowXVelocity)
                        {
                            if (npc.velocity.X < targetTilePosX)
                                npc.velocity.X += speed;
                            else if (npc.velocity.X > targetTilePosX)
                                npc.velocity.X -= speed;
                        }
                        else
                            npc.velocity.X = 0f;
                    }
                    else if (npc.velocity.Y > 4f)
                    {
                        if (slowXVelocity)
                        {
                            if (npc.velocity.X < 0f)
                                npc.velocity.X += speed * 0.9f;
                            else
                                npc.velocity.X -= speed * 0.9f;
                        }
                        else
                            npc.velocity.X = 0f;
                    }
                }
                else
                {
                    if (npc.soundDelay == 0)
                    {
                        float soundDelay = targetTileDist / 40f;
                        if (soundDelay < 10f)
                            soundDelay = 10f;
                        if (soundDelay > 20f)
                            soundDelay = 20f;

                        npc.soundDelay = (int)soundDelay;
                        SoundEngine.PlaySound(SoundID.WormDig, npc.Center);
                    }

                    targetTileDist = (float)Math.Sqrt(targetTilePosX * targetTilePosX + targetTilePosY * targetTilePosY);
                    float absoluteTilePosX = Math.Abs(targetTilePosX);
                    float absoluteTilePosY = Math.Abs(targetTilePosY);
                    float tileToReachTarget = segmentVelocity / targetTileDist;
                    targetTilePosX *= tileToReachTarget;
                    targetTilePosY *= tileToReachTarget;

                    bool flyWyvernMovement = false;
                    if (flyAtTarget)
                    {
                        if (((npc.velocity.X > 0f && targetTilePosX < 0f) || (npc.velocity.X < 0f && targetTilePosX > 0f) || (npc.velocity.Y > 0f && targetTilePosY < 0f) || (npc.velocity.Y < 0f && targetTilePosY > 0f)) && Math.Abs(npc.velocity.X) + Math.Abs(npc.velocity.Y) > speed / 2f && targetTileDist < 600f)
                        {
                            flyWyvernMovement = true;

                            if (Math.Abs(npc.velocity.X) + Math.Abs(npc.velocity.Y) < segmentVelocity)
                                npc.velocity *= 1.1f;
                        }

                        if (npc.position.Y > player.position.Y)
                        {
                            flyWyvernMovement = true;

                            if (Math.Abs(npc.velocity.X) < segmentVelocity / 2f)
                            {
                                if (npc.velocity.X == 0f)
                                    npc.velocity.X -= npc.direction;

                                npc.velocity.X *= 1.1f;
                            }
                            else if (npc.velocity.Y > -segmentVelocity)
                                npc.velocity.Y -= speed;
                        }
                    }

                    if (!flyWyvernMovement)
                    {
                        if (!flyAtTarget)
                        {
                            if (((npc.velocity.X > 0f && targetTilePosX > 0f) || (npc.velocity.X < 0f && targetTilePosX < 0f)) && ((npc.velocity.Y > 0f && targetTilePosY > 0f) || (npc.velocity.Y < 0f && targetTilePosY < 0f)))
                            {
                                if (npc.velocity.X < targetTilePosX)
                                    npc.velocity.X += turnSpeed;
                                else if (npc.velocity.X > targetTilePosX)
                                    npc.velocity.X -= turnSpeed;
                                if (npc.velocity.Y < targetTilePosY)
                                    npc.velocity.Y += turnSpeed;
                                else if (npc.velocity.Y > targetTilePosY)
                                    npc.velocity.Y -= turnSpeed;
                            }
                        }

                        if ((npc.velocity.X > 0f && targetTilePosX > 0f) || (npc.velocity.X < 0f && targetTilePosX < 0f) || (npc.velocity.Y > 0f && targetTilePosY > 0f) || (npc.velocity.Y < 0f && targetTilePosY < 0f))
                        {
                            if (npc.velocity.X < targetTilePosX)
                                npc.velocity.X += speed;
                            else if (npc.velocity.X > targetTilePosX)
                                npc.velocity.X -= speed;
                            if (npc.velocity.Y < targetTilePosY)
                                npc.velocity.Y += speed;
                            else if (npc.velocity.Y > targetTilePosY)
                                npc.velocity.Y -= speed;

                            if (Math.Abs(targetTilePosY) < segmentVelocity * 0.2 && ((npc.velocity.X > 0f && targetTilePosX < 0f) || (npc.velocity.X < 0f && targetTilePosX > 0f)))
                            {
                                if (npc.velocity.Y > 0f)
                                    npc.velocity.Y += speed * 2f;
                                else
                                    npc.velocity.Y -= speed * 2f;
                            }
                            if (Math.Abs(targetTilePosX) < segmentVelocity * 0.2 && ((npc.velocity.Y > 0f && targetTilePosY < 0f) || (npc.velocity.Y < 0f && targetTilePosY > 0f)))
                            {
                                if (npc.velocity.X > 0f)
                                    npc.velocity.X += speed * 2f;
                                else
                                    npc.velocity.X -= speed * 2f;
                            }
                        }
                        else if (absoluteTilePosX > absoluteTilePosY)
                        {
                            if (npc.velocity.X < targetTilePosX)
                                npc.velocity.X += speed * 1.1f;
                            else if (npc.velocity.X > targetTilePosX)
                                npc.velocity.X -= speed * 1.1f;

                            if ((Math.Abs(npc.velocity.X) + Math.Abs(npc.velocity.Y)) < segmentVelocity * 0.5)
                            {
                                if (npc.velocity.Y > 0f)
                                    npc.velocity.Y += speed;
                                else
                                    npc.velocity.Y -= speed;
                            }
                        }
                        else
                        {
                            if (npc.velocity.Y < targetTilePosY)
                                npc.velocity.Y += speed * 1.1f;
                            else if (npc.velocity.Y > targetTilePosY)
                                npc.velocity.Y -= speed * 1.1f;

                            if ((Math.Abs(npc.velocity.X) + Math.Abs(npc.velocity.Y)) < segmentVelocity * 0.5)
                            {
                                if (npc.velocity.X > 0f)
                                    npc.velocity.X += speed;
                                else
                                    npc.velocity.X -= speed;
                            }
                        }
                    }
                }

                npc.rotation = (float)Math.Atan2(npc.velocity.Y, npc.velocity.X) + MathHelper.PiOver2;

                if (npc.type == HeadType)
                {
                    if (shouldFly)
                    {
                        if (npc.localAI[0] != 1f)
                            npc.netUpdate = true;

                        npc.localAI[0] = 1f;
                    }
                    else
                    {
                        if (npc.localAI[0] != 0f)
                            npc.netUpdate = true;

                        npc.localAI[0] = 0f;
                    }

                    if (((npc.velocity.X > 0f && npc.oldVelocity.X < 0f) || (npc.velocity.X < 0f && npc.oldVelocity.X > 0f) || (npc.velocity.Y > 0f && npc.oldVelocity.Y < 0f) || (npc.velocity.Y < 0f && npc.oldVelocity.Y > 0f)) && !npc.justHit)
                        npc.netUpdate = true;
                }
            }

            // Force the fucker to turn around in ground phase in Master
            // Turns slower if Oblivion is alive, for fairness
            if (npc.type == HeadType && masterMode && !flyAtTarget)
            {
                if (npc.Distance(player.Center) > 2000f)
                    npc.velocity += (player.Center - npc.Center).SafeNormalize(Vector2.UnitY) * (oblivionAlive ? speed : turnSpeed);
            }

            if (NPC.IsMechQueenUp && npc.type == HeadType)
            {
                NPC nPC = Main.npc[NPC.mechQueen];
                Vector2 mechQueenCenter = nPC.GetMechQueenCenter();
                Vector2 mechdusaSpinningVector = new Vector2(0f, 100f);
                Vector2 spinningpoint = mechQueenCenter + mechdusaSpinningVector;
                float mechdusaRotation = nPC.velocity.X * 0.025f;
                spinningpoint = spinningpoint.RotatedBy(mechdusaRotation, mechQueenCenter);
                npc.position = spinningpoint - npc.Size / 2f + nPC.velocity;
                npc.velocity.X = 0f;
                npc.velocity.Y = 0f;
                npc.rotation = mechdusaRotation * 0.75f + (float)Math.PI;
            }

            // Calculate contact damage based on velocity
            float minimalContactDamageVelocity = segmentVelocity * 0.25f;
            float minimalDamageVelocity = segmentVelocity * 0.5f;
            if (npc.type == HeadType)
            {
                if (npc.velocity.Length() <= minimalContactDamageVelocity)
                {
                    npc.damage = (int)Math.Round(npc.defDamage * 0.5);
                }
                else
                {
                    float velocityDamageScalar = MathHelper.Clamp((npc.velocity.Length() - minimalContactDamageVelocity) / minimalDamageVelocity, 0f, 1f);
                    npc.damage = (int)MathHelper.Lerp((float)Math.Round(npc.defDamage * 0.5), npc.defDamage, velocityDamageScalar);
                }
            }
            else
            {
                float bodyAndTailVelocity = (npc.position - npc.oldPosition).Length();
                if (bodyAndTailVelocity <= minimalContactDamageVelocity)
                {
                    npc.damage = 0;
                }
                else
                {
                    float velocityDamageScalar = MathHelper.Clamp((bodyAndTailVelocity - minimalContactDamageVelocity) / minimalDamageVelocity, 0f, 1f);
                    npc.damage = (int)MathHelper.Lerp(0f, npc.defDamage, velocityDamageScalar);
                }
            }

            return false;
        }

    }
}


