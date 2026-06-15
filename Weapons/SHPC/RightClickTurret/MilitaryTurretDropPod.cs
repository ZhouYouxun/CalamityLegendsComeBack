using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SHPC.RightClickTurret
{
    internal sealed class MilitaryTurretDropPod : ModProjectile, ILocalizedModType
    {
        private const float Gravity = 0.28f;
        private const float MaxFallSpeed = 24f;

        private bool deployed;

        public new string LocalizationCategory => "Projectiles.SHPC";
        public override string Texture => "CalamityLegendsComeBack/Weapons/SHPC/RightClickTurret/空投仓";

        private MilitaryTurretKind Kind => (MilitaryTurretKind)Utils.Clamp((int)Projectile.ai[0], 0, 6);
        private int SourceDamage => (int)Projectile.ai[1];

        public override void SetDefaults()
        {
            Projectile.width = 34;
            Projectile.height = 64;
            Projectile.tileCollide = true;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 105;
            Projectile.netImportant = true;
        }

        public override bool? CanDamage() => false;

        public override void AI()
        {
            Projectile.velocity.X *= 0.98f;
            Projectile.velocity.Y = MathHelper.Clamp(Projectile.velocity.Y + Gravity, -MaxFallSpeed, MaxFallSpeed);
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;

            MilitaryTurretStats stats = MilitaryTurretUtility.GetStats(Kind);
            Lighting.AddLight(Projectile.Center, stats.ThemeColor.ToVector3() * 0.18f);
            SpawnDropTrail(stats.ThemeColor);
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            Deploy();
            Projectile.Kill();
            return false;
        }

        public override void OnKill(int timeLeft)
        {
            Deploy();
        }

        private void Deploy()
        {
            if (deployed)
                return;

            deployed = true;
            Player owner = Main.player[Projectile.owner];
            Vector2 restingPoint = MilitaryTurretUtility.FindRestingPoint(Projectile.Center);

            if (!MilitaryTurretUtility.CanDeployTurret(owner, restingPoint, out string failureReason))
            {
                MilitaryTurretUtility.NotifyFailure(owner, failureReason, restingPoint);
                return;
            }

            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                MilitaryTurretUtility.ReplaceOldestTurretIfAtCapacity(owner);

                int turretIndex = Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    restingPoint - new Vector2(0f, 18f),
                    Vector2.Zero,
                    ModContent.ProjectileType<MilitaryFriendlyTurret>(),
                    0,
                    0f,
                    Projectile.owner,
                    (float)Kind,
                    SourceDamage);

                if (Main.projectile.IndexInRange(turretIndex))
                {
                    Main.projectile[turretIndex].CritChance = Projectile.CritChance;
                    Main.projectile[turretIndex].netUpdate = true;
                }
            }

            SoundEngine.PlaySound(SoundID.Item14 with { Volume = 0.82f, Pitch = -0.12f }, restingPoint);
            SpawnImpactBurst(restingPoint, MilitaryTurretUtility.GetStats(Kind).ThemeColor);
        }

        private void SpawnDropTrail(Color themeColor)
        {
            if (Main.dedServ)
                return;

            for (int i = 0; i < 2; i++)
            {
                Dust smoke = Dust.NewDustPerfect(Projectile.Center - Projectile.velocity * 0.35f + Main.rand.NextVector2Circular(8f, 8f), DustID.Smoke);
                smoke.velocity = -Projectile.velocity * Main.rand.NextFloat(0.015f, 0.045f) + Main.rand.NextVector2Circular(1.2f, 1.2f);
                smoke.scale = Main.rand.NextFloat(0.9f, 1.5f);
                smoke.noGravity = true;
            }

            if (Projectile.timeLeft % 3 == 0)
            {
                Dust spark = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(7f, 7f), DustID.Electric);
                spark.velocity = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(2f, 5f);
                spark.color = Color.Lerp(themeColor, Color.White, Main.rand.NextFloat(0.25f, 0.85f));
                spark.noGravity = true;
                spark.scale = Main.rand.NextFloat(0.8f, 1.2f);
            }
        }

        private static void SpawnImpactBurst(Vector2 restingPoint, Color themeColor)
        {
            if (Main.dedServ)
                return;

            for (int i = 0; i < 24; i++)
            {
                Dust dust = Dust.NewDustPerfect(restingPoint + Main.rand.NextVector2Circular(22f, 10f), DustID.Electric);
                dust.velocity = new Vector2(Main.rand.NextFloat(-4f, 4f), Main.rand.NextFloat(-7f, -2f));
                dust.color = Color.Lerp(themeColor, Color.White, Main.rand.NextFloat(0.2f, 0.85f));
                dust.noGravity = true;
                dust.scale = Main.rand.NextFloat(0.85f, 1.35f);
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Type].Value;
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            Vector2 origin = texture.Size() * 0.5f;
            Color themeColor = MilitaryTurretUtility.GetStats(Kind).ThemeColor with { A = 0 };
            Color drawColor = Projectile.GetAlpha(lightColor);
            Color outlineColor = Color.White * 0.82f;
            const float outlineDistance = 2f;

            Main.EntitySpriteDraw(bloom, drawPosition, null, themeColor * 0.48f, 0f, bloom.Size() * 0.5f, new Vector2(0.32f, 0.48f), SpriteEffects.None, 0);

            for (int i = 0; i < 8; i++)
            {
                Vector2 outlineOffset = (MathHelper.TwoPi * i / 8f).ToRotationVector2() * outlineDistance;
                Main.EntitySpriteDraw(texture, drawPosition + outlineOffset, null, outlineColor, Projectile.rotation, origin, Projectile.scale, SpriteEffects.None, 0);
            }

            Main.EntitySpriteDraw(texture, drawPosition, null, drawColor, Projectile.rotation, origin, Projectile.scale, SpriteEffects.None, 0);
            return false;
        }
    }
}
