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

        private const int FuseFrames = 58;

        public new string LocalizationCategory => "Projectiles.YharimsCrystal";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private bool Exploding
        {
            get => Projectile.ai[0] == 1f;
            set => Projectile.ai[0] = value ? 1f : 0f;
        }

        public override void SetStaticDefaults()
        {
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
            CalamityUtils.CircularHitboxCollision(Projectile.Center, Exploding ? 132f : 18f, targetHitbox);

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

                if (Projectile.timeLeft % 4 == 0)
                {
                    Vector2 ringPos = Projectile.Center + Main.rand.NextVector2CircularEdge(80f, 80f) * Main.rand.NextFloat(0.7f, 1.1f);
                    GeneralParticleHandler.SpawnParticle(new CustomPulse(ringPos, Vector2.Zero, BombGold, "CalamityMod/Particles/HighResHollowCircleHardEdge", Vector2.One, Main.rand.NextFloat(-10f, 10f), 0f, Main.rand.NextFloat(0.04f, 0.07f), 13));
                }
            }
            else
            {
                Projectile.velocity *= 0.988f;
            }

            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
            Lighting.AddLight(Projectile.Center, Color.Lerp(BombOrange, BombGold, 0.4f).ToVector3() * 0.7f);

            if (!Main.dedServ && Projectile.timeLeft % 2 == 0 && (Exploding || Main.rand.NextBool()))
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
            owner.Calamity().GeneralScreenShakePower = System.Math.Max(owner.Calamity().GeneralScreenShakePower, 9f);

            SoundStyle boom = new("CalamityMod/Sounds/Item/DeadSunExplosion");
            SoundEngine.PlaySound(boom with { Volume = 1.1f, Pitch = -0.18f, PitchVariance = 0.12f }, Projectile.Center);

            if (Main.dedServ)
                return;

            GeneralParticleHandler.SpawnParticle(new CustomPulse(Projectile.Center, Vector2.Zero, BombWhite, "CalamityMod/Particles/HighResHollowCircleHardEdge", Vector2.One, Main.rand.NextFloat(-10f, 10f), 0f, 0.13f, 16));
            GeneralParticleHandler.SpawnParticle(new CustomPulse(Projectile.Center, Vector2.Zero, Color.Lerp(BombGold, Color.White, 0.4f), "CalamityMod/Particles/BloomCircle", Vector2.One, Main.rand.NextFloat(-10f, 10f), 3.2f, 0f, 26));

            // Own-style finish: a radial starburst of bloom streaks, distinct from the purple pulse-ring look it replaces.
            int rays = 14;
            for (int i = 0; i < rays; i++)
            {
                float angle = MathHelper.TwoPi * i / rays + Main.rand.NextFloat(-0.05f, 0.05f);
                Vector2 dir = angle.ToRotationVector2();
                Particle ray = new CustomSpark(Projectile.Center, dir * Main.rand.NextFloat(2f, 4f), "CalamityMod/Particles/BloomLineAngled", false, 22, Main.rand.NextFloat(0.9f, 1.3f), i % 2 == 0 ? BombGold : BombOrange, new Vector2(0.18f, 1f), true, false, 0f, false, false, 0.9f);
                GeneralParticleHandler.SpawnParticle(ray);
            }

            for (int i = 0; i < 30; i++)
            {
                Vector2 vel = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(4f, 13f);
                Dust dust = Dust.NewDustPerfect(Projectile.Center, DustID.GoldFlame, vel, 0, Main.rand.NextBool(3) ? BombWhite : BombGold, Main.rand.NextFloat(1.1f, 1.7f));
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

            Texture2D texture = ModContent.Request<Texture2D>("CalamityMod/Particles/Light").Value;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            Vector2 rotationPoint = texture.Size() * 0.5f;

            CalamityUtils.DrawAfterimagesCentered(Projectile, ProjectileID.Sets.TrailingMode[Type], Color.Lerp(BombOrange, BombGold, 0.5f) * 0.55f, 1);
            Main.EntitySpriteDraw(texture, drawPosition, null, BombGold with { A = 0 }, Projectile.rotation, rotationPoint, Projectile.scale * 0.42f, SpriteEffects.None);
            Main.EntitySpriteDraw(texture, drawPosition, null, Color.White with { A = 0 }, Projectile.rotation, rotationPoint, Projectile.scale * 0.22f, SpriteEffects.None);

            return false;
        }
    }
}
