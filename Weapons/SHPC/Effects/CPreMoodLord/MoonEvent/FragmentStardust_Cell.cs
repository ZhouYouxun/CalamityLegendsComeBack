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

        public const int MaxActiveCells = 18;
        private const int GrowTime = 90;
        private const int MaxSplitDepth = 5;

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
            frame = 0;
            frameTimer = 0;

            wanderTimer = 0;
            wanderAngle = Main.rand.NextFloat(MathHelper.TwoPi);

            Projectile.ai[0] = MathHelper.Clamp((int)Projectile.ai[0], 0, MaxSplitDepth);
        }

        public override bool? CanDamage()
        {
            return !isSmall;
        }

        public override void AI()
        {
            stateTimer++;
            wanderTimer++;


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

            if (stateTimer >= GrowTime)
                GrowToBig();
        }

        // ================= 大细胞（强化版） =================
        // ================= 大细胞（强化版） =================
        private void BigStateAI()
        {
            float maxSpeed = 39f;        // 你自己调上限
            float accel = 0.45f;         // 加速度

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
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            // ===== 反冲粒子 =====
            for (int i = 0; i < 0; i++)
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
                if (Projectile.owner == Main.myPlayer)
                    SpawnChildCells(target);

                Projectile.Kill();
            }
        }

        public static int ActiveCellCount()
        {
            int count = 0;
            int type = ModContent.ProjectileType<FragmentStardust_Cell>();

            foreach (Projectile projectile in Main.ActiveProjectiles)
            {
                if (projectile.type == type)
                    count++;
            }

            return count;
        }

        private static int GetSplitCountForActiveCells(int activeCellCount)
        {
            if (activeCellCount <= 3)
                return 4;

            if (activeCellCount <= 6)
                return 3;

            if (activeCellCount <= 9)
                return 2;

            return 1;
        }

        private void SpawnChildCells(NPC target)
        {
            int splitDepth = (int)Projectile.ai[0];
            if (splitDepth >= MaxSplitDepth)
                return;

            int activeCellCount = ActiveCellCount();
            int desiredChildCount = GetSplitCountForActiveCells(activeCellCount);
            int availableSlotsAfterThisCellDies = MaxActiveCells - System.Math.Max(0, activeCellCount - 1);
            int childCount = System.Math.Min(desiredChildCount, availableSlotsAfterThisCellDies);

            if (childCount <= 0)
                return;

            Vector2 away = (Projectile.Center - target.Center).SafeNormalize(Vector2.UnitX);
            float baseRotation = away.ToRotation() + Main.rand.NextFloat(-0.18f, 0.18f);

            for (int i = 0; i < childCount; i++)
            {
                float spread = childCount == 1 ? 0f : MathHelper.Lerp(-0.78f, 0.78f, i / (float)(childCount - 1));
                Vector2 direction = (baseRotation + spread).ToRotationVector2();
                int childIndex = Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    Projectile.Center + direction * Main.rand.NextFloat(10f, 20f),
                    direction * Main.rand.NextFloat(4.2f, 6.4f),
                    ModContent.ProjectileType<FragmentStardust_Cell>(),
                    (int)(Projectile.damage * 1f),
                    Projectile.knockBack,
                    Projectile.owner,
                    splitDepth + 1);

                if (Main.projectile.IndexInRange(childIndex))
                {
                    Projectile child = Main.projectile[childIndex];
                    child.scale = Projectile.scale;
                    child.timeLeft = 600;
                    child.netUpdate = true;
                }
            }
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

            // 1. Calculate growth progress
            float progress = isSmall ? MathHelper.Clamp(stateTimer / (float)GrowTime, 0f, 1f) : 1f;

            // 2. Interpolate backlight properties based on progress
            // Start: Pale Cyan/Sky Blue
            Color startColor = new Color(120, 210, 255);
            // End: Deep Stardust Violet/Pink
            Color endColor = new Color(240, 100, 255);
            Color baseColor = Color.Lerp(startColor, endColor, progress);

            // Dynamic color shift using a sine wave
            Color dynamicColor = Color.Lerp(baseColor, new Color(130, 255, 230), (float)Math.Sin(Main.GlobalTimeWrappedHourly * 6.5f) * 0.25f + 0.25f);

            // Fluctuation/vibration parameters: high frequency when small, majestic pulse when large
            float waveSpeed = MathHelper.Lerp(28f, 7f, progress);
            float waveAmp = MathHelper.Lerp(0.6f, 2.2f, progress);
            float baseOffset = MathHelper.Lerp(2.8f, 7.5f, progress);
            
            float currentFluctuation = (float)Math.Sin(Main.GlobalTimeWrappedHourly * waveSpeed) * waveAmp;
            float chargeOffset = baseOffset + currentFluctuation;

            float baseOpacity = MathHelper.Lerp(0.45f, 0.85f, progress);
            float pulseOpacity = baseOpacity + (float)Math.Cos(Main.GlobalTimeWrappedHourly * waveSpeed * 0.5f) * 0.12f;

            // 3. Draw Backlight (Additive)
            Main.spriteBatch.SetBlendState(BlendState.Additive);
            
            int numDraws = 16;
            for (int i = 0; i < numDraws; i++)
            {
                float angle = MathHelper.TwoPi * i / (float)numDraws;
                Vector2 drawOffset = angle.ToRotationVector2() * chargeOffset;

                // Alternate colors around the circle for a rich celestial texture
                Color backlightColor = (i % 2 == 0) ? dynamicColor : new Color(180, 240, 255);
                backlightColor = backlightColor * (pulseOpacity * (i % 2 == 0 ? 0.65f : 0.45f));
                backlightColor.A = 0; // Ensure transparent center/glow effect

                Main.EntitySpriteDraw(
                    tex,
                    pos + drawOffset,
                    frameRect,
                    backlightColor,
                    0f,
                    origin,
                    Projectile.scale,
                    SpriteEffects.None
                );
            }

            // Draw a soft bloom circle behind the cell for the extra glow factor
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Color bloomColor = dynamicColor * (pulseOpacity * 0.35f);
            bloomColor.A = 0;
            Main.EntitySpriteDraw(
                bloom,
                pos,
                null,
                bloomColor,
                0f,
                bloom.Size() * 0.5f,
                Projectile.scale * MathHelper.Lerp(0.24f, 0.55f, progress),
                SpriteEffects.None
            );

            // 4. Draw Main Body (AlphaBlend)
            Main.spriteBatch.SetBlendState(BlendState.AlphaBlend);
            
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
