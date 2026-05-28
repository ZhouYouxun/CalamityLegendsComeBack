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
            {
                if (Main.rand.NextBool(2))
                {
                    Dust dust = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(10f, 10f), DustID.Torch, -Projectile.velocity * Main.rand.NextFloat(0.05f, 0.14f), 100, Color.OrangeRed, Main.rand.NextFloat(0.8f, 1.35f));
                    dust.noGravity = true;
                }

                if (Main.rand.NextBool(4))
                {
                    Particle smoke = new HeavySmokeParticle(
                        Projectile.Center + Main.rand.NextVector2Circular(8f, 8f),
                        -Projectile.velocity * Main.rand.NextFloat(0.04f, 0.11f),
                        Color.Lerp(Color.Gray, Color.OrangeRed, 0.18f),
                        Main.rand.Next(24, 40),
                        Main.rand.NextFloat(0.45f, 0.88f),
                        0.62f,
                        Main.rand.NextFloat(-0.04f, 0.04f),
                        Stage >= 4);
                    GeneralParticleHandler.SpawnParticle(smoke);
                }
            }

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

                if (Main.rand.NextBool(3))
                {
                    Particle smoke = new HeavySmokeParticle(
                        Projectile.Center + Main.rand.NextVector2Circular(32f, 20f),
                        -Vector2.UnitY * Main.rand.NextFloat(1.2f, 3.8f) + Main.rand.NextVector2Circular(0.8f, 0.8f),
                        Color.Lerp(Color.Gray, Color.OrangeRed, 0.16f),
                        Main.rand.Next(26, 46),
                        Main.rand.NextFloat(0.55f, 1.25f),
                        0.72f,
                        Main.rand.NextFloat(-0.04f, 0.04f),
                        Stage >= 4,
                        required: true);
                    GeneralParticleHandler.SpawnParticle(smoke);
                }
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

            if (!Main.dedServ && Main.rand.NextBool(3))
            {
                Dust dust = Dust.NewDustPerfect(Projectile.Center + Projectile.velocity, Main.rand.NextBool(3) ? DustID.Torch : DustID.Smoke, -Projectile.velocity * 0.08f, 100, Color.OrangeRed, Main.rand.NextFloat(0.8f, 1.2f));
                dust.noGravity = true;
            }
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
                if (Main.rand.NextBool(2))
                {
                    Particle smoke = new HeavySmokeParticle(
                        Projectile.Center + Main.rand.NextVector2Circular(Projectile.width * 0.45f, Projectile.height * 0.45f),
                        new Vector2(-Math.Sign(Projectile.velocity.X) * Main.rand.NextFloat(0.4f, 1.8f), -Main.rand.NextFloat(0.4f, 2.8f)),
                        Color.Lerp(Color.DarkGray, Color.OrangeRed, 0.16f),
                        Main.rand.Next(30, 58),
                        Main.rand.NextFloat(0.65f, 1.35f),
                        0.78f,
                        Main.rand.NextFloat(-0.03f, 0.03f),
                        false,
                        required: true);
                    GeneralParticleHandler.SpawnParticle(smoke);
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
                }

                if (Projectile.localAI[0] % 2f == 0f)
                {
                    Particle smoke = new HeavySmokeParticle(
                        Projectile.Center + Main.rand.NextVector2Circular(90f, 42f),
                        -Vector2.UnitY * Main.rand.NextFloat(1.5f, 4.8f),
                        Color.Lerp(Color.DarkGray, Color.OrangeRed, 0.16f),
                        Main.rand.Next(36, 70),
                        Main.rand.NextFloat(0.9f, 1.9f),
                        0.82f,
                        Main.rand.NextFloat(-0.04f, 0.04f),
                        true,
                        required: true);
                    GeneralParticleHandler.SpawnParticle(smoke);
                }
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
}
