using System;
using CalamityMod;
using CalamityMod.Buffs.StatDebuffs;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Upgrade.AntiMaterielRifle.Proj
{
    internal sealed class AMRMetalJetShard : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.AntiMaterielRifle";
        public override string Texture => "CalamityMod/Projectiles/Ranged/AMRShot";

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 10;
            ProjectileID.Sets.TrailingMode[Type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = 12;
            Projectile.height = 12;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = true;
            Projectile.penetrate = 1;
            Projectile.extraUpdates = 3;
            Projectile.timeLeft = 126;
            Projectile.scale = 0.9f;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }

        public override bool? CanDamage()
        {
            return Projectile.ai[0] <= 0 ? null : false;
        }

        public override void AI()
        {
            bool finalUpdate = CalamityUtils.FinalExtraUpdate(Projectile);
            if (Projectile.ai[0] > 0f && finalUpdate)
                Projectile.ai[0]--;

            Projectile.velocity *= 0.98f;

            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;

            if (!Main.dedServ && finalUpdate)
            {
                Vector2 backward = -Projectile.velocity.SafeNormalize(Vector2.UnitX);
                Color metalColor = Main.rand.NextBool()
                    ? Color.Lerp(new Color(25, 22, 18), Color.Gold, Main.rand.NextFloat(0.35f, 0.85f))
                    : Color.Lerp(Color.Gold, new Color(180, 80, 20), Main.rand.NextFloat(0.2f, 0.65f));

                GeneralParticleHandler.SpawnParticle(new SparkParticle(
                    Projectile.Center,
                    backward.RotatedByRandom(0.16f) * Main.rand.NextFloat(1.2f, 2.8f),
                    false,
                    Main.rand.Next(18, 27),
                    Main.rand.NextFloat(0.72f, 1.08f),
                    metalColor));

                GeneralParticleHandler.SpawnParticle(new GlowOrbParticle(
                    Projectile.Center,
                    backward * 0.4f,
                    false,
                    10,
                    Main.rand.NextFloat(0.24f, 0.34f),
                    metalColor,
                    true,
                    false,
                    true));
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            SpawnMetalImpact(target.Center);

            if (AMRBalance.TryGetMaxLifeTrueDamage(target, out int finalTrueDamage))
            {
                hit.HideCombatText = false;
                target.life -= finalTrueDamage;
                CombatText.NewText(target.getRect(), new Color(255, 140, 40), finalTrueDamage, true);

                if (target.life <= 0)
                    target.checkDead();
            }

            if (AMRBalance.DeathMarkUnlocked)
            {
                target.AddBuff(ModContent.BuffType<MarkedforDeath>(), 5 * 60);
                int defenseLoss = Math.Max(25, (int)(target.defense * 0.6f));
                target.Calamity().miscDefenseLoss = Math.Max(target.Calamity().miscDefenseLoss, defenseLoss);
            }
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            SpawnMetalImpact(Projectile.Center);
            Collision.HitTiles(Projectile.position, oldVelocity, Projectile.width, Projectile.height);
            SoundEngine.PlaySound(SoundID.NPCHit4 with { Volume = 0.35f, Pitch = 0.18f }, Projectile.Center);
            return true;
        }

        private void SpawnMetalImpact(Vector2 impactPoint)
        {
            if (Main.dedServ)
                return;

            Vector2 forward = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            Vector2 splatterDirection = -forward;

            GeneralParticleHandler.SpawnParticle(new ImpactParticle(
                impactPoint,
                Main.rand.NextFloat(-0.18f, 0.18f),
                20,
                0.68f,
                Color.Lerp(new Color(25, 22, 18), Color.Gold, 0.65f)));

            for (int i = 0; i < 8; i++)
            {
                int sparkLifetime = Main.rand.Next(20, 35);
                float sparkScale = Main.rand.NextFloat(0.85f, 1.25f);
                if (Main.rand.NextBool(10))
                    sparkScale *= 2f;

                Color sparkColor = Color.Lerp(new Color(25, 22, 18), Color.Gold, Main.rand.NextFloat(0.7f));
                sparkColor = Color.Lerp(sparkColor, new Color(180, 80, 20), Main.rand.NextFloat());

                Vector2 sparkVelocity = splatterDirection.RotatedByRandom(0.8f) * Main.rand.NextFloat(10f, 22f);
                sparkVelocity.Y -= Main.rand.NextFloat(3.5f, 7.5f);

                GeneralParticleHandler.SpawnParticle(new SparkParticle(
                    impactPoint,
                    sparkVelocity,
                    true,
                    sparkLifetime,
                    sparkScale,
                    sparkColor));
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Type].Value;
            Vector2 origin = texture.Size() * 0.5f;

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, Main.DefaultSamplerState,
                DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);

            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Vector2 drawPos = Projectile.Center - Main.screenPosition;
            Main.EntitySpriteDraw(bloom, drawPos, null, new Color(255, 180, 50, 0), 0f,
                bloom.Size() * 0.5f, 0.12f * Projectile.scale, SpriteEffects.None, 0f);

            Main.EntitySpriteDraw(texture, drawPos, null, new Color(255, 230, 160, 0),
                Projectile.rotation, origin, new Vector2(0.8f, 1.4f) * Projectile.scale, SpriteEffects.None, 0f);

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState,
                DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);

            return false;
        }
    }
}
