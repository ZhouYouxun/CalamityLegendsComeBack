using CalamityLegendsComeBack.Weapons.BlossomFlux;
using CalamityLegendsComeBack.Weapons.BlossomFlux.RightUI;
using CalamityLegendsComeBack.Accssory.BF.Common;
using CalamityMod;
using CalamityMod.Enums;
using CalamityMod.Graphics.Primitives;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.IO;
using System.Linq;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.BlossomFlux.RightClick
{
    internal class BFArrow_CDetec : ModProjectile, IPixelatedPrimitiveRenderer
    {
        private const int PriorityMarkDuration = 15 * 60;
        private const int MaxScannedTargets = 10;
        private const int SilvaStickDuration = 15 * 60;
        private const float SilvaFieldRadius = 160f;

        public new string LocalizationCategory => "Projectiles.BlossomFlux";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";
        public GeneralDrawLayer LayerToRenderTo => GeneralDrawLayer.BeforeProjectiles;

        private ref float BestTargetIndex => ref Projectile.ai[0];
        private ref float BestTargetLifeMax => ref Projectile.ai[1];
        private ref float FlightTimer => ref Projectile.localAI[0];
        private int configuredMarkDuration = PriorityMarkDuration;
        private int configuredAmpDuration = BFReconRightBalance.DamageAmpDuration;
        private int configuredEffectTier;
        private int scannedTargetCount;
        private readonly bool[] scannedTargets = new bool[Main.maxNPCs];
        private int attachedNpcIndex = -1;
        private int attachedTimer;
        private Vector2 stickOffset;

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 24;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 2;
        }

        public override void SetDefaults()
        {
            // Transformer 在 K 键释放后是高速、直线、无限贯穿的束状攻击；这里复刻那一段手感。
            Projectile.width = 16;
            Projectile.height = 16;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 62;
            Projectile.extraUpdates = 8;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }

        public void ConfigureMark(int markDuration, int ampDuration, int effectTier)
        {
            configuredMarkDuration = System.Math.Max(60, markDuration);
            configuredAmpDuration = System.Math.Max(30, ampDuration);
            configuredEffectTier = Utils.Clamp(effectTier, 0, 2);
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(configuredMarkDuration);
            writer.Write(configuredAmpDuration);
            writer.Write(configuredEffectTier);
            writer.Write(attachedNpcIndex);
            writer.Write(attachedTimer);
            writer.WriteVector2(stickOffset);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            configuredMarkDuration = reader.ReadInt32();
            configuredAmpDuration = reader.ReadInt32();
            configuredEffectTier = reader.ReadInt32();
            attachedNpcIndex = reader.ReadInt32();
            attachedTimer = reader.ReadInt32();
            stickOffset = reader.ReadVector2();
        }

        public override void OnSpawn(Terraria.DataStructures.IEntitySource source)
        {
            BestTargetIndex = -1f;
            BestTargetLifeMax = -1f;
            scannedTargetCount = 0;
            Array.Clear(scannedTargets, 0, scannedTargets.Length);
            Projectile.rotation = Projectile.velocity.ToRotation();
        }

        public override void AI()
        {
            if (attachedNpcIndex >= 0)
            {
                UpdateSilvaAttachment();
                return;
            }

            FlightTimer++;

            Lighting.AddLight(Projectile.Center, BFArrowCommon.GetPresetColor(BlossomFluxChloroplastPresetType.Chlo_CDetec).ToVector3() * 0.52f);
            Projectile.rotation = Projectile.velocity.ToRotation();
            EmitPenetrationFlightFX();

            if ((int)FlightTimer % 16 == 0 && Projectile.owner == Main.myPlayer)
                SoundEngine.PlaySound(BlossomFluxSounds.RightReconProjAction, Projectile.Center);
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (!BFArrowCommon.InBounds(target.whoAmI, scannedTargets.Length) || scannedTargets[target.whoAmI] || scannedTargetCount >= MaxScannedTargets)
                return;

            scannedTargets[target.whoAmI] = true;
            scannedTargetCount++;
            // 命中的每一个目标都拿到完整标记：描边 + 短暂的全局受伤放大，不再只有最高血量那一个有用。
            target.GetGlobalNPC<BFArrow_CDetecNPC>().ApplyDamageAmpMark(Projectile.owner, configuredMarkDuration, configuredAmpDuration);

            if (target.lifeMax > BestTargetLifeMax)
            {
                BestTargetLifeMax = target.lifeMax;
                BestTargetIndex = target.whoAmI;
                Projectile.netUpdate = true;
            }

            if (Projectile.owner == Main.myPlayer)
            {
                Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    target.Center,
                    Vector2.Zero,
                    ModContent.ProjectileType<BFArrow_CDetecReticle>(),
                    0,
                    0f,
                    Projectile.owner,
                    target.whoAmI);
            }

            SpawnPenetrationImpactFX(target.Center, 0.82f);
            SoundEngine.PlaySound(BlossomFluxSounds.RightReconProjHit, target.Center);

            if (Main.player[Projectile.owner].GetModPlayer<BFAccessoryPlayer>().SilvaHarpEquipped)
                AttachSilvaArrow(target);
        }

        public override bool? CanDamage() => attachedNpcIndex >= 0 ? false : null;

        private void AttachSilvaArrow(NPC target)
        {
            attachedNpcIndex = target.whoAmI;
            attachedTimer = 0;
            stickOffset = Projectile.Center - target.Center;
            Projectile.Center = target.Center + stickOffset;
            Projectile.velocity = Vector2.Zero;
            Projectile.friendly = false;
            Projectile.extraUpdates = 0;
            Projectile.timeLeft = SilvaStickDuration;
            Projectile.netUpdate = true;
        }

        private void UpdateSilvaAttachment()
        {
            if (!BFArrowCommon.InBounds(attachedNpcIndex, Main.maxNPCs))
            {
                Projectile.Kill();
                return;
            }

            NPC attachedNpc = Main.npc[attachedNpcIndex];
            if (!attachedNpc.active || attachedNpc.friendly || attachedNpc.dontTakeDamage)
            {
                Projectile.Kill();
                return;
            }

            Projectile.Center = attachedNpc.Center + stickOffset;
            Projectile.gfxOffY = attachedNpc.gfxOffY;
            Projectile.rotation += 0.025f;
            Lighting.AddLight(Projectile.Center, new Vector3(0.05f, 0.24f, 0.42f));

            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!npc.friendly && !npc.dontTakeDamage && Vector2.DistanceSquared(npc.Center, Projectile.Center) <= SilvaFieldRadius * SilvaFieldRadius)
                    npc.GetGlobalNPC<BFArrow_CDetecNPC>().ApplySilvaField(Projectile.owner, 2);
            }

            attachedTimer++;
            if (attachedTimer % 60 == 0)
            {
                attachedNpc.GetGlobalNPC<BFArrow_CDetecNPC>().ApplyDamageAmpMark(Projectile.owner, configuredMarkDuration, configuredAmpDuration);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                    attachedNpc.SimpleStrikeNPC(System.Math.Max(1, Projectile.damage), System.Math.Sign(attachedNpc.Center.X - Main.player[Projectile.owner].Center.X), false, 0f, DamageClass.Ranged, false, noPlayerInteraction: true);

                SpawnPenetrationImpactFX(attachedNpc.Center, 0.48f);
                Projectile.netUpdate = true;
            }
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            return false;
        }

        public override void OnKill(int timeLeft)
        {
            ApplyBestTargetMark();
            BFArrowCommon.EmitPresetBurst(Projectile, BlossomFluxChloroplastPresetType.Chlo_CDetec, 14, 1.1f, 4.8f, 0.88f, 1.25f);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Texture2D spark = ModContent.Request<Texture2D>("CalamityMod/Particles/GlowSpark").Value;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            Color coreColor = new Color(225, 252, 255, 0) * Projectile.Opacity;
            Color edgeColor = new Color(78, 194, 255, 0) * Projectile.Opacity;

            Main.spriteBatch.SetBlendState(BlendState.Additive);
            if (attachedNpcIndex >= 0)
            {
                Texture2D field = ModContent.Request<Texture2D>("CalamityMod/Particles/HighResHollowCircleHardEdge").Value;
                float pulse = 0.95f + MathF.Sin(Main.GlobalTimeWrappedHourly * 4f) * 0.035f;
                Main.EntitySpriteDraw(field, drawPosition, null, edgeColor * 0.28f, -Projectile.rotation * 0.35f, field.Size() * 0.5f, SilvaFieldRadius * 2f / field.Width * pulse, SpriteEffects.None, 0);
                Main.EntitySpriteDraw(field, drawPosition, null, coreColor * 0.12f, Projectile.rotation * 0.22f, field.Size() * 0.5f, SilvaFieldRadius * 1.82f / field.Width, SpriteEffects.None, 0);
            }
            Main.EntitySpriteDraw(bloom, drawPosition, null, edgeColor * 0.72f, 0f, bloom.Size() * 0.5f, 0.24f, SpriteEffects.None, 0);
            Main.EntitySpriteDraw(bloom, drawPosition, null, coreColor * 0.82f, 0f, bloom.Size() * 0.5f, 0.1f, SpriteEffects.None, 0);
            Main.EntitySpriteDraw(spark, drawPosition, null, coreColor, Projectile.rotation, spark.Size() * 0.5f, new Vector2(0.055f, 0.24f), SpriteEffects.None, 0);
            Main.spriteBatch.SetBlendState(BlendState.AlphaBlend);
            return false;
        }

        // ─── 双层拖尾（青色扫描风格）──────────────────────────────────────────
        private Vector2[] BuildTrailPoints()
        {
            Vector2[] pts = Projectile.oldPos
                .Where(p => p != Vector2.Zero)
                .Select(p => p + Projectile.Size * 0.5f)
                .ToArray();
            if (pts.Length == 0)
                return new[] { Projectile.Center - Projectile.velocity, Projectile.Center };
            if (pts[0] != Projectile.Center)
                pts = new[] { Projectile.Center }.Concat(pts).ToArray();
            return pts;
        }

        private float OuterWidthFunc(float t, Vector2 _)
        {
            float max = Projectile.scale * 24f;
            return t < 0.16f
                ? MathF.Sin(t / 0.16f * MathHelper.PiOver2) * max
                : Utils.Remap(t, 0.16f, 1f, max, 0f);
        }

        private Color OuterColorFunc(float t, Vector2 _)
        {
            Color c = Color.Lerp(new Color(80, 240, 255), new Color(20, 110, 210), t * 0.65f) * Projectile.Opacity;
            c = Color.Lerp(c, Color.Transparent, Utils.GetLerpValue(0.70f, 1f, t, true));
            c.A = 0;
            return c;
        }

        private float CoreWidthFunc(float t, Vector2 _)
        {
            float max = Projectile.scale * 12f;
            return t < 0.16f
                ? MathF.Sin(t / 0.16f * MathHelper.PiOver2) * max
                : Utils.Remap(t, 0.16f, 1f, max, 0f);
        }

        private Color CoreColorFunc(float t, Vector2 _)
        {
            Color c = Color.Lerp(Color.White, new Color(160, 250, 255), t * 0.5f) * Projectile.Opacity;
            c = Color.Lerp(c, Color.Transparent, Utils.GetLerpValue(0.72f, 1f, t, true));
            c.A = 0;
            return c;
        }

        public void RenderPixelatedPrimitives(SpriteBatch spriteBatch, GeneralDrawLayer layer)
        {
            Vector2[] pts = BuildTrailPoints();
            if (pts.Length < 2) return;

            GameShaders.Misc["CalamityMod:ImpFlameTrail"].SetShaderTexture(
                ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/Trails/SylvestaffStreak"));
            PrimitiveRenderer.RenderTrail(pts,
                new PrimitiveSettings(OuterWidthFunc, OuterColorFunc, (_, _) => Vector2.Zero, true, true,
                    GameShaders.Misc["CalamityMod:ImpFlameTrail"]),
                pts.Length * 2);

            Vector2[] core = pts.Take(Math.Min(9, pts.Length)).ToArray();
            if (core.Length < 2) return;
            GameShaders.Misc["CalamityMod:ImpFlameTrail"].SetShaderTexture(
                ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/Trails/ScarletDevilStreak"));
            PrimitiveRenderer.RenderTrail(core,
                new PrimitiveSettings(CoreWidthFunc, CoreColorFunc, (_, _) => Vector2.Zero, true, true,
                    GameShaders.Misc["CalamityMod:ImpFlameTrail"]),
                core.Length * 2);
        }

        // ─── 脉冲扫描双股螺旋（青/靛互旋，带呼吸半径）────────────────────────
        private void DrawArrowHelix(Vector2 drawPos, Vector2 forward)
        {
            Texture2D tex = ModContent.Request<Texture2D>("CalamityMod/Particles/WaterFlavored").Value;
            Vector2 right = forward.RotatedBy(MathHelper.PiOver2);
            Color mainColor = BFArrowCommon.GetPresetColor(BlossomFluxChloroplastPresetType.Chlo_CDetec);
            Color accentColor = BFArrowCommon.GetPresetAccentColor(BlossomFluxChloroplastPresetType.Chlo_CDetec);
            float time = Main.GlobalTimeWrappedHourly * 6.5f + Projectile.identity * 0.37f;
            const int len = 9;

            Main.spriteBatch.SetBlendState(BlendState.Additive);
            for (int strand = 0; strand < 2; strand++)
            {
                float dir = strand == 0 ? 1f : -1f;
                Color sc = strand == 0 ? mainColor : accentColor;
                for (int i = 0; i < len; i++)
                {
                    float t = i / (float)(len - 1);
                    float angle = time * dir - t * 3.5f + MathHelper.Pi * strand;
                    float breathe = 1f + 0.28f * (float)Math.Sin(time * 2.4f + strand * MathHelper.Pi + t * 1.8f);
                    float radius = MathHelper.Lerp(16f, 4f, t) * breathe;
                    Vector2 off = right.RotatedBy(angle) * radius - forward * MathHelper.Lerp(2f, 38f, t);
                    float opacity = MathHelper.Lerp(0.55f, 0.05f, t) * Projectile.Opacity;
                    Main.EntitySpriteDraw(tex, drawPos + off, null, sc with { A = 0 } * opacity,
                        forward.ToRotation() - MathHelper.PiOver2, tex.Size() * 0.5f,
                        new Vector2(0.16f, MathHelper.Lerp(0.80f, 0.28f, t)) * Projectile.scale,
                        SpriteEffects.None, 0);
                }
            }
            Main.spriteBatch.SetBlendState(BlendState.AlphaBlend);
        }

        // ─── 原有视觉逻辑（保留）─────────────────────────────────────────────
        private void DrawReconOpticOverlay()
        {
            if (Main.dedServ)
                return;

            Texture2D opticTexture = ModContent.Request<Texture2D>("CalamityLegendsComeBack/Texture/KsTexture/muzzle_02").Value;
            Vector2 center = Projectile.Center - Main.screenPosition;
            Vector2 forward = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            Vector2 side = forward.RotatedBy(MathHelper.PiOver2);
            float fadeIn = Utils.GetLerpValue(0f, 18f, FlightTimer, true);
            float fadeOut = Utils.GetLerpValue(0f, 24f, Projectile.timeLeft, true);
            float fade = fadeIn * fadeOut;
            float scanPulse = 0.5f + 0.5f * (float)System.Math.Sin(FlightTimer * 0.35f + Projectile.identity * 0.47f);
            float wingOpen = 0.2f + 0.28f * scanPulse;

            Main.spriteBatch.SetBlendState(BlendState.Additive);
            DrawReconOpticWing(opticTexture, center, side, 1f, wingOpen, fade);
            DrawReconOpticWing(opticTexture, center, -side, -1f, wingOpen, fade);
            Main.spriteBatch.SetBlendState(BlendState.AlphaBlend);
        }

        private void DrawReconOpticWing(Texture2D opticTexture, Vector2 center, Vector2 wingDirection, float sideSign, float wingOpen, float fade)
        {
            Color mainColor = BFArrowCommon.GetPresetColor(BlossomFluxChloroplastPresetType.Chlo_CDetec);
            Color accentColor = BFArrowCommon.GetPresetAccentColor(BlossomFluxChloroplastPresetType.Chlo_CDetec);
            Vector2 origin = new(opticTexture.Width * 0.5f, opticTexture.Height * 0.84f);
            float baseRotation = wingDirection.ToRotation() + MathHelper.PiOver2;
            float flutter = (float)System.Math.Sin(FlightTimer * 0.62f + sideSign * 0.8f + Projectile.identity * 0.13f) * wingOpen;
            Vector2 forward = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            Vector2 basePosition = center + wingDirection * 4f - forward * 1.5f;

            for (int i = 0; i < 3; i++)
            {
                float ghostOffset = i - 1f;
                float opacity = (i == 1 ? 0.38f : 0.14f) * fade;
                Color wingColor = Color.Lerp(mainColor, Color.Lerp(accentColor, Color.White, 0.25f), 0.32f + 0.18f * i) with { A = 0 } * opacity;
                Main.EntitySpriteDraw(opticTexture,
                    basePosition + wingDirection * ghostOffset * 1.6f,
                    null, wingColor,
                    baseRotation + flutter + ghostOffset * 0.2f,
                    origin,
                    new Vector2(0.072f, 0.105f),
                    SpriteEffects.None, 0);
            }
        }

        private void ApplyBestTargetMark()
        {
            if (!BFArrowCommon.InBounds(BestTargetIndex, Main.maxNPCs) || !BFArrowCommon.InBounds(Projectile.owner, Main.maxPlayers))
                return;

            NPC target = Main.npc[(int)BestTargetIndex];
            if (!target.active || target.dontTakeDamage)
                return;

            BFArrow_CDetecNPC mark = target.GetGlobalNPC<BFArrow_CDetecNPC>();
            mark.ApplyPriorityMark(Projectile.owner, configuredMarkDuration);
            mark.ApplyDamageAmpMark(Projectile.owner, configuredMarkDuration, configuredAmpDuration);
            Main.player[Projectile.owner].GetModPlayer<BFRightUIPlayer>().SetReconPriorityTarget(target.whoAmI, configuredMarkDuration);

            SpawnMarkAcquireFX(target.Center);
            SoundEngine.PlaySound(BlossomFluxSounds.RightReconProjEvent, target.Center);

            if (Projectile.owner != Main.myPlayer)
                return;

            Projectile.NewProjectile(
                Projectile.GetSource_FromThis(),
                target.Center,
                Vector2.Zero,
                ModContent.ProjectileType<BFArrow_CDetecReticle>(),
                0,
                0f,
                Projectile.owner,
                target.whoAmI);
        }

        private void EmitPenetrationFlightFX()
        {
            if (Main.dedServ || !Projectile.FinalExtraUpdate())
                return;

            Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitY);
            Vector2 normal = direction.RotatedBy(MathHelper.PiOver2);
            Color mainColor = BFArrowCommon.GetPresetColor(BlossomFluxChloroplastPresetType.Chlo_CDetec);
            Color accentColor = BFArrowCommon.GetPresetAccentColor(BlossomFluxChloroplastPresetType.Chlo_CDetec);

            float phase = FlightTimer * 0.28f + Projectile.identity * 0.57f;
            for (int strand = 0; strand < 2; strand++)
            {
                float strandPhase = phase + strand * MathHelper.Pi;
                float sideOffset = (float)Math.Sin(strandPhase) * 10f;
                float depth = (float)Math.Cos(strandPhase);
                Vector2 position = Projectile.Center - direction * (8f + depth * 4f) + normal * sideOffset;
                Vector2 velocity = -Projectile.velocity * 0.035f + normal * (float)Math.Cos(strandPhase) * 0.55f;
                Color color = strand == 0
                    ? Color.Lerp(mainColor, Color.White, 0.24f)
                    : Color.Lerp(accentColor, Color.White, 0.34f);

                GeneralParticleHandler.SpawnParticle(new GlowSquareParticle(
                    position, velocity, false,
                    Main.rand.Next(13, 19),
                    Main.rand.NextFloat(0.055f, 0.095f) * (0.75f + Math.Abs(depth) * 0.35f),
                    color, true, Main.rand.NextFloat(-0.12f, 0.12f)));
            }

            if ((int)FlightTimer % 3 == 0)
            {
                GeneralParticleHandler.SpawnParticle(new LineParticle(
                    Projectile.Center - direction * 12f,
                    normal * (float)Math.Sin(phase) * 1.3f,
                    false, 9, 0.22f,
                    Color.Lerp(mainColor, accentColor, 0.45f)));
            }

            EmitReconScanFX(direction, normal, mainColor, accentColor);
        }

        // 交叉 TechyHolosquare 轨道 + CritSpark 扫描束 + SquishyLight 脉冲
        private void EmitReconScanFX(Vector2 direction, Vector2 normal, Color mainColor, Color accentColor)
        {
            float time = Main.GlobalTimeWrappedHourly * 9.2f + Projectile.identity * 0.53f;

            for (int i = 0; i < 4; i++)
            {
                float scanAngle = time + i * MathHelper.PiOver2;
                float wave = (float)Math.Sin(scanAngle);
                float radius = 14f * Math.Abs(wave);
                Vector2 offset = normal.RotatedBy(scanAngle) * radius - direction * Main.rand.NextFloat(4f, 16f);

                GeneralParticleHandler.SpawnParticle(new TechyHoloysquareParticle(
                    Projectile.Center + offset,
                    -direction * 0.06f + normal.RotatedBy(scanAngle) * (float)Math.Cos(scanAngle) * 0.38f,
                    Main.rand.NextFloat(0.07f, 0.13f),
                    Color.Lerp(i % 2 == 0 ? mainColor : accentColor, Color.White, 0.22f),
                    Main.rand.Next(8, 13),
                    0.72f));
            }

            if ((int)FlightTimer % 5 == 0)
            {
                for (int b = 0; b < 3; b++)
                {
                    float beamAngle = time + b * MathHelper.TwoPi / 3f;
                    GeneralParticleHandler.SpawnParticle(new CritSpark(
                        Projectile.Center,
                        beamAngle.ToRotationVector2() * Main.rand.NextFloat(2.2f, 4.8f),
                        accentColor, Color.White,
                        Main.rand.NextFloat(0.25f, 0.42f), 9));
                }
            }

            if ((int)FlightTimer % 6 == 0)
            {
                GeneralParticleHandler.SpawnParticle(new SquishyLightParticle(
                    Projectile.Center - direction * Main.rand.NextFloat(4f, 14f) + normal * Main.rand.NextFloat(-8f, 8f),
                    -direction * Main.rand.NextFloat(0.2f, 0.8f),
                    Main.rand.NextFloat(0.12f, 0.20f),
                    Color.Lerp(mainColor, Color.White, 0.28f),
                    Main.rand.Next(9, 14)));
            }
        }

        private void SpawnPenetrationImpactFX(Vector2 center, float intensity)
        {
            if (Main.dedServ)
                return;

            Color mainColor = BFArrowCommon.GetPresetColor(BlossomFluxChloroplastPresetType.Chlo_CDetec);
            Color accentColor = BFArrowCommon.GetPresetAccentColor(BlossomFluxChloroplastPresetType.Chlo_CDetec);
            Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitY);

            GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(
                center, direction * 0.7f,
                Color.Lerp(mainColor, Color.White, 0.18f),
                new Vector2(0.78f, 1.95f), direction.ToRotation(),
                0.14f * intensity, 0.032f, 10));

            GeneralParticleHandler.SpawnParticle(new StrongBloom(
                center, Vector2.Zero,
                Color.Lerp(mainColor, accentColor, 0.35f),
                0.38f * intensity, 9));

            for (int i = 0; i < 10; i++)
            {
                float yDir = (i % 2 == 0) ? -1f : 1f;
                float baseSpeed = yDir < 0f ? Main.rand.NextFloat(5f, 9f) : Main.rand.NextFloat(3f, 6f);
                Vector2 sparkVel = new Vector2(0f, yDir * baseSpeed).RotatedByRandom(MathHelper.Pi / 7.2f)
                    * Main.rand.NextFloat(0.1f, 1.9f) * intensity;

                GeneralParticleHandler.SpawnParticle(new GlowSquareParticle(
                    center + Main.rand.NextVector2Circular(10f, 10f),
                    sparkVel, false,
                    Main.rand.Next(11, 17),
                    Main.rand.NextFloat(0.05f, 0.09f) * intensity,
                    Main.rand.NextBool() ? Color.Cyan : Color.Turquoise,
                    true, Main.rand.NextFloat(-0.16f, 0.16f)));
            }
        }

        private void SpawnMarkAcquireFX(Vector2 center)
        {
            if (Main.dedServ)
                return;

            Color mainColor = BFArrowCommon.GetPresetColor(BlossomFluxChloroplastPresetType.Chlo_CDetec);
            Color accentColor = BFArrowCommon.GetPresetAccentColor(BlossomFluxChloroplastPresetType.Chlo_CDetec);

            float tierScale = 1f + configuredEffectTier * 0.28f;
            GeneralParticleHandler.SpawnParticle(new StrongBloom(center, Vector2.Zero,
                Color.Lerp(mainColor, Color.White, 0.22f), 0.92f * tierScale, 16 + configuredEffectTier * 4));

            for (int i = 0; i < 3 + configuredEffectTier * 2; i++)
            {
                GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(
                    center, Vector2.Zero,
                    Color.Lerp(mainColor, accentColor, 0.25f + i * 0.12f),
                    new Vector2(0.78f + i * 0.16f, 1.35f + i * 0.24f),
                    MathHelper.TwoPi * i / (3f + configuredEffectTier * 2f),
                    (0.14f + i * 0.03f) * tierScale, 0.026f, 12 + i * 2));
            }
        }
    }
}
