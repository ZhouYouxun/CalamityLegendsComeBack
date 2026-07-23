using System;
using CalamityMod;
using CalamityMod.Particles;
using CalamityMod.Projectiles.Summon;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Upgrade.CallofDuty
{
    /// <summary>
    /// Left Click sequence controller: Fires 3 tokens sequentially (哔 · 哔 · 哔——)
    /// 1. Cursor (光标 - Straight, establishes lock-on)
    /// 2. Signal Packet (信号包 - Curve, LCD block trail)
    /// 3. Confirm Key (确认键 - Pincer, Connect Pulse)
    /// </summary>
    internal sealed class ResponsibilityCommunicationSequence : ModProjectile
    {
        private int tokenIndex;
        private int tokenTimer;

        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 2;
            Projectile.friendly = false;
            Projectile.hostile = false;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 60;
            Projectile.netImportant = true;
        }

        public override bool ShouldUpdatePosition() => false;
        public override bool? CanDamage() => false;

        public override void AI()
        {
            if (!Main.player.IndexInRange(Projectile.owner) || tokenIndex >= 3)
            {
                Projectile.Kill();
                return;
            }

            if (tokenTimer-- > 0)
                return;

            if (Projectile.owner == Main.myPlayer)
            {
                Player owner = Main.player[Projectile.owner];
                CallofDutyPlayer phonePlayer = owner.GetModPlayer<CallofDutyPlayer>();

                // Pitch escalates for the 3 button tones: 哔 · 哔 · 哔——
                float pitch = -0.1f + tokenIndex * 0.22f;
                SoundEngine.PlaySound(SoundID.MenuTick with { Volume = 0.45f, Pitch = pitch }, Projectile.Center);

                float spread = tokenIndex == 0 ? 0f : (tokenIndex == 1 ? -0.22f : 0.22f);
                Vector2 velocity = Projectile.velocity.SafeNormalize(Vector2.UnitX * owner.direction).RotatedBy(spread) * (14f + tokenIndex * 2f);

                // Priority target homing check
                if (phonePlayer.FastDialPriorityTarget >= 0 && Main.npc.IndexInRange(phonePlayer.FastDialPriorityTarget))
                {
                    NPC priority = Main.npc[phonePlayer.FastDialPriorityTarget];
                    if (priority.CanBeChasedBy())
                    {
                        Vector2 desired = Projectile.SafeDirectionTo(priority.Center) * (16f + tokenIndex * 2f);
                        velocity = Vector2.Lerp(velocity, desired, 0.45f);
                    }
                }

                int token = Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    Projectile.Center,
                    velocity,
                    ModContent.ProjectileType<ResponsibilityCommunicationToken>(),
                    Projectile.damage,
                    Projectile.knockBack,
                    Projectile.owner,
                    tokenIndex,
                    Projectile.ai[1]); // SequenceId

                if (Main.projectile.IndexInRange(token))
                {
                    Main.projectile[token].localAI[0] = tokenIndex;
                    Main.projectile[token].netUpdate = true;
                }
            }

            tokenIndex++;
            tokenTimer = 5; // 5 frames between 哔 · 哔 · 哔
        }
    }

    internal sealed class ResponsibilityCommunicationToken : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public int TokenType => (int)Projectile.ai[0];
        public int SequenceId => (int)Projectile.ai[1];

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.CultistIsResistantTo[Type] = true;
            ProjectileID.Sets.TrailCacheLength[Type] = 8;
            ProjectileID.Sets.TrailingMode[Type] = 0;
        }

        public override void SetDefaults()
        {
            Projectile.width = 18;
            Projectile.height = 18;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.DamageType = DamageClass.Summon;
            Projectile.tileCollide = true;
            Projectile.ignoreWater = true;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 80;
        }

        public override void AI()
        {
            Player owner = Main.player[Projectile.owner];
            CallofDutyPlayer phonePlayer = owner.GetModPlayer<CallofDutyPlayer>();

            // Homing behavior for token 1 & 2 toward priority/locked target
            int targetIndex = phonePlayer.FastDialPriorityTarget >= 0 ? phonePlayer.FastDialPriorityTarget : phonePlayer.RedialTarget;
            if (TokenType > 0 && targetIndex >= 0 && Main.npc.IndexInRange(targetIndex) && Main.npc[targetIndex].CanBeChasedBy())
            {
                NPC target = Main.npc[targetIndex];
                Vector2 desired = Projectile.SafeDirectionTo(target.Center) * (14f + TokenType * 2f);
                float homingStrength = phonePlayer.FastDialPriorityTarget >= 0 ? 0.18f : 0.08f;
                Projectile.velocity = Vector2.Lerp(Projectile.velocity, desired, homingStrength);
            }

            // LCD particle trail
            if (Main.rand.NextBool(2))
            {
                Color lcdColor = TokenType == 0 ? new Color(132, 226, 255) : TokenType == 1 ? new Color(255, 218, 76) : new Color(194, 255, 67);
                Dust d = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(4f, 4f), DustID.Electric, -Projectile.velocity * 0.1f, 100, lcdColor, 0.55f);
                d.noGravity = true;
            }

            Projectile.rotation = Projectile.velocity.ToRotation();
            Lighting.AddLight(Projectile.Center, new Color(132, 226, 255).ToVector3() * 0.25f);
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            Player owner = Main.player[Projectile.owner];
            CallofDutyPlayer phonePlayer = owner.GetModPlayer<CallofDutyPlayer>();

            // Register sequence hit
            CallofDutyGlobalNPC.RegisterSequenceHit(target, Projectile.owner, SequenceId, TokenType, Projectile.damage);

            // Set Redial Target on 3rd token hit or full sequence completion
            if (TokenType == 2 || TokenType == 0)
            {
                phonePlayer.SetRedialTarget(target.whoAmI, 360);
            }

            // On Token 2 (Confirm Key), spawn square connect pulse
            if (TokenType == 2 && !Main.dedServ)
            {
                GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(
                    target.Center,
                    Vector2.Zero,
                    new Color(194, 255, 67),
                    Vector2.One * 0.6f,
                    0f,
                    0.05f,
                    0.65f,
                    16));

                SoundEngine.PlaySound(SoundID.MaxMana with { Volume = 0.35f, Pitch = 0.2f }, target.Center);
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            SpriteBatch sb = Main.spriteBatch;
            Vector2 drawPos = Projectile.Center - Main.screenPosition;
            Color lcdColor = TokenType == 0 ? new Color(132, 226, 255) : TokenType == 1 ? new Color(255, 218, 76) : new Color(194, 255, 67);

            // Draw 1, 2, 3 LCD block shape
            string text = TokenType == 0 ? "[1]" : TokenType == 1 ? "[2]" : "[3]";
            Vector2 textDim = FontAssets.MouseText.Value.MeasureString(text);
            Utils.DrawBorderString(sb, text, drawPos - textDim * 0.45f, lcdColor, 0.85f);

            // Draw short LCD trail
            for (int i = 0; i < Projectile.oldPos.Length; i += 2)
            {
                if (Projectile.oldPos[i] == Vector2.Zero) continue;
                Vector2 trailPos = Projectile.oldPos[i] + Projectile.Size * 0.5f - Main.screenPosition;
                float alpha = (1f - i / (float)Projectile.oldPos.Length) * 0.4f;
                Utils.DrawBorderString(sb, ".", trailPos - textDim * 0.3f, lcdColor * alpha, 0.7f);
            }

            return false;
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            SoundEngine.PlaySound(SoundID.MenuTick with { Volume = 0.2f, Pitch = -0.2f }, Projectile.Center);
            return true;
        }
    }

    /// <summary>
    /// Right Click Redial: Spawns a 3x4 LCD keyboard outline around the target.
    /// Sequentially lights up keys [2] -> [5] -> [8] (or 4 keys if Fast Dial active),
    /// popping keycaps towards the target to explode into a Signal Burst!
    /// </summary>
    internal sealed class CallofDutyRedialKeyboard : ModProjectile
    {
        private readonly int[] keysToPop = { 2, 5, 8, 11 }; // Key indices in 3x4 grid
        private int keyStep;
        private int timer;

        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";
        public int TargetIndex => (int)Projectile.ai[0];
        public bool IsFastDialBoosted => Projectile.ai[1] > 0f;

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 120;
            Projectile.friendly = false;
            Projectile.hostile = false;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 40;
        }

        public override bool ShouldUpdatePosition() => false;
        public override bool? CanDamage() => false;

        public override void AI()
        {
            if (!Main.npc.IndexInRange(TargetIndex) || !Main.npc[TargetIndex].active)
            {
                Projectile.Kill();
                return;
            }

            NPC target = Main.npc[TargetIndex];
            Projectile.Center = target.Center;

            timer++;

            int maxSteps = IsFastDialBoosted ? 4 : 3;
            int stepInterval = 8;

            if (keyStep < maxSteps && timer >= (keyStep + 1) * stepInterval)
            {
                if (Projectile.owner == Main.myPlayer)
                {
                    int keyNum = keysToPop[keyStep % keysToPop.Length];
                    Vector2 keyOffset = GetKeyMatrixOffset(keyNum);
                    Vector2 spawnPos = Projectile.Center + keyOffset;
                    Vector2 vel = (target.Center - spawnPos).SafeNormalize(Vector2.UnitY) * 16f;

                    bool isLastKey = keyStep == maxSteps - 1;
                    Projectile.NewProjectile(
                        Projectile.GetSource_FromThis(),
                        spawnPos,
                        vel,
                        ModContent.ProjectileType<CallofDutyRedialKeyCap>(),
                        Projectile.damage,
                        Projectile.knockBack,
                        Projectile.owner,
                        keyNum,
                        isLastKey ? 1f : 0f);

                    SoundEngine.PlaySound(SoundID.MenuTick with { Volume = 0.45f, Pitch = 0.1f + keyStep * 0.15f }, spawnPos);
                }
                keyStep++;
            }
        }

        private static Vector2 GetKeyMatrixOffset(int keyIndex)
        {
            int row = keyIndex / 3;
            int col = keyIndex % 3;
            return new Vector2((col - 1) * 26f, (row - 1.5f) * 24f);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            SpriteBatch sb = Main.spriteBatch;
            Vector2 centerPos = Projectile.Center - Main.screenPosition;
            float opacity = MathHelper.Clamp(Projectile.timeLeft / 10f, 0f, 0.45f);

            // Draw 3x4 LCD Keyboard Outline around target
            Color boardColor = new Color(132, 226, 255) * opacity;
            string[] keyLabels = { "1", "2", "3", "4", "5", "6", "7", "8", "9", "*", "0", "#" };

            int maxSteps = IsFastDialBoosted ? 4 : 3;

            for (int i = 0; i < 12; i++)
            {
                Vector2 keyOff = GetKeyMatrixOffset(i);
                Vector2 keyPos = centerPos + keyOff;

                bool isLit = false;
                for (int s = 0; s < keyStep && s < maxSteps; s++)
                {
                    if (keysToPop[s] == i)
                        isLit = true;
                }

                Color keyColor = isLit ? new Color(194, 255, 67) * (opacity * 2f) : boardColor;
                float scale = isLit ? 0.95f : 0.75f;
                Vector2 strSize = FontAssets.MouseText.Value.MeasureString(keyLabels[i]);

                Utils.DrawBorderString(sb, keyLabels[i], keyPos - strSize * 0.5f * scale, keyColor, scale);
            }

            return false;
        }
    }

    /// <summary>
    /// Keycap projectile popped from the 3x4 keyboard matrix towards target.
    /// </summary>
    internal sealed class CallofDutyRedialKeyCap : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public int KeyNumber => (int)Projectile.ai[0];
        public bool IsLastKey => Projectile.ai[1] > 0f;

        public override void SetDefaults()
        {
            Projectile.width = 22;
            Projectile.height = 22;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.DamageType = DamageClass.Summon;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 50;
        }

        public override void AI()
        {
            if (Main.rand.NextBool(2))
            {
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.Electric, -Projectile.velocity * 0.1f, 120, new Color(194, 255, 67), 0.5f);
                d.noGravity = true;
            }

            Lighting.AddLight(Projectile.Center, new Color(194, 255, 67).ToVector3() * 0.3f);
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (IsLastKey && !Main.dedServ)
            {
                // Final keycap hit: Signal Burst explosion
                GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(
                    target.Center,
                    Vector2.Zero,
                    new Color(132, 226, 255),
                    Vector2.One * 1.2f,
                    0f,
                    0.06f,
                    1.2f,
                    20));

                GeneralParticleHandler.SpawnParticle(new BloomParticle(
                    target.Center,
                    Vector2.Zero,
                    new Color(194, 255, 67),
                    0.25f,
                    0.6f,
                    20));

                SoundEngine.PlaySound(SoundID.Item93 with { Volume = 0.55f, Pitch = 0.3f }, target.Center);
            }
            else
            {
                SoundEngine.PlaySound(SoundID.MenuTick with { Volume = 0.35f, Pitch = 0.4f }, target.Center);
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            SpriteBatch sb = Main.spriteBatch;
            Vector2 pos = Projectile.Center - Main.screenPosition;
            string[] keyLabels = { "1", "2", "3", "4", "5", "6", "7", "8", "9", "*", "0", "#" };
            string label = KeyNumber >= 0 && KeyNumber < 12 ? keyLabels[KeyNumber] : "OK";

            string text = $"[{label}]";
            Vector2 textDim = FontAssets.MouseText.Value.MeasureString(text);
            Utils.DrawBorderString(sb, text, pos - textDim * 0.45f, new Color(194, 255, 67), 0.95f);

            return false;
        }
    }

    /// <summary>
    /// Left+Right hold Speed Dial Main Signal projectile (快捷拨号主信号)
    /// Slower main signal projectile with rectangular afterimages.
    /// Marks target as Priority Communication Object (FastDialPriorityTarget).
    /// </summary>
    internal sealed class CallofDutyFastDialSignal : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 12;
            ProjectileID.Sets.TrailingMode[Type] = 0;
        }

        public override void SetDefaults()
        {
            Projectile.width = 28;
            Projectile.height = 28;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.DamageType = DamageClass.Summon;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 120;
        }

        public override void AI()
        {
            Player owner = Main.player[Projectile.owner];
            CallofDutyPlayer phonePlayer = owner.GetModPlayer<CallofDutyPlayer>();

            int targetIndex = phonePlayer.FastDialPriorityTarget >= 0 ? phonePlayer.FastDialPriorityTarget : phonePlayer.RedialTarget;
            if (targetIndex >= 0 && Main.npc.IndexInRange(targetIndex) && Main.npc[targetIndex].CanBeChasedBy())
            {
                NPC target = Main.npc[targetIndex];
                Vector2 desired = Projectile.SafeDirectionTo(target.Center) * 12f;
                Projectile.velocity = Vector2.Lerp(Projectile.velocity, desired, 0.12f);
            }

            // Rectangular signal ghost afterimages particle
            if (Main.rand.NextBool(2))
            {
                Dust d = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(6f, 6f), DustID.Electric, -Projectile.velocity * 0.15f, 80, new Color(132, 226, 255), 0.65f);
                d.noGravity = true;
            }

            Lighting.AddLight(Projectile.Center, new Color(132, 226, 255).ToVector3() * 0.4f);
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            Player owner = Main.player[Projectile.owner];
            CallofDutyPlayer phonePlayer = owner.GetModPlayer<CallofDutyPlayer>();

            // Mark as Fast Dial Priority Target (6s for Boss, 12s for normal enemy)
            int duration = target.boss ? 360 : 720;
            phonePlayer.SetFastDialTarget(target.whoAmI, duration);

            if (!Main.dedServ)
            {
                GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(
                    target.Center,
                    Vector2.Zero,
                    new Color(132, 226, 255),
                    Vector2.One * 1.5f,
                    0f,
                    0.05f,
                    1.4f,
                    24));

                SoundEngine.PlaySound(SoundID.Item4 with { Volume = 0.5f, Pitch = 0.4f }, target.Center);
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            SpriteBatch sb = Main.spriteBatch;
            Vector2 pos = Projectile.Center - Main.screenPosition;
            string text = "<SPEED DIAL>";
            Vector2 textDim = FontAssets.MouseText.Value.MeasureString(text);
            Utils.DrawBorderString(sb, text, pos - textDim * 0.45f, new Color(132, 226, 255), 0.9f);

            // Draw rectangular signal ghosts
            for (int i = 0; i < Projectile.oldPos.Length; i += 2)
            {
                if (Projectile.oldPos[i] == Vector2.Zero) continue;
                Vector2 trailPos = Projectile.oldPos[i] + Projectile.Size * 0.5f - Main.screenPosition;
                float alpha = (1f - i / (float)Projectile.oldPos.Length) * 0.5f;
                Utils.DrawBorderString(sb, "■", trailPos - textDim * 0.2f, new Color(132, 226, 255) * alpha, 0.75f);
            }

            return false;
        }
    }

    internal sealed class ResponsibilityHatMarker : ModProjectile
    {
        public override string Texture => "CalamityMod/Items/Armor/Wulfrum/WulfrumHat";
        public int TargetIndex => (int)Projectile.ai[0];

        public override void SetDefaults()
        {
            Projectile.width = 26;
            Projectile.height = 18;
            Projectile.friendly = false;
            Projectile.hostile = false;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 360;
            Projectile.netImportant = true;
        }

        public override bool ShouldUpdatePosition() => false;
        public override bool? CanDamage() => false;

        public override void AI()
        {
            if (!Main.npc.IndexInRange(TargetIndex) || !Main.npc[TargetIndex].active)
            {
                Projectile.Kill();
                return;
            }

            NPC target = Main.npc[TargetIndex];
            Projectile.Center = target.Top - Vector2.UnitY * MathHelper.Clamp(target.height * 0.08f, 5f, 18f);
            Projectile.rotation = target.rotation * 0.18f;
            Projectile.scale = MathHelper.Clamp(target.width / 40f, 0.72f, 1.35f);

            Lighting.AddLight(Projectile.Center, new Color(76, 202, 255).ToVector3() * 0.35f);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = ModContent.Request<Texture2D>(Texture).Value;
            Vector2 position = Projectile.Center - Main.screenPosition;
            Vector2 origin = texture.Size() * 0.5f;

            for (int i = 0; i < 8; i++)
            {
                Vector2 offset = (MathHelper.TwoPi * i / 8f).ToRotationVector2() * 2f;
                Main.EntitySpriteDraw(texture, position + offset, null, new Color(68, 194, 255) * 0.75f, Projectile.rotation, origin, Projectile.scale, SpriteEffects.None);
            }
            Main.EntitySpriteDraw(texture, position, null, lightColor, Projectile.rotation, origin, Projectile.scale, SpriteEffects.None);
            return false;
        }
    }

    internal sealed class ResponsibilityHatImpact : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 32;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.DamageType = DamageClass.Summon;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 2;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }

        public override bool? CanHitNPC(NPC target) => target.whoAmI == (int)Projectile.ai[0];

        public override void AI()
        {
            if (Main.npc.IndexInRange((int)Projectile.ai[0]) && Main.npc[(int)Projectile.ai[0]].active)
                Projectile.Center = Main.npc[(int)Projectile.ai[0]].Center;
        }
    }
}
