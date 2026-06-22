using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using CalamityMod;
using CalamityMod.NPCs;
using CalamityMod.Events;
using CalamityMod.World;

namespace CalamityLegendsComeBack.BossAI.ReBack.Prime2041
{
    public abstract class Prime2041Arm : ModNPC
    {
        protected static bool IsPrime2041Head(NPC npc)
        {
            return npc.active && (npc.type == ModContent.NPCType<Prime2041>() || npc.type == ModContent.NPCType<Prime2041SecondHead>());
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            NPC parent = Main.npc[(int)NPC.ai[1]];
            if (IsPrime2041Head(parent))
            {
                Vector2 armCenter = NPC.Center;
                Vector2 headCenter = parent.Center;
                Texture2D chainTexture = TextureAssets.Chain12.Value;
                Vector2 vector = headCenter - armCenter;
                float rotation = vector.ToRotation() - MathHelper.PiOver2;
                bool drawChain = true;
                while (drawChain)
                {
                    float length = vector.Length();
                    if (length < 16f || float.IsNaN(length))
                    {
                        drawChain = false;
                    }
                    else
                    {
                        vector.Normalize();
                        vector *= 16f;
                        armCenter += vector;
                        vector = headCenter - armCenter;
                        Color color = Lighting.GetColor((int)armCenter.X / 16, (int)armCenter.Y / 16);
                        spriteBatch.Draw(chainTexture, armCenter - screenPos, null, color, rotation, new Vector2(chainTexture.Width * 0.5f, chainTexture.Height * 0.5f), 1f, SpriteEffects.None, 0f);
                    }
                }
            }
            return true;
        }

        public override void ApplyDifficultyAndPlayerScaling(int numPlayers, float balance, float bossAdjustment)
        {
            NPC.lifeMax = (int)(NPC.lifeMax * 0.75f * balance * bossAdjustment);
            NPC.damage = (int)(NPC.damage * NPC.GetExpertDamageMultiplier());
        }

        public override void HitEffect(NPC.HitInfo hit)
        {
            if (NPC.life <= 0)
            {
                if (Main.netMode != NetmodeID.Server)
                {
                    for (int i = 0; i < 15; i++)
                    {
                        int dustType = Main.rand.NextBool() ? DustID.Iron : DustID.Smoke;
                        Dust.NewDust(NPC.position, NPC.width, NPC.height, dustType, NPC.velocity.X * 0.5f, NPC.velocity.Y * 0.5f, 100, default, 1.5f);
                    }
                }
            }
        }
    }

    public class Prime2041Saw : Prime2041Arm
    {
        public override string Texture => "Terraria/Images/NPC_129";

        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[Type] = Main.npcFrameCount[NPCID.PrimeSaw];
            NPCID.Sets.NPCBestiaryDrawModifiers bestiaryData = new NPCID.Sets.NPCBestiaryDrawModifiers() { Hide = true };
            NPCID.Sets.NPCBestiaryDrawOffset.Add(Type, bestiaryData);
        }

        public override void SetDefaults()
        {
            NPC.width = 52;
            NPC.height = 52;
            NPC.damage = 56;
            NPC.defense = 38;
            NPC.lifeMax = 9000;
            NPC.knockBackResist = 0f;
            NPC.noGravity = true;
            NPC.noTileCollide = true;
            NPC.HitSound = SoundID.NPCHit4;
            NPC.DeathSound = SoundID.NPCDeath14;
            NPC.aiStyle = -1;
            AnimationType = NPCID.PrimeSaw;
            NPC.DR_NERD(0.2f);
        }

        public override void AI()
        {
            bool bossRush = BossRushEvent.BossRushActive;
            bool masterMode = Main.masterMode || bossRush;
            bool death = CalamityWorld.death || bossRush;

            // Get a target
            if (NPC.target < 0 || NPC.target == Main.maxPlayers || Main.player[NPC.target].dead || !Main.player[NPC.target].active)
                NPC.TargetClosest();

            // Despawn safety
            if (Vector2.Distance(Main.player[NPC.target].Center, NPC.Center) > 3200f)
                NPC.TargetClosest();

            NPC.spriteDirection = -(int)NPC.ai[0];

            // Despawn if head is gone
            if (!IsPrime2041Head(Main.npc[(int)NPC.ai[1]]))
            {
                NPC.ai[2] += 10f;
                if (NPC.ai[2] > 50f || Main.netMode != NetmodeID.Server)
                {
                    NPC.life = -1;
                    NPC.HitEffect(new NPC.HitInfo());
                    NPC.active = false;
                }
            }

            Prime2041.primeSaw = NPC.whoAmI;

            // Check if arms are alive
            bool cannonAlive = false;
            bool laserAlive = false;
            bool viceAlive = false;
            if (Prime2041.primeCannon != -1)
            {
                if (Main.npc[Prime2041.primeCannon].active)
                    cannonAlive = true;
            }
            if (Prime2041.primeLaser != -1)
            {
                if (Main.npc[Prime2041.primeLaser].active)
                    laserAlive = true;
            }
            if (Prime2041.primeVice != -1)
            {
                if (Main.npc[Prime2041.primeVice].active)
                    viceAlive = true;
            }

            // Min saw damage
            int reducedSetDamage = (int)Math.Round(NPC.defDamage * 0.5);

            // Avoid cheap hits
            NPC.damage = reducedSetDamage;

            if (NPC.ai[2] == 99f)
            {
                float acceleration = (bossRush ? 0.6f : death ? (masterMode ? 0.375f : 0.3f) : (masterMode ? 0.3125f : 0.25f));
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
                if (masterMode)
                    acceleration *= accelerationMult;

                float topVelocity = acceleration * 100f;
                float deceleration = masterMode ? 0.6f : 0.8f;

                if (NPC.position.Y > Main.npc[(int)NPC.ai[1]].position.Y + 20f)
                {
                    if (NPC.velocity.Y > 0f)
                        NPC.velocity.Y *= deceleration;

                    NPC.velocity.Y -= acceleration;

                    if (NPC.velocity.Y > topVelocity)
                        NPC.velocity.Y = topVelocity;
                }
                else if (NPC.position.Y < Main.npc[(int)NPC.ai[1]].position.Y - 20f)
                {
                    if (NPC.velocity.Y < 0f)
                        NPC.velocity.Y *= deceleration;

                    NPC.velocity.Y += acceleration;

                    if (NPC.velocity.Y < -topVelocity)
                        NPC.velocity.Y = -topVelocity;
                }

                if (NPC.Center.X > Main.npc[(int)NPC.ai[1]].Center.X + 20f)
                {
                    if (NPC.velocity.X > 0f)
                        NPC.velocity.X *= deceleration;

                    NPC.velocity.X -= acceleration * 2f;

                    if (NPC.velocity.X > topVelocity)
                        NPC.velocity.X = topVelocity;
                }
                if (NPC.Center.X < Main.npc[(int)NPC.ai[1]].Center.X - 20f)
                {
                    if (NPC.velocity.X < 0f)
                        NPC.velocity.X *= deceleration;

                    NPC.velocity.X += acceleration * 2f;

                    if (NPC.velocity.X < -topVelocity)
                        NPC.velocity.X = -topVelocity;
                }
            }
            else
            {
                if (NPC.ai[2] == 0f || NPC.ai[2] == 3f)
                {
                    if (Main.npc[(int)NPC.ai[1]].ai[1] == 3f && NPC.timeLeft > 10)
                        NPC.timeLeft = 10;

                    // Start charging after 3 seconds (change this as each arm dies)
                    NPC.ai[3] += 1f;
                    if (!cannonAlive)
                        NPC.ai[3] += 1f;
                    if (!laserAlive)
                        NPC.ai[3] += 1f;
                    if (!viceAlive)
                        NPC.ai[3] += 1f;

                    if (NPC.ai[3] >= (masterMode ? 90f : 180f))
                    {
                        NPC.ai[2] += 1f;
                        NPC.ai[3] = 0f;
                        NPC.TargetClosest();
                        NPC.netUpdate = true;
                    }

                    float acceleration = (bossRush ? 0.6f : death ? (masterMode ? 0.375f : 0.3f) : (masterMode ? 0.3125f : 0.25f));
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
                    if (masterMode)
                        acceleration *= accelerationMult;

                    float topVelocity = acceleration * 100f;
                    float deceleration = masterMode ? 0.6f : 0.8f;

                    if (NPC.position.Y > Main.npc[(int)NPC.ai[1]].position.Y + 310f)
                    {
                        if (NPC.velocity.Y > 0f)
                            NPC.velocity.Y *= deceleration;

                        NPC.velocity.Y -= acceleration;

                        if (NPC.velocity.Y > topVelocity)
                            NPC.velocity.Y = topVelocity;
                    }
                    else if (NPC.position.Y < Main.npc[(int)NPC.ai[1]].position.Y + 270f)
                    {
                        if (NPC.velocity.Y < 0f)
                            NPC.velocity.Y *= deceleration;

                        NPC.velocity.Y += acceleration;

                        if (NPC.velocity.Y < -topVelocity)
                            NPC.velocity.Y = -topVelocity;
                    }

                    if (NPC.Center.X > Main.npc[(int)NPC.ai[1]].Center.X - 100f)
                    {
                        if (NPC.velocity.X > 0f)
                            NPC.velocity.X *= deceleration;

                        NPC.velocity.X -= acceleration * 1.5f;

                        if (NPC.velocity.X > topVelocity)
                            NPC.velocity.X = topVelocity;
                    }
                    if (NPC.Center.X < Main.npc[(int)NPC.ai[1]].Center.X - 150f)
                    {
                        if (NPC.velocity.X < 0f)
                            NPC.velocity.X *= deceleration;

                        NPC.velocity.X += acceleration * 1.5f;

                        if (NPC.velocity.X < -topVelocity)
                            NPC.velocity.X = -topVelocity;
                    }

                    Vector2 sawArmReelbackCurrentPos = NPC.Center;
                    float sawArmReelbackXDest = Main.npc[(int)NPC.ai[1]].Center.X - 200f * NPC.ai[0] - sawArmReelbackCurrentPos.X;
                    float sawArmReelbackYDest = Main.npc[(int)NPC.ai[1]].position.Y + 230f - sawArmReelbackCurrentPos.Y;
                    NPC.rotation = (float)Math.Atan2(sawArmReelbackYDest, sawArmReelbackXDest) + MathHelper.PiOver2;
                    return;
                }

                if (NPC.ai[2] == 1f)
                {
                    Vector2 sawArmChargePos = NPC.Center;
                    float sawArmChargeTargetX = Main.npc[(int)NPC.ai[1]].Center.X - 200f * NPC.ai[0] - sawArmChargePos.X;
                    float sawArmChargeTargetY = Main.npc[(int)NPC.ai[1]].position.Y + 230f - sawArmChargePos.Y;
                    NPC.rotation = (float)Math.Atan2(sawArmChargeTargetY, sawArmChargeTargetX) + MathHelper.PiOver2;

                    float deceleration = masterMode ? 0.875f : 0.9f;
                    NPC.velocity.X *= deceleration;
                    NPC.velocity.Y -= 0.5f;
                    if (NPC.velocity.Y < -12f)
                        NPC.velocity.Y = -12f;

                    if (NPC.position.Y < Main.npc[(int)NPC.ai[1]].position.Y - 200f)
                    {
                        NPC.damage = NPC.defDamage;

                        float chargeVelocity = bossRush ? 27.5f : 22f;
                        if (!cannonAlive)
                            chargeVelocity += 1.5f;
                        if (!laserAlive)
                            chargeVelocity += 1.5f;
                        if (!viceAlive)
                            chargeVelocity += 1.5f;

                        NPC.ai[2] = 2f;
                        NPC.TargetClosest();
                        sawArmChargePos = NPC.Center;
                        sawArmChargeTargetX = Main.player[NPC.target].Center.X - sawArmChargePos.X;
                        sawArmChargeTargetY = Main.player[NPC.target].Center.Y - sawArmChargePos.Y;
                        float sawArmChargeTargetDist = (float)Math.Sqrt(sawArmChargeTargetX * sawArmChargeTargetX + sawArmChargeTargetY * sawArmChargeTargetY);
                        sawArmChargeTargetDist = chargeVelocity / sawArmChargeTargetDist;
                        NPC.velocity.X = sawArmChargeTargetX * sawArmChargeTargetDist;
                        NPC.velocity.Y = sawArmChargeTargetY * sawArmChargeTargetDist;
                        NPC.netUpdate = true;
                    }
                }
                else if (NPC.ai[2] == 2f)
                {
                    NPC.damage = NPC.defDamage;

                    if (NPC.position.Y > Main.player[NPC.target].position.Y || NPC.velocity.Y < 0f)
                        NPC.ai[2] = 3f;
                }
                else
                {
                    if (NPC.ai[2] == 4f)
                    {
                        NPC.damage = NPC.defDamage;

                        float chargeVelocity = bossRush ? 13.5f : 11f;
                        if (!cannonAlive)
                            chargeVelocity += 1.5f;
                        if (!laserAlive)
                            chargeVelocity += 1.5f;
                        if (!viceAlive)
                            chargeVelocity += 1.5f;
                        if (masterMode)
                            chargeVelocity *= 1.25f;

                        Vector2 sawArmOtherChargePos = NPC.Center;
                        float sawArmOtherChargeTargetX = Main.player[NPC.target].Center.X - sawArmOtherChargePos.X;
                        float sawArmOtherChargeTargetY = Main.player[NPC.target].Center.Y - sawArmOtherChargePos.Y;
                        float sawArmOtherChargeTargetDist = (float)Math.Sqrt(sawArmOtherChargeTargetX * sawArmOtherChargeTargetX + sawArmOtherChargeTargetY * sawArmOtherChargeTargetY);
                        sawArmOtherChargeTargetDist = chargeVelocity / sawArmOtherChargeTargetDist;
                        sawArmOtherChargeTargetX *= sawArmOtherChargeTargetDist;
                        sawArmOtherChargeTargetY *= sawArmOtherChargeTargetDist;

                        float acceleration = bossRush ? 0.3f : death ? 0.1f : 0.08f;
                        if (masterMode)
                            acceleration *= 1.25f;

                        float deceleration = masterMode ? 0.6f : 0.8f;

                        if (NPC.velocity.X > sawArmOtherChargeTargetX)
                        {
                            if (NPC.velocity.X > 0f)
                                NPC.velocity.X *= deceleration;

                            NPC.velocity.X -= acceleration;
                        }
                        if (NPC.velocity.X < sawArmOtherChargeTargetX)
                        {
                            if (NPC.velocity.X < 0f)
                                NPC.velocity.X *= deceleration;

                            NPC.velocity.X += acceleration;
                        }
                        if (NPC.velocity.Y > sawArmOtherChargeTargetY)
                        {
                            if (NPC.velocity.Y > 0f)
                                NPC.velocity.Y *= deceleration;

                            NPC.velocity.Y -= acceleration;
                        }
                        if (NPC.velocity.Y < sawArmOtherChargeTargetY)
                        {
                            if (NPC.velocity.Y < 0f)
                                NPC.velocity.Y *= deceleration;

                            NPC.velocity.Y += acceleration;
                        }

                        NPC.ai[3] += 1f;
                        if (NPC.justHit)
                            NPC.ai[3] += 2f;

                        if (NPC.ai[3] >= 600f)
                        {
                            NPC.ai[2] = 0f;
                            NPC.ai[3] = 0f;
                            NPC.TargetClosest();
                            NPC.netUpdate = true;
                        }

                        sawArmOtherChargePos = NPC.Center;
                        sawArmOtherChargeTargetX = Main.npc[(int)NPC.ai[1]].Center.X - 200f * NPC.ai[0] - sawArmOtherChargePos.X;
                        sawArmOtherChargeTargetY = Main.npc[(int)NPC.ai[1]].position.Y + 230f - sawArmOtherChargePos.Y;
                        NPC.rotation = (float)Math.Atan2(sawArmOtherChargeTargetY, sawArmOtherChargeTargetX) + MathHelper.PiOver2;
                        return;
                    }

                    if (NPC.ai[2] == 5f && ((NPC.velocity.X > 0f && NPC.Center.X > Main.player[NPC.target].Center.X) || (NPC.velocity.X < 0f && NPC.Center.X < Main.player[NPC.target].Center.X)))
                        NPC.ai[2] = 0f;
                }
            }
        }
    }

    public class Prime2041Laser : Prime2041Arm
    {
        public override string Texture => "Terraria/Images/NPC_131";

        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[Type] = Main.npcFrameCount[NPCID.PrimeLaser];
            NPCID.Sets.NPCBestiaryDrawModifiers bestiaryData = new NPCID.Sets.NPCBestiaryDrawModifiers() { Hide = true };
            NPCID.Sets.NPCBestiaryDrawOffset.Add(Type, bestiaryData);
        }

        public override void SetDefaults()
        {
            NPC.width = 52;
            NPC.height = 52;
            NPC.damage = 29;
            NPC.defense = 20;
            NPC.lifeMax = 6000;
            NPC.knockBackResist = 0f;
            NPC.noGravity = true;
            NPC.noTileCollide = true;
            NPC.HitSound = SoundID.NPCHit4;
            NPC.DeathSound = SoundID.NPCDeath14;
            NPC.aiStyle = -1;
            AnimationType = NPCID.PrimeLaser;
            NPC.DR_NERD(0.2f);
        }

        public override void AI()
        {
            bool bossRush = BossRushEvent.BossRushActive;
            bool masterMode = Main.masterMode || bossRush;
            bool death = CalamityWorld.death || bossRush;

            // Get a target
            if (NPC.target < 0 || NPC.target == Main.maxPlayers || Main.player[NPC.target].dead || !Main.player[NPC.target].active)
                NPC.TargetClosest();

            // Despawn safety
            if (Vector2.Distance(Main.player[NPC.target].Center, NPC.Center) > 3200f)
                NPC.TargetClosest();

            NPC.spriteDirection = -(int)NPC.ai[0];

            // Despawn if head is gone
            if (!IsPrime2041Head(Main.npc[(int)NPC.ai[1]]))
            {
                NPC.ai[2] += 10f;
                if (NPC.ai[2] > 50f || Main.netMode != NetmodeID.Server)
                {
                    NPC.life = -1;
                    NPC.HitEffect(new NPC.HitInfo());
                    NPC.active = false;
                }
            }

            Prime2041.primeLaser = NPC.whoAmI;

            // Check if arms are alive
            bool cannonAlive = false;
            bool viceAlive = false;
            bool sawAlive = false;
            if (Prime2041.primeCannon != -1)
            {
                if (Main.npc[Prime2041.primeCannon].active)
                    cannonAlive = true;
            }
            if (Prime2041.primeVice != -1)
            {
                if (Main.npc[Prime2041.primeVice].active)
                    viceAlive = true;
            }
            if (Prime2041.primeSaw != -1)
            {
                if (Main.npc[Prime2041.primeSaw].active)
                    sawAlive = true;
            }

            // Inflict 0 damage for 3 seconds after spawning
            float timeToNotAttack = 180f;
            bool dontAttack = NPC.Calamity().newAI[2] < timeToNotAttack;
            if (dontAttack)
            {
                NPC.Calamity().newAI[2] += 1f;
                if (NPC.Calamity().newAI[2] >= timeToNotAttack)
                    NPC.SyncExtraAI();
            }

            NPC.damage = 0;

            bool normalLaserRotation = NPC.localAI[1] % 2f == 0f;

            // Movement
            float acceleration = (bossRush ? 0.6f : death ? (masterMode ? 0.375f : 0.3f) : (masterMode ? 0.3125f : 0.25f));
            float accelerationMult = 1f;
            if (!cannonAlive)
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
            float deceleration = masterMode ? 0.6f : 0.8f;

            if (NPC.position.Y > Main.npc[(int)NPC.ai[1]].position.Y - 80f)
            {
                if (NPC.velocity.Y > 0f)
                    NPC.velocity.Y *= deceleration;

                NPC.velocity.Y -= acceleration;

                if (NPC.velocity.Y > topVelocity)
                    NPC.velocity.Y = topVelocity;
            }
            else if (NPC.position.Y < Main.npc[(int)NPC.ai[1]].position.Y - 120f)
            {
                if (NPC.velocity.Y < 0f)
                    NPC.velocity.Y *= deceleration;

                NPC.velocity.Y += acceleration;

                if (NPC.velocity.Y < -topVelocity)
                    NPC.velocity.Y = -topVelocity;
            }

            if (NPC.Center.X > Main.npc[(int)NPC.ai[1]].Center.X - 160f * NPC.ai[0])
            {
                if (NPC.velocity.X > 0f)
                    NPC.velocity.X *= deceleration;

                NPC.velocity.X -= acceleration;

                if (NPC.velocity.X > topVelocity)
                    NPC.velocity.X = topVelocity;
            }
            if (NPC.Center.X < Main.npc[(int)NPC.ai[1]].Center.X - 200f * NPC.ai[0])
            {
                if (NPC.velocity.X < 0f)
                    NPC.velocity.X *= deceleration;

                NPC.velocity.X += acceleration;

                if (NPC.velocity.X < -topVelocity)
                    NPC.velocity.X = -topVelocity;
            }

            // Phase 1
            if (NPC.ai[2] == 0f)
            {
                if (Main.npc[(int)NPC.ai[1]].ai[1] == 3f && NPC.timeLeft > 10)
                    NPC.timeLeft = 10;

                NPC.ai[3] += 1f;
                if (!cannonAlive)
                    NPC.ai[3] += 1f;
                if (!viceAlive)
                    NPC.ai[3] += 1f;
                if (!sawAlive)
                    NPC.ai[3] += 1f;

                if (NPC.ai[3] >= (masterMode ? 200f : 800f))
                {
                    NPC.localAI[0] = 0f;
                    NPC.ai[2] = 1f;
                    NPC.ai[3] = 0f;
                    NPC.TargetClosest();
                    NPC.netUpdate = true;
                }

                Vector2 laserArmPosition = NPC.Center;
                float laserArmTargetX = Main.player[NPC.target].Center.X - laserArmPosition.X;
                float laserArmTargetY = Main.player[NPC.target].Center.Y - laserArmPosition.Y;
                float laserArmTargetDist = (float)Math.Sqrt(laserArmTargetX * laserArmTargetX + laserArmTargetY * laserArmTargetY);
                NPC.rotation = (float)Math.Atan2(laserArmTargetY, laserArmTargetX) - MathHelper.PiOver2;

                if (Main.netMode != NetmodeID.MultiplayerClient && !dontAttack)
                {
                    NPC.localAI[0] += 1f;
                    if (!cannonAlive)
                        NPC.localAI[0] += 1f;
                    if (!viceAlive)
                        NPC.localAI[0] += 1f;
                    if (!sawAlive)
                        NPC.localAI[0] += 1f;

                    if (NPC.localAI[0] >= 48f)
                    {
                        NPC.localAI[0] = 0f;
                        NPC.TargetClosest();
                        float laserSpeed = bossRush ? 5f : 4f;
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

                        laserArmTargetDist = laserSpeed / laserArmTargetDist;
                        laserArmTargetX *= laserArmTargetDist;
                        laserArmTargetY *= laserArmTargetDist;
                        Vector2 laserVelocity = new Vector2(laserArmTargetX, laserArmTargetY);
                        Projectile.NewProjectile(NPC.GetSource_FromAI(), laserArmPosition + laserVelocity.SafeNormalize(Vector2.UnitY) * 100f, laserVelocity, type, damage, 0f, Main.myPlayer, 1f, 0f);
                    }
                }
            }
            // Other phase
            else if (NPC.ai[2] == 1f)
            {
                NPC.ai[3] += 1f;

                float timeLimit = 135f;
                float timeMult = 1.882075f;
                if (!cannonAlive)
                    timeLimit *= timeMult;
                if (!viceAlive)
                    timeLimit *= timeMult;
                if (!sawAlive)
                    timeLimit *= timeMult;

                if (NPC.ai[3] >= timeLimit)
                {
                    NPC.localAI[0] = 0f;
                    NPC.ai[2] = 0f;
                    NPC.ai[3] = 0f;
                    NPC.TargetClosest();
                    NPC.netUpdate = true;
                }

                Vector2 laserRingArmPosition = NPC.Center;
                float laserRingTargetX = Main.player[NPC.target].Center.X - laserRingArmPosition.X;
                float laserRingTargetY = Main.player[NPC.target].Center.Y - laserRingArmPosition.Y;
                NPC.rotation = (float)Math.Atan2(laserRingTargetY, laserRingTargetX) - MathHelper.PiOver2;

                if (Main.netMode != NetmodeID.MultiplayerClient && !dontAttack)
                {
                    NPC.localAI[0] += 1f;
                    if (!cannonAlive)
                        NPC.localAI[0] += 0.5f;
                    if (!viceAlive)
                        NPC.localAI[0] += 0.5f;
                    if (!sawAlive)
                        NPC.localAI[0] += 0.5f;

                    if (NPC.localAI[0] >= 120f)
                    {
                        NPC.localAI[0] = 0f;
                        NPC.TargetClosest();
                        int totalProjectiles = bossRush ? 32 : (masterMode ? 24 : 16);
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
                        float laserVelocityX = (float)(velocity * Math.Sin(angleA) / Math.Sin(angleB));
                        Vector2 spinningPoint = normalLaserRotation ? new Vector2(0f, -velocity) : new Vector2(-laserVelocityX, -velocity);
                        for (int k = 0; k < totalProjectiles; k++)
                        {
                            Vector2 laserFireDirection = spinningPoint.RotatedBy(radians * k);
                            int proj = Projectile.NewProjectile(NPC.GetSource_FromAI(), NPC.Center + laserFireDirection.SafeNormalize(Vector2.UnitY) * 100f, laserFireDirection, type, damage, 0f, Main.myPlayer, 1f, 0f);
                            Main.projectile[proj].timeLeft = 900;
                        }
                        NPC.localAI[1] += 1f;
                    }
                }
            }
        }
    }

    public class Prime2041Vice : Prime2041Arm
    {
        public override string Texture => "Terraria/Images/NPC_130";

        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[Type] = Main.npcFrameCount[NPCID.PrimeVice];
            NPCID.Sets.NPCBestiaryDrawModifiers bestiaryData = new NPCID.Sets.NPCBestiaryDrawModifiers() { Hide = true };
            NPCID.Sets.NPCBestiaryDrawOffset.Add(Type, bestiaryData);
        }

        public override void SetDefaults()
        {
            NPC.width = 52;
            NPC.height = 52;
            NPC.damage = 52;
            NPC.defense = 34;
            NPC.lifeMax = 9000;
            NPC.knockBackResist = 0f;
            NPC.noGravity = true;
            NPC.noTileCollide = true;
            NPC.HitSound = SoundID.NPCHit4;
            NPC.DeathSound = SoundID.NPCDeath14;
            NPC.aiStyle = -1;
            AnimationType = NPCID.PrimeVice;
            NPC.DR_NERD(0.2f);
        }

        public override void AI()
        {
            bool bossRush = BossRushEvent.BossRushActive;
            bool masterMode = Main.masterMode || bossRush;
            bool death = CalamityWorld.death || bossRush;

            // Get a target
            if (NPC.target < 0 || NPC.target == Main.maxPlayers || Main.player[NPC.target].dead || !Main.player[NPC.target].active)
                NPC.TargetClosest();

            // Despawn safety
            if (Vector2.Distance(Main.player[NPC.target].Center, NPC.Center) > 3200f)
                NPC.TargetClosest();

            NPC.spriteDirection = -(int)NPC.ai[0];

            Vector2 viceArmPosition = NPC.Center;
            float viceArmIdleXPos = Main.npc[(int)NPC.ai[1]].Center.X - 200f * NPC.ai[0] - viceArmPosition.X;
            float viceArmIdleYPos = Main.npc[(int)NPC.ai[1]].position.Y + 230f - viceArmPosition.Y;
            float viceArmIdleDistance = (float)Math.Sqrt(viceArmIdleXPos * viceArmIdleXPos + viceArmIdleYPos * viceArmIdleYPos);

            if (NPC.ai[2] != 99f)
            {
                if (viceArmIdleDistance > 800f)
                    NPC.ai[2] = 99f;
            }
            else if (viceArmIdleDistance < 400f)
                NPC.ai[2] = 0f;

            // Despawn if head is gone
            if (!IsPrime2041Head(Main.npc[(int)NPC.ai[1]]))
            {
                NPC.ai[2] += 10f;
                if (NPC.ai[2] > 50f || Main.netMode != NetmodeID.Server)
                {
                    NPC.life = -1;
                    NPC.HitEffect(new NPC.HitInfo());
                    NPC.active = false;
                }
            }

            Prime2041.primeVice = NPC.whoAmI;

            // Check if arms are alive
            bool cannonAlive = false;
            bool laserAlive = false;
            bool sawAlive = false;
            if (Prime2041.primeCannon != -1)
            {
                if (Main.npc[Prime2041.primeCannon].active)
                    cannonAlive = true;
            }
            if (Prime2041.primeLaser != -1)
            {
                if (Main.npc[Prime2041.primeLaser].active)
                    laserAlive = true;
            }
            if (Prime2041.primeSaw != -1)
            {
                if (Main.npc[Prime2041.primeSaw].active)
                    sawAlive = true;
            }

            NPC.damage = 0;

            // Return to the head
            if (NPC.ai[2] == 99f)
            {
                float acceleration = (bossRush ? 0.6f : death ? (masterMode ? 0.375f : 0.3f) : (masterMode ? 0.3125f : 0.25f));
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
                if (!sawAlive)
                    acceleration += 0.025f;
                if (masterMode)
                    acceleration *= accelerationMult;

                float topVelocity = acceleration * 100f;
                float deceleration = masterMode ? 0.6f : 0.8f;

                if (NPC.position.Y > Main.npc[(int)NPC.ai[1]].position.Y + 20f)
                {
                    if (NPC.velocity.Y > 0f)
                        NPC.velocity.Y *= deceleration;

                    NPC.velocity.Y -= acceleration;

                    if (NPC.velocity.Y > topVelocity)
                        NPC.velocity.Y = topVelocity;
                }
                else if (NPC.position.Y < Main.npc[(int)NPC.ai[1]].position.Y - 20f)
                {
                    if (NPC.velocity.Y < 0f)
                        NPC.velocity.Y *= deceleration;

                    NPC.velocity.Y += acceleration;

                    if (NPC.velocity.Y < -topVelocity)
                        NPC.velocity.Y = -topVelocity;
                }

                if (NPC.Center.X > Main.npc[(int)NPC.ai[1]].Center.X + 20f)
                {
                    if (NPC.velocity.X > 0f)
                        NPC.velocity.X *= deceleration;

                    NPC.velocity.X -= acceleration * 2f;

                    if (NPC.velocity.X > topVelocity)
                        NPC.velocity.X = topVelocity;
                }
                if (NPC.Center.X < Main.npc[(int)NPC.ai[1]].Center.X - 20f)
                {
                    if (NPC.velocity.X < 0f)
                        NPC.velocity.X *= deceleration;

                    NPC.velocity.X += acceleration * 2f;

                    if (NPC.velocity.X < -topVelocity)
                        NPC.velocity.X = -topVelocity;
                }
            }
            // Other phases
            else
            {
                if (NPC.ai[2] == 0f || NPC.ai[2] == 3f)
                {
                    if (Main.npc[(int)NPC.ai[1]].ai[1] == 3f && NPC.timeLeft > 10)
                        NPC.timeLeft = 10;

                    NPC.ai[3] += 1f;
                    if (!cannonAlive)
                        NPC.ai[3] += 1f;
                    if (!laserAlive)
                        NPC.ai[3] += 1f;
                    if (!sawAlive)
                        NPC.ai[3] += 1f;

                    if (NPC.ai[3] >= (masterMode ? 150f : 600f))
                    {
                        NPC.ai[2] += 1f;
                        NPC.ai[3] = 0f;
                        NPC.TargetClosest();
                        NPC.netUpdate = true;
                    }

                    float acceleration = (bossRush ? 0.6f : death ? (masterMode ? 0.375f : 0.3f) : (masterMode ? 0.3125f : 0.25f));
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
                    if (!sawAlive)
                        acceleration += 0.025f;
                    if (masterMode)
                        acceleration *= accelerationMult;

                    float topVelocity = acceleration * 100f;
                    float deceleration = masterMode ? 0.6f : 0.8f;

                    if (NPC.position.Y > Main.npc[(int)NPC.ai[1]].position.Y + 290f)
                    {
                        if (NPC.velocity.Y > 0f)
                            NPC.velocity.Y *= deceleration;

                        NPC.velocity.Y -= acceleration;

                        if (NPC.velocity.Y > topVelocity)
                            NPC.velocity.Y = topVelocity;
                    }
                    else if (NPC.position.Y < Main.npc[(int)NPC.ai[1]].position.Y + 240f)
                    {
                        if (NPC.velocity.Y < 0f)
                            NPC.velocity.Y *= deceleration;

                        NPC.velocity.Y += acceleration;

                        if (NPC.velocity.Y < -topVelocity)
                            NPC.velocity.Y = -topVelocity;
                    }

                    if (NPC.Center.X > Main.npc[(int)NPC.ai[1]].Center.X + 150f)
                    {
                        if (NPC.velocity.X > 0f)
                            NPC.velocity.X *= deceleration;

                        NPC.velocity.X -= acceleration;

                        if (NPC.velocity.X > topVelocity)
                            NPC.velocity.X = topVelocity;
                    }
                    if (NPC.Center.X < Main.npc[(int)NPC.ai[1]].Center.X + 100f)
                    {
                        if (NPC.velocity.X < 0f)
                            NPC.velocity.X *= deceleration;

                        NPC.velocity.X += acceleration;

                        if (NPC.velocity.X < -topVelocity)
                            NPC.velocity.X = -topVelocity;
                    }

                    Vector2 viceArmReelbackCurrentPos = NPC.Center;
                    float viceArmReelbackXDest = Main.npc[(int)NPC.ai[1]].Center.X - 200f * NPC.ai[0] - viceArmReelbackCurrentPos.X;
                    float viceArmReelbackYDest = Main.npc[(int)NPC.ai[1]].position.Y + 230f - viceArmReelbackCurrentPos.Y;
                    NPC.rotation = (float)Math.Atan2(viceArmReelbackYDest, viceArmReelbackXDest) + MathHelper.PiOver2;
                    return;
                }

                if (NPC.ai[2] == 1f)
                {
                    float deceleration = masterMode ? 0.75f : 0.8f;
                    if (NPC.velocity.Y > 0f)
                        NPC.velocity.Y *= deceleration;

                    Vector2 viceArmChargePosition = NPC.Center;
                    float viceArmChargeTargetX = Main.npc[(int)NPC.ai[1]].Center.X - 280f * NPC.ai[0] - viceArmChargePosition.X;
                    float viceArmChargeTargetY = Main.npc[(int)NPC.ai[1]].position.Y + 230f - viceArmChargePosition.Y;
                    NPC.rotation = (float)Math.Atan2(viceArmChargeTargetY, viceArmChargeTargetX) + MathHelper.PiOver2;

                    NPC.velocity.X = (NPC.velocity.X * 5f + Main.npc[(int)NPC.ai[1]].velocity.X) / 6f;
                    NPC.velocity.X += 0.5f;

                    NPC.velocity.Y -= 0.5f;
                    if (NPC.velocity.Y < -12f)
                        NPC.velocity.Y = -12f;

                    if (NPC.position.Y < Main.npc[(int)NPC.ai[1]].position.Y - 280f)
                    {
                        NPC.damage = NPC.defDamage;

                        float chargeVelocity = bossRush ? 20f : 16f;
                        if (!cannonAlive)
                            chargeVelocity += 1.5f;
                        if (!laserAlive)
                            chargeVelocity += 1.5f;
                        if (!sawAlive)
                            chargeVelocity += 1.5f;

                        NPC.ai[2] = 2f;
                        NPC.TargetClosest();
                        viceArmChargePosition = NPC.Center;
                        viceArmChargeTargetX = Main.player[NPC.target].Center.X - viceArmChargePosition.X;
                        viceArmChargeTargetY = Main.player[NPC.target].Center.Y - viceArmChargePosition.Y;
                        float viceArmChargeTargetDist = (float)Math.Sqrt(viceArmChargeTargetX * viceArmChargeTargetX + viceArmChargeTargetY * viceArmChargeTargetY);
                        viceArmChargeTargetDist = chargeVelocity / viceArmChargeTargetDist;
                        NPC.velocity.X = viceArmChargeTargetX * viceArmChargeTargetDist;
                        NPC.velocity.Y = viceArmChargeTargetY * viceArmChargeTargetDist;
                        NPC.netUpdate = true;
                    }
                }
                else if (NPC.ai[2] == 2f)
                {
                    NPC.damage = NPC.defDamage;

                    if (NPC.position.Y > Main.player[NPC.target].position.Y || NPC.velocity.Y < 0f)
                    {
                        float chargeAmt = 4f;
                        if (!cannonAlive)
                            chargeAmt += 1f;
                        if (!laserAlive)
                            chargeAmt += 1f;
                        if (!sawAlive)
                            chargeAmt += 1f;

                        if (NPC.ai[3] >= chargeAmt)
                        {
                            NPC.ai[2] = 3f;
                            NPC.ai[3] = 0f;
                            NPC.TargetClosest();
                            return;
                        }

                        NPC.ai[2] = 1f;
                        NPC.ai[3] += 1f;
                    }
                }
                else if (NPC.ai[2] == 4f)
                {
                    Vector2 viceArmOtherChargePosition = NPC.Center;
                    float viceArmOtherChargeTargetX = Main.npc[(int)NPC.ai[1]].Center.X - 200f * NPC.ai[0] - viceArmOtherChargePosition.X;
                    float viceArmOtherChargeTargetY = Main.npc[(int)NPC.ai[1]].position.Y + 230f - viceArmOtherChargePosition.Y;
                    NPC.rotation = (float)Math.Atan2(viceArmOtherChargeTargetY, viceArmOtherChargeTargetX) + MathHelper.PiOver2;

                    NPC.velocity.Y = (NPC.velocity.Y * 5f + Main.npc[(int)NPC.ai[1]].velocity.Y) / 6f;

                    NPC.velocity.X += 0.5f;
                    if (NPC.velocity.X > 12f)
                        NPC.velocity.X = 12f;

                    if (NPC.Center.X < Main.npc[(int)NPC.ai[1]].Center.X - 500f || NPC.Center.X > Main.npc[(int)NPC.ai[1]].Center.X + 500f)
                    {
                        NPC.damage = NPC.defDamage;

                        float chargeVelocity = bossRush ? 17.5f : 14f;
                        if (!cannonAlive)
                            chargeVelocity += 1.15f;
                        if (!laserAlive)
                            chargeVelocity += 1.15f;
                        if (!sawAlive)
                            chargeVelocity += 1.15f;

                        NPC.ai[2] = 5f;
                        NPC.TargetClosest();
                        viceArmOtherChargePosition = NPC.Center;
                        viceArmOtherChargeTargetX = Main.player[NPC.target].Center.X - viceArmOtherChargePosition.X;
                        viceArmOtherChargeTargetY = Main.player[NPC.target].Center.Y - viceArmOtherChargePosition.Y;
                        float viceArmOtherChargeTargetDist = (float)Math.Sqrt(viceArmOtherChargeTargetX * viceArmOtherChargeTargetX + viceArmOtherChargeTargetY * viceArmOtherChargeTargetY);
                        viceArmOtherChargeTargetDist = chargeVelocity / viceArmOtherChargeTargetDist;
                        NPC.velocity.X = viceArmOtherChargeTargetX * viceArmOtherChargeTargetDist;
                        NPC.velocity.Y = viceArmOtherChargeTargetY * viceArmOtherChargeTargetDist;
                        NPC.netUpdate = true;
                    }
                }
                else if (NPC.ai[2] == 5f && NPC.Center.X < Main.player[NPC.target].Center.X - 100f)
                {
                    NPC.damage = NPC.defDamage;

                    float chargeAmt = 4f;
                    if (!cannonAlive)
                        chargeAmt += 1f;
                    if (!laserAlive)
                        chargeAmt += 1f;
                    if (!sawAlive)
                        chargeAmt += 1f;

                    if (NPC.ai[3] >= chargeAmt)
                    {
                        NPC.ai[2] = 0f;
                        NPC.ai[3] = 0f;
                        NPC.TargetClosest();
                        return;
                    }

                    NPC.ai[2] = 4f;
                    NPC.ai[3] += 1f;
                }
            }
        }
    }

    public class Prime2041Cannon : Prime2041Arm
    {
        public override string Texture => "Terraria/Images/NPC_128";

        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[Type] = Main.npcFrameCount[NPCID.PrimeCannon];
            NPCID.Sets.NPCBestiaryDrawModifiers bestiaryData = new NPCID.Sets.NPCBestiaryDrawModifiers() { Hide = true };
            NPCID.Sets.NPCBestiaryDrawOffset.Add(Type, bestiaryData);
        }

        public override void SetDefaults()
        {
            NPC.width = 52;
            NPC.height = 52;
            NPC.damage = 30;
            NPC.defense = 30;
            NPC.lifeMax = 7000;
            NPC.knockBackResist = 0f;
            NPC.noGravity = true;
            NPC.noTileCollide = true;
            NPC.HitSound = SoundID.NPCHit4;
            NPC.DeathSound = SoundID.NPCDeath14;
            NPC.aiStyle = -1;
            AnimationType = NPCID.PrimeCannon;
            NPC.DR_NERD(0.2f);
        }

        public override void AI()
        {
            bool bossRush = BossRushEvent.BossRushActive;
            bool masterMode = Main.masterMode || bossRush;
            bool death = CalamityWorld.death || bossRush;

            // Get a target
            if (NPC.target < 0 || NPC.target == Main.maxPlayers || Main.player[NPC.target].dead || !Main.player[NPC.target].active)
                NPC.TargetClosest();

            // Despawn safety
            if (Vector2.Distance(Main.player[NPC.target].Center, NPC.Center) > 3200f)
                NPC.TargetClosest();

            NPC.spriteDirection = -(int)NPC.ai[0];

            // Despawn if head is gone
            if (!IsPrime2041Head(Main.npc[(int)NPC.ai[1]]))
            {
                NPC.ai[2] += 10f;
                if (NPC.ai[2] > 50f || Main.netMode != NetmodeID.Server)
                {
                    NPC.life = -1;
                    NPC.HitEffect(new NPC.HitInfo());
                    NPC.active = false;
                }
            }

            Prime2041.primeCannon = NPC.whoAmI;

            // Check if arms are alive
            bool laserAlive = false;
            bool viceAlive = false;
            bool sawAlive = false;
            if (Prime2041.primeLaser != -1)
            {
                if (Main.npc[Prime2041.primeLaser].active)
                    laserAlive = true;
            }
            if (Prime2041.primeVice != -1)
            {
                if (Main.npc[Prime2041.primeVice].active)
                    viceAlive = true;
            }
            if (Prime2041.primeSaw != -1)
            {
                if (Main.npc[Prime2041.primeSaw].active)
                    sawAlive = true;
            }

            // Inflict 0 damage for 3 seconds after spawning
            float timeToNotAttack = 180f;
            bool dontAttack = NPC.Calamity().newAI[2] < timeToNotAttack;
            if (dontAttack)
            {
                NPC.Calamity().newAI[2] += 1f;
                if (NPC.Calamity().newAI[2] >= timeToNotAttack)
                    NPC.SyncExtraAI();
            }

            NPC.damage = 0;

            bool fireSlower = false;
            if (laserAlive)
            {
                if (Main.npc[Prime2041.primeLaser].ai[2] == 1f)
                    fireSlower = true;
            }
            else
            {
                fireSlower = NPC.ai[2] == 0f;

                if (fireSlower)
                {
                    NPC.ai[3] += 1f;
                    if (!laserAlive)
                        NPC.ai[3] += 1f;
                    if (!viceAlive)
                        NPC.ai[3] += 1f;
                    if (!sawAlive)
                        NPC.ai[3] += 1f;

                    if (NPC.ai[3] >= (masterMode ? 200f : 800f))
                    {
                        NPC.localAI[0] = 0f;
                        NPC.ai[2] = 1f;
                        fireSlower = false;
                        NPC.ai[3] = 0f;
                        NPC.TargetClosest();
                        NPC.netUpdate = true;
                    }
                }
                else
                {
                    NPC.ai[3] += 1f;

                    float timeLimit = 120f;
                    float timeMult = 1.882075f;
                    if (!laserAlive)
                        timeLimit *= timeMult;
                    if (!viceAlive)
                        timeLimit *= timeMult;
                    if (!sawAlive)
                        timeLimit *= timeMult;

                    if (NPC.ai[3] >= timeLimit)
                    {
                        NPC.localAI[0] = 0f;
                        NPC.ai[2] = 0f;
                        fireSlower = true;
                        NPC.ai[3] = 0f;
                        NPC.TargetClosest();
                        NPC.netUpdate = true;
                    }
                }
            }

            // Movement
            float acceleration = (bossRush ? 0.6f : death ? (masterMode ? 0.375f : 0.3f) : (masterMode ? 0.3125f : 0.25f));
            float accelerationMult = 1f;
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
            float deceleration = masterMode ? 0.6f : 0.8f;

            if (NPC.position.Y > Main.npc[(int)NPC.ai[1]].position.Y - 130f)
            {
                if (NPC.velocity.Y > 0f)
                    NPC.velocity.Y *= deceleration;

                NPC.velocity.Y -= acceleration;

                if (NPC.velocity.Y > topVelocity)
                    NPC.velocity.Y = topVelocity;
            }
            else if (NPC.position.Y < Main.npc[(int)NPC.ai[1]].position.Y - 170f)
            {
                if (NPC.velocity.Y < 0f)
                    NPC.velocity.Y *= deceleration;

                NPC.velocity.Y += acceleration;

                if (NPC.velocity.Y < -topVelocity)
                    NPC.velocity.Y = -topVelocity;
            }

            if (NPC.Center.X > Main.npc[(int)NPC.ai[1]].Center.X + 160f)
            {
                if (NPC.velocity.X > 0f)
                    NPC.velocity.X *= deceleration;

                NPC.velocity.X -= acceleration;

                if (NPC.velocity.X > topVelocity)
                    NPC.velocity.X = topVelocity;
            }
            if (NPC.Center.X < Main.npc[(int)NPC.ai[1]].Center.X + 200f)
            {
                if (NPC.velocity.X < 0f)
                    NPC.velocity.X *= deceleration;

                NPC.velocity.X += acceleration;

                if (NPC.velocity.X < -topVelocity)
                    NPC.velocity.X = -topVelocity;
            }

            if (fireSlower)
            {
                if (Main.npc[(int)NPC.ai[1]].ai[1] == 3f && NPC.timeLeft > 10)
                    NPC.timeLeft = 10;

                Vector2 cannonArmPosition = NPC.Center;
                float cannonArmTargetX = Main.player[NPC.target].Center.X - cannonArmPosition.X;
                float cannonArmTargetY = Main.player[NPC.target].Center.Y - cannonArmPosition.Y;
                float cannonArmTargetDist = (float)Math.Sqrt(cannonArmTargetX * cannonArmTargetX + cannonArmTargetY * cannonArmTargetY);
                NPC.rotation = (float)Math.Atan2(cannonArmTargetY, cannonArmTargetX) - MathHelper.PiOver2;

                if (Main.netMode != NetmodeID.MultiplayerClient && !dontAttack)
                {
                    NPC.localAI[0] += 1f;
                    if (!laserAlive)
                        NPC.localAI[0] += 1f;
                    if (!viceAlive)
                        NPC.localAI[0] += 1f;
                    if (!sawAlive)
                        NPC.localAI[0] += 1f;

                    if (NPC.localAI[0] >= 120f)
                    {
                        SoundEngine.PlaySound(SoundID.Item62, NPC.Center);
                        NPC.localAI[0] = 0f;
                        NPC.TargetClosest();
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

                        float rocketSpeed = 10f;
                        cannonArmTargetDist = rocketSpeed / cannonArmTargetDist;
                        cannonArmTargetX *= cannonArmTargetDist;
                        cannonArmTargetY *= cannonArmTargetDist;

                        Vector2 rocketVelocity = new Vector2(cannonArmTargetX, cannonArmTargetY);
                        int proj = Projectile.NewProjectile(NPC.GetSource_FromAI(), cannonArmPosition + rocketVelocity.SafeNormalize(Vector2.UnitY) * 40f, rocketVelocity, type, damage, 0f, Main.myPlayer, NPC.target, 2f);
                        Main.projectile[proj].timeLeft = 600;
                    }
                }
            }
            else
            {
                Vector2 cannonSpreadArmPosition = NPC.Center;
                float cannonSpreadArmTargetX = Main.player[NPC.target].Center.X - cannonSpreadArmPosition.X;
                float cannonSpreadArmTargetY = Main.player[NPC.target].Center.Y - cannonSpreadArmPosition.Y;
                NPC.rotation = (float)Math.Atan2(cannonSpreadArmTargetY, cannonSpreadArmTargetX) - MathHelper.PiOver2;

                if (Main.netMode != NetmodeID.MultiplayerClient && !dontAttack)
                {
                    NPC.localAI[0] += 1f;
                    if (!laserAlive)
                        NPC.localAI[0] += 0.5f;
                    if (!viceAlive)
                        NPC.localAI[0] += 0.5f;
                    if (!sawAlive)
                        NPC.localAI[0] += 0.5f;

                    if (NPC.localAI[0] >= 180f)
                    {
                        SoundEngine.PlaySound(SoundID.Item62, NPC.Center);
                        NPC.localAI[0] = 0f;
                        NPC.TargetClosest();
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

                        float rocketSpeed = 10f; 
                        Vector2 cannonSpreadTargetDist = (Main.player[NPC.target].Center - NPC.Center).SafeNormalize(Vector2.UnitY) * rocketSpeed;
                        int numProj = bossRush ? 5 : 3;
                        float rotation = MathHelper.ToRadians(bossRush ? 15 : 9);
                        for (int i = 0; i < numProj; i++)
                        {
                            Vector2 perturbedSpeed = cannonSpreadTargetDist.RotatedBy(MathHelper.Lerp(-rotation, rotation, i / (float)(numProj - 1)));
                            int proj = Projectile.NewProjectile(NPC.GetSource_FromAI(), NPC.Center + perturbedSpeed.SafeNormalize(Vector2.UnitY) * 40f, perturbedSpeed, type, damage, 0f, Main.myPlayer, NPC.target, 2f);
                            Main.projectile[proj].timeLeft = 600;
                        }
                    }
                }
            }
        }
    }
}
