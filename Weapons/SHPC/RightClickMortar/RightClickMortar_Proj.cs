using CalamityMod;
using CalamityMod.Dusts;
using CalamityMod.Enums;
using CalamityMod.Graphics.Primitives;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Linq;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SHPC.RightClickMortar
{
    internal sealed class RightClickMortar_Proj : ModProjectile, ILocalizedModType, IPixelatedPrimitiveRenderer
    {
        private const int ReachedPeakTime = 90;
        private const int RainDownStartTime = 110;
        private const float FinalDiveSpeed = 18f;
        private const float MouseTargetLockRange = 460f;

        public new string LocalizationCategory => "Projectiles.SHPC";
        public override string Texture => "CalamityLegendsComeBack/Weapons/SHPC/RightClickMortar/RightClickMortar_Proj";

        private ref float SavedMouseX => ref Projectile.ai[0];
        private ref float SavedMouseY => ref Projectile.ai[1];

        private int time;
        private int rainDownTimer = RainDownStartTime;
        private int lockedTargetIndex = -1;
        private float fade = 1f;

        private NPC LockedTarget =>
            Main.npc.IndexInRange(lockedTargetIndex) && Main.npc[lockedTargetIndex].CanBeChasedBy(Projectile, false)
                ? Main.npc[lockedTargetIndex]
                : null;

        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = 4;
            ProjectileID.Sets.TrailingMode[Type] = 2;
            ProjectileID.Sets.TrailCacheLength[Type] = 14;
        }

        public override void SetDefaults()
        {
            Projectile.width = 22;
            Projectile.height = 22;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.extraUpdates = 1;
            Projectile.timeLeft = 700;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 10;
        }

        public override void OnSpawn(IEntitySource source)
        {
            if (SavedMouseX == 0f && SavedMouseY == 0f)
            {
                Player owner = Main.player[Projectile.owner];
                Vector2 mouseWorld = GetOwnerMouseWorld(owner);
                SavedMouseX = mouseWorld.X;
                SavedMouseY = mouseWorld.Y;
            }
        }

        public override void AI()
        {
            Player owner = Main.player[Projectile.owner];
            if (!owner.active || owner.dead)
            {
                Projectile.Kill();
                return;
            }

            UpdateSavedMouseTarget(owner);
            UpdateAnimation();

            Vector2 mouse = GetSavedMouseWorld();
            if (time > ReachedPeakTime)
                UpdateRainDownPhase(owner, mouse);
            else
                Projectile.velocity *= 0.995f;

            Projectile.spriteDirection = Projectile.direction = (Projectile.velocity.X > 0f).ToDirectionInt();
            Projectile.rotation = Projectile.velocity.ToRotation();
            fade = rainDownTimer > 0 ? Utils.GetLerpValue(ReachedPeakTime, ReachedPeakTime * 0.7f, time, true) : 1f;

            SpawnFlightEffects();
            time++;
        }

        private void UpdateRainDownPhase(Player owner, Vector2 mouse)
        {
            EnsureTargetLocked(mouse);
            Vector2 strikePoint = GetStrikePoint(mouse);

            if (rainDownTimer > 1)
                Projectile.Center = new Vector2(strikePoint.X, owner.Center.Y - 620f);

            if (rainDownTimer > 0)
                rainDownTimer--;

            if (rainDownTimer == 65 && Projectile.numUpdates == 0)
                SoundEngine.PlaySound(new SoundStyle("CalamityMod/Sounds/Item/MissileNearing") { Volume = 0.55f, Pitch = 0.35f, MaxInstances = 3 }, Projectile.Center);

            if (rainDownTimer == 1)
            {
                Projectile.extraUpdates = 7;
                Projectile.penetrate = 1;
                Projectile.velocity = (strikePoint - Projectile.Center).SafeNormalize(Vector2.UnitY) * FinalDiveSpeed;
                Projectile.netUpdate = true;
            }

            if (rainDownTimer != 0)
                return;

            if (Projectile.numUpdates == 0 && !Main.dedServ)
            {
                Particle spark = new SparkParticle(Projectile.Center, -Projectile.velocity * 0.05f, false, 19, 1.7f, new Color(120, 235, 255));
                GeneralParticleHandler.SpawnParticle(spark);
            }

            NPC target = LockedTarget;
            Vector2 trackingPoint = target?.Center ?? mouse;
            Vector2 desiredDirection = (trackingPoint - Projectile.Center).SafeNormalize(Vector2.UnitY);

            if (Projectile.velocity.Length() < FinalDiveSpeed)
                Projectile.velocity = Projectile.velocity * 0.96f + desiredDirection * 2.8f;
            else
                Projectile.velocity = Projectile.velocity.ToRotation().AngleTowards(desiredDirection.ToRotation(), 0.045f).ToRotationVector2() * Projectile.velocity.Length() * 0.995f;

            if (Projectile.Center.Y > trackingPoint.Y)
                Projectile.tileCollide = true;

            if (target == null && Vector2.Distance(Projectile.Center, trackingPoint) < 28f)
                Projectile.Kill();
        }

        private void EnsureTargetLocked(Vector2 mouse)
        {
            if (LockedTarget != null)
                return;

            NPC closest = null;
            float bestDistance = MouseTargetLockRange;
            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!npc.CanBeChasedBy(Projectile, false))
                    continue;

                float distance = Vector2.Distance(mouse, npc.Center);
                if (distance >= bestDistance)
                    continue;

                bestDistance = distance;
                closest = npc;
            }

            if (closest == null)
                return;

            lockedTargetIndex = closest.whoAmI;
            Projectile.netUpdate = true;
        }

        private Vector2 GetStrikePoint(Vector2 mouse)
        {
            NPC target = LockedTarget;
            return target?.Center ?? mouse;
        }

        private void UpdateSavedMouseTarget(Player owner)
        {
            if (Projectile.owner != Main.myPlayer || rainDownTimer <= 0 || lockedTargetIndex >= 0 || time % 12 != 0)
                return;

            Vector2 mouseWorld = GetOwnerMouseWorld(owner);
            SavedMouseX = mouseWorld.X;
            SavedMouseY = mouseWorld.Y;
            Projectile.netUpdate = true;
        }

        private Vector2 GetSavedMouseWorld()
        {
            Vector2 savedMouse = new(SavedMouseX, SavedMouseY);
            if (savedMouse != Vector2.Zero)
                return savedMouse;

            Player owner = Main.player[Projectile.owner];
            return GetOwnerMouseWorld(owner);
        }

        private static Vector2 GetOwnerMouseWorld(Player owner)
        {
            Vector2 mouseWorld = owner.Calamity().mouseWorld;
            if (mouseWorld == Vector2.Zero)
                mouseWorld = Main.MouseWorld;

            return mouseWorld;
        }

        private void UpdateAnimation()
        {
            Projectile.frameCounter++;
            if (Projectile.frameCounter < 4)
                return;

            Projectile.frameCounter = 0;
            Projectile.frame = (Projectile.frame + 1) % Main.projFrames[Type];
        }

        private void SpawnFlightEffects()
        {
            if (Main.dedServ)
                return;

            Vector2 back = -Projectile.velocity.SafeNormalize(Vector2.UnitX);
            Color blue = new(78, 194, 255);
            Color cyan = new(172, 246, 255);
            Lighting.AddLight(Projectile.Center, cyan.ToVector3() * 0.42f);

            if (fade <= 0.2f)
                return;

            if (Projectile.numUpdates == 0)
            {
                Dust dust = Dust.NewDustPerfect(Projectile.Center + back * 8f + Main.rand.NextVector2Circular(3f, 3f), DustID.RainbowMk2);
                dust.velocity = back.RotatedByRandom(0.22f) * Main.rand.NextFloat(1.8f, 4.4f);
                dust.color = Color.Lerp(blue, cyan, Main.rand.NextFloat(0.2f, 0.9f));
                dust.scale = Main.rand.NextFloat(0.75f, 1.05f);
                dust.noGravity = true;
            }

            if (Projectile.timeLeft % 3 == 0)
            {
                Particle smoke = new HeavySmokeParticle(
                    Projectile.Center,
                    -Projectile.velocity * Main.rand.NextFloat(0.2f, 0.6f),
                    cyan,
                    Main.rand.Next(40, 61),
                    Main.rand.NextFloat(0.28f, 0.55f),
                    0.5f,
                    Main.rand.NextFloat(-0.2f, 0.2f),
                    Main.rand.NextBool(),
                    required: true);

                GeneralParticleHandler.SpawnParticle(smoke);
            }
        }

        public override void OnKill(int timeLeft)
        {
            int projIndex = Projectile.NewProjectile(
                Projectile.GetSource_FromThis(),
                Projectile.Center,
                Vector2.Zero,
                ModContent.ProjectileType<NewLegendSHPE>(),
                (int)(Projectile.damage * 0.8f),
                Projectile.knockBack,
                Projectile.owner
            );

            Projectile proj = Main.projectile[projIndex];
            proj.width = 375;
            proj.height = 375;



            Vector2 explosionCenter = Projectile.Center;

            int oldWidth = Projectile.width;
            int oldHeight = Projectile.height;
            Projectile.position = Projectile.Center;
            Projectile.width = Projectile.height = 190;
            Projectile.Center = explosionCenter;
            Projectile.penetrate = -1;
            Projectile.damage = Math.Max(1, (int)(Projectile.damage * 0.5f));
            Projectile.Damage();
            Projectile.width = oldWidth;
            Projectile.height = oldHeight;
            Projectile.Center = explosionCenter;

            SoundEngine.PlaySound(NewLegendSHPC.AntiPersonnelMineExplosion, explosionCenter);

            if (Main.myPlayer == Projectile.owner)
                SpawnLaserBarrage(explosionCenter);

            SpawnExplosionEffects(explosionCenter);
        }

        private void SpawnLaserBarrage(Vector2 explosionCenter)
        {
            NPC target = FindLaserTarget(explosionCenter);
            Vector2 targetCenter = target?.Center ?? explosionCenter;
            int laserCount = Main.rand.Next(24, 31);
            int laserDamage = Math.Max(1, (int)(Projectile.damage * 0.38f));

            for (int i = 0; i < laserCount; i++)
            {
                Vector2 spawnPos = targetCenter + new Vector2(Main.rand.NextFloat(-900f, 900f), -Main.rand.NextFloat(560f, 980f));
                Vector2 targetPos = targetCenter + Main.rand.NextVector2Circular(260f, 130f);
                Vector2 velocity = (targetPos - spawnPos).SafeNormalize(Vector2.UnitY) * Main.rand.NextFloat(20f, 28f);

                Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    spawnPos,
                    velocity,
                    ModContent.ProjectileType<RightClickMortar_Lazer>(),
                    laserDamage,
                    Projectile.knockBack * 0.35f,
                    Projectile.owner);
            }
        }

        private NPC FindLaserTarget(Vector2 explosionCenter)
        {
            NPC target = LockedTarget;
            if (target != null)
                return target;

            NPC closest = null;
            float bestDistance = 620f;
            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!npc.CanBeChasedBy(Projectile, false))
                    continue;

                float distance = Vector2.Distance(explosionCenter, npc.Center);
                if (distance >= bestDistance)
                    continue;

                bestDistance = distance;
                closest = npc;
            }

            return closest;
        }

        private void SpawnExplosionEffects(Vector2 explosionCenter)
        {
            if (Main.dedServ)
                return;

            Color deepBlue = new(35, 125, 255);
            Color cyan = new(100, 235, 255);
            Color white = new(235, 255, 255);
            float blastRadiusVisual = 5.6f;

            Particle orb4 = new CustomPulse(explosionCenter, Vector2.Zero, cyan, "CalamityMod/Particles/SoftRoundExplosion", Vector2.One, Main.rand.NextFloat(-10f, 10f), 0f, 0.34f, 19);
            GeneralParticleHandler.SpawnParticle(orb4);
            Particle orb5 = new CustomPulse(explosionCenter, Vector2.Zero, deepBlue, "CalamityMod/Particles/SoftRoundExplosion", Vector2.One, Main.rand.NextFloat(-10f, 10f), 0f, 0.25f, 19);
            GeneralParticleHandler.SpawnParticle(orb5);

            for (int i = 0; i < 3; i++)
            {
                Particle orb = new CustomPulse(explosionCenter, Vector2.Zero, Color.Lerp(Color.Orchid, cyan, 0.45f), "CalamityMod/Particles/SmallBloom", Vector2.One, Main.rand.NextFloat(-10f, 10f), 0f, 2.05f, 15, true);
                GeneralParticleHandler.SpawnParticle(orb);
            }

            for (int i = 0; i < 46; i++)
            {
                Dust dust = Dust.NewDustPerfect(explosionCenter, ModContent.DustType<LightDust>());
                dust.velocity = (MathHelper.TwoPi * i / 46f).ToRotationVector2() * Main.rand.NextFloat(5.5f, 12.5f) * blastRadiusVisual;
                dust.color = Color.Lerp(deepBlue, white, Main.rand.NextFloat(0.25f, 0.92f));
                dust.scale = Main.rand.NextFloat(1.1f, 1.85f);
                dust.noGravity = true;
                dust.noLightEmittence = true;
            }

            for (int i = 0; i < 25; i++)
            {
                if (i < 14)
                {
                    Particle spark = new CustomSpark(
                        explosionCenter,
                        new Vector2(4f, 4f).RotatedByRandom(100f) * blastRadiusVisual * Main.rand.NextFloat(0.35f, 1f),
                        "CalamityMod/Particles/ProvidenceMarkParticle",
                        false,
                        27,
                        Main.rand.NextFloat(2.25f, 2.65f),
                        Color.Lerp(Color.Orchid, Color.White, Main.rand.NextFloat(0f, 0.7f)),
                        new Vector2(1.3f, 0.5f),
                        true,
                        false,
                        0f,
                        false,
                        false,
                        Main.rand.NextFloat(0.35f, 0.4f));

                    GeneralParticleHandler.SpawnParticle(spark);
                }
                else
                {
                    Dust dust = Dust.NewDustPerfect(explosionCenter, DustID.FireworksRGB);
                    dust.velocity = new Vector2(5f, 5f).RotatedByRandom(100f) * blastRadiusVisual * Main.rand.NextFloat(0.4f, 1f);
                    dust.scale = Main.rand.NextFloat(0.8f, 1.15f);
                    dust.color = cyan;
                }
            }

            for (int i = 0; i < 15; i++)
            {
                Vector2 velocity = new Vector2(8f, 8f).RotatedByRandom(100f) * blastRadiusVisual * Main.rand.NextFloat(0.4f, 1f);
                Particle spark = new CustomSpark(explosionCenter, velocity, "CalamityMod/Projectiles/Boss/ProvidenceCrystal", false, 12, 0.9f, white, new Vector2(1.5f, 0.4f), true, false, 0f, false, false, 0.7f);
                GeneralParticleHandler.SpawnParticle(spark);
            }
        }

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            float damageMult = Utils.Remap(Projectile.numHits, 0f, 10f, 1f, 0.35f, true);
            modifiers.SourceDamage *= damageMult * (rainDownTimer <= 0 ? 1f : 0.2f);
        }

        public override bool? CanDamage() => fade <= 0.2f ? false : null;

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            Projectile.Kill();
            return false;
        }

        public void RenderPixelatedPrimitives(SpriteBatch spriteBatch, GeneralDrawLayer layer)
        {
            Vector2[] trailPoints = Projectile.oldPos
                .Where(position => position != Vector2.Zero)
                .Select(position => position + Projectile.Size * 0.5f)
                .ToArray();

            if (trailPoints.Length < 2)
                return;

            if (trailPoints[0] != Projectile.Center)
                trailPoints = new[] { Projectile.Center }.Concat(trailPoints).ToArray();

            GameShaders.Misc["CalamityMod:TrailStreak"].SetShaderTexture(
                ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/Trails/SylvestaffStreak"));

            PrimitiveRenderer.RenderTrail(
                trailPoints,
                new PrimitiveSettings(
                    TrailWidthFunction,
                    TrailColorFunction,
                    (_, _) => Vector2.Zero,
                    true,
                    true,
                    GameShaders.Misc["CalamityMod:TrailStreak"]),
                trailPoints.Length * 2);
        }

        private float TrailWidthFunction(float completion, Vector2 _) =>
            Utils.Remap(completion, 0f, 0.9f, Projectile.scale * 18f, 0f) * fade;

        private Color TrailColorFunction(float completion, Vector2 _)
        {
            Color head = new(118, 240, 255);
            Color body = new(45, 120, 255);
            Color color = Color.Lerp(head, body, completion * 0.82f) * Utils.GetLerpValue(255f, 0f, Projectile.alpha, true) * fade;
            color.A = 0;
            return color * (1f - completion * 0.68f);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Type].Value;
            Texture2D glowTexture = ModContent.Request<Texture2D>("CalamityLegendsComeBack/Weapons/SHPC/RightClickMortar/RightClickMortar_Proj_Glow").Value;
            Rectangle frame = texture.Frame(1, Main.projFrames[Type], 0, Projectile.frame);
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            Vector2 origin = frame.Size() * 0.5f;
            float rotation = Projectile.rotation + MathHelper.PiOver2;

            Color backglowColor = new Color(58, 190, 255, 0) * 0.72f * fade;
            for (int i = 0; i < 8; i++)
            {
                Vector2 offset = (MathHelper.TwoPi * i / 8f).ToRotationVector2() * 4f;
                Main.EntitySpriteDraw(texture, drawPosition + offset, frame, backglowColor, rotation, origin, Projectile.scale, SpriteEffects.None, 0);
            }

            Main.EntitySpriteDraw(texture, drawPosition, frame, Projectile.GetAlpha(lightColor) * fade, rotation, origin, Projectile.scale, SpriteEffects.None, 0);
            Main.EntitySpriteDraw(glowTexture, drawPosition, frame, Color.White * fade, rotation, origin, Projectile.scale, SpriteEffects.None, 0);
            return false;
        }
    }
}
