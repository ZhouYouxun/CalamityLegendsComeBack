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

                if (Projectile.timeLeft % 4 == 0)
                {
                    Vector2 ringPos = Projectile.Center + Main.rand.NextVector2CircularEdge(104f, 104f) * Main.rand.NextFloat(0.75f, 1.15f);
                    GeneralParticleHandler.SpawnParticle(new CustomPulse(ringPos, Vector2.Zero, BombGold, "CalamityMod/Particles/HighResHollowCircleHardEdge", Vector2.One, Main.rand.NextFloat(-10f, 10f), 0f, Main.rand.NextFloat(0.055f, 0.09f), 15));
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
            owner.Calamity().GeneralScreenShakePower = System.Math.Max(owner.Calamity().GeneralScreenShakePower, 12f);

            SoundStyle boom = new("CalamityMod/Sounds/Item/DeadSunExplosion");
            SoundEngine.PlaySound(boom with { Volume = 1.1f, Pitch = -0.18f, PitchVariance = 0.12f }, Projectile.Center);

            if (Main.dedServ)
                return;

            GeneralParticleHandler.SpawnParticle(new CustomPulse(Projectile.Center, Vector2.Zero, BombWhite, "CalamityMod/Particles/HighResHollowCircleHardEdge", Vector2.One, Main.rand.NextFloat(-10f, 10f), 0f, 0.17f * Projectile.scale, 18));
            GeneralParticleHandler.SpawnParticle(new CustomPulse(Projectile.Center, Vector2.Zero, Color.Lerp(BombGold, Color.White, 0.4f), "CalamityMod/Particles/BloomCircle", Vector2.One, Main.rand.NextFloat(-10f, 10f), 4.1f * Projectile.scale, 0f, 30));

            // Own-style finish: a restrained golden-angle starburst around the unchanged shockwave.
            int rays = 13;
            float goldenAngle = MathHelper.Pi * (3f - System.MathF.Sqrt(5f));
            for (int i = 0; i < rays; i++)
            {
                float angle = goldenAngle * i + Main.rand.NextFloat(-0.035f, 0.035f);
                Vector2 dir = angle.ToRotationVector2();
                Particle ray = new CustomSpark(Projectile.Center, dir * Main.rand.NextFloat(2.3f, 4.4f), "CalamityMod/Particles/BloomLineAngled", false, 20, Main.rand.NextFloat(0.62f, 0.95f), i % 2 == 0 ? BombGold : BombOrange, new Vector2(0.16f, 0.82f), true, false, 0f, false, false, 0.72f);
                GeneralParticleHandler.SpawnParticle(ray);
            }

            // Three exact rotational symmetries expand at different rates: hexagon, octagon, dodecagon.
            int[] polygonSides = { 6, 8, 12 };
            for (int ring = 0; ring < polygonSides.Length; ring++)
            {
                int sides = polygonSides[ring];
                float radius = (38f + ring * 30f) * Projectile.scale;
                float phase = ring * MathHelper.Pi / sides + Projectile.identity * 0.017f;
                for (int vertex = 0; vertex < sides; vertex++)
                {
                    float angle = MathHelper.TwoPi * vertex / sides + phase;
                    Vector2 radial = angle.ToRotationVector2();
                    Vector2 tangent = radial.RotatedBy(MathHelper.PiOver2);
                    Color color = ring == 1 ? BombWhite : Color.Lerp(BombOrange, BombGold, ring * 0.42f);
                    GeneralParticleHandler.SpawnParticle(new CustomSpark(
                        Projectile.Center + radial * radius,
                        radial * (2.2f + ring * 1.15f) + tangent * (ring == 1 ? -1.2f : 1.2f),
                        "CalamityMod/Particles/BloomCircle",
                        false,
                        20 + ring * 4,
                        0.28f + ring * 0.08f,
                        color,
                        new Vector2(0.72f, 1.3f),
                        true));
                }
            }

            // Fermat spiral: sqrt radius spacing keeps the points evenly packed while the
            // golden angle prevents visible radial clumping.
            for (int i = 1; i <= 21; i++)
            {
                float angle = goldenAngle * i;
                float radius = System.MathF.Sqrt(i / 21f) * 118f * Projectile.scale;
                Vector2 radial = angle.ToRotationVector2();
                GeneralParticleHandler.SpawnParticle(new CustomSpark(
                    Projectile.Center + radial * radius,
                    radial.RotatedBy(MathHelper.PiOver2) * (1.4f + radius * 0.012f),
                    "CalamityMod/Particles/BloomCircle",
                    false,
                    24,
                    MathHelper.Lerp(0.22f, 0.48f, i / 21f),
                    Color.Lerp(BombOrange, BombWhite, i / 21f),
                    new Vector2(0.62f, 1.08f),
                    true));
            }

            for (int i = 0; i < 48; i++)
            {
                Vector2 vel = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(5f, 18f);
                Dust dust = Dust.NewDustPerfect(Projectile.Center, DustID.GoldFlame, vel, 0, Main.rand.NextBool(3) ? BombWhite : BombGold, Main.rand.NextFloat(1.25f, 2f));
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
