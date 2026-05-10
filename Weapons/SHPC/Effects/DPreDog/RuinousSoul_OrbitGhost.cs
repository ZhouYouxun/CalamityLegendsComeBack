using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SHPC.Effects.DPreDog
{
    internal class RuinousSoul_OrbitGhost : ModProjectile, ILocalizedModType
    {
        public const int ReleaseCap = 6;

        public new string LocalizationCategory => "Projectiles.SHPC";
        public override string Texture => "CalamityLegendsComeBack/Weapons/SHPC/Effects/DPreDog/RuinousSoul_OrbitGhost";

        public ref float State => ref Projectile.ai[0];
        public ref float TargetIndex => ref Projectile.ai[1];
        public ref float OrbitAngle => ref Projectile.ai[2];

        public override void SetDefaults()
        {
            Projectile.width = 24;
            Projectile.height = 24;
            Projectile.friendly = false;
            Projectile.hostile = false;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 900;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 10;
        }

        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = 4;
            ProjectileID.Sets.TrailCacheLength[Type] = 6;
            ProjectileID.Sets.TrailingMode[Type] = 2;
        }

        public override void OnSpawn(Terraria.DataStructures.IEntitySource source)
        {
            if (Projectile.localAI[0] == 0f)
                Projectile.localAI[0] = Main.rand.NextFloat(62f, 96f);

            if (Projectile.localAI[1] == 0f)
                Projectile.localAI[1] = Main.rand.NextFloat(28f, 58f);
        }

        public override bool? CanDamage() => State == 1f ? null : false;

        public override void AI()
        {
            Player owner = Main.player[Projectile.owner];
            if (!owner.active || owner.dead)
            {
                Projectile.Kill();
                return;
            }

            if (State == 0f)
                OrbitOwner(owner);
            else
                ReleasedAI();

            Projectile.frameCounter++;
            if (Projectile.frameCounter > 4)
            {
                Projectile.frameCounter = 0;
                Projectile.frame = (Projectile.frame + 1) % Main.projFrames[Type];
            }

            Lighting.AddLight(Projectile.Center, new Color(205, 225, 255).ToVector3() * 0.24f);
            SpawnGhostTrail();
        }

        public void Release(int targetIndex)
        {
            State = 1f;
            TargetIndex = targetIndex;
            Projectile.friendly = true;
            Projectile.timeLeft = 150;
            Projectile.netUpdate = true;
        }

        private void OrbitOwner(Player owner)
        {
            if (Projectile.localAI[0] == 0f)
                Projectile.localAI[0] = Main.rand.NextFloat(62f, 96f);
            if (Projectile.localAI[1] == 0f)
                Projectile.localAI[1] = Main.rand.NextFloat(28f, 58f);

            float speed = 0.034f + (Projectile.identity % ReleaseCap) * 0.0045f;
            OrbitAngle += speed;

            float axisRotation = Projectile.identity * 0.71f + (Projectile.localAI[0] - Projectile.localAI[1]) * 0.01f;
            float wobble = (float)System.Math.Sin(Main.GameUpdateCount * 0.035f + Projectile.identity) * 6f;
            Vector2 ellipse = new Vector2(
                (float)System.Math.Cos(OrbitAngle) * (Projectile.localAI[0] + wobble),
                (float)System.Math.Sin(OrbitAngle) * Projectile.localAI[1]
            ).RotatedBy(axisRotation);

            Vector2 desiredCenter = owner.Center + new Vector2(0f, -26f) + ellipse;
            Projectile.velocity = (desiredCenter - Projectile.Center) * 0.24f;
            Projectile.spriteDirection = Projectile.direction = Projectile.velocity.X >= 0f ? 1 : -1;
            Projectile.rotation = Projectile.velocity.ToRotation() + (Projectile.spriteDirection == 1 ? 0f : MathHelper.Pi);
        }

        private void ReleasedAI()
        {
            NPC target = Main.npc.IndexInRange((int)TargetIndex) ? Main.npc[(int)TargetIndex] : null;
            if (target != null && target.active && target.CanBeChasedBy(Projectile))
            {
                Vector2 desired = (target.Center - Projectile.Center).SafeNormalize(Projectile.velocity.SafeNormalize(Vector2.UnitX));
                Projectile.velocity = Vector2.Lerp(Projectile.velocity, desired * 18f, 0.14f);
            }
            else
            {
                Vector2 fallback = (Main.MouseWorld - Projectile.Center).SafeNormalize(Projectile.velocity.SafeNormalize(Vector2.UnitX));
                Projectile.velocity = Vector2.Lerp(Projectile.velocity, fallback * 16f, 0.08f);
            }

            Projectile.spriteDirection = Projectile.direction = Projectile.velocity.X >= 0f ? 1 : -1;
            Projectile.rotation = Projectile.velocity.ToRotation() + (Projectile.spriteDirection == 1 ? 0f : MathHelper.Pi);
        }

        private void SpawnGhostTrail()
        {
            if (Main.rand.NextBool(2))
            {
                SquishyLightParticle particle = new(
                    Projectile.Center + Main.rand.NextVector2Circular(4f, 4f),
                    Main.rand.NextVector2Circular(1.2f, 1.2f) - Projectile.velocity * 0.04f,
                    Main.rand.NextFloat(0.32f, 0.62f),
                    Color.Lerp(new Color(230, 245, 255), new Color(140, 170, 220), Main.rand.NextFloat()),
                    Main.rand.Next(14, 22)
                );
                GeneralParticleHandler.SpawnParticle(particle);
            }

            if (Main.rand.NextBool(3))
            {
                PointParticle point = new(
                    Projectile.Center + Main.rand.NextVector2Circular(6f, 6f),
                    Main.rand.NextVector2Circular(0.6f, 0.6f) - Projectile.velocity * 0.03f,
                    false,
                    Main.rand.Next(10, 16),
                    Main.rand.NextFloat(0.55f, 0.85f),
                    Color.Lerp(Color.White, new Color(130, 160, 235), Main.rand.NextFloat(0.2f, 0.7f)) * 0.75f);
                GeneralParticleHandler.SpawnParticle(point);
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            for (int i = 0; i < 8; i++)
            {
                Dust dust = Dust.NewDustPerfect(
                    target.Center,
                    DustID.SpectreStaff,
                    Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(1.8f, 4.6f),
                    0,
                    Color.White,
                    Main.rand.NextFloat(0.8f, 1.2f));
                dust.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Type].Value;
            Rectangle frame = texture.Frame(1, Main.projFrames[Type], 0, Projectile.frame);
            Vector2 origin = frame.Size() * 0.5f;
            SpriteEffects effects = Projectile.spriteDirection == -1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;

            for (int i = Projectile.oldPos.Length - 1; i >= 0; i--)
            {
                float fade = 1f - i / (float)Projectile.oldPos.Length;
                Vector2 drawPosition = Projectile.oldPos[i] + Projectile.Size * 0.5f - Main.screenPosition;
                Main.EntitySpriteDraw(
                    texture,
                    drawPosition,
                    frame,
                    Color.Lerp(new Color(160, 190, 255), Color.White, fade) * (0.22f * fade),
                    Projectile.rotation,
                    origin,
                    Projectile.scale * (0.85f + fade * 0.25f),
                    effects,
                    0);
            }

            Main.EntitySpriteDraw(
                texture,
                Projectile.Center - Main.screenPosition,
                frame,
                Color.White,
                Projectile.rotation,
                origin,
                Projectile.scale,
                effects,
                0);

            return false;
        }
    }
}
