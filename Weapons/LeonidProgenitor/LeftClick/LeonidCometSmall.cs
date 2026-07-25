using CalamityLegendsComeBack.Weapons.LeonidProgenitor.Core;
using CalamityLegendsComeBack.Weapons.LeonidProgenitor.Effects;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
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

        private bool initialized;
        private Vector2 playerOffset;

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
        private bool hitNPC;

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

        public override void AI()
        {
            if (!initialized)
            {
                InitialCenter = Projectile.Center;
                MeteorColor = FromStealthRain
                    ? Color.Lerp(LeonidVisualUtils.StratusBlue, LeonidVisualUtils.MoonWhite, 0.35f)
                    : LeonidVisualUtils.GetCelestialColor((float)Projectile.whoAmI % 1f);
                Projectile.DamageType = Owner.HeldItem.DamageType;
                initialized = true;
            }

            // Reveal alpha gradually
            if (Projectile.alpha > 0)
            {
                Projectile.alpha -= 35;
                if (Projectile.alpha < 0)
                    Projectile.alpha = 0;
            }

            Lighting.AddLight(Projectile.Center, MeteorColor.ToVector3() * 0.6f);

            // Launch delay AI logic for small comets
            if (Projectile.localAI[1] > 0f)
            {
                Projectile.localAI[1]--;

                if (playerOffset == Vector2.Zero)
                    playerOffset = Projectile.Center - Owner.Center;

                Projectile.Center = Owner.Center + playerOffset;

                if (Projectile.localAI[1] <= 0f)
                {
                    SpawnNormalLaunchVFX();
                    SoundEngine.PlaySound(SoundID.Item61 with { Volume = 0.55f, Pitch = 0.1f }, Projectile.Center);
                }
                return;
            }

            // Decrement bounce cooldown
            if (bounceCooldown > 0)
                bounceCooldown--;

            // Orbit state AI logic
            if (IsOrbiting)
            {
                int totalOrbiting = 0;
                int myOrbitIndex = 0;
                int oldestProj = -1;
                int maxTimeLeft = -1;

                for (int i = 0; i < Main.maxProjectiles; i++)
                {
                    Projectile p = Main.projectile[i];
                    if (p.active && p.type == Type && p.owner == Projectile.owner && p.ModProjectile is LeonidCometSmall small)
                    {
                        if (small.IsOrbiting)
                        {
                            if (i == Projectile.whoAmI)
                                myOrbitIndex = totalOrbiting;
                            totalOrbiting++;

                            if (p.timeLeft > maxTimeLeft)
                            {
                                maxTimeLeft = p.timeLeft;
                                oldestProj = i;
                            }
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
                        Color.Lerp(MeteorColor, LeonidVisualUtils.MoonViolet, 0.45f),
                        Main.rand.NextFloat(0.6f, 1f));
                    trailDust.noGravity = true;
                }

                if (Main.rand.NextBool(26))
                {
                    LeonidStarlight.Spawn(
                        Projectile.Center,
                        angle.ToRotationVector2().RotatedBy(MathHelper.PiOver2) * Main.rand.NextFloat(1.4f, 2.8f),
                        Color.Lerp(MeteorColor, LeonidVisualUtils.MoonViolet, 0.4f),
                        LeonidStarlightShape.Mote,
                        0.62f,
                        hoverTime: 46,
                        lifetime: 180,
                        lanceSpeed: 16f);
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
                    Projectile.timeLeft = 360;
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
                            BounceCount++;
                            reflective.TimesBounced++;
                            bounceCooldown = 12;

                            Vector2 moveDir = Projectile.velocity.SafeNormalize(Vector2.UnitX);
                            p.velocity = moveDir * 5f;
                            reflective.decelerateTimer = 15;

                            bool hasRasalas = Owner.GetModPlayer<LeonidConstellationPlayer>().IsUnlocked(LeonidStar.Rasalas);
                            float speed = Projectile.velocity.Length() * (hasRasalas ? 1.12f : 1.05f);

                            SoundEngine.PlaySound(SoundID.Item10 with { Volume = 0.7f, Pitch = 0.3f }, Projectile.Center);

                            LeonidStarlight.Spray(
                                Projectile.Center,
                                -moveDir,
                                3 + BounceCount / 2,
                                Color.Lerp(MeteorColor, LeonidVisualUtils.StarGold, BounceCount / 9f),
                                LeonidStarlightShape.Mote,
                                speed: 4.5f + BounceCount * 0.5f,
                                spread: 1.1f,
                                scale: 0.7f + BounceCount * 0.05f,
                                hoverTime: 16,
                                lifetime: 120,
                                lanceSpeed: 15f);

                            if (BounceCount >= 7)
                            {
                                LeonidStarlight.Ring(Projectile.Center, 10, 26f, LeonidVisualUtils.GetReadyGold(),
                                    LeonidStarlightShape.Shard, speed: 5.5f, scale: 0.85f, hoverTime: 24, lifetime: 170);
                                LeonidStarlight.Spawn(Projectile.Center, Vector2.Zero, LeonidVisualUtils.StarGold,
                                    LeonidStarlightShape.Halo, 1f, 14, 90, 12f, linksToSiblings: false);

                                NPC target = FindClosestNPC(1000f);
                                if (target != null)
                                    Projectile.velocity = (target.Center - Projectile.Center).SafeNormalize(Vector2.UnitY) * speed;
                                else
                                    Projectile.velocity = Projectile.velocity.SafeNormalize(Vector2.UnitY) * speed;

                                orbitTransitionTimer = 5;
                            }
                            else
                            {
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

            if (Main.rand.NextBool(2))
            {
                LeonidVisualUtils.SpawnStratusDust(
                    Projectile.Center + Main.rand.NextVector2Circular(6f, 6f),
                    Projectile.velocity,
                    Main.rand.NextFloat(0.85f, 1.2f));
            }

            if (Main.rand.NextBool(FromStealthRain ? 22 : 40))
            {
                LeonidStarlight.Shed(Projectile.Center, Projectile.velocity, MeteorColor,
                    LeonidStarlightShape.Mote, FromStealthRain ? 0.72f : 0.58f);
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
            bool hasRasalas = Owner.GetModPlayer<LeonidConstellationPlayer>().IsUnlocked(LeonidStar.Rasalas);
            float multiplier = 1f + BounceCount * (hasRasalas ? 0.06f : 0.04f) + (BounceCount / 3) * 0.15f;
            if (IsOrbiting)
            {
                multiplier *= 0.5f;
            }
            modifiers.SourceDamage *= multiplier;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            hitNPC = true;

            if (BounceCount >= 7)
            {
                int energy = Owner.GetModPlayer<LeonidConstellationPlayer>().IsUnlocked(LeonidStar.Chertan) ? 4 : 3;
                Owner.GetModPlayer<LeonidProgenitorPlayer>().AddUltimateEnergy(energy);
            }

            // 无论普通还是潜伏，左键弹幕命中敌人时，在目标处释放 1 发 LeonidAstralEnergy
            if (Projectile.owner == Main.myPlayer)
            {
                Vector2 spawnPos = target.Center;
                Vector2 randVel = Main.rand.NextVector2Circular(6f, 6f);
                if (randVel == Vector2.Zero) randVel = -Vector2.UnitY * 6f;

                Projectile.NewProjectile(
                    Projectile.GetSource_OnHit(target),
                    spawnPos,
                    randVel,
                    ModContent.ProjectileType<LeonidAstralEnergy>(),
                    (int)(Projectile.damage * 0.5f),
                    0f,
                    Projectile.owner
                );
            }
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            if (IsOrbiting)
                return false;

            return true;
        }

        public override void OnKill(int timeLeft)
        {
            SoundEngine.PlaySound(SoundID.Item89 with { Volume = 0.85f, Pitch = -0.08f }, Projectile.Center);
            LeonidVisualUtils.SpawnDustBurst(Projectile.Center, MeteorColor, FromStealthRain ? 26 : 18, FromStealthRain ? 6.0f : 4.5f, FromStealthRain ? 2.4f : 1.8f);
            LeonidVisualUtils.SpawnCelestialPulse(Projectile.Center, Projectile.oldVelocity, MeteorColor, FromStealthRain ? 1.35f : 1f, FromStealthRain ? 24 : 18);
            CLCBLightingBoltsSystem.Spawn_LeonidStarfieldMatrixBurst(Projectile.Center, FromStealthRain ? 1.8f : 1f);

            // 潜伏攻击模式下，若弹幕销毁且未命中敌人，在 OnKill 时向四周每隔 120 度发射 3 发 LeonidAstralEnergy
            if (FromStealthRain && !hitNPC && Projectile.owner == Main.myPlayer)
            {
                float baseAngle = Main.rand.NextFloat(MathHelper.TwoPi);
                float speed = 10f;
                for (int i = 0; i < 3; i++)
                {
                    float angle = baseAngle + i * (MathHelper.TwoPi / 3f);
                    Vector2 vel = angle.ToRotationVector2() * speed;

                    Projectile.NewProjectile(
                        Projectile.GetSource_FromThis(),
                        Projectile.Center,
                        vel,
                        ModContent.ProjectileType<LeonidAstralEnergy>(),
                        (int)(Projectile.damage * 0.5f),
                        0f,
                        Projectile.owner
                    );
                }
            }

            LeonidStarlight.Burst(
                Projectile.Center,
                FromStealthRain ? 9 : 6,
                MeteorColor,
                LeonidStarlightShape.Shard,
                speed: FromStealthRain ? 7.5f : 5.5f,
                scale: FromStealthRain ? 1.05f : 0.8f,
                hoverTime: 24,
                lifetime: 150,
                lanceSpeed: FromStealthRain ? 20f : 16f);

            LeonidStarlight.Burst(
                Projectile.Center,
                FromStealthRain ? 6 : 4,
                Color.Lerp(MeteorColor, LeonidVisualUtils.MoonWhite, 0.4f),
                LeonidStarlightShape.Mote,
                speed: 4f,
                scale: 0.75f,
                hoverTime: 32,
                lifetime: 160);

            if (FromStealthRain)
            {
                LeonidStarlight.Spawn(Projectile.Center, Vector2.Zero, LeonidVisualUtils.StarGold,
                    LeonidStarlightShape.Halo, 0.9f, 12, 80, 12f, linksToSiblings: false);
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = ModContent.Request<Texture2D>(Texture).Value;
            Texture2D glow = ModContent.Request<Texture2D>("CalamityMod/Items/Weapons/Rogue/LeonidProgenitorGlow").Value;
            Texture2D bloomTex = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;

            float launchCharge = !FromStealthRain && (SpawnFlags & GravityFieldFlag) == 0 && Projectile.localAI[1] > 0f
                ? 1f - Utils.GetLerpValue(0f, 10f, Projectile.localAI[1], true)
                : 0f;
            if (launchCharge > 0f)
            {
                LeonidVisualUtils.BeginAdditiveSpriteBatch();
                float chargeScale = MathHelper.Lerp(0.32f, 0.76f, launchCharge);
                LeonidVisualUtils.DrawBloom(Projectile.Center, MeteorColor, chargeScale);
                LeonidVisualUtils.DrawCelestialHead(Projectile.Center, MeteorColor, launchCharge * 0.9f, chargeScale, Main.GlobalTimeWrappedHourly * 2.6f);
                LeonidVisualUtils.DrawSparkle(Projectile.Center, LeonidVisualUtils.MoonWhite, launchCharge * 0.72f, chargeScale * 0.62f, -Main.GlobalTimeWrappedHourly * 3.4f);
                LeonidVisualUtils.BeginAlphaBlendSpriteBatch();
            }

            float opacity = 1f - Projectile.alpha / 255f;
            Vector2 origin = texture.Size() * 0.5f;
            Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitY);
            Color trailColor = Color.Lerp(MeteorColor, LeonidVisualUtils.StratusBlue, 0.32f);

            LeonidVisualUtils.BeginAdditiveSpriteBatch();

            for (int i = 1; i < Projectile.oldPos.Length; i++)
            {
                if (Projectile.oldPos[i] == Vector2.Zero)
                    continue;

                float t = 1f - i / (float)Projectile.oldPos.Length;
                Vector2 trailWorld = Projectile.oldPos[i] + Projectile.Size * 0.5f;
                Vector2 trailPos = trailWorld - Main.screenPosition;

                float trailScale = 0.026f + t * 0.04f;
                Main.EntitySpriteDraw(bloomTex, trailPos, null, trailColor * (0.24f * t * opacity),
                    0f, bloomTex.Size() * 0.5f, trailScale, SpriteEffects.None, 0);

                if (i % 3 == 0)
                    LeonidVisualUtils.DrawSparkle(trailWorld, LeonidVisualUtils.MoonWhite, 0.18f * t * opacity, 0.18f + t * 0.12f, Projectile.rotation + i);

                if (i <= 6)
                    LeonidVisualUtils.DrawGlowBlade(trailWorld - direction * 5f, direction, trailColor, 0.12f * t * opacity, 0.045f + t * 0.045f, 0.014f + t * 0.01f);
            }

            Vector2 headPos = Projectile.Center - Main.screenPosition;
            LeonidVisualUtils.DrawGlowBlade(Projectile.Center - direction * 6f, direction, trailColor, 0.38f * opacity, 0.1f * Projectile.scale, 0.026f * Projectile.scale);
            LeonidVisualUtils.DrawCelestialHead(Projectile.Center, MeteorColor, opacity, Projectile.scale * (FromStealthRain ? 1.18f : 1f), Projectile.rotation);
            Main.EntitySpriteDraw(glow, headPos, null, Color.White * opacity,
                Projectile.rotation, glow.Size() * 0.5f, Projectile.scale, SpriteEffects.None, 0f);

            float nsOpacity = (1f - Projectile.alpha / 255f) * 0.44f;
            for (int i = 0; i < 8; i++)
            {
                Vector2 off = (i * MathHelper.TwoPi / 8f).ToRotationVector2() * 2.6f;
                Main.EntitySpriteDraw(texture, headPos + off, null,
                    LeonidVisualUtils.NightSkyBlue * nsOpacity,
                    Projectile.rotation, origin, Projectile.scale, SpriteEffects.None, 0f);
            }

            LeonidVisualUtils.BeginAlphaBlendSpriteBatch();

            LeonidStarlight.DrawStarTrail(
                Projectile.oldPos,
                Projectile.Size,
                Color.Lerp(MeteorColor, LeonidVisualUtils.MoonWhite, 0.45f),
                Color.Lerp(MeteorColor, LeonidVisualUtils.DeepStratusBlue, 0.4f),
                opacity * (FromStealthRain ? 0.62f : 0.46f),
                0.052f * Projectile.scale,
                Projectile.rotation);

            for (int i = 0; i < Projectile.oldPos.Length; i++)
            {
                Vector2 oldDrawPos = Projectile.oldPos[i] + Projectile.Size * 0.5f - Main.screenPosition;
                float t = 1f - i / (float)Projectile.oldPos.Length;
                Main.EntitySpriteDraw(texture, oldDrawPos, null, Color.Lerp(MeteorColor, LeonidVisualUtils.DeepStratusBlue, 0.25f) * t * 0.28f, Projectile.rotation, origin, Projectile.scale, SpriteEffects.None, 0f);
            }

            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            Color drawColor = Color.Lerp(MeteorColor, LeonidVisualUtils.MoonWhite, 0.12f) * opacity;

            Color outlineColor = LeonidVisualUtils.NightSkyBlue * opacity;
            for (int i = 0; i < 8; i++)
            {
                Vector2 offset = (i * MathHelper.TwoPi / 8f).ToRotationVector2() * 2.2f;
                Main.EntitySpriteDraw(texture, drawPosition + offset, null, outlineColor * 0.72f, Projectile.rotation, origin, Projectile.scale, SpriteEffects.None, 0f);
            }

            Main.EntitySpriteDraw(texture, drawPosition, null, drawColor, Projectile.rotation, origin, Projectile.scale, SpriteEffects.None, 0f);

            float twinkle = 0.72f + 0.28f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 5.4f + Projectile.whoAmI);
            LeonidStarlight.DrawFlare(
                Projectile.Center,
                Color.Lerp(MeteorColor, LeonidVisualUtils.MoonWhite, 0.5f),
                opacity * 0.7f * twinkle,
                0.085f * Projectile.scale * (FromStealthRain ? 1.3f : 1f),
                -Projectile.rotation * 0.35f);

            if (IsOrbiting)
            {
                LeonidStarlight.DrawOrbitingStars(
                    Projectile.Center,
                    LeonidVisualUtils.MoonViolet,
                    opacity * 0.5f,
                    0.03f,
                    18f,
                    3,
                    2.4f);
            }

            return false;
        }

        private void SpawnNormalLaunchVFX()
        {
            if (FromStealthRain || (SpawnFlags & GravityFieldFlag) != 0)
                return;

            Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitY);
            LeonidVisualUtils.SpawnBloomBurst(Projectile.Center, MeteorColor, 0.68f, 16);
            LeonidVisualUtils.SpawnCelestialPulse(Projectile.Center, direction, MeteorColor, 0.72f, 16);

            LeonidStarlight.Spray(Projectile.Center, -direction, 5, MeteorColor, LeonidStarlightShape.Mote,
                speed: 5.5f, spread: 0.9f, scale: 0.85f, hoverTime: 20, lifetime: 130, lanceSpeed: 15f);

            for (int i = 0; i < 7; i++)
            {
                Vector2 velocity = -direction * Main.rand.NextFloat(0.5f, 2.2f) + Main.rand.NextVector2Circular(1.25f, 1.25f);
                Dust dust = Dust.NewDustPerfect(
                    Projectile.Center + Main.rand.NextVector2Circular(8f, 8f),
                    DustID.TintableDustLighted,
                    velocity,
                    100,
                    Color.Lerp(MeteorColor, LeonidVisualUtils.MoonWhite, Main.rand.NextFloat(0.3f, 0.7f)),
                    Main.rand.NextFloat(0.72f, 1.08f));
                dust.noGravity = true;
            }
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
