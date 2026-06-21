using System;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using CalamityMod;
using CalamityMod.NPCs;
using CalamityMod.Events;
using CalamityMod.World;

namespace CalamityLegendsComeBack.BossAI.ReBack.Prime2041
{
    public class Prime2041 : ModNPC
    {
        public override string Texture => "Terraria/Images/NPC_127";
        public override string BossHeadTexture => "Terraria/Images/NPC_Head_Boss_18";

        public static int primeCannon = -1;
        public static int primeLaser = -1;
        public static int primeVice = -1;
        public static int primeSaw = -1;

        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[NPC.type] = 6;
            NPCID.Sets.NPCBestiaryDrawModifiers bestiaryData = new NPCID.Sets.NPCBestiaryDrawModifiers() { Hide = true };
            NPCID.Sets.NPCBestiaryDrawOffset.Add(Type, bestiaryData);
        }

        public override void SetDefaults()
        {
            NPC.Calamity().canBreakPlayerDefense = true;
            NPC.aiStyle = -1;
            NPC.DR_NERD(0.2f);

            NPC.width = 80;
            NPC.height = 102;
            if (Main.tenthAnniversaryWorld)
                NPC.scale *= 0.5f;
            if (Main.getGoodWorld)
                NPC.scale *= 1.1f;

            NPC.defense = 24;
            NPC.damage = 47;
            NPC.lifeMax = 28000;
            double HPBoost = Prime2041Compat.BossHealthBoost * 0.01;
            NPC.lifeMax += (int)(NPC.lifeMax * HPBoost);

            NPC.knockBackResist = 0f;
            NPC.noGravity = true;
            NPC.noTileCollide = true;
            NPC.boss = true;
            NPC.value = 200000f;
            NPC.HitSound = SoundID.NPCHit4;
            NPC.DeathSound = SoundID.NPCDeath14;
            NPC.Calamity().VulnerableToElectricity = true;
            NPC.Calamity().VulnerableToSickness = false;
            NPC.chaseable = false;
            AnimationType = NPCID.SkeletronPrime;
            Music = MusicID.Boss3;
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(NPC.localAI[0]);
            writer.Write(NPC.localAI[1]);
            writer.Write(NPC.localAI[2]);
            writer.Write(NPC.localAI[3]);
            for (int i = 0; i < 4; i++)
                writer.Write(NPC.Calamity().newAI[i]);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            NPC.localAI[0] = reader.ReadSingle();
            NPC.localAI[1] = reader.ReadSingle();
            NPC.localAI[2] = reader.ReadSingle();
            NPC.localAI[3] = reader.ReadSingle();
            for (int i = 0; i < 4; i++)
                NPC.Calamity().newAI[i] = reader.ReadSingle();
        }

        public override void BossHeadRotation(ref float rotation)
        {
            rotation = (NPC.ai[1] == 1f || NPC.ai[1] == 2f) ? NPC.rotation : 0f;
        }

        public override void FindFrame(int frameHeight)
        {
            if (NPC.ai[1] == 1f || NPC.ai[1] == 2f || NPC.ai[1] == 5f)
            {
                NPC.frameCounter += 1.0;
                if (NPC.frameCounter >= 8.0)
                {
                    NPC.frameCounter = 0.0;
                    NPC.frame.Y += frameHeight;
                    if (NPC.frame.Y >= frameHeight * 6)
                    {
                        NPC.frame.Y = frameHeight * 3;
                    }
                }
                if (NPC.frame.Y < frameHeight * 3)
                {
                    NPC.frame.Y = frameHeight * 3;
                }
            }
            else
            {
                NPC.frameCounter += 1.0;
                if (NPC.frameCounter >= 8.0)
                {
                    NPC.frameCounter = 0.0;
                    NPC.frame.Y += frameHeight;
                    if (NPC.frame.Y >= frameHeight * 3)
                    {
                        NPC.frame.Y = 0;
                    }
                }
                if (NPC.frame.Y >= frameHeight * 3)
                {
                    NPC.frame.Y = 0;
                }
            }
        }

        public override void AI()
        {
            CalamityGlobalNPC calamityGlobalNPC = NPC.Calamity();

            bool bossRush = BossRushEvent.BossRushActive;
            bool masterMode = Main.masterMode || bossRush;
            bool death = CalamityWorld.death || bossRush;

            // Get a target
            if (NPC.target < 0 || NPC.target == Main.maxPlayers || Main.player[NPC.target].dead || !Main.player[NPC.target].active)
                NPC.TargetClosest();

            // Despawn safety
            if (Vector2.Distance(Main.player[NPC.target].Center, NPC.Center) > 3200f)
                NPC.TargetClosest();

            // Percent life remaining
            float lifeRatio = NPC.life / (float)NPC.lifeMax;

            if (NPC.ai[3] != 0f)
                NPC.mechQueen = NPC.whoAmI;

            // Spawn arms
            if (calamityGlobalNPC.newAI[1] == 0f)
            {
                calamityGlobalNPC.newAI[1] = 1f;

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    // Spawn second head in Master Mode
                    // The main head owns the Saw and the Laser in Master Mode
                    if (masterMode)
                    {
                        int head = NPC.NewNPC(NPC.GetSource_FromAI(), (int)NPC.Center.X, (int)NPC.Center.Y, ModContent.NPCType<Prime2041SecondHead>(), NPC.whoAmI);
                        Main.npc[head].ai[0] = NPC.whoAmI;
                        Main.npc[head].target = NPC.target;
                        Main.npc[head].netUpdate = true;
                        NPC.ai[0] = head;

                        int arm = NPC.NewNPC(NPC.GetSource_FromAI(), (int)NPC.Center.X, (int)NPC.Center.Y, ModContent.NPCType<Prime2041Saw>(), NPC.whoAmI);
                        Main.npc[arm].ai[0] = 1f;
                        Main.npc[arm].ai[1] = NPC.whoAmI;
                        Main.npc[arm].target = NPC.target;
                        Main.npc[arm].netUpdate = true;

                        arm = NPC.NewNPC(NPC.GetSource_FromAI(), (int)NPC.Center.X, (int)NPC.Center.Y, ModContent.NPCType<Prime2041Laser>(), NPC.whoAmI);
                        Main.npc[arm].ai[0] = 1f;
                        Main.npc[arm].ai[1] = NPC.whoAmI;
                        Main.npc[arm].target = NPC.target;
                        Main.npc[arm].netUpdate = true;
                        Main.npc[arm].ai[3] = 150f;
                    }
                    else
                    {
                        // In non-master mode, spawn standard arms
                        int arm = NPC.NewNPC(NPC.GetSource_FromAI(), (int)NPC.Center.X, (int)NPC.Center.Y, ModContent.NPCType<Prime2041Cannon>(), NPC.whoAmI);
                        Main.npc[arm].ai[0] = -1f;
                        Main.npc[arm].ai[1] = NPC.whoAmI;
                        Main.npc[arm].target = NPC.target;
                        Main.npc[arm].netUpdate = true;

                        arm = NPC.NewNPC(NPC.GetSource_FromAI(), (int)NPC.Center.X, (int)NPC.Center.Y, ModContent.NPCType<Prime2041Saw>(), NPC.whoAmI);
                        Main.npc[arm].ai[0] = 1f;
                        Main.npc[arm].ai[1] = NPC.whoAmI;
                        Main.npc[arm].target = NPC.target;
                        Main.npc[arm].netUpdate = true;

                        arm = NPC.NewNPC(NPC.GetSource_FromAI(), (int)NPC.Center.X, (int)NPC.Center.Y, ModContent.NPCType<Prime2041Vice>(), NPC.whoAmI);
                        Main.npc[arm].ai[0] = -1f;
                        Main.npc[arm].ai[1] = NPC.whoAmI;
                        Main.npc[arm].target = NPC.target;
                        Main.npc[arm].ai[3] = 150f;
                        Main.npc[arm].netUpdate = true;

                        arm = NPC.NewNPC(NPC.GetSource_FromAI(), (int)NPC.Center.X, (int)NPC.Center.Y, ModContent.NPCType<Prime2041Laser>(), NPC.whoAmI);
                        Main.npc[arm].ai[0] = 1f;
                        Main.npc[arm].ai[1] = NPC.whoAmI;
                        Main.npc[arm].target = NPC.target;
                        Main.npc[arm].netUpdate = true;
                        Main.npc[arm].ai[3] = 150f;
                    }
                }

                NPC.netUpdate = true;
                NPC.SyncExtraAI();
            }

            if (masterMode)
            {
                if (!Main.npc[(int)NPC.ai[0]].active || Main.npc[(int)NPC.ai[0]].type != ModContent.NPCType<Prime2041SecondHead>())
                {
                    NPC.life = 0;
                    NPC.HitEffect(new NPC.HitInfo());
                    NPC.active = false;
                    NPC.netUpdate = true;
                }
                else
                {
                    // Link the HP of both heads
                    if (NPC.life > Main.npc[(int)NPC.ai[0]].life)
                        NPC.life = Main.npc[(int)NPC.ai[0]].life;

                    // Push away from the lead head if too close, pull closer if too far, if Mechdusa isn't real
                    if (!NPC.IsMechQueenUp)
                    {
                        float pushVelocity = 0.25f;
                        if (Vector2.Distance(NPC.Center, Main.npc[(int)NPC.ai[0]].Center) < 80f * NPC.scale)
                        {
                            if (NPC.position.X < Main.npc[(int)NPC.ai[0]].position.X)
                                NPC.velocity.X -= pushVelocity;
                            else
                                NPC.velocity.X += pushVelocity;

                            if (NPC.position.Y < Main.npc[(int)NPC.ai[0]].position.Y)
                                NPC.velocity.Y -= pushVelocity;
                            else
                                NPC.velocity.Y += pushVelocity;
                        }
                        else if (Vector2.Distance(NPC.Center, Main.npc[(int)NPC.ai[0]].Center) > 240f * NPC.scale)
                        {
                            if (NPC.position.X < Main.npc[(int)NPC.ai[0]].position.X)
                                NPC.velocity.X += pushVelocity;
                            else
                                NPC.velocity.X -= pushVelocity;

                            if (NPC.position.Y < Main.npc[(int)NPC.ai[0]].position.Y)
                                NPC.velocity.Y += pushVelocity;
                            else
                                NPC.velocity.Y -= pushVelocity;
                        }
                    }
                }
            }

            // Check if arms are alive
            bool cannonAlive = false;
            bool laserAlive = false;
            bool viceAlive = false;
            bool sawAlive = false;
            if (primeCannon != -1)
            {
                if (Main.npc[primeCannon].active && Main.npc[primeCannon].type == ModContent.NPCType<Prime2041Cannon>())
                    cannonAlive = true;
            }
            if (primeLaser != -1)
            {
                if (Main.npc[primeLaser].active && Main.npc[primeLaser].type == ModContent.NPCType<Prime2041Laser>())
                    laserAlive = true;
            }
            if (primeVice != -1)
            {
                if (Main.npc[primeVice].active && Main.npc[primeVice].type == ModContent.NPCType<Prime2041Vice>())
                    viceAlive = true;
            }
            if (primeSaw != -1)
            {
                if (Main.npc[primeSaw].active && Main.npc[primeSaw].type == ModContent.NPCType<Prime2041Saw>())
                    sawAlive = true;
            }
            bool allArmsDead = !cannonAlive && !laserAlive && !viceAlive && !sawAlive;
            NPC.chaseable = allArmsDead;

            NPC.defense = NPC.defDefense;

            // Phases
            bool phase2 = lifeRatio < 0.66f;
            bool spawnDestroyer = lifeRatio < 0.75f && masterMode && !bossRush && NPC.localAI[2] == 0f;
            bool phase3 = lifeRatio < 0.33f;
            bool spawnRetinazer = lifeRatio < 0.5f && masterMode && !bossRush && NPC.localAI[2] == 1f;

            // Spawn The Destroyer in Master Mode
            if (spawnDestroyer)
            {
                Player destroyerSpawnPlayer = Main.player[Player.FindClosest(NPC.position, NPC.width, NPC.height)];
                SoundEngine.PlaySound(SoundID.Roar, destroyerSpawnPlayer.Center);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                    Mechs2041Spawn.SpawnDestroyer(destroyerSpawnPlayer, NPC.GetSource_FromAI());

                NPC.localAI[2] = 1f;
                NPC.SyncVanillaLocalAI();
            }

            // Spawn Retinazer in Master Mode
            if (spawnRetinazer)
            {
                Player retinazerSpawnPlayer = Main.player[Player.FindClosest(NPC.position, NPC.width, NPC.height)];
                SoundEngine.PlaySound(SoundID.Roar, retinazerSpawnPlayer.Center);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                    Mechs2041Spawn.SpawnRetinazer(retinazerSpawnPlayer, NPC.GetSource_FromAI());

                NPC.localAI[2] = 2f;
                NPC.SyncVanillaLocalAI();
            }

            // Despawn
            if (Main.player[NPC.target].dead || Math.Abs(NPC.Center.X - Main.player[NPC.target].Center.X) > 6000f || Math.Abs(NPC.Center.Y - Main.player[NPC.target].Center.Y) > 6000f)
            {
                NPC.TargetClosest();
                if (Main.player[NPC.target].dead || Math.Abs(NPC.Center.X - Main.player[NPC.target].Center.X) > 6000f || Math.Abs(NPC.Center.Y - Main.player[NPC.target].Center.Y) > 6000f)
                    NPC.ai[1] = 3f;
            }

            // Activate daytime enrage
            if (Main.IsItDay() && !bossRush && NPC.ai[1] != 3f && NPC.ai[1] != 2f)
            {
                // Heal
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    int healAmt = NPC.life - 300;
                    if (healAmt < 0)
                    {
                        int absHeal = Math.Abs(healAmt);
                        NPC.life += absHeal;
                        NPC.HealEffect(absHeal, true);
                        NPC.netUpdate = true;
                    }
                }

                NPC.ai[1] = 2f;
                SoundEngine.PlaySound(SoundID.ForceRoar, NPC.Center);
            }

            // Adjust slowing debuff immunity
            bool immuneToSlowingDebuffs = NPC.ai[1] == 5f;
            NPC.buffImmune[ModContent.BuffType<CalamityMod.Buffs.StatDebuffs.TemporalSadness>()] = immuneToSlowingDebuffs;
            NPC.buffImmune[ModContent.BuffType<CalamityMod.Buffs.StatDebuffs.Eutrophication>()] = immuneToSlowingDebuffs;
            NPC.buffImmune[ModContent.BuffType<CalamityMod.Buffs.StatDebuffs.TimeDistortion>()] = immuneToSlowingDebuffs;
            NPC.buffImmune[ModContent.BuffType<CalamityMod.Buffs.StatDebuffs.GalvanicCorrosion>()] = immuneToSlowingDebuffs;
            NPC.buffImmune[ModContent.BuffType<CalamityMod.Buffs.DamageOverTime.Vaporfied>()] = immuneToSlowingDebuffs;
            NPC.buffImmune[BuffID.Slow] = immuneToSlowingDebuffs;
            NPC.buffImmune[BuffID.Webbed] = immuneToSlowingDebuffs;

            bool normalLaserRotation = NPC.localAI[1] % 2f == 0f;

            // Prevents cheap hits
            bool canUseAttackInMaster = NPC.position.Y < Main.player[NPC.target].position.Y - 350f;

            // Float near player
            if (NPC.ai[1] == 0f || NPC.ai[1] == 4f)
            {
                NPC.damage = 0;

                // Start other phases; if arms are dead, start with spin phase
                bool otherHeadChargingOrSpinning = masterMode && (Main.npc[(int)NPC.ai[0]].ai[1] == 5f || Main.npc[(int)NPC.ai[0]].ai[1] == 1f);
                if (phase2 || CalamityWorld.LegendaryMode || allArmsDead)
                {
                    // Start spin phase after 1.5 seconds
                    NPC.ai[2] += phase3 ? 1.5f : 1f;
                    if (NPC.ai[2] >= (90f - (death ? (masterMode ? 15f : 60f) * (1f - lifeRatio) : 0f)) && (!otherHeadChargingOrSpinning || !masterMode || phase3) && (canUseAttackInMaster || !masterMode))
                    {
                        bool shouldSpinAround = NPC.ai[1] == 4f && NPC.position.Y < Main.player[NPC.target].position.Y - 400f &&
                            Vector2.Distance(Main.player[NPC.target].Center, NPC.Center) < 600f && Vector2.Distance(Main.player[NPC.target].Center, NPC.Center) > 400f;

                        if (shouldSpinAround || NPC.ai[1] != 4f)
                        {
                            if (shouldSpinAround)
                            {
                                NPC.localAI[3] = 300f;
                                NPC.SyncVanillaLocalAI();
                            }

                            NPC.ai[2] = 0f;
                            NPC.ai[1] = shouldSpinAround ? 5f : 1f;
                            NPC.TargetClosest();
                            NPC.netUpdate = true;
                        }
                    }
                }

                if (NPC.IsMechQueenUp)
                    NPC.rotation = NPC.rotation.AngleLerp(NPC.velocity.X / 15f * 0.5f, 0.75f);
                else
                    NPC.rotation = NPC.velocity.X / 15f;

                float acceleration = (bossRush ? 0.2f : masterMode ? 0.125f : 0.1f) + (death ? 0.05f * (1f - lifeRatio) : 0f);
                float accelerationMult = 1f;
                if (!cannonAlive)
                {
                    acceleration += 0.025f;
                    accelerationMult += 0.5f;
                }
                if (!laserAlive)
                {
                    acceleration += 0.025f;
                    accelerationMult += 0.5f;
                }
                if (!viceAlive)
                    acceleration += 0.025f;
                if (!sawAlive)
                    acceleration += 0.025f;
                if (masterMode)
                    acceleration *= accelerationMult;

                float topVelocity = acceleration * 100f;
                float deceleration = masterMode ? 0.7f : 0.85f;

                float headDecelerationUpDist = 0f;
                float headDecelerationDownDist = 0f;
                float headDecelerationHorizontalDist = 0f;
                int headHorizontalDirection = ((!(Main.player[NPC.target].Center.X < NPC.Center.X)) ? 1 : (-1));
                if (NPC.IsMechQueenUp)
                {
                    headDecelerationHorizontalDist = -150f * (float)headHorizontalDirection;
                    headDecelerationUpDist = -100f;
                    headDecelerationDownDist = -100f;
                }

                if (NPC.position.Y > Main.player[NPC.target].position.Y - (400f + headDecelerationUpDist))
                {
                    if (NPC.velocity.Y > 0f)
                        NPC.velocity.Y *= deceleration;

                    NPC.velocity.Y -= acceleration;

                    if (NPC.velocity.Y > topVelocity)
                        NPC.velocity.Y = topVelocity;
                }
                else if (NPC.position.Y < Main.player[NPC.target].position.Y - (450f + headDecelerationDownDist))
                {
                    if (NPC.velocity.Y < 0f)
                        NPC.velocity.Y *= deceleration;

                    NPC.velocity.Y += acceleration;

                    if (NPC.velocity.Y < -topVelocity)
                        NPC.velocity.Y = -topVelocity;
                }

                if (NPC.Center.X > Main.player[NPC.target].Center.X + (400f + headDecelerationHorizontalDist))
                {
                    if (NPC.velocity.X > 0f)
                        NPC.velocity.X *= deceleration;

                    NPC.velocity.X -= acceleration;

                    if (NPC.velocity.X > topVelocity)
                        NPC.velocity.X = topVelocity;
                }
                if (NPC.Center.X < Main.player[NPC.target].Center.X - (400f + headDecelerationHorizontalDist))
                {
                    if (NPC.velocity.X < 0f)
                        NPC.velocity.X *= deceleration;

                    NPC.velocity.X += acceleration;

                    if (NPC.velocity.X < -topVelocity)
                        NPC.velocity.X = -topVelocity;
                }
            }
            else
            {
                // Spinning
                if (NPC.ai[1] == 1f)
                {
                    NPC.defense *= 2;
                    NPC.damage = NPC.defDamage * 2;

                    calamityGlobalNPC.CurrentlyIncreasingDefenseOrDR = true;

                    if (phase2 && Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        NPC.localAI[0] += 1f;
                        if (NPC.localAI[0] >= 45f)
                        {
                            NPC.localAI[0] = 0f;

                            int totalProjectiles = bossRush ? 24 : death ? (masterMode ? 15 : 18) : 12;
                            float radians = MathHelper.TwoPi / totalProjectiles;
                            int type = ProjectileID.DeathLaser;
                            int damage = NPC.GetProjectileDamage(type);

                            if (Prime2041Compat.EarlyHardmodeProgressionRework && !BossRushEvent.BossRushActive)
                            {
                                double firstMechMultiplier = CalamityGlobalNPC.EarlyHardmodeProgressionReworkFirstMechStatMultiplier_Expert;
                                double secondMechMultiplier = CalamityGlobalNPC.EarlyHardmodeProgressionReworkSecondMechStatMultiplier_Expert;
                                if (!NPC.downedMechBossAny)
                                    damage = (int)(damage * firstMechMultiplier);
                                else if ((!NPC.downedMechBoss1 && !NPC.downedMechBoss2) || (!NPC.downedMechBoss2 && !NPC.downedMechBoss3) || (!NPC.downedMechBoss3 && !NPC.downedMechBoss1))
                                    damage = (int)(damage * secondMechMultiplier);
                            }

                            float velocity = 3f;
                            double angleA = radians * 0.5;
                            double angleB = MathHelper.ToRadians(90f) - angleA;
                            float velocityX = (float)(velocity * Math.Sin(angleA) / Math.Sin(angleB));
                            Vector2 spinningPoint = normalLaserRotation ? new Vector2(0f, -velocity) : new Vector2(-velocityX, -velocity);
                            for (int k = 0; k < totalProjectiles; k++)
                            {
                                Vector2 laserFireDirection = spinningPoint.RotatedBy(radians * k);
                                int proj = Projectile.NewProjectile(NPC.GetSource_FromAI(), NPC.Center + laserFireDirection.SafeNormalize(Vector2.UnitY) * 100f, laserFireDirection, type, damage, 0f, Main.myPlayer, 1f, 0f);
                                Main.projectile[proj].timeLeft = 900;
                            }
                            NPC.localAI[1] += 1f;
                        }
                    }

                    NPC.ai[2] += 1f;
                    if (NPC.ai[2] == 2f)
                        SoundEngine.PlaySound(SoundID.ForceRoar, NPC.Center);

                    float phaseTimer = 240f;
                    if (phase2 && !phase3)
                        phaseTimer += 60f;

                    if (NPC.ai[2] >= (phaseTimer - (death ? 60f * (1f - lifeRatio) : 0f)))
                    {
                        NPC.TargetClosest();
                        NPC.ai[2] = 0f;
                        NPC.ai[1] = 4f;
                        NPC.localAI[0] = 0f;
                    }

                    if (NPC.IsMechQueenUp)
                        NPC.rotation = NPC.rotation.AngleLerp(NPC.velocity.X / 15f * 0.5f, 0.75f);
                    else
                        NPC.rotation += NPC.direction * 0.3f;

                    Vector2 headPosition = NPC.Center;
                    float headTargetX = Main.player[NPC.target].Center.X - headPosition.X;
                    float headTargetY = Main.player[NPC.target].Center.Y - headPosition.Y;
                    float headTargetDistance = (float)Math.Sqrt(headTargetX * headTargetX + headTargetY * headTargetY);

                    float speed = bossRush ? 12f : (masterMode ? 8f : 6f);
                    if (phase2)
                        speed += 0.5f;
                    if (phase3)
                        speed += 0.5f;

                    if (headTargetDistance > 150f)
                    {
                        float baseDistanceVelocityMult = 1f + MathHelper.Clamp((headTargetDistance - 150f) * 0.0015f, 0.05f, 1.5f);
                        speed *= baseDistanceVelocityMult;
                    }

                    if (NPC.IsMechQueenUp)
                    {
                        float mechdusaSpeedMult = (NPC.npcsFoundForCheckActive[ModContent.NPCType<Destroyer2041Body>()] ? 0.6f : 0.75f);
                        speed *= mechdusaSpeedMult;
                    }

                    headTargetDistance = speed / headTargetDistance;
                    NPC.velocity.X = headTargetX * headTargetDistance;
                    NPC.velocity.Y = headTargetY * headTargetDistance;

                    if (NPC.IsMechQueenUp)
                    {
                        float mechdusaAccelMult = Vector2.Distance(NPC.Center, Main.player[NPC.target].Center);
                        if (mechdusaAccelMult < 0.1f)
                            mechdusaAccelMult = 0f;

                        if (mechdusaAccelMult < speed)
                            NPC.velocity = NPC.velocity.SafeNormalize(Vector2.Zero) * mechdusaAccelMult;
                    }
                }

                // Daytime enrage
                if (NPC.ai[1] == 2f)
                {
                    NPC.damage = 1000;
                    calamityGlobalNPC.DR = 0.9999f;
                    calamityGlobalNPC.unbreakableDR = true;

                    calamityGlobalNPC.CurrentlyEnraged = true;
                    calamityGlobalNPC.CurrentlyIncreasingDefenseOrDR = true;

                    if (NPC.IsMechQueenUp)
                        NPC.rotation = NPC.rotation.AngleLerp(NPC.velocity.X / 15f * 0.5f, 0.75f);
                    else
                        NPC.rotation += NPC.direction * 0.3f;

                    Vector2 enragedHeadPosition = NPC.Center;
                    float enragedHeadTargetX = Main.player[NPC.target].Center.X - enragedHeadPosition.X;
                    float enragedHeadTargetY = Main.player[NPC.target].Center.Y - enragedHeadPosition.Y;
                    float enragedHeadTargetDist = (float)Math.Sqrt(enragedHeadTargetX * enragedHeadTargetX + enragedHeadTargetY * enragedHeadTargetY);

                    float enragedHeadSpeed = 10f;
                    enragedHeadSpeed += enragedHeadTargetDist / 100f;
                    if (enragedHeadSpeed < 8f)
                        enragedHeadSpeed = 8f;
                    if (enragedHeadSpeed > 32f)
                        enragedHeadSpeed = 32f;

                    enragedHeadTargetDist = enragedHeadSpeed / enragedHeadTargetDist;
                    NPC.velocity.X = enragedHeadTargetX * enragedHeadTargetDist;
                    NPC.velocity.Y = enragedHeadTargetY * enragedHeadTargetDist;

                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        NPC.localAI[0] += 1f;
                        if (NPC.localAI[0] >= 60f)
                        {
                            NPC.localAI[0] = 0f;
                            Vector2 headCenter = NPC.Center;
                            if (Collision.CanHit(headCenter, 1, 1, Main.player[NPC.target].position, Main.player[NPC.target].width, Main.player[NPC.target].height))
                            {
                                enragedHeadSpeed = 7f;
                                float enragedHeadSkullTargetX = Main.player[NPC.target].Center.X - headCenter.X + Main.rand.Next(-20, 21);
                                float enragedHeadSkullTargetY = Main.player[NPC.target].Center.Y - headCenter.Y + Main.rand.Next(-20, 21);
                                float enragedHeadSkullTargetDist = (float)Math.Sqrt(enragedHeadSkullTargetX * enragedHeadSkullTargetX + enragedHeadSkullTargetY * enragedHeadSkullTargetY);
                                enragedHeadSkullTargetDist = enragedHeadSpeed / enragedHeadSkullTargetDist;
                                enragedHeadSkullTargetX *= enragedHeadSkullTargetDist;
                                enragedHeadSkullTargetY *= enragedHeadSkullTargetDist;

                                Vector2 value = new Vector2(enragedHeadSkullTargetX * 1f + Main.rand.Next(-50, 51) * 0.01f, enragedHeadSkullTargetY * 1f + Main.rand.Next(-50, 51) * 0.01f).SafeNormalize(Vector2.UnitY);
                                value *= enragedHeadSpeed;
                                value += NPC.velocity;
                                enragedHeadSkullTargetX = value.X;
                                enragedHeadSkullTargetY = value.Y;

                                int type = ProjectileID.Skull;
                                headCenter += value * 5f;
                                int enragedSkulls = Projectile.NewProjectile(NPC.GetSource_FromAI(), headCenter.X, headCenter.Y, enragedHeadSkullTargetX, enragedHeadSkullTargetY, type, 250, 0f, Main.myPlayer, -3f, 0f);
                                Main.projectile[enragedSkulls].timeLeft = 300;
                            }
                        }
                    }
                }

                // Despawning
                if (NPC.ai[1] == 3f)
                {
                    NPC.damage = 0;

                    if (NPC.IsMechQueenUp)
                    {
                        int mechdusaBossDespawning = NPC.FindFirstNPC(ModContent.NPCType<Twins2041Retinazer>());
                        if (mechdusaBossDespawning >= 0)
                            Main.npc[mechdusaBossDespawning].EncourageDespawn(5);

                        mechdusaBossDespawning = NPC.FindFirstNPC(ModContent.NPCType<Twins2041Spazmatism>());
                        if (mechdusaBossDespawning >= 0)
                            Main.npc[mechdusaBossDespawning].EncourageDespawn(5);

                        if (!NPC.AnyNPCs(ModContent.NPCType<Twins2041Retinazer>()) && !NPC.AnyNPCs(ModContent.NPCType<Twins2041Spazmatism>()))
                        {
                            mechdusaBossDespawning = NPC.FindFirstNPC(ModContent.NPCType<Destroyer2041Head>());
                            if (mechdusaBossDespawning >= 0)
                                Main.npc[mechdusaBossDespawning].Transform(ModContent.NPCType<Destroyer2041Tail>());

                            NPC.EncourageDespawn(5);
                        }

                        NPC.velocity.Y += 0.1f;
                        if (NPC.velocity.Y < 0f)
                            NPC.velocity.Y *= 0.95f;

                        NPC.velocity.X *= 0.95f;
                        if (NPC.velocity.Y > 13f)
                            NPC.velocity.Y = 13f;
                    }
                    else
                    {
                        NPC.velocity.Y += 0.1f;
                        if (NPC.velocity.Y < 0f)
                            NPC.velocity.Y *= 0.9f;

                        NPC.velocity.X *= 0.9f;

                        if (NPC.timeLeft > 500)
                            NPC.timeLeft = 500;
                    }
                }

                // Fly around in a circle
                if (NPC.ai[1] == 5f)
                {
                    NPC.damage = 0;

                    NPC.ai[2] += 1f;

                    NPC.rotation = NPC.velocity.X / 50f;

                    float skullSpawnDivisor = bossRush ? 9f : death ? 15f - (float)Math.Round((masterMode ? 3f : 5f) * (1f - lifeRatio)) : 15f;
                    float totalSkulls = 12f;
                    int skullSpread = bossRush ? 250 : death ? (masterMode ? 125 : 150) : 100;

                    float spinVelocity = 30f;
                    if (NPC.ai[2] == 2f)
                    {
                        SoundEngine.PlaySound(SoundID.ForceRoar, NPC.Center);

                        if (Main.player[NPC.target].velocity.X > 0f)
                            calamityGlobalNPC.newAI[0] = 1f;
                        else if (Main.player[NPC.target].velocity.X < 0f)
                            calamityGlobalNPC.newAI[0] = -1f;
                        else
                            calamityGlobalNPC.newAI[0] = Main.player[NPC.target].direction;

                        NPC.velocity.X = MathHelper.Pi * NPC.localAI[3] / spinVelocity;
                        NPC.velocity *= -calamityGlobalNPC.newAI[0];
                        NPC.SyncExtraAI();
                        NPC.netUpdate = true;
                    }
                    else if (NPC.ai[2] > 2f)
                    {
                        NPC.velocity = NPC.velocity.RotatedBy(MathHelper.Pi / spinVelocity * -calamityGlobalNPC.newAI[0]);
                        if (NPC.ai[2] == 3f)
                            NPC.velocity *= 0.6f;

                        if (NPC.ai[2] % skullSpawnDivisor == 0f)
                        {
                            NPC.localAI[0] += 1f;

                            if (Vector2.Distance(Main.player[NPC.target].Center, NPC.Center) > 64f)
                            {
                                if (Main.netMode != NetmodeID.MultiplayerClient)
                                {
                                    Vector2 headCenter = NPC.Center;
                                    float enragedHeadSpeed = (masterMode ? 5f : 4f) + (death ? (masterMode ? 1f : 2f) * (1f - lifeRatio) : 0f);
                                    float enragedHeadSkullTargetX = Main.player[NPC.target].Center.X - headCenter.X + Main.rand.Next(-20, 21);
                                    float enragedHeadSkullTargetY = Main.player[NPC.target].Center.Y - headCenter.Y + Main.rand.Next(-20, 21);
                                    float enragedHeadSkullTargetDist = (float)Math.Sqrt(enragedHeadSkullTargetX * enragedHeadSkullTargetX + enragedHeadSkullTargetY * enragedHeadSkullTargetY);
                                    enragedHeadSkullTargetDist = enragedHeadSpeed / enragedHeadSkullTargetDist;
                                    enragedHeadSkullTargetX *= enragedHeadSkullTargetDist;
                                    enragedHeadSkullTargetY *= enragedHeadSkullTargetDist;

                                    Vector2 value = new Vector2(enragedHeadSkullTargetX + Main.rand.Next(-skullSpread, skullSpread + 1) * 0.01f, enragedHeadSkullTargetY + Main.rand.Next(-skullSpread, skullSpread + 1) * 0.01f).SafeNormalize(Vector2.UnitY);
                                    value *= enragedHeadSpeed;
                                    enragedHeadSkullTargetX = value.X;
                                    enragedHeadSkullTargetY = value.Y;

                                    int type = ProjectileID.Skull;
                                    int damage = NPC.GetProjectileDamage(type);

                                    if (Prime2041Compat.EarlyHardmodeProgressionRework && !BossRushEvent.BossRushActive)
                                    {
                                        double firstMechMultiplier = CalamityGlobalNPC.EarlyHardmodeProgressionReworkFirstMechStatMultiplier_Expert;
                                        double secondMechMultiplier = CalamityGlobalNPC.EarlyHardmodeProgressionReworkSecondMechStatMultiplier_Expert;
                                        if (!NPC.downedMechBossAny)
                                            damage = (int)(damage * firstMechMultiplier);
                                        else if ((!NPC.downedMechBoss1 && !NPC.downedMechBoss2) || (!NPC.downedMechBoss2 && !NPC.downedMechBoss3) || (!NPC.downedMechBoss3 && !NPC.downedMechBoss1))
                                            damage = (int)(damage * secondMechMultiplier);
                                    }

                                    if (NPC.localAI[0] % 3f == 0f)
                                    {
                                        int probeLimit = death ? (masterMode ? 3 : 4) : 2;
                                        if (NPC.CountNPCS(NPCID.Probe) < probeLimit)
                                            NPC.NewNPC(NPC.GetSource_FromAI(), (int)headCenter.X, (int)headCenter.Y + 30, NPCID.Probe);
                                    }

                                    int enragedSkulls = Projectile.NewProjectile(NPC.GetSource_FromAI(), headCenter.X, headCenter.Y + 30f, enragedHeadSkullTargetX, enragedHeadSkullTargetY, type, damage, 0f, Main.myPlayer, -3f, 0f);
                                    Main.projectile[enragedSkulls].timeLeft = 480;
                                    Main.projectile[enragedSkulls].tileCollide = false;
                                }
                            }

                            if (NPC.localAI[0] >= totalSkulls)
                            {
                                NPC.velocity = NPC.velocity.SafeNormalize(Vector2.UnitY);

                                NPC.ai[1] = phase3 ? 6f : 1f;
                                NPC.ai[2] = 0f;
                                NPC.localAI[3] = 0f;
                                NPC.localAI[0] = 0f;
                                calamityGlobalNPC.newAI[0] = 0f;
                                NPC.SyncVanillaLocalAI();
                                NPC.SyncExtraAI();
                                NPC.TargetClosest();
                                NPC.netUpdate = true;
                            }
                        }
                    }
                }

                // Fly overhead and spit missiles
                if (NPC.ai[1] == 6f)
                {
                    NPC.damage = 0;

                    NPC.rotation = NPC.velocity.X / 15f;

                    float flightVelocity = bossRush ? 28f : death ? 24f : 20f;
                    float flightAcceleration = bossRush ? 1.12f : death ? 0.96f : 0.8f;

                    if (masterMode)
                    {
                        flightVelocity += 4f;
                        flightAcceleration += 0.16f;
                    }

                    Vector2 destination = new Vector2(Main.player[NPC.target].Center.X, Main.player[NPC.target].Center.Y - 500f);
                    NPC.SimpleFlyMovement((destination - NPC.Center).SafeNormalize(Vector2.UnitY) * flightVelocity, flightAcceleration);

                    NPC.localAI[3] += 1f;
                    if (Vector2.Distance(NPC.Center, destination) < 80f || NPC.ai[2] > 0f || NPC.localAI[3] > 120f)
                    {
                        float missileSpawnDivisor = death ? 8f : (masterMode ? 10f : 12f);
                        float totalMissiles = masterMode ? 12f : 10f;
                        NPC.ai[2] += 1f;
                        if (NPC.ai[2] % missileSpawnDivisor == 0f)
                        {
                            NPC.localAI[0] += 1f;

                            if (Main.netMode != NetmodeID.MultiplayerClient)
                            {
                                Vector2 velocity = new Vector2(-1f * (float)Main.rand.NextDouble() * 5f, 1f);
                                velocity = velocity.RotatedBy((Main.rand.NextDouble() - 0.5) * MathHelper.PiOver4);
                                int type = ProjectileID.RocketSkeleton;
                                int damage = NPC.GetProjectileDamage(type);

                                if (Prime2041Compat.EarlyHardmodeProgressionRework && !BossRushEvent.BossRushActive)
                                {
                                    double firstMechMultiplier = CalamityGlobalNPC.EarlyHardmodeProgressionReworkFirstMechStatMultiplier_Expert;
                                    double secondMechMultiplier = CalamityGlobalNPC.EarlyHardmodeProgressionReworkSecondMechStatMultiplier_Expert;
                                    if (!NPC.downedMechBossAny)
                                        damage = (int)(damage * firstMechMultiplier);
                                    else if ((!NPC.downedMechBoss1 && !NPC.downedMechBoss2) || (!NPC.downedMechBoss2 && !NPC.downedMechBoss3) || (!NPC.downedMechBoss3 && !NPC.downedMechBoss1))
                                        damage = (int)(damage * secondMechMultiplier);
                                }

                                int proj = Projectile.NewProjectile(NPC.GetSource_FromAI(), NPC.Center.X + Main.rand.Next(NPC.width / 2), NPC.Center.Y + 30f, velocity.X, velocity.Y, type, damage, 0f, Main.myPlayer, NPC.target, 1f);
                                Main.projectile[proj].timeLeft = 540;
                            }

                            SoundEngine.PlaySound(SoundID.Item62, NPC.Center);

                            if (NPC.localAI[0] >= totalMissiles)
                            {
                                NPC.ai[1] = 0f;
                                NPC.ai[2] = 0f;
                                NPC.localAI[3] = 0f;
                                calamityGlobalNPC.newAI[0] = 0f;
                                NPC.localAI[0] = 0f;
                                NPC.SyncVanillaLocalAI();
                                NPC.SyncExtraAI();
                                NPC.TargetClosest();
                                NPC.netUpdate = true;
                            }
                        }
                    }
                }
            }
        }

        public override void ApplyDifficultyAndPlayerScaling(int numPlayers, float balance, float bossAdjustment)
        {
            NPC.lifeMax = (int)(NPC.lifeMax * 0.75f * balance * bossAdjustment);
            NPC.damage = (int)(NPC.damage * NPC.GetExpertDamageMultiplier());
        }

        public override void BossLoot(ref string name, ref int potionType) => potionType = ItemID.GreaterHealingPotion;

        public override void ModifyNPCLoot(NPCLoot npcLoot)
        {
            npcLoot.Add(ItemDropRule.Common(ItemID.HallowedBar, 1, 20, 35));
            npcLoot.Add(ItemDropRule.Common(ItemID.SoulofFright, 1, 25, 40));
        }

        public override bool CheckActive() => false;

        public override bool CheckDead()
        {
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC nPC = Main.npc[i];
                if (nPC.active && nPC.type == ModContent.NPCType<Prime2041SecondHead>() && nPC.life > 0)
                {
                    nPC.life = 0;
                    nPC.HitEffect(new NPC.HitInfo());
                    nPC.checkDead();
                    nPC.active = false;
                    nPC.netUpdate = true;
                }
            }

            return true;
        }

        public override void HitEffect(NPC.HitInfo hit)
        {
            if (NPC.life <= 0)
            {
                if (Main.netMode != NetmodeID.Server)
                {
                    Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, 149);
                    Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, 150);

                    for (int g = 0; g < 4; g++)
                    {
                        int num802 = Gore.NewGore(NPC.GetSource_Death(), NPC.position, default(Vector2), Main.rand.Next(61, 64));
                        Main.gore[num802].velocity *= 0.4f;
                    }
                }

                for (int num798 = 0; num798 < 10; num798++)
                {
                    int num799 = Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.Smoke, 0f, 0f, 100, default(Color), 1.5f);
                    Main.dust[num799].velocity *= 1.4f;
                }

                for (int num800 = 0; num800 < 5; num800++)
                {
                    int num801 = Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.Torch, 0f, 0f, 100, default(Color), 2.5f);
                    Main.dust[num801].noGravity = true;
                    Main.dust[num801].velocity *= 5f;
                }
            }
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            bool bossRush = BossRushEvent.BossRushActive;
            bool masterMode = Main.masterMode || bossRush;
            bool revenge = CalamityWorld.revenge || bossRush;

            if (masterMode && revenge)
            {
                Texture2D texture = ModContent.Request<Texture2D>("CalamityLegendsComeBack/BossAI/ReBack/Prime2041/ChadPrime").Value;
                SpriteEffects effects = NPC.spriteDirection == 1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
                Vector2 drawOrigin = NPC.frame.Size() / 2f;
                Vector2 drawPos = NPC.Center - screenPos;
                spriteBatch.Draw(texture, drawPos, NPC.frame, NPC.GetAlpha(drawColor), NPC.rotation, drawOrigin, NPC.scale, effects, 0f);
                return false;
            }
            return true;
        }

        public override void PostDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            bool bossRush = BossRushEvent.BossRushActive;
            bool masterMode = Main.masterMode || bossRush;
            bool revenge = CalamityWorld.revenge || bossRush;

            Texture2D glowTexture;
            Color eyesColor;

            if (masterMode && revenge)
            {
                glowTexture = ModContent.Request<Texture2D>("CalamityLegendsComeBack/BossAI/ReBack/Prime2041/ChadPrimeHeadGlow").Value;
                eyesColor = new Color(255, 255, 0, NPC.alpha);
            }
            else
            {
                glowTexture = ModContent.Request<Texture2D>("Terraria/Images/Glow_11").Value;
                eyesColor = new Color(255, 255, 255, NPC.alpha);
            }

            if (glowTexture != null)
            {
                SpriteEffects effects = NPC.spriteDirection == 1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
                Vector2 drawOrigin = NPC.frame.Size() / 2f;
                Vector2 drawPos = NPC.Center - screenPos;
                spriteBatch.Draw(glowTexture, drawPos, NPC.frame, eyesColor, NPC.rotation, drawOrigin, NPC.scale, effects, 0f);
            }
        }
    }
}
