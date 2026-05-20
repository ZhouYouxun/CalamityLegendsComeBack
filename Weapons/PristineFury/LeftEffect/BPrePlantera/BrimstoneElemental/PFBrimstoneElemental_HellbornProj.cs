using CalamityMod;
using CalamityMod.Dusts;
using CalamityMod.Particles;
using CalamityMod.Projectiles.Typeless;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.PristineFury.LeftEffect
{
    internal sealed class PFBrimstoneElemental_HellbornProj : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.PristineFury";
        public override string Texture => "CalamityMod/Projectiles/Ranged/HellbornProj";

        public override void SetStaticDefaults() => Main.projFrames[Type] = 6;

        public override void SetDefaults()
        {
            Projectile.width = 38;
            Projectile.height = 38;
            Projectile.friendly = true;
            Projectile.ignoreWater = true;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 600;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.extraUpdates = 2;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 10;
        }

        public override void AI()
        {
            float ownerDistance = Vector2.Distance(Main.player[Projectile.owner].Center, Projectile.Center);
            Projectile.frameCounter += Projectile.frame == 0 || Projectile.frame == 3 ? 1 : 2;
            if (Projectile.frameCounter > 20)
            {
                Projectile.frame++;
                Projectile.frameCounter = 0;
            }
            if (Projectile.frame > 5)
                Projectile.frame = 0;

            if (Projectile.velocity.Length() < 18f)
                Projectile.velocity *= 1.035f;

            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.ToRadians(90f);
            Lighting.AddLight(Projectile.Center, Color.Orange.ToVector3());

            if (ownerDistance < 1400f)
            {
                float sine = (float)Math.Sin(Projectile.timeLeft * 0.575f / Math.PI);
                Vector2 offset = Projectile.velocity.SafeNormalize(Vector2.UnitX).RotatedBy(MathHelper.PiOver2) * sine * 16f;
                SpawnSquashDust(Projectile.Center + offset);
                SpawnSquashDust(Projectile.Center - offset);
            }

            Dust diamond = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(30f, 30f), ModContent.DustType<DiamondDust>(), -Projectile.velocity * Main.rand.NextFloat(0.1f, 0.4f));
            diamond.noGravity = true;
            diamond.scale = Main.rand.NextFloat(0.62f, 0.82f);
            diamond.color = Color.Lerp(Color.Orange, Color.Red, Main.rand.NextFloat());
            diamond.fadeIn = 1f;
            diamond.noLight = true;
        }

        private void SpawnSquashDust(Vector2 position)
        {
            if (!Main.rand.NextBool(2))
                return;

            Dust dust = Dust.NewDustPerfect(position, ModContent.DustType<SquashDust>(), -Projectile.velocity.SafeNormalize(Vector2.UnitX) * Main.rand.NextFloat(3f, 5f));
            dust.noGravity = true;
            dust.scale = Main.rand.NextFloat(2.3f, 2.7f);
            dust.color = Color.Lerp(Color.Orange, Color.Red, Main.rand.NextFloat());
            dust.fadeIn = 2.5f;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            Vector2 direction = Projectile.Center.DirectionTo(target.Center);
            target.MoveNPC(direction, 6f, ignoreKBImmune: true);
            MakeBlast(target.whoAmI, hitTarget: true);
        }

        public override void OnKill(int timeLeft)
        {
            if (!Main.dedServ)
            {
                for (int i = 0; i < 17; i++)
                {
                    Dust orange = Dust.NewDustPerfect(Projectile.Center, 278, new Vector2(8f, 8f).RotatedByRandom(100f) * Main.rand.NextFloat(0.1f, 0.8f));
                    orange.noGravity = false;
                    orange.scale = Main.rand.NextFloat(0.62f, 0.82f);
                    orange.color = Color.Lerp(Color.Orange, Color.Red, Main.rand.NextFloat());

                    Dust squash = Dust.NewDustPerfect(Projectile.Center, ModContent.DustType<SquashDust>(), new Vector2(10f, 10f).RotatedByRandom(100f) * Main.rand.NextFloat(0.1f, 0.8f));
                    squash.noGravity = true;
                    squash.scale = Main.rand.NextFloat(2.1f, 2.4f);
                    squash.color = Color.Lerp(Color.Orange, Color.Red, Main.rand.NextFloat());
                    squash.fadeIn = 1.7f;
                }

                GeneralParticleHandler.SpawnParticle(new CustomPulse(Projectile.Center, Vector2.Zero, Color.OrangeRed * 0.7f, "CalamityMod/Particles/BloomCircle", Vector2.One, Main.rand.NextFloat(-10f, 10f), 1.2f, 1.7f, 16));
                GeneralParticleHandler.SpawnParticle(new CustomPulse(Projectile.Center, Vector2.Zero, Color.White * 0.7f, "CalamityMod/Particles/BloomCircle", Vector2.One, Main.rand.NextFloat(-10f, 10f), 0.7f, 0.9f, 16));
            }

            SoundEngine.PlaySound(new SoundStyle("CalamityMod/Sounds/Item/HellbornImpact") { PitchVariance = 0.15f, Volume = 0.8f }, Projectile.Center);
            if (Projectile.numHits == 0)
                MakeBlast(0, hitTarget: false);
        }

        public void MakeBlast(int target, bool hitTarget)
        {
            float radius = 70f;
            float knockback = 0.25f;
            int iFrames = 5;
            int damage = (int)(Projectile.damage * 0.33f);
            if (hitTarget)
            {
                Projectile blast = Projectile.NewProjectileDirect(Projectile.GetSource_FromThis(), Projectile.Center, Vector2.Zero, ModContent.ProjectileType<BasicBurstExclusive>(), damage, Projectile.knockBack, Projectile.owner, radius, knockback, iFrames);
                blast.timeLeft = 2;
                blast.DamageType = DamageClass.Ranged;
                blast.localAI[0] = target;
            }
            else
            {
                Projectile blast = Projectile.NewProjectileDirect(Projectile.GetSource_FromThis(), Projectile.Center, Vector2.Zero, ModContent.ProjectileType<BasicBurst>(), damage, Projectile.knockBack, Projectile.owner, radius, knockback, iFrames);
                blast.timeLeft = 2;
                blast.DamageType = DamageClass.Ranged;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D value = ModContent.Request<Texture2D>(Texture, AssetRequestMode.ImmediateLoad).Value;
            Rectangle frame = value.Frame(1, 6, 0, Projectile.frame);
            Vector2 origin = frame.Size() * 0.5f;
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle", AssetRequestMode.ImmediateLoad).Value;
            float randomScale = Main.rand.NextFloat(0.8f, 1.2f);
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;

            Main.EntitySpriteDraw(bloom, drawPosition, null, Color.OrangeRed with { A = 0 }, Projectile.rotation, bloom.Size() * 0.5f, 0.5f * randomScale, SpriteEffects.None, 0);
            Main.EntitySpriteDraw(bloom, drawPosition, null, (Color.White with { A = 0 }) * 0.75f, Projectile.rotation, bloom.Size() * 0.5f, 0.35f * randomScale, SpriteEffects.None, 0);
            Main.EntitySpriteDraw(value, drawPosition, frame, Color.White, 0f, origin, Projectile.scale * 1.15f, SpriteEffects.None, 0);
            return false;
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) => CalamityUtils.CircularHitboxCollision(Projectile.Center, 35f, targetHitbox);
    }
}
