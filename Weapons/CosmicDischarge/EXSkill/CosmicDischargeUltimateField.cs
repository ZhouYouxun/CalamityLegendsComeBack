using System;
using CalamityMod;
using CalamityMod.Graphics.Primitives;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.CosmicDischarge
{
    public class CosmicDischargeUltimateField : ModProjectile, ILocalizedModType
    {
        private const int ChargeDuration = 60;
        private const float RiftBurstRadius = 25f * 16f;
        private const float ShockRadius = 200f * 16f;
        private const float FieldRadius = 8f * 16f;
        private const int ShockSlowDuration = 60;
        private const float ShockSpeedMultiplier = 0.48f;

        public new string LocalizationCategory => "Projectiles.Melee";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private ref float Time => ref Projectile.ai[0];
        private Player Owner => Main.player[Projectile.owner];
        private int shockVisualTimer;

        public override void SetDefaults()
        {
            Projectile.width = (int)(FieldRadius * 2f);
            Projectile.height = (int)(FieldRadius * 2f);
            Projectile.penetrate = -1;
            Projectile.timeLeft = CosmicDischargePlayer.UltimateFieldDuration + ChargeDuration;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.friendly = false;
            Projectile.hostile = false;
        }

        public override bool? CanDamage() => false;
        public override bool ShouldUpdatePosition() => false;

        public override void AI()
        {
            if (!Owner.active || Owner.dead)
            {
                Projectile.Kill();
                return;
            }

            Time++;
            if (shockVisualTimer > 0)
                shockVisualTimer--;
            Projectile.Center = Owner.Center;
            Projectile.Opacity = Utils.GetLerpValue(0f, 18f, Time, true) * Utils.GetLerpValue(0f, 25f, Projectile.timeLeft, true);
            Owner.AddBuff(ModContent.BuffType<CosmicDischargeUltimateGuardBuff>(), 4);

            if (Time == 1f)
            {
                SoundEngine.PlaySound(new SoundStyle("CalamityMod/Sounds/Custom/DevourerRiftBuilding") { Volume = 0.78f, Pitch = -0.1f }, Projectile.Center);
                SoundEngine.PlaySound(SoundID.Item92 with { Volume = 0.5f, Pitch = -0.18f }, Projectile.Center);
            }

            if (Time <= ChargeDuration)
            {
                // 60 帧蓄力只脉冲 3 次（20/40/60），对齐 DoGTeleportRift 的 RiftLifetime/3 节奏。
                if (Time % 20f == 0f)
                    CosmicDischargeCommon.SpawnChargePulse(Owner.MountedCenter, Time / ChargeDuration, 1f);

                ApplyScreenShake(Time / ChargeDuration * 5f);
                return;
            }

            if (Time == ChargeDuration + 1)
                ApplyRiftBurst();

            int fieldTime = (int)(Time - ChargeDuration);
            if (IsShockPulseTime(fieldTime))
                EmitShockwave();

            SpawnFieldDust();
        }

        // Each shock lasts one second, then leaves a full two-second no-slow window before the next.
        // Four pulses therefore land at roughly 0, 3, 6, and 9 seconds after the charge finishes.
        private static bool IsShockPulseTime(int fieldTime) => fieldTime is 1 or 181 or 361 or 541;

        private void ApplyRiftBurst()
        {
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                foreach (NPC npc in Main.ActiveNPCs)
                {
                    if (!npc.active || npc.friendly || npc.dontTakeDamage || Vector2.DistanceSquared(npc.Center, Projectile.Center) > RiftBurstRadius * RiftBurstRadius)
                        continue;

                    CosmicDischargeCommon.ApplyDoGDebuffs(npc, 10 * 60);
                }

                Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    Projectile.Center,
                    Vector2.Zero,
                    ModContent.ProjectileType<CosmicDischargeDoGConvergenceExplosion>(),
                    Projectile.damage,
                    Projectile.knockBack,
                    Projectile.owner,
                    0f,
                    RiftBurstRadius,
                    1f);
            }

            CosmicDischargeCommon.SpawnUltimateBurst(Projectile.GetSource_FromThis(), Owner, Projectile.Center, Projectile.damage, Projectile.knockBack);

            if (!Main.dedServ)
            {
                SoundEngine.PlaySound(new SoundStyle("CalamityMod/Sounds/Custom/DevourerRiftOpen") { Volume = 0.82f, Pitch = -0.05f }, Projectile.Center);
                SoundEngine.PlaySound(new SoundStyle("CalamityMod/Sounds/Custom/DevourerSpawn") { Volume = 0.46f, Pitch = 0.18f }, Projectile.Center);
                ApplyScreenShake(18f);
            }
        }

        private void EmitShockwave()
        {
            shockVisualTimer = 30;
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                float radiusSquared = ShockRadius * ShockRadius;
                foreach (NPC npc in Main.ActiveNPCs)
                {
                    if (!npc.active || npc.friendly || Vector2.DistanceSquared(npc.Center, Projectile.Center) > radiusSquared)
                        continue;

                    npc.GetGlobalNPC<CosmicDischargeShockwaveSlowGlobalNPC>().ApplyShockwave(npc, ShockSlowDuration, ShockSpeedMultiplier);
                }

                for (int i = 0; i < Main.maxProjectiles; i++)
                {
                    Projectile proj = Main.projectile[i];
                    if (!proj.active || proj.friendly || !proj.hostile || Vector2.DistanceSquared(proj.Center, Projectile.Center) > radiusSquared)
                        continue;

                    proj.GetGlobalProjectile<CosmicDischargeShockwaveSlowGlobalProjectile>().ApplyShockwave(proj, ShockSlowDuration, ShockSpeedMultiplier);
                }
            }

            if (!Main.dedServ)
            {
                SoundEngine.PlaySound(new SoundStyle("CalamityMod/Sounds/Custom/DevourerAttack") { Volume = 0.48f, Pitch = -0.12f, MaxInstances = 2 }, Projectile.Center);
                CosmicDischargeCommon.SpawnRiftBurst(Projectile.Center, RiftTier.Heavy, default, CosmicDischargeCommon.DoGSpecialColor);
                ApplyScreenShake(7.5f);
            }
        }

        private void SpawnFieldDust()
        {
            if (Main.dedServ)
                return;

            CosmicDischargeCommon.SpawnUltimateFieldIdle(Projectile.Center, FieldRadius, Time);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Texture2D portal = ModContent.Request<Texture2D>("CalamityMod/Projectiles/Melee/StreamGougePortal").Value;
            Vector2 origin = bloom.Size() * 0.5f;
            Vector2 portalOrigin = portal.Size() * 0.5f;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            float pulse = 1f + 0.035f * MathF.Sin(Main.GlobalTimeWrappedHourly * MathHelper.TwoPi);
            float portalRotation = Main.GlobalTimeWrappedHourly * 5.5f + Projectile.identity * 0.18f;
            float chargeInterpolant = Utils.GetLerpValue(0f, ChargeDuration, Time, true);
            float fieldOpacity = Time <= ChargeDuration ? chargeInterpolant : Projectile.Opacity;

            Main.spriteBatch.SetBlendState(BlendState.Additive);

            DrawConstellationField(chargeInterpolant, fieldOpacity);

            if (Time <= ChargeDuration)
            {
                Texture2D star = ModContent.Request<Texture2D>("CalamityMod/Projectiles/StarProj").Value;
                Main.EntitySpriteDraw(star, drawPosition, null, Color.White * 0.65f * chargeInterpolant, 0f, star.Size() * 0.5f, new Vector2(1f, 8f) * (0.45f + chargeInterpolant * 1.35f), SpriteEffects.None);
                Main.EntitySpriteDraw(star, drawPosition, null, Color.White * 0.65f * chargeInterpolant, MathHelper.PiOver2, star.Size() * 0.5f, new Vector2(1f, 5f) * (0.45f + chargeInterpolant * 1.35f), SpriteEffects.None);
                Main.EntitySpriteDraw(bloom, drawPosition, null, CosmicDischargeCommon.Transparent(CosmicDischargeCommon.RiftLightBlue) * (0.35f * chargeInterpolant), 0f, origin, (1f - chargeInterpolant) * 2f, SpriteEffects.None);
            }

            Main.EntitySpriteDraw(
                bloom,
                drawPosition,
                null,
                CosmicDischargeCommon.Transparent(CosmicDischargeCommon.RiftMagenta) * 0.15f * fieldOpacity,
                0f,
                origin,
                FieldRadius / bloom.Width * 2f * pulse,
                SpriteEffects.None);

            Main.EntitySpriteDraw(portal, drawPosition, null, Color.Black * 0.28f * fieldOpacity, portalRotation, portalOrigin, 0.42f * pulse, SpriteEffects.None);
            Main.EntitySpriteDraw(portal, drawPosition, null, CosmicDischargeCommon.Transparent(CosmicDischargeCommon.RiftLightBlue) * 0.34f * fieldOpacity, portalRotation * 0.6f, portalOrigin, 0.42f * pulse, SpriteEffects.None);
            Main.EntitySpriteDraw(portal, drawPosition, null, CosmicDischargeCommon.Transparent(CosmicDischargeCommon.RiftMagenta) * 0.34f * fieldOpacity, -portalRotation * 0.7f, portalOrigin, 0.42f * pulse, SpriteEffects.None);

            DrawDoGFireFieldRing(FieldRadius * pulse, fieldOpacity);
            if (shockVisualTimer > 0)
                DrawShockwaveRing();

            Main.spriteBatch.SetBlendState(BlendState.AlphaBlend);

            return false;
        }

        private void DrawConstellationField(float chargeProgress, float opacity)
        {
            const int nodeCount = 9;
            Texture2D pixel = Terraria.GameContent.TextureAssets.MagicPixel.Value;
            Texture2D star = ModContent.Request<Texture2D>("CalamityMod/Projectiles/StarProj").Value;
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Vector2[] nodes = new Vector2[nodeCount];
            float radius = Time <= ChargeDuration
                ? MathHelper.Lerp(34f, FieldRadius * 0.86f, chargeProgress)
                : FieldRadius * 0.86f;
            float rotation = Main.GlobalTimeWrappedHourly * (Time <= ChargeDuration ? 1.8f : 0.42f) + Projectile.identity * 0.21f;
            float pulse = 0.88f + 0.12f * MathF.Sin(Main.GlobalTimeWrappedHourly * 4.5f);

            for (int i = 0; i < nodeCount; i++)
            {
                float angle = MathHelper.TwoPi * i / nodeCount + rotation;
                float unevenRadius = radius * (i % 3 == 0 ? 1f : 0.72f);
                nodes[i] = Projectile.Center + new Vector2(MathF.Cos(angle), MathF.Sin(angle) * 0.7f) * unevenRadius;
            }

            for (int i = 0; i < nodeCount; i++)
            {
                Vector2 start = nodes[i] - Main.screenPosition;
                Vector2 end = nodes[(i + (i % 2 == 0 ? 2 : 1)) % nodeCount] - Main.screenPosition;
                Vector2 segment = end - start;
                Color accent = Color.Lerp(CosmicDischargeCommon.RiftLightBlue, CosmicDischargeCommon.RiftMagenta, i / (float)(nodeCount - 1));
                Main.EntitySpriteDraw(
                    pixel,
                    start,
                    new Rectangle(0, 0, 1, 1),
                    accent * (0.2f * opacity),
                    segment.ToRotation(),
                    new Vector2(0f, 0.5f),
                    new Vector2(segment.Length(), 1.35f + chargeProgress),
                    SpriteEffects.None);

                float starScale = (0.1f + (i % 3 == 0 ? 0.055f : 0f)) * pulse * MathHelper.Lerp(0.5f, 1f, opacity);
                Main.EntitySpriteDraw(bloom, start, null, accent * (0.18f * opacity), 0f, bloom.Size() * 0.5f, starScale * 2.2f, SpriteEffects.None);
                Main.EntitySpriteDraw(star, start, null, Color.White * (0.68f * opacity), -rotation * 1.4f + i, star.Size() * 0.5f, starScale, SpriteEffects.None);
            }
        }

        private void DrawDoGFireFieldRing(float radius, float opacity)
        {
            Vector2[] ring = new Vector2[48];
            for (int i = 0; i < ring.Length; i++)
            {
                float angle = MathHelper.TwoPi * i / (ring.Length - 1) + Main.GlobalTimeWrappedHourly * 0.35f;
                ring[i] = Projectile.Center + angle.ToRotationVector2() * radius;
            }

            float OuterWidth(float completion, Vector2 _) => MathHelper.Lerp(50f, 32f, completion);
            Color OuterColor(float completion, Vector2 _) => Color.Lerp(CosmicDischargeCommon.RiftLightBlue, Color.Transparent, completion) * (0.44f * opacity);
            float InnerWidth(float completion, Vector2 _) => MathHelper.Lerp(20f, 10f, completion);
            Color InnerColor(float completion, Vector2 _) => Color.Lerp(CosmicDischargeCommon.RiftMagenta, Color.Transparent, completion) * (0.58f * opacity);

            GameShaders.Misc["CalamityMod:ImpFlameTrail"].SetShaderTexture(ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/Trails/ScarletDevilStreak"));
            PrimitiveRenderer.RenderTrail(ring, new PrimitiveSettings(OuterWidth, OuterColor, smoothen: true, pixelate: false, shader: GameShaders.Misc["CalamityMod:ImpFlameTrail"]), ring.Length + 8);

            GameShaders.Misc["CalamityMod:ImpFlameTrail"].SetShaderTexture(ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/Trails/SylvestaffStreak"));
            PrimitiveRenderer.RenderTrail(ring, new PrimitiveSettings(InnerWidth, InnerColor, smoothen: true, pixelate: false, shader: GameShaders.Misc["CalamityMod:ImpFlameTrail"]), ring.Length + 8);
        }

        private void DrawShockwaveRing()
        {
            float completion = 1f - shockVisualTimer / 30f;
            float radius = MathHelper.Lerp(120f, ShockRadius, MathF.Sqrt(completion));
            Vector2[] ring = new Vector2[56];
            for (int i = 0; i < ring.Length; i++)
            {
                float angle = MathHelper.TwoPi * i / (ring.Length - 1);
                ring[i] = Projectile.Center + angle.ToRotationVector2() * radius;
            }

            float OuterWidth(float progress, Vector2 _) => MathHelper.Lerp(38f, 12f, completion) * (1f - progress * 0.35f);
            Color OuterColor(float progress, Vector2 _) => Color.Lerp(CosmicDischargeCommon.RiftLightBlue, Color.Transparent, progress) * (0.68f * (1f - completion));
            float InnerWidth(float progress, Vector2 _) => MathHelper.Lerp(15f, 5f, completion) * (1f - progress * 0.45f);
            Color InnerColor(float progress, Vector2 _) => Color.Lerp(CosmicDischargeCommon.RiftMagenta, Color.Transparent, progress) * (0.72f * (1f - completion));

            GameShaders.Misc["CalamityMod:ImpFlameTrail"].SetShaderTexture(ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/Trails/ScarletDevilStreak"));
            PrimitiveRenderer.RenderTrail(ring, new PrimitiveSettings(OuterWidth, OuterColor, smoothen: true, pixelate: false, shader: GameShaders.Misc["CalamityMod:ImpFlameTrail"]), ring.Length + 8);
            GameShaders.Misc["CalamityMod:ImpFlameTrail"].SetShaderTexture(ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/Trails/SylvestaffStreak"));
            PrimitiveRenderer.RenderTrail(ring, new PrimitiveSettings(InnerWidth, InnerColor, smoothen: true, pixelate: false, shader: GameShaders.Misc["CalamityMod:ImpFlameTrail"]), ring.Length + 8);
        }

        private void ApplyScreenShake(float power)
        {
            if (Main.dedServ)
                return;

            float distanceFactor = Utils.GetLerpValue(1600f, 120f, Vector2.Distance(Main.LocalPlayer.Center, Projectile.Center), true);
            Main.LocalPlayer.Calamity().GeneralScreenShakePower = Math.Max(Main.LocalPlayer.Calamity().GeneralScreenShakePower, power * distanceFactor);
        }
    }
}
