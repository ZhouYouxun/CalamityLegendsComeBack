using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.BlossomFlux.TurretMode
{
    // 炮台期间周期性甩出的藤蔓，用来补足范围骚扰与场面感。
    internal sealed class BFTurretVine : ModProjectile
    {
        public const int TotalSegments = 8;

        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetDefaults()
        {
            Projectile.width = 28;
            Projectile.height = 28;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 20;
            Projectile.alpha = 255;
            Projectile.timeLeft = 90;
        }

        public override void AI()
        {
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;

            if (Projectile.ai[1] == 0f)
            {
                Projectile.alpha -= 95;
                if (Projectile.alpha > 0)
                    return;

                Projectile.alpha = 0;
                Projectile.ai[1] = 1f;

                if (Projectile.ai[0] == 0f)
                    Projectile.position += Projectile.velocity;

                if (Main.myPlayer == Projectile.owner && Projectile.ai[0] < TotalSegments)
                {
                    int nextSegment = Projectile.NewProjectile(
                        Projectile.GetSource_FromThis(),
                        Projectile.Center + Projectile.velocity,
                        Projectile.velocity.RotatedBy(Main.rand.NextFloat(-0.06f, 0.06f)),
                        Type,
                        Projectile.damage,
                        Projectile.knockBack,
                        Projectile.owner,
                        Projectile.ai[0] + 1f);
                    NetMessage.SendData(MessageID.SyncProjectile, -1, -1, null, nextSegment);
                }

                return;
            }

            Projectile.alpha += 10;
            if (Projectile.alpha == 120)
            {
                for (int i = 0; i < 7; i++)
                {
                    Dust thorn = Dust.NewDustPerfect(
                        Projectile.Center,
                        Main.rand.NextBool(3) ? DustID.TerraBlade : DustID.GemEmerald,
                        Projectile.velocity.RotatedByRandom(0.5f) * Main.rand.NextFloat(0.15f, 0.65f),
                        100,
                        new Color(135, 255, 150),
                        Main.rand.NextFloat(0.9f, 1.2f));
                    thorn.noGravity = true;
                }
            }

            if (Projectile.alpha >= 255)
                Projectile.Kill();
        }

        public override bool ShouldUpdatePosition() => false;

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D vineTexture = ModContent.Request<Texture2D>("CalamityLegendsComeBack/Texture/SuperTexturePack/fx_EnergyBolt6").Value;
            Texture2D budTexture = ModContent.Request<Texture2D>("CalamityLegendsComeBack/Texture/SuperTexturePack/flower_015").Value;

            float opacity = 1f - Projectile.alpha / 255f;
            float segmentCompletion = 1f - Projectile.ai[0] / TotalSegments;
            Color vineColor = new Color(92, 255, 136, 0) * (0.55f * opacity);
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;

            Main.EntitySpriteDraw(
                vineTexture,
                drawPosition,
                null,
                vineColor,
                Projectile.rotation,
                vineTexture.Size() * 0.5f,
                0.16f + 0.18f * segmentCompletion,
                SpriteEffects.None,
                0f);

            if (Projectile.ai[0] == TotalSegments)
            {
                Main.EntitySpriteDraw(
                    budTexture,
                    drawPosition,
                    null,
                    new Color(180, 255, 180, 0) * (0.4f * opacity),
                    Main.GlobalTimeWrappedHourly * 1.4f,
                    budTexture.Size() * 0.5f,
                    0.18f,
                    SpriteEffects.None,
                    0f);
            }

            return false;
        }
    }
}
