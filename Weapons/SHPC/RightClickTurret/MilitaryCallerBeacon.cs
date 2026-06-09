using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SHPC.RightClickTurret
{
    internal sealed class MilitaryCallerBeacon : ModProjectile, ILocalizedModType
    {
        private const float Gravity = 0.18f;
        private const float MaxFallSpeed = 12f;

        private bool calledDropPod;

        public new string LocalizationCategory => "Projectiles.SHPC";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private MilitaryTurretKind Kind => (MilitaryTurretKind)Utils.Clamp((int)Projectile.ai[0], 0, 6);
        private int SourceDamage => (int)Projectile.ai[1];

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 14;
            ProjectileID.Sets.TrailingMode[Type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = 18;
            Projectile.height = 18;
            Projectile.tileCollide = true;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 150;
            Projectile.netImportant = true;
        }

        public override bool? CanDamage() => false;

        public override void AI()
        {
            Projectile.rotation += Projectile.direction * 0.28f;
            Projectile.velocity.Y = MathHelper.Clamp(Projectile.velocity.Y + Gravity, -MaxFallSpeed, MaxFallSpeed);
            Projectile.spriteDirection = Projectile.velocity.X >= 0f ? 1 : -1;

            MilitaryTurretStats stats = MilitaryTurretUtility.GetStats(Kind);
            Lighting.AddLight(Projectile.Center, stats.ThemeColor.ToVector3() * 0.22f);
            SpawnFlightEffects(stats.ThemeColor);
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            CallDropPod();
            Projectile.Kill();
            return false;
        }

        public override void OnKill(int timeLeft)
        {
            CallDropPod();
        }

        private void CallDropPod()
        {
            if (calledDropPod)
                return;

            calledDropPod = true;
            Player owner = Main.player[Projectile.owner];
            Vector2 restingPoint = MilitaryTurretUtility.FindRestingPoint(Projectile.Center);

            if (!MilitaryTurretUtility.CanDeployTurret(owner, restingPoint, out string failureReason))
            {
                MilitaryTurretUtility.NotifyFailure(owner, failureReason, restingPoint);
                return;
            }

            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            Vector2 dropPosition = new(restingPoint.X, restingPoint.Y - 780f);
            int dropIndex = Projectile.NewProjectile(
                Projectile.GetSource_FromThis(),
                dropPosition,
                new Vector2(0f, 17f),
                ModContent.ProjectileType<MilitaryTurretDropPod>(),
                0,
                0f,
                Projectile.owner,
                (float)Kind,
                SourceDamage);

            if (Main.projectile.IndexInRange(dropIndex))
            {
                Main.projectile[dropIndex].CritChance = Projectile.CritChance;
                Main.projectile[dropIndex].netUpdate = true;
            }

            SoundEngine.PlaySound(new SoundStyle("CalamityMod/Sounds/Item/MissileNearing") { Volume = 0.5f, Pitch = 0.28f }, dropPosition);
        }

        private void SpawnFlightEffects(Color themeColor)
        {
            if (Main.dedServ)
                return;

            if (Projectile.timeLeft % 2 == 0)
            {
                Dust dust = Dust.NewDustPerfect(Projectile.Center - Projectile.velocity * 0.4f + Main.rand.NextVector2Circular(3f, 3f), DustID.Electric);
                dust.velocity = -Projectile.velocity * Main.rand.NextFloat(0.04f, 0.12f) + Main.rand.NextVector2Circular(0.8f, 0.8f);
                dust.color = Color.Lerp(themeColor, Color.White, Main.rand.NextFloat(0.25f, 0.8f));
                dust.noGravity = true;
                dust.scale = Main.rand.NextFloat(0.75f, 1.15f);
            }

            if (Projectile.timeLeft % 9 == 0)
            {
                Particle pulse = new DirectionalPulseRing(
                    Projectile.Center,
                    -Projectile.velocity.SafeNormalize(Vector2.UnitY) * 0.8f,
                    themeColor * 0.55f,
                    new Vector2(0.65f, 0.65f),
                    Projectile.velocity.ToRotation(),
                    0.04f,
                    0.18f,
                    14);

                GeneralParticleHandler.SpawnParticle(pulse);
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Texture2D star = ModContent.Request<Texture2D>("CalamityMod/Particles/HalfStar").Value;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            Color themeColor = MilitaryTurretUtility.GetStats(Kind).ThemeColor;
            Color bloomColor = themeColor with { A = 0 };
            float pulse = 0.82f + 0.18f * (float)System.Math.Sin(Main.GlobalTimeWrappedHourly * 12f + Projectile.identity);

            for (int i = 0; i < Projectile.oldPos.Length; i++)
            {
                float opacity = (Projectile.oldPos.Length - i) / (float)Projectile.oldPos.Length;
                Vector2 oldDrawPosition = Projectile.oldPos[i] + Projectile.Size * 0.5f - Main.screenPosition;
                Main.EntitySpriteDraw(bloom, oldDrawPosition, null, bloomColor * 0.18f * opacity, 0f, bloom.Size() * 0.5f, 0.16f * opacity, SpriteEffects.None, 0);
            }

            Main.EntitySpriteDraw(bloom, drawPosition, null, bloomColor * 0.72f, 0f, bloom.Size() * 0.5f, 0.24f * pulse, SpriteEffects.None, 0);
            Main.EntitySpriteDraw(star, drawPosition, null, Color.White with { A = 0 } * 0.82f, Projectile.rotation, star.Size() * 0.5f, new Vector2(0.34f, 0.88f) * pulse, SpriteEffects.None, 0);
            Main.EntitySpriteDraw(star, drawPosition, null, bloomColor * 0.85f, Projectile.rotation + MathHelper.PiOver2, star.Size() * 0.5f, new Vector2(0.26f, 0.66f) * pulse, SpriteEffects.None, 0);
            return false;
        }
    }
}
