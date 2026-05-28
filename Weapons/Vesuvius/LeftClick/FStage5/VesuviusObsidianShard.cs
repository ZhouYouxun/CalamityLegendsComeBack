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

namespace CalamityLegendsComeBack.Weapons.Vesuvius.LeftClick.FStage5
{
    public class VesuviusObsidianShard : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.Vesuvius";
        public override string Texture => "CalamityMod/Projectiles/Magic/AsteroidMolten6";

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 12;
            ProjectileID.Sets.TrailingMode[Type] = 0;
        }

        public override void SetDefaults()
        {
            Projectile.width = 34;
            Projectile.height = 34;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.tileCollide = true;
            Projectile.ignoreWater = true;
            Projectile.penetrate = 10;
            Projectile.timeLeft = 180;
            Projectile.extraUpdates = 1;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 6;
        }

        public override void AI()
        {
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
            Projectile.velocity.Y += 0.015f;
            Lighting.AddLight(Projectile.Center, 0.34f, 0.08f, 0.42f);

            if (!Main.dedServ)
            {
                Particle spark = new VoidSparkParticle(
                    Projectile.Center + Main.rand.NextVector2Circular(9f, 9f),
                    -Projectile.velocity * Main.rand.NextFloat(0.04f, 0.11f) + Main.rand.NextVector2Circular(0.4f, 0.4f),
                    false,
                    Main.rand.Next(12, 20),
                    Main.rand.NextFloat(0.42f, 0.78f),
                    Color.Lerp(new Color(60, 28, 95), Color.OrangeRed, 0.3f),
                    0.86f);
                GeneralParticleHandler.SpawnParticle(spark);

                if (Main.rand.NextBool(3))
                {
                    RancorLavaMetaball.SpawnParticle(
                        Projectile.Center + Main.rand.NextVector2Circular(7f, 7f),
                        Main.rand.NextFloat(16f, 28f));
                }
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.OnFire3, 360);

            if (Projectile.owner != Main.myPlayer)
                return;

            for (int i = 0; i < 3; i++)
            {
                Vector2 slashDirection = Projectile.velocity.SafeNormalize(Vector2.UnitX).RotatedBy(MathHelper.ToRadians(-28f + i * 28f));
                Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    target.Center + slashDirection * 42f,
                    slashDirection,
                    ModContent.ProjectileType<VesuviusObsidianSlash>(),
                    Math.Max(1, (int)(Projectile.damage * 0.54f)),
                    Projectile.knockBack * 0.25f,
                    Projectile.owner,
                    i);
            }

            Projectile.NewProjectile(
                Projectile.GetSource_FromThis(),
                target.Center,
                Vector2.Zero,
                ModContent.ProjectileType<VesuviusObsidianBlast>(),
                Math.Max(1, (int)(Projectile.damage * 0.65f)),
                Projectile.knockBack,
                Projectile.owner);
        }

        public override void OnKill(int timeLeft)
        {
            if (Main.dedServ)
                return;

            SoundEngine.PlaySound(SoundID.Item14 with { Volume = 0.55f, Pitch = -0.4f }, Projectile.Center);
            for (int i = 0; i < 16; i++)
            {
                Dust dust = Dust.NewDustPerfect(Projectile.Center, DustID.Obsidian, Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(2f, 8f), 120, Color.Lerp(Color.Black, Color.OrangeRed, 0.35f), Main.rand.NextFloat(0.9f, 1.6f));
                dust.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            CalamityMod.CalamityUtils.DrawAfterimagesCentered(Projectile, ProjectileID.Sets.TrailingMode[Type], new Color(170, 80, 255, 0) * 0.62f);
            Texture2D texture = TextureAssets.Projectile[Type].Value;
            Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, null, Color.Lerp(Color.White, Color.Black, 0.25f), Projectile.rotation, texture.Size() * 0.5f, Projectile.scale * 1.08f, SpriteEffects.None);
            return false;
        }
    }

    public class VesuviusObsidianSlash : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.Vesuvius";
        public override string Texture => "CalamityLegendsComeBack/Weapons/SHPC/Effects/DPreDog/SZPC/BlackSLASH";

        public override void SetDefaults()
        {
            Projectile.width = 160;
            Projectile.height = 76;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 18;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }

        public override bool ShouldUpdatePosition() => false;

        public override bool? CanDamage() => Projectile.timeLeft >= 10;

        public override void AI()
        {
            Projectile.rotation = Projectile.velocity.ToRotation();
            Lighting.AddLight(Projectile.Center, 0.3f, 0.08f, 0.42f);

            if (Projectile.localAI[0] == 0f)
            {
                Projectile.localAI[0] = 1f;
                Projectile.Damage();
                SoundEngine.PlaySound(SoundID.Item71 with { Volume = 0.5f, Pitch = -0.35f }, Projectile.Center);
            }
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            Vector2 start = Projectile.Center - Projectile.velocity.SafeNormalize(Vector2.UnitX) * 90f;
            Vector2 end = Projectile.Center + Projectile.velocity.SafeNormalize(Vector2.UnitX) * 130f;
            float collisionPoint = 0f;
            return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), start, end, 44f, ref collisionPoint);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Type].Value;
            float fade = Utils.GetLerpValue(0f, 10f, Projectile.timeLeft, true);
            Main.EntitySpriteDraw(
                texture,
                Projectile.Center - Main.screenPosition,
                null,
                new Color(210, 92, 255, 0) * fade,
                Projectile.rotation,
                texture.Size() * 0.5f,
                new Vector2(1.4f, 0.72f) * Projectile.scale,
                SpriteEffects.None);
            return false;
        }
    }

    public class VesuviusObsidianBlast : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.Vesuvius";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetDefaults()
        {
            Projectile.width = 150;
            Projectile.height = 150;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 22;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }

        public override bool ShouldUpdatePosition() => false;

        public override bool? CanDamage() => Projectile.timeLeft >= 16;

        public override void AI()
        {
            if (Projectile.localAI[0] == 0f)
            {
                Projectile.localAI[0] = 1f;
                Projectile.Damage();
                SoundEngine.PlaySound(SoundID.Item14 with { Volume = 0.72f, Pitch = -0.45f }, Projectile.Center);
            }

            if (!Main.dedServ && Projectile.timeLeft % 2 == 0)
            {
                RancorLavaMetaball.SpawnParticle(
                    Projectile.Center + Main.rand.NextVector2Circular(50f, 50f),
                    Main.rand.NextFloat(30f, 58f));
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            float expand = Utils.GetLerpValue(22f, 0f, Projectile.timeLeft, true);
            float fade = Utils.GetLerpValue(0f, 12f, Projectile.timeLeft, true);
            Main.EntitySpriteDraw(
                bloom,
                Projectile.Center - Main.screenPosition,
                null,
                new Color(210, 62, 255, 0) * 0.26f * fade,
                0f,
                bloom.Size() * 0.5f,
                0.55f + expand * 1.25f,
                SpriteEffects.None);
            return false;
        }
    }
}
