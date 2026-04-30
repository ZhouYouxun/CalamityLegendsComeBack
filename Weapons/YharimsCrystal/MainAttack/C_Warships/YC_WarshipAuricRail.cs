using CalamityMod.Buffs.DamageOverTime;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.YharimsCrystal.MainAttack.C_Warships
{
    public class YC_WarshipAuricRail : ModProjectile, ILocalizedModType
    {
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";
        public new string LocalizationCategory => "Projectiles.YharimsCrystal";

        private ref float SlotSeed => ref Projectile.ai[0];
        private ref float Timer => ref Projectile.localAI[0];

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 24;
            ProjectileID.Sets.TrailingMode[Type] = 2;
            ProjectileID.Sets.DrawScreenCheckFluff[Type] = 12000;
        }

        public override void SetDefaults()
        {
            Projectile.width = 16;
            Projectile.height = 16;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 72;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.extraUpdates = 5;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 8;
        }

        public override void OnSpawn(Terraria.DataStructures.IEntitySource source)
        {
            Projectile.rotation = Projectile.velocity.ToRotation();
            SoundEngine.PlaySound(SoundID.Item72 with { Volume = 0.16f, Pitch = 0.22f }, Projectile.Center);
        }

        public override void AI()
        {
            Timer++;
            Vector2 forward = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            Projectile.rotation = forward.ToRotation();
            Projectile.velocity *= 1.002f;

            Lighting.AddLight(Projectile.Center, new Color(255, 205, 115).ToVector3() * 0.55f);

            if (!Main.dedServ && Main.GameUpdateCount % 2 == 0)
            {
                Vector2 normal = forward.RotatedBy(MathHelper.PiOver2);
                float wave = (float)System.Math.Sin((Timer + SlotSeed * 23f) * 0.38f) * 8f;

                Dust dust = Dust.NewDustPerfect(
                    Projectile.Center + normal * wave,
                    DustID.GoldFlame,
                    -forward * Main.rand.NextFloat(0.25f, 0.8f) + normal * Main.rand.NextFloat(-0.22f, 0.22f),
                    0,
                    Color.Lerp(new Color(255, 178, 90), Color.White, Main.rand.NextFloat(0.16f, 0.55f)),
                    Main.rand.NextFloat(0.75f, 1.1f));
                dust.noGravity = true;
            }

            if (!Main.dedServ && Timer % 5f == 0f)
            {
                Vector2 forwardOffset = forward * Main.rand.NextFloat(-12f, 12f);
                GlowOrbParticle glow = new(
                    Projectile.Center + forwardOffset,
                    -forward * Main.rand.NextFloat(0.2f, 0.55f),
                    false,
                    Main.rand.Next(8, 12),
                    Main.rand.NextFloat(0.2f, 0.32f),
                    Color.Lerp(new Color(255, 195, 110), Color.White, Main.rand.NextFloat(0.2f, 0.65f)),
                    true,
                    false,
                    true);
                GeneralParticleHandler.SpawnParticle(glow);
            }
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            float collisionPoint = 0f;
            Vector2 forward = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            Vector2 start = Projectile.Center - forward * 52f;
            Vector2 end = Projectile.Center + forward * 54f;
            return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), start, end, 18f, ref collisionPoint);
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(ModContent.BuffType<Dragonfire>(), 90);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D line = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomLineFade").Value;
            Texture2D glow = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Vector2 forward = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            Color outer = new Color(255, 182, 90, 0);
            Color inner = new Color(255, 245, 205, 0);

            for (int i = Projectile.oldPos.Length - 1; i >= 0; i--)
            {
                if (Projectile.oldPos[i] == Vector2.Zero)
                    continue;

                float completion = 1f - i / (float)Projectile.oldPos.Length;
                Vector2 drawPosition = Projectile.oldPos[i] + Projectile.Size * 0.5f - Main.screenPosition;
                float ribbon = 0.24f + completion * 0.46f;

                Main.EntitySpriteDraw(
                    line,
                    drawPosition,
                    null,
                    outer * (completion * 0.54f),
                    Projectile.rotation + MathHelper.PiOver2,
                    line.Size() * 0.5f,
                    new Vector2(ribbon, 82f / line.Height),
                    SpriteEffects.None,
                    0);
            }

            Vector2 center = Projectile.Center - Main.screenPosition;
            Main.EntitySpriteDraw(line, center, null, inner * 0.72f, Projectile.rotation + MathHelper.PiOver2, line.Size() * 0.5f, new Vector2(0.24f, 112f / line.Height), SpriteEffects.None, 0);
            Main.EntitySpriteDraw(glow, center + forward * 22f, null, outer * 0.82f, Projectile.rotation, glow.Size() * 0.5f, 0.18f, SpriteEffects.None, 0);
            Main.EntitySpriteDraw(glow, center + forward * 22f, null, (Color.White with { A = 0 }) * 0.36f, Projectile.rotation, glow.Size() * 0.5f, 0.08f, SpriteEffects.None, 0);
            return false;
        }
    }
}
