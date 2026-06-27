using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Dev.MK14EBR
{
    public class M61Bullet : ModProjectile, ILocalizedModType
    {
        private static readonly Color HelixGoldBright = new(255, 238, 110);
        private static readonly Color HelixGoldDeep = new(255, 168, 32);

        public new string LocalizationCategory => "Projectiles.MK14EBR";
        public override string Texture => "CalamityLegendsComeBack/Weapons/A_Dev/MK14EBR/M61Bullet";

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 12;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 0;
        }

        public override void SetDefaults()
        {
            Projectile.width = 8;
            Projectile.height = 4;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.tileCollide = true;
            Projectile.ignoreWater = true;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 600;
            Projectile.extraUpdates = 4;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 14;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Projectile.type].Value;
            Vector2 drawOrigin = new Vector2(texture.Width * 0.5f, texture.Height * 0.5f);

            // Draw custom trail
            for (int i = 0; i < Projectile.oldPos.Length; i++)
            {
                if (Projectile.oldPos[i] == Vector2.Zero) continue;

                float progress = (float)(Projectile.oldPos.Length - i) / Projectile.oldPos.Length;
                
                // Additive golden trail
                Color trailColor = Color.Lerp(HelixGoldDeep, HelixGoldBright, progress) * progress * 0.4f;
                trailColor.A = 0; 

                Vector2 trailDrawPos = Projectile.oldPos[i] + Projectile.Size * 0.5f - Main.screenPosition + new Vector2(0f, Projectile.gfxOffY);
                float scale = Projectile.scale * (0.5f + progress * 0.5f);

                Main.EntitySpriteDraw(texture, trailDrawPos, null, trailColor, Projectile.rotation, drawOrigin, scale, SpriteEffects.None, 0);
            }

            // Draw the main projectile centered
            Vector2 mainDrawPos = Projectile.Center - Main.screenPosition + new Vector2(0f, Projectile.gfxOffY);
            Main.EntitySpriteDraw(texture, mainDrawPos, null, lightColor, Projectile.rotation, drawOrigin, Projectile.scale, SpriteEffects.None, 0);

            return false;
        }

        public override void AI()
        {
            Projectile.rotation = Projectile.velocity.ToRotation();

            Projectile.localAI[0]++;
            if (Projectile.localAI[0] <= 2f)
                return;

            Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            Vector2 perpendicular = direction.RotatedBy(MathHelper.PiOver2);
            float phase = Projectile.localAI[0] * 0.15f;

            SpawnHelixStrand(direction, perpendicular, phase);
            SpawnHelixStrand(direction, perpendicular, phase + MathHelper.Pi);

            // Add center golden stream
            if (Main.rand.NextBool(2))
            {
                Dust centerDust = Dust.NewDustPerfect(
                    Projectile.Center,
                    DustID.YellowTorch,
                    -direction * Main.rand.NextFloat(0.5f, 2f),
                    100,
                    Color.Lerp(HelixGoldDeep, HelixGoldBright, Main.rand.NextFloat()),
                    Main.rand.NextFloat(0.5f, 0.9f));
                centerDust.noGravity = true;
            }

            // Add extra gold sparks
            if (Main.rand.NextBool(3))
            {
                Dust spark = Dust.NewDustPerfect(
                    Projectile.Center,
                    DustID.GoldCoin,
                    -direction.RotatedByRandom(0.2f) * Main.rand.NextFloat(1.5f, 4f),
                    0,
                    default,
                    Main.rand.NextFloat(0.4f, 0.7f));
                spark.noGravity = true;
                spark.velocity *= 0.6f;
            }
        }

        private void SpawnHelixStrand(Vector2 direction, Vector2 perpendicular, float phase)
        {
            float sinVal = (float)Math.Sin(phase);
            float depth = Math.Abs(sinVal);

            Dust strand = Dust.NewDustPerfect(
                Projectile.Center + perpendicular * sinVal * 7f,
                DustID.Torch,
                -direction * Main.rand.NextFloat(0.15f, 0.55f),
                0,
                Color.Lerp(HelixGoldDeep, HelixGoldBright, depth),
                Main.rand.NextFloat(0.42f, 0.72f) * (0.35f + depth * 0.65f));
            strand.noGravity = true;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.Electrified, 300);
        }

        public override void OnKill(int timeLeft)
        {
            // 1. 爆发一个向外高速扩散的金色圆环（清爽的爆裂基底）
            int ringCount = 12;
            for (int i = 0; i < ringCount; i++)
            {
                float angle = i * MathHelper.TwoPi / ringCount;
                Vector2 velocity = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle)) * Main.rand.NextFloat(3.5f, 5.5f);
                
                Dust ringDust = Dust.NewDustPerfect(
                    Projectile.Center,
                    DustID.YellowTorch,
                    velocity,
                    100,
                    Color.Lerp(HelixGoldDeep, HelixGoldBright, Main.rand.NextFloat()),
                    Main.rand.NextFloat(0.8f, 1.2f)
                );
                ringDust.noGravity = true;
            }

            // 2. 生成一个 4 臂的阿基米德螺旋线旋臂，呈现数学上的对称螺旋爆裂效果
            int arms = 4;
            int particlesPerArm = 6;
            for (int arm = 0; arm < arms; arm++)
            {
                float armAngle = arm * MathHelper.TwoPi / arms;
                for (int i = 0; i < particlesPerArm; i++)
                {
                    float t = (float)i / particlesPerArm;
                    
                    // 阿基米德螺旋线：角度随向外进度 t 偏转
                    float angle = armAngle + t * MathHelper.Pi * 1.2f;
                    // 速度外扩增强
                    float speed = 1.5f + t * 4f;
                    
                    Vector2 velocity = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle)) * speed;
                    Vector2 spawnPos = Projectile.Center + new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle)) * (t * 6f);
                    
                    Dust spiralDust = Dust.NewDustPerfect(
                        spawnPos,
                        DustID.GoldCoin,
                        velocity,
                        0,
                        default,
                        Main.rand.NextFloat(0.7f, 1.0f) * (1.1f - t * 0.4f)
                    );
                    spiralDust.noGravity = true;
                }
            }
        }
    }
}
