using CalamityLegendsComeBack.Weapons.Vesuvius.LeftClick.CStage2;
using CalamityMod;
using CalamityMod.Graphics.Metaballs;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.Vesuvius.RightClick
{
    public class VesuviusFaultJavelin : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.Vesuvius";
        public override string Texture => "CalamityLegendsComeBack/Weapons/Vesuvius/NewVesuvius";

        private int Stage => (int)MathHelper.Clamp(Projectile.ai[0], 1f, 5f);

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 8;
            ProjectileID.Sets.TrailingMode[Type] = 0;
        }

        public override void SetDefaults()
        {
            Projectile.width = 62;
            Projectile.height = 62;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.tileCollide = true;
            Projectile.ignoreWater = true;
            Projectile.penetrate = 10;
            Projectile.timeLeft = 240;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 12;
        }

        public override void AI()
        {
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver4;
            Lighting.AddLight(Projectile.Center, 0.65f, 0.18f, 0.04f);

            if (Projectile.localAI[0]++ < 5f)
                Projectile.tileCollide = false;
            else
                Projectile.tileCollide = true;

            if (!Main.dedServ)
                VesuviusVolcanicVisuals.SpawnTravelMix(Projectile.Center, -Projectile.velocity * Main.rand.NextFloat(0.05f, 0.14f), 0.9f + Stage * 0.08f, Stage >= 4);

            if (Stage >= 4 && Projectile.owner == Main.myPlayer && Projectile.localAI[0] % 8f == 0f)
            {
                Vector2 fallVelocity = new Vector2(Projectile.velocity.X * 0.08f, Main.rand.NextFloat(5f, 8f));
                Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    Projectile.Center + Main.rand.NextVector2Circular(30f, 18f) - Vector2.UnitY * Main.rand.NextFloat(30f, 70f),
                    fallVelocity,
                    ModContent.ProjectileType<VesuviusPyroclasticFlow>(),
                    Math.Max(1, (int)(Projectile.damage * 0.22f)),
                    Projectile.knockBack * 0.25f,
                    Projectile.owner,
                    Math.Sign(Projectile.velocity.X == 0f ? 1f : Projectile.velocity.X));
            }
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            SpawnFaultCore(oldVelocity);
            Projectile.Kill();
            return false;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.OnFire3, 240);
            SpawnSmallImpact(target.Center);

            if (Stage >= 5 && Projectile.ai[1] == 0f && Projectile.owner == Main.myPlayer)
            {
                Projectile.ai[1] = 1f;
                Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitX * Math.Sign(target.Center.X - Main.player[Projectile.owner].Center.X));
                Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    target.Center,
                    direction * 8f + Vector2.UnitY * 2f,
                    ModContent.ProjectileType<VesuviusSubductionZone>(),
                    Math.Max(1, (int)(Projectile.damage * 1.85f)),
                    Projectile.knockBack,
                    Projectile.owner,
                    direction.X >= 0f ? 1f : -1f);

                Projectile.Kill();
            }
        }

        private void SpawnFaultCore(Vector2 oldVelocity)
        {
            if (Projectile.owner == Main.myPlayer)
            {
                Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    Projectile.Center,
                    Vector2.Zero,
                    ModContent.ProjectileType<VesuviusFaultCore>(),
                    Math.Max(1, (int)(Projectile.damage * 0.72f)),
                    Projectile.knockBack,
                    Projectile.owner,
                    Stage,
                    oldVelocity.ToRotation());
            }

            SpawnSmallImpact(Projectile.Center);
        }

        private void SpawnSmallImpact(Vector2 center)
        {
            if (Main.dedServ)
                return;

            SoundEngine.PlaySound(SoundID.Item89 with { Volume = 0.55f, Pitch = -0.16f }, center);
            for (int i = 0; i < 10; i++)
            {
                Dust dust = Dust.NewDustPerfect(center, Main.rand.NextBool(3) ? DustID.Torch : DustID.Smoke, Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(2f, 6f), 100, Color.OrangeRed, Main.rand.NextFloat(0.8f, 1.4f));
                dust.noGravity = Main.rand.NextBool();
            }

            RancorLavaMetaball.SpawnParticle(center + Main.rand.NextVector2Circular(6f, 6f), Main.rand.NextFloat(24f, 42f));
            VesuviusVolcanicVisuals.SpawnImpactMix(center, 0.72f + Stage * 0.1f);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            CalamityUtils.DrawAfterimagesCentered(Projectile, ProjectileID.Sets.TrailingMode[Type], Color.OrangeRed * 0.52f);
            Texture2D texture = TextureAssets.Projectile[Type].Value;
            Texture2D glow = ModContent.Request<Texture2D>("CalamityLegendsComeBack/Weapons/Vesuvius/NewVesuviusGlow").Value;
            Vector2 origin = texture.Size() * 0.5f;
            SpriteEffects effects = Projectile.spriteDirection < 0 ? SpriteEffects.FlipVertically : SpriteEffects.None;

            Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, null, lightColor, Projectile.rotation, origin, Projectile.scale, effects);
            Main.EntitySpriteDraw(glow, Projectile.Center - Main.screenPosition, null, Color.White, Projectile.rotation, origin, Projectile.scale, effects);
            return false;
        }
    }

    public class VesuviusFaultCore : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.Vesuvius";
        public override string Texture => "CalamityLegendsComeBack/Weapons/Vesuvius/NewVesuvius";

        private int Stage => (int)MathHelper.Clamp(Projectile.ai[0], 1f, 5f);

        public override void SetDefaults()
        {
            Projectile.width = 70;
            Projectile.height = 70;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 360;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 22;
        }

        public override bool ShouldUpdatePosition() => false;

        public override void AI()
        {
            Projectile.localAI[0]++;
            Projectile.rotation = Projectile.ai[1] + MathHelper.PiOver4;
            Lighting.AddLight(Projectile.Center, 0.75f, 0.24f, 0.05f);

            if (!Main.dedServ)
            {
                if (Projectile.localAI[0] % 3f == 0f)
                {
                    RancorLavaMetaball.SpawnParticle(
                        Projectile.Center + Main.rand.NextVector2Circular(28f, 18f),
                        Main.rand.NextFloat(20f, 40f));
                }

                if (Projectile.localAI[0] % 2f == 0f)
                    VesuviusVolcanicVisuals.SpawnVentMix(Projectile.Center + Main.rand.NextVector2Circular(24f, 14f), 0.82f + Stage * 0.08f, Stage >= 4);

                if (Projectile.localAI[0] % Math.Max(12, 26 - Stage * 2) == 0f)
                    VesuviusVolcanicVisuals.SpawnHeatPulse(Projectile.Center, 0.72f + Stage * 0.1f);
            }

            if (Stage >= 2)
                TryFireTurretShot();
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.OnFire3, 180);
            if (Projectile.owner == Main.myPlayer)
            {
                Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    target.Center,
                    Vector2.Zero,
                    ModContent.ProjectileType<VesuviusLingeringLava>(),
                    Math.Max(1, (int)(Projectile.damage * 0.36f)),
                    0f,
                    Projectile.owner,
                    58f);
            }
        }

        private void TryFireTurretShot()
        {
            int interval = Stage >= 3 ? 18 : 30;
            if (Projectile.localAI[0] % interval != 0f || Projectile.owner != Main.myPlayer)
                return;

            NPC target = FindTarget(780f);
            Vector2 direction = target != null
                ? Projectile.SafeDirectionTo(target.Center + target.velocity * 10f)
                : Vector2.UnitY.RotatedBy(Main.rand.NextFloat(-0.65f, 0.65f));

            float speed = Stage >= 3 ? 15.5f : 12f;
            int damage = Stage >= 3 ? (int)(Projectile.damage * 0.55f) : (int)(Projectile.damage * 0.38f);
            float knockback = Stage >= 3 ? Projectile.knockBack * 1.3f : Projectile.knockBack;

            Projectile.NewProjectile(
                Projectile.GetSource_FromThis(),
                Projectile.Center + direction * 42f,
                direction * speed,
                ModContent.ProjectileType<VesuviusFaultFireball>(),
                Math.Max(1, damage),
                knockback,
                Projectile.owner,
                Stage);

            SoundEngine.PlaySound(SoundID.Item20 with { Volume = 0.44f, Pitch = Stage >= 3 ? 0.1f : -0.1f }, Projectile.Center);
            if (!Main.dedServ)
                VesuviusVolcanicVisuals.SpawnImpactMix(Projectile.Center + direction * 42f, 0.44f + Stage * 0.06f);
        }

        private NPC FindTarget(float range)
        {
            NPC bestTarget = null;
            float bestDistance = range;
            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!npc.CanBeChasedBy(Projectile))
                    continue;

                float distance = Vector2.Distance(Projectile.Center, npc.Center);
                if (distance < bestDistance && Collision.CanHitLine(Projectile.Center, 1, 1, npc.Center, 1, 1))
                {
                    bestDistance = distance;
                    bestTarget = npc;
                }
            }

            return bestTarget;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Type].Value;
            Texture2D glow = ModContent.Request<Texture2D>("CalamityLegendsComeBack/Weapons/Vesuvius/NewVesuviusGlow").Value;
            Vector2 origin = texture.Size() * 0.5f;
            float pulse = 0.8f + 0.2f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 8f);

            Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, null, lightColor, Projectile.rotation, origin, Projectile.scale, SpriteEffects.None);
            Main.EntitySpriteDraw(glow, Projectile.Center - Main.screenPosition, null, Color.White * pulse, Projectile.rotation, origin, Projectile.scale, SpriteEffects.None);
            return false;
        }
    }

    public class VesuviusFaultFireball : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.Vesuvius";
        public override string Texture => "CalamityMod/Projectiles/Melee/VolcanicFireball";

        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = 4;
            ProjectileID.Sets.TrailCacheLength[Type] = 6;
            ProjectileID.Sets.TrailingMode[Type] = 0;
        }

        public override void SetDefaults()
        {
            Projectile.width = 16;
            Projectile.height = 16;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.tileCollide = true;
            Projectile.ignoreWater = true;
            Projectile.penetrate = 10;
            Projectile.timeLeft = 300;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 10;
        }

        public override void AI()
        {
            Projectile.rotation = Projectile.velocity.ToRotation();
            Projectile.frameCounter++;
            if (Projectile.frameCounter > 4)
            {
                Projectile.frameCounter = 0;
                Projectile.frame++;
                if (Projectile.frame >= Main.projFrames[Type])
                    Projectile.frame = 0;
            }

            Lighting.AddLight(Projectile.Center, 0.55f, 0.18f, 0f);

            if (!Main.dedServ)
                VesuviusVolcanicVisuals.SpawnTravelMix(Projectile.Center + Projectile.velocity, -Projectile.velocity * 0.08f, 0.74f, false);
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.OnFire3, 180);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            CalamityUtils.DrawAfterimagesCentered(Projectile, ProjectileID.Sets.TrailingMode[Type], lightColor, 1);
            return false;
        }
    }

    public class VesuviusPyroclasticFlow : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.Vesuvius";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetDefaults()
        {
            Projectile.width = 72;
            Projectile.height = 38;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.tileCollide = true;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 120;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 16;
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            Projectile.ai[1] = 1f;
            Projectile.velocity = new Vector2((Projectile.ai[0] == 0f ? 1f : Projectile.ai[0]) * 7.5f, 0f);
            Projectile.tileCollide = false;
            Projectile.netUpdate = true;
            return false;
        }

        public override void AI()
        {
            if (Projectile.ai[1] == 0f)
            {
                Projectile.velocity.Y += 0.35f;
                Projectile.rotation = Projectile.velocity.ToRotation();
            }
            else
            {
                Projectile.velocity.X *= 0.985f;
                Projectile.velocity.Y = 0f;
                Projectile.rotation = 0f;
            }

            if (!Main.dedServ)
            {
                if (Main.rand.NextBool(3))
                {
                    Vector2 lavaPosition = Projectile.Center + Main.rand.NextVector2Circular(Projectile.width * 0.5f, Projectile.height * 0.42f);
                    RancorLavaMetaball.SpawnParticle(lavaPosition, Main.rand.NextFloat(16f, 34f));
                }

                if (Main.rand.NextBool(3))
                {
                    Particle ash = new SquareAshParticle(
                        Projectile.Center + Main.rand.NextVector2Circular(Projectile.width * 0.5f, Projectile.height * 0.5f),
                        new Vector2(-Projectile.velocity.X * 0.1f, -Main.rand.NextFloat(0.4f, 2f)),
                        Main.rand.Next(22, 42),
                        Main.rand.NextFloat(0.45f, 0.9f),
                        Color.Lerp(Color.Gray, Color.OrangeRed, 0.22f));
                    GeneralParticleHandler.SpawnParticle(ash);
                }

                VesuviusVolcanicVisuals.SpawnPyroclasticMix(
                    Projectile.Center + Main.rand.NextVector2Circular(Projectile.width * 0.42f, Projectile.height * 0.38f),
                    Math.Sign(Projectile.velocity.X == 0f ? 1f : Projectile.velocity.X));
            }
        }

        public override bool PreDraw(ref Color lightColor) => false;
    }

    public class VesuviusSubductionZone : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.Vesuvius";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetDefaults()
        {
            Projectile.width = 72;
            Projectile.height = 72;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 105;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 8;
        }

        public override void AI()
        {
            Projectile.localAI[0]++;
            Projectile.velocity.Y += 0.12f;
            Projectile.rotation = Projectile.velocity.ToRotation();
            Lighting.AddLight(Projectile.Center, 1f, 0.24f, 0.04f);

            if (!Main.dedServ)
            {
                Vector2 dir = Projectile.velocity.SafeNormalize(new Vector2(Projectile.ai[0], 0.35f));
                Vector2 start = Projectile.Center - dir * 30f;
                for (int i = 0; i < 3; i++)
                {
                    float along = Main.rand.NextFloat(0f, 260f);
                    Vector2 pos = start + dir * along + Main.rand.NextVector2Circular(18f, 18f);
                    RancorLavaMetaball.SpawnParticle(pos, Main.rand.NextFloat(26f, 54f));
                    VesuviusVolcanicVisuals.SpawnSubductionMix(pos, dir, i == 0);
                }

                if (Projectile.localAI[0] % 12f == 0f)
                    VesuviusVolcanicVisuals.SpawnHeatPulse(Projectile.Center + dir * Main.rand.NextFloat(32f, 190f), 1.15f);
            }
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            Vector2 dir = Projectile.velocity.SafeNormalize(new Vector2(Projectile.ai[0], 0.35f));
            Vector2 start = Projectile.Center - dir * 20f;
            Vector2 end = Projectile.Center + dir * 290f + Vector2.UnitY * 80f;
            float collisionPoint = 0f;
            return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), start, end, 72f, ref collisionPoint);
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.OnFire3, 360);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D pixel = TextureAssets.MagicPixel.Value;
            Vector2 dir = Projectile.velocity.SafeNormalize(new Vector2(Projectile.ai[0], 0.35f));
            Vector2 start = Projectile.Center - dir * 20f;
            Vector2 end = Projectile.Center + dir * 290f + Vector2.UnitY * 80f;
            Vector2 line = end - start;
            float fade = Utils.GetLerpValue(0f, 20f, Projectile.timeLeft, true);

            Main.EntitySpriteDraw(
                pixel,
                start - Main.screenPosition,
                new Rectangle(0, 0, 1, 1),
                new Color(255, 72, 24, 0) * 0.44f * fade,
                line.ToRotation(),
                new Vector2(0f, 0.5f),
                new Vector2(line.Length(), 72f),
                SpriteEffects.None);
            return false;
        }
    }

    internal static class VesuviusVolcanicVisuals
    {
        private static readonly Color LavaColor = new(255, 104, 28);
        private static readonly Color HotColor = new(255, 188, 72);
        private static readonly Color SmokeColor = new(92, 78, 72);

        internal static void SpawnTravelMix(Vector2 position, Vector2 velocity, float intensity, bool allowSmoke)
        {
            if (Main.dedServ)
                return;

            intensity = MathHelper.Clamp(intensity, 0.3f, 1.6f);

            if (Main.rand.NextFloat() < 0.36f * intensity)
                RancorLavaMetaball.SpawnParticle(position + Main.rand.NextVector2Circular(5f, 5f), Main.rand.NextFloat(10f, 19f) * intensity);

            if (Main.rand.NextFloat() < 0.72f * intensity)
            {
                Dust flame = Dust.NewDustPerfect(
                    position + Main.rand.NextVector2Circular(7f, 7f),
                    DustID.InfernoFork,
                    velocity.RotatedByRandom(0.28f) + Main.rand.NextVector2Circular(0.45f, 0.45f),
                    80,
                    Main.rand.NextBool(4) ? HotColor : LavaColor,
                    Main.rand.NextFloat(0.72f, 1.22f) * intensity);
                flame.noGravity = true;
            }

            if (Main.rand.NextFloat() < 0.42f * intensity)
            {
                Particle ember = new GlowOrbParticle(
                    position + Main.rand.NextVector2Circular(8f, 8f),
                    velocity.RotatedByRandom(0.35f) * Main.rand.NextFloat(0.45f, 0.9f),
                    false,
                    Main.rand.Next(8, 15),
                    Main.rand.NextFloat(0.18f, 0.46f) * intensity,
                    Main.rand.NextBool(4) ? Color.White : Color.Lerp(LavaColor, HotColor, Main.rand.NextFloat(0.2f, 0.72f)));
                GeneralParticleHandler.SpawnParticle(ember);
            }

            if (Main.rand.NextFloat() < 0.13f * intensity)
            {
                Particle heatMist = new MediumMistParticle(
                    position,
                    velocity * Main.rand.NextFloat(0.25f, 0.55f),
                    Color.Lerp(LavaColor, HotColor, 0.28f),
                    Color.DarkSlateGray,
                    Main.rand.NextFloat(0.32f, 0.62f) * intensity,
                    Main.rand.Next(74, 126),
                    Main.rand.NextFloat(-0.08f, 0.08f));
                GeneralParticleHandler.SpawnParticle(heatMist);
            }

            if (allowSmoke && Main.rand.NextFloat() < 0.055f * intensity)
                SpawnRareSmoke(position, velocity * 0.4f, intensity * 0.72f, false);
        }

        internal static void SpawnVentMix(Vector2 position, float intensity, bool glowing)
        {
            if (Main.dedServ)
                return;

            Vector2 riseVelocity = -Vector2.UnitY.RotatedByRandom(0.38f) * Main.rand.NextFloat(1.1f, 3.4f);
            SpawnTravelMix(position, riseVelocity, intensity, false);

            if (Main.rand.NextBool(7))
            {
                Particle heatMist = new MediumMistParticle(
                    position,
                    riseVelocity * Main.rand.NextFloat(0.35f, 0.7f),
                    Color.Lerp(LavaColor, HotColor, 0.36f),
                    SmokeColor,
                    Main.rand.NextFloat(0.5f, 0.92f) * intensity,
                    Main.rand.Next(92, 148),
                    Main.rand.NextFloat(-0.075f, 0.075f));
                GeneralParticleHandler.SpawnParticle(heatMist);
            }

            if (Main.rand.NextBool(glowing ? 13 : 18))
                SpawnRareSmoke(position, riseVelocity, intensity * 0.85f, glowing);
        }

        internal static void SpawnPyroclasticMix(Vector2 position, int direction)
        {
            if (Main.dedServ)
                return;

            Vector2 drift = new(-direction * Main.rand.NextFloat(0.45f, 1.65f), -Main.rand.NextFloat(0.55f, 2.7f));

            if (Main.rand.NextBool(2))
            {
                Dust flame = Dust.NewDustPerfect(position, DustID.InfernoFork, drift * Main.rand.NextFloat(0.32f, 0.72f), 110, LavaColor, Main.rand.NextFloat(0.62f, 1.12f));
                flame.noGravity = !Main.rand.NextBool(4);
            }

            if (Main.rand.NextBool(3))
            {
                Particle heatMist = new MediumMistParticle(
                    position,
                    drift,
                    Color.Lerp(LavaColor, SmokeColor, 0.32f),
                    Color.DarkSlateGray,
                    Main.rand.NextFloat(0.54f, 1f),
                    Main.rand.Next(108, 172),
                    Main.rand.NextFloat(-0.055f, 0.055f));
                GeneralParticleHandler.SpawnParticle(heatMist);
            }

            if (Main.rand.NextBool(14))
                SpawnRareSmoke(position, drift, Main.rand.NextFloat(0.52f, 0.82f), false);
        }

        internal static void SpawnSubductionMix(Vector2 position, Vector2 direction, bool allowMist)
        {
            if (Main.dedServ)
                return;

            Vector2 riseVelocity = -Vector2.UnitY.RotatedByRandom(0.5f) * Main.rand.NextFloat(1.4f, 4.6f) - direction * Main.rand.NextFloat(0.1f, 0.8f);

            Dust flame = Dust.NewDustPerfect(
                position,
                DustID.InfernoFork,
                riseVelocity.RotatedByRandom(0.28f),
                90,
                Main.rand.NextBool(3) ? HotColor : LavaColor,
                Main.rand.NextFloat(0.88f, 1.52f));
            flame.noGravity = true;

            if (Main.rand.NextBool(2))
            {
                Particle ember = new GlowOrbParticle(
                    position + Main.rand.NextVector2Circular(9f, 9f),
                    riseVelocity * Main.rand.NextFloat(0.3f, 0.72f),
                    false,
                    Main.rand.Next(10, 18),
                    Main.rand.NextFloat(0.32f, 0.72f),
                    Color.Lerp(LavaColor, Color.White, Main.rand.NextFloat(0.08f, 0.32f)));
                GeneralParticleHandler.SpawnParticle(ember);
            }

            if (allowMist && Main.rand.NextBool(3))
            {
                Particle heatMist = new MediumMistParticle(
                    position,
                    riseVelocity * 0.42f,
                    Color.Lerp(LavaColor, HotColor, 0.28f),
                    Color.DarkSlateGray,
                    Main.rand.NextFloat(0.62f, 1.12f),
                    Main.rand.Next(96, 164),
                    Main.rand.NextFloat(-0.07f, 0.07f));
                GeneralParticleHandler.SpawnParticle(heatMist);
            }

            if (Main.rand.NextBool(26))
                SpawnRareSmoke(position, riseVelocity * 0.7f, Main.rand.NextFloat(0.72f, 1.08f), true);
        }

        internal static void SpawnHeatPulse(Vector2 center, float intensity)
        {
            if (Main.dedServ)
                return;

            GeneralParticleHandler.SpawnParticle(new CustomPulse(
                center,
                Vector2.Zero,
                LavaColor * 0.42f,
                "CalamityMod/Particles/SoftRoundExplosion",
                Vector2.One,
                Main.rand.NextFloat(MathHelper.TwoPi),
                0.015f * intensity,
                0.085f * intensity,
                16));
        }

        internal static void SpawnImpactMix(Vector2 center, float strength)
        {
            if (Main.dedServ)
                return;

            strength = MathHelper.Clamp(strength, 0.3f, 1.8f);
            RancorLavaMetaball.SpawnParticle(center + Main.rand.NextVector2Circular(7f, 7f), Main.rand.NextFloat(26f, 46f) * strength);

            GeneralParticleHandler.SpawnParticle(new CustomPulse(
                center,
                Vector2.Zero,
                LavaColor,
                "CalamityMod/Particles/FlameExplosion",
                Vector2.One,
                Main.rand.NextFloat(-5f, 5f),
                0f,
                0.075f * strength,
                18));
            GeneralParticleHandler.SpawnParticle(new CustomPulse(
                center,
                Vector2.Zero,
                HotColor * 0.52f,
                "CalamityMod/Particles/SoftRoundExplosion",
                Vector2.One,
                Main.rand.NextFloat(MathHelper.TwoPi),
                0.02f,
                0.11f * strength,
                20));

            int burstCount = Math.Max(6, (int)(11f * strength));
            for (int i = 0; i < burstCount; i++)
            {
                Vector2 burstVelocity = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(2.2f, 7.2f) * strength;
                Dust flame = Dust.NewDustPerfect(center, DustID.InfernoFork, burstVelocity, 80, Main.rand.NextBool(3) ? LavaColor : HotColor, Main.rand.NextFloat(0.82f, 1.42f));
                flame.noGravity = true;

                if (i % 3 == 0)
                {
                    Particle ember = new GlowOrbParticle(
                        center,
                        burstVelocity * 0.72f,
                        false,
                        Main.rand.Next(9, 16),
                        Main.rand.NextFloat(0.24f, 0.54f) * strength,
                        Main.rand.NextBool(4) ? Color.White : HotColor);
                    GeneralParticleHandler.SpawnParticle(ember);
                }
            }

            if (strength > 0.7f)
            {
                Particle heatMist = new MediumMistParticle(
                    center,
                    -Vector2.UnitY * Main.rand.NextFloat(0.6f, 1.8f),
                    Color.Lerp(LavaColor, HotColor, 0.24f),
                    SmokeColor,
                    Main.rand.NextFloat(0.72f, 1.12f) * strength,
                    Main.rand.Next(86, 138),
                    Main.rand.NextFloat(-0.07f, 0.07f));
                GeneralParticleHandler.SpawnParticle(heatMist);
            }
        }

        private static void SpawnRareSmoke(Vector2 position, Vector2 velocity, float scale, bool glowing)
        {
            Particle smoke = new HeavySmokeParticle(
                position + Main.rand.NextVector2Circular(6f, 6f),
                velocity + Main.rand.NextVector2Circular(0.45f, 0.45f),
                Color.Lerp(SmokeColor, LavaColor, Main.rand.NextFloat(0.08f, 0.24f)),
                Main.rand.Next(18, 34),
                Main.rand.NextFloat(0.32f, 0.72f) * scale,
                0.56f,
                Main.rand.NextFloat(-0.045f, 0.045f),
                glowing);
            GeneralParticleHandler.SpawnParticle(smoke);
        }
    }
}
