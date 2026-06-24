using CalamityLegendsComeBack.Weapons.LeonidProgenitor.Core;
using CalamityLegendsComeBack.Weapons.LeonidProgenitor.Effects;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.LeonidProgenitor
{
    public class LeonidCometSmall : ModProjectile, ILocalizedModType
    {
        public const int FromStealthFlag = 1;
        public const int SilverSplitFlag = 2;
        public const int SpectreCloneFlag = 4;
        public const int GravityFieldFlag = 8;

        public override string Texture => "CalamityMod/Items/Weapons/Rogue/LeonidProgenitor";
        public new string LocalizationCategory => "Projectiles.LeonidProgenitor";

        private readonly HashSet<string> effectFlags = new();
        private readonly Dictionary<string, float> effectStates = new();

        private LeonidMetalEffect[] activeEffects = System.Array.Empty<LeonidMetalEffect>();
        private bool initialized;
        private Vector2 playerOffset;

        public int PrimaryEffectID => (int)Projectile.ai[0];
        public int SecondaryEffectID => (int)Projectile.ai[1];
        public int SpawnFlags => (int)Projectile.ai[2];
        public bool FromStealthRain => (SpawnFlags & FromStealthFlag) != 0;
        public Player Owner => Main.player[Projectile.owner];
        public Vector2 InitialCenter { get; private set; }
        public Color MeteorColor { get; private set; }

        public bool IgnoreGravity { get; private set; }
        public bool EnableHoming { get; private set; }
        public float HomingStrength { get; private set; }
        public float HomingRange { get; private set; } = 720f;

        // Custom fields for the new design
        public int BounceCount;
        public bool IsOrbiting;
        private int orbitTransitionTimer = -1;
        private int bounceCooldown;

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 12;
            ProjectileID.Sets.TrailingMode[Type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = 28;
            Projectile.height = 28;
            Projectile.friendly = true;
            Projectile.ignoreWater = true;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 300;
            Projectile.extraUpdates = 1;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
            Projectile.alpha = 255;
        }

        public override void OnSpawn(Terraria.DataStructures.IEntitySource source)
        {
            InitialCenter = Projectile.Center;
            MeteorColor = LeonidVisualUtils.GetMeteorColor(PrimaryEffectID, SecondaryEffectID);
            activeEffects = LeonidMetalEffectRegistry.ResolveEffects(PrimaryEffectID, SecondaryEffectID);
            Projectile.DamageType = Owner.HeldItem.DamageType;
            playerOffset = Projectile.Center - Owner.Center;

            if ((SpawnFlags & SilverSplitFlag) != 0)
                SetFlag("silver_split");

            if ((SpawnFlags & SpectreCloneFlag) != 0)
                SetFlag("spectre_clone");

            for (int i = 0; i < activeEffects.Length; i++)
                activeEffects[i].OnSpawn(this, Owner);

            initialized = true;
        }

        public override void AI()
        {
            if (!initialized)
                OnSpawn(Projectile.GetSource_FromThis());

            // Handle launch delay
            if (Projectile.localAI[1] > 0f)
            {
                Projectile.localAI[1]--;
                Projectile.velocity = Vector2.Zero;

                if ((SpawnFlags & GravityFieldFlag) != 0)
                {
                    // Gravity field meteor: stay stationary at spawn position
                    Projectile.Center = InitialCenter;

                    if (Projectile.localAI[1] <= 0f)
                    {
                        Projectile.velocity = new Vector2(Main.rand.NextFloat(-2f, 2f), 16f);
                        Projectile.netUpdate = true;
                        SoundEngine.PlaySound(SoundID.Item8 with { Volume = 0.5f, Pitch = 0.2f }, Projectile.Center);
                    }
                }
                else
                {
                    // Player-held meteor: follow player relative offset
                    Vector2 currentOffset = playerOffset;
                    currentOffset.Y += (float)Math.Sin(Main.GlobalTimeWrappedHourly * 6f) * 4f;
                    Projectile.Center = Owner.Center + currentOffset;

                    if (Projectile.localAI[1] <= 0f)
                    {
                        Vector2 targetVelocity = (Main.MouseWorld - Projectile.Center).SafeNormalize(Vector2.UnitY) * 14f;
                        Projectile.velocity = targetVelocity;
                        Projectile.netUpdate = true;
                        SoundEngine.PlaySound(SoundID.Item8 with { Volume = 0.5f, Pitch = 0.2f }, Projectile.Center);
                    }
                }

                Projectile.friendly = false;
                return;
            }

            Projectile.friendly = true;
            Projectile.alpha -= 32;
            if (Projectile.alpha < 0)
                Projectile.alpha = 0;

            Lighting.AddLight(Projectile.Center, MeteorColor.ToVector3() * (FromStealthRain ? 0.75f : 0.5f));

            if (bounceCooldown > 0)
                bounceCooldown--;

            // Handle Orbiting Shield state
            if (IsOrbiting)
            {
                int myOrbitIndex = 0;
                int totalOrbiting = 0;
                int oldestProj = -1;
                int minTimeLeft = 999999;

                for (int i = 0; i < Main.maxProjectiles; i++)
                {
                    Projectile p = Main.projectile[i];
                    if (p.active && p.type == Type && p.owner == Projectile.owner && p.ModProjectile is LeonidCometSmall lcs && lcs.IsOrbiting)
                    {
                        if (p.whoAmI == Projectile.whoAmI)
                        {
                            myOrbitIndex = totalOrbiting;
                        }
                        totalOrbiting++;

                        if (p.timeLeft < minTimeLeft)
                        {
                            minTimeLeft = p.timeLeft;
                            oldestProj = i;
                        }
                    }
                }

                if (totalOrbiting > 6 && oldestProj != -1 && Projectile.whoAmI == oldestProj)
                {
                    Projectile.Kill();
                    return;
                }

                float angle = Main.GlobalTimeWrappedHourly * 4.5f + (myOrbitIndex * MathHelper.TwoPi / 6f);
                Vector2 orbitPos = Owner.Center + angle.ToRotationVector2() * 70f;
                Projectile.Center = orbitPos;
                Projectile.velocity = Vector2.Zero;
                Projectile.tileCollide = false;
                Projectile.penetrate = -1;

                if (Main.rand.NextBool(4))
                {
                    Dust trailDust = Dust.NewDustPerfect(
                        Projectile.Center + Main.rand.NextVector2Circular(6f, 6f),
                        DustID.TintableDustLighted,
                        Main.rand.NextVector2Circular(1f, 1f),
                        100,
                        Color.Lerp(MeteorColor, Color.Purple, 0.4f),
                        Main.rand.NextFloat(0.6f, 1f));
                    trailDust.noGravity = true;
                }
                return;
            }

            // Orbit transition timer
            if (orbitTransitionTimer > 0)
            {
                orbitTransitionTimer--;
                if (orbitTransitionTimer == 0)
                {
                    IsOrbiting = true;
                    Projectile.timeLeft = 360; // 6 seconds
                    Projectile.netUpdate = true;
                    return;
                }
            }

            // Collide and bounce check with right-click reflective meteors
            if (BounceCount < 7 && bounceCooldown <= 0)
            {
                int reflectiveType = ModContent.ProjectileType<LeonidReflectiveMeteor>();
                for (int i = 0; i < Main.maxProjectiles; i++)
                {
                    Projectile p = Main.projectile[i];
                    if (p.active && p.type == reflectiveType && p.ModProjectile is LeonidReflectiveMeteor reflective)
                    {
                        if (reflective.TimesBounced < 7 && Vector2.Distance(Projectile.Center, p.Center) < 32f)
                        {
                            // Trigger bounce!
                            BounceCount++;
                            reflective.TimesBounced++;
                            bounceCooldown = 12;

                            // Push reflective meteor a short distance
                            Vector2 moveDir = Projectile.velocity.SafeNormalize(Vector2.UnitX);
                            p.velocity = moveDir * 5f;
                            reflective.decelerateTimer = 15; // stay pushed before decelerating

                            float speed = Projectile.velocity.Length() * 1.05f; // speed up by 5%

                            // Play billiard clink sound
                            SoundEngine.PlaySound(SoundID.Item10 with { Volume = 0.7f, Pitch = 0.3f }, Projectile.Center);

                            if (BounceCount >= 7)
                            {
                                // Target nearest enemy and prepare to orbit after 4.5 frames (5 ticks)
                                NPC target = FindClosestNPC(1000f);
                                if (target != null)
                                {
                                    Projectile.velocity = (target.Center - Projectile.Center).SafeNormalize(Vector2.UnitY) * speed;
                                }
                                else
                                {
                                    Projectile.velocity = Projectile.velocity.SafeNormalize(Vector2.UnitY) * speed;
                                }
                                orbitTransitionTimer = 5;
                            }
                            else
                            {
                                // Redirect towards another reflective meteor, or nearest enemy if none
                                Projectile.velocity = FindNextBounceDirection(p.whoAmI) * speed;
                            }

                            Projectile.netUpdate = true;
                            break;
                        }
                    }
                }
            }

            // Gravity effect
            if (!IgnoreGravity && orbitTransitionTimer <= 0)
            {
                Projectile.localAI[0]++;
                if (Projectile.localAI[0] >= 14f)
                {
                    Projectile.velocity.Y += 0.22f;
                    if (Projectile.velocity.Y > 16f)
                        Projectile.velocity.Y = 16f;
                }
            }

            // Homing check
            if (EnableHoming || FromStealthRain || (BounceCount > 0 && BounceCount < 7))
            {
                if (FromStealthRain && BounceCount < 7)
                {
                    // Prioritize targeting reflective meteors
                    Projectile targetProj = FindClosestReflectiveMeteor(600f);
                    if (targetProj != null)
                    {
                        Vector2 desiredVelocity = (targetProj.Center - Projectile.Center).SafeNormalize(Vector2.UnitY) * Projectile.velocity.Length();
                        Projectile.velocity = Vector2.Lerp(Projectile.velocity, desiredVelocity, 0.08f);
                    }
                    else
                    {
                        NPC targetNPC = FindClosestNPC(HomingRange);
                        if (targetNPC != null)
                        {
                            Vector2 desiredVelocity = (targetNPC.Center - Projectile.Center).SafeNormalize(Vector2.UnitY) * Projectile.velocity.Length();
                            Projectile.velocity = Vector2.Lerp(Projectile.velocity, desiredVelocity, 0.06f);
                        }
                    }
                }
                else if (BounceCount > 0 && BounceCount < 7)
                {
                    // Homing to closest reflective meteor to facilitate billiard chains
                    Projectile targetProj = FindClosestReflectiveMeteor(800f);
                    if (targetProj != null)
                    {
                        Vector2 desiredVelocity = (targetProj.Center - Projectile.Center).SafeNormalize(Vector2.UnitY) * Projectile.velocity.Length();
                        Projectile.velocity = Vector2.Lerp(Projectile.velocity, desiredVelocity, 0.07f);
                    }
                }
                else
                {
                    NPC target = FindClosestNPC(HomingRange);
                    if (target != null)
                    {
                        Vector2 desiredVelocity = (target.Center - Projectile.Center).SafeNormalize(Vector2.UnitY) * Projectile.velocity.Length();
                        Projectile.velocity = Vector2.Lerp(Projectile.velocity, desiredVelocity, HomingStrength > 0 ? HomingStrength : 0.06f);
                    }
                }
            }

            for (int i = 0; i < activeEffects.Length; i++)
                activeEffects[i].AI(this, Owner);

            for (int i = 0; i < activeEffects.Length; i++)
                activeEffects[i].UpdateInjectedEnergy(this, Owner);

            if (Main.rand.NextBool(2))
            {
                Dust trailDust = Dust.NewDustPerfect(
                    Projectile.Center + Main.rand.NextVector2Circular(6f, 6f),
                    DustID.TintableDustLighted,
                    -Projectile.velocity * Main.rand.NextFloat(0.05f, 0.12f),
                    100,
                    Color.Lerp(MeteorColor, Color.White, Main.rand.NextFloat(0.25f)),
                    Main.rand.NextFloat(0.75f, 1.25f));
                trailDust.noGravity = true;
            }

            Projectile.rotation += Projectile.velocity.X * 0.04f + 0.22f * System.Math.Sign(Projectile.velocity.X == 0f ? 1f : Projectile.velocity.X);
        }

        private Vector2 FindNextBounceDirection(int currentReflectiveId)
        {
            int reflectiveType = ModContent.ProjectileType<LeonidReflectiveMeteor>();
            float closestDist = 999999f;
            Vector2 bestDir = Projectile.velocity.SafeNormalize(Vector2.UnitX);

            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile p = Main.projectile[i];
                if (p.active && p.type == reflectiveType && i != currentReflectiveId && p.ModProjectile is LeonidReflectiveMeteor refM)
                {
                    if (refM.TimesBounced < 7)
                    {
                        float dist = Vector2.Distance(Projectile.Center, p.Center);
                        if (dist < closestDist)
                        {
                            closestDist = dist;
                            bestDir = (p.Center - Projectile.Center).SafeNormalize(Vector2.UnitY);
                        }
                    }
                }
            }

            if (closestDist > 700f)
            {
                NPC target = FindClosestNPC(800f);
                if (target != null)
                {
                    bestDir = (target.Center - Projectile.Center).SafeNormalize(Vector2.UnitY);
                }
            }

            return bestDir;
        }

        private Projectile FindClosestReflectiveMeteor(float range)
        {
            int reflectiveType = ModContent.ProjectileType<LeonidReflectiveMeteor>();
            Projectile target = null;
            float sqrRange = range * range;

            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile p = Main.projectile[i];
                if (p.active && p.type == reflectiveType && p.ModProjectile is LeonidReflectiveMeteor refM)
                {
                    if (refM.TimesBounced < 7)
                    {
                        float sqrDistance = Vector2.DistanceSquared(p.Center, Projectile.Center);
                        if (sqrDistance < sqrRange)
                        {
                            sqrRange = sqrDistance;
                            target = p;
                        }
                    }
                }
            }

            return target;
        }

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            // Apply Kinetic Cycle and bounce bonus damage
            float multiplier = 1f + BounceCount * 0.04f + (BounceCount / 3) * 0.15f;
            if (IsOrbiting)
            {
                multiplier *= 0.5f;
            }
            modifiers.SourceDamage *= multiplier;

            for (int i = 0; i < activeEffects.Length; i++)
                activeEffects[i].ModifyHitNPC(this, Owner, target, ref modifiers);
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (BounceCount >= 7)
            {
                // Regain 3 ultimate energy
                Owner.GetModPlayer<LeonidProgenitorPlayer>().AddUltimateEnergy(3);
            }

            for (int i = 0; i < activeEffects.Length; i++)
                activeEffects[i].OnHitNPC(this, Owner, target, hit, damageDone);
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            if (IsOrbiting)
                return false;

            bool shouldDie = true;
            for (int i = 0; i < activeEffects.Length; i++)
            {
                if (!activeEffects[i].OnTileCollide(this, Owner, oldVelocity))
                    shouldDie = false;
            }

            return shouldDie;
        }

        public override void OnKill(int timeLeft)
        {
            SoundEngine.PlaySound(SoundID.Item89 with { Volume = 0.85f, Pitch = -0.08f }, Projectile.Center);
            LeonidVisualUtils.SpawnDustBurst(Projectile.Center, MeteorColor, FromStealthRain ? 26 : 18, FromStealthRain ? 6.0f : 4.5f, FromStealthRain ? 2.4f : 1.8f);

            for (int i = 0; i < activeEffects.Length; i++)
                activeEffects[i].OnKill(this, Owner, timeLeft);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = ModContent.Request<Texture2D>(Texture).Value;
            Texture2D glow = ModContent.Request<Texture2D>("CalamityMod/Items/Weapons/Rogue/LeonidProgenitorGlow").Value;
            Texture2D bloomTex = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;

            float opacity = 1f - Projectile.alpha / 255f;
            Vector2 origin = texture.Size() * 0.5f;

            // Round halo trail: a meteor should read as a soft, dense body rather than a sharp magic spark.
            LeonidVisualUtils.BeginAdditiveSpriteBatch();

            for (int i = 1; i < Projectile.oldPos.Length; i++)
            {
                if (Projectile.oldPos[i] == Vector2.Zero)
                    continue;

                float t = 1f - i / (float)Projectile.oldPos.Length;
                Vector2 trailPos = Projectile.oldPos[i] + Projectile.Size * 0.5f - Main.screenPosition;

                float trailScale = 0.026f + t * 0.04f;
                Main.EntitySpriteDraw(bloomTex, trailPos, null, MeteorColor * (0.22f * t * opacity),
                    0f, bloomTex.Size() * 0.5f, trailScale, SpriteEffects.None, 0);

                if (i % 3 == 0)
                    Main.EntitySpriteDraw(bloomTex, trailPos, null, Color.White * (0.07f * t * opacity),
                        0f, bloomTex.Size() * 0.5f, trailScale * 0.48f, SpriteEffects.None, 0);
            }

            // Soft concentric halos keep the meteor's silhouette round.
            Vector2 headPos = Projectile.Center - Main.screenPosition;
            Main.EntitySpriteDraw(bloomTex, headPos, null, MeteorColor * (0.42f * opacity),
                0f, bloomTex.Size() * 0.5f, 0.14f * Projectile.scale, SpriteEffects.None, 0);
            Main.EntitySpriteDraw(bloomTex, headPos, null, Color.White * (0.2f * opacity),
                0f, bloomTex.Size() * 0.5f, 0.065f * Projectile.scale, SpriteEffects.None, 0);
            Main.EntitySpriteDraw(glow, headPos, null, Color.White * opacity,
                Projectile.rotation, glow.Size() * 0.5f, Projectile.scale, SpriteEffects.None, 0f);

            for (int i = 0; i < activeEffects.Length; i++)
                activeEffects[i].DrawInjectedEnergy(this, Owner, Main.spriteBatch);

            LeonidVisualUtils.BeginAlphaBlendSpriteBatch();

            // Afterimage texture trail
            Color outlineColor = new Color(130, 100, 255) * opacity;
            for (int i = 0; i < Projectile.oldPos.Length; i++)
            {
                Vector2 oldDrawPos = Projectile.oldPos[i] + Projectile.Size * 0.5f - Main.screenPosition;
                float t = 1f - i / (float)Projectile.oldPos.Length;
                Main.EntitySpriteDraw(texture, oldDrawPos, null, MeteorColor * t * 0.25f, Projectile.rotation, origin, Projectile.scale, SpriteEffects.None, 0f);
            }

            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            Color drawColor = MeteorColor * opacity;

            // Outline
            for (int i = 0; i < 8; i++)
            {
                Vector2 offset = (i * MathHelper.TwoPi / 8f).ToRotationVector2() * 1.8f;
                Main.EntitySpriteDraw(texture, drawPosition + offset, null, outlineColor * 0.55f, Projectile.rotation, origin, Projectile.scale, SpriteEffects.None, 0f);
            }

            Main.EntitySpriteDraw(texture, drawPosition, null, drawColor, Projectile.rotation, origin, Projectile.scale, SpriteEffects.None, 0f);

            for (int i = 0; i < activeEffects.Length; i++)
                activeEffects[i].PostDraw(this, Owner, Main.spriteBatch);

            return false;
        }

        public void DisableGravity()
        {
            IgnoreGravity = true;
        }

        public void EnableSimpleHoming(float strength, float range)
        {
            EnableHoming = true;
            HomingStrength = System.Math.Max(HomingStrength, strength);
            HomingRange = System.Math.Max(HomingRange, range);
        }

        public bool HasFlag(string key) => effectFlags.Contains(key);
        public void SetFlag(string key) => effectFlags.Add(key);
        public float GetState(string key) => effectStates.TryGetValue(key, out float value) ? value : 0f;
        public void SetState(string key, float value) => effectStates[key] = value;

        public NPC FindClosestNPC(float range)
        {
            NPC target = null;
            float sqrRange = range * range;

            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!npc.CanBeChasedBy(Projectile))
                    continue;

                float sqrDistance = Vector2.DistanceSquared(npc.Center, Projectile.Center);
                if (sqrDistance > sqrRange)
                    continue;

                sqrRange = sqrDistance;
                target = npc;
            }

            return target;
        }
    }
}
