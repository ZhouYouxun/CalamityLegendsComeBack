using CalamityLegendsComeBack.Accssory.BF.Common;
using CalamityLegendsComeBack.Weapons.BlossomFlux;
using CalamityLegendsComeBack.Weapons.BlossomFlux.RightUI;
using CalamityMod.Buffs.DamageOverTime;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Accssory.BF.FairyDanceSeries
{
    internal sealed class FairyDanceWisp : ModProjectile
    {
        internal const int FairyCount = 3;
        // 每只仙灵两次突进之间的节拍；三只仙灵按 1/3 周期错开，形成连续的“舞”。
        internal const int AttackCadence = 24;
        private const float DashSpeed = 21f;
        private const float MaxDashDistance = 640f;
        // 单次突进的最长帧数，略短于 AttackCadence，确保突进结束后仙灵能赶上下一拍。
        private const int MaxDashFrames = 22;

        // Vanilla resources: Terraria/Images/NPC_583, NPC_584 and NPC_585.
        private static readonly string[] FairyTextures =
        {
            "Terraria/Images/NPC_583",
            "Terraria/Images/NPC_584",
            "Terraria/Images/NPC_585"
        };

        private ref float VariantValue => ref Projectile.ai[0];
        private ref float StateValue => ref Projectile.ai[1];
        private int Variant => Utils.Clamp((int)VariantValue, 0, FairyCount - 1);
        private int State { get => (int)StateValue; set => StateValue = value; }
        private int dashTimer;

        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetDefaults()
        {
            Projectile.width = 22;
            Projectile.height = 22;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 2;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 10;
            Projectile.ArmorPenetration = 10;
        }

        public override bool? CanDamage() => State == 1 ? null : false;

        public override void AI()
        {
            if (Projectile.owner < 0 || Projectile.owner >= Main.maxPlayers)
            {
                Projectile.Kill();
                return;
            }

            Player owner = Main.player[Projectile.owner];
            BFAccessoryPlayer accessoryPlayer = owner.GetModPlayer<BFAccessoryPlayer>();
            if (!owner.active || owner.dead || !accessoryPlayer.FairyDanceEquipped)
            {
                Projectile.Kill();
                return;
            }

            Projectile.timeLeft = 2;
            Lighting.AddLight(Projectile.Center, GetFairyColor(Variant).ToVector3() * 0.35f);

            Projectile.frameCounter++;
            if (Projectile.frameCounter >= 4)
            {
                Projectile.frameCounter = 0;
                Projectile.frame = (Projectile.frame + 1) % 4;
            }

            SpawnFairySparkles();

            switch (State)
            {
                case 1:
                    UpdateDash(owner);
                    break;
                default:
                    UpdateOrbit(owner);
                    break;
            }

            Projectile.rotation = Projectile.velocity.X * 0.1f;
        }

        internal void TriggerDash(Vector2 target, int baseDamage)
        {
            // The charged right-click release must recall and relaunch all three fairies
            // even if a normal 20/30-damage contact dash is still in progress.
            if (State == 1 && baseDamage < 120)
                return;

            Player owner = Main.player[Projectile.owner];
            Vector2 direction = (target - Projectile.Center).SafeNormalize(Vector2.UnitX * owner.direction);
            Projectile.velocity = direction * DashSpeed;
            Projectile.damage = Math.Max(1, (int)owner.GetTotalDamage(DamageClass.Ranged).ApplyTo(baseDamage));
            Projectile.CritChance = 0;
            dashTimer = 0;
            State = 1;
            Projectile.netUpdate = true;

            SpawnDashBurst(direction);
        }

        private void UpdateOrbit(Player owner)
        {
            float angle = Main.GameUpdateCount * 0.025f + MathHelper.TwoPi * Variant / FairyCount;
            Vector2 desiredPosition = owner.Center + new Vector2(72f, 0f).RotatedBy(angle) + Vector2.UnitY * (float)Math.Sin(angle * 1.7f) * 12f;
            MoveTowards(desiredPosition, 0.18f, 12f);
            Projectile.damage = 0;
        }

        private void UpdateDash(Player owner)
        {
            dashTimer++;
            if (dashTimer >= MaxDashFrames || Vector2.DistanceSquared(Projectile.Center, owner.Center) >= MaxDashDistance * MaxDashDistance)
            {
                // 突进结束直接回到环绕态：惯性会把它往玩家方向带一点，但不再强制先飞回玩家身边——
                // 下一拍会从半途重新突进，配合三只仙灵错开的节拍形成连续攻击。
                State = 0;
                Projectile.damage = 0;
                Projectile.netUpdate = true;
            }
        }

        private void MoveTowards(Vector2 destination, float inertia, float maxSpeed)
        {
            Vector2 desiredVelocity = (destination - Projectile.Center) * inertia;
            if (desiredVelocity.LengthSquared() > maxSpeed * maxSpeed)
                desiredVelocity = desiredVelocity.SafeNormalize(Vector2.Zero) * maxSpeed;

            Projectile.velocity = Vector2.Lerp(Projectile.velocity, desiredVelocity, 0.24f);
        }

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers) => modifiers.DisableCrit();

        // ===== 原版三色精灵同款拖尾（NPC.cs AI_112：FireworksRGB 双色渐变微尘）+ 灾厄辉光球 =====
        private void SpawnFairySparkles()
        {
            if (Main.dedServ)
                return;

            Color mainColor = GetFairyColor(Variant);
            Color accentColor = GetFairyAccentColor(Variant);
            bool dashing = State == 1;

            if (dashing || (int)Main.timeForVisualEffects % 2 == 0)
            {
                Dust dust = Dust.NewDustDirect(
                    Projectile.Center - new Vector2(4f) * 0.5f, 8, 8,
                    DustID.FireworksRGB, 0f, 0f, 200,
                    Color.Lerp(mainColor, accentColor, Main.rand.NextFloat()),
                    dashing ? 1.05f : 0.65f);
                dust.velocity *= 0f;
                dust.velocity += Projectile.velocity * (dashing ? 0.55f : 0.3f);
                dust.noGravity = true;
                dust.noLight = true;
            }

            if (dashing ? Main.rand.NextBool(2) : Main.rand.NextBool(9))
            {
                GeneralParticleHandler.SpawnParticle(new GlowOrbParticle(
                    Projectile.Center + Main.rand.NextVector2Circular(8f, 8f),
                    dashing
                        ? -Projectile.velocity * Main.rand.NextFloat(0.08f, 0.2f)
                        : Main.rand.NextVector2Circular(0.7f, 0.7f) - Vector2.UnitY * 0.6f,
                    false,
                    Main.rand.Next(16, 28),
                    Main.rand.NextFloat(0.22f, 0.4f) * (dashing ? 1.35f : 1f),
                    Color.Lerp(mainColor, accentColor, Main.rand.NextFloat(0.5f)),
                    true, false, true));
            }
        }

        // ===== 突进启动爆点：仿原版 NPC.FairyEffects 的加速尘环，缩小规模按方向喷出 =====
        private void SpawnDashBurst(Vector2 direction)
        {
            if (Main.dedServ)
                return;

            Color mainColor = GetFairyColor(Variant);
            Color accentColor = GetFairyAccentColor(Variant);

            for (int i = 0; i < 14; i++)
            {
                Dust dust = Dust.NewDustDirect(
                    Projectile.Center - new Vector2(4f), 8, 8,
                    DustID.FireworksRGB, 0f, 0f, 200,
                    Color.Lerp(mainColor, accentColor, Main.rand.NextFloat()), 0.85f);
                dust.velocity = dust.velocity * 1.5f + direction * Main.rand.NextFloat(1.5f, 5f);
                dust.fadeIn = Main.rand.Next(0, 17) * 0.1f;
                dust.noGravity = true;
            }

            for (int i = 0; i < 3; i++)
            {
                GeneralParticleHandler.SpawnParticle(new GlowOrbParticle(
                    Projectile.Center,
                    direction.RotatedByRandom(0.5f) * Main.rand.NextFloat(2f, 5f),
                    false,
                    Main.rand.Next(18, 28),
                    Main.rand.NextFloat(0.3f, 0.5f),
                    Color.Lerp(mainColor, Color.White, 0.25f),
                    true, false, true));
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = ModContent.Request<Texture2D>(FairyTextures[Variant]).Value;
            Rectangle frame = texture.Frame(1, 4, 0, Projectile.frame);
            SpriteEffects effects = Projectile.velocity.X < 0f ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            Vector2 origin = frame.Size() * 0.5f;
            Color drawColor = Color.Lerp(Projectile.GetAlpha(lightColor), Color.White, 0.35f);

            // ===== 颜色包边：本体色 A=0 加法描边，环绕呼吸、突进时增强 =====
            bool dashing = State == 1;
            float outlinePulse = 0.5f + 0.5f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 3.4f + Projectile.identity * 0.7f);
            float outlineOffset = dashing ? 3.2f : 1.8f + outlinePulse * 0.8f;
            Color outlineColor = (GetFairyColor(Variant) with { A = 0 }) * (dashing ? 0.85f : 0.5f + outlinePulse * 0.15f);

            for (int i = 0; i < 4; i++)
            {
                Vector2 offset = (MathHelper.PiOver2 * i + Main.GlobalTimeWrappedHourly * 2.6f).ToRotationVector2() * outlineOffset;
                Main.EntitySpriteDraw(
                    texture,
                    drawPosition + offset,
                    frame,
                    outlineColor,
                    Projectile.rotation,
                    origin,
                    Projectile.scale,
                    effects);
            }

            Main.EntitySpriteDraw(
                texture,
                drawPosition,
                frame,
                drawColor,
                Projectile.rotation,
                origin,
                Projectile.scale,
                effects);
            return false;
        }

        // 原版 NPC.cs HitEffect / AI_112 的官方配色：粉/绿/蓝 各带一个浅色副色
        private static Color GetFairyColor(int variant) => variant switch
        {
            1 => Color.LimeGreen,
            2 => Color.RoyalBlue,
            _ => Color.HotPink
        };

        private static Color GetFairyAccentColor(int variant) => variant switch
        {
            1 => Color.LightSeaGreen,
            2 => Color.LightBlue,
            _ => Color.LightPink
        };

    }

    internal sealed class RainbowSpiritLacewing : ModProjectile
    {
        internal const int LifetimeFrames = 18 * 60;
        internal const int MaximumCount = 7;
        private const float DashSpeed = 30f;
        private const string LacewingTexture = "Terraria/Images/NPC_661";

        private int State { get => (int)Projectile.ai[1]; set => Projectile.ai[1] = value; }
        private int shootTimer;
        private double animationCounter;
        private bool wasRightHeld;

        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetDefaults()
        {
            Projectile.width = 24;
            Projectile.height = 24;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = LifetimeFrames;
        }

        public override bool? CanDamage() => State == 1 ? null : false;

        public override void AI()
        {
            if (Projectile.owner < 0 || Projectile.owner >= Main.maxPlayers)
            {
                Projectile.Kill();
                return;
            }

            Player owner = Main.player[Projectile.owner];
            BFAccessoryPlayer accessoryPlayer = owner.GetModPlayer<BFAccessoryPlayer>();
            if (!owner.active || owner.dead || !accessoryPlayer.RainbowSpiritDanceEquipped)
            {
                Projectile.Kill();
                return;
            }

            UpdateAnimation();
            // 原版七彩草蛉的灯光公式：hslToRgb(时间*0.33) * 0.3 + 0.1（NPC.cs type==661）
            Lighting.AddLight(Projectile.Center, GetRainbowColor().ToVector3() * 0.3f + Vector3.One * 0.1f);
            SpawnLacewingSparkles();

            if (State == 1)
            {
                if (Vector2.DistanceSquared(Projectile.Center, owner.Center) > 1400f * 1400f)
                    Projectile.Kill();
                return;
            }

            bool localOwner = Projectile.owner == Main.myPlayer;
            BFRightUIPlayer input = owner.GetModPlayer<BFRightUIPlayer>();
            bool rightHeld = localOwner && accessoryPlayer.HoldingBlossomFlux && input.RightMouseHeld;
            float orbitSpeed = rightHeld ? 0.17f : 0.035f;
            float angle = Main.GameUpdateCount * orbitSpeed + MathHelper.TwoPi * ((int)Projectile.ai[0] % MaximumCount) / MaximumCount;
            float radius = rightHeld ? 82f : 98f;
            Vector2 desiredPosition = owner.Center + new Vector2(radius, 0f).RotatedBy(angle);
            Projectile.velocity = Vector2.Lerp(Projectile.velocity, (desiredPosition - Projectile.Center) * 0.24f, rightHeld ? 0.34f : 0.2f);
            if (Projectile.velocity.LengthSquared() > 22f * 22f)
                Projectile.velocity = Projectile.velocity.SafeNormalize(Vector2.Zero) * 22f;

            if (localOwner && wasRightHeld && !rightHeld)
            {
                ReleaseTowards(Main.MouseWorld);
                return;
            }

            wasRightHeld = rightHeld;
            if (rightHeld)
            {
                shootTimer = 0;
                return;
            }

            bool leftHeld = localOwner && accessoryPlayer.HoldingBlossomFlux && Main.mouseLeft && !owner.mouseInterface && !Main.blockMouse;
            if (!leftHeld)
            {
                shootTimer = 0;
                return;
            }

            shootTimer++;
            if (shootTimer >= 45)
            {
                shootTimer = 0;
                FireRainbowBolt(owner, Main.MouseWorld);
            }
        }

        private void ReleaseTowards(Vector2 target)
        {
            Player owner = Main.player[Projectile.owner];
            Projectile.velocity = (target - Projectile.Center).SafeNormalize(Vector2.UnitX * owner.direction) * DashSpeed;
            Projectile.damage = Math.Max(1, (int)owner.GetTotalDamage(DamageClass.Ranged).ApplyTo(1200f));
            Projectile.penetrate = 1;
            State = 1;
            Projectile.netUpdate = true;

            SpawnReleaseBurst(Projectile.velocity.SafeNormalize(Vector2.UnitX * owner.direction));
        }

        // ===== 释放突进的爆点：整圈彩虹尘环 + 辉光球沿突进方向喷出 =====
        private void SpawnReleaseBurst(Vector2 direction)
        {
            if (Main.dedServ)
                return;

            GeneralParticleHandler.SpawnParticle(new StrongBloom(
                Projectile.Center, Vector2.Zero,
                GetRainbowColor() with { A = 0 }, 0.5f, 12));

            for (int i = 0; i < 24; i++)
            {
                // 原版死亡爆点同款：按序号扫过整个色环（NPC.cs type==661 HitEffect）
                Color ringColor = Main.hslToRgb(i / 24f, 1f, 0.5f) * 0.5f;
                Vector2 ringVelocity = (MathHelper.TwoPi * i / 24f).ToRotationVector2() * Main.rand.NextFloat(1.5f, 4f) + direction * 2f;
                Dust dust = Dust.NewDustPerfect(Projectile.Center, DustID.RainbowMk2, ringVelocity, 0, ringColor, 0.5f);
                dust.noGravity = true;
                dust.fadeIn = 0.7f + Main.rand.NextFloat() * 1.1f;
            }

            for (int i = 0; i < 5; i++)
            {
                GeneralParticleHandler.SpawnParticle(new GlowOrbParticle(
                    Projectile.Center,
                    direction.RotatedByRandom(0.6f) * Main.rand.NextFloat(2.5f, 6f),
                    false,
                    Main.rand.Next(18, 30),
                    Main.rand.NextFloat(0.3f, 0.52f),
                    GetRainbowColor(i / 5f * 0.4f),
                    true, false, true));
            }
        }

        // ===== 原版七彩草蛉同款微尘（RainbowMk2 半亮彩虹 + 白色半尺寸克隆）+ 灾厄辉光球 =====
        private void SpawnLacewingSparkles()
        {
            if (Main.dedServ)
                return;

            bool dashing = State == 1;

            if (dashing || Main.rand.NextBool(3))
            {
                Color sparkleColor = GetRainbowColor(Main.rand.NextFloat(0.08f)) * 0.5f;
                Dust dust = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.RainbowMk2, 0f, 0f, 0, sparkleColor);
                dust.position = Projectile.Center + Main.rand.NextVector2Circular(Projectile.width, Projectile.height);
                dust.velocity *= Main.rand.NextFloat() * 0.8f;
                dust.velocity += Projectile.velocity * (dashing ? 0.8f : 0.6f);
                dust.noGravity = true;
                dust.fadeIn = 0.6f + Main.rand.NextFloat() * 0.7f;
                dust.scale = dashing ? 0.5f : 0.35f;

                Dust cloneDust = Dust.CloneDust(dust);
                cloneDust.scale /= 2f;
                cloneDust.fadeIn *= 0.85f;
                cloneDust.color = new Color(255, 255, 255, 255) * 0.5f;
            }

            if (dashing ? Main.rand.NextBool(2) : Main.rand.NextBool(10))
            {
                GeneralParticleHandler.SpawnParticle(new GlowOrbParticle(
                    Projectile.Center + Main.rand.NextVector2Circular(10f, 10f),
                    dashing
                        ? -Projectile.velocity * Main.rand.NextFloat(0.06f, 0.16f)
                        : Main.rand.NextVector2Circular(0.6f, 0.6f) - Vector2.UnitY * 0.5f,
                    false,
                    Main.rand.Next(16, 28),
                    Main.rand.NextFloat(0.24f, 0.42f) * (dashing ? 1.4f : 1f),
                    GetRainbowColor(Main.rand.NextFloat(0.15f)),
                    true, false, true));
            }
        }

        // 原版色环公式（hslToRgb(时间*0.33, 1, 0.5)），identity 相位让 7 只草蛉铺满七彩
        private Color GetRainbowColor(float extraOffset = 0f) =>
            Main.hslToRgb((Main.GlobalTimeWrappedHourly * 0.33f + Projectile.identity * 0.13f + extraOffset) % 1f, 1f, 0.5f);

        private void FireRainbowBolt(Player owner, Vector2 target)
        {
            Vector2 velocity = (target - Projectile.Center).SafeNormalize(Vector2.UnitX * owner.direction) * 28f;
            int damage = Math.Max(1, (int)owner.GetTotalDamage(DamageClass.Ranged).ApplyTo(135f));
            int index = Projectile.NewProjectile(
                Projectile.GetSource_FromThis(),
                Projectile.Center,
                velocity,
                // 光之女皇武器的原版直线彩虹弹：931 的原版绘制会调用 GetFairyQueenWeaponsColor，
                // 而非首领 873 的普通 HSL 色环。
                ProjectileID.FairyQueenMagicItemShot,
                damage,
                1f,
                Projectile.owner,
                owner.whoAmI,
                Main.rand.NextFloat());

            if (index >= 0 && index < Main.maxProjectiles)
            {
                Projectile rainbowStreak = Main.projectile[index];
                rainbowStreak.friendly = true;
                rainbowStreak.hostile = false;
                rainbowStreak.DamageType = DamageClass.Ranged;
                rainbowStreak.penetrate = 1;
                rainbowStreak.CritChance = (int)Math.Round(owner.GetTotalCritChance(DamageClass.Ranged));
                rainbowStreak.netUpdate = true;
            }
        }

        private void UpdateAnimation()
        {
            animationCounter += 1d + (Math.Abs(Projectile.velocity.X) + Math.Abs(Projectile.velocity.Y)) * 0.5d;
            const int frameInterval = 7;
            if (animationCounter < frameInterval)
                Projectile.frame = 0;
            else if (animationCounter < frameInterval * 2)
                Projectile.frame = 1;
            else if (animationCounter < frameInterval * 3)
                Projectile.frame = 2;
            else
                Projectile.frame = 1;

            if (animationCounter >= frameInterval * 4 - 1)
                animationCounter = 0d;
        }

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            if (State == 1)
                modifiers.SetCrit();
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = ModContent.Request<Texture2D>(LacewingTexture).Value;
            Rectangle frame = texture.Frame(1, 3, 0, Projectile.frame);
            SpriteEffects effects = Projectile.velocity.X < 0f ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            Vector2 origin = frame.Size() * 0.5f;

            // ===== 颜色包边：原版色环 A=0 加法描边，旋转呼吸、突进时增强 =====
            bool dashing = State == 1;
            float outlinePulse = 0.5f + 0.5f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 3.1f + Projectile.identity * 0.9f);
            float outlineOffset = dashing ? 3.5f : 2f + outlinePulse * 0.9f;
            Color outlineColor = (GetRainbowColor() with { A = 0 }) * (dashing ? 0.9f : 0.55f + outlinePulse * 0.15f);

            // ===== 草蛉发光绘制：本体后方叠一层彩虹 BloomCircle 光晕（A=0 防黑底），突进时更亮更大 =====
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            float glowPulse = 0.85f + 0.15f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 4.2f + Projectile.identity);
            Main.EntitySpriteDraw(bloom, drawPosition, null, (GetRainbowColor() with { A = 0 }) * ((dashing ? 0.85f : 0.6f) * glowPulse), 0f, bloom.Size() * 0.5f, (dashing ? 0.52f : 0.4f) * glowPulse, SpriteEffects.None, 0);
            Main.EntitySpriteDraw(bloom, drawPosition, null, (Color.Lerp(GetRainbowColor(0.5f), Color.White, 0.4f) with { A = 0 }) * (0.4f * glowPulse), 0f, bloom.Size() * 0.5f, (dashing ? 0.28f : 0.2f) * glowPulse, SpriteEffects.None, 0);

            for (int i = 0; i < 6; i++)
            {
                Vector2 offset = (MathHelper.TwoPi * i / 6f + Main.GlobalTimeWrappedHourly * 3f).ToRotationVector2() * outlineOffset;
                Main.EntitySpriteDraw(texture, drawPosition + offset, frame, outlineColor, Projectile.rotation, origin, Projectile.scale, effects);
            }

            // 彩虹光晕底衬（A=0 防黑底）+ 白色本体
            Main.EntitySpriteDraw(texture, drawPosition, frame, (GetRainbowColor() with { A = 0 }) * 0.45f, Projectile.rotation, origin, Projectile.scale * 1.12f, effects);
            Main.EntitySpriteDraw(texture, drawPosition, frame, Color.White, Projectile.rotation, origin, Projectile.scale, effects);
            return false;
        }
    }

    internal sealed class RainbowSpiritBolt : ModProjectile
    {
        private const float MaximumRange = 35f * 16f;

        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetDefaults()
        {
            Projectile.width = 12;
            Projectile.height = 12;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.penetrate = 1;
            Projectile.tileCollide = true;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 60;
            Projectile.extraUpdates = 1;
        }

        public override void AI()
        {
            Vector2 origin = new(Projectile.ai[0], Projectile.ai[1]);
            if (Vector2.DistanceSquared(Projectile.Center, origin) >= MaximumRange * MaximumRange)
            {
                Projectile.Kill();
                return;
            }

            Projectile.rotation = Projectile.velocity.ToRotation();
            Color color = Main.hslToRgb((float)(Main.GlobalTimeWrappedHourly * 0.6f + Projectile.identity * 0.17f) % 1f, 1f, 0.65f);
            Lighting.AddLight(Projectile.Center, color.ToVector3() * 0.55f);
            if (Main.rand.NextBool(3))
            {
                Dust dust = Dust.NewDustPerfect(Projectile.Center, DustID.RainbowMk2, -Projectile.velocity * 0.04f, 100, color, 0.8f);
                dust.noGravity = true;
                dust.fadeIn = 0.5f + Main.rand.NextFloat() * 0.6f;
            }

            // 灾厄辉光球：弹道上零星洒落的小彩虹光珠
            if (!Main.dedServ && Main.rand.NextBool(7))
            {
                GeneralParticleHandler.SpawnParticle(new GlowOrbParticle(
                    Projectile.Center,
                    -Projectile.velocity * Main.rand.NextFloat(0.02f, 0.08f),
                    false,
                    Main.rand.Next(12, 20),
                    Main.rand.NextFloat(0.16f, 0.28f),
                    color,
                    true, false, true));
            }
        }

        public override void OnKill(int timeLeft)
        {
            if (Main.dedServ)
                return;

            // 命中/消散爆点：小圈彩虹尘 + 一颗辉光球
            Color color = Main.hslToRgb((float)(Main.GlobalTimeWrappedHourly * 0.6f + Projectile.identity * 0.17f) % 1f, 1f, 0.65f);
            for (int i = 0; i < 8; i++)
            {
                Color ringColor = Main.hslToRgb(i / 8f, 1f, 0.5f) * 0.6f;
                Dust dust = Dust.NewDustPerfect(
                    Projectile.Center,
                    DustID.RainbowMk2,
                    (MathHelper.TwoPi * i / 8f).ToRotationVector2() * Main.rand.NextFloat(1.2f, 3f),
                    0, ringColor, 0.55f);
                dust.noGravity = true;
                dust.fadeIn = 0.5f + Main.rand.NextFloat() * 0.6f;
            }

            GeneralParticleHandler.SpawnParticle(new GlowOrbParticle(
                Projectile.Center, Vector2.Zero, false, 14,
                Main.rand.NextFloat(0.3f, 0.42f), color, true, false, true));
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(ModContent.BuffType<HolyFlames>(), 180);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D pixel = TextureAssets.MagicPixel.Value;
            Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            Color color = Main.hslToRgb((float)(Main.GlobalTimeWrappedHourly * 0.6f + Projectile.identity * 0.17f) % 1f, 1f, 0.65f);
            // 颜色包边：外层更宽的 A=0 加法光边damage
            Main.EntitySpriteDraw(pixel, Projectile.Center - Main.screenPosition - direction * 21f, null, (color with { A = 0 }) * 0.55f, Projectile.rotation, new Vector2(0f, 0.5f), new Vector2(42f, 7f), SpriteEffects.None);
            Main.EntitySpriteDraw(pixel, Projectile.Center - Main.screenPosition - direction * 18f, null, color * 0.8f, Projectile.rotation, new Vector2(0f, 0.5f), new Vector2(36f, 4f), SpriteEffects.None);
            Main.EntitySpriteDraw(pixel, Projectile.Center - Main.screenPosition - direction * 10f, null, Color.White, Projectile.rotation, new Vector2(0f, 0.5f), new Vector2(20f, 1.5f), SpriteEffects.None);
            return false;
        }
    }
}
