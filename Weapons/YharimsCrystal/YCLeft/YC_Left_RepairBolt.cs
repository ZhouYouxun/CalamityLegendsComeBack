using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.YharimsCrystal.YCLeft
{
    public class YC_Left_RepairBolt : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.YharimsCrystal";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private ref float Timer => ref Projectile.ai[0];

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 10;
            ProjectileID.Sets.TrailingMode[Type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = 12;
            Projectile.height = 12;
            Projectile.friendly = false;
            Projectile.hostile = false;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 150;
            Projectile.extraUpdates = 2;
        }

        public override bool? CanDamage() => false;

        public override void AI()
        {
            Timer++;

            Player owner = Main.player[Projectile.owner];
            if (!owner.active || owner.dead)
            {
                Projectile.Kill();
                return;
            }

            if (Timer > 8f)
            {
                Vector2 toOwner = owner.Center - Projectile.Center;
                Vector2 desiredVelocity = toOwner.SafeNormalize(Vector2.UnitY) * MathHelper.Lerp(7f, 16f, Utils.GetLerpValue(8f, 34f, Timer, true));
                Projectile.velocity = Vector2.Lerp(Projectile.velocity, desiredVelocity, 0.09f);
            }
            else
            {
                Projectile.velocity = Projectile.velocity.RotatedBy(0.025f * owner.direction);
            }

            Projectile.rotation = Projectile.velocity.ToRotation();
            Lighting.AddLight(Projectile.Center, new Color(110, 255, 225).ToVector3() * 0.4f);

            if (Projectile.Hitbox.Intersects(owner.Hitbox) || Vector2.DistanceSquared(Projectile.Center, owner.Center) <= 18f * 18f)
                Projectile.Kill();

            if (Main.dedServ || !Main.rand.NextBool(3))
                return;

            YC_LeftSquadronHelper.EmitTechDust(
                Projectile.Center,
                -Projectile.velocity * 0.08f + Main.rand.NextVector2Circular(0.4f, 0.4f),
                Color.Lerp(new Color(100, 255, 220), Color.White, Main.rand.NextFloat(0.4f)),
                Main.rand.NextFloat(0.65f, 0.95f));
        }

        public override void OnKill(int timeLeft)
        {
            Player owner = Main.player[Projectile.owner];
            if (owner.active && !owner.dead)
            {
                int healAmount = System.Math.Min(Main.rand.Next(2, 5), owner.statLifeMax2 - owner.statLife);
                if (healAmount > 0)
                {
                    owner.statLife += healAmount;
                    owner.HealEffect(healAmount, true);
                }
            }

            if (Main.dedServ)
                return;

            for (int i = 0; i < 12; i++)
            {
                Vector2 direction = Main.rand.NextVector2CircularEdge(1f, 1f);
                YC_LeftSquadronHelper.EmitTechDust(
                    Projectile.Center,
                    direction * Main.rand.NextFloat(1.2f, 3.5f),
                    Color.Lerp(new Color(90, 255, 215), Color.White, Main.rand.NextFloat(0.35f)),
                    Main.rand.NextFloat(0.7f, 1.1f));
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D trail = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomLineFade").Value;
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Vector2 previous = Projectile.Center;

            for (int i = 0; i < Projectile.oldPos.Length; i++)
            {
                if (Projectile.oldPos[i] == Vector2.Zero)
                    continue;

                Vector2 current = Projectile.oldPos[i] + Projectile.Size * 0.5f;
                Vector2 segment = previous - current;
                float length = segment.Length();
                float completion = 1f - i / (float)Projectile.oldPos.Length;

                if (length > 0.01f)
                {
                    Main.EntitySpriteDraw(
                        trail,
                        previous - Main.screenPosition - segment * 0.5f,
                        null,
                        new Color(110, 255, 220, 0) * (completion * 0.32f),
                        segment.ToRotation() + MathHelper.PiOver2,
                        trail.Size() * 0.5f,
                        new Vector2(0.22f, length / trail.Height) * Projectile.scale,
                        SpriteEffects.None,
                        0);
                }

                previous = current;
            }

            Main.EntitySpriteDraw(
                bloom,
                Projectile.Center - Main.screenPosition,
                null,
                new Color(220, 255, 245, 0) * 0.7f,
                Projectile.rotation,
                bloom.Size() * 0.5f,
                Projectile.scale * 0.15f,
                SpriteEffects.None,
                0);

            return false;
        }
    }
}
