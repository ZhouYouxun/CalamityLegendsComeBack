using CalamityMod;
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
            Projectile.width = 12;
            Projectile.height = 12;
            Projectile.scale = 0.33f;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.tileCollide = true;
            Projectile.ignoreWater = true;
            // No gravity — a weightless void-forged splinter that punches through exactly 2 targets before shattering
            Projectile.penetrate = 2;
            Projectile.timeLeft = 120;
            Projectile.extraUpdates = 2;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 6;
        }

        public override void AI()
        {
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
            Lighting.AddLight(Projectile.Center, 0.03f * VesuviusProjectileVisuals.VisualIntensity, 0.03f * VesuviusProjectileVisuals.VisualIntensity, 0.035f * VesuviusProjectileVisuals.VisualIntensity);

            VesuviusProjectileVisuals.SpawnObsidianTrail(Projectile, 1f);
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.OnFire3, 360);
            VesuviusProjectileVisuals.SpawnObsidianImpact(target.Center, 0.72f);

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
            VesuviusProjectileVisuals.SpawnObsidianImpact(Projectile.Center, 0.95f);
            int dustCount = Math.Max(1, (int)Math.Ceiling(16f * VesuviusProjectileVisuals.VisualIntensity));
            for (int i = 0; i < dustCount; i++)
            {
                Dust dust = Dust.NewDustPerfect(Projectile.Center, DustID.Obsidian, Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(2f, 8f) * VesuviusProjectileVisuals.VisualScale, 140, Main.rand.NextBool(3) ? VesuviusProjectileVisuals.ObsidianBlack : VesuviusProjectileVisuals.ObsidianEdge, Main.rand.NextFloat(0.9f, 1.6f) * VesuviusProjectileVisuals.VisualScale);
                dust.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Type].Value;
            Color edge = VesuviusProjectileVisuals.ObsidianEdge with { A = 0 };

            Main.spriteBatch.SetBlendState(BlendState.Additive);
            for (int i = Projectile.oldPos.Length - 1; i >= 1; i--)
            {
                if (Projectile.oldPos[i] == Vector2.Zero)
                    continue;

                float t = (Projectile.oldPos.Length - i) / (float)Projectile.oldPos.Length;
                Vector2 oldCenter = Projectile.oldPos[i] + Projectile.Size * 0.5f - Main.screenPosition;
                Main.EntitySpriteDraw(texture, oldCenter, null, edge * (0.36f * t * VesuviusProjectileVisuals.VisualIntensity), Projectile.oldRot[i], texture.Size() * 0.5f, Projectile.scale * MathHelper.Lerp(0.72f, 1.18f, t), SpriteEffects.None);
            }

            Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, null, edge * 0.92f * VesuviusProjectileVisuals.VisualIntensity, Projectile.rotation, texture.Size() * 0.5f, Projectile.scale * 1.18f, SpriteEffects.None);
            Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, null, Color.White with { A = 0 } * 0.08f, Projectile.rotation, texture.Size() * 0.5f, Projectile.scale * 0.86f, SpriteEffects.None);
            Main.spriteBatch.SetBlendState(BlendState.AlphaBlend);
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
            Lighting.AddLight(Projectile.Center, 0.025f * VesuviusProjectileVisuals.VisualIntensity, 0.025f * VesuviusProjectileVisuals.VisualIntensity, 0.03f * VesuviusProjectileVisuals.VisualIntensity);

            if (Projectile.localAI[0] == 0f)
            {
                Projectile.localAI[0] = 1f;
                Projectile.Damage();
                SoundEngine.PlaySound(SoundID.Item71 with { Volume = 0.5f, Pitch = -0.35f }, Projectile.Center);
                VesuviusProjectileVisuals.SpawnObsidianImpact(Projectile.Center, 0.55f);
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

            Main.spriteBatch.SetBlendState(BlendState.Additive);
            // BlackSLASH has a black background intended for additive blending.
            // Use A=0 so black pixels contribute nothing; bright slash lines glow additively.
            // Outer dark halo
            Color outerC = Color.Lerp(VesuviusProjectileVisuals.ObsidianEdge, Color.White, 0.28f) * (fade * 0.72f);
            Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, null,
                new Color(outerC.R, outerC.G, outerC.B, 0),
                Projectile.rotation, texture.Size() * 0.5f,
                new Vector2(1.56f, 0.82f) * Projectile.scale, SpriteEffects.None);

            // Inner bright slash core
            Color innerC = Color.White * (fade * 0.42f);
            Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, null,
                new Color(innerC.R, innerC.G, innerC.B, 0),
                Projectile.rotation, texture.Size() * 0.5f,
                new Vector2(1.05f, 0.55f) * Projectile.scale, SpriteEffects.None);

            Main.spriteBatch.SetBlendState(BlendState.AlphaBlend);
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

            if (!Main.dedServ && Projectile.timeLeft % 9 == 0)
                VesuviusProjectileVisuals.SpawnObsidianImpact(Projectile.Center + Main.rand.NextVector2Circular(34f, 34f), 0.42f);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Texture2D smoke = ModContent.Request<Texture2D>("CalamityMod/Particles/HighResFoggyCircleHardEdge").Value;
            float expand = Utils.GetLerpValue(22f, 0f, Projectile.timeLeft, true);
            float fade = Utils.GetLerpValue(0f, 12f, Projectile.timeLeft, true);
            Main.spriteBatch.SetBlendState(BlendState.Additive);
            Main.EntitySpriteDraw(
                smoke,
                Projectile.Center - Main.screenPosition,
                null,
                VesuviusProjectileVisuals.ObsidianEdge * 0.38f * fade * VesuviusProjectileVisuals.VisualIntensity,
                Main.GlobalTimeWrappedHourly * 0.7f,
                smoke.Size() * 0.5f,
                (0.28f + expand * 0.96f) * VesuviusProjectileVisuals.VisualScale,
                SpriteEffects.None);
            Main.EntitySpriteDraw(
                bloom,
                Projectile.Center - Main.screenPosition,
                null,
                VesuviusProjectileVisuals.ObsidianEdge * 0.24f * fade * VesuviusProjectileVisuals.VisualIntensity,
                0f,
                bloom.Size() * 0.5f,
                (0.55f + expand * 1.25f) * VesuviusProjectileVisuals.VisualScale,
                SpriteEffects.None);
            Main.spriteBatch.SetBlendState(BlendState.AlphaBlend);
            return false;
        }
    }
}
