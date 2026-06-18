using System;
using System.Collections.Generic;
using CalamityMod;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.CosmicDischarge
{
    public class CosmicDischargeSwordWave : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.Melee";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private readonly List<Vector2> oldCenters = new();
        private ref float Time => ref Projectile.ai[0];

        public override void SetDefaults()
        {
            Projectile.width = 116;
            Projectile.height = 54;
            Projectile.friendly = true;
            Projectile.penetrate = 5;
            Projectile.timeLeft = 30;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.DamageType = DamageClass.MeleeNoSpeed;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 7;
        }

        public override void AI()
        {
            Time++;
            Projectile.rotation = Projectile.velocity.ToRotation();
            Projectile.velocity *= 0.945f;
            Projectile.Opacity = Utils.GetLerpValue(0f, 4f, Time, true) * Utils.GetLerpValue(0f, 12f, Projectile.timeLeft, true);
            Lighting.AddLight(Projectile.Center, CosmicDischargeCommon.DoGSpecialColor.ToVector3() * 0.48f);

            oldCenters.Insert(0, Projectile.Center);
            if (oldCenters.Count > 8)
                oldCenters.RemoveAt(oldCenters.Count - 1);

            if (Time == 1f)
            {
                SoundEngine.PlaySound(SoundID.Item60 with { Volume = 0.42f, Pitch = 0.28f }, Projectile.Center);
                ApplyScreenShake(3.6f);
            }

            if (!Main.dedServ && Main.rand.NextBool(2))
            {
                Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitX);
                Dust dust = Dust.NewDustPerfect(
                    Projectile.Center + direction.RotatedBy(MathHelper.PiOver2) * Main.rand.NextFloat(-26f, 26f),
                    DustID.PurpleTorch,
                    direction.RotatedByRandom(0.32f) * Main.rand.NextFloat(0.4f, 1.25f),
                    120,
                    CosmicDischargeCommon.RandomDoGColor(),
                    Main.rand.NextFloat(0.9f, 1.25f));
                dust.noGravity = true;

                if (Main.rand.NextBool(3))
                {
                    GeneralParticleHandler.SpawnParticle(new LineParticle(
                        Projectile.Center + Main.rand.NextVector2Circular(34f, 18f),
                        -direction * Main.rand.NextFloat(2.5f, 6.5f),
                        false,
                        Main.rand.Next(12, 18),
                        Main.rand.NextFloat(0.34f, 0.62f),
                        CosmicDischargeCommon.Transparent(CosmicDischargeCommon.DoGSpecialColor) * 0.65f));
                }
            }
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            Vector2 start = Projectile.Center - direction * 18f;
            Vector2 end = Projectile.Center + direction * 118f;
            float collisionPoint = 0f;
            return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), start, end, 42f, ref collisionPoint);
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            CosmicDischargeCommon.ApplyDoGDebuffs(target, 180);
            ApplyScreenShake(4.8f);

            if (!target.boss && target.knockBackResist > 0f)
            {
                Player player = Main.player[Projectile.owner];
                Vector2 pullDir = (player.Center - target.Center).SafeNormalize(Vector2.Zero);
                float dist = Vector2.Distance(player.Center, target.Center);
                if (dist > 100f)
                {
                    float pullSpeed = MathHelper.Clamp(dist / 15f, 8f, 22f);
                    target.velocity = pullDir * pullSpeed;
                    target.netUpdate = true;
                }
            }

            if (Main.dedServ)
                return;

            Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            SoundEngine.PlaySound(new SoundStyle("CalamityMod/Sounds/Item/LanceofDestinyStrong")
            {
                Volume = 0.42f,
                Pitch = 0.16f,
                MaxInstances = 4
            }, target.Center);

            GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(
                target.Center,
                direction,
                CosmicDischargeCommon.Transparent(CosmicDischargeCommon.DoGSpecialColor) * 0.34f,
                Vector2.One,
                direction.ToRotation(),
                0.035f,
                0.22f,
                14));

            for (int i = 0; i < 10; i++)
            {
                Vector2 velocity = direction.RotatedByRandom(0.86f) * Main.rand.NextFloat(2.4f, 8.8f);
                GeneralParticleHandler.SpawnParticle(new SparkParticle(
                    target.Center + Main.rand.NextVector2Circular(14f, 14f),
                    velocity,
                    false,
                    Main.rand.Next(12, 20),
                    Main.rand.NextFloat(0.34f, 0.62f),
                    CosmicDischargeCommon.RandomDoGColor()));
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Vector2 origin = bloom.Size() * 0.5f;
            Vector2 scale = new Vector2(1.65f, 0.32f) * Projectile.Opacity;
            Color outer = CosmicDischargeCommon.Transparent(CosmicDischargeCommon.DoGPurpleColor) * 0.18f * Projectile.Opacity;
            Color inner = CosmicDischargeCommon.Transparent(CosmicDischargeCommon.DoGSpecialColor) * 0.34f * Projectile.Opacity;

            Main.spriteBatch.SetBlendState(BlendState.Additive);

            for (int i = oldCenters.Count - 1; i >= 0; i--)
            {
                float fade = 1f - i / (float)oldCenters.Count;
                Main.EntitySpriteDraw(
                    bloom,
                    oldCenters[i] - Main.screenPosition,
                    null,
                    CosmicDischargeCommon.Transparent(CosmicDischargeCommon.DoGPurpleColor) * 0.12f * fade * Projectile.Opacity,
                    Projectile.rotation,
                    origin,
                    scale * MathHelper.Lerp(0.75f, 1.25f, fade),
                    SpriteEffects.None);
            }

            Main.EntitySpriteDraw(bloom, Projectile.Center - Main.screenPosition, null, outer, Projectile.rotation, origin, scale * 1.45f, SpriteEffects.None);
            Main.EntitySpriteDraw(bloom, Projectile.Center - Main.screenPosition, null, inner, Projectile.rotation, origin, scale, SpriteEffects.None);

            Main.spriteBatch.SetBlendState(BlendState.AlphaBlend);

            return false;
        }

        private void ApplyScreenShake(float power)
        {
            if (Main.dedServ)
                return;

            float distanceFactor = Utils.GetLerpValue(1300f, 120f, Vector2.Distance(Main.LocalPlayer.Center, Projectile.Center), true);
            Main.LocalPlayer.Calamity().GeneralScreenShakePower = Math.Max(Main.LocalPlayer.Calamity().GeneralScreenShakePower, power * distanceFactor);
        }
    }

    public class CosmicDischargeDoGRiftBomb : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.Melee";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private ref float DetonateDelay => ref Projectile.ai[0];
        private ref float Time => ref Projectile.ai[1];
        private bool detonated;

        public override void SetDefaults()
        {
            Projectile.width = 28;
            Projectile.height = 28;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 80;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.DamageType = DamageClass.MeleeNoSpeed;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }

        public override bool ShouldUpdatePosition() => !detonated;

        public override bool? CanDamage() => detonated && Time <= DetonateDelay + 3f;

        public override void AI()
        {
            if (DetonateDelay <= 0f)
                DetonateDelay = 22f;

            Time++;
            Projectile.velocity *= 0.94f;
            Projectile.rotation += 0.08f;
            Lighting.AddLight(Projectile.Center, CosmicDischargeCommon.DoGSpecialColor.ToVector3() * 0.3f);

            if (!detonated && Time >= DetonateDelay)
                Detonate();

            if (detonated)
            {
                Projectile.velocity = Vector2.Zero;
                Projectile.Opacity = Utils.GetLerpValue(DetonateDelay + 15f, DetonateDelay, Time, true);
                if (Time >= DetonateDelay + 16f)
                    Projectile.Kill();
            }
            else if (!Main.dedServ && Main.rand.NextBool(3))
            {
                GeneralParticleHandler.SpawnParticle(new GlowSquareParticle(
                    Projectile.Center + Main.rand.NextVector2Circular(10f, 10f),
                    Main.rand.NextVector2Circular(1.4f, 1.4f),
                    false,
                    12,
                    Main.rand.NextFloat(0.05f, 0.1f),
                    CosmicDischargeCommon.ThreeColorSpark,
                    rotation: Main.rand.NextFloat(0.05f, 0.12f)));
                GeneralParticleHandler.SpawnParticle(new ElectricSpark(
                    Projectile.Center + Main.rand.NextVector2Circular(12f, 12f),
                    Projectile.velocity.RotatedByRandom(0.7f) * -0.35f,
                    CosmicDischargeCommon.DoGCyanColor,
                    CosmicDischargeCommon.DoGFuchsiaColor,
                    0.45f,
                    10,
                    MathHelper.PiOver4,
                    5f));
            }
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            if (!detonated)
                return false;

            Vector2 closest = Vector2.Clamp(targetHitbox.Center.ToVector2(), targetHitbox.TopLeft(), targetHitbox.BottomRight());
            return Vector2.DistanceSquared(closest, Projectile.Center) <= 92f * 92f;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            CosmicDischargeCommon.ApplyDoGDebuffs(target, 240);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Vector2 origin = bloom.Size() * 0.5f;

            Main.spriteBatch.SetBlendState(BlendState.Additive);

            if (!detonated)
            {
                float pulse = 0.7f + 0.18f * MathF.Sin(Time * 0.35f);
                Main.EntitySpriteDraw(
                    bloom,
                    Projectile.Center - Main.screenPosition,
                    null,
                    CosmicDischargeCommon.Transparent(CosmicDischargeCommon.DoGSpecialColor) * 0.32f,
                    Projectile.rotation,
                    origin,
                    0.18f * pulse,
                    SpriteEffects.None);
                Main.spriteBatch.SetBlendState(BlendState.AlphaBlend);
                return false;
            }

            float progress = Utils.GetLerpValue(DetonateDelay, DetonateDelay + 16f, Time, true);
            float fade = Utils.GetLerpValue(DetonateDelay + 16f, DetonateDelay + 4f, Time, true);
            Main.EntitySpriteDraw(
                bloom,
                Projectile.Center - Main.screenPosition,
                null,
                CosmicDischargeCommon.Transparent(CosmicDischargeCommon.DoGFuchsiaColor) * 0.3f * fade,
                0f,
                origin,
                MathHelper.Lerp(0.35f, 1.45f, progress),
                SpriteEffects.None);

            Main.spriteBatch.SetBlendState(BlendState.AlphaBlend);
            return false;
        }

        private void Detonate()
        {
            detonated = true;
            Projectile.Resize(184, 184);
            Projectile.Damage();
            SoundEngine.PlaySound(new SoundStyle("CalamityMod/Sounds/Custom/DevourerRiftOpen") { Volume = 0.42f, Pitch = 0.25f, MaxInstances = 4 }, Projectile.Center);
            ApplyScreenShake(4.4f);

            if (Main.dedServ)
                return;

            GeneralParticleHandler.SpawnParticle(new PulseRing(
                Projectile.Center,
                Vector2.Zero,
                CosmicDischargeCommon.Transparent(CosmicDischargeCommon.DoGSpecialColor) * 0.58f,
                0.05f,
                1.2f,
                18));
            GeneralParticleHandler.SpawnParticle(new StrongBloom(
                Projectile.Center,
                Vector2.Zero,
                CosmicDischargeCommon.Transparent(CosmicDischargeCommon.DoGFuchsiaColor) * 0.42f,
                0.5f,
                16));
            GeneralParticleHandler.SpawnParticle(new DetailedExplosion(
                Projectile.Center,
                Vector2.Zero,
                CosmicDischargeCommon.DoGCyanColor,
                new Vector2(1.2f, 0.8f),
                0f,
                0.15f,
                0.95f,
                16));
            GeneralParticleHandler.SpawnParticle(new DetailedExplosion(
                Projectile.Center,
                Vector2.Zero,
                CosmicDischargeCommon.DoGFuchsiaColor * 0.8f,
                new Vector2(0.8f, 1.25f),
                MathHelper.Pi / 3f,
                0.12f,
                0.78f,
                14));
            CosmicDischargeCommon.SpawnRiftCrackProjectiles(Projectile.GetSource_FromThis(), Projectile.Center, Projectile.owner, 5, 3f, 8f, 14f, 22f);
            CosmicDischargeCommon.SpawnDistortionBurst(Projectile.Center, 6, 3, 38f, 25f);

            for (int i = 0; i < 12; i++)
            {
                Vector2 velocity = (MathHelper.TwoPi * i / 12f).ToRotationVector2() * Main.rand.NextFloat(2.4f, 6.2f);
                GeneralParticleHandler.SpawnParticle(new GlowSparkParticle(
                    Projectile.Center + Main.rand.NextVector2Circular(8f, 8f),
                    velocity,
                    true,
                    Main.rand.Next(12, 20),
                    Main.rand.NextFloat(0.55f, 0.95f),
                    CosmicDischargeCommon.ThreeColorSpark,
                    new Vector2(0.25f, 1.7f),
                    true));
            }
        }

        private void ApplyScreenShake(float power)
        {
            if (Main.dedServ)
                return;

            float distanceFactor = Utils.GetLerpValue(1100f, 100f, Vector2.Distance(Main.LocalPlayer.Center, Projectile.Center), true);
            Main.LocalPlayer.Calamity().GeneralScreenShakePower = Math.Max(Main.LocalPlayer.Calamity().GeneralScreenShakePower, power * distanceFactor);
        }
    }

    public class CosmicDischargeDoGEnergyBolt : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.Melee";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private ref float Time => ref Projectile.ai[0];

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 6;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = 12;
            Projectile.height = 12;
            Projectile.friendly = true;
            Projectile.penetrate = 5;
            Projectile.timeLeft = 150;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.DamageType = DamageClass.MeleeNoSpeed;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 6;
        }

        public override void AI()
        {
            Time++;
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
            Lighting.AddLight(Projectile.Center, CosmicDischargeCommon.DoGSpecialColor.ToVector3() * 0.32f);

            if (Time >= 4f)
            {
                NPC target = FindBestTarget(980f);
                if (target != null)
                {
                    Vector2 desiredVel = Projectile.SafeDirectionTo(target.Center) * 18.5f;
                    Projectile.velocity = Vector2.Lerp(Projectile.velocity, desiredVel, 0.18f);
                }
            }

            if (Main.rand.NextBool(3))
            {
                Dust d = Dust.NewDustPerfect(
                    Projectile.Center,
                    DustID.PurpleTorch,
                    Projectile.velocity * 0.2f,
                    100,
                    CosmicDischargeCommon.RandomDoGColor(false),
                    0.9f
                );
                d.noGravity = true;
            }

            if (!Main.dedServ)
            {
                Vector2 back = -Projectile.velocity.SafeNormalize(Vector2.UnitX);
                GeneralParticleHandler.SpawnParticle(new GlowSparkParticle(
                    Projectile.Center + Main.rand.NextVector2Circular(4f, 4f),
                    back * Main.rand.NextFloat(2f, 5f),
                    false,
                    12,
                    Main.rand.NextFloat(0.38f, 0.7f),
                    CosmicDischargeCommon.ThreeColorSpark,
                    new Vector2(0.2f, 2.1f),
                    true));

                if (Main.rand.NextBool(4))
                    GeneralParticleHandler.SpawnParticle(new BoltParticle(
                        Projectile.Center,
                        back.RotatedByRandom(0.5f) * Main.rand.NextFloat(2f, 6f),
                        false,
                        10,
                        0.45f,
                        CosmicDischargeCommon.DoGCyanColor,
                        new Vector2(0.1f, 3.2f),
                        true,
                        true));
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            CosmicDischargeCommon.ApplyDoGDebuffs(target, 180);
            if (!Main.dedServ)
                CosmicDischargeCommon.SpawnDoGImpact(target.Center, Projectile.velocity, false, false);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Vector2 origin = bloom.Size() * 0.5f;
            Main.spriteBatch.SetBlendState(BlendState.Additive);

            for (int i = 0; i < Projectile.oldPos.Length; i++)
            {
                float factor = 1f - i / (float)Projectile.oldPos.Length;
                Vector2 drawPos = Projectile.oldPos[i] + Projectile.Size * 0.5f - Main.screenPosition;
                Main.EntitySpriteDraw(
                    bloom,
                    drawPos,
                    null,
                    CosmicDischargeCommon.Transparent(CosmicDischargeCommon.DoGPurpleColor) * 0.32f * factor,
                    0f,
                    origin,
                    0.12f * factor,
                    SpriteEffects.None
                );
            }

            Main.EntitySpriteDraw(
                bloom,
                Projectile.Center - Main.screenPosition,
                null,
                CosmicDischargeCommon.DoGWhiteColor * 0.65f,
                0f,
                origin,
                0.16f,
                SpriteEffects.None
            );

            Main.spriteBatch.SetBlendState(BlendState.AlphaBlend);
            return false;
        }

        private NPC FindBestTarget(float maxDistance)
        {
            NPC marked = null;
            NPC normal = null;
            float closestMarked = maxDistance;
            float closestNormal = maxDistance;

            int markDebuff = ModContent.BuffType<CosmicDischargeDoGMarkDebuff>();

            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];
                if (!npc.CanBeChasedBy(Projectile))
                    continue;

                float dist = Projectile.Distance(npc.Center);
                if (npc.HasBuff(markDebuff))
                {
                    if (dist < closestMarked)
                    {
                        closestMarked = dist;
                        marked = npc;
                    }
                }
                else
                {
                    if (dist < closestNormal)
                    {
                        closestNormal = dist;
                        normal = npc;
                    }
                }
            }

            return marked ?? normal;
        }
    }

    public class CosmicDischargeDoGConvergenceExplosion : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.Melee";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private ref float Time => ref Projectile.ai[0];
        private ref float Radius => ref Projectile.ai[1];

        public override void SetDefaults()
        {
            Projectile.width = 220;
            Projectile.height = 220;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 58;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.DamageType = DamageClass.MeleeNoSpeed;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 8;
        }

        public override bool ShouldUpdatePosition() => false;

        public override bool? CanDamage() => Time <= 12f || Projectile.timeLeft % 7 == 0;

        public override void AI()
        {
            Time++;
            if (Radius <= 0f)
                Radius = 130f;

            Projectile.Resize((int)(Radius * 2f), (int)(Radius * 2f));
            Projectile.Opacity = Utils.GetLerpValue(0f, 5f, Time, true) * Utils.GetLerpValue(0f, 18f, Projectile.timeLeft, true);
            Lighting.AddLight(Projectile.Center, CosmicDischargeCommon.DoGSpecialColor.ToVector3() * 0.55f * Projectile.Opacity);

            if (Time == 1f)
            {
                SoundEngine.PlaySound(new SoundStyle("CalamityMod/Sounds/Custom/DoGLaserWallBigAttack") { Volume = 0.48f, Pitch = 0.18f, MaxInstances = 3 }, Projectile.Center);
                CosmicDischargeCommon.SpawnRiftCrackProjectiles(Projectile.GetSource_FromThis(), Projectile.Center, Projectile.owner, 6, 3f, 8f, 14f, 22f);

                if (!Main.dedServ)
                {
                    CosmicDischargeCommon.SpawnDistortionBurst(Projectile.Center, 8, 4, 48f, 30f);
                    CosmicDischargeCommon.SpawnCustomPulse(Projectile.Center, CosmicDischargeCommon.DoGWhiteColor, 0.3f, 2.4f, "CalamityMod/Particles/PlasmaExplosion", 20);
                    GeneralParticleHandler.SpawnParticle(new DetailedExplosion(Projectile.Center, Vector2.Zero, CosmicDischargeCommon.DoGCyanColor, new Vector2(1.1f, 0.75f), 0f, 0.25f, 1.55f, 20));
                    GeneralParticleHandler.SpawnParticle(new DetailedExplosion(Projectile.Center, Vector2.Zero, CosmicDischargeCommon.DoGFuchsiaColor, new Vector2(0.75f, 1.1f), MathHelper.PiOver4, 0.25f, 1.35f, 18));
                    GeneralParticleHandler.SpawnParticle(new StrongBloom(Projectile.Center, Vector2.Zero, CosmicDischargeCommon.DoGWhiteColor, 1.8f, 20));
                }
            }

            if (!Main.dedServ && Main.rand.NextBool(2))
            {
                Vector2 direction = Main.rand.NextVector2CircularEdge(1f, 1f);
                GeneralParticleHandler.SpawnParticle(new StaticGlowLine(
                    Projectile.Center,
                    Projectile.Center + direction * Main.rand.NextFloat(60f, Radius),
                    direction * 0.4f,
                    14,
                    0.07f,
                    0.9f,
                    Main.rand.NextBool() ? CosmicDischargeCommon.DoGCyanColor : CosmicDischargeCommon.DoGFuchsiaColor));
                GeneralParticleHandler.SpawnParticle(new NanoParticle(
                    Projectile.Center + Main.rand.NextVector2Circular(Radius * 0.7f, Radius * 0.7f),
                    Main.rand.NextVector2Circular(2f, 2f),
                    CosmicDischargeCommon.DoGSpecialColor,
                    0.35f,
                    18,
                    emitsLight: true));
            }
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            Vector2 closest = Vector2.Clamp(targetHitbox.Center.ToVector2(), targetHitbox.TopLeft(), targetHitbox.BottomRight());
            float pulseRadius = Radius * MathHelper.Lerp(0.62f, 1f, Utils.GetLerpValue(0f, 12f, Time, true));
            return Vector2.DistanceSquared(closest, Projectile.Center) <= pulseRadius * pulseRadius;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            CosmicDischargeCommon.ApplyDoGDebuffs(target, 300);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Texture2D portal = ModContent.Request<Texture2D>("CalamityMod/Projectiles/Melee/StreamGougePortal").Value;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            Vector2 bloomOrigin = bloom.Size() * 0.5f;
            Vector2 portalOrigin = portal.Size() * 0.5f;
            float pulse = 0.82f + 0.18f * MathF.Sin(Time * 0.55f);
            float scale = Radius / 110f * Projectile.Opacity;
            float rotation = Main.GlobalTimeWrappedHourly * 7.5f + Projectile.identity * 0.18f;

            Main.spriteBatch.SetBlendState(BlendState.Additive);
            Main.EntitySpriteDraw(bloom, drawPosition, null, CosmicDischargeCommon.Transparent(CosmicDischargeCommon.DoGFuchsiaColor) * 0.22f * Projectile.Opacity, 0f, bloomOrigin, scale * 1.35f * pulse, SpriteEffects.None);
            Main.EntitySpriteDraw(portal, drawPosition, null, Color.Black * 0.42f * Projectile.Opacity, rotation, portalOrigin, scale * 0.8f, SpriteEffects.None);
            Main.EntitySpriteDraw(portal, drawPosition, null, CosmicDischargeCommon.Transparent(CosmicDischargeCommon.DoGCyanColor) * 0.55f * Projectile.Opacity, rotation * 0.6f, portalOrigin, scale * 0.8f, SpriteEffects.None);
            Main.EntitySpriteDraw(portal, drawPosition, null, CosmicDischargeCommon.Transparent(CosmicDischargeCommon.DoGFuchsiaColor) * 0.55f * Projectile.Opacity, -rotation * 0.7f, portalOrigin, scale * 0.8f, SpriteEffects.None);
            Main.spriteBatch.SetBlendState(BlendState.AlphaBlend);
            return false;
        }
    }

    public class CosmicDischargeSwitchPortal : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.Melee";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private ref float TargetMode => ref Projectile.ai[0];
        private ref float Time => ref Projectile.localAI[0];
        private Player Owner => Main.player[Projectile.owner];

        public override void SetDefaults()
        {
            Projectile.width = 120;
            Projectile.height = 120;
            Projectile.timeLeft = 18;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.friendly = false;
            Projectile.hostile = false;
            Projectile.penetrate = -1;
        }

        public override bool? CanDamage() => false;
        public override bool ShouldUpdatePosition() => false;

        public override void AI()
        {
            if (!Owner.active || Owner.dead || Owner.HeldItem.type != ModContent.ItemType<NewLegendCosmicDischarge>())
            {
                Projectile.Kill();
                return;
            }

            Time++;
            Projectile.Center = Owner.Top + new Vector2(0f, -14f);
            Owner.itemTime = 2;
            Owner.itemAnimation = 2;

            CosmicDischargeCommon.SpawnSwitchPortalAI(Owner, Projectile.Center, Time, (CosmicDischargeAttackMode)(int)TargetMode);

            if (Time == 1f)
            {
                SoundEngine.PlaySound(new SoundStyle("CalamityMod/Sounds/Item/DemonSwordKillMode") { Volume = 0.78f, Pitch = 0.15f, MaxInstances = 2 }, Owner.Center);
            }

            if (Time == 8f && Main.myPlayer == Projectile.owner)
            {
                Owner.GetModPlayer<CosmicDischargePlayer>().SetAttackMode((CosmicDischargeAttackMode)(int)TargetMode);
                SoundEngine.PlaySound(new SoundStyle("CalamityMod/Sounds/Custom/DevourerRiftOpen") { Volume = 0.45f, Pitch = 0.35f, MaxInstances = 2 }, Owner.Center);
                Projectile.netUpdate = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D portal = ModContent.Request<Texture2D>("CalamityMod/Projectiles/Melee/StreamGougePortal").Value;
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition + Vector2.UnitY * Owner.gfxOffY;
            Vector2 portalOrigin = portal.Size() * 0.5f;
            Vector2 bloomOrigin = bloom.Size() * 0.5f;
            float open = Utils.GetLerpValue(0f, 6f, Time, true);
            float close = Utils.GetLerpValue(18f, 11f, Time, true);
            float scale = MathF.Sin(MathHelper.PiOver2 * MathHelper.Clamp(open * close, 0f, 1f)) * 1.15f;
            float opacity = MathHelper.Clamp(open * close, 0f, 1f);
            float rotation = Main.GlobalTimeWrappedHourly * 8f + Projectile.identity * 1.45f;
            Color modeColor = CosmicDischargeCommon.GetModeColor((CosmicDischargeAttackMode)(int)TargetMode);

            Main.spriteBatch.SetBlendState(BlendState.Additive);
            Main.EntitySpriteDraw(bloom, drawPosition, null, CosmicDischargeCommon.Transparent(modeColor) * 0.25f * opacity, 0f, bloomOrigin, scale * 0.72f, SpriteEffects.None);
            Main.EntitySpriteDraw(portal, drawPosition, null, Color.Black * 0.55f * opacity, rotation, portalOrigin, scale * 1.35f, SpriteEffects.None);
            Main.EntitySpriteDraw(portal, drawPosition, null, CosmicDischargeCommon.Transparent(CosmicDischargeCommon.DoGCyanColor) * 0.9f * opacity, rotation * 0.6f, portalOrigin, scale * 1.35f, SpriteEffects.None);
            Main.EntitySpriteDraw(portal, drawPosition, null, CosmicDischargeCommon.Transparent(CosmicDischargeCommon.DoGFuchsiaColor) * 0.9f * opacity, -rotation * 0.7f, portalOrigin, scale * 1.35f, SpriteEffects.None);
            Main.spriteBatch.SetBlendState(BlendState.AlphaBlend);
            return false;
        }
    }
}
