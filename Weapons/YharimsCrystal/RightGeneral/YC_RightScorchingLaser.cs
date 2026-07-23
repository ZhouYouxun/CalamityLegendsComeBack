using System;
using CalamityLegendsComeBack.Weapons.YharimsCrystal.Passive;
using CalamityMod;
using CalamityMod.Graphics.Primitives;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.Enums;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.YharimsCrystal.RightGeneral
{
    internal sealed class YC_RightScorchingLaser : ModProjectile, ILocalizedModType
    {
        private const float MaxBeamLength = 2600f;
        private const int SampleCount = 3;
        private const int SplitBeamLifetime = 48;
        // 重击后基底激光保持增粗状态的帧数
        public const int EmpowerDuration = 85;
        private static readonly Color LaserRed = new(255, 70, 32);
        private static readonly Color LaserGold = new(255, 216, 92);
        private static readonly Color PristineGold = new(255, 224, 78);

        private readonly BalanceYharimsCrystal balance = new();
        private bool wasEmpowered;

        public new string LocalizationCategory => "Projectiles.YharimsCrystal";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        // ai[0] >= 0：附着模式，值为持械弹幕索引（常驻基底激光）
        // ai[0] == -1：自由独立激光
        // ai[0] <= -2：分裂激光，锚定持械索引 = -ai[0] - 2
        // 附着模式 ai[1]：重击增粗剩余帧数；分裂模式 ai[1]：宽度倍率，ai[2]：每帧扇形漂移弧度
        // localAI[2]：充能状态（0=普通, 1=聚合, 2=散射）——仅附着模式使用
        private bool IsStandalone => Projectile.ai[0] < 0f;
        private int AnchorIndex => Projectile.ai[0] >= 0f ? (int)Projectile.ai[0] : (int)(-Projectile.ai[0]) - 2;
        private ref float EmpowerFramesLeft => ref Projectile.ai[1];
        private float SplitScale => Projectile.ai[1] <= 0f ? 1f : Projectile.ai[1];
        private float FanDrift => Projectile.ai[2];
        private ref float Timer => ref Projectile.localAI[0];
        private ref float BeamLength => ref Projectile.localAI[1];
        private ref float ChargeState => ref Projectile.localAI[2]; // 0=normal,1=converged,2=scattered
        private YCRightLaserVisualTier Tier => balance.GetRightLaserTier();
        private bool Empowered => !IsStandalone && EmpowerFramesLeft > 0f;
        // Converged: beam is thicker (+60%), gold-white tinted, limited turn speed
        private bool IsConverged => !IsStandalone && ChargeState >= 1f && ChargeState < 1.5f;
        // Scattered: beam is thinner (80%), faster turn, paler coloring
        private bool IsScattered => !IsStandalone && ChargeState >= 1.5f;

        private float EmpowerBoost
        {
            get
            {
                if (!Empowered)
                    return 1f;

                float ramp = Utils.GetLerpValue(EmpowerDuration, EmpowerDuration - 7f, EmpowerFramesLeft, true);
                float fade = Utils.GetLerpValue(0f, 16f, EmpowerFramesLeft, true);
                return 1f + 1.5f * ramp * fade;
            }
        }

        public static void Empower(Projectile laser)
        {
            laser.ai[1] = EmpowerDuration;
            laser.netUpdate = true;
        }

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.DrawScreenCheckFluff[Type] = 10000;
        }

        public override void SetDefaults()
        {
            Projectile.width = 22;
            Projectile.height = 22;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 6;
            Projectile.timeLeft = 2;
        }

        public override bool ShouldUpdatePosition() => false;

        public override void OnSpawn(Terraria.DataStructures.IEntitySource source)
        {
            YharimsCrystalHellBladeGlobalProjectile.Mark(Projectile, YCWeaponForm.Crystal);
            if (IsStandalone)
            {
                Projectile.timeLeft = SplitBeamLifetime;
                return;
            }
            SoundEngine.PlaySound(new SoundStyle("CalamityMod/Sounds/Item/PlasmaBolt") { Volume = 0.42f, Pitch = -0.15f, MaxInstances = 4 }, Projectile.Center);
        }

        public override void AI()
        {
            if (IsStandalone)
            {
                Timer++;

                // 分裂激光锚定在水晶炮口上，方向按自身漂移量持续张开
                if (TryGetHoldout(AnchorIndex, out _, out YC_RightCrystalHoldout anchor))
                    Projectile.Center = anchor.Muzzle;

                if (FanDrift != 0f)
                    Projectile.velocity = Projectile.velocity.RotatedBy(FanDrift);

                Projectile.velocity = Projectile.velocity.SafeNormalize(Vector2.UnitX);
                Projectile.rotation = Projectile.velocity.ToRotation();
                Projectile.scale = GetBeamScale() * SplitScale;
                UpdateBeamLength();
                EmitBeamFX();
                if (Projectile.ai[0] > -2f)
                    EmitBeamStreamFX();
                CastBeamLight();
                // timeLeft decrements naturally — no refresh
                return;
            }

            if (!TryGetHoldout(AnchorIndex, out Projectile holdoutProjectile, out YC_RightCrystalHoldout holdout))
            {
                Projectile.Kill();
                return;
            }

            Player owner = Main.player[Projectile.owner];
            Timer++;
            Projectile.timeLeft = 2;
            Projectile.Center = holdout.Muzzle;
            Projectile.velocity = holdout.ForwardDirection;
            Projectile.rotation = Projectile.velocity.ToRotation();

            // Sync charge state from holdout (Projectile.owner only)
            if (Projectile.owner == Main.myPlayer)
                ChargeState = holdout.ChargeRatio >= 1f
                    ? (holdout.IsScatteredMode ? 2f : 1f)
                    : 0f;

            // Scale: converged = +60% thicker, scattered = 80% normal, else normal
            float chargeScaleMult = IsConverged ? 1.6f : (IsScattered ? 0.8f : 1.0f);
            Projectile.scale = GetBeamScale() * EmpowerBoost * chargeScaleMult;

            float empower = owner.GetModPlayer<YharimsCrystalStatePlayer>().CrystalEmpowered ? 1.35f : 1f;
            float sustain = Empowered ? 2.4f : 0.55f;
            // Converged mode deals 1.4x damage, scattered deals 0.85x
            float chargeDamageMult = IsConverged ? 1.4f : (IsScattered ? 0.85f : 1f);
            Projectile.damage = Math.Max(1, (int)(holdoutProjectile.damage * GetDamageMultiplier() * empower * sustain * chargeDamageMult));

            UpdateBeamLength();

            bool empoweredNow = EmpowerFramesLeft > 0f;
            if (empoweredNow && !wasEmpowered)
                TriggerEmpowerBurst();
            wasEmpowered = empoweredNow;
            if (empoweredNow)
                EmpowerFramesLeft--;

            EmitBeamFX();
            if (empoweredNow)
            {
                EmitBeamFX();
                EmitEmpoweredTipFX();
                EmitEmpoweredSideSpray();
            }
            // Converged: emit extra tip glow + beam sparkle
            if (IsConverged && !empoweredNow)
                EmitConvergedFX();
            if (Projectile.ai[0] > -2f)
                EmitBeamStreamFX();
            CastBeamLight();
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(balance.GetFireDebuffType(), 240);
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            if (Projectile.velocity == Vector2.Zero || BeamLength <= 0f)
                return false;

            float collisionPoint = 0f;
            return Collision.CheckAABBvLineCollision(
                targetHitbox.TopLeft(),
                targetHitbox.Size(),
                Projectile.Center,
                Projectile.Center + Projectile.velocity * BeamLength,
                GetCollisionWidth(),
                ref collisionPoint);
        }

        public override void CutTiles()
        {
            if (Projectile.velocity == Vector2.Zero || BeamLength <= 0f)
                return;

            DelegateMethods.tilecut_0 = TileCuttingContext.AttackProjectile;
            Utils.PlotTileLine(Projectile.Center, Projectile.Center + Projectile.velocity * BeamLength, GetCollisionWidth() + 8f, DelegateMethods.CutTiles);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Projectile.velocity == Vector2.Zero || BeamLength <= 0f)
                return false;

            if (IsStandalone)
            {
                DrawProvidenceStyleBeam(PristineGold);
                return false;
            }

            DrawClassicLaser();
            switch (Tier)
            {
                case YCRightLaserVisualTier.Cell:
                    DrawCellLaser();
                    break;
                case YCRightLaserVisualTier.MoonLord:
                    DrawMoonLordLaser();
                    break;
                case YCRightLaserVisualTier.Providence:
                    DrawProvidenceLaser();
                    break;
                case YCRightLaserVisualTier.Yharon:
                    DrawYharonLaser();
                    break;
            }

            // PF 圣光射线风格的基底主激光：右键持械期间永远绘制
            // 聚合态：金白色；散射态：橙色偏淡；普通：混合色
            Color baseBeamColor = IsConverged
                ? Color.Lerp(LaserGold, Color.White, 0.22f)
                : (IsScattered
                    ? Color.Lerp(LaserRed, LaserGold, 0.3f)
                    : Color.Lerp(LaserRed, LaserGold, 0.55f));
            DrawProvidenceStyleBeam(baseBeamColor);

            // Converged: draw an extra outer glow ring at the beam tip
            if (IsConverged)
            {
                Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
                Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitX);
                Vector2 tipPos = Projectile.Center + direction * BeamLength - Main.screenPosition;
                float pulse = 0.88f + 0.12f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 22f);
                Main.EntitySpriteDraw(bloom, tipPos, null, new Color(255, 240, 160, 0) * 0.58f * pulse, 0f, bloom.Size() * 0.5f, Projectile.scale * 0.24f * pulse, SpriteEffects.None);
                Main.EntitySpriteDraw(bloom, tipPos, null, Color.White with { A = 0 } * 0.32f * pulse, 0f, bloom.Size() * 0.5f, Projectile.scale * 0.12f, SpriteEffects.None);
            }

            return false;
        }

        private static bool TryGetHoldout(int index, out Projectile holdoutProjectile, out YC_RightCrystalHoldout holdout)
        {
            holdoutProjectile = null;
            holdout = null;

            if (index < 0 || index >= Main.maxProjectiles)
                return false;

            Projectile candidate = Main.projectile[index];
            if (!candidate.active ||
                candidate.type != ModContent.ProjectileType<YC_RightCrystalHoldout>() ||
                candidate.ModProjectile is not YC_RightCrystalHoldout holdoutMod)
            {
                return false;
            }

            holdoutProjectile = candidate;
            holdout = holdoutMod;
            return true;
        }

        private void UpdateBeamLength()
        {
            float[] samples = new float[SampleCount];
            float width = Math.Max(4f, GetCollisionWidth() * 0.35f);
            Collision.LaserScan(Projectile.Center, Projectile.velocity.SafeNormalize(Vector2.UnitX), width, MaxBeamLength, samples);

            float average = 0f;
            for (int i = 0; i < samples.Length; i++)
                average += samples[i];

            average /= samples.Length;
            if (average <= 0f)
                average = MaxBeamLength;

            BeamLength = MathHelper.Lerp(BeamLength <= 0f ? average : BeamLength, average, 0.58f);
        }

        private float GetBeamScale()
        {
            return Tier switch
            {
                YCRightLaserVisualTier.Cell => 0.62f,
                YCRightLaserVisualTier.Classic => 0.86f,
                YCRightLaserVisualTier.MoonLord => 1.12f,
                YCRightLaserVisualTier.Providence => 1.18f,
                YCRightLaserVisualTier.Yharon => 1.0f,
                _ => 0.86f,
            };
        }

        private float GetCollisionWidth()
        {
            return Tier switch
            {
                YCRightLaserVisualTier.Cell => 16f,
                YCRightLaserVisualTier.Classic => 26f,
                YCRightLaserVisualTier.MoonLord => 36f,
                YCRightLaserVisualTier.Providence => 32f,
                YCRightLaserVisualTier.Yharon => 34f,
                _ => 24f,
            } * Projectile.scale;
        }

        private float GetDamageMultiplier()
        {
            return Tier switch
            {
                YCRightLaserVisualTier.Cell => 0.86f,
                YCRightLaserVisualTier.Classic => 1.0f,
                YCRightLaserVisualTier.MoonLord => 1.16f,
                YCRightLaserVisualTier.Providence => 1.28f,
                YCRightLaserVisualTier.Yharon => 1.42f,
                _ => 1f,
            };
        }

        private void TriggerEmpowerBurst()
        {
            if (BeamLength <= 0f)
                UpdateBeamLength();

            Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            Vector2 endPoint = Projectile.Center + direction * BeamLength;

            if (!Main.dedServ)
            {
                SoundEngine.PlaySound(new SoundStyle("CalamityMod/Sounds/Item/LargeWeaponFire") { Volume = 0.5f, Pitch = -0.1f, MaxInstances = 3 }, Projectile.Center);
                GeneralParticleHandler.SpawnParticle(new StrongBloom(Projectile.Center, Vector2.Zero, LaserGold, 0.9f, 14));
                GeneralParticleHandler.SpawnParticle(new StrongBloom(endPoint, Vector2.Zero, Color.Lerp(LaserRed, LaserGold, 0.4f), 1.02f, 16));
                GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(endPoint, Vector2.Zero, LaserGold, Vector2.One, Main.rand.NextFloat(MathHelper.TwoPi), 0.1f, 1.6f, 20));

                for (int i = 0; i < 16; i++)
                {
                    Vector2 sparkVelocity = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(3.5f, 11f);
                    Dust dust = Dust.NewDustPerfect(endPoint, Main.rand.NextBool(4) ? 267 : DustID.GoldFlame, sparkVelocity, 0, Main.rand.NextBool(3) ? Color.White : LaserGold, Main.rand.NextFloat(0.9f, 1.35f));
                    dust.noGravity = true;
                }
            }

            if (Projectile.owner == Main.myPlayer)
            {
                int blast = Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    endPoint,
                    Vector2.Zero,
                    ModContent.ProjectileType<YC_LaserBlast>(),
                    Math.Max(1, (int)(Projectile.damage * 1.5f)),
                    Projectile.knockBack,
                    Projectile.owner);
                if (Main.projectile.IndexInRange(blast))
                {
                    YharimsCrystalHellBladeGlobalProjectile.Mark(Main.projectile[blast], YCWeaponForm.Crystal);
                    Main.projectile[blast].CritChance = Projectile.CritChance;
                }
            }
        }

        private void CastBeamLight()
        {
            Color lightColor = Color.Lerp(LaserRed, LaserGold, 0.42f);
            DelegateMethods.v3_1 = lightColor.ToVector3() * 0.46f;
            Utils.PlotTileLine(Projectile.Center, Projectile.Center + Projectile.velocity * BeamLength, GetCollisionWidth(), DelegateMethods.CastLight);
        }

        private void EmitBeamFX()
        {
            if (Main.dedServ || Main.GameUpdateCount % 4 != 0)
                return;

            Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            Vector2 position = Projectile.Center + direction * Main.rand.NextFloat(BeamLength);
            Color color = Main.rand.NextBool(4) ? Color.White : Color.Lerp(LaserRed, LaserGold, Main.rand.NextFloat(0.2f, 0.85f));

            GeneralParticleHandler.SpawnParticle(new PointParticle(
                position + Main.rand.NextVector2Circular(8f, 8f),
                direction.RotatedByRandom(0.84f) * Main.rand.NextFloat(1.1f, 4.2f),
                false,
                Main.rand.Next(12, 22),
                Main.rand.NextFloat(0.72f, 1.2f),
                color,
                true));
        }

        private void EmitBeamStreamFX()
        {
            // 沿激光全长向水晶方向回流的光点流。
            // 只允许小型辉光球（GlowOrbParticle）/发光点粒子（PointParticle）；
            // 禁用大型拉伸光斑 GlowSparkParticle——体积太大，已被明确否决。
            if (Main.dedServ || Projectile.velocity == Vector2.Zero || BeamLength <= 0f)
                return;

            Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            float halfWidth = GetCollisionWidth() * 0.45f;
            int streamCount = (int)MathHelper.Clamp(BeamLength / 70f, 3f, 18f);

            for (int i = 0; i < streamCount; i++)
            {
                float along = Main.rand.NextFloat(BeamLength);
                Vector2 perpendicular = direction.RotatedBy(MathHelper.PiOver2) * Main.rand.NextFloat(-halfWidth, halfWidth);
                Vector2 position = Projectile.Center + direction * along + perpendicular;
                Vector2 velocity = -direction * Main.rand.NextFloat(9f, 20f) + direction.RotatedByRandom(0.3f) * Main.rand.NextFloat(-2f, 2f);
                Color color = Main.rand.NextBool(3) ? Color.White : Color.Lerp(LaserRed, LaserGold, Main.rand.NextFloat(0.2f, 0.9f));

                if (Main.rand.NextBool())
                {
                    GeneralParticleHandler.SpawnParticle(new GlowOrbParticle(
                        position,
                        velocity,
                        false,
                        Main.rand.Next(9, 15),
                        Main.rand.NextFloat(0.26f, 0.5f) * MathHelper.Clamp(Projectile.scale, 0.5f, 1.6f),
                        color));
                }
                else
                {
                    GeneralParticleHandler.SpawnParticle(new PointParticle(
                        position,
                        velocity * 0.6f,
                        false,
                        Main.rand.Next(10, 16),
                        Main.rand.NextFloat(0.6f, 1.05f),
                        color,
                        true));
                }
            }
        }

        private void EmitEmpoweredTipFX()
        {
            if (Main.dedServ || Main.GameUpdateCount % 2 != 0)
                return;

            Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            Vector2 endPoint = Projectile.Center + direction * BeamLength;

            for (int i = 0; i < 3; i++)
            {
                GeneralParticleHandler.SpawnParticle(new GlowOrbParticle(
                    endPoint + Main.rand.NextVector2Circular(10f, 10f),
                    -direction.RotatedByRandom(0.9f) * Main.rand.NextFloat(3f, 9f),
                    false,
                    Main.rand.Next(8, 14),
                    Main.rand.NextFloat(0.3f, 0.6f),
                    Main.rand.NextBool(3) ? Color.White : LaserGold));
            }
        }

        // 重击期间：激光沿身向两侧喷出大量光点，并成对甩出金色光弹
        private void EmitEmpoweredSideSpray()
        {
            if (Projectile.velocity == Vector2.Zero || BeamLength <= 0f)
                return;

            Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            Vector2 perpendicular = direction.RotatedBy(MathHelper.PiOver2);

            if (!Main.dedServ)
            {
                int sprayCount = (int)MathHelper.Clamp(BeamLength / 110f, 3f, 12f);
                for (int i = 0; i < sprayCount; i++)
                {
                    float along = Main.rand.NextFloat(BeamLength);
                    int side = Main.rand.NextBool() ? 1 : -1;
                    Vector2 position = Projectile.Center + direction * along;
                    Vector2 velocity = perpendicular * side * Main.rand.NextFloat(5f, 14f) + direction * Main.rand.NextFloat(-1.5f, 1.5f);
                    Color color = Main.rand.NextBool(3) ? Color.White : Color.Lerp(LaserRed, LaserGold, Main.rand.NextFloat(0.25f, 0.9f));

                    if (Main.rand.NextBool())
                        GeneralParticleHandler.SpawnParticle(new GlowOrbParticle(position, velocity, false, Main.rand.Next(10, 17), Main.rand.NextFloat(0.3f, 0.58f), color));
                    else
                        GeneralParticleHandler.SpawnParticle(new PointParticle(position, velocity, false, Main.rand.Next(10, 16), Main.rand.NextFloat(0.65f, 1.1f), color, true));
                }
            }

            // 额外弹幕：从激光上随机一点向两侧同时甩出金色光弹（使用 BackgroundCrystalBolt 替代旧 DroneShot）
            if (Projectile.owner == Main.myPlayer && Main.GameUpdateCount % 5 == 0)
            {
                float along = Main.rand.NextFloat(BeamLength * 0.15f, BeamLength * 0.9f);
                Vector2 position = Projectile.Center + direction * along;
                for (int side = -1; side <= 1; side += 2)
                {
                    Vector2 velocity = (perpendicular * side).RotatedByRandom(0.22f) * Main.rand.NextFloat(9f, 13f);
                    int bolt = Projectile.NewProjectile(
                        Projectile.GetSource_FromThis(),
                        position,
                        velocity,
                        ModContent.ProjectileType<Passive.YC_BackgroundCrystalBolt>(),
                        Math.Max(1, (int)(Projectile.damage * 0.4f)),
                        Projectile.knockBack * 0.4f,
                        Projectile.owner);
                    if (Main.projectile.IndexInRange(bolt))
                    {
                        YharimsCrystalHellBladeGlobalProjectile.Mark(Main.projectile[bolt], YCWeaponForm.Crystal);
                        Main.projectile[bolt].CritChance = Projectile.CritChance;
                    }
                }
            }
        }

        private void DrawCellLaser()
        {
            Texture2D outer = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomLineFade").Value;
            Texture2D inner = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomLineThick").Value;
            Texture2D glow = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Vector2 start = Projectile.Center - Main.screenPosition;
            Vector2 unit = Projectile.rotation.ToRotationVector2();
            Vector2 beamCenter = start + unit * BeamLength * 0.5f;
            float beamLengthScale = BeamLength / 1000f;
            float opacity = LaserOpacity();
            Color outerColor = LaserRed with { A = 0 };
            Color innerColor = Color.White with { A = 0 };

            Main.EntitySpriteDraw(outer, beamCenter, null, outerColor * opacity, Projectile.rotation + MathHelper.PiOver2, outer.Size() * 0.5f, new Vector2(1.5f, 55f * beamLengthScale) * Projectile.scale * 0.01f, SpriteEffects.FlipVertically);
            Main.EntitySpriteDraw(inner, beamCenter, null, innerColor * opacity, Projectile.rotation + MathHelper.PiOver2, inner.Size() * 0.5f, new Vector2(0.32f, 55f * beamLengthScale) * Projectile.scale * 0.01f, SpriteEffects.FlipVertically);
            Main.EntitySpriteDraw(glow, start + unit * 7f, null, LaserGold with { A = 0 } * opacity, Projectile.rotation, glow.Size() * 0.5f, Projectile.scale * 0.08f, SpriteEffects.None);
        }

        private void DrawClassicLaser()
        {
            YC_YharimBeamVisuals.DrawYharimBeam(Projectile, BeamLength, Projectile.scale * 0.48f, LaserOpacity(), Color.Lerp(LaserRed, LaserGold, 0.5f));
        }

        private void DrawMoonLordLaser()
        {
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Texture2D ring = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomRing").Value;
            Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            Vector2 start = Projectile.Center - Main.screenPosition;
            Vector2 end = Projectile.Center + direction * BeamLength - Main.screenPosition;
            float opacity = LaserOpacity();
            Color color = Color.Lerp(LaserRed, Color.LimeGreen, 0.22f) with { A = 0 };

            for (int i = 0; i < 5; i++)
            {
                float rotation = Main.GlobalTimeWrappedHourly * (1.1f + i * 0.14f) + MathHelper.TwoPi * i / 5f;
                Main.EntitySpriteDraw(ring, start, null, color * 0.18f * opacity, rotation, ring.Size() * 0.5f, Projectile.scale * (0.18f + i * 0.018f), SpriteEffects.None);
                Main.EntitySpriteDraw(bloom, end, null, color * 0.22f * opacity, -rotation, bloom.Size() * 0.5f, Projectile.scale * (0.18f + i * 0.02f), SpriteEffects.None);
            }
        }

        private void DrawProvidenceLaser()
        {
            Texture2D startTex = ModContent.Request<Texture2D>("CalamityMod/Projectiles/Boss/ProvidenceHolyRay").Value;
            Texture2D midTex = ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/Lasers/ProvidenceHolyRayMid").Value;
            Texture2D endTex = ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/Lasers/ProvidenceHolyRayEnd").Value;
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            float rotation = direction.ToRotation() - MathHelper.PiOver2;
            float drawScale = Projectile.scale * 0.45f;
            float opacity = LaserOpacity();
            Color color = Color.Lerp(LaserRed, LaserGold, 0.52f) with { A = 0 };
            Vector2 start = Projectile.Center;
            Vector2 drawStart = start - Main.screenPosition;

            Main.spriteBatch.Draw(startTex, drawStart, null, color * opacity, rotation, startTex.Size() / 2f, drawScale, SpriteEffects.None, 0f);

            float currentLength = BeamLength - (startTex.Height / 2f + endTex.Height) * drawScale;
            Vector2 center = start + direction * drawScale * startTex.Height / 2f;
            if (currentLength > 0f)
            {
                float lengthDrawn = 0f;
                int frameHeight = 36;
                int frameY = frameHeight * ((int)Timer / 3 % 4);
                Rectangle sourceRect = new(0, frameY, midTex.Width, frameHeight);
                while (lengthDrawn + 1f < currentLength)
                {
                    if (currentLength - lengthDrawn < frameHeight * drawScale)
                        sourceRect.Height = (int)((currentLength - lengthDrawn) / drawScale);
                    if (sourceRect.Height <= 0)
                        break;

                    Main.spriteBatch.Draw(midTex, center - Main.screenPosition, sourceRect, color * opacity, rotation, new Vector2(sourceRect.Width / 2f, 0f), drawScale, SpriteEffects.None, 0f);
                    lengthDrawn += sourceRect.Height * drawScale;
                    center += direction * sourceRect.Height * drawScale;
                    sourceRect.Y += frameHeight;
                    if (sourceRect.Y + sourceRect.Height > midTex.Height)
                        sourceRect.Y = 0;
                }
            }

            Main.spriteBatch.Draw(endTex, center - Main.screenPosition, null, color * opacity, rotation, new Vector2(endTex.Width / 2f, 0f), drawScale, SpriteEffects.None, 0f);
            Main.EntitySpriteDraw(bloom, drawStart, null, LaserGold with { A = 0 } * 0.62f * opacity, 0f, bloom.Size() * 0.5f, Projectile.scale * 0.16f, SpriteEffects.None);
        }

        // PF 水晶激光同款的 ProvidenceHolyRay 三段式绘制。
        // 注意：颜色 A=0 走默认 AlphaBlend（预乘加算）即可，绝不能再套 BlendState.Additive，
        // Additive 的源因子是 SourceAlpha，配 A=0 会把整条激光乘成全透明。
        private void DrawProvidenceStyleBeam(Color beamColor)
        {
            Texture2D startTex = ModContent.Request<Texture2D>("CalamityMod/Projectiles/Boss/ProvidenceHolyRay").Value;
            Texture2D midTex = ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/Lasers/ProvidenceHolyRayMid").Value;
            Texture2D endTex = ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/Lasers/ProvidenceHolyRayEnd").Value;
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            float rotation = direction.ToRotation() - MathHelper.PiOver2;
            float drawScale = Projectile.scale * 0.56f;
            float opacity = LaserOpacity();
            Color color = beamColor with { A = 0 };
            Vector2 drawStart = Projectile.Center - Main.screenPosition;

            Main.EntitySpriteDraw(startTex, drawStart, null, color * opacity, rotation, startTex.Size() * 0.5f, drawScale, SpriteEffects.None);

            float currentLength = BeamLength - (startTex.Height * 0.5f + endTex.Height) * drawScale;
            Vector2 center = Projectile.Center + direction * drawScale * startTex.Height * 0.5f;
            if (currentLength > 0f)
            {
                float lengthDrawn = 0f;
                int frameHeight = 36;
                int frameY = frameHeight * ((int)Timer / 3 % 4);
                Rectangle sourceRect = new(0, frameY, midTex.Width, frameHeight);
                while (lengthDrawn + 1f < currentLength)
                {
                    if (currentLength - lengthDrawn < frameHeight * drawScale)
                        sourceRect.Height = (int)((currentLength - lengthDrawn) / drawScale);
                    if (sourceRect.Height <= 0)
                        break;

                    Main.EntitySpriteDraw(midTex, center - Main.screenPosition, sourceRect, color * opacity, rotation, new Vector2(sourceRect.Width * 0.5f, 0f), drawScale, SpriteEffects.None);
                    lengthDrawn += sourceRect.Height * drawScale;
                    center += direction * sourceRect.Height * drawScale;
                    sourceRect.Y += frameHeight;
                    if (sourceRect.Y + sourceRect.Height > midTex.Height)
                        sourceRect.Y = 0;
                }
            }

            Main.EntitySpriteDraw(endTex, center - Main.screenPosition, null, color * opacity, rotation, new Vector2(endTex.Width * 0.5f, 0f), drawScale, SpriteEffects.None);
            Main.EntitySpriteDraw(bloom, drawStart, null, Color.White with { A = 0 } * 0.42f * opacity, 0f, bloom.Size() * 0.5f, Projectile.scale * 0.18f, SpriteEffects.None);
        }

        private void DrawYharonLaser()
        {
            Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            Vector2[] points =
            {
                Projectile.Center,
                Projectile.Center + direction * BeamLength * 0.25f,
                Projectile.Center + direction * BeamLength * 0.55f,
                Projectile.Center + direction * BeamLength
            };

            Main.spriteBatch.EnterShaderRegion();
            GameShaders.Misc["CalamityMod:Bordernado"].UseSaturation(-0.05f);
            GameShaders.Misc["CalamityMod:Bordernado"].UseOpacity(LaserOpacity());
            GameShaders.Misc["CalamityMod:Bordernado"].SetShaderTexture(ModContent.Request<Texture2D>("Terraria/Images/Misc/Perlin"));
            PrimitiveRenderer.RenderTrail(points, new PrimitiveSettings(YharonWidthFunction, YharonColorFunction, shader: GameShaders.Misc["CalamityMod:Bordernado"]), 72);
            Main.spriteBatch.ExitShaderRegion();

            Texture2D vortex = ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/GreyscaleGradients/Cracks").Value;
            GameShaders.Misc["CalamityMod:DoGPortal"].UseOpacity(0.75f * LaserOpacity());
            GameShaders.Misc["CalamityMod:DoGPortal"].UseColor(LaserGold);
            GameShaders.Misc["CalamityMod:DoGPortal"].UseSecondaryColor(Color.White);
            Main.spriteBatch.EnterShaderRegion();
            GameShaders.Misc["CalamityMod:DoGPortal"].Apply();
            for (int i = 0; i < 4; i++)
            {
                float angle = MathHelper.TwoPi * i / 4f + Main.GlobalTimeWrappedHourly * MathHelper.TwoPi;
                Color drawColor = Color.White;
                drawColor.A = 0;
                Main.EntitySpriteDraw(vortex, Projectile.Center - Main.screenPosition + angle.ToRotationVector2() * 3f, null, drawColor, angle + MathHelper.PiOver2, vortex.Size() * 0.5f, Projectile.scale * 0.75f, SpriteEffects.None);
            }
            Main.spriteBatch.ExitShaderRegion();
        }

        private float LaserOpacity()
        {
            float fadeIn = Utils.GetLerpValue(0f, 12f, Timer, true);
            if (IsStandalone)
                return fadeIn * Utils.GetLerpValue(0f, 14f, Projectile.timeLeft, true);
            return fadeIn;
        }

        /// <summary>Sparkle FX emitted when beam is in converged (charged, no left-hold) state.</summary>
        private void EmitConvergedFX()
        {
            if (Main.dedServ || BeamLength <= 0f)
                return;

            Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            Vector2 tipPos = Projectile.Center + direction * BeamLength;

            // Tip sparkle: GenericSparkle burst (white/gold)
            if (Main.GameUpdateCount % 3 == 0)
            {
                GeneralParticleHandler.SpawnParticle(new GenericSparkle(
                    tipPos + Main.rand.NextVector2Circular(10f, 10f),
                    direction.RotatedByRandom(0.5f) * Main.rand.NextFloat(-2f, -0.5f),
                    Color.White,
                    LaserGold,
                    Main.rand.NextFloat(0.3f, 0.55f),
                    Main.rand.Next(10, 18),
                    Main.rand.NextFloat(-0.1f, 0.1f),
                    2.6f));
            }

            // Beam body: occasional drift spark along the beam
            if (Main.rand.NextBool(4))
            {
                float along = Main.rand.NextFloat(BeamLength * 0.1f, BeamLength);
                Vector2 pos = Projectile.Center + direction * along;
                GeneralParticleHandler.SpawnParticle(new CustomSpark(
                    pos,
                    direction.RotatedByRandom(0.35f) * Main.rand.NextFloat(-3f, -1f),
                    "CalamityMod/Particles/Sparkle",
                    false,
                    Main.rand.Next(8, 14),
                    Main.rand.NextFloat(0.28f, 0.52f) * Projectile.scale,
                    Main.rand.NextBool(3) ? Color.White : LaserGold,
                    new Vector2(0.24f, 1.0f),
                    true, true,
                    shrinkSpeed: 0.18f));
            }
        }

        private float YharonWidthFunction(float completionRatio, Vector2 vertexPosition)
        {
            float endFade = Utils.GetLerpValue(1f, 0.82f, completionRatio, true);
            return 41f * Projectile.scale * endFade;
        }

        private Color YharonColorFunction(float completionRatio, Vector2 vertexPosition)
        {
            return Color.Lerp(LaserGold, LaserRed, completionRatio * 0.45f) * LaserOpacity();
        }
    }

    // 重击瞬间在基底激光命中点引爆的范围爆炸
    internal sealed class YC_LaserBlast : ModProjectile, ILocalizedModType
    {
        private static readonly Color BlastGold = new(255, 216, 92);
        private static readonly Color BlastRed = new(255, 70, 32);

        private readonly BalanceYharimsCrystal balance = new();

        public new string LocalizationCategory => "Projectiles.YharimsCrystal";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetDefaults()
        {
            Projectile.width = 176;
            Projectile.height = 176;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 4;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }

        public override bool ShouldUpdatePosition() => false;

        public override void AI()
        {
            if (Projectile.localAI[0]++ != 0f || Main.dedServ)
                return;

            SoundEngine.PlaySound(SoundID.Item14 with { Volume = 0.45f, Pitch = -0.1f }, Projectile.Center);
            GeneralParticleHandler.SpawnParticle(new StrongBloom(Projectile.Center, Vector2.Zero, Color.Lerp(BlastRed, BlastGold, 0.45f), 0.82f, 13));
            GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(Projectile.Center, Vector2.Zero, BlastGold, Vector2.One, Main.rand.NextFloat(MathHelper.TwoPi), 0.1f, 1.35f, 19));

            for (int i = 0; i < 10; i++)
            {
                Dust dust = Dust.NewDustPerfect(
                    Projectile.Center,
                    Main.rand.NextBool(4) ? 267 : DustID.GoldFlame,
                    Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(2.4f, 7.5f),
                    0,
                    Main.rand.NextBool(3) ? Color.White : BlastGold,
                    Main.rand.NextFloat(0.8f, 1.25f));
                dust.noGravity = true;
            }
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            float radius = Projectile.width * 0.5f;
            return Vector2.Distance(Projectile.Center, targetHitbox.ClosestPointInRect(Projectile.Center)) <= radius;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(balance.GetFireDebuffType(), 240);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            float fade = Utils.GetLerpValue(0f, 4f, Projectile.timeLeft, true);
            Main.EntitySpriteDraw(
                bloom,
                Projectile.Center - Main.screenPosition,
                null,
                (BlastGold with { A = 0 }) * 0.4f * fade,
                0f,
                bloom.Size() * 0.5f,
                1.3f,
                SpriteEffects.None);
            return false;
        }
    }
}
