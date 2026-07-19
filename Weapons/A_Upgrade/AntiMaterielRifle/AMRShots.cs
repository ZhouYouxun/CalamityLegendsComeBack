using System;
using CalamityMod;
using CalamityMod.Buffs.StatDebuffs;
using CalamityMod.Graphics.Primitives;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Upgrade.AntiMaterielRifle
{
    internal sealed class AMRRound : ModProjectile, ILocalizedModType
    {
        private const int HiddenFrames = 5;
        private const float GoldenAngle = 2.39996323f;

        private bool hitAnyTarget;
        private bool configured;
        private int visualAgeFrames;

        public new string LocalizationCategory => "Projectiles.AntiMaterielRifle";
        public override string Texture => "CalamityMod/Projectiles/Ranged/AMRShot";

        private bool IsAimedShot => Projectile.ai[0] > 0.001f;
        private bool IsMarkerRound => Projectile.ai[1] >= 0.5f;

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 20;
            ProjectileID.Sets.TrailingMode[Type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = 8;
            Projectile.height = 8;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = true;
            Projectile.penetrate = 1;
            Projectile.extraUpdates = 7;
            Projectile.timeLeft = 240;
            Projectile.scale = 1.55f;
            Projectile.light = 0.75f;
            Projectile.ArmorPenetration = 25;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }

        public override void AI()
        {
            if (!configured)
            {
                configured = true;
                Projectile.penetrate = IsAimedShot ? -1 : 1;
                Projectile.ArmorPenetration = IsAimedShot ? 45 : 25;
            }

            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
            Lighting.AddLight(Projectile.Center, new Color(255, 196, 64).ToVector3() * 0.7f);

            if (!CalamityUtils.FinalExtraUpdate(Projectile))
                return;

            visualAgeFrames++;
            SpawnFlightWake();

            if (visualAgeFrames == HiddenFrames + 1)
                SpawnBreakthroughReveal();
        }

        private void SpawnBreakthroughReveal()
        {
            if (Main.dedServ)
                return;

            Vector2 forward = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            Vector2 center = Projectile.Center - forward * 5f;
            float rotation = forward.ToRotation();

            GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(center, Vector2.Zero,
                new Color(67, 45, 13), new Vector2(0.42f, 1.28f), rotation, 0.06f, 0.62f, 15));
            GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(center, Vector2.Zero,
                new Color(255, 190, 48), new Vector2(0.32f, 1.05f), rotation, 0.03f, 0.46f, 13));
            GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(center, Vector2.Zero,
                new Color(255, 248, 205), new Vector2(0.22f, 0.78f), rotation, 0.02f, 0.27f, 10));

            for (int i = 0; i < 4; i++)
            {
                float angle = MathHelper.ToRadians(5f + i * 5f);
                float speed = 8f + i * 2.2f;
                Color color = i % 2 == 0 ? new Color(255, 214, 90) : new Color(180, 116, 24);

                for (int sign = -1; sign <= 1; sign += 2)
                {
                    Vector2 velocity = -forward.RotatedBy(angle * sign) * speed;
                    GeneralParticleHandler.SpawnParticle(new LineParticle(center, velocity, false,
                        11 + i, 0.55f - i * 0.055f, color));
                }
            }
        }

        private void SpawnFlightWake()
        {
            if (Main.dedServ)
                return;

            Vector2 forward = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            Vector2 normal = new(-forward.Y, forward.X);
            float frameTravel = Projectile.velocity.Length() * (Projectile.extraUpdates + 1f);

            float cullingMargin = frameTravel + 180f;
            Rectangle expandedView = new(
                (int)(Main.screenPosition.X - cullingMargin),
                (int)(Main.screenPosition.Y - cullingMargin),
                (int)(Main.screenWidth + cullingMargin * 2f),
                (int)(Main.screenHeight + cullingMargin * 2f));
            if (!expandedView.Contains(Projectile.Center.ToPoint()))
                return;

            int pressureNodeCount = Math.Clamp((int)MathF.Ceiling(frameTravel / 145f), 2, 4);

            // A matched pair at each node makes the wake read as compressed
            // air being split by the round, rather than as a spark spray.
            for (int node = 0; node < pressureNodeCount; node++)
            {
                float completion = (node + 1f) / (pressureNodeCount + 1f);
                float distance = MathHelper.Lerp(10f, frameTravel, completion);
                float phase = (visualAgeFrames * pressureNodeCount + node) * GoldenAngle;
                float lateralDistance = 3.4f + MathF.Sin(phase) * 1.1f;
                float shearAngle = MathHelper.ToRadians(8f + node * 2.25f);
                Color color = node % 2 == 0 ? new Color(255, 212, 82) : new Color(172, 106, 17);

                for (int sign = -1; sign <= 1; sign += 2)
                {
                    Vector2 position = Projectile.Center - forward * distance + normal * lateralDistance * sign;
                    Vector2 velocity = -forward.RotatedBy(shearAngle * sign) *
                        MathHelper.Lerp(3.2f, 6.4f, completion);

                    GeneralParticleHandler.SpawnParticle(new LineParticle(
                        position,
                        velocity,
                        false,
                        8 + node,
                        0.3f + (1f - completion) * 0.12f,
                        color));
                }

                if (node % 2 == 0)
                {
                    GeneralParticleHandler.SpawnParticle(new GlowOrbParticle(
                        Projectile.Center - forward * distance,
                        -forward * 0.9f,
                        false,
                        8,
                        0.2f + (1f - completion) * 0.09f,
                        new Color(255, 228, 142),
                        true,
                        false,
                        true));
                }
            }

            // One thin collar per frame is enough to imply repeated supersonic
            // compression without turning the projectile into a ring emitter.
            GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(
                Projectile.Center - forward * Math.Min(frameTravel * 0.34f, 150f),
                -forward * 0.45f,
                new Color(224, 151, 29),
                new Vector2(0.14f, 0.7f),
                forward.ToRotation(),
                0.015f,
                0.29f,
                9));

            if ((visualAgeFrames - 1) % 2 == 0)
            {
                GeneralParticleHandler.SpawnParticle(new HeavySmokeParticle(
                    Projectile.Center - forward * Math.Min(frameTravel * 0.72f, 300f),
                    -forward * 1.4f,
                    new Color(13, 10, 7),
                    14,
                    0.3f,
                    0.72f,
                    0.018f * (visualAgeFrames % 4 < 2 ? 1f : -1f),
                    false,
                    required: true));
            }
        }

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            modifiers.ScalingArmorPenetration += IsAimedShot ? 0.55f : 0.35f;

            if (Projectile.owner >= 0 && Projectile.owner < Main.maxPlayers)
            {
                AMRPlayer player = Main.player[Projectile.owner].GetModPlayer<AMRPlayer>();
                modifiers.SourceDamage *= player.GetCalibrationMultiplier(target.whoAmI);
            }

            if (AMRBalance.CriticalOverflowUnlocked && Projectile.CritChance > 100)
                modifiers.CritDamage += (Projectile.CritChance - 100) / 100f;

            if (AMRBalance.CoreRuptureUnlocked)
                modifiers.CritDamage += IsAimedShot ? 0.75f : 0.5f;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            hitAnyTarget = true;

            if (AMRBalance.DeathMarkUnlocked)
            {
                target.AddBuff(ModContent.BuffType<MarkedforDeath>(), 5 * 60);
                int defenseLoss = Math.Max(25, (int)(target.defense * (IsAimedShot ? 0.6f : 0.4f)));
                target.Calamity().miscDefenseLoss = Math.Max(target.Calamity().miscDefenseLoss, defenseLoss);
            }

            if (AMRBalance.MetalJetUnlocked)
                SpawnMetalJet(target.Center);

            if (Projectile.owner >= 0 && Projectile.owner < Main.maxPlayers)
                Main.player[Projectile.owner].GetModPlayer<AMRPlayer>().RegisterCalibrationHit(target.whoAmI);

            if (AMRBalance.OnyxSequenceUnlocked)
                ResolveOnyxSequence(target, damageDone);

            SpawnImpact(target.Center, hit.Crit);
        }

        private void ResolveOnyxSequence(NPC target, int damageDone)
        {
            AMRMarkerGlobalNPC marker = target.GetGlobalNPC<AMRMarkerGlobalNPC>();
            if (IsMarkerRound)
            {
                marker.SetMarker(Projectile.owner, Math.Max(Projectile.damage, damageDone));
                SoundEngine.PlaySound(SoundID.Item4 with { Volume = 0.35f, Pitch = -0.45f }, target.Center);
                return;
            }

            if (!marker.TryConsumeMarker(Projectile.owner, Math.Max(Projectile.damage, damageDone), out int detonationDamage))
                return;

            if (Projectile.owner != Main.myPlayer)
                return;

            int detonation = Projectile.NewProjectile(
                Projectile.GetSource_FromThis(),
                target.Center,
                Vector2.Zero,
                ModContent.ProjectileType<AMROnyxDetonation>(),
                detonationDamage,
                Projectile.knockBack * 1.5f,
                Projectile.owner,
                target.whoAmI);
            if (Main.projectile.IndexInRange(detonation))
                Main.projectile[detonation].CritChance = Projectile.CritChance;
        }

        private void SpawnMetalJet(Vector2 impactCenter)
        {
            Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            Vector2 normal = new(-direction.Y, direction.X);
            for (int i = 0; i < 12; i++)
            {
                float distance = MathHelper.Lerp(-10f, 46f, i / 11f);
                float sineOffset = MathF.Sin(i * GoldenAngle) * 2.2f;
                Dust jet = Dust.NewDustPerfect(
                    impactCenter + direction * distance + normal * sineOffset,
                    i % 3 == 0 ? DustID.Torch : DustID.GoldFlame,
                    direction * MathHelper.Lerp(1.5f, 5f, i / 11f),
                    55,
                    new Color(255, 202, 81),
                    MathHelper.Lerp(0.6f, 1.05f, i / 11f));
                jet.noGravity = true;
            }
        }

        private void SpawnImpact(Vector2 center, bool critical)
        {
            if (Main.dedServ)
                return;

            float strength = critical ? 1.25f : 1f;
            Vector2 forward = Projectile.velocity.SafeNormalize(Vector2.UnitX);

            GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(center, Vector2.Zero,
                new Color(60, 40, 12), Vector2.One, forward.ToRotation(), 0.1f, 1.15f * strength, 18));
            GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(center, Vector2.Zero,
                new Color(255, 196, 64), Vector2.One, forward.ToRotation(), 0.05f, 0.78f * strength, 14));

            int count = critical ? 16 : 12;
            for (int i = 0; i < count; i++)
            {
                float angle = MathHelper.TwoPi * i / count;
                float speed = (i % 2 == 0 ? 8f : 5.5f) * strength;
                Color color = i % 3 == 0 ? new Color(255, 248, 205) :
                    i % 3 == 1 ? new Color(255, 196, 64) : new Color(139, 88, 17);
                GeneralParticleHandler.SpawnParticle(new LineParticle(center, angle.ToRotationVector2() * speed,
                    false, critical ? 15 : 12, critical ? 0.58f : 0.42f, color));
            }

            for (int i = 0; i < 4; i++)
            {
                Vector2 smokeVelocity = (MathHelper.PiOver2 * i).ToRotationVector2() * 1.8f;
                GeneralParticleHandler.SpawnParticle(new HeavySmokeParticle(center, smokeVelocity,
                    new Color(18, 13, 8), 20, 0.5f * strength, 0.75f, i % 2 == 0 ? 0.04f : -0.04f,
                    false, required: true));
            }
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            Collision.HitTiles(Projectile.position, Projectile.velocity, Projectile.width, Projectile.height);
            SpawnImpact(Projectile.Center, false);
            SoundEngine.PlaySound(SoundID.Dig with { Volume = 0.7f }, Projectile.Center);
            return true;
        }

        public override void OnKill(int timeLeft)
        {
            if (!hitAnyTarget && Projectile.owner >= 0 && Projectile.owner < Main.maxPlayers)
                Main.player[Projectile.owner].GetModPlayer<AMRPlayer>().ResetCalibration();
        }

        public override Color? GetAlpha(Color lightColor)
        {
            return new Color(255, 248, 205) * Projectile.Opacity;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            DrawShaderTrail();

            Texture2D texture = TextureAssets.Projectile[Type].Value;
            Vector2 origin = texture.Size() * 0.5f;
            Vector2 forward = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            Vector2 normal = new(-forward.Y, forward.X);
            Vector2 drawCenter = Projectile.Center - Main.screenPosition;
            float pulse = 0.94f + MathF.Sin(Main.GlobalTimeWrappedHourly * 18f + Projectile.identity * 0.31f) * 0.06f;

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, Main.DefaultSamplerState,
                DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
            DrawCompressionChevrons(drawCenter, forward, normal, pulse);

            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Vector2 bloomOrigin = bloom.Size() * 0.5f;
            Main.EntitySpriteDraw(bloom, drawCenter, null, new Color(255, 171, 24, 0), 0f,
                bloomOrigin, 0.16f * pulse, SpriteEffects.None, 0f);
            Main.EntitySpriteDraw(bloom, drawCenter + forward * 2f, null, new Color(255, 249, 220, 0), 0f,
                bloomOrigin, 0.06f * pulse, SpriteEffects.None, 0f);
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState,
                DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);

            // Only Calamity's AMRShot sprite is hidden for the first five
            // frames. The pressure trail and optical structures are immediate.
            if (visualAgeFrames > HiddenFrames)
            {
                Color outlineColor = new(255, 187, 42, 0);
                const int glowDraws = 4;
                for (int i = 0; i < glowDraws; i++)
                {
                    Vector2 offset = (MathHelper.TwoPi * i / glowDraws).ToRotationVector2() * 1.8f;
                    Main.EntitySpriteDraw(texture, drawCenter + offset, null,
                        outlineColor * 0.48f, Projectile.rotation, origin,
                        Projectile.scale * 1.04f, SpriteEffects.None, 0f);
                }

                Color mainColor = GetAlpha(lightColor) ?? Color.White;
                Main.EntitySpriteDraw(texture, drawCenter, null, mainColor,
                    Projectile.rotation, origin, Projectile.scale, SpriteEffects.None, 0f);
            }

            return false;
        }

        private void DrawShaderTrail()
        {
            Vector2[] trailPoints = BuildFlightTrailPoints();
            if (trailPoints.Length < 3)
                return;

            MiscShaderData trailShader = GameShaders.Misc["CalamityMod:TrailStreak"];
            trailShader.SetShaderTexture(
                ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/Trails/BasicTrail"));

            PrimitiveRenderer.RenderTrail(
                trailPoints,
                new PrimitiveSettings(
                    PressureTrailWidth,
                    PressureTrailColor,
                    (_, _) => Vector2.Zero,
                    smoothen: false,
                    pixelate: false,
                    shader: trailShader),
                trailPoints.Length * 2);

            PrimitiveRenderer.RenderTrail(
                trailPoints,
                new PrimitiveSettings(
                    ThermalTrailWidth,
                    ThermalTrailColor,
                    (_, _) => Vector2.Zero,
                    smoothen: false,
                    pixelate: false,
                    shader: trailShader),
                trailPoints.Length * 2);

            int corePointCount = Math.Min(11, trailPoints.Length);
            if (corePointCount < 3)
                return;

            Vector2[] corePoints = new Vector2[corePointCount];
            Array.Copy(trailPoints, corePoints, corePointCount);
            PrimitiveRenderer.RenderTrail(
                corePoints,
                new PrimitiveSettings(
                    PenetratorCoreWidth,
                    PenetratorCoreColor,
                    (_, _) => Vector2.Zero,
                    smoothen: false,
                    pixelate: false,
                    shader: trailShader),
                corePoints.Length * 2);
        }

        private Vector2[] BuildFlightTrailPoints()
        {
            Vector2[] points = new Vector2[Projectile.oldPos.Length + 3];
            int count = 0;
            points[count++] = Projectile.Center;

            Vector2 lastPoint = Projectile.Center;
            for (int i = 0; i < Projectile.oldPos.Length; i++)
            {
                if (Projectile.oldPos[i] == Vector2.Zero)
                    continue;

                Vector2 point = Projectile.oldPos[i] + Projectile.Size * 0.5f;
                if (Vector2.DistanceSquared(point, lastPoint) < 4f)
                    continue;

                points[count++] = point;
                lastPoint = point;
            }

            while (count < 3)
            {
                points[count] = Projectile.Center - Projectile.velocity * count;
                count++;
            }

            Array.Resize(ref points, count);
            return points;
        }

        private float PressureTrailWidth(float completion, Vector2 _)
        {
            return TaperedTrailWidth(completion, 14f * Projectile.scale / 1.55f, 0.075f, 0.62f);
        }

        private Color PressureTrailColor(float completion, Vector2 _)
        {
            float tailFade = 1f - Utils.GetLerpValue(0.58f, 1f, completion, true);
            Color pressureColor = Color.Lerp(new Color(172, 96, 13), new Color(78, 40, 5), completion);
            pressureColor.A = 0;
            return pressureColor * (tailFade * 0.58f);
        }

        private float ThermalTrailWidth(float completion, Vector2 _)
        {
            return TaperedTrailWidth(completion, 8.2f * Projectile.scale / 1.55f, 0.065f, 0.7f);
        }

        private Color ThermalTrailColor(float completion, Vector2 _)
        {
            float tailFade = 1f - Utils.GetLerpValue(0.7f, 1f, completion, true);
            Color thermalColor = Color.Lerp(new Color(255, 244, 190), new Color(142, 79, 7),
                MathHelper.Clamp(completion * 1.25f, 0f, 1f));
            thermalColor.A = 0;
            return thermalColor * (tailFade * 0.92f);
        }

        private float PenetratorCoreWidth(float completion, Vector2 _)
        {
            return TaperedTrailWidth(completion, 3.35f * Projectile.scale / 1.55f, 0.055f, 0.78f);
        }

        private Color PenetratorCoreColor(float completion, Vector2 _)
        {
            float tailFade = 1f - Utils.GetLerpValue(0.6f, 1f, completion, true);
            Color coreColor = Color.Lerp(new Color(255, 255, 244), new Color(255, 190, 52), completion * 0.72f);
            coreColor.A = 0;
            return coreColor * tailFade;
        }

        private static float TaperedTrailWidth(float completion, float maximumWidth, float headFraction, float tailPower)
        {
            if (completion < headFraction)
            {
                float headCompletion = MathHelper.Clamp(completion / headFraction, 0f, 1f);
                return MathHelper.SmoothStep(0.4f, maximumWidth, headCompletion);
            }

            float tailCompletion = MathHelper.Clamp((completion - headFraction) / (1f - headFraction), 0f, 1f);
            return maximumWidth * MathF.Pow(1f - tailCompletion, tailPower);
        }

        private static void DrawCompressionChevrons(Vector2 drawCenter, Vector2 forward, Vector2 normal, float pulse)
        {
            Texture2D lineTexture = ModContent.Request<Texture2D>("CalamityMod/Particles/DrainLineBloom").Value;
            Vector2 origin = lineTexture.Size() * 0.5f;

            for (int chevron = 0; chevron < 2; chevron++)
            {
                float depth = 38f + chevron * 58f;
                float armLength = 22f + chevron * 7f;
                float halfWidth = 9f + chevron * 3f;
                float strength = chevron == 0 ? 0.72f : 0.4f;
                Vector2 vertex = drawCenter - forward * depth;

                for (int sign = -1; sign <= 1; sign += 2)
                {
                    Vector2 arm = -forward * armLength + normal * halfWidth * sign;
                    Vector2 armCenter = vertex + arm * 0.5f;
                    float rotation = arm.ToRotation() + MathHelper.PiOver2;
                    float lengthScale = arm.Length() / lineTexture.Height;

                    Main.EntitySpriteDraw(lineTexture, armCenter, null, new Color(162, 85, 9, 0) * strength,
                        rotation, origin, new Vector2(0.2f, lengthScale) * pulse, SpriteEffects.None, 0f);
                    Main.EntitySpriteDraw(lineTexture, armCenter, null, new Color(255, 224, 126, 0) * strength,
                        rotation, origin, new Vector2(0.075f, lengthScale * 0.92f) * pulse, SpriteEffects.None, 0f);
                }
            }
        }
    }

    internal sealed class AMRMarkerGlobalNPC : GlobalNPC
    {
        public override bool InstancePerEntity => true;

        private readonly int[] markerTimers = new int[Main.maxPlayers];
        private readonly int[] markerDamages = new int[Main.maxPlayers];

        public override void PostAI(NPC npc)
        {
            for (int i = 0; i < markerTimers.Length; i++)
            {
                if (markerTimers[i] > 0)
                    markerTimers[i]--;
                else
                    markerDamages[i] = 0;
            }
        }

        internal void SetMarker(int owner, int damage)
        {
            if (owner < 0 || owner >= markerTimers.Length)
                return;

            markerTimers[owner] = 5 * 60;
            markerDamages[owner] = Math.Max(1, damage);
        }

        internal bool TryConsumeMarker(int owner, int detonatorDamage, out int damage)
        {
            damage = 0;
            if (owner < 0 || owner >= markerTimers.Length || markerTimers[owner] <= 0)
                return false;

            damage = Math.Max((int)(detonatorDamage * 1.8f), markerDamages[owner] * 2);
            markerTimers[owner] = 0;
            markerDamages[owner] = 0;
            return true;
        }
    }

    internal sealed class AMROnyxDetonation : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.AntiMaterielRifle";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetDefaults()
        {
            Projectile.width = 96;
            Projectile.height = 96;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 3;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }

        public override bool ShouldUpdatePosition() => false;

        public override void AI()
        {
            int targetIndex = (int)Projectile.ai[0];
            if (targetIndex >= 0 && targetIndex < Main.maxNPCs && Main.npc[targetIndex].active)
                Projectile.Center = Main.npc[targetIndex].Center;

            if (Projectile.timeLeft == 3)
            {
                SoundEngine.PlaySound(SoundID.Item14 with { Volume = 0.75f, Pitch = -0.35f }, Projectile.Center);
                for (int i = 0; i < 28; i++)
                {
                    Dust dust = Dust.NewDustPerfect(Projectile.Center, DustID.PurpleTorch,
                        Main.rand.NextVector2Unit() * Main.rand.NextFloat(3f, 12f), 50,
                        Main.rand.NextBool() ? new Color(233, 102, 238) : new Color(255, 202, 81),
                        Main.rand.NextFloat(0.9f, 1.7f));
                    dust.noGravity = true;
                }
            }
        }

        public override bool? CanHitNPC(NPC target)
        {
            return target.whoAmI == (int)Projectile.ai[0] ? null : false;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            float completion = 1f - Projectile.timeLeft / 3f;
            float scale = MathHelper.Lerp(0.15f, 1.25f, completion);

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, Main.DefaultSamplerState,
                DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
            Main.EntitySpriteDraw(bloom, Projectile.Center - Main.screenPosition, null,
                new Color(233, 102, 238) * (1f - completion), 0f, bloom.Size() * 0.5f, scale,
                SpriteEffects.None, 0f);
            Main.EntitySpriteDraw(bloom, Projectile.Center - Main.screenPosition, null,
                new Color(255, 245, 213) * (0.8f - completion * 0.6f), 0f, bloom.Size() * 0.5f,
                scale * 0.45f, SpriteEffects.None, 0f);
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState,
                DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
            return false;
        }
    }
}
