using CalamityLegendsComeBack.Weapons.BlossomFlux;
using CalamityLegendsComeBack.Weapons.BlossomFlux.RightUI;
using CalamityMod;
using CalamityMod.Enums;
using CalamityMod.Graphics.Primitives;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Linq;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.BlossomFlux.RightClick
{
    // E 战术右键箭：扎附在目标或地形后，持续朝周围排出毒云。
    internal class BFArrow_EPlague : ModProjectile, IPixelatedPrimitiveRenderer
    {
        public new string LocalizationCategory => "Projectiles.BlossomFlux";
        public override string Texture => "CalamityLegendsComeBack/Weapons/BlossomFlux/RightClick/EPlague/BFArrow_EPlague";
        public GeneralDrawLayer LayerToRenderTo => GeneralDrawLayer.BeforeProjectiles;

        private const float RightSporeSearchRange = 920f;
        private const int SporeHomingDelay = 25;

        private int gasTimer;
        private int storedGasDamage = 1;
        private Vector2 stickOffset;

        // 由疫球（BFPlagueSporeBomb）散射出来的子箭。一次会同时出现十几根，
        // 所以驻留时间、放毒频率和命中演出都要压下来，否则弹幕数和粒子数会直接爆掉。
        private bool fromSporeBomb;

        private int StickDuration => fromSporeBomb ? 105 : 180;

        internal void ConfigureFromSporeBomb()
        {
            fromSporeBomb = true;
            Projectile.netUpdate = true;
        }

        private ref float State => ref Projectile.ai[0];
        private ref float AttachedNpcIndex => ref Projectile.ai[1];
        private ref float FlightTimer => ref Projectile.localAI[0];
        private ref float BounceCounter => ref Projectile.localAI[1];

        private bool InFlight => State == 0f;

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 16;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 2;
        }

        public override void SetDefaults()
        {
            BFArrowCommon.SetBaseArrowDefaults(Projectile, width: 22, height: 22, timeLeft: 300, penetrate: -1, extraUpdates: 1, tileCollide: true);
            Projectile.localNPCHitCooldown = -1;
        }

        public override void OnSpawn(Terraria.DataStructures.IEntitySource source)
        {
            storedGasDamage = System.Math.Max(Projectile.damage, 1);
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2 + MathHelper.Pi;

            Projectile.extraUpdates = 2;
            Projectile.velocity *= 1.2f;
            Projectile.scale = 1f;
        }

        public override bool? CanDamage() => State <= 0f ? null : false;

        public override bool? CanHitNPC(NPC target) => State <= 0f ? null : false;

        public override void AI()
        {
            Lighting.AddLight(Projectile.Center, BFArrowCommon.GetPresetColor(BlossomFluxChloroplastPresetType.Chlo_EPlague).ToVector3() * 0.42f);

            if (State == 0f)
            {
                UpdateSporeBombFlight();
                BFArrowCommon.FaceForward(Projectile);
                EmitPlagueFlightFX(false);

                return;
            }

            Projectile.friendly = false;
            Projectile.tileCollide = false;

            if (State == 1f)
            {
                if (!BFArrowCommon.InBounds(AttachedNpcIndex, Main.maxNPCs))
                {
                    Projectile.Kill();
                    return;
                }

                NPC attachedNpc = Main.npc[(int)AttachedNpcIndex];
                if (!attachedNpc.active || attachedNpc.dontTakeDamage)
                {
                    Projectile.Kill();
                    return;
                }

                Projectile.Center = attachedNpc.Center + stickOffset;
                Projectile.gfxOffY = attachedNpc.gfxOffY;
            }
            else
            {
                Projectile.velocity = Vector2.Zero;
            }

            EmitPlagueGas();
            EmitPlagueAnchorAura();
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (State != 0f)
                return;

            stickOffset = Projectile.Center - target.Center;
            storedGasDamage = System.Math.Max(Projectile.damage, storedGasDamage);
            State = 1f;
            AttachedNpcIndex = target.whoAmI;
            Projectile.velocity = Vector2.Zero;
            Projectile.damage = 0;
            Projectile.timeLeft = StickDuration;
            Projectile.netUpdate = true;

            BFPlaguePollutionNPC pollution = target.GetGlobalNPC<BFPlaguePollutionNPC>();
            pollution.ApplyDeath(target);
            ExtendReconMark(target);

            if (fromSporeBomb)
            {
                // 疫球子箭：只保留一层轻量演出，重头戏归疫球自己的引爆。
                Main.player[Projectile.owner].SetScreenshake(1.8f);
                SpawnPlagueCollapseBurst(target.Center, 0.5f);
                SpawnPlagueAnchorFX(target.Center, 0.7f);
                if (Main.rand.NextBool(3))
                    SoundEngine.PlaySound(BlossomFluxSounds.RightPlagueProjHit1 with { Volume = 0.2f }, target.Center);
                return;
            }

            Main.player[Projectile.owner].SetScreenshake(6.5f);
            SpawnPlagueCollapseBurst(target.Center, 1.25f);
            SpawnSporeGasBurst(target.Center, System.Math.Max(1, (int)(storedGasDamage * 0.22f)), Main.rand.Next(2, 4), 1.2f);
            BFArrowCommon.EmitPresetBurst(Projectile, BlossomFluxChloroplastPresetType.Chlo_EPlague, 12, 0.9f, 3f, 0.85f, 1.15f);
            SpawnPlagueAnchorFX(target.Center, 1.1f);
            SoundEngine.PlaySound(BlossomFluxSounds.RightPlagueProjHit1, target.Center);
            SoundEngine.PlaySound(BlossomFluxSounds.RightPlagueProjHit2, target.Center);
            SoundEngine.PlaySound(BlossomFluxSounds.RightPlagueProjHit3, target.Center);
        }

        private void ExtendReconMark(NPC target)
        {
            if (!BFArrowCommon.InBounds(Projectile.owner, Main.maxPlayers))
                return;

            BFPlagueRightStats stats = BFPlagueRightBalance.GetStats();
            if (stats.MarkDurationMultiplier <= 1f)
                return;

            BFArrow_CDetecNPC reconMark = target.GetGlobalNPC<BFArrow_CDetecNPC>();
            int newTime = reconMark.MultiplyCurrentMarkTime(Projectile.owner, stats.MarkDurationMultiplier);
            if (newTime <= 0)
                return;

            Main.player[Projectile.owner].GetModPlayer<BFRightUIPlayer>().SetReconPriorityTarget(target.whoAmI, newTime);
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            State = 2f;
            Projectile.velocity = Vector2.Zero;
            Projectile.timeLeft = StickDuration;
            Projectile.netUpdate = true;

            SpawnPlagueAnchorFX(Projectile.Center, fromSporeBomb ? 0.6f : 0.9f);
            if (!fromSporeBomb || Main.rand.NextBool(3))
                SoundEngine.PlaySound(BlossomFluxSounds.RightPlagueProjAction, Projectile.Center);
            return false;
        }

        public override void OnKill(int timeLeft)
        {
            BFArrowCommon.EmitPresetBurst(Projectile, BlossomFluxChloroplastPresetType.Chlo_EPlague, 14, 1.2f, 4.2f, 0.9f, 1.25f);
            BlossomFluxSounds.PlayRightPlagueProjExplode(Projectile.Center);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (State <= 0f)
            {
                Vector2 forward = Projectile.velocity.SafeNormalize(Vector2.UnitX);
                DrawArrowHelix(Projectile.Center - Main.screenPosition, forward);
                DrawPlagueSprayOverlay();
                BFArrowCommon.DrawPresetArrow(Projectile, lightColor, BlossomFluxChloroplastPresetType.Chlo_EPlague, 1.28f);
            }
            else
                BFArrowCommon.DrawProjectile(Projectile, TextureAssets.Projectile[Projectile.type].Value, Projectile.GetAlpha(lightColor), Projectile.rotation, Projectile.scale);

            return false;
        }

        // ─── 双层拖尾（绿酸风格，仅飞行阶段）────────────────────────────────
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
            float max = Projectile.scale * 20f;
            return t < 0.16f
                ? MathF.Sin(t / 0.16f * MathHelper.PiOver2) * max
                : Utils.Remap(t, 0.16f, 1f, max, 0f);
        }

        private Color OuterColorFunc(float t, Vector2 _)
        {
            Color c = Color.Lerp(new Color(90, 255, 70), new Color(20, 130, 30), t * 0.65f) * Projectile.Opacity;
            c = Color.Lerp(c, Color.Transparent, Utils.GetLerpValue(0.70f, 1f, t, true));
            c.A = 0;
            return c;
        }

        private float CoreWidthFunc(float t, Vector2 _)
        {
            float max = Projectile.scale * 10f;
            return t < 0.16f
                ? MathF.Sin(t / 0.16f * MathHelper.PiOver2) * max
                : Utils.Remap(t, 0.16f, 1f, max, 0f);
        }

        private Color CoreColorFunc(float t, Vector2 _)
        {
            Color c = Color.Lerp(Color.White, new Color(190, 255, 130), t * 0.5f) * Projectile.Opacity;
            c = Color.Lerp(c, Color.Transparent, Utils.GetLerpValue(0.72f, 1f, t, true));
            c.A = 0;
            return c;
        }

        public void RenderPixelatedPrimitives(SpriteBatch spriteBatch, GeneralDrawLayer layer)
        {
            if (!InFlight) return;

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

        // ─── 呼吸双股毒液螺旋（绿/酸绿互旋）──────────────────────────────────
        private void DrawArrowHelix(Vector2 drawPos, Vector2 forward)
        {
            Texture2D tex = ModContent.Request<Texture2D>("CalamityMod/Particles/WaterFlavored").Value;
            Vector2 right = forward.RotatedBy(MathHelper.PiOver2);
            Color mainColor = BFArrowCommon.GetPresetColor(BlossomFluxChloroplastPresetType.Chlo_EPlague);
            Color accentColor = BFArrowCommon.GetPresetAccentColor(BlossomFluxChloroplastPresetType.Chlo_EPlague);
            float time = Main.GlobalTimeWrappedHourly * 5.1f + Projectile.identity * 0.41f;
            const int len = 10;

            Main.spriteBatch.SetBlendState(BlendState.Additive);
            for (int strand = 0; strand < 2; strand++)
            {
                float dir = strand == 0 ? 1f : -1f;
                Color sc = strand == 0 ? mainColor : accentColor;
                for (int i = 0; i < len; i++)
                {
                    float t = i / (float)(len - 1);
                    float angle = time * dir - t * 3.2f + MathHelper.Pi * strand;
                    // 呼吸式半径
                    float breathe = 1f + 0.32f * (float)Math.Sin(time * 1.9f + strand * MathHelper.Pi + t * 2.1f);
                    float radius = MathHelper.Lerp(15f, 5f, t) * breathe;
                    Vector2 off = right.RotatedBy(angle) * radius - forward * MathHelper.Lerp(2f, 40f, t);
                    float opacity = MathHelper.Lerp(0.58f, 0.05f, t) * Projectile.Opacity;
                    Main.EntitySpriteDraw(tex, drawPos + off, null, sc with { A = 0 } * opacity,
                        forward.ToRotation() - MathHelper.PiOver2, tex.Size() * 0.5f,
                        new Vector2(0.18f, MathHelper.Lerp(0.85f, 0.30f, t)) * Projectile.scale,
                        SpriteEffects.None, 0);
                }
            }
            Main.spriteBatch.SetBlendState(BlendState.AlphaBlend);
        }

        // ─── 飞行特效（保留原有，追加螺旋轨道特效）──────────────────────────
        private void EmitPlagueFlightFX(bool leftSpore)
        {
            if (Main.dedServ)
                return;

            Color mainColor = BFArrowCommon.GetPresetColor(BlossomFluxChloroplastPresetType.Chlo_EPlague);
            Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitY);
            Vector2 normal = direction.RotatedBy(MathHelper.PiOver2);
            float intensity = leftSpore ? 1.25f : 0.88f;

            Color staticPlague = new(124, 255, 44);
            Color acidMist = new(136, 211, 113, 127);
            Vector2 gasSpawnPosition = Projectile.Center - direction * Main.rand.NextFloat(6f, 20f) + Main.rand.NextVector2Circular(9f, 9f);
            Vector2 gasVelocity = -Projectile.velocity.RotatedByRandom(0.32f) * Main.rand.NextFloat(0.18f, 0.56f) + normal * Main.rand.NextFloat(-0.7f, 0.7f);
            GeneralParticleHandler.SpawnParticle(new MediumMistParticle(
                gasSpawnPosition, gasVelocity,
                Color.Lerp(acidMist, mainColor, 0.35f),
                Color.Lerp(Color.Black, staticPlague, 0.18f),
                Main.rand.NextFloat(0.34f, 0.72f) * intensity,
                Main.rand.Next(58, 92),
                Main.rand.NextFloat(0.006f, 0.018f)));

            // 疫球子箭不要光刺，十几根一起飞会糊成一片白。
            if (!fromSporeBomb && Projectile.FinalExtraUpdate() && (int)FlightTimer % 3 == 0)
            {
                GeneralParticleHandler.SpawnParticle(new GlowSparkParticle(
                    Projectile.Center - direction * 10f + normal * Main.rand.NextFloat(-5f, 5f),
                    -Projectile.velocity * Main.rand.NextFloat(0.22f, 0.42f),
                    false, Main.rand.Next(7, 11),
                    Main.rand.NextFloat(0.055f, 0.1f) * intensity,
                    Color.Lerp(Color.SeaGreen, Color.PaleGreen, 0.35f) * 0.78f,
                    new Vector2(0.28f, 1.15f), false, false, 0.75f));
            }

            if (Projectile.FinalExtraUpdate() && (int)FlightTimer % 8 == 0)
            {
                GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(
                    Projectile.Center - direction * 14f + normal * Main.rand.NextFloat(-12f, 12f),
                    -Projectile.velocity * 0.08f,
                    Main.rand.NextBool(3) ? mainColor : staticPlague,
                    new Vector2(0.42f, 0.78f), direction.ToRotation(),
                    0.09f * intensity, 0.022f, 14));
            }

            if (Main.rand.NextBool(leftSpore ? 2 : 3))
            {
                Vector2 placement = Projectile.Center - direction * Main.rand.NextFloat(2f, 14f) + normal * Main.rand.NextFloat(-9f, 9f);
                GeneralParticleHandler.SpawnParticle(new GlowOrbParticle(
                    placement,
                    -Projectile.velocity * Main.rand.NextFloat(0.12f, 0.44f),
                    false, Main.rand.Next(6, leftSpore ? 10 : 8),
                    Main.rand.NextFloat(0.26f, 0.46f) * intensity,
                    Color.Lerp(mainColor, Color.White, Main.rand.NextFloat(0.12f, 0.34f)),
                    true, false, true));
            }

            if (leftSpore && Projectile.FinalExtraUpdate() && (int)FlightTimer % 3 == 0)
            {
                GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(
                    Projectile.Center + direction * 7f,
                    Projectile.velocity * 0.035f,
                    Color.Lerp(mainColor, new Color(190, 255, 84), 0.32f),
                    new Vector2(0.42f, 1.2f), direction.ToRotation(),
                    0.105f, 0.034f, 9));
            }

            Dust dust = Dust.NewDustPerfect(
                Projectile.Center - direction * Main.rand.NextFloat(2f, 10f) + normal * Main.rand.NextFloat(-5f, 5f),
                DustID.GreenTorch,
                -Projectile.velocity.RotatedByRandom(leftSpore ? 0.44f : 0.22f) * Main.rand.NextFloat(0.12f, leftSpore ? 0.36f : 0.5f),
                0,
                Color.Lerp(mainColor, new Color(172, 228, 92), 0.42f),
                Main.rand.NextFloat(0.46f, 0.92f) * intensity);
            dust.noGravity = true;

            // 新增螺旋轨道特效
            if (Projectile.FinalExtraUpdate())
                EmitPlagueHelixFX(direction, normal, mainColor, staticPlague);
        }

        // 双螺旋 GlowOrb 轨道 + MediumMist 毒雾团 + GlowSpark 酸液光束
        private void EmitPlagueHelixFX(Vector2 direction, Vector2 normal, Color mainColor, Color acidColor)
        {
            float time = Main.GlobalTimeWrappedHourly * 7.6f + Projectile.identity * 0.47f;

            for (int strand = 0; strand < 2; strand++)
            {
                float dir = strand == 0 ? 1f : -1f;
                float strandAngle = time * dir + strand * MathHelper.Pi;
                float breathe = 1f + 0.26f * (float)Math.Sin(time * 1.6f + strand * MathHelper.Pi);
                float radius = 12f * breathe;
                Vector2 orbOffset = normal.RotatedBy(strandAngle) * radius - direction * Main.rand.NextFloat(2f, 10f);
                Vector2 orbVel = -direction * Main.rand.NextFloat(0.2f, 0.5f) + normal.RotatedBy(strandAngle) * (float)Math.Cos(strandAngle) * 0.45f;

                if (Main.rand.NextBool(2))
                {
                    GeneralParticleHandler.SpawnParticle(new GlowOrbParticle(
                        Projectile.Center + orbOffset,
                        orbVel,
                        false, Main.rand.Next(5, 9),
                        Main.rand.NextFloat(0.24f, 0.40f),
                        Color.Lerp(strand == 0 ? mainColor : acidColor, Color.White, 0.18f),
                        true, false, true));
                }
            }

            if ((int)FlightTimer % 4 == 0)
            {
                GeneralParticleHandler.SpawnParticle(new MediumMistParticle(
                    Projectile.Center + normal * Main.rand.NextFloat(-8f, 8f) - direction * Main.rand.NextFloat(4f, 18f),
                    normal * (float)Math.Sin(time) * 0.6f - direction * Main.rand.NextFloat(0.1f, 0.35f),
                    Color.Lerp(mainColor, acidColor, 0.42f),
                    Color.Lerp(Color.Black, mainColor, 0.14f),
                    Main.rand.NextFloat(0.22f, 0.44f),
                    Main.rand.Next(30, 48),
                    Main.rand.NextFloat(0.008f, 0.016f)));
            }

            // 同上：螺旋上的酸液光束也只留给单发的那根主箭。
            if (!fromSporeBomb && (int)FlightTimer % 3 == 0)
            {
                float beamAngle = time * 1.3f;
                GeneralParticleHandler.SpawnParticle(new GlowSparkParticle(
                    Projectile.Center + normal.RotatedBy(beamAngle) * Main.rand.NextFloat(5f, 13f) - direction * Main.rand.NextFloat(2f, 12f),
                    normal.RotatedBy(beamAngle) * Main.rand.NextFloat(1.2f, 2.8f) - direction * Main.rand.NextFloat(0.3f, 0.7f),
                    false, Main.rand.Next(5, 8),
                    Main.rand.NextFloat(0.06f, 0.12f),
                    Color.Lerp(acidColor, Color.White, 0.25f) * 0.85f,
                    new Vector2(0.22f, 1.1f), false, false, 0.9f));
            }
        }

        // ─── 原有逻辑（保留）──────────────────────────────────────────────────
        private void UpdateSporeBombFlight()
        {
            FlightTimer++;
            Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            float speed = MathHelper.Clamp(Projectile.velocity.Length(), 10f, 22.8f);
            float curveTimer = FlightTimer % 60f;
            float curveStrength = 0.18f;

            if (curveTimer >= 20f && curveTimer < 40f)
            {
                Projectile.velocity.Y += curveStrength;
                Projectile.velocity.X *= 0.985f;
            }
            else if (curveTimer >= 40f)
            {
                Projectile.velocity.Y -= curveStrength;
                Projectile.velocity.X *= 1.012f;
            }

            NPC target = FindSporeHomingTarget(RightSporeSearchRange);
            if (target != null && FlightTimer >= SporeHomingDelay * Projectile.MaxUpdates)
            {
                Vector2 desiredVelocity = (target.Center + target.velocity * 4f - Projectile.Center).SafeNormalize(direction) * speed;
                Projectile.velocity = Vector2.Lerp(Projectile.velocity, desiredVelocity, 0.095f);
            }

            Projectile.velocity = Projectile.velocity.SafeNormalize(direction) * MathHelper.Lerp(Projectile.velocity.Length(), speed, 0.08f);
            Projectile.scale = 1f + 0.1f * (float)System.Math.Sin(FlightTimer * 0.18f + Projectile.identity * 0.23f);
        }

        private NPC FindSporeHomingTarget(float range)
        {
            NPC bestTarget = null;
            float bestDistance = range;

            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];
                if (!npc.active || npc.dontTakeDamage || npc.friendly || npc.life <= 0)
                    continue;

                if (!Collision.CanHitLine(Projectile.Center, 1, 1, npc.Center, 1, 1))
                    continue;

                float distance = Vector2.Distance(Projectile.Center, npc.Center);
                if (distance >= bestDistance)
                    continue;

                bestDistance = distance;
                bestTarget = npc;
            }

            return bestTarget;
        }

        private void SpawnSporeGasBurst(Vector2 center, int damage, int sporeAmount, float spreadScale)
        {
            if (Projectile.owner != Main.myPlayer)
                return;

            for (int s = 0; s < sporeAmount; s++)
            {
                Vector2 velocity = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(2.6f, 5.4f) * spreadScale;
                velocity.Y += Main.rand.NextFloat(-1.2f, 0.8f);
                int sporeIndex = Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    center + Main.rand.NextVector2Circular(8f, 8f),
                    velocity,
                    ProjectileID.SporeGas + Main.rand.Next(3),
                    damage, 0f, Projectile.owner);

                if (!BFArrowCommon.InBounds(sporeIndex, Main.maxProjectiles))
                    continue;

                Projectile spore = Main.projectile[sporeIndex];
                spore.DamageType = DamageClass.Ranged;
                spore.usesLocalNPCImmunity = true;
                spore.usesIDStaticNPCImmunity = false;
                spore.localNPCHitCooldown = 30;
            }
        }

        private void EmitPlagueGas()
        {
            gasTimer++;

            if (Main.rand.NextBool())
            {
                Dust dust = Dust.NewDustPerfect(
                    Projectile.Center + Main.rand.NextVector2Circular(14f, 14f),
                    DustID.GreenTorch,
                    Main.rand.NextVector2Circular(1.2f, 1.2f) + new Vector2(0f, -0.18f),
                    100, new Color(172, 228, 92),
                    Main.rand.NextFloat(0.85f, 1.35f));
                dust.noGravity = true;
            }

            if (!Main.dedServ && Main.rand.NextBool(fromSporeBomb ? 7 : 3))
            {
                GeneralParticleHandler.SpawnParticle(new HeavySmokeParticle(
                    Projectile.Center + Main.rand.NextVector2Circular(14f, 14f),
                    Main.rand.NextVector2Circular(0.6f, 0.6f) + new Vector2(0f, -0.16f),
                    Color.Lerp(BFArrowCommon.GetPresetColor(BlossomFluxChloroplastPresetType.Chlo_EPlague), Color.White, 0.12f),
                    18, Main.rand.NextFloat(0.45f, 0.7f), 0.58f,
                    Main.rand.NextFloat(-0.04f, 0.04f), false));
            }

            if (!Main.dedServ && gasTimer % 10 == 0)
            {
                GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(
                    Projectile.Center + Main.rand.NextVector2Circular(6f, 6f),
                    Main.rand.NextVector2Circular(0.3f, 0.3f),
                    Color.Lerp(BFArrowCommon.GetPresetColor(BlossomFluxChloroplastPresetType.Chlo_EPlague), Color.White, 0.18f),
                    new Vector2(Main.rand.NextFloat(0.95f, 1.3f), Main.rand.NextFloat(1.2f, 1.8f)),
                    Main.rand.NextFloat(-0.6f, 0.6f),
                    Main.rand.NextFloat(0.12f, 0.18f), 0.024f,
                    Main.rand.Next(12, 18)));
            }

            int gasInterval = fromSporeBomb ? 16 : 8;
            if (gasTimer % gasInterval != 0 || Projectile.owner != Main.myPlayer)
                return;

            int burstCount = fromSporeBomb ? 1 : 2;
            float baseAngle = Main.rand.NextFloat(MathHelper.TwoPi);
            for (int i = 0; i < burstCount; i++)
            {
                Vector2 velocity = (baseAngle + MathHelper.TwoPi * i / burstCount + Main.rand.NextFloat(-0.28f, 0.28f)).ToRotationVector2() * Main.rand.NextFloat(3f, 5.8f) + new Vector2(0f, Main.rand.NextFloat(-1.15f, -0.28f));
                int gasIndex = Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    Projectile.Center + Main.rand.NextVector2Circular(12f, 12f),
                    velocity,
                    ModContent.ProjectileType<BFArrow_EPlagueGas>(),
                    System.Math.Max(1, (int)(storedGasDamage * 0.38f)),
                    0f, Projectile.owner,
                    Main.rand.Next(3),
                    Main.rand.NextFloat(1.35f, 1.75f));

                if (BFArrowCommon.InBounds(gasIndex, Main.maxProjectiles))
                    BFArrowCommon.ForceLocalNPCImmunity(Main.projectile[gasIndex], 12);
            }
        }

        private void SpawnPlagueAnchorFX(Vector2 center, float intensity)
        {
            if (Main.dedServ)
                return;

            Color mainColor = BFArrowCommon.GetPresetColor(BlossomFluxChloroplastPresetType.Chlo_EPlague);
            GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(
                center, Vector2.Zero,
                Color.Lerp(mainColor, Color.White, 0.1f),
                new Vector2(1.08f, 1.32f), Main.rand.NextFloat(-0.25f, 0.25f),
                0.16f * intensity, 0.034f, 15));

            GeneralParticleHandler.SpawnParticle(new HeavySmokeParticle(
                center, Main.rand.NextVector2Circular(0.3f, 0.3f),
                Color.Lerp(mainColor, Color.Black, 0.18f),
                18, 0.62f * intensity, 0.6f,
                Main.rand.NextFloat(-0.03f, 0.03f), false));

            for (int i = 0; i < 3; i++)
            {
                GeneralParticleHandler.SpawnParticle(new DetailedExplosion(
                    center + Main.rand.NextVector2Circular(6f, 6f),
                    Main.rand.NextVector2Circular(0.8f, 0.8f),
                    Color.Lerp(mainColor, Color.White, Main.rand.NextFloat(0.08f, 0.24f)),
                    Vector2.One, Main.rand.NextFloat(-0.4f, 0.4f),
                    0f, 0.16f * intensity, 10 + i));
            }
        }

        private void SpawnPlagueCollapseBurst(Vector2 center, float intensity)
        {
            if (Main.dedServ)
                return;

            Color mainColor = BFArrowCommon.GetPresetColor(BlossomFluxChloroplastPresetType.Chlo_EPlague);
            Color acidColor = new(198, 255, 78);
            Color darkColor = new(18, 92, 30);

            GeneralParticleHandler.SpawnParticle(new CustomPulse(
                center, Vector2.Zero,
                Color.Lerp(darkColor, mainColor, 0.35f),
                "CalamityMod/Particles/SmallBloom", Vector2.One,
                Main.rand.NextFloat(-0.4f, 0.4f), 0f, 1.05f * intensity, 32, true));

            GeneralParticleHandler.SpawnParticle(new CustomPulse(
                center, Vector2.Zero,
                Color.Lerp(mainColor, acidColor, 0.5f),
                "CalamityMod/Particles/BloomRing", Vector2.One,
                Main.rand.NextFloat(-0.25f, 0.25f), 0.18f * intensity, 2.1f * intensity, 34));

            for (int i = 0; i < 10; i++)
            {
                Vector2 velocity = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(5.5f, 13f) * intensity;
                GeneralParticleHandler.SpawnParticle(new CustomSpark(
                    center + Main.rand.NextVector2Circular(8f, 8f), velocity,
                    "CalamityMod/Particles/VerticalSmear",
                    false, Main.rand.Next(15, 23),
                    Main.rand.NextFloat(1.8f, 2.8f) * intensity,
                    Color.Lerp(mainColor, acidColor, Main.rand.NextFloat(0.25f, 0.8f)),
                    new Vector2(0.18f, 1f)));
            }

            for (int i = 0; i < 42; i++)
            {
                Vector2 velocity = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(2.4f, 7.8f) * intensity;
                Dust dust = Dust.NewDustPerfect(
                    center + Main.rand.NextVector2Circular(12f, 12f),
                    Main.rand.NextBool(4) ? DustID.TerraBlade : DustID.GreenTorch,
                    velocity, 80,
                    Color.Lerp(darkColor, acidColor, Main.rand.NextFloat(0.25f, 0.9f)),
                    Main.rand.NextFloat(0.95f, 1.75f) * intensity);
                dust.noGravity = true;
            }

            for (int i = 0; i < 5; i++)
            {
                GeneralParticleHandler.SpawnParticle(new HeavySmokeParticle(
                    center + Main.rand.NextVector2Circular(18f, 18f),
                    Main.rand.NextVector2Circular(1.4f, 1.4f),
                    Color.Lerp(mainColor, darkColor, 0.28f),
                    Main.rand.Next(20, 30),
                    Main.rand.NextFloat(0.76f, 1.15f) * intensity, 0.62f,
                    Main.rand.NextFloat(-0.05f, 0.05f), false));
            }
        }

        private void DrawPlagueSprayOverlay()
        {
            Texture2D pointTexture = ModContent.Request<Texture2D>("CalamityMod/Particles/GlowSpark").Value;
            Texture2D bloomTexture = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            float squash = Utils.GetLerpValue(-3f, 10f, Projectile.velocity.Length(), true);
            Color mainColor = BFArrowCommon.GetPresetColor(BlossomFluxChloroplastPresetType.Chlo_EPlague);
            float visualScale = 1.28f;

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive,
                SamplerState.PointClamp, DepthStencilState.None,
                Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);

            Main.EntitySpriteDraw(bloomTexture, drawPosition, null, mainColor * 0.5f, 0f,
                bloomTexture.Size() * 0.5f, (0.22f + squash * 0.08f) * visualScale, SpriteEffects.None, 0);

            for (int i = 0; i < 3; i++)
            {
                float fade = 0.55f + 0.2f * i;
                Main.EntitySpriteDraw(pointTexture,
                    drawPosition + Projectile.velocity * 5f * fade, null,
                    Color.Lerp(Color.White, mainColor, i * 0.4f) * 0.55f,
                    Projectile.velocity.ToRotation() + MathHelper.PiOver2,
                    pointTexture.Size() * 0.5f,
                    new Vector2(0.68f - 0.16f * i, 0.72f + 0.28f * i) * 0.04f * (0.8f + squash * 0.55f) * visualScale,
                    SpriteEffects.None, 0);
            }

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
                SamplerState.PointClamp, DepthStencilState.None,
                Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
        }

        private void EmitPlagueAnchorAura()
        {
            if (Main.dedServ || gasTimer % (fromSporeBomb ? 14 : 6) != 0)
                return;

            Color mainColor = BFArrowCommon.GetPresetColor(BlossomFluxChloroplastPresetType.Chlo_EPlague);
            for (int i = 0; i < (fromSporeBomb ? 1 : 2); i++)
            {
                GeneralParticleHandler.SpawnParticle(new GlowOrbParticle(
                    Projectile.Center + Main.rand.NextVector2Circular(20f, 20f),
                    Main.rand.NextVector2Circular(0.6f, 0.6f) + new Vector2(0f, -0.2f),
                    false, 12, Main.rand.NextFloat(0.22f, 0.34f),
                    Color.Lerp(mainColor, Color.White, 0.22f),
                    true, false, true));
            }
        }
    }
}
