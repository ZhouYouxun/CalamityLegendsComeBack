using CalamityMod;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.BrinyBaron.CommonAttack
{
    // A synchronized, target-bound blue vortex derived from the Nadir singularity's
    // transparent-background visual language. ai[0] is the target NPC; ai[1] is -1
    // for the single attached vortex, otherwise the Baron Helix orbit slot.
    internal sealed class BrinyBaron_JazzTyphoon : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.BrinyBaron";
        // The attached vortex uses the vanilla Razorblade Typhoon's three-frame sprite.
        public override string Texture => $"Terraria/Images/Projectile_{ProjectileID.Typhoon}";

        private const float SingleSlot = -1f;
        private int Age => BB_Balance.JazzTyphoonLifetime - Projectile.timeLeft;
        private bool IsHelixVariant => Projectile.ai[1] >= 0f;
        private float Slot => Projectile.ai[1];
        private float VisualScale =>
            Utils.GetLerpValue(0f, 10f, Age, true) *
            Utils.GetLerpValue(BB_Balance.JazzTyphoonLifetime, BB_Balance.JazzTyphoonLifetime - 18f, Age, true) *
            (IsHelixVariant ? 0.68f : 1f);

        private NPC BoundTarget
        {
            get
            {
                int index = (int)Projectile.ai[0];
                return Main.npc.IndexInRange(index) && Main.npc[index].CanBeChasedBy(Projectile) ? Main.npc[index] : null;
            }
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 90;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = BB_Balance.JazzTyphoonLifetime;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 24;
        }

        public override void SetStaticDefaults() => Main.projFrames[Type] = 3;

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
            => CalamityUtils.CircularHitboxCollision(Projectile.Center, 42f * VisualScale, targetHitbox);

        public override void AI()
        {
            Projectile.frameCounter++;
            Projectile.frame = Projectile.frameCounter / 5 % Main.projFrames[Type];

            NPC target = BoundTarget;
            if (!IsHelixVariant)
            {
                if (target is null)
                {
                    Projectile.Kill();
                    return;
                }

                // The regular Jazz Typhoon remains visibly stuck beneath its victim.
                Projectile.Center = target.Bottom + new Vector2(0f, 10f);
                Projectile.velocity = Vector2.Zero;
            }
            else if (Age < BB_Balance.BaronHelixJazzTyphoonOrbitFrames && target is not null)
            {
                float angle = Age * BB_Balance.BaronHelixJazzTyphoonOrbitAngularVelocity +
                              MathHelper.TwoPi * Slot / 3f;
                Vector2 ellipse = new(
                    (float)System.Math.Cos(angle) * BB_Balance.BaronHelixJazzTyphoonOrbitRadiusX,
                    (float)System.Math.Sin(angle) * BB_Balance.BaronHelixJazzTyphoonOrbitRadiusY);
                Projectile.Center = target.Bottom + ellipse;
                Projectile.velocity = Vector2.Zero;
            }
            else
            {
                NPC homingTarget = target ?? Projectile.Center.ClosestNPCAt(900f);
                if (homingTarget is not null)
                {
                    Vector2 desiredVelocity = Projectile.SafeDirectionTo(homingTarget.Center) * BB_Balance.BaronHelixJazzTyphoonHomingSpeed;
                    Projectile.velocity = Vector2.Lerp(Projectile.velocity, desiredVelocity, BB_Balance.BaronHelixJazzTyphoonHomingInertia);
                }
                else
                {
                    Projectile.velocity *= 0.98f;
                }
            }

            Projectile.rotation += IsHelixVariant ? 0.17f : 0.12f;
            Lighting.AddLight(Projectile.Center, new Vector3(0.08f, 0.56f, 0.92f) * VisualScale);
            EmitVortexParticles();
        }

        private void EmitVortexParticles()
        {
            if (Main.dedServ || Age % 6 != 0 || VisualScale <= 0.08f)
                return;

            float angle = Projectile.rotation + Main.rand.NextFloat(-0.6f, 0.6f);
            Vector2 radial = angle.ToRotationVector2();
            Vector2 position = Projectile.Center + radial * Main.rand.NextFloat(12f, 30f) * VisualScale;
            Vector2 velocity = radial.RotatedBy(MathHelper.PiOver2) * Main.rand.NextFloat(0.45f, 1.15f) - radial * 0.18f;
            Color glow = Color.Lerp(new Color(75, 210, 255), Color.White, Main.rand.NextFloat(0.25f, 0.65f));

            GeneralParticleHandler.SpawnParticle(new GlowOrbParticle(
                position, velocity, false, Main.rand.Next(11, 17), Main.rand.NextFloat(0.18f, 0.30f) * VisualScale,
                glow, true, false, true));

            Dust dust = Dust.NewDustPerfect(position, Main.rand.NextBool() ? DustID.Frost : DustID.BlueTorch,
                velocity * 0.8f, 70, glow, Main.rand.NextFloat(0.45f, 0.8f) * VisualScale);
            dust.noGravity = true;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
            => target.AddBuff(BuffID.Frostburn, 120);

        public override bool PreDraw(ref Color lightColor)
        {
            float scale = VisualScale;
            if (scale <= 0.01f)
                return false;

            Texture2D razorbladeTyphoon = TextureAssets.Projectile[Type].Value;
            Asset<Texture2D> water = ModContent.Request<Texture2D>("CalamityMod/Particles/WaterFlavored");
            Asset<Texture2D> bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle");
            Asset<Texture2D> soft = ModContent.Request<Texture2D>("CalamityMod/Particles/SmallBloom");
            Vector2 position = Projectile.Center - Main.screenPosition;
            Color paleBlue = new(126, 231, 255, 0);
            Color seaBlue = new(62, 199, 255, 0);
            Color whiteBlue = new(222, 251, 255, 0);

            Main.spriteBatch.EnterShaderRegion(BlendState.Additive);

            Rectangle typhoonFrame = razorbladeTyphoon.Frame(1, Main.projFrames[Type], 0, Projectile.frame);
            Main.EntitySpriteDraw(
                razorbladeTyphoon,
                position,
                typhoonFrame,
                Color.White * (0.92f * scale),
                Projectile.rotation,
                typhoonFrame.Size() * 0.5f,
                (IsHelixVariant ? 0.88f : 1.05f) * scale,
                SpriteEffects.None,
                0);

            for (int ring = 0; ring < 3; ring++)
            {
                float ringRotation = Projectile.rotation * (ring % 2 == 0 ? 0.76f : -0.54f) + ring * 0.9f;
                float ringPulse = 0.88f + 0.12f * (float)System.Math.Sin(Age * 0.16f + ring * 1.7f);
                Main.EntitySpriteDraw(bloom.Value, position, null, seaBlue * (0.34f - ring * 0.07f),
                    ringRotation, bloom.Value.Size() * 0.5f, new Vector2(0.72f + ring * 0.17f, 0.38f + ring * 0.12f) * scale * ringPulse, SpriteEffects.None, 0);
            }

            const int petals = 11;
            for (int i = 0; i < petals; i++)
            {
                float phase = MathHelper.TwoPi * i / petals + Projectile.rotation;
                float radius = (16f + i % 3 * 7f + (float)System.Math.Sin(Age * 0.19f + i * 1.8f) * 8f) * scale;
                Vector2 offset = phase.ToRotationVector2() * radius + new Vector2(0f, (float)System.Math.Cos(phase * 2.1f + Age * 0.1f) * 5f * scale);
                float twist = phase + MathHelper.PiOver2 + (float)System.Math.Sin(Age * 0.12f + i) * 0.42f;
                Main.EntitySpriteDraw(water.Value, position + offset, null, Color.Lerp(seaBlue, whiteBlue, i / (float)petals) * 0.48f,
                    twist, water.Value.Size() * 0.5f,
                    new Vector2(0.24f + (i % 2) * 0.06f, 0.58f + (i % 4) * 0.11f) * scale, SpriteEffects.None, 0);
            }

            Main.EntitySpriteDraw(soft.Value, position, null, paleBlue * 0.54f,
                0f, soft.Value.Size() * 0.5f, new Vector2(0.38f, 0.26f) * scale, SpriteEffects.None, 0);
            Main.EntitySpriteDraw(bloom.Value, position, null, whiteBlue * 0.64f,
                0f, bloom.Value.Size() * 0.5f, 0.15f * scale, SpriteEffects.None, 0);
            Main.spriteBatch.ExitShaderRegion();
            return false;
        }

        public static void Spawn(Projectile source, NPC target, int damage, float knockback, bool helixVariant, int orbitSlot = 0)
        {
            if (Main.myPlayer != source.owner)
                return;

            Projectile.NewProjectile(
                source.GetSource_FromThis(),
                target.Bottom + new Vector2(0f, 10f),
                Vector2.Zero,
                ModContent.ProjectileType<BrinyBaron_JazzTyphoon>(),
                damage,
                knockback,
                source.owner,
                target.whoAmI,
                helixVariant ? orbitSlot : SingleSlot);
        }
    }
}
