using System;
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
    // Fired by the forward pair of the drone battery. Borrows ScorpioLargeRocket's
    // sprite sheet and glow asset only — homing, coloring, and detonation are our own.
    internal sealed class YC_TrackingRocket : ModProjectile, ILocalizedModType
    {
        private static readonly Color RocketGold = new(255, 226, 110);
        private static readonly Color RocketOrange = new(255, 116, 38);

        private ref float Timer => ref Projectile.localAI[0];

        public new string LocalizationCategory => "Projectiles.YharimsCrystal";
        public override string Texture => "CalamityMod/Projectiles/Ranged/ScorpioLargeRocket";

        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = 4;
            ProjectileID.Sets.TrailingMode[Type] = 2;
            ProjectileID.Sets.TrailCacheLength[Type] = 8;
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 15;
            Projectile.friendly = true;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 240;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
            Projectile.extraUpdates = 1;
        }

        public override void OnSpawn(Terraria.DataStructures.IEntitySource source)
        {
            YharimsCrystalHellBladeGlobalProjectile.Mark(Projectile, YCWeaponForm.Crystal);
        }

        public override void AI()
        {
            Timer++;

            NPC target = Projectile.Center.ClosestNPCAt(1100f);
            if (target != null)
            {
                Vector2 desired = (target.Center - Projectile.Center).SafeNormalize(Vector2.UnitX);
                float turnRate = MathHelper.ToRadians(MathHelper.Lerp(3.5f, 11f, Utils.GetLerpValue(0f, 24f, Timer, true)));
                float newAngle = Projectile.velocity.ToRotation().AngleTowards(desired.ToRotation(), turnRate);
                Projectile.velocity = newAngle.ToRotationVector2() * Projectile.velocity.Length();
            }

            if (Projectile.velocity.Length() < 17f)
                Projectile.velocity *= 1.028f;

            Projectile.frameCounter++;
            if (Projectile.frameCounter >= 4)
            {
                Projectile.frame = (Projectile.frame + 1) % Main.projFrames[Type];
                Projectile.frameCounter = 0;
            }

            Projectile.rotation = Projectile.velocity.ToRotation();
            Lighting.AddLight(Projectile.Center, Color.Lerp(RocketOrange, RocketGold, 0.5f).ToVector3() * 0.6f);

            if (Main.dedServ)
                return;

            Projectile.alpha = (int)Utils.Remap(Projectile.timeLeft, 24f, 0f, 0f, 255f);

            if (Timer % 2 == 0)
            {
                Vector2 dustVel = -Projectile.velocity.SafeNormalize(Vector2.UnitX).RotatedByRandom(0.25f) * Main.rand.NextFloat(1.5f, 4f);
                Dust dust = Dust.NewDustPerfect(Projectile.Center - Projectile.velocity * 0.6f, DustID.GoldFlame, dustVel, 0, Main.rand.NextBool(3) ? Color.White : RocketGold, Main.rand.NextFloat(1.05f, 1.4f));
                dust.noGravity = true;
            }
        }

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            modifiers.SourceDamage *= 1.18f;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(new BalanceYharimsCrystal().GetFireDebuffType(), 200);
        }

        public override void OnKill(int timeLeft)
        {
            Player owner = Main.player[Projectile.owner];
            owner.Calamity().GeneralScreenShakePower = System.Math.Max(owner.Calamity().GeneralScreenShakePower, 4.5f);
            SoundEngine.PlaySound(SoundID.Item62 with { Volume = 0.65f, Pitch = -0.1f }, Projectile.Center);

            if (Main.dedServ)
                return;

            GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(Projectile.Center, Vector2.Zero, RocketGold, Vector2.One, Projectile.rotation, 0.07f, 1.5f, 18));
            for (int i = 0; i < 22; i++)
            {
                Vector2 vel = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(3f, 9f);
                Dust dust = Dust.NewDustPerfect(Projectile.Center, DustID.GoldFlame, vel, 0, Main.rand.NextBool(3) ? Color.White : RocketOrange, Main.rand.NextFloat(1f, 1.5f));
                dust.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = ModContent.Request<Texture2D>(Texture).Value;
            Texture2D glow = ModContent.Request<Texture2D>("CalamityMod/Projectiles/Ranged/ScorpioLargeRocket_Glow").Value;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            Rectangle frame = texture.Frame(1, Main.projFrames[Type], 0, Projectile.frame);
            float drawRotation = Projectile.rotation + MathHelper.PiOver2;
            Vector2 origin = frame.Size() * 0.5f;
            Color drawColor = Projectile.GetAlpha(Color.Lerp(lightColor, Color.Yellow, 0.45f));

            // Gold border outline — same pulsing-offset trick used on the drone hull.
            float pulse = 0.82f + 0.18f * MathF.Sin(Main.GlobalTimeWrappedHourly * 6f);
            Color borderColor = RocketGold with { A = 0 };
            for (int i = 0; i < 5; i++)
            {
                float angle = MathHelper.TwoPi * i / 5f;
                Vector2 offset = angle.ToRotationVector2() * 2.4f * pulse;
                Main.EntitySpriteDraw(texture, drawPosition + offset, frame, borderColor * 0.42f, drawRotation, origin, Projectile.scale, SpriteEffects.None);
            }

            Main.EntitySpriteDraw(texture, drawPosition, frame, drawColor, drawRotation, origin, Projectile.scale, SpriteEffects.None);
            Main.EntitySpriteDraw(glow, drawPosition, frame, Color.White * Projectile.Opacity, drawRotation, origin, Projectile.scale, SpriteEffects.None);

            return false;
        }
    }
}
