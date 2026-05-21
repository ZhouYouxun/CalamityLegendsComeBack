using CalamityMod;
using CalamityMod.Buffs.DamageOverTime;
using CalamityMod.Particles;
using CalamityMod.Projectiles.Typeless;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.PristineFury.LeftEffect
{
    internal sealed class PFGoliath_HiveNukeMissile : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.PristineFury";
        public override string Texture => "CalamityLegendsComeBack/Weapons/PristineFury/LeftEffect/CPreMoodLord/Goliath/HiveNuke";

        private const int HomingDelay = 34;
        private const float HomingSpeed = 21.5f;
        private bool hasHit;
        private ref float Time => ref Projectile.localAI[0];
        private Vector2 TargetPoint => new(Projectile.ai[0], Projectile.ai[1]);

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 20;
            ProjectileID.Sets.TrailingMode[Type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 38;
            Projectile.friendly = true;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 360;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.extraUpdates = 3;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
            Projectile.scale = 1.35f;
        }

        public override void AI()
        {
            Time++;
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
            Color theme = PFLeftEffectRules.GetThemeColor(Projectile, new Color(139, 242, 73));

            if (Time > HomingDelay)
            {
                Vector2 desiredDirection = (TargetPoint - Projectile.Center).SafeNormalize(Projectile.velocity.SafeNormalize(Vector2.UnitY));
                Projectile.velocity = Vector2.Lerp(Projectile.velocity, desiredDirection * HomingSpeed, 0.075f);
                if (Projectile.Distance(TargetPoint) < 24f)
                    Projectile.Kill();
            }

            if (Projectile.timeLeft <= 30)
                Projectile.velocity *= 0.95f;

            if (!Main.dedServ)
            {
                Projectile.alpha = (int)Utils.Remap(Projectile.timeLeft, 10f, 0f, 0f, 255f, true);
                if (Time % 3f == 0f)
                {
                    Dust dust = Dust.NewDustDirect(Projectile.Center, Projectile.width, Projectile.height, Main.rand.NextBool() ? DustID.GreenTorch : 303, 0f, 0f, 0, theme, Main.rand.NextFloat(0.3f, 0.6f));
                    dust.noGravity = true;
                    dust.velocity = -Projectile.velocity.RotatedByRandom(0.5f) * Main.rand.NextFloat(0.2f, 0.8f);
                }

                if (Time > 5f)
                {
                    GeneralParticleHandler.SpawnParticle(new HeavySmokeParticle(
                        Projectile.Center - Projectile.velocity * 2f,
                        -Projectile.velocity * Main.rand.NextFloat(0.2f, 0.6f),
                        Color.Lerp(Color.Black, theme, 0.25f) * 0.65f,
                        9,
                        Main.rand.NextFloat(0.45f, 0.6f),
                        0.23f,
                        Main.rand.NextFloat(-0.2f, 0.2f)));
                }
            }

            Lighting.AddLight(Projectile.Center, theme.ToVector3() * 0.7f);
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) => CalamityUtils.CircularHitboxCollision(Projectile.Center, Projectile.width * Projectile.scale * 0.42f, targetHitbox);

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            hasHit = true;
            return true;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            hasHit = true;
            target.AddBuff(ModContent.BuffType<Plague>(), 120);
        }

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            if (Projectile.numHits > 1)
                Projectile.damage = (int)(Projectile.damage * 0.7f);
            if (Projectile.damage < 1)
                Projectile.damage = 1;
        }

        public override void OnKill(int timeLeft)
        {
            if (!hasHit)
            {
                SpawnMissDust();
                return;
            }

            Color theme = PFLeftEffectRules.GetThemeColor(Projectile, new Color(139, 242, 73));
            SoundEngine.PlaySound(new SoundStyle("CalamityMod/Sounds/Item/TheHiveNuke") { Volume = 0.78f }, Projectile.Center);
            Main.player[Projectile.owner].Calamity().GeneralScreenShakePower = System.Math.Max(Main.player[Projectile.owner].Calamity().GeneralScreenShakePower, 6.5f);

            Projectile.ExpandHitboxBy(120);
            Projectile.damage = (int)(Projectile.damage * 0.7f);
            Projectile.penetrate = -1;
            Projectile.Damage();

            if (!Main.dedServ)
            {
                for (int i = 0; i < 3; i++)
                {
                    GeneralParticleHandler.SpawnParticle(new CustomPulse(Projectile.Center, Vector2.Zero, theme * 0.8f, "CalamityMod/Particles/FlameExplosion", Vector2.One, Main.rand.NextFloat(-10f, 10f), 0.06f, 0.55f + i * 0.16f, 20 + 4 * i));
                }

                for (int i = 0; i < 30; i++)
                {
                    GeneralParticleHandler.SpawnParticle(new GlowOrbParticle(Projectile.Center, new Vector2(30f, 30f).RotatedByRandom(100f) * Main.rand.NextFloat(0.05f, 0.8f), false, 60, Main.rand.NextFloat(0.8f, 1.4f), theme, true, true));
                }

                for (int i = 0; i < 45; i++)
                {
                    Dust dust = Dust.NewDustPerfect(Projectile.Center, Main.rand.NextBool(5) ? DustID.GreenTorch : 303, new Vector2(35f, 35f).RotatedByRandom(100f) * Main.rand.NextFloat(0.05f, 0.8f), 0, Main.rand.NextBool(3) ? theme : Color.Black, Main.rand.NextFloat(0.85f, 1.7f));
                    dust.noGravity = Main.rand.NextBool(4);
                    dust.alpha = dust.color == Color.Black ? Main.rand.Next(90, 221) : 0;
                }
            }

            for (int i = 0; i < 18; i++)
            {
                int bee = Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, new Vector2(10f, 10f).RotatedByRandom(100f) * Main.rand.NextFloat(0.2f, 0.8f), ModContent.ProjectileType<BasicPlagueBee>(), (int)(Projectile.damage * 0.04f), 0f, Projectile.owner);
                if (bee >= 0 && bee < Main.maxProjectiles)
                {
                    Main.projectile[bee].penetrate = 1;
                    Main.projectile[bee].DamageType = DamageClass.Ranged;
                }
            }
        }

        private void SpawnMissDust()
        {
            if (Main.dedServ)
                return;

            Color theme = PFLeftEffectRules.GetThemeColor(Projectile, new Color(139, 242, 73));
            for (int i = 0; i <= 15; i++)
            {
                Dust dust = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(10f, 10f) - Projectile.velocity * 3.5f, DustID.GreenTorch, Projectile.velocity.RotatedByRandom(0.3f) * Main.rand.NextFloat(0.4f, 0.9f), 0, theme, Main.rand.NextFloat(0.5f, 0.9f));
                dust.noGravity = false;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D afterimage = ModContent.Request<Texture2D>("CalamityMod/Projectiles/StarProj").Value;
            if (Time > 6f)
                CalamityUtils.DrawAfterimagesCentered(Projectile, ProjectileID.Sets.TrailingMode[Type], PFLeftEffectRules.GetThemeColor(Projectile, new Color(139, 242, 73)) * 0.4f, 1, afterimage);

            Texture2D value = TextureAssets.Projectile[Type].Value;
            Main.EntitySpriteDraw(value, Projectile.Center - Main.screenPosition, null, Projectile.GetAlpha(lightColor), Projectile.rotation, value.Size() / 2f, Projectile.scale, SpriteEffects.None, 0f);
            return false;
        }
    }
}
