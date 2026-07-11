using CalamityLegendsComeBack.Weapons.YharimsCrystal.Passive;
using CalamityMod;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.YharimsCrystal.RightGeneral
{
    // Shared heavy-attack ordnance for every drone role: a slow, decelerating gold bomb
    // that detonates into a wide radial blast. Not a re-skin of Omicron's grenade —
    // own deceleration curve, own hitbox growth, own starburst-and-shockwave finish.
    internal sealed class YC_GoldBomb : ModProjectile, ILocalizedModType
    {
        private static readonly Color BombGold = new(255, 214, 88);
        private static readonly Color BombOrange = new(255, 111, 34);
        private static readonly Color BombWhite = new(255, 246, 196);

        private const int FuseFrames = 64;
        private const float FlightDamping = 0.98f;

        public new string LocalizationCategory => "Projectiles.YharimsCrystal";
        // The former normal-fire missile silhouette now belongs exclusively to the heavy attack.
        public override string Texture => "CalamityMod/Projectiles/Ranged/ScorpioLargeRocket";

        private bool Exploding
        {
            get => Projectile.ai[0] == 1f;
            set => Projectile.ai[0] = value ? 1f : 0f;
        }

        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = 4;
            ProjectileID.Sets.TrailCacheLength[Type] = 16;
            ProjectileID.Sets.TrailingMode[Type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 22;
            Projectile.friendly = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 420;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.extraUpdates = 5;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }

        public override void OnSpawn(Terraria.DataStructures.IEntitySource source)
        {
            YharimsCrystalHellBladeGlobalProjectile.Mark(Projectile, YCWeaponForm.Crystal);
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) =>
            CalamityUtils.CircularHitboxCollision(Projectile.Center, Exploding ? 168f * Projectile.scale : 20f * Projectile.scale, targetHitbox);

        public override void AI()
        {
            if (Projectile.timeLeft <= FuseFrames)
                Exploding = true;

            if (Exploding)
            {
                Projectile.velocity = Vector2.Zero;
                if (Projectile.timeLeft > FuseFrames)
                    Projectile.timeLeft = FuseFrames;

                if (Projectile.timeLeft == FuseFrames)
                    Detonate();

                if (Projectile.timeLeft % 12 == 0)
                {
                    Vector2 ringPos = Projectile.Center + Main.rand.NextVector2CircularEdge(68f, 68f) * Main.rand.NextFloat(0.72f, 1f);
                    GeneralParticleHandler.SpawnParticle(new CustomPulse(ringPos, Vector2.Zero, BombGold, "CalamityMod/Particles/HighResHollowCircleHardEdge", Vector2.One, Main.rand.NextFloat(-6f, 6f), 0f, Main.rand.NextFloat(0.035f, 0.05f), 10));
                }
            }
            else
            {
                Projectile.velocity *= FlightDamping;
            }

            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
            if (++Projectile.frameCounter >= 24)
            {
                Projectile.frameCounter = 0;
                Projectile.frame = (Projectile.frame + 1) % Main.projFrames[Type];
            }
            Lighting.AddLight(Projectile.Center, Color.Lerp(BombOrange, BombGold, 0.4f).ToVector3() * 0.7f);

            if (!Main.dedServ && Projectile.timeLeft % (Exploding ? 6 : 2) == 0 && (Exploding || Main.rand.NextBool()))
            {
                Vector2 dustVel = new Vector2(4f, 4f).RotatedByRandom(100) * Main.rand.NextFloat(0.1f, 0.8f) * (Exploding ? 1.4f : 1f);
                Dust dust = Dust.NewDustPerfect(Projectile.Center + dustVel * (Exploding ? 5f : 1f), DustID.GoldFlame, dustVel * (Exploding ? 5f : 1f), 0, default, Main.rand.NextFloat(0.9f, 1.2f) * (Exploding ? 1.5f : 1f));
                dust.noGravity = true;
                dust.color = Main.rand.NextBool() ? Color.Lerp(BombGold, Color.White, 0.5f) : BombOrange;
            }

            Projectile.ForceNetUpdate();
        }

        private void Detonate()
        {
            Player owner = Main.player[Projectile.owner];
            owner.Calamity().GeneralScreenShakePower = System.Math.Max(owner.Calamity().GeneralScreenShakePower, 12f);

            SoundStyle boom = new("CalamityMod/Sounds/Item/DeadSunExplosion");
            SoundEngine.PlaySound(boom with { Volume = 1.1f, Pitch = -0.18f, PitchVariance = 0.12f }, Projectile.Center);

            if (Main.dedServ)
                return;

            GeneralParticleHandler.SpawnParticle(new CustomPulse(Projectile.Center, Vector2.Zero, BombWhite, "CalamityMod/Particles/HighResHollowCircleHardEdge", Vector2.One, Main.rand.NextFloat(-6f, 6f), 0f, 0.08f * Projectile.scale, 12));

            // Keep a small directional read without covering the encounter in geometry.
            int rays = 5;
            float goldenAngle = MathHelper.Pi * (3f - System.MathF.Sqrt(5f));
            for (int i = 0; i < rays; i++)
            {
                float angle = goldenAngle * i + Main.rand.NextFloat(-0.035f, 0.035f);
                Vector2 dir = angle.ToRotationVector2();
                Particle ray = new CustomSpark(Projectile.Center, dir * Main.rand.NextFloat(1.6f, 2.8f), "CalamityMod/Particles/BloomLineAngled", false, 14, Main.rand.NextFloat(0.34f, 0.52f), i % 2 == 0 ? BombGold : BombOrange, new Vector2(0.12f, 0.48f), true, false, 0f, false, false, 0.72f);
                GeneralParticleHandler.SpawnParticle(ray);
            }

            for (int i = 0; i < 14; i++)
            {
                Vector2 vel = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(3f, 9f);
                Dust dust = Dust.NewDustPerfect(Projectile.Center, DustID.GoldFlame, vel, 0, Main.rand.NextBool(3) ? BombWhite : BombGold, Main.rand.NextFloat(0.8f, 1.2f));
                dust.noGravity = true;
            }
        }

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            if (!Exploding)
                Exploding = true;

            if (Projectile.numHits > 0)
                modifiers.SourceDamage *= 0.8f;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(new BalanceYharimsCrystal().GetFireDebuffType(), 240);
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            Projectile.tileCollide = false;
            Exploding = true;
            return false;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Exploding)
                return false;

            Texture2D texture = ModContent.Request<Texture2D>(Texture).Value;
            Texture2D glow = ModContent.Request<Texture2D>("CalamityMod/Projectiles/Ranged/ScorpioLargeRocket_Glow").Value;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            Rectangle frame = texture.Frame(1, Main.projFrames[Type], 0, Projectile.frame);
            Vector2 rotationPoint = frame.Size() * 0.5f;

            CalamityUtils.DrawAfterimagesCentered(Projectile, ProjectileID.Sets.TrailingMode[Type], Color.Lerp(BombOrange, BombGold, 0.5f) * 0.55f, 1);
            Main.EntitySpriteDraw(texture, drawPosition, frame, Projectile.GetAlpha(Color.Lerp(lightColor, BombGold, 0.34f)), Projectile.rotation, rotationPoint, Projectile.scale, SpriteEffects.None);
            Main.EntitySpriteDraw(glow, drawPosition, frame, Color.White * Projectile.Opacity, Projectile.rotation, rotationPoint, Projectile.scale, SpriteEffects.None);

            return false;
        }
    }
}
