using System;
using CalamityMod;
using CalamityMod.Buffs.StatDebuffs;
using CalamityMod.Dusts;
using CalamityMod.Enums;
using CalamityMod.Graphics.Primitives;
using CalamityMod.Particles;
using CalamityMod.Projectiles.BaseProjectiles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.Enums;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Upgrade.AntiMaterielRifle.Proj
{
    public class AMRBeamRound : BaseLaserbeamProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.AntiMaterielRifle";
        public override string Texture => "CalamityMod/Projectiles/Magic/YharimsCrystalBeam";
        public override Texture2D LaserBeginTexture => ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/Lasers/UltimaRayStart", AssetRequestMode.ImmediateLoad).Value;
        public override Texture2D LaserMiddleTexture => ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/Lasers/UltimaRayMid", AssetRequestMode.ImmediateLoad).Value;
        public override Texture2D LaserEndTexture => ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/Lasers/UltimaRayEnd", AssetRequestMode.ImmediateLoad).Value;

        public override float MaxScale => 0.85f + 1.25f * ChargePercent;
        public override float Lifetime => 18f;
        public override float MaxLaserLength => 2400f;

        public ref float ChargePercent => ref Projectile.ai[1];
        private bool IsMarkerRound => Projectile.ai[0] >= 0.5f;

        public override Color LaserOverlayColor => new Color(255, 205, 50);
        public override Color LightCastColor => LaserOverlayColor;

        public override void SetDefaults()
        {
            Projectile.width = 16;
            Projectile.height = 16;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.scale = MaxScale;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.timeLeft = (int)Lifetime;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
            Projectile.ArmorPenetration = 45;
        }

        public override void UpdateLaserMotion()
        {
            RotationalSpeed = 0f;
            Projectile.rotation = Projectile.velocity.ToRotation() - MathHelper.PiOver2;
        }

        public override void ExtraBehavior()
        {
            if (Projectile.timeLeft == (int)Lifetime)
            {
                Vector2 beamVector = Projectile.velocity;
                float beamLength = DetermineLaserLength_CollideWithTiles();

                if (Main.dedServ)
                    return;

                // 1. 破音音障冲击波 (Supersonic Shockwave Rings along the beam)
                float ringInterval = 150f;
                int ringCount = (int)(beamLength / ringInterval);
                for (int i = 0; i <= ringCount; i++)
                {
                    float dist = i * ringInterval;
                    if (dist > beamLength)
                        break;

                    Vector2 ringPos = Projectile.Center + beamVector * dist;

                    // 黑金双色相间音障环
                    Color ringColor = (i % 2 == 0)
                        ? new Color(255, 215, 0)
                        : new Color(35, 28, 20);

                    DirectionalPulseRing pulse = new DirectionalPulseRing(
                        ringPos,
                        beamVector * 0.25f,
                        ringColor,
                        new Vector2(0.35f, 1.45f),
                        beamVector.ToRotation(),
                        0.04f,
                        0.85f + 0.55f * ChargePercent,
                        22);
                    GeneralParticleHandler.SpawnParticle(pulse);

                    // 音障突破处散发黑金爆发火花
                    for (int k = 0; k < 4; k++)
                    {
                        Vector2 sparkVel = beamVector.RotatedBy(Main.rand.NextFloat(-0.4f, 0.4f)) * Main.rand.NextFloat(3f, 11f);
                        CritSpark spark = new CritSpark(
                            ringPos,
                            sparkVel,
                            Color.White,
                            i % 2 == 0 ? new Color(255, 200, 40) : new Color(30, 25, 20),
                            0.42f,
                            14,
                            0.05f,
                            2.4f);
                        GeneralParticleHandler.SpawnParticle(spark);
                    }
                }

                // 2. 狂暴黑金高能量粒子雨 (Dense Black & Gold Particles Along Beam)
                int stepCount = (int)(beamLength / 32f);
                for (int i = 0; i < stepCount; i++)
                {
                    float dustDist = Main.rand.NextFloat(0f, beamLength);
                    Vector2 dustPos = Projectile.Center + beamVector * dustDist + beamVector.RotatedBy(MathHelper.PiOver2) * Main.rand.NextFloat(-12f, 12f) * Projectile.scale;

                    Color mainColor = Main.rand.NextBool(3) ? new Color(25, 20, 15) : new Color(255, 200, 50);
                    Dust dust = Dust.NewDustPerfect(dustPos, DustID.GoldFlame, beamVector * Main.rand.NextFloat(6f, 32f), 0, mainColor, Main.rand.NextFloat(1.2f, 2.5f));
                    dust.noGravity = true;

                    if (i % 3 == 0)
                    {
                        Vector2 lineVel = beamVector.RotatedBy(Main.rand.NextFloat(-0.6f, 0.6f)) * Main.rand.NextFloat(4f, 16f);
                        LineParticle line = new LineParticle(
                            dustPos,
                            lineVel,
                            false,
                            12,
                            1.2f,
                            Main.rand.NextBool() ? new Color(255, 220, 100) : new Color(40, 32, 20));
                        GeneralParticleHandler.SpawnParticle(line);
                    }

                    if (i % 4 == 0)
                    {
                        Vector2 orbVel = -beamVector * Main.rand.NextFloat(1f, 4f);
                        GlowOrbParticle orb = new GlowOrbParticle(
                            dustPos,
                            orbVel,
                            false,
                            14,
                            Main.rand.NextFloat(0.35f, 0.6f),
                            Main.rand.NextBool() ? new Color(255, 210, 80) : new Color(20, 18, 15),
                            true,
                            false,
                            true);
                        GeneralParticleHandler.SpawnParticle(orb);
                    }
                }

                // 3. 墙体打入发热黑金弹壳 (Tile Collision Embedded Shell)
                if (beamLength < MaxLaserLength)
                {
                    Vector2 endPoint = beamLength * beamVector + Projectile.Center + beamVector * 8.5f;
                    Point anchorPos = new Point((int)endPoint.X / 16, (int)endPoint.Y / 16);

                    Color burnColor = Main.rand.NextBool() ? new Color(255, 185, 40) : new Color(255, 100, 20);
                    Particle shell = new TitaniumRailgunShell(endPoint, anchorPos, Projectile.rotation + MathHelper.PiOver2, burnColor);
                    GeneralParticleHandler.SpawnParticle(shell);

                    DirectionalPulseRing impactPulse = new DirectionalPulseRing(
                        endPoint,
                        Vector2.Zero,
                        new Color(255, 200, 60),
                        new Vector2(0.4f, 1.2f),
                        Projectile.rotation,
                        0.1f,
                        1.4f,
                        24);
                    GeneralParticleHandler.SpawnParticle(impactPulse);

                    for (int s = 0; s < 6; s++)
                    {
                        HeavySmokeParticle smoke = new HeavySmokeParticle(
                            endPoint,
                            -beamVector.RotatedByRandom(0.8f) * Main.rand.NextFloat(2f, 7f),
                            new Color(25, 20, 15),
                            25,
                            Main.rand.NextFloat(0.5f, 0.9f),
                            0.7f,
                            Main.rand.NextFloat(-0.03f, 0.03f),
                            false);
                        GeneralParticleHandler.SpawnParticle(smoke);
                    }
                }
            }
        }

        public override void DetermineScale() => Projectile.scale = Projectile.timeLeft / Lifetime * MaxScale;

        public override float DetermineLaserLength() => DetermineLaserLength_CollideWithTiles();

        public override bool ShouldUpdatePosition() => false;

        public override void CutTiles()
        {
            DelegateMethods.tilecut_0 = TileCuttingContext.AttackProjectile;
            Vector2 unit = Projectile.velocity;
            Utils.PlotTileLine(Projectile.Center, Projectile.Center + unit * LaserLength, Projectile.width + 24, DelegateMethods.CutTiles);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Projectile.velocity == Vector2.Zero)
                return false;

            float currentScale = Projectile.scale;

            // 1. 辉煌外金光晕 (Outer Gold Glow)
            Color outerGold = new Color(255, 180, 20, 0) * 0.9f;
            DrawBeamWithColor(outerGold, currentScale * 1.35f);

            // 2. 亮黄过渡层 (Middle Radiant Gold)
            Color midGold = new Color(255, 220, 100) * 0.95f;
            DrawBeamWithColor(midGold, currentScale * 1.0f);

            // 3. 核心深邃曜黑能量核 (Deep Obsidian Black Core)
            Color blackCore = new Color(15, 12, 10) * 0.95f;
            DrawBeamWithColor(blackCore, currentScale * 0.48f);

            // 4. 核心高能微粒闪光 (Inner Core Sparkle Line)
            Color innerSparkle = new Color(255, 245, 210) * 0.85f;
            DrawBeamWithColor(innerSparkle, currentScale * 0.22f);

            return false;
        }

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            modifiers.ScalingArmorPenetration += 0.55f;

            if (Projectile.owner >= 0 && Projectile.owner < Main.maxPlayers)
            {
                AMRPlayer player = Main.player[Projectile.owner].GetModPlayer<AMRPlayer>();
                modifiers.SourceDamage *= player.GetCalibrationMultiplier(target.whoAmI);
            }

            if (AMRBalance.CriticalOverflowUnlocked && Projectile.CritChance > 100)
                modifiers.CritDamage += (Projectile.CritChance - 100) / 100f;

            if (AMRBalance.CoreRuptureUnlocked)
                modifiers.CritDamage += 0.75f;

            if (!Main.dedServ)
            {
                Vector2 backward = -Projectile.velocity.SafeNormalize(Vector2.UnitX);
                for (int i = 0; i < 4; i++)
                {
                    LineParticle sparkLeft = new LineParticle(
                        target.Center,
                        backward.RotatedBy(Main.rand.NextFloat(0.18f, 0.5f)) * Main.rand.NextFloat(2f, 6f),
                        false,
                        12,
                        1.2f,
                        Main.rand.NextBool() ? new Color(25, 20, 15) : new Color(255, 200, 50));
                    GeneralParticleHandler.SpawnParticle(sparkLeft);

                    LineParticle sparkRight = new LineParticle(
                        target.Center,
                        backward.RotatedBy(Main.rand.NextFloat(-0.18f, -0.5f)) * Main.rand.NextFloat(2f, 6f),
                        false,
                        12,
                        1.2f,
                        Main.rand.NextBool() ? new Color(255, 215, 0) : new Color(35, 28, 18));
                    GeneralParticleHandler.SpawnParticle(sparkRight);
                }
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (AMRBalance.TryGetMaxLifeTrueDamage(target, out int finalTrueDamage))
            {
                target.life -= finalTrueDamage;
                CombatText.NewText(target.getRect(), new Color(255, 180, 30), finalTrueDamage, true);

                if (target.life <= 0)
                    target.checkDead();
            }

            if (AMRBalance.DeathMarkUnlocked)
            {
                target.AddBuff(ModContent.BuffType<MarkedforDeath>(), 5 * 60);
                int defenseLoss = Math.Max(25, (int)(target.defense * 0.6f));
                target.Calamity().miscDefenseLoss = Math.Max(target.Calamity().miscDefenseLoss, defenseLoss);
            }

            if (AMRBalance.MetalJetUnlocked && Projectile.owner == Main.myPlayer)
            {
                SpawnMetalJetProjectiles(target.Center);
            }

            if (AMRBalance.CoreRuptureUnlocked && Projectile.owner == Main.myPlayer)
            {
                SpawnSubBullets(target, hit.Crit);
            }

            if (AMRBalance.Stage >= AMRProgressionStage.DevourerOfGods && target.active)
            {
                bool isBossOrSpecial = target.boss || target.type == NPCID.TargetDummy || target.realLife >= 0;
                if (!isBossOrSpecial && target.lifeMax < 1000000)
                {
                    target.life = 0;
                    target.checkDead();
                }
            }

            if (Projectile.owner >= 0 && Projectile.owner < Main.maxPlayers)
                Main.player[Projectile.owner].GetModPlayer<AMRPlayer>().RegisterCalibrationHit(target.whoAmI);

            if (AMRBalance.OnyxSequenceUnlocked)
                ResolveOnyxSequence(target, damageDone);

            SpawnImpact(target.Center, hit.Crit);
        }

        private void SpawnMetalJetProjectiles(Vector2 impactCenter)
        {
            Vector2 forward = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            int count = 6;
            int shardDamage = (int)(Projectile.damage * 0.5f);

            for (int i = 0; i < count; i++)
            {
                float angle = MathHelper.ToRadians(Main.rand.NextFloat(-45f, 45f));
                Vector2 shardVel = forward.RotatedBy(angle) * Main.rand.NextFloat(8f, 20f);

                Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    impactCenter + forward * 10f,
                    shardVel,
                    ModContent.ProjectileType<AMRMetalJetShard>(),
                    Math.Max(1, shardDamage),
                    Projectile.knockBack * 0.3f,
                    Projectile.owner,
                    15f);
            }
        }

        private void SpawnSubBullets(NPC target, bool critical)
        {
            if (!critical)
                return;

            int bulletCount = 6;
            float damageRatio = 0.175f;
            float speed = 12f;
            int subDamage = Math.Max(1, (int)(Projectile.damage * damageRatio));

            for (int i = 0; i < bulletCount; i++)
            {
                bool fromRight = i < bulletCount / 2;
                CalamityUtils.ProjectileBarrage(
                    Projectile.GetSource_FromThis(),
                    Projectile.Center,
                    target.Center,
                    fromRight,
                    500f,
                    500f,
                    0f,
                    500f,
                    speed,
                    ModContent.ProjectileType<AMRSubBullet>(),
                    subDamage,
                    Projectile.knockBack * 0.5f,
                    Projectile.owner);
            }
        }

        private void ResolveOnyxSequence(NPC target, int damageDone)
        {
            AMRMarkerGlobalNPC marker = target.GetGlobalNPC<AMRMarkerGlobalNPC>();
            if (IsMarkerRound)
            {
                marker.SetMarker(Projectile.owner, Math.Max(Projectile.damage, damageDone));
                SpawnOnyxTargetMarker(target);
                SoundEngine.PlaySound(SoundID.Item4 with { Volume = 0.35f, Pitch = -0.45f }, target.Center);
                return;
            }

            if (!marker.TryConsumeMarker(Projectile.owner, Math.Max(Projectile.damage, damageDone), out int detonationDamage))
                return;

            if (Projectile.owner != Main.myPlayer)
                return;

            RemoveOnyxTargetMarker(target.whoAmI);

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

        private void SpawnOnyxTargetMarker(NPC target)
        {
            if (Projectile.owner != Main.myPlayer)
                return;

            RemoveOnyxTargetMarker(target.whoAmI);

            Vector2 forward = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            Vector2 markerPosition = Projectile.Center - forward * 9f;
            Vector2 targetOffset = markerPosition - target.Center;
            Projectile.NewProjectile(
                Projectile.GetSource_FromThis(),
                markerPosition,
                targetOffset,
                ModContent.ProjectileType<AMROnyxTileMarker>(),
                0,
                0f,
                Projectile.owner,
                forward.ToRotation() + MathHelper.PiOver2,
                target.whoAmI + 1f);
        }

        private void RemoveOnyxTargetMarker(int targetIndex)
        {
            int markerType = ModContent.ProjectileType<AMROnyxTileMarker>();
            foreach (Projectile markerProjectile in Main.ActiveProjectiles)
            {
                if (markerProjectile.type == markerType && markerProjectile.owner == Projectile.owner &&
                    markerProjectile.ModProjectile is AMROnyxTileMarker marker &&
                    marker.IsAttachedTo(targetIndex))
                {
                    markerProjectile.Kill();
                }
            }
        }

        private void SpawnImpact(Vector2 center, bool critical)
        {
            if (Main.dedServ)
                return;

            float strength = critical ? 1.4f : 1.1f;
            Vector2 forward = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            Vector2 normal = new(-forward.Y, forward.X);
            float rotation = forward.ToRotation();

            if (Main.LocalPlayer.active && !Main.LocalPlayer.dead)
            {
                Main.LocalPlayer.Calamity().GeneralScreenShakePower = Math.Max(
                    Main.LocalPlayer.Calamity().GeneralScreenShakePower,
                    critical ? 8.5f : 5f);
            }

            GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(
                center,
                forward * 0.45f,
                new Color(255, 215, 0),
                new Vector2(0.25f, 0.85f),
                rotation,
                0.06f,
                1.2f * strength,
                22));

            int orbCount = critical ? 24 : 16;
            for (int i = 0; i < orbCount; i++)
            {
                float spread = i / (orbCount - 1f) - 0.5f;
                float angle = MathHelper.ToRadians(75f) * spread;
                float speed = MathHelper.Lerp(5f, 17f, 1f - MathF.Abs(spread * 2f)) * Main.rand.NextFloat(0.85f, 1.15f) * strength;
                Color color = i % 2 == 0 ? new Color(255, 210, 80) : new Color(30, 24, 18);
                GeneralParticleHandler.SpawnParticle(new GlowOrbParticle(
                    center + forward * 5f,
                    forward.RotatedBy(angle) * speed,
                    false,
                    Main.rand.Next(14, 22),
                    Main.rand.NextFloat(0.35f, 0.6f) * strength,
                    color,
                    true,
                    false,
                    true));
            }
        }
    }
}
