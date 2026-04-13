using CalamityMod;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SHPC.Effects.CPreMoodLord.MoonEvent
{
    public class FragmentStardust_Cell : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.SHPC";
        public override string Texture => "Terraria/Images/NPC_405";

        private bool isSmall = true;
        private int stateTimer = 0;

        private int loopCount => (int)Projectile.ai[0];
        private const int MaxLoop = 6;

        private int frame;
        private int frameTimer;

        private float wanderAngle;
        private int wanderTimer;

        public override void SetDefaults()
        {
            Projectile.width = 50;   // ← 修改
            Projectile.height = 50;  // ← 修改
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 600;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;

            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 18;
        }

        public override void OnSpawn(Terraria.DataStructures.IEntitySource source)
        {
            isSmall = true;
            stateTimer = 0;
            Projectile.ai[0] = 0;

            frame = 0;
            frameTimer = 0;

            wanderTimer = 0;
            wanderAngle = Main.rand.NextFloat(MathHelper.TwoPi);
        }

        public override bool? CanDamage()
        {
            return !isSmall;
        }

        public override void AI()
        {
            stateTimer++;
            wanderTimer++;

            // ===== 前5次循环：强制锁时间 =====
            if (loopCount < MaxLoop - 1)
            {
                Projectile.timeLeft = 150;
            }

            // ===== 粒子铺满碰撞体积 =====
            if (Main.rand.NextBool(2))
            {
                Vector2 randPos = Projectile.Center + new Vector2(
                    Main.rand.NextFloat(-Projectile.width / 2f, Projectile.width / 2f),
                    Main.rand.NextFloat(-Projectile.height / 2f, Projectile.height / 2f)
                );

                Dust d = Dust.NewDustPerfect(
                    randPos,
                    DustID.Electric,
                    Main.rand.NextVector2Circular(1.2f, 1.2f),
                    0,
                    Color.LightBlue,
                    1.2f
                );
                d.noGravity = true;
            }

            if (isSmall)
                SmallStateAI();
            else
                BigStateAI();

            UpdateFrame();
        }

        private void SmallStateAI()
        {
            NPC target = FindClosestNPC(400f);

            Vector2 desiredVelocity;

            if (target != null)
            {
                Vector2 away = (Projectile.Center - target.Center).SafeNormalize(Vector2.UnitX);
                float turnSign = (Projectile.whoAmI % 2 == 0) ? 1f : -1f;
                Vector2 tangent = away.RotatedBy((MathHelper.Pi / 2f) * turnSign);

                Vector2 desiredDir = (away * 0.84f + tangent * 0.32f).SafeNormalize(Vector2.UnitX);
                desiredVelocity = desiredDir * 5.6f;
            }
            else
            {
                if (wanderTimer >= 24)
                {
                    wanderTimer = 0;
                    wanderAngle += Main.rand.NextFloat(-0.75f, 0.75f);
                }

                Vector2 wanderDir = wanderAngle.ToRotationVector2();
                desiredVelocity = wanderDir * 2.8f;
            }

            Projectile.velocity = Vector2.Lerp(Projectile.velocity, desiredVelocity, 0.075f);
            Projectile.velocity *= 0.992f;

            if (stateTimer >= 120 && loopCount < MaxLoop)
                GrowToBig();
        }

        // ================= 大细胞（强化版） =================
        // ================= 大细胞（强化版） =================
        private void BigStateAI()
        {
            float maxSpeed = 22f;        // 你自己调上限
            float accel = 0.35f;         // 加速度

            NPC target = FindClosestNPC(2700f);

            if (target != null)
            {
                Vector2 currentDir = Projectile.velocity.SafeNormalize(Vector2.UnitX);
                Vector2 targetDir = (target.Center - Projectile.Center).SafeNormalize(Vector2.UnitX);

                // ===== 转向角限制：随时间逐渐减小，最终接近 0 =====
                float maxTurnAngle = MathHelper.Lerp(0.45f, 0f, Utils.GetLerpValue(0f, 90f, stateTimer, true));

                Vector2 newDir = currentDir;

                if (maxTurnAngle <= 0.001f)
                {
                    newDir = targetDir;
                }
                else
                {
                    float angleToTarget = currentDir.AngleBetween(targetDir);

                    if (angleToTarget <= maxTurnAngle)
                        newDir = targetDir;
                    else
                    {
                        float crossZ = currentDir.X * targetDir.Y - currentDir.Y * targetDir.X;
                        newDir = currentDir.RotatedBy(Math.Sign(crossZ) * maxTurnAngle);
                    }
                }

                Projectile.velocity = newDir * (Projectile.velocity.Length() + accel);

                if (Projectile.velocity.Length() > maxSpeed)
                    Projectile.velocity = Projectile.velocity.SafeNormalize(Vector2.UnitX) * maxSpeed;
            }
            else
            {
                // 没目标也持续加速（当前方向）
                if (Projectile.velocity.Length() < maxSpeed)
                    Projectile.velocity *= 1.04f;
            }
        }

        private void GrowToBig()
        {
            isSmall = false;
            stateTimer = 0;
            frame = 0;
            frameTimer = 0;

            for (int i = 0; i < 12; i++)
            {
                Dust d = Dust.NewDustPerfect(
                    Projectile.Center,
                    DustID.Electric,
                    Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(2f, 6f),
                    0,
                    Color.LightBlue,
                    1.4f
                );
                d.noGravity = true;
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            // ===== 反冲粒子 =====
            for (int i = 0; i < 10; i++)
            {
                Vector2 dir = (Projectile.Center - target.Center).SafeNormalize(Vector2.UnitX);
                Vector2 vel = dir.RotatedByRandom(0.6f) * Main.rand.NextFloat(2f, 6f);

                Dust d = Dust.NewDustPerfect(
                    Projectile.Center,
                    DustID.Electric,
                    vel,
                    0,
                    Color.LightBlue,
                    1.4f
                );
                d.noGravity = true;
            }

            // ===== 音效 =====
            SoundEngine.PlaySound(SoundID.Item94, Projectile.Center);

            if (!isSmall)
            {
                Projectile.ai[0]++;

                if (loopCount < MaxLoop)
                {
                    //Projectile.timeLeft = 150;
                }
                else
                {
                    return;
                }

                TurnBackToSmall(target);
            }
        }

        private void TurnBackToSmall(NPC target)
        {
            isSmall = true;
            stateTimer = 0;
            frame = 0;
            frameTimer = 0;

            Vector2 away = (Projectile.Center - target.Center).SafeNormalize(Vector2.UnitX);
            Vector2 tangent = away.RotatedBy(Main.rand.NextBool() ? (MathHelper.Pi / 3f) : -(MathHelper.Pi / 3f));
            Vector2 escapeDir = (away * 0.88f + tangent * 0.25f).SafeNormalize(Vector2.UnitX);

            Projectile.velocity = escapeDir * 5.2f;
        }

        private NPC FindClosestNPC(float maxDist)
        {
            NPC target = null;
            float dist = maxDist;

            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!npc.active || npc.friendly || npc.lifeMax <= 5 || npc.dontTakeDamage)
                    continue;

                float d = Vector2.Distance(Projectile.Center, npc.Center);
                if (d < dist)
                {
                    dist = d;
                    target = npc;
                }
            }

            return target;
        }

        private void UpdateFrame()
        {
            frameTimer++;

            if (frameTimer >= 6)
            {
                frameTimer = 0;
                frame++;
            }

            if (isSmall)
            {
                if (frame >= 2)
                    frame = 0;
            }
            else
            {
                if (frame >= 4)
                    frame = 0;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D tex = ModContent.Request<Texture2D>(
                isSmall ? "Terraria/Images/NPC_406" : "Terraria/Images/NPC_405"
            ).Value;

            int frameCount = isSmall ? 2 : 4;
            Rectangle frameRect = tex.Frame(1, frameCount, 0, frame);

            Vector2 origin = frameRect.Size() / 2f;
            Vector2 pos = Projectile.Center - Main.screenPosition;

            for (int i = 0; i < 4; i++)
            {
                Vector2 offset = new Vector2(2f, 0f).RotatedBy(i * (MathHelper.Pi / 2f));

                Main.EntitySpriteDraw(
                    tex,
                    pos + offset,
                    frameRect,
                    Color.LightBlue * 0.6f,
                    0f,
                    origin,
                    Projectile.scale,
                    SpriteEffects.None
                );
            }

            Main.EntitySpriteDraw(
                tex,
                pos,
                frameRect,
                Color.White,
                0f,
                origin,
                Projectile.scale,
                SpriteEffects.None
            );

            return false;
        }
    }
}