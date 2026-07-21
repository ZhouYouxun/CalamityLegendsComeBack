using System;
using CalamityLegendsComeBack.Weapons.BlossomFlux.RightClick;
using CalamityMod;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.BlossomFlux.LeftClick
{
    // 轰炸战术呼叫的专属落矛：借亵渎矛的收束后加速节奏，但从外形、粒子到爆点都属于叶流。
    internal sealed class BFBombardCallSpear : ModProjectile, ILocalizedModType
    {
        private const int SettleFrames = 16;
        private const int TileEnableFrames = 70;
        private const float MaxFallSpeed = 24f;

        public new string LocalizationCategory => "Projectiles.BlossomFlux";
        public override string Texture => "CalamityLegendsComeBack/Weapons/BlossomFlux/LeftClick/BFLeafProj";

        private ref float Timer => ref Projectile.localAI[0];
        private bool detonated;

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 10;
            ProjectileID.Sets.TrailingMode[Type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = 18;
            Projectile.height = 18;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 180;
            Projectile.extraUpdates = 1;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }

        public override void OnSpawn(Terraria.DataStructures.IEntitySource source)
        {
            Projectile.noDropItem = true;
            BFArrowCommon.TagBlossomFluxLeftArrow(Projectile);
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.Pi;
        }

        public override void AI()
        {
            Timer++;
            if (Timer <= SettleFrames * Projectile.MaxUpdates)
                Projectile.velocity *= 0.945f;
            else
                Projectile.velocity *= 1.045f;

            if (Projectile.velocity.LengthSquared() > MaxFallSpeed * MaxFallSpeed)
                Projectile.velocity = Projectile.velocity.SafeNormalize(Vector2.UnitY) * MaxFallSpeed;

            if (Timer >= TileEnableFrames * Projectile.MaxUpdates)
                Projectile.tileCollide = true;

            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.Pi;
            Color mainColor = BFArrowCommon.GetPresetColor(BlossomFluxChloroplastPresetType.Chlo_DBomb);
            Lighting.AddLight(Projectile.Center, mainColor.ToVector3() * 0.42f);

            if (Main.dedServ || !Projectile.FinalExtraUpdate() || (int)Timer % 4 != 0)
                return;

            Vector2 backward = -Projectile.velocity.SafeNormalize(Vector2.UnitY);
            GeneralParticleHandler.SpawnParticle(new GlowOrbParticle(
                Projectile.Center + Main.rand.NextVector2Circular(4f, 4f),
                backward * Main.rand.NextFloat(0.7f, 1.8f) + Main.rand.NextVector2Circular(0.2f, 0.2f),
                false,
                Main.rand.Next(8, 13),
                Main.rand.NextFloat(0.12f, 0.2f),
                Color.Lerp(mainColor, Color.Goldenrod, Main.rand.NextFloat(0.2f, 0.65f)),
                true,
                false,
                true));
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) => Detonate(Projectile.Center, 1f);

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            Detonate(Projectile.Center, 0.82f);
            return false;
        }

        public override void OnKill(int timeLeft)
        {
            if (detonated)
                return;

            BFLeafProj.SpawnLeafImpactFX(Projectile, Projectile.Center, BlossomFluxChloroplastPresetType.Chlo_DBomb, 0.58f);
        }

        private void Detonate(Vector2 center, float intensity)
        {
            if (detonated)
                return;

            detonated = true;
            BFLeafProj.SpawnLeafImpactFX(Projectile, center, BlossomFluxChloroplastPresetType.Chlo_DBomb, intensity);
            SoundEngine.PlaySound(BlossomFluxSounds.LeftBombardProjHit, center);

            if (Projectile.owner == Main.myPlayer)
            {
                Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    center,
                    Vector2.Zero,
                    ModContent.ProjectileType<BFLeafBombExplosion>(),
                    Math.Max(1, (int)(Projectile.damage * 0.58f)),
                    Projectile.knockBack * 0.55f,
                    Projectile.owner,
                    68f * Projectile.scale);
            }

            Projectile.Kill();
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D leafTexture = TextureAssets.Projectile[Type].Value;
            Texture2D bloomTexture = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            Color mainColor = BFArrowCommon.GetPresetColor(BlossomFluxChloroplastPresetType.Chlo_DBomb) * Projectile.Opacity;
            Color accentColor = Color.Lerp(mainColor, Color.White, 0.28f);

            Main.spriteBatch.SetBlendState(BlendState.Additive);
            Main.EntitySpriteDraw(bloomTexture, drawPosition, null, mainColor * 0.38f, 0f, bloomTexture.Size() * 0.5f, 0.2f, SpriteEffects.None, 0);
            Main.EntitySpriteDraw(bloomTexture, drawPosition - Projectile.velocity.SafeNormalize(Vector2.UnitY) * 7f, null, accentColor * 0.26f, 0f, bloomTexture.Size() * 0.5f, 0.12f, SpriteEffects.None, 0);
            Main.spriteBatch.SetBlendState(BlendState.AlphaBlend);

            Main.EntitySpriteDraw(
                leafTexture,
                drawPosition,
                null,
                Projectile.GetAlpha(lightColor),
                Projectile.rotation,
                leafTexture.Size() * 0.5f,
                new Vector2(0.76f, 1.28f) * Projectile.scale,
                SpriteEffects.None,
                0);
            return false;
        }
    }
}
