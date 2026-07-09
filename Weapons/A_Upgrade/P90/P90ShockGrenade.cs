using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Upgrade.P90
{
    internal sealed class P90ShockGrenade : ModProjectile, ILocalizedModType
    {
        private const int FlightLifetime = 72;
        private const int ExplosionFrames = 3;
        private const int ExplosionSize = 224;
        private const int ShockMarkDuration = 180;

        public new string LocalizationCategory => "Projectiles.P90";
        public override string Texture => "CalamityMod/Items/Weapons/Rogue/DoomsdayDevice";

        private bool Exploding
        {
            get => Projectile.ai[0] == 1f;
            set => Projectile.ai[0] = value ? 1f : 0f;
        }

        private ref float Time => ref Projectile.ai[1];

        public override void SetDefaults()
        {
            Projectile.width = 24;
            Projectile.height = 24;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.penetrate = -1;
            Projectile.timeLeft = FlightLifetime;
            Projectile.tileCollide = true;
            Projectile.ignoreWater = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }

        public override bool? CanDamage() => Exploding ? null : false;

        public override void AI()
        {
            if (Exploding)
            {
                Projectile.velocity = Vector2.Zero;
                Projectile.alpha = 255;
                return;
            }

            Time++;
            Projectile.rotation += Projectile.velocity.X * 0.035f;
            Projectile.velocity.Y += 0.18f;
            Projectile.velocity.X *= 0.992f;
            Lighting.AddLight(Projectile.Center, new Vector3(0.12f, 0.45f, 0.38f));

            if (Main.rand.NextBool(2))
            {
                Dust dust = Dust.NewDustPerfect(
                    Projectile.Center + Main.rand.NextVector2Circular(6f, 6f),
                    Main.rand.NextBool() ? DustID.GreenTorch : DustID.PurpleTorch,
                    -Projectile.velocity.SafeNormalize(Vector2.UnitY).RotatedByRandom(0.45f) * Main.rand.NextFloat(0.4f, 1.8f),
                    110,
                    Main.rand.NextBool() ? new Color(72, 255, 180) : new Color(190, 116, 255),
                    Main.rand.NextFloat(0.65f, 1.1f));
                dust.noGravity = true;
            }

            if (Time >= 44f || TouchingTarget())
                Detonate();
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            Detonate();
            return false;
        }

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            if (!Exploding)
                return;

            modifiers.Knockback *= 2.8f;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.Confused, 90);
            target.AddBuff(ModContent.BuffType<P90ShockDebuff>(), ShockMarkDuration);
        }

        private bool TouchingTarget()
        {
            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (npc.CanBeChasedBy(Projectile) && Projectile.Hitbox.Intersects(npc.Hitbox))
                    return true;
            }

            return false;
        }

        private void Detonate()
        {
            if (Exploding)
                return;

            Exploding = true;
            Vector2 center = Projectile.Center;
            Projectile.Resize(ExplosionSize, ExplosionSize);
            Projectile.Center = center;
            Projectile.velocity = Vector2.Zero;
            Projectile.tileCollide = false;
            Projectile.timeLeft = ExplosionFrames;
            Projectile.netUpdate = true;

            SoundEngine.PlaySound(SoundID.Item14 with { Volume = 0.72f, Pitch = 0.12f }, center);
            SoundEngine.PlaySound(SoundID.Item92 with { Volume = 0.45f, Pitch = -0.18f }, center);
            SpawnExplosion(center);
        }

        private static void SpawnExplosion(Vector2 center)
        {
            for (int i = 0; i < 42; i++)
            {
                float angle = MathHelper.TwoPi * i / 42f;
                Vector2 velocity = angle.ToRotationVector2() * Main.rand.NextFloat(4.2f, 12.5f);
                Dust dust = Dust.NewDustPerfect(
                    center + velocity.SafeNormalize(Vector2.UnitY) * 14f,
                    i % 3 == 0 ? DustID.PurpleTorch : DustID.GreenTorch,
                    velocity,
                    80,
                    i % 3 == 0 ? new Color(198, 122, 255) : new Color(72, 255, 180),
                    Main.rand.NextFloat(1.0f, 1.85f));
                dust.noGravity = true;
            }

            for (int i = 0; i < 14; i++)
            {
                Dust smoke = Dust.NewDustPerfect(
                    center + Main.rand.NextVector2Circular(40f, 40f),
                    DustID.Smoke,
                    Main.rand.NextVector2Circular(2.4f, 2.4f),
                    140,
                    Color.Lerp(Color.Gray, new Color(70, 96, 86), Main.rand.NextFloat()),
                    Main.rand.NextFloat(1.0f, 1.7f));
                smoke.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Exploding)
            {
                Texture2D ring = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomRing").Value;
                float fade = Projectile.timeLeft / (float)ExplosionFrames;
                Main.EntitySpriteDraw(
                    ring,
                    Projectile.Center - Main.screenPosition,
                    null,
                    new Color(92, 255, 190, 0) * fade * 0.7f,
                    Main.GlobalTimeWrappedHourly * 3f,
                    ring.Size() * 0.5f,
                    0.74f + (1f - fade) * 0.46f,
                    SpriteEffects.None,
                    0f);
                return false;
            }

            Texture2D texture = TextureAssets.Projectile[Type].Value;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            SpriteEffects effects = Projectile.velocity.X < 0f ? SpriteEffects.FlipVertically : SpriteEffects.None;

            for (int i = 0; i < 6; i++)
            {
                Vector2 offset = (MathHelper.TwoPi * i / 6f + Main.GlobalTimeWrappedHourly * 2f).ToRotationVector2() * 2.2f;
                Main.EntitySpriteDraw(texture, drawPosition + offset, null, new Color(92, 255, 190, 0) * 0.18f, Projectile.rotation, texture.Size() * 0.5f, Projectile.scale, effects, 0f);
            }

            Main.EntitySpriteDraw(texture, drawPosition, null, lightColor, Projectile.rotation, texture.Size() * 0.5f, Projectile.scale, effects, 0f);
            return false;
        }
    }
}
