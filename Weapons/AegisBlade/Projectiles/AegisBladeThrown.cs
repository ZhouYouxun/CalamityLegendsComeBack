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
    // 蓄力完成后按左键触发：刀刃垂直插入地面，并在两侧升起伤害性土墙。
    // 类似灾劫星史一阶段的雷霆大坐效果。
    public class AegisBladeThrown : ModProjectile
    {
        public override string Texture => "CalamityLegendsComeBack/Weapons/AegisBlade/AegisBlade";

        private bool embedded   = false;
        private bool wallSpawned = false;
        private int  embedTimer  = 0;

        private static readonly Color FlashColor = new(255, 220, 80, 0);
        private static readonly Color DustColor  = new(255, 200, 60, 0);

        public override void SetDefaults()
        {
            Projectile.width  = Projectile.height = 24;
            Projectile.friendly    = true;
            Projectile.DamageType  = DamageClass.Melee;
            Projectile.tileCollide = true;
            Projectile.penetrate   = -1;
            Projectile.timeLeft    = 180;
            Projectile.usesLocalNPCImmunity   = true;
            Projectile.localNPCHitCooldown    = 8;
            Projectile.ignoreWater = true;
        }

        public override void AI()
        {
            if (!embedded)
            {
                // 垂直下落，刀尖朝下
                Projectile.rotation = MathHelper.PiOver2 + MathHelper.PiOver4;

                // 轻微旋转加速感
                Projectile.velocity.Y = System.MathF.Min(Projectile.velocity.Y + 0.8f, 30f);

                // 下落轨迹粒子
                if (!Main.dedServ && Main.rand.NextBool(2))
                {
                    Vector2 back = Projectile.Center - Vector2.UnitY * (-30f);
                    GeneralParticleHandler.SpawnParticle(new GlowSparkParticle(
                        back + Main.rand.NextVector2Circular(6f, 6f),
                        Main.rand.NextVector2Circular(1f, 1f), false,
                        Main.rand.Next(5, 10), 0.035f, FlashColor,
                        new Vector2(0.3f, 1.5f), true, false, 0.85f));
                }
            }
            else
            {
                embedTimer++;

                // 插地后立即生成双侧土墙
                if (!wallSpawned && embedTimer == 1)
                {
                    SpawnWalls();
                    wallSpawned = true;
                }

                // 插地后停留短暂时间再消失
                if (embedTimer > 40)
                    Projectile.Kill();
            }
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            embedded = true;
            Projectile.velocity    = Vector2.Zero;
            Projectile.tileCollide = false;
            Projectile.timeLeft    = 60;

            SoundEngine.PlaySound(SoundID.Item10 with { Volume = 1.0f, Pitch = -0.4f }, Projectile.Center);

            if (!Main.dedServ)
            {
                // 插地爆炸尘
                for (int i = 0; i < 22; i++)
                {
                    Vector2 vel = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(4f, 12f);
                    vel.Y = -System.MathF.Abs(vel.Y); // 向上喷出
                    Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.GoldFlame, vel, 0, DustColor, 1.4f);
                    d.noGravity = true;
                }
                // 圆形冲击波
                GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(
                    Projectile.Center, Vector2.Zero, FlashColor,
                    new Vector2(1.4f, 0.5f), 0f, 0.07f, 1.2f, 16));
            }
            return false;
        }

        private void SpawnWalls()
        {
            if (Main.myPlayer != Projectile.owner) return;

            int wallType    = ModContent.ProjectileType<AegisWallProjectile>();
            int halfH       = AegisWallProjectile.WallHalfHeight;
            int spread      = BalanceAegisBlade.WallSpreadPixels;
            float riseSpeed = halfH / (float)BalanceAegisBlade.WallRiseTime;

            int wallDamage = (int)(Projectile.damage * 0.8f);

            // 左墙（从插地点地面向上升起）
            Vector2 leftSpawn = new Vector2(Projectile.Center.X - spread, Projectile.Center.Y);
            Projectile.NewProjectile(Projectile.GetSource_FromThis(),
                leftSpawn, new Vector2(0f, -riseSpeed),
                wallType, wallDamage, 4f, Projectile.owner, -1f);

            // 右墙
            Vector2 rightSpawn = new Vector2(Projectile.Center.X + spread, Projectile.Center.Y);
            Projectile.NewProjectile(Projectile.GetSource_FromThis(),
                rightSpawn, new Vector2(0f, -riseSpeed),
                wallType, wallDamage, 4f, Projectile.owner, 1f);

            SoundEngine.PlaySound(SoundID.Item14 with { Volume = 0.7f, Pitch = -0.5f }, Projectile.Center);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.dedServ) return false;

            Texture2D tex    = ModContent.Request<Texture2D>(Texture).Value;
            Vector2 origin   = new Vector2(0f, tex.Height);
            Vector2 drawPos  = Projectile.Center - Main.screenPosition;

            if (embedded)
            {
                // 插地后渐隐光晕
                Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
                float fade = System.MathF.Max(0f, 1f - embedTimer / 40f);
                Main.spriteBatch.SetBlendState(BlendState.Additive);
                Main.EntitySpriteDraw(bloom, drawPos, null,
                    FlashColor * fade * 0.6f, 0f, bloom.Size() * 0.5f, 0.6f, SpriteEffects.None, 0);
                Main.spriteBatch.SetBlendState(BlendState.AlphaBlend);
            }

            Main.EntitySpriteDraw(tex, drawPos, null, lightColor,
                Projectile.rotation, origin, Projectile.scale, SpriteEffects.None, 0);
            return false;
        }
    }
}
