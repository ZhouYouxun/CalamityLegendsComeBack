using System;
using CalamityLegendsComeBack.BossAI.NewDiff.Content.BehaviorOverrides.BossAIs.Common;
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
    // =====================================================================================================================
    // CRYO STONE BARRIER - HORIZONTAL ARENA FLOOR
    // =====================================================================================================================
    public class CryoStoneBarrier : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetDefaults()
        {
            Projectile.width = 1600;
            Projectile.height = 16;
            Projectile.hostile = false;
            Projectile.friendly = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 99999;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
        }

        public override bool? CanDamage() => false;

        public override void AI()
        {
            // Keep the barrier active only if Cryogen is active
            NPC parent = Main.npc[(int)Projectile.ai[0]];
            if (!parent.active || parent.type != ModContent.NPCType<CalamityMod.NPCs.Cryogen.Cryogen>())
            {
                Projectile.Kill();
                return;
            }

            // Snap parent phase. If phase > 3 (Phase 4+), barrier disappears
            if (parent.ai[0] > 3f)
            {
                Projectile.Kill();
                return;
            }

            // Apply solid collision logic for the local player
            Player player = Main.LocalPlayer;
            if (player.active && !player.dead)
            {
                float leftBound = Projectile.Center.X - 800f;
                float rightBound = Projectile.Center.X + 800f;
                float floorY = Projectile.Center.Y;

                // Check if player is horizontally within the barrier
                if (player.Center.X >= leftBound && player.Center.X <= rightBound)
                {
                    // Check if player is landing on the floor
                    float playerFeetY = player.position.Y + player.height;
                    float prevPlayerFeetY = player.oldPosition.Y + player.height;

                    if (playerFeetY >= floorY - 2f && prevPlayerFeetY <= floorY + 8f)
                    {
                        if (player.velocity.Y >= 0f)
                        {
                            player.position.Y = floorY - player.height;
                            player.velocity.Y = 0f;
                            player.fallStart = (int)(player.position.Y / 16f);
                            player.wingTime = player.wingTimeMax;
                        }
                    }
                }
            }

            // Periodically release subtle particles along the barrier
            if (Main.rand.NextBool(5))
            {
                float rx = Main.rand.NextFloat(-800f, 800f);
                Dust d = Dust.NewDustPerfect(
                    new Vector2(Projectile.Center.X + rx, Projectile.Center.Y + Main.rand.NextFloat(-4f, 4f)),
                    DustID.Ice,
                    new Vector2(Main.rand.NextFloat(-0.5f, 0.5f), Main.rand.NextFloat(-1f, 0f)),
                    100,
                    Color.DeepSkyBlue,
                    Main.rand.NextFloat(0.6f, 1.2f)
                );
                d.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            SpriteBatch spriteBatch = Main.spriteBatch;
            Vector2 start = new Vector2(Projectile.Center.X - 800f, Projectile.Center.Y);
            Vector2 end = new Vector2(Projectile.Center.X + 800f, Projectile.Center.Y);

            // Draw a thick additively blended blue glowing line
            spriteBatch.End();
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullCounterClockwise, null, Main.GameViewMatrix.TransformationMatrix);

            float pulse = 0.8f + 0.2f * (float)Math.Sin(Main.GameUpdateCount * 0.15f);
            Color coreColor = Color.White * 0.9f;

            // Draw traversing pulse wave in segments (outward propagating ripples)
            int segmentsCount = 40;
            float segLength = 1600f / segmentsCount;
            Vector2 startPos = new Vector2(Projectile.Center.X - 800f, Projectile.Center.Y);
            
            for (int i = 0; i < segmentsCount; i++)
            {
                Vector2 segStart = startPos + new Vector2(i * segLength, 0f);
                Vector2 segEnd = startPos + new Vector2((i + 1) * segLength, 0f);
                
                // Calculate distance of segment center from projectile center
                float segCenterX = segStart.X + segLength * 0.5f;
                float distFromCenter = Math.Abs(segCenterX - Projectile.Center.X);
                
                // Rippling phase conducting outwards (every 15 frames per wave cycle)
                float wavePhase = (distFromCenter / 180f) - (Main.GameUpdateCount / 18f);
                float waveGlow = 0.35f + 0.65f * (float)Math.Cos(wavePhase * MathHelper.TwoPi);
                Color segColor = Color.DeepSkyBlue * (0.35f + 0.65f * waveGlow) * pulse * 0.72f;
                
                DrawLineHelper(spriteBatch, segStart, segEnd, segColor, 6f + 4f * waveGlow);
            }

            // Draw core bright line
            DrawLineHelper(spriteBatch, start, end, coreColor, 2f);

            // Draw end cap runes/nodes. These used to reuse the Cryogen NPC spritesheet, which drew every frame of
            // the boss's own body stacked into a smear at each end of the floor. A bloom core plus a slow spinning
            // cross is what an anchor node should look like — and BloomCircle is safe here because this whole
            // block is inside the additive scope (that texture has no alpha channel, only additive hides its black).
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle", AssetRequestMode.ImmediateLoad).Value;
            Color capColor = Color.DeepSkyBlue * pulse * 0.8f;
            foreach (Vector2 node in new[] { start, end })
            {
                if (bloom != null)
                    spriteBatch.Draw(bloom, node - Main.screenPosition, null, capColor * 0.75f, 0f, bloom.Size() * 0.5f, 0.28f, SpriteEffects.None, 0f);

                float spin = Main.GameUpdateCount * 0.02f * (node == start ? 1f : -1f);
                for (int i = 0; i < 4; i++)
                {
                    float a = spin + MathHelper.PiOver2 * i;
                    DrawLineHelper(spriteBatch, node - a.ToRotationVector2() * 16f, node + a.ToRotationVector2() * 16f, capColor, 2.5f);
                }
            }

            spriteBatch.End();
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullCounterClockwise, null, Main.GameViewMatrix.TransformationMatrix);

            return false;
        }

        private static void DrawLineHelper(SpriteBatch spriteBatch, Vector2 start, Vector2 end, Color color, float width)
        {
            Vector2 delta = end - start;
            float length = delta.Length();
            float rotation = delta.ToRotation();
            // MagicPixel is 1x1000, NOT 1x1 — sample a single pixel or the "line" becomes a 1000x-tall sheet
            spriteBatch.Draw(
                TextureAssets.MagicPixel.Value,
                start - Main.screenPosition,
                new Rectangle(0, 0, 1, 1),
                color,
                rotation,
                new Vector2(0f, 0.5f),
                new Vector2(length, width),
                SpriteEffects.None,
                0f
            );
        }
    }

    // =====================================================================================================================
    // WHITE FROST BOW ARROW (P1 ATTACK 1)
    // =====================================================================================================================
    public class CryogenMistArrow : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/Ranged/MistArrow";

        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 3;
        }

        public override void SetDefaults()
        {
            Projectile.width = 14;
            Projectile.height = 36;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 100;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
        }

        public override void AI()
        {
            // Simple frame animation
            Projectile.frameCounter++;
            if (Projectile.frameCounter >= 5)
            {
                Projectile.frame = (Projectile.frame + 1) % 3;
                Projectile.frameCounter = 0;
            }

            // Curve slightly
            Projectile.velocity = Projectile.velocity.RotatedBy(MathF.Sin(Projectile.timeLeft * 0.04f) * 0.003f);
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;

            // Emit frost particles
            if (Main.rand.NextBool(3))
            {
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.Ice, -Projectile.velocity * 0.2f, 100, Color.Cyan, Main.rand.NextFloat(1f, 1.3f));
                d.fadeIn = 1.2f;
                d.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            // Fullbright with a soft backglow — arrows must stay readable in dark biomes
            Texture2D tex = ModContent.Request<Texture2D>("CalamityMod/Projectiles/Ranged/MistArrow", AssetRequestMode.ImmediateLoad).Value;
            if (tex == null)
                return true;

            Rectangle frame = tex.Frame(1, 3, 0, Projectile.frame);
            Vector2 origin = frame.Size() * 0.5f;

            Color glow = new Color(40, 180, 240, 0) * 0.4f;
            for (int i = 0; i < 8; i++)
            {
                Vector2 off = (MathHelper.TwoPi * i / 8f).ToRotationVector2() * 3f;
                Main.spriteBatch.Draw(tex, Projectile.Center + off - Main.screenPosition, frame, glow, Projectile.rotation, origin, Projectile.scale, SpriteEffects.None, 0f);
            }
            Main.spriteBatch.Draw(tex, Projectile.Center - Main.screenPosition, frame, Color.White, Projectile.rotation, origin, Projectile.scale, SpriteEffects.None, 0f);
            return false;
        }

        public override void OnKill(int timeLeft)
        {
            SoundEngine.PlaySound(SoundID.Item27 with { Volume = 0.5f, Pitch = 0.1f }, Projectile.Center);
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            // Spawn 3 lingering frost mists
            for (int i = 0; i < 3; i++)
            {
                Vector2 vel = new Vector2(Main.rand.NextFloat(-1f, 1f), Main.rand.NextFloat(-2f, 0f));
                Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    Projectile.Center,
                    vel,
                    ModContent.ProjectileType<CryogenFrostMist>(),
                    Projectile.damage,
                    0f,
                    Main.myPlayer
                );
            }
        }
    }

    // =====================================================================================================================
    // LINGERING FROST MIST (P1 ATTACK 1)
    // =====================================================================================================================
    public class CryogenFrostMist : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/Ranged/MistArrowFrostMist";

        public override void SetDefaults()
        {
            Projectile.width = 30;
            Projectile.height = 30;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 160;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
        }

        // No damage while faded — an invisible mist that hurts is an unreadable hit
        public override bool? CanDamage() => Projectile.alpha < 140 ? null : (bool?)false;

        public override void AI()
        {
            Projectile.velocity *= 0.96f;
            Projectile.rotation += 0.012f;

            // Fade in and out
            float progress = (160f - Projectile.timeLeft) / 160f;
            Projectile.alpha = (int)(255 * (1f - MathF.Sin(progress * MathHelper.Pi)));
            Projectile.scale = 0.6f + progress * 0.8f;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            // Dynamically load the frost mist texture
            Texture2D tex = ModContent.Request<Texture2D>("CalamityMod/Projectiles/Ranged/MistArrowFrostMist", AssetRequestMode.ImmediateLoad).Value;
            if (tex == null)
                return true;

            SpriteEffects effects = Projectile.whoAmI % 2 == 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
            Color drawColor = Color.Lerp(Color.DeepSkyBlue, Color.Cyan, 0.5f) * ((255 - Projectile.alpha) / 255f) * 0.6f;
            
            Main.spriteBatch.Draw(
                tex,
                Projectile.Center - Main.screenPosition,
                null,
                drawColor,
                Projectile.rotation,
                tex.Size() * 0.5f,
                Projectile.scale,
                effects,
                0f
            );
            return false;
        }
    }

    // =====================================================================================================================
    // ICEBREAKER BOOMERANG HAMMER (P1 ATTACK 2)
    // =====================================================================================================================
    public class CryogenIcebreaker : ModProjectile
    {
        public override string Texture => "CalamityMod/Items/Weapons/Rogue/Icebreaker";

        public override void SetDefaults()
        {
            Projectile.width = 38;
            Projectile.height = 38;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 300;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
        }

        public override void AI()
        {
            Projectile.rotation += 0.18f;
            Projectile.localAI[0]++; // Local timer

            // Frost sheds off the spinning hammer
            if (Main.rand.NextBool(2))
            {
                Dust d = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(18f, 18f), DustID.Ice, -Projectile.velocity * 0.15f, 100, Color.Cyan, Main.rand.NextFloat(1f, 1.3f));
                d.fadeIn = 1.2f;
                d.noGravity = true;
            }

            // Check if Y coordinate crosses the barrier
            // Find active CryoStoneBarrier
            float barrierY = -9999f;
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile p = Main.projectile[i];
                if (p.active && p.type == ModContent.ProjectileType<CryoStoneBarrier>())
                {
                    barrierY = p.Center.Y;
                    break;
                }
            }

            if (barrierY != -9999f && Projectile.Center.Y >= barrierY && Projectile.velocity.Y > 0f)
            {
                // Hit the floor, explode!
                Projectile.Kill();
                return;
            }

            // Normal flying boomerang AI
            if (Projectile.localAI[0] < 80f)
            {
                // Straight path
            }
            else if (Projectile.localAI[0] < 110f)
            {
                // Slow down
                Projectile.velocity *= 0.92f;
            }
            else if (Projectile.localAI[0] < 128f)
            {
                // Pause and float in place
                Projectile.velocity = Vector2.Zero;
            }
            else if (Projectile.localAI[0] == 128f)
            {
                // Play warning flash cue
                SoundEngine.PlaySound(SoundID.Item30 with { Volume = 0.5f, Pitch = -0.2f }, Projectile.Center);
            }
            else
            {
                // Aim once at the player's real-time position when the return begins, then commit to a straight
                // line — design calls for "direct throw to real position, NOT continuous tracking". A re-aiming
                // lerp here would make this a soft homing missile with a huge window to correct, which straight-up
                // contradicts that and can feel inescapable in a tight arena.
                if (Projectile.localAI[1] == 0f)
                {
                    Player player = Main.player[Projectile.owner];
                    if (player.active && !player.dead)
                        Projectile.velocity = (player.Center - Projectile.Center).SafeNormalize(Vector2.UnitY) * Projectile.velocity.Length();
                    Projectile.localAI[1] = 1f;
                }

                float curSpeed = Projectile.velocity.Length();
                if (curSpeed < 21f)
                    Projectile.velocity *= Math.Min(21f / Math.Max(curSpeed, 0.01f), 1.14f);
            }
        }

        public override void OnKill(int timeLeft)
        {
            SoundEngine.PlaySound(SoundID.Item27, Projectile.Center);
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            // Explode into shards flying upwards/diagonally
            for (int i = 0; i < 5; i++)
            {
                Vector2 vel = new Vector2(Main.rand.NextFloat(-3f, 3f), Main.rand.NextFloat(-8f, -4f));
                Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    Projectile.Center,
                    vel,
                    ModContent.ProjectileType<CryogenJavelinShard>(),
                    Projectile.damage,
                    0f,
                    Main.myPlayer
                );
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            // Draw with ice-blue glow
            Texture2D tex = ModContent.Request<Texture2D>("CalamityMod/Items/Weapons/Rogue/Icebreaker", AssetRequestMode.ImmediateLoad).Value;
            if (tex == null)
                return true;

            Color glowColor = new Color(40, 180, 240, 0) * 0.4f;
            for (int i = 0; i < 12; i++)
            {
                Vector2 offset = (MathHelper.TwoPi * i / 12f).ToRotationVector2() * 4f;
                Main.spriteBatch.Draw(
                    tex,
                    Projectile.Center + offset - Main.screenPosition,
                    null,
                    glowColor,
                    Projectile.rotation,
                    tex.Size() * 0.5f,
                    Projectile.scale * 1.5f,
                    SpriteEffects.None,
                    0f
                );
            }

            Main.spriteBatch.Draw(
                tex,
                Projectile.Center - Main.screenPosition,
                null,
                Color.White,
                Projectile.rotation,
                tex.Size() * 0.5f,
                Projectile.scale * 1.5f,
                SpriteEffects.None,
                0f
            );
            return false;
        }
    }

    // =====================================================================================================================
    // ICE BOMB (P1 ATTACK 3 / AVALANCHE SHARDS)
    // =====================================================================================================================
    public class CryogenIceBomb : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/Boss/IceBomb";

        public override void SetDefaults()
        {
            Projectile.width = 24;
            Projectile.height = 24;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 70; // 70 frames lifetime total
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
        }

        public override bool? CanDamage()
        {
            // Only deal damage after it has finished rising (at 30+ frames)
            return Projectile.localAI[0] >= 30f ? null : (bool?)false;
        }

        public override void AI()
        {
            Projectile.localAI[0]++;
            Projectile.rotation += 0.06f;

            if (Projectile.localAI[0] == 1f)
            {
                // Start with upward vertical velocity
                Projectile.velocity = new Vector2(0f, -6f);
            }

            if (Projectile.localAI[0] <= 30f)
            {
                // Decelerate over the first 30 frames to a stop
                Projectile.velocity *= 0.9f;
                // Fade in and scale up
                float progress = Projectile.localAI[0] / 30f;
                Projectile.scale = MathHelper.Lerp(0.4f, 1.2f, progress);
                Projectile.alpha = (int)MathHelper.Lerp(255f, 0f, progress);
            }
            else
            {
                // Hover in place
                Projectile.velocity = Vector2.Zero;
                Projectile.alpha = 0;
                Projectile.scale = 1.2f + 0.08f * (float)Math.Sin((Projectile.localAI[0] - 30f) * 0.2f); // Pulsing hover
            }

            // Frost converges into the hovering bomb — the charge-up IS the warning
            if (Projectile.localAI[0] >= 20f && Main.rand.NextBool(3))
            {
                Vector2 around = Projectile.Center + (MathHelper.TwoPi * Main.rand.NextFloat()).ToRotationVector2() * Main.rand.NextFloat(30f, 70f);
                Dust d = Dust.NewDustPerfect(around, DustID.AncientLight, (Projectile.Center - around) * 0.08f, 100, Color.Cyan, Main.rand.NextFloat(1.1f, 1.4f));
                d.fadeIn = 1.2f;
                d.noGravity = true;
            }
        }

        public override void OnKill(int timeLeft)
        {
            SoundEngine.PlaySound(SoundID.Item27 with { Volume = 0.7f, Pitch = -0.1f }, Projectile.Center);
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            // Explode into cross (+) and diagonal axes, total 8 shards with gravity
            for (int i = 0; i < 8; i++)
            {
                float angle = MathHelper.PiOver4 * i;
                float speed = (i % 2 == 0) ? 7.5f : 4.5f; // Cross directions (+, 0, 90, 180, 270) are faster/stronger
                Vector2 vel = angle.ToRotationVector2() * speed;
                Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    Projectile.Center,
                    vel,
                    ModContent.ProjectileType<CryogenIceShard>(),
                    Projectile.damage,
                    0f,
                    Main.myPlayer
                );
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D tex = ModContent.Request<Texture2D>("CalamityMod/Projectiles/Boss/IceBomb", AssetRequestMode.ImmediateLoad).Value;
            if (tex == null)
                return true;

            Color c = Color.Lerp(Color.Cyan, Color.White, 0.4f) * ((255 - Projectile.alpha) / 255f);
            Main.spriteBatch.Draw(
                tex,
                Projectile.Center - Main.screenPosition,
                null,
                c,
                Projectile.rotation,
                tex.Size() * 0.5f,
                Projectile.scale,
                SpriteEffects.None,
                0f
            );
            return false;
        }
    }

    // =====================================================================================================================
    // FALLING SHARDS & STARS
    // =====================================================================================================================
    public class CryogenIceShard : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/Rogue/CrystalPiercerShard";

        public override void SetDefaults()
        {
            Projectile.width = 10;
            Projectile.height = 10;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 90;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
        }

        // CrystalPiercerShard.png is SIX pixels across. Eight of these come out of every ice bomb, and at native
        // size against a snow biome they were effectively invisible — the player ate damage from something they
        // never saw. Scaled up, given a glowing streak and a lit trail, they read as real shrapnel.
        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 6;
            ProjectileID.Sets.TrailingMode[Type] = 2;
        }

        public override void AI()
        {
            Projectile.velocity.Y += 0.15f; // Gravity
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;

            Lighting.AddLight(Projectile.Center, new Color(140, 220, 255).ToVector3() * 0.4f);

            if (!Main.dedServ && Main.rand.NextBool(2))
            {
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.BlueCrystalShard, -Projectile.velocity * 0.15f, 0, Color.White, Main.rand.NextFloat(0.9f, 1.3f));
                d.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D tex = ModContent.Request<Texture2D>("CalamityMod/Projectiles/Rogue/CrystalPiercerShard", AssetRequestMode.ImmediateLoad).Value;
            if (tex == null)
                return true;

            Vector2 origin = tex.Size() * 0.5f;
            const float Scale = 2.4f;

            // Motion streak: the shards fall fast, so a stretched afterimage is what actually catches the eye.
            for (int i = Projectile.oldPos.Length - 1; i >= 1; i--)
            {
                if (Projectile.oldPos[i] == Vector2.Zero)
                    continue;
                float pct = 1f - i / (float)Projectile.oldPos.Length;
                Vector2 ghost = Projectile.oldPos[i] + Projectile.Size * 0.5f - Main.screenPosition;
                Main.spriteBatch.Draw(tex, ghost, null, new Color(90, 200, 255, 0) * pct * 0.5f, Projectile.rotation, origin, Scale * pct, SpriteEffects.None, 0f);
            }

            // Zero-alpha backglow ring — brightens without darkening what is behind it
            Color glow = new Color(90, 210, 255, 0) * 0.55f;
            for (int i = 0; i < 8; i++)
            {
                Vector2 off = (MathHelper.TwoPi * i / 8f).ToRotationVector2() * 4f;
                Main.spriteBatch.Draw(tex, Projectile.Center + off - Main.screenPosition, null, glow, Projectile.rotation, origin, Scale, SpriteEffects.None, 0f);
            }

            Main.spriteBatch.Draw(
                tex,
                Projectile.Center - Main.screenPosition,
                null,
                Color.White,
                Projectile.rotation,
                origin,
                Scale,
                SpriteEffects.None,
                0f
            );
            return false;
        }
    }

    public class CryogenIceStar : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/Typeless/KelvinCatalystStar";

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 6;
            ProjectileID.Sets.TrailingMode[Type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = 12;
            Projectile.height = 12;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 140;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
        }

        public override void AI()
        {
            Projectile.velocity.Y += 0.1f;
            Projectile.rotation += 0.05f;

            Lighting.AddLight(Projectile.Center, new Color(120, 210, 255).ToVector3() * 0.4f);

            if (!Main.dedServ && Main.rand.NextBool(3))
            {
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.IceTorch, -Projectile.velocity * 0.12f, 0, Color.White, Main.rand.NextFloat(0.9f, 1.2f));
                d.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D tex = ModContent.Request<Texture2D>("CalamityMod/Projectiles/Typeless/KelvinCatalystStar", AssetRequestMode.ImmediateLoad).Value;
            if (tex == null)
                return true;

            Vector2 origin = tex.Size() * 0.5f;
            const float Scale = 1.5f;

            for (int i = Projectile.oldPos.Length - 1; i >= 1; i--)
            {
                if (Projectile.oldPos[i] == Vector2.Zero)
                    continue;
                float pct = 1f - i / (float)Projectile.oldPos.Length;
                Vector2 ghost = Projectile.oldPos[i] + Projectile.Size * 0.5f - Main.screenPosition;
                Main.spriteBatch.Draw(tex, ghost, null, new Color(60, 190, 255, 0) * pct * 0.45f, Projectile.rotation, origin, Scale * pct, SpriteEffects.None, 0f);
            }

            Color glow = new Color(60, 190, 255, 0) * 0.55f;
            for (int i = 0; i < 8; i++)
            {
                Vector2 off = (MathHelper.TwoPi * i / 8f).ToRotationVector2() * 4f;
                Main.spriteBatch.Draw(tex, Projectile.Center + off - Main.screenPosition, null, glow, Projectile.rotation, origin, Scale, SpriteEffects.None, 0f);
            }

            Main.spriteBatch.Draw(
                tex,
                Projectile.Center - Main.screenPosition,
                null,
                Color.White,
                Projectile.rotation,
                origin,
                Scale,
                SpriteEffects.None,
                0f
            );
            return false;
        }
    }

    // =====================================================================================================================
    // SNOWSTORM SNOWFLAKE (P1 ATTACK 4)
    // =====================================================================================================================
    public class CryogenSnowflake : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/Magic/Snowflake";

        public override void SetDefaults()
        {
            Projectile.width = 44;
            Projectile.height = 44;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 350;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
        }

        public override void AI()
        {
            Projectile.rotation += 0.05f;
            Projectile.localAI[0]++; // Proj age timer

            // Orbit math
            NPC parent = Main.npc[(int)Projectile.ai[0]];
            if (!parent.active || parent.type != ModContent.NPCType<CalamityMod.NPCs.Cryogen.Cryogen>())
            {
                Projectile.Kill();
                return;
            }

            Player player = Main.player[parent.target];
            if (!player.active || player.dead)
                return;

            // Determine lock-on sequence start time based on initial angle offset
            float lockStart = 80f;
            if (Math.Abs(Projectile.ai[1] - MathHelper.PiOver2) < 0.1f)
                lockStart = 130f;
            else if (Math.Abs(Projectile.ai[1] - MathHelper.Pi) < 0.1f)
                lockStart = 180f;

            // State management: localAI[1] = state (0 = orbit, 1 = lock-on, 2 = charge)
            int state = (int)Projectile.localAI[1];

            if (state == 0) // Orbiting
            {
                float speed = 0.016f;
                float angle = Projectile.ai[1] + Projectile.localAI[0] * speed;
                Vector2 desiredPos = player.Center + angle.ToRotationVector2() * 260f;
                Projectile.Center = Vector2.Lerp(Projectile.Center, desiredPos, 0.08f);

                // Shoot Kelvin stars in flight direction every 45 frames
                if (Projectile.localAI[0] % 45f == 0f && Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Vector2 radial = Projectile.Center - player.Center;
                    Vector2 tang = new Vector2(-radial.Y, radial.X).SafeNormalize(Vector2.UnitY);
                    float spread = MathHelper.ToRadians(25f);
                    for (int i = -1; i <= 1; i++)
                    {
                        Vector2 vel = tang.RotatedBy(i * spread) * 5f;
                        Projectile.NewProjectile(
                            Projectile.GetSource_FromThis(),
                            Projectile.Center,
                            vel,
                            ModContent.ProjectileType<CryogenIceStar>(),
                            Projectile.damage,
                            0f,
                            Main.myPlayer
                        );
                    }
                    SoundEngine.PlaySound(SoundID.Item28 with { Volume = 0.4f, Pitch = 0.1f }, Projectile.Center);
                }

                if (Projectile.localAI[0] >= lockStart)
                {
                    Projectile.localAI[1] = 1f; // Transition to Lock-on
                    Projectile.localAI[2] = 0f; // Reset state timer
                    Projectile.velocity = Vector2.Zero;
                    SoundEngine.PlaySound(SoundID.Item15 with { Volume = 0.6f, Pitch = 0.4f }, Projectile.Center);
                }
            }
            else if (state == 1) // Locking-on (30 frames)
            {
                Projectile.velocity = Vector2.Zero;
                Projectile.localAI[2]++; // Lock timer

                // Flare visual pulse during locking
                Projectile.scale = 1.0f + 0.15f * (float)Math.Sin(Projectile.localAI[2] * 0.4f);

                if (Projectile.localAI[2] >= 30f)
                {
                    // Lock-on complete! Charge!
                    Vector2 chargeDir = (player.Center - Projectile.Center).SafeNormalize(Vector2.UnitY);
                    Projectile.velocity = chargeDir * 21f;
                    Projectile.localAI[1] = 2f; // State 2: Charging
                    Projectile.localAI[2] = 0f; // Reset state timer
                    SoundEngine.PlaySound(SoundID.Item30 with { Volume = 0.8f, Pitch = -0.1f }, Projectile.Center);
                }
            }
            else if (state == 2) // Charging
            {
                Projectile.localAI[2]++;
                Projectile.scale = 1.25f;

                // Leave subtle ice trial dust
                if (Main.rand.NextBool(2))
                {
                    Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.Ice, -Projectile.velocity * 0.15f, 100, Color.Cyan, 1f);
                    d.noGravity = true;
                }

                // Die if traveled too far or too long
                if (Projectile.localAI[2] > 120f)
                {
                    Projectile.Kill();
                }
            }
        }

        public override void OnKill(int timeLeft)
        {
            // Quiet-kill sentinel (-1): the boss retires leftover flakes at attack transitions without the
            // surprise burst. Negative on purpose — localAI[1] doubles as the live state machine (0/1/2).
            if (Projectile.localAI[1] < 0f)
                return;

            SoundEngine.PlaySound(SoundID.Item27 with { Volume = 0.8f }, Projectile.Center);
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            // Explode into 6 stars in a circle
            for (int i = 0; i < 6; i++)
            {
                Vector2 vel = (MathHelper.TwoPi * i / 6f).ToRotationVector2() * 6f;
                Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    Projectile.Center,
                    vel,
                    ModContent.ProjectileType<CryogenIceStar>(),
                    Projectile.damage,
                    0f,
                    Main.myPlayer
                );
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D tex = ModContent.Request<Texture2D>("CalamityMod/Projectiles/Magic/Snowflake", AssetRequestMode.ImmediateLoad).Value;
            if (tex == null)
                return true;

            int state = (int)Projectile.localAI[1];
            float scale = Projectile.scale * 2.5f;

            if (state == 1) // Draw warning telegraph laser during lock-on
            {
                NPC parent = Main.npc[(int)Projectile.ai[0]];
                if (parent.active)
                {
                    Player player = Main.player[parent.target];
                    if (player.active && !player.dead)
                    {
                        float progress = MathHelper.Clamp(Projectile.localAI[2] / 30f, 0f, 1f);
                        SpriteBatch sb = Main.spriteBatch;

                        sb.End();
                        sb.Begin(SpriteSortMode.Deferred, BlendState.Additive, Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullCounterClockwise, null, Main.GameViewMatrix.TransformationMatrix);

                        // Draw line
                        Vector2 targetPos = player.Center;
                        Vector2 delta = targetPos - Projectile.Center;
                        float len = delta.Length();
                        float rot = delta.ToRotation();
                        sb.Draw(TextureAssets.MagicPixel.Value, Projectile.Center - Main.screenPosition, new Rectangle(0, 0, 1, 1), Color.Red * progress * 0.72f, rot, new Vector2(0f, 0.5f), new Vector2(len, 2f + progress * 2f), SpriteEffects.None, 0f);

                        // Draw target ring/reticle around the player. This used to blit the Cryogen NPC spritesheet —
                        // the boss's whole body, every frame stacked, painted red over the player. An actual
                        // reticle: a ring that tightens as the lock completes, plus four closing crosshair ticks.
                        Color reticle = Color.Lerp(Color.OrangeRed, Color.Red, progress) * (0.35f + progress * 0.6f);
                        float radius = MathHelper.Lerp(96f, 34f, progress);
                        const int RingSegments = 24;
                        for (int seg = 0; seg < RingSegments; seg++)
                        {
                            float a0 = MathHelper.TwoPi * seg / RingSegments + Main.GameUpdateCount * 0.05f;
                            float a1 = MathHelper.TwoPi * (seg + 1) / RingSegments + Main.GameUpdateCount * 0.05f;
                            DrawLineHelper(sb,targetPos + a0.ToRotationVector2() * radius, targetPos + a1.ToRotationVector2() * radius, reticle, 2f + progress * 2f);
                        }
                        for (int tick = 0; tick < 4; tick++)
                        {
                            Vector2 outward = (MathHelper.PiOver2 * tick + MathHelper.PiOver4).ToRotationVector2();
                            DrawLineHelper(sb,targetPos + outward * (radius + 18f), targetPos + outward * (radius + 4f), reticle, 3f);
                        }

                        sb.End();
                        sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullCounterClockwise, null, Main.GameViewMatrix.TransformationMatrix);
                    }
                }
            }

            // Normal snowflake sprite
            float cycle = Projectile.localAI[0] % 45f;
            float flash = (state == 0 && cycle > 33f) ? (cycle - 33f) / 12f : 0f;
            if (state == 1)
                flash = Projectile.localAI[2] / 30f;

            Main.spriteBatch.Draw(
                tex,
                Projectile.Center - Main.screenPosition,
                null,
                Color.Lerp(Color.White, Color.Cyan, flash * 0.8f) * (0.95f + flash * 0.35f),
                Projectile.rotation,
                tex.Size() * 0.5f,
                scale + flash * 0.45f,
                SpriteEffects.None,
                0f
            );
            return false;
        }

        private static void DrawLineHelper(SpriteBatch spriteBatch, Vector2 start, Vector2 end, Color color, float width)
        {
            Vector2 delta = end - start;
            // MagicPixel is 1x1000, NOT 1x1 — sample a single pixel or the "line" becomes a 1000x-tall sheet
            spriteBatch.Draw(
                TextureAssets.MagicPixel.Value,
                start - Main.screenPosition,
                new Rectangle(0, 0, 1, 1),
                color,
                delta.ToRotation(),
                new Vector2(0f, 0.5f),
                new Vector2(delta.Length(), width),
                SpriteEffects.None,
                0f
            );
        }
    }

    // =====================================================================================================================
    // SOUL OF CRYOGEN SHARD (P1 ATTACK 5)
    // =====================================================================================================================
    public class CryogenSoulShard : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/Rogue/FrostShardFriendly";

        // Calamity's FrostShardFriendly.png is a 5-frame vertical sheet (12x155). Without this the whole strip
        // gets drawn as one 155px-long smear instead of a single 31px shard.
        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 5;
        }

        public override void SetDefaults()
        {
            Projectile.width = 14;
            Projectile.height = 24;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 130;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
        }

        public override void AI()
        {
            Projectile.velocity.Y += 0.2f; // Downwards gravity acceleration
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;

            Projectile.frameCounter++;
            if (Projectile.frameCounter >= 5)
            {
                Projectile.frame = (Projectile.frame + 1) % 5;
                Projectile.frameCounter = 0;
            }

            if (Main.rand.NextBool(2))
            {
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.Ice, -Projectile.velocity * 0.1f, 100, Color.Cyan, Main.rand.NextFloat(1f, 1.3f));
                d.fadeIn = 1.2f;
                d.noGravity = true;
            }

            // A raining shard has to be spotted against a snow biome; dust alone washes out. A short streak of
            // bright ice motes with fadeIn gives it a readable "falling glass" trail.
            if (!Main.dedServ && Main.rand.NextBool(2))
            {
                Dust spark = Dust.NewDustPerfect(Projectile.Center - Projectile.velocity * Main.rand.NextFloat(0.4f), DustID.BlueCrystalShard, Vector2.Zero, 0, Color.White, Main.rand.NextFloat(0.9f, 1.3f));
                spark.noGravity = true;
                spark.velocity = -Projectile.velocity * 0.05f;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D tex = ModContent.Request<Texture2D>("CalamityMod/Projectiles/Rogue/FrostShardFriendly", AssetRequestMode.ImmediateLoad).Value;
            if (tex == null)
                return true;

            Rectangle frame = tex.Frame(1, 5, 0, Projectile.frame);
            Vector2 origin = frame.Size() * 0.5f;

            // Zero-alpha backglow: brightens without darkening what's behind it, so the shard stays legible
            // over both the snow and the boss's own light.
            Color glow = new Color(60, 190, 255, 0) * 0.45f;
            for (int i = 0; i < 8; i++)
            {
                Vector2 off = (MathHelper.TwoPi * i / 8f).ToRotationVector2() * 3.5f;
                Main.spriteBatch.Draw(tex, Projectile.Center + off - Main.screenPosition, frame, glow, Projectile.rotation, origin, Projectile.scale * 1.2f, SpriteEffects.None, 0f);
            }

            Main.spriteBatch.Draw(
                tex,
                Projectile.Center - Main.screenPosition,
                frame,
                Color.DeepSkyBlue * 0.9f,
                Projectile.rotation,
                origin,
                Projectile.scale * 1.2f,
                SpriteEffects.None,
                0f
            );
            return false;
        }
    }

    // =====================================================================================================================
    // GLACIAL EMBRACE SPIKE (P1 ATTACK 6)
    // =====================================================================================================================
    public class CryogenGlbraceSpike : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/Summon/GlacialEmbracePointyThing";

        public override void SetDefaults()
        {
            Projectile.width = 14;
            Projectile.height = 36;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 360;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
        }

        public override void AI()
        {
            Projectile.localAI[0]++;

            NPC parent = Main.npc[(int)Projectile.ai[0]];
            if (!parent.active || parent.type != ModContent.NPCType<CalamityMod.NPCs.Cryogen.Cryogen>())
            {
                Projectile.Kill();
                return;
            }

            if (Projectile.localAI[0] < 240f)
            {
                // Orbit and contract
                float progress = Math.Min(Projectile.localAI[0] / 220f, 1f);
                float radius = MathHelper.Lerp(200f, 100f, progress);
                float orbitAngle = Projectile.ai[1] + Projectile.localAI[0] * 0.04f;

                Projectile.Center = parent.Center + orbitAngle.ToRotationVector2() * radius;
                Projectile.rotation = Projectile.ai[1] + Projectile.localAI[0] * 0.06f;
                Projectile.velocity = Vector2.Zero;
            }
            else if (Projectile.localAI[0] == 240f)
            {
                // Shoot outward in radial direction
                float finalAngle = Projectile.ai[1] + Projectile.localAI[0] * 0.04f;
                Projectile.velocity = finalAngle.ToRotationVector2() * 18f;
                Projectile.rotation = finalAngle + MathHelper.PiOver2;
                SoundEngine.PlaySound(SoundID.Item30 with { Volume = 0.5f }, Projectile.Center);
            }
            else
            {
                // Constant velocity, fades after traveling 560px
                float distTraveled = (Projectile.localAI[0] - 240f) * 18f;
                if (distTraveled >= 560f)
                {
                    Projectile.Kill();
                }
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D tex = ModContent.Request<Texture2D>("CalamityMod/Projectiles/Summon/GlacialEmbracePointyThing", AssetRequestMode.ImmediateLoad).Value;
            if (tex == null)
                return true;

            // Draw with bright ice outline trail
            Color glowColor = new Color(80, 220, 255, 0) * 0.4f;
            for (int i = 0; i < 12; i++)
            {
                Vector2 offset = (MathHelper.TwoPi * i / 12f).ToRotationVector2() * 4f;
                Main.spriteBatch.Draw(
                    tex,
                    Projectile.Center + offset - Main.screenPosition,
                    null,
                    glowColor,
                    Projectile.rotation,
                    tex.Size() * 0.5f,
                    1.3f,
                    SpriteEffects.None,
                    0f
                );
            }

            Main.spriteBatch.Draw(
                tex,
                Projectile.Center - Main.screenPosition,
                null,
                Color.White,
                Projectile.rotation,
                tex.Size() * 0.5f,
                1.3f,
                SpriteEffects.None,
                0f
            );
            return false;
        }
    }

    // =====================================================================================================================
    // DARKLIGHT GREATSWORD BLUE/PURPLE BEAMS (P2 ATTACK 7)
    // =====================================================================================================================
    public class CryogenDarkBeam : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/Melee/DarkBeam";

        public override void SetDefaults()
        {
            Projectile.width = 30;
            Projectile.height = 30;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 280;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
        }

        public override void AI()
        {
            Projectile.localAI[0]++;

            if (Projectile.localAI[0] < 36f)
            {
                // Slow phase
                Projectile.velocity = Projectile.velocity.SafeNormalize(Vector2.UnitX) * 3f;
                Projectile.scale = 2.0f;
                Projectile.alpha = 150; // Semi-transparent

                // Dark energy converges into the ghost while it creeps — the charge IS the tell for the lunge
                if (Main.rand.NextBool(2))
                {
                    Vector2 around = Projectile.Center + (MathHelper.TwoPi * Main.rand.NextFloat()).ToRotationVector2() * Main.rand.NextFloat(25f, 60f);
                    Dust d = Dust.NewDustPerfect(around, DustID.Vortex, (Projectile.Center - around) * 0.09f, 120, Color.MediumPurple, Main.rand.NextFloat(1f, 1.3f));
                    d.fadeIn = 1.2f;
                    d.noGravity = true;
                }
            }
            else if (Projectile.localAI[0] == 36f)
            {
                // Trigger acceleration leap!
                Projectile.velocity = Projectile.velocity.SafeNormalize(Vector2.UnitX) * 28f;
                Projectile.scale = 1.0f;
                Projectile.alpha = 0;
                SoundEngine.PlaySound(SoundID.Item71 with { Volume = 0.7f, Pitch = 0.1f }, Projectile.Center);
            }

            // DarkBeam.png is the Darklight Greatsword's blade drawn diagonally (tip top-right). Pointing it along
            // the lunge needs the same +45° every diagonal Calamity sprite needs.
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver4;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D tex = ModContent.Request<Texture2D>("CalamityMod/Projectiles/Melee/DarkBeam", AssetRequestMode.ImmediateLoad).Value;
            if (tex == null)
                return true;

            Color c = Color.Purple * ((255 - Projectile.alpha) / 255f) * 0.8f;

            // After the lunge, a speed-stretched ghost trail sells the sudden acceleration
            if (Projectile.localAI[0] > 36f)
            {
                for (int i = 3; i >= 1; i--)
                {
                    Vector2 ghostPos = Projectile.Center - Projectile.velocity * i * 0.45f;
                    Main.spriteBatch.Draw(tex, ghostPos - Main.screenPosition, null, new Color(150, 70, 220, 0) * (0.4f - i * 0.1f), Projectile.rotation, tex.Size() * 0.5f, Projectile.scale, SpriteEffects.None, 0f);
                }
            }

            Main.spriteBatch.Draw(
                tex,
                Projectile.Center - Main.screenPosition,
                null,
                c,
                Projectile.rotation,
                tex.Size() * 0.5f,
                Projectile.scale,
                SpriteEffects.None,
                0f
            );
            return false;
        }
    }

    public class CryogenLightBeam : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/Melee/LightBeam";

        public override void SetDefaults()
        {
            Projectile.width = 24;
            Projectile.height = 24;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 240;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
        }

        public override void AI()
        {
            // Same slow-then-fast language as its DarkBeam sibling: 18f crawl, then it snaps forward
            Projectile.localAI[0]++;
            float speed = Projectile.localAI[0] < 18f ? 7f : Math.Min(7f + (Projectile.localAI[0] - 18f) * 1.8f, 24f);
            Projectile.velocity = Projectile.velocity.SafeNormalize(Vector2.UnitX) * speed;
            // Same diagonal blade sheet as its DarkBeam sibling — +45° to sit on the flight path.
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver4;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D tex = ModContent.Request<Texture2D>("CalamityMod/Projectiles/Melee/LightBeam", AssetRequestMode.ImmediateLoad).Value;
            if (tex == null)
                return true;

            Vector2 origin = tex.Size() * 0.5f;

            // Its DarkBeam sibling already sells the snap-forward with a stretched ghost trail; this one was left
            // as a single flat sprite and read as much less dangerous than the attack it belongs to.
            if (Projectile.localAI[0] > 18f)
            {
                for (int i = 3; i >= 1; i--)
                {
                    Vector2 ghostPos = Projectile.Center - Projectile.velocity * i * 0.45f;
                    Main.spriteBatch.Draw(tex, ghostPos - Main.screenPosition, null, new Color(70, 190, 255, 0) * (0.4f - i * 0.1f), Projectile.rotation, origin, Projectile.scale, SpriteEffects.None, 0f);
                }
            }

            Color glow = new Color(70, 190, 255, 0) * 0.45f;
            for (int i = 0; i < 8; i++)
            {
                Vector2 off = (MathHelper.TwoPi * i / 8f).ToRotationVector2() * 4f;
                Main.spriteBatch.Draw(tex, Projectile.Center + off - Main.screenPosition, null, glow, Projectile.rotation, origin, Projectile.scale, SpriteEffects.None, 0f);
            }

            Main.spriteBatch.Draw(
                tex,
                Projectile.Center - Main.screenPosition,
                null,
                Color.DeepSkyBlue * 0.85f,
                Projectile.rotation,
                origin,
                Projectile.scale,
                SpriteEffects.None,
                0f
            );
            return false;
        }
    }

    // =====================================================================================================================
    // STARNIGHT LANCE BEAM & TELEGRAPH (P2 ATTACK 8)
    // =====================================================================================================================
    public class CryogenStarnightBeam : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/Melee/StarnightBeam";

        public override void SetDefaults()
        {
            Projectile.width = 18;
            Projectile.height = 36;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 120;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
        }

        public override void AI()
        {
            // Brief emergence at moderate speed, then a violent lunge — never pure constant velocity
            Projectile.localAI[0]++;
            float speed = Projectile.localAI[0] < 8f ? 16f : Math.Min(16f + (Projectile.localAI[0] - 8f) * 4f, 44f);
            Projectile.velocity = Projectile.velocity.SafeNormalize(Vector2.UnitX) * speed;
            // StarnightBeam.png is a diagonal bolt whose tip sits in the top-right corner, so it lines up with
            // the flight path at +45°, not +90°.
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver4;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D tex = ModContent.Request<Texture2D>("CalamityMod/Projectiles/Melee/StarnightBeam", AssetRequestMode.ImmediateLoad).Value;
            if (tex == null)
                return true;

            // Speed-scaled afterimage trail — the faster it lunges, the longer the streak
            for (int i = 4; i >= 1; i--)
            {
                Vector2 ghostPos = Projectile.Center - Projectile.velocity * i * 0.5f;
                Main.spriteBatch.Draw(tex, ghostPos - Main.screenPosition, null, Color.Cyan * (0.5f - i * 0.1f), Projectile.rotation, tex.Size() * 0.5f, Projectile.scale, SpriteEffects.None, 0f);
            }

            Main.spriteBatch.Draw(
                tex,
                Projectile.Center - Main.screenPosition,
                null,
                Color.Cyan * 0.95f,
                Projectile.rotation,
                tex.Size() * 0.5f,
                Projectile.scale,
                SpriteEffects.None,
                0f
            );
            return false;
        }
    }

    // =====================================================================================================================
    // SHADECRYSTAL BARRAGE (P2 ATTACK 9)
    // =====================================================================================================================
    public class CryogenShadecrystal : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/Magic/ShadecrystalProjectile";

        // Materializes in place (no damage), forming a readable ring, then launches along its stored heading.
        private const int TelegraphTime = 24;

        public override void SetDefaults()
        {
            Projectile.width = 16;
            Projectile.height = 16;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 240;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
        }

        public override bool? CanDamage()
        {
            if (Projectile.localAI[0] < TelegraphTime)
                return false;
            return null;
        }

        public override void AI()
        {
            Projectile.localAI[0]++;

            if (Projectile.localAI[0] == 1f)
            {
                // Freeze at the ring position; remember the heading we'll strike along
                Projectile.ai[0] = Projectile.velocity.X;
                Projectile.ai[1] = Projectile.velocity.Y;
                Projectile.velocity = Vector2.Zero;
            }

            if (Projectile.localAI[0] < TelegraphTime)
            {
                // Spin in place & fade in — the forming ring reads clearly before it closes
                float p = Projectile.localAI[0] / (float)TelegraphTime;
                Projectile.rotation += 0.25f;
                Projectile.scale = 0.5f + p * 0.6f;
                Projectile.alpha = (int)(255 * (1f - p));
                if (Main.rand.NextBool(3))
                {
                    Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.Vortex, (MathHelper.TwoPi * Main.rand.NextFloat()).ToRotationVector2() * Main.rand.NextFloat(1.4f), 120, Color.Purple, 0.7f);
                    d.noGravity = true;
                }
            }
            else if (Projectile.localAI[0] == TelegraphTime)
            {
                // Close inward along the stored heading
                Projectile.velocity = new Vector2(Projectile.ai[0], Projectile.ai[1]);
                Projectile.alpha = 0;
                Projectile.scale = 1f;
                SoundEngine.PlaySound(SoundID.Item27 with { Volume = 0.35f, Pitch = 0.2f }, Projectile.Center);
            }
            else
            {
                // The ring tightens with gentle acceleration after launch
                if (Projectile.velocity.Length() < 19f)
                    Projectile.velocity *= 1.02f;

                Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
                if (Main.rand.NextBool(4))
                {
                    Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.Vortex, -Projectile.velocity * 0.1f, 120, Color.Purple, 0.8f);
                    d.noGravity = true;
                }
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D tex = ModContent.Request<Texture2D>("CalamityMod/Projectiles/Magic/ShadecrystalProjectile", AssetRequestMode.ImmediateLoad).Value;
            if (tex == null)
                return true;

            float fade = (255 - Projectile.alpha) / 255f;

            // A bright warning halo while forming, so the ring & its gap are unmistakable
            if (Projectile.localAI[0] < TelegraphTime)
            {
                float pulse = 0.4f + 0.6f * (Projectile.localAI[0] / (float)TelegraphTime);
                Main.spriteBatch.Draw(tex, Projectile.Center - Main.screenPosition, null, Color.White * pulse * fade, Projectile.rotation, tex.Size() * 0.5f, Projectile.scale * 1.5f, SpriteEffects.None, 0f);
            }

            Main.spriteBatch.Draw(tex, Projectile.Center - Main.screenPosition, null, Color.MediumPurple * 0.95f * fade, Projectile.rotation, tex.Size() * 0.5f, Projectile.scale, SpriteEffects.None, 0f);
            return false;
        }
    }

    // =====================================================================================================================
    // DAEDALUS GOLEM PELLET & LIGHTNING (P2 ATTACK 10)
    // =====================================================================================================================
    public class CryogenDaedalusPellet : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/Summon/DaedalusPellet";

        // Calamity's DaedalusPellet.png is a 3-frame vertical sheet (16x54); drawing it whole stretches one
        // pellet into a 54px ribbon.
        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 3;
        }

        public override void SetDefaults()
        {
            Projectile.width = 14;
            Projectile.height = 14;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 180;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
        }

        public override void AI()
        {
            Projectile.velocity.Y += 0.08f; // Falling gravity
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;

            Projectile.frameCounter++;
            if (Projectile.frameCounter >= 5)
            {
                Projectile.frame = (Projectile.frame + 1) % 3;
                Projectile.frameCounter = 0;
            }

            // A 14px pellet fired from an orbiting golem is easy to lose in a crowded arena — give it a
            // short bright tracer so the incoming line is readable before it arrives.
            if (!Main.dedServ && Main.rand.NextBool(2))
            {
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.BlueCrystalShard, -Projectile.velocity * 0.12f, 0, Color.White, Main.rand.NextFloat(0.9f, 1.25f));
                d.noGravity = true;
            }
            Lighting.AddLight(Projectile.Center, new Color(120, 200, 255).ToVector3() * 0.35f);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D tex = ModContent.Request<Texture2D>("CalamityMod/Projectiles/Summon/DaedalusPellet", AssetRequestMode.ImmediateLoad).Value;
            if (tex == null)
                return true;

            Rectangle frame = tex.Frame(1, 3, 0, Projectile.frame);
            Vector2 origin = frame.Size() * 0.5f;

            Color glow = new Color(60, 170, 255, 0) * 0.5f;
            for (int i = 0; i < 8; i++)
            {
                Vector2 off = (MathHelper.TwoPi * i / 8f).ToRotationVector2() * 3f;
                Main.spriteBatch.Draw(tex, Projectile.Center + off - Main.screenPosition, frame, glow, Projectile.rotation, origin, Projectile.scale * 1.15f, SpriteEffects.None, 0f);
            }

            Main.spriteBatch.Draw(
                tex,
                Projectile.Center - Main.screenPosition,
                frame,
                Color.LightBlue,
                Projectile.rotation,
                origin,
                Projectile.scale * 1.15f,
                SpriteEffects.None,
                0f
            );
            return false;
        }
    }

    public class CryogenDaedalusLightning : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetDefaults()
        {
            Projectile.width = 40;
            Projectile.height = 40;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 48; // 18 frames telegraph, 30 frames active
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
        }

        public override bool? CanDamage() => Projectile.timeLeft <= 30;

        public override void AI()
        {
            if (Projectile.localAI[0] == 0f)
            {
                // Capture rotation from initial velocity direction
                if (Projectile.velocity != Vector2.Zero)
                {
                    Projectile.rotation = Projectile.velocity.ToRotation();
                }
                
                // If it is spawned horizontally (e.g. from Golem minion), lock Y position to the Golem height
                if (Math.Abs(Projectile.rotation) < 0.05f || Math.Abs(Projectile.rotation - MathHelper.Pi) < 0.05f)
                {
                    Projectile.Center = new Vector2(Projectile.Center.X, Projectile.ai[0]);
                }
                
                Projectile.velocity = Vector2.Zero;
            }
            Projectile.localAI[0]++;

            if (Main.dedServ)
                return;

            Vector2 dir = Projectile.rotation.ToRotationVector2();
            Vector2 perp = new Vector2(-dir.Y, dir.X);
            Vector2 laneStart = Projectile.Center - dir * 800f;

            if (Projectile.timeLeft > 30)
            {
                // Charge-up motes converge onto the lane so the warning line is more than a thin red streak.
                if (Main.rand.NextBool(2))
                {
                    Vector2 at = laneStart + dir * Main.rand.NextFloat(1600f);
                    Dust d = Dust.NewDustPerfect(at + perp * Main.rand.NextFloat(-46f, 46f), DustID.Electric, Vector2.Zero, 0, Color.White, Main.rand.NextFloat(0.8f, 1.2f));
                    d.velocity = -perp * Math.Sign(d.position.Y - at.Y) * Main.rand.NextFloat(1.5f, 3.5f);
                    d.noGravity = true;
                }
            }
            else
            {
                // Crackling sparks along the live bolt so it reads as lethal even at a glance
                for (int i = 0; i < 2; i++)
                {
                    Vector2 sparkAt = laneStart + dir * Main.rand.NextFloat(1600f) + perp * Main.rand.NextFloat(-10f, 10f);
                    Dust d = Dust.NewDustPerfect(sparkAt, DustID.Electric, perp * Main.rand.NextFloat(-4f, 4f), 0, Color.White, Main.rand.NextFloat(1f, 1.5f));
                    d.noGravity = true;
                }
                Lighting.AddLight(Projectile.Center, new Color(150, 180, 255).ToVector3() * 0.6f);
            }
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            if (Projectile.timeLeft > 30)
                return false;

            // Collision check along the rotated line segment
            Vector2 dir = Projectile.rotation.ToRotationVector2();
            Vector2 start = Projectile.Center - dir * 800f;
            Vector2 end = Projectile.Center + dir * 800f;

            float collisionPoint = 0f;
            return Collision.CheckAABBvLineCollision(
                targetHitbox.TopLeft(),
                targetHitbox.Size(),
                start,
                end,
                24f, // slightly wider hit line for premium feel
                ref collisionPoint
            );
        }

        public override bool PreDraw(ref Color lightColor)
        {
            SpriteBatch spriteBatch = Main.spriteBatch;
            Vector2 dir = Projectile.rotation.ToRotationVector2();
            Vector2 start = Projectile.Center - dir * 800f;
            Vector2 end = Projectile.Center + dir * 800f;

            bool active = Projectile.timeLeft <= 30;
            float opacity = active ? MathHelper.Clamp(Projectile.timeLeft / 15f, 0f, 1f) : 0.42f + 0.18f * (float)Math.Sin(Main.GameUpdateCount * 0.3f);

            Color color = Color.Lerp(Color.DeepSkyBlue, Color.Purple, 0.4f);

            spriteBatch.End();
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullCounterClockwise, null, Main.GameViewMatrix.TransformationMatrix);

            if (!active)
            {
                // Draw thin red warning telegraph line along the rotated direction
                DrawLineHelper(spriteBatch, start, end, Color.Red * opacity, 3f);
            }
            else
            {
                // NOT the DaedalusLightning sprite: Calamity never draws that PNG (DaedalusLightning.PreDraw renders
                // a PrimitiveRenderer trail instead), so the file is an unused placeholder — a plain white octagon.
                // Tiling it produced a row of stretched white lozenges, not a bolt. Draw the arc ourselves instead:
                // a jagged polyline that reads as electricity, seeded off identity so every client draws the same one.
                Vector2 perp = new Vector2(-dir.Y, dir.X);
                const int Segments = 16;
                float wobbleSeed = Projectile.identity * 1.37f;

                Vector2 prev = start;
                for (int i = 1; i <= Segments; i++)
                {
                    float t = i / (float)Segments;
                    // Taper the jitter to zero at both ends so the bolt still spans the full lane.
                    float taper = MathF.Sin(t * MathHelper.Pi);
                    float jitter = (MathF.Sin(wobbleSeed + t * 21f + Main.GameUpdateCount * 0.55f) + MathF.Sin(wobbleSeed * 2.3f + t * 37f)) * 9f * taper;
                    Vector2 node = start + dir * (1600f * t) + perp * jitter;

                    DrawLineHelper(spriteBatch, prev, node, color * opacity * 0.55f, 18f);
                    DrawLineHelper(spriteBatch, prev, node, color * opacity, 7f);
                    DrawLineHelper(spriteBatch, prev, node, Color.White * opacity, 2.5f);
                    prev = node;
                }
            }

            spriteBatch.End();
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullCounterClockwise, null, Main.GameViewMatrix.TransformationMatrix);

            return false;
        }

        private static void DrawLineHelper(SpriteBatch spriteBatch, Vector2 start, Vector2 end, Color color, float width)
        {
            Vector2 delta = end - start;
            float length = delta.Length();
            float rotation = delta.ToRotation();
            // MagicPixel is 1x1000, NOT 1x1 — sample a single pixel or the "line" becomes a 1000x-tall sheet
            spriteBatch.Draw(
                TextureAssets.MagicPixel.Value,
                start - Main.screenPosition,
                new Rectangle(0, 0, 1, 1),
                color,
                rotation,
                new Vector2(0f, 0.5f),
                new Vector2(length, width),
                SpriteEffects.None,
                0f
            );
        }
    }

    // =====================================================================================================================
    // SHIMMERSPARK YOYO & STARS (P2 ATTACK 11)
    // =====================================================================================================================
    public class CryogenShimmersparkYoyo : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/Melee/Yoyos/ShimmersparkYoyo";

        public override void SetDefaults()
        {
            Projectile.width = 28;
            Projectile.height = 28;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 400;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
        }

        public override void AI()
        {
            Projectile.rotation += 0.16f;
            Projectile.localAI[0]++;

            NPC parent = Main.npc[(int)Projectile.ai[0]];
            if (!parent.active || parent.type != ModContent.NPCType<CalamityMod.NPCs.Cryogen.Cryogen>())
            {
                Projectile.Kill();
                return;
            }

            // Orbital expansion logic
            float radius = 140f;
            if (Projectile.timeLeft < 20f)
            {
                float factor = (20f - Projectile.timeLeft) / 20f;
                radius = MathHelper.Lerp(140f, 280f, factor);
            }

            float speed = 0.015f;
            float angle = Projectile.ai[1] + Projectile.localAI[0] * speed;
            Projectile.Center = parent.Center + angle.ToRotationVector2() * radius;

            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            // Emit yoyo stars periodically
            if (Projectile.localAI[0] % 20f == 0f && Projectile.timeLeft >= 20f)
            {
                Vector2 vel = Main.rand.NextFloat(MathHelper.TwoPi).ToRotationVector2() * 4f;
                Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    Projectile.Center,
                    vel,
                    ModContent.ProjectileType<CryogenYoyoStar>(),
                    Projectile.damage,
                    0f,
                    Main.myPlayer
                );
            }

            // Outward burst at end
            if (Projectile.timeLeft == 20f)
            {
                for (int i = 0; i < 6; i++)
                {
                    Vector2 vel = (MathHelper.TwoPi * i / 6f).ToRotationVector2() * 4f;
                    Projectile.NewProjectile(
                        Projectile.GetSource_FromThis(),
                        Projectile.Center,
                        vel,
                        ModContent.ProjectileType<CryogenYoyoStar>(),
                        Projectile.damage,
                        0f,
                        Main.myPlayer
                    );
                }
                SoundEngine.PlaySound(SoundID.Item28 with { Volume = 0.6f }, Projectile.Center);
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            NPC parent = Main.npc[(int)Projectile.ai[0]];
            if (parent.active && parent.type == ModContent.NPCType<CalamityMod.NPCs.Cryogen.Cryogen>())
            {
                // Draw yoyo string from boss center to yoyo
                Vector2 parentCenter = parent.Center;
                Vector2 currentCenter = Projectile.Center;
                Vector2 diff = parentCenter - currentCenter;
                float len = diff.Length();
                Vector2 dir = diff.SafeNormalize(Vector2.Zero);

                int segments = 16;
                float segLen = len / segments;
                for (int i = 0; i < segments; i++)
                {
                    float progress = i / (float)segments;
                    Vector2 perpendicular = new Vector2(-dir.Y, dir.X);
                    // Sine-wave wobble for natural string suspension
                    float wobble = (float)Math.Sin(progress * MathHelper.Pi + Main.GameUpdateCount * 0.25f) * 6f;
                    float nextWobble = (float)Math.Sin(((i + 1) / (float)segments) * MathHelper.Pi + Main.GameUpdateCount * 0.25f) * 6f;

                    Vector2 pos = currentCenter + dir * (i * segLen) + perpendicular * wobble;
                    Vector2 nextPos = currentCenter + dir * ((i + 1) * segLen) + perpendicular * nextWobble;

                    DrawLineHelper(Main.spriteBatch, pos, nextPos, Color.Cyan * 0.65f, 2.5f);
                    DrawLineHelper(Main.spriteBatch, pos, nextPos, Color.White * 0.9f, 1f);
                }
            }

            Texture2D tex = ModContent.Request<Texture2D>("CalamityMod/Projectiles/Melee/Yoyos/ShimmersparkYoyo", AssetRequestMode.ImmediateLoad).Value;
            if (tex == null)
                return true;

            // Glowing yoyo trails
            Color glowColor = Color.Magenta * 0.4f;
            for (int i = 0; i < 4; i++)
            {
                Vector2 offset = (MathHelper.TwoPi * i / 4f).ToRotationVector2() * 3f;
                Main.spriteBatch.Draw(
                    tex,
                    Projectile.Center + offset - Main.screenPosition,
                    null,
                    glowColor,
                    Projectile.rotation,
                    tex.Size() * 0.5f,
                    1.2f,
                    SpriteEffects.None,
                    0f
                );
            }

            Main.spriteBatch.Draw(
                tex,
                Projectile.Center - Main.screenPosition,
                null,
                Color.White,
                Projectile.rotation,
                tex.Size() * 0.5f,
                1.2f,
                SpriteEffects.None,
                0f
            );
            return false;
        }

        private static void DrawLineHelper(SpriteBatch spriteBatch, Vector2 start, Vector2 end, Color color, float width)
        {
            Vector2 diff = end - start;
            float len = diff.Length();
            float rot = diff.ToRotation();
            // MagicPixel is 1x1000, NOT 1x1 — sample a single pixel or the "line" becomes a 1000x-tall sheet
            spriteBatch.Draw(
                TextureAssets.MagicPixel.Value,
                start - Main.screenPosition,
                new Rectangle(0, 0, 1, 1),
                color,
                rot,
                new Vector2(0f, 0.5f),
                new Vector2(len, width),
                SpriteEffects.None,
                0f
            );
        }
    }

    public class CryogenYoyoStar : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetDefaults()
        {
            Projectile.width = 12;
            Projectile.height = 12;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 150;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
        }

        public override void AI()
        {
            Projectile.rotation += 0.05f;

            // Weak homing towards player
            Player player = Main.player[Player.FindClosest(Projectile.Center, 1, 1)];
            if (player.active && !player.dead)
            {
                Vector2 destDir = (player.Center - Projectile.Center).SafeNormalize(Vector2.UnitY);
                Projectile.velocity = Vector2.Lerp(Projectile.velocity, destDir * 4f, 0.024f);
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            // Rotating four-point sparkle with a soft halo instead of a bare square
            Vector2 drawPos = Projectile.Center - Main.screenPosition;
            Texture2D pixel = TextureAssets.MagicPixel.Value;
            Rectangle source = new Rectangle(0, 0, 1, 1);

            Main.spriteBatch.Draw(pixel, drawPos, source, new Color(180, 100, 255, 0) * 0.35f, 0f, new Vector2(0.5f), new Vector2(16f, 16f), SpriteEffects.None, 0f);
            Main.spriteBatch.Draw(pixel, drawPos, source, Color.MediumOrchid * 0.9f, Projectile.rotation, new Vector2(0.5f), new Vector2(11f, 2.5f), SpriteEffects.None, 0f);
            Main.spriteBatch.Draw(pixel, drawPos, source, Color.MediumOrchid * 0.9f, Projectile.rotation + MathHelper.PiOver2, new Vector2(0.5f), new Vector2(11f, 2.5f), SpriteEffects.None, 0f);
            return false;
        }
    }

    // =====================================================================================================================
    // DARKECHO bow arrow and CRYSTAL DART (P2 ATTACK 11)
    // =====================================================================================================================
    public class CryogenDarkechoArrow : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetDefaults()
        {
            Projectile.width = 10;
            Projectile.height = 10;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 200;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
        }

        public override void AI()
        {
            // Straight aimed shot, matching design (two arrows bracket the player at +-0.18 rad). No homing:
            // these are spawned in a pair to form a dodgeable "V" gap around the player — a continuous re-aim
            // would pull both arrows onto the same point over their 200-frame lifetime and delete that gap.
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;

            if (Main.rand.NextBool(3))
            {
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.Vortex, -Projectile.velocity * 0.05f, 100, Color.MediumPurple, 0.8f);
                d.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            // Render a telegraphed purple laser bolt
            Vector2 start = Projectile.Center - Projectile.velocity.SafeNormalize(Vector2.UnitY) * 20f;
            DrawLineHelper(Main.spriteBatch, start, Projectile.Center, Color.MediumPurple * 0.8f, 3f);
            DrawLineHelper(Main.spriteBatch, start, Projectile.Center, Color.White * 0.5f, 1f);
            return false;
        }

        private static void DrawLineHelper(SpriteBatch spriteBatch, Vector2 start, Vector2 end, Color color, float width)
        {
            Vector2 delta = end - start;
            float length = delta.Length();
            float rotation = delta.ToRotation();
            // MagicPixel is 1x1000, NOT 1x1 — sample a single pixel or the "line" becomes a 1000x-tall sheet
            spriteBatch.Draw(
                TextureAssets.MagicPixel.Value,
                start - Main.screenPosition,
                new Rectangle(0, 0, 1, 1),
                color,
                rotation,
                new Vector2(0f, 0.5f),
                new Vector2(length, width),
                SpriteEffects.None,
                0f
            );
        }
    }

    public class CryogenCrystalDart : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetDefaults()
        {
            Projectile.width = 12;
            Projectile.height = 12;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 240;
            Projectile.tileCollide = true; // Bounces off solid walls
            Projectile.ignoreWater = true;
        }

        public override void AI()
        {
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            Projectile.ai[0]++; // Count bounces
            if (Projectile.ai[0] > 1)
            {
                Projectile.Kill();
                return true;
            }

            // Single bounce reflection
            Collision.HitTiles(Projectile.position, Projectile.velocity, Projectile.width, Projectile.height);
            SoundEngine.PlaySound(SoundID.Item10, Projectile.Center);

            if (Projectile.velocity.X != oldVelocity.X)
            {
                Projectile.velocity.X = -oldVelocity.X;
            }
            if (Projectile.velocity.Y != oldVelocity.Y)
            {
                Projectile.velocity.Y = -oldVelocity.Y;
            }

            return false;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            // A 22-speed dart reads as a streak, not a dot: soft halo line + hot core stretched along the velocity
            Vector2 drawPos = Projectile.Center - Main.screenPosition;
            Texture2D pixel = TextureAssets.MagicPixel.Value;
            Rectangle source = new Rectangle(0, 0, 1, 1);
            float rot = Projectile.velocity.ToRotation();
            float len = MathHelper.Clamp(Projectile.velocity.Length() * 1.6f, 14f, 44f);

            Main.spriteBatch.Draw(pixel, drawPos, source, new Color(255, 60, 160, 0) * 0.45f, rot, new Vector2(0.5f), new Vector2(len * 1.4f, 7f), SpriteEffects.None, 0f);
            Main.spriteBatch.Draw(pixel, drawPos, source, Color.DeepPink * 0.95f, rot, new Vector2(0.5f), new Vector2(len, 3.5f), SpriteEffects.None, 0f);
            Main.spriteBatch.Draw(pixel, drawPos, source, Color.White * 0.8f, rot, new Vector2(0.5f), new Vector2(len * 0.55f, 1.5f), SpriteEffects.None, 0f);
            return false;
        }
    }

    // =====================================================================================================================
    // CRYSTAL PIERCER JAVELIN & SHARDS (P2 ATTACK 12)
    // =====================================================================================================================
    public class CryogenJavelin : ModProjectile
    {
        public override string Texture => "CalamityMod/Items/Weapons/Rogue/CrystalPiercer";

        public override void SetDefaults()
        {
            Projectile.width = 18;
            Projectile.height = 42;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 300;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
        }

        public override void AI()
        {
            Projectile.localAI[0]++;

            // Gravity arc behavior or straight path based on ai[0]
            // ai[0] = 0 (gravity arc from left), 1 (straight from right), 2 (vertical straight from top)
            int mode = (int)Projectile.ai[0];

            if (mode == 0)
            {
                Projectile.velocity.Y += 0.25f; // Gravity
            }
            else if (Projectile.velocity.Length() < 26f)
            {
                // Straight-line javelins build speed as they cross the arena
                Projectile.velocity *= 1.012f;
            }

            // CrystalPiercer is an ITEM sprite: like every Calamity item icon its tip points up-RIGHT, i.e. 45°,
            // not straight up. +PiOver2 was tilting the whole javelin a quarter-turn off its own flight path.
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver4;

            // Emit shard trailing every 14 frames
            if (Projectile.localAI[0] % 14f == 0f && Main.netMode != NetmodeID.MultiplayerClient)
            {
                Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    Projectile.Center,
                    new Vector2(0f, 6f), // Vertical fall
                    ModContent.ProjectileType<CryogenJavelinShard>(),
                    Projectile.damage,
                    0f,
                    Main.myPlayer
                );
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D tex = ModContent.Request<Texture2D>("CalamityMod/Items/Weapons/Rogue/CrystalPiercer", AssetRequestMode.ImmediateLoad).Value;
            if (tex == null)
                return true;

            Main.spriteBatch.Draw(
                tex,
                Projectile.Center - Main.screenPosition,
                null,
                Color.White,
                Projectile.rotation,
                tex.Size() * 0.5f,
                1.4f,
                SpriteEffects.None,
                0f
            );
            return false;
        }
    }

    public class CryogenJavelinShard : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/Rogue/CrystalPiercerShard";

        public override void SetDefaults()
        {
            Projectile.width = 8;
            Projectile.height = 8;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 80;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
        }

        public override void AI()
        {
            Projectile.velocity = new Vector2(0f, 6f); // Constant vertical falling speed
            Projectile.rotation += 0.05f;

            Lighting.AddLight(Projectile.Center, new Color(120, 220, 255).ToVector3() * 0.35f);

            if (!Main.dedServ && Main.rand.NextBool(3))
            {
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.BlueCrystalShard, new Vector2(0f, -1f), 0, Color.White, Main.rand.NextFloat(0.8f, 1.1f));
                d.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D tex = ModContent.Request<Texture2D>("CalamityMod/Projectiles/Rogue/CrystalPiercerShard", AssetRequestMode.ImmediateLoad).Value;
            if (tex == null)
                return true;

            // Same 6px sprite as CryogenIceShard — the javelin curtain drops a lot of these, so they get the same
            // scale-up and backglow treatment rather than a near-invisible speck.
            Vector2 origin = tex.Size() * 0.5f;
            const float Scale = 2f;

            Color glow = new Color(70, 200, 255, 0) * 0.5f;
            for (int i = 0; i < 8; i++)
            {
                Vector2 off = (MathHelper.TwoPi * i / 8f).ToRotationVector2() * 3.5f;
                Main.spriteBatch.Draw(tex, Projectile.Center + off - Main.screenPosition, null, glow, Projectile.rotation, origin, Scale, SpriteEffects.None, 0f);
            }

            Main.spriteBatch.Draw(
                tex,
                Projectile.Center - Main.screenPosition,
                null,
                Color.White,
                Projectile.rotation,
                origin,
                Scale,
                SpriteEffects.None,
                0f
            );
            return false;
        }
    }

    // =====================================================================================================================
    // FROST COLUMN TELEGRAPH + SPEAR (P1 ATTACK 1 REWORK — "读缝隙" / read the gap)
    // Director-layer template: 占位由Boss完成, 预告由弹幕本体承担 (CanDamage=false), 到点才落下真正的危险矛.
    // Copy this pattern for any "danger appears here in N frames" attack.
    // =====================================================================================================================
    // 这个类现在建在公共基类 LegendsTelegraph 上 —— 它原本是全项目唯一的独立预告弹幕，
    // 计时/无伤契约/加法混合/MagicPixel 采样这些通用部分已经上移，这里只剩它自己的形状：
    // 一根从天而降的冰柱预告。其余 boss 照这个样子实现 SpawnPayload 就能拿到同一套预警语言。
    public class CryogenFrostColumnTelegraph : LegendsTelegraph
    {
        private const float ColumnHeight = 620f;

        protected override int DefaultDuration => 45;
        protected override SoundStyle? FireSound => SoundID.Item28 with { Volume = 0.45f, Pitch = 0.35f };

        private Vector2 ColumnTop => new(Projectile.Center.X, Projectile.Center.Y - ColumnHeight);

        protected override void WarningTick(float progress)
        {
            // Frost motes rain down the column, thickening as the strike nears — reads unmistakably as "danger here soon".
            if (Main.rand.NextFloat() < progress * 0.9f)
            {
                float y = Projectile.Center.Y - Main.rand.NextFloat(ColumnHeight);
                Dust d = Dust.NewDustPerfect(new Vector2(Projectile.Center.X + Main.rand.NextFloat(-6f, 6f), y), DustID.Ice, new Vector2(0f, Main.rand.NextFloat(2f, 6f)), 90, Color.Cyan, 0.7f + progress * 0.6f);
                d.noGravity = true;
            }
        }

        protected override void SpawnPayload(int damage)
        {
            Projectile.NewProjectile(Projectile.GetSource_FromThis(), ColumnTop, new Vector2(0f, 17f), ModContent.ProjectileType<CryogenFrostSpear>(), damage, 0f, Main.myPlayer, Projectile.Center.Y);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            DrawStandardWarnBeam(Main.spriteBatch, ColumnTop, Projectile.Center, Color.DeepSkyBlue, Progress);
            return false;
        }
    }

    public class CryogenFrostSpear : ModProjectile
    {
        // Weapon identity: this IS the Hoarfrost Bow's arrow coming back down — same MistArrow texture as the bow's own shot.
        public override string Texture => "CalamityMod/Projectiles/Ranged/MistArrow";

        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 3;
        }

        // ai[0] = arena floor Y to die at
        public override void SetDefaults()
        {
            Projectile.width = 16;
            Projectile.height = 34;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 120;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
        }

        public override void AI()
        {
            Projectile.frameCounter++;
            if (Projectile.frameCounter >= 5)
            {
                Projectile.frame = (Projectile.frame + 1) % 3;
                Projectile.frameCounter = 0;
            }

            if (Projectile.velocity.Y < 23f)
                Projectile.velocity.Y += 0.5f; // a little weight
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;

            if (Projectile.ai[0] > 0f && Projectile.Center.Y >= Projectile.ai[0])
            {
                Projectile.Kill();
                return;
            }

            if (Main.rand.NextBool(2))
            {
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.Ice, -Projectile.velocity * 0.12f, 100, Color.Cyan, Main.rand.NextFloat(1.1f, 1.4f));
                d.fadeIn = 1.2f;
                d.noGravity = true;
            }
        }

        public override void OnKill(int timeLeft)
        {
            SoundEngine.PlaySound(SoundID.Item27 with { Volume = 0.35f, Pitch = 0.3f }, Projectile.Center);
            for (int i = 0; i < 10; i++)
            {
                Vector2 v = new Vector2(Main.rand.NextFloat(-3.5f, 3.5f), Main.rand.NextFloat(-8f, -4f));
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.Ice, v, 100, Color.White, Main.rand.NextFloat(1f, 1.4f));
                d.fadeIn = 1.3f;
                d.noGravity = true;
            }

            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            // Sparse lingering mist hazard: ~1 in 4 spears leaves one cloud. Two per spear across a 14-spear
            // curtain meant ~28 damaging mists per volley — way past the ≤15 on-screen red line.
            if (Main.rand.NextBool(4))
            {
                Vector2 vel = new Vector2(Main.rand.NextFloat(-1.5f, 1.5f), Main.rand.NextFloat(-1.5f, 0.5f));
                Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    Projectile.Center,
                    vel,
                    ModContent.ProjectileType<CryogenFrostMist>(),
                    Projectile.damage,
                    0f,
                    Main.myPlayer
                );
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D tex = ModContent.Request<Texture2D>("CalamityMod/Projectiles/Ranged/MistArrow", AssetRequestMode.ImmediateLoad).Value;
            if (tex == null)
                return true;

            Rectangle frame = tex.Frame(1, 3, 0, Projectile.frame);
            Vector2 origin = frame.Size() * 0.5f;

            // Thick zero-alpha backglow: adds brightness without darkening what's behind it
            Color glow = new Color(40, 180, 240, 0) * 0.4f;
            for (int i = 0; i < 12; i++)
            {
                Vector2 off = (MathHelper.TwoPi * i / 12f).ToRotationVector2() * 4f;
                Main.spriteBatch.Draw(tex, Projectile.Center + off - Main.screenPosition, frame, glow, Projectile.rotation, origin, Projectile.scale * 1.25f, SpriteEffects.None, 0f);
            }
            Main.spriteBatch.Draw(tex, Projectile.Center - Main.screenPosition, frame, Color.White, Projectile.rotation, origin, Projectile.scale * 1.25f, SpriteEffects.None, 0f);
            return false;
        }
    }

    // =====================================================================================================================
    // ICE ERUPTION (P1 AVALANCHE SHOCKWAVE) — one step of the traveling wall you jump over.
    // Telegraphs by growing out of the floor (CanDamage delayed ~10f), stays lethal briefly, then melts back down.
    // =====================================================================================================================
    public class CryogenIceEruption : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/Summon/GlacialEmbracePointyThing";

        public override void SetDefaults()
        {
            Projectile.width = 28;
            Projectile.height = 110;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 48;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
        }

        public override bool? CanDamage()
        {
            if (Projectile.localAI[0] < 10f || Projectile.localAI[0] > 36f)
                return false;
            return null;
        }

        public override void AI()
        {
            Projectile.localAI[0]++;
            if (Projectile.localAI[0] == 1f)
                SoundEngine.PlaySound(SoundID.Item27 with { Volume = 0.3f, Pitch = 0.4f }, Projectile.Center);

            float t = Projectile.localAI[0];
            if (t <= 12f)
                Projectile.scale = MathHelper.Lerp(0.2f, 1f, t / 12f); // erupt upward
            else if (t >= 36f)
                Projectile.scale = MathHelper.Lerp(1f, 0f, (t - 36f) / 12f); // melt back
            else
                Projectile.scale = 1f;

            if (Main.rand.NextBool(2))
            {
                Dust d = Dust.NewDustPerfect(Projectile.Center + new Vector2(Main.rand.NextFloat(-10f, 10f), 40f), DustID.Ice, new Vector2(0f, Main.rand.NextFloat(-3f, -1f)), 100, Color.Cyan, 1f);
                d.noGravity = true;
            }
        }

        public override void OnKill(int timeLeft)
        {
            // Shatter along the whole spike rather than popping from a single point
            for (float offset = 0f; offset < 100f; offset += Main.rand.NextFloat(8f, 16f))
            {
                Vector2 pos = Projectile.Center + new Vector2(Main.rand.NextFloat(-8f, 8f), Projectile.height * 0.5f - offset);
                Dust d = Dust.NewDustPerfect(pos, DustID.BlueCrystalShard, Main.rand.NextVector2Unit() * Main.rand.NextFloat(2f, 6f));
                d.velocity.Y -= 3f;
                d.scale = Main.rand.NextFloat(0.9f, 1.3f);
                d.noGravity = Main.rand.NextBool();
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D tex = ModContent.Request<Texture2D>("CalamityMod/Projectiles/Summon/GlacialEmbracePointyThing", AssetRequestMode.ImmediateLoad).Value;
            if (tex == null)
                return true;

            // Anchor at the base so the icicle grows upward out of the floor line
            Vector2 origin = new Vector2(tex.Width * 0.5f, tex.Height);
            Vector2 basePos = Projectile.Center + new Vector2(0f, Projectile.height * 0.5f) - Main.screenPosition;
            float alpha = Projectile.localAI[0] < 10f ? Projectile.localAI[0] / 10f : 1f;

            Color glow = new Color(40, 180, 240, 0) * 0.4f * alpha;
            for (int i = 0; i < 8; i++)
            {
                Vector2 off = (MathHelper.TwoPi * i / 8f).ToRotationVector2() * 3f;
                Main.spriteBatch.Draw(tex, basePos + off, null, glow, 0f, origin, new Vector2(1f, Projectile.scale * 1.4f), SpriteEffects.None, 0f);
            }
            Main.spriteBatch.Draw(tex, basePos, null, Color.White * alpha, 0f, origin, new Vector2(1f, Projectile.scale * 1.4f), SpriteEffects.None, 0f);
            return false;
        }
    }
}
