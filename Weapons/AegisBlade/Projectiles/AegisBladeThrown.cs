using CalamityMod;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.AegisBlade.Projectiles
{
    // 剑盾联动投掷出的大剑。直线飞行，插地后生成土墙。
    public class AegisBladeThrown : ModProjectile
    {
        public override string Texture => "CalamityLegendsComeBack/Weapons/AegisBlade/AegisBlade";

        private bool embedded = false;
        private bool wallSpawned = false;
        private int embedTimer = 0;

        private static readonly Color TrailColor = new(255, 200, 60, 0);

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 28;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.tileCollide = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 300;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 10;
            Projectile.ignoreWater = true;
        }

        public override void AI()
        {
            if (!embedded)
            {
                // 旋转
                Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver4;

                // 飞行尾迹
                if (!Main.dedServ && Main.rand.NextBool(2))
                {
                    Vector2 back = Projectile.Center - Projectile.velocity.SafeNormalize(Vector2.Zero) * 30f;
                    GeneralParticleHandler.SpawnParticle(new GlowSparkParticle(
                        back + Main.rand.NextVector2Circular(6f, 6f),
                        Main.rand.NextVector2Circular(1.5f, 1.5f), false,
                        Main.rand.Next(6, 12), 0.04f, TrailColor,
                        new Vector2(1.3f, 0.25f), true, false, 0.8f));
                }
            }
            else
            {
                embedTimer++;

                // 插地后立刻生成土墙
                if (!wallSpawned && embedTimer == 1)
                {
                    SpawnWall();
                    wallSpawned = true;
                }

                // 已插地，计时消失
                if (embedTimer > 60)
                    Projectile.Kill();
            }
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            embedded = true;
            Projectile.velocity = Vector2.Zero;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            SoundEngine.PlaySound(SoundID.Item10 with { Volume = 0.85f, Pitch = -0.3f }, Projectile.Center);

            // 落地粒子
            if (!Main.dedServ)
            {
                for (int i = 0; i < 14; i++)
                {
                    Vector2 vel = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(3f, 8f);
                    Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.GoldFlame, vel, 0, TrailColor, 1.1f);
                    d.noGravity = true;
                }
            }
            return false;
        }

        private void SpawnWall()
        {
            if (Main.myPlayer != Projectile.owner) return;

            // 墙体中心：大剑插地点正上方 WallHalfHeight 处
            Vector2 wallCenter = Projectile.Center - new Vector2(0f, AegisWallProjectile.WallHalfHeight);

            Projectile.NewProjectile(Projectile.GetSource_FromThis(),
                wallCenter,
                Vector2.Zero,
                ModContent.ProjectileType<AegisWallProjectile>(),
                0, 0f, Projectile.owner);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.dedServ) return false;

            Texture2D tex = ModContent.Request<Texture2D>(Texture).Value;
            Vector2 origin = new Vector2(0f, tex.Height);
            Vector2 drawPos = Projectile.Center - Main.screenPosition + new Vector2(0f, Main.player[Projectile.owner].gfxOffY);

            // 插地后微微发光
            if (embedded)
            {
                Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
                float fade = System.MathF.Max(0f, 1f - embedTimer / 60f);
                Main.spriteBatch.SetBlendState(BlendState.Additive);
                Main.EntitySpriteDraw(bloom, drawPos, null, TrailColor * fade * 0.5f,
                    0f, bloom.Size() * 0.5f, 0.5f, SpriteEffects.None, 0);
                Main.spriteBatch.SetBlendState(BlendState.AlphaBlend);
            }

            Main.EntitySpriteDraw(tex, drawPos, null, lightColor,
                Projectile.rotation, origin, Projectile.scale, SpriteEffects.None, 0);
            return false;
        }
    }
}
