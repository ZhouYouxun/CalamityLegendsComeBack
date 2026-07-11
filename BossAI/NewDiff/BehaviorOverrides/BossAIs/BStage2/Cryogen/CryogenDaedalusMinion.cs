using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.BossAI.NewDiff.Content.BehaviorOverrides.BossAIs.BStage2.Cryogen
{
    public class CryogenDaedalusMinion : ModNPC
    {
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetStaticDefaults()
        {
            NPCID.Sets.MustAlwaysDraw[Type] = true;
            NPCID.Sets.CantTakeLunchMoney[Type] = true;
            NPCID.Sets.BossBestiaryPriority.Add(Type);
        }

        public override void SetDefaults()
        {
            NPC.width = 40;
            NPC.height = 46;
            NPC.damage = 50;
            NPC.defense = 10;
            NPC.lifeMax = 600;
            NPC.HitSound = SoundID.NPCHit5;
            NPC.DeathSound = SoundID.NPCDeath15;
            NPC.noGravity = true;
            NPC.noTileCollide = true;
            NPC.knockBackResist = 0f;
            NPC.dontCountMe = true; // Does not count towards world completion or generic boss bar
        }

        public override void AI()
        {
            // ai[0]: Parent index
            // ai[1]: Minion type (0 = Golem A / Pellets, 1 = Golem B / Lightning)
            // ai[2]: Starting angle offset
            int parentIndex = (int)NPC.ai[0];
            if (parentIndex < 0 || parentIndex >= Main.maxNPCs)
            {
                NPC.active = false;
                return;
            }

            NPC parent = Main.npc[parentIndex];
            if (!parent.active || parent.type != ModContent.NPCType<CalamityMod.NPCs.Cryogen.Cryogen>())
            {
                NPC.active = false;
                return;
            }

            // Orbit around parent
            NPC.localAI[0]++; // Timer for rotation

            // Materialize burst on the first frame — the golem condenses into existence instead of popping in
            if (NPC.localAI[0] == 1f)
            {
                SoundEngine.PlaySound(SoundID.Item28 with { Volume = 0.5f, Pitch = 0.3f }, NPC.Center);
                for (int i = 0; i < 25; i++)
                {
                    Dust d = Dust.NewDustPerfect(NPC.Center + Main.rand.NextVector2Circular(40f, 40f), DustID.AncientLight);
                    d.color = Color.Lerp(Color.White, Color.Cyan, Main.rand.NextFloat(0.15f, 0.7f));
                    d.velocity = Main.rand.NextVector2Circular(3f, 3f) - Vector2.UnitY * 1.6f;
                    d.scale = Main.rand.NextFloat(1.2f, 1.6f);
                    d.fadeIn = 1.5f;
                    d.noGravity = true;
                }

                // ai[1]==2: the "Overcharged" single golem (Daedalus Staff variant B trades two weak adds for
                // one tough one). ai values aren't set yet during SetDefaults, so the stat/size bump happens here
                // on the first live frame instead.
                if ((int)NPC.ai[1] == 2)
                {
                    NPC.lifeMax = (int)(NPC.lifeMax * 1.8f);
                    NPC.life = NPC.lifeMax;
                    NPC.damage = (int)(NPC.damage * 1.3f);
                    NPC.scale = 1.35f;
                }
            }
            float orbitRadius = (int)NPC.ai[1] == 2 ? 140f : 100f;
            float speed = 0.022f;
            float currentAngle = NPC.ai[2] + NPC.localAI[0] * speed;
            NPC.Center = parent.Center + currentAngle.ToRotationVector2() * orbitRadius;
            NPC.velocity = Vector2.Zero;

            // Face the player
            Player player = Main.player[parent.target];
            if (player.active && !player.dead)
            {
                NPC.direction = NPC.spriteDirection = Math.Sign(player.Center.X - NPC.Center.X);
            }

            // Attack Logic
            int type = (int)NPC.ai[1];
            NPC.localAI[1]++; // Attack timer

            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            if (type == 0) // Golem A: Pellet Spread
            {
                if (NPC.localAI[1] >= 60f)
                {
                    NPC.localAI[1] = 0f;
                    if (player.active && !player.dead)
                    {
                        // Fire a 3-pellet spread towards player's predicted position
                        Vector2 targetPos = player.Center + player.velocity * 12f;
                        Vector2 baseVel = (targetPos - NPC.Center).SafeNormalize(Vector2.UnitY) * 10.5f;
                        float spread = MathHelper.ToRadians(15f);

                        for (int i = -1; i <= 1; i++)
                        {
                            Vector2 vel = baseVel.RotatedBy(i * spread);
                            Projectile.NewProjectile(
                                NPC.GetSource_FromAI(),
                                NPC.Center,
                                vel,
                                ModContent.ProjectileType<CryogenDaedalusPellet>(),
                                parent.damage / 3,
                                0f,
                                Main.myPlayer
                            );
                        }
                        SoundEngine.PlaySound(SoundID.Item30 with { Volume = 0.6f, Pitch = 0.2f }, NPC.Center);
                    }
                }
            }
            else if (type == 1) // Golem B: Lightning
            {
                if (NPC.localAI[1] >= 90f)
                {
                    NPC.localAI[1] = 0f;
                    if (player.active && !player.dead)
                    {
                        // Shoot a horizontal telegraphed lightning bolt at the player's Y level
                        // Spawn the lightning projectile at the golem's position, aligned horizontally
                        Vector2 spawnPos = new Vector2(player.Center.X, player.Center.Y);
                        Projectile.NewProjectile(
                            NPC.GetSource_FromAI(),
                            spawnPos,
                            new Vector2(player.direction, 0f),
                            ModContent.ProjectileType<CryogenDaedalusLightning>(),
                            parent.damage / 2,
                            0f,
                            Main.myPlayer,
                            NPC.Center.Y // Pass the Golem's Y coordinate to anchor the lightning height
                        );
                    }
                }
            }
            else // type == 2: Overcharged single golem — alternates both attacks itself on a shared, faster clock
            {
                if (NPC.localAI[1] >= 50f)
                {
                    NPC.localAI[1] = 0f;
                    NPC.localAI[2] = (NPC.localAI[2] + 1f) % 2f;

                    if (player.active && !player.dead)
                    {
                        if (NPC.localAI[2] == 0f)
                        {
                            Vector2 targetPos = player.Center + player.velocity * 12f;
                            Vector2 baseVel = (targetPos - NPC.Center).SafeNormalize(Vector2.UnitY) * 11f;
                            float spread = MathHelper.ToRadians(15f);
                            for (int i = -1; i <= 1; i++)
                            {
                                Vector2 vel = baseVel.RotatedBy(i * spread);
                                Projectile.NewProjectile(NPC.GetSource_FromAI(), NPC.Center, vel, ModContent.ProjectileType<CryogenDaedalusPellet>(), parent.damage / 3, 0f, Main.myPlayer);
                            }
                            SoundEngine.PlaySound(SoundID.Item30 with { Volume = 0.6f, Pitch = 0.2f }, NPC.Center);
                        }
                        else
                        {
                            Vector2 spawnPos = new Vector2(player.Center.X, player.Center.Y);
                            Projectile.NewProjectile(NPC.GetSource_FromAI(), spawnPos, new Vector2(player.direction, 0f), ModContent.ProjectileType<CryogenDaedalusLightning>(), parent.damage / 2, 0f, Main.myPlayer, NPC.Center.Y);
                        }
                    }
                }
            }
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            // Dynamically load the Calamity Golem projectile texture (since it is a 18-frame sheet)
            Texture2D texture = ModContent.Request<Texture2D>("CalamityMod/Projectiles/Summon/DaedalusGolem", AssetRequestMode.ImmediateLoad).Value;
            if (texture == null)
                return true;

            // Golem texture has 18 frames. Let's calculate the frame based on localAI[0]
            int frameCount = 18;
            int currentFrame = (int)(NPC.localAI[0] / 4) % frameCount;
            int frameHeight = texture.Height / frameCount;
            Rectangle sourceRect = new Rectangle(0, currentFrame * frameHeight, texture.Width, frameHeight);
            Vector2 origin = sourceRect.Size() * 0.5f;

            SpriteEffects effects = NPC.spriteDirection == 1 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;

            // Draw a light blue glow outline for the minion
            Color glowColor = Color.DeepSkyBlue * 0.45f * NPC.Opacity;
            for (int i = 0; i < 8; i++)
            {
                Vector2 offset = (MathHelper.TwoPi * i / 8f).ToRotationVector2() * 2f;
                spriteBatch.Draw(
                    texture,
                    NPC.Center + offset - screenPos,
                    sourceRect,
                    glowColor,
                    NPC.rotation,
                    origin,
                    NPC.scale,
                    effects,
                    0f
                );
            }

            // Draw the Golem body
            spriteBatch.Draw(
                texture,
                NPC.Center - screenPos,
                sourceRect,
                NPC.GetAlpha(drawColor),
                NPC.rotation,
                origin,
                NPC.scale,
                effects,
                0f
            );

            return false;
        }

        public override void HitEffect(NPC.HitInfo hit)
        {
            if (NPC.life <= 0)
            {
                // Blooming shatter with upward drift on death
                for (int i = 0; i < 30; i++)
                {
                    Dust d = Dust.NewDustPerfect(NPC.Center + Main.rand.NextVector2Circular(20f, 20f), DustID.Ice, Main.rand.NextVector2Circular(6f, 6f) - Vector2.UnitY * 2f, 100, Color.DeepSkyBlue, Main.rand.NextFloat(1.1f, 1.6f));
                    d.fadeIn = 1.4f;
                    d.noGravity = true;
                }
            }
        }
    }
}
