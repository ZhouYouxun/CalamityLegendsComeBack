using System;
using CalamityLegendsComeBack.Weapons.YharimsCrystal.Passive;
using CalamityMod;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.YharimsCrystal.LeftGeneral
{
    internal sealed class YC_ThrownBlade : ModProjectile, ILocalizedModType
    {
        private const int StateFlight = 0;
        private const int StateStuck = 1;
        private const int StateDirectedThrow = 2;
        private const int StateRightThrow = 3;
        private const int RightThrowRising = 0;
        private const int RightThrowHovering = 1;
        private const int RightThrowPulling = 2;
        private const int RightThrowRiseFrames = 72;
        private const int RightThrowHoverFrames = 180;
        private const int RightThrowMaxPullFrames = 90;
        private const int RightThrowJudgementCharges = 9;
        private const float RightThrowPeakSpeed = 28f;
        private const float RightThrowArrivalDistance = 42f;
        private static float UpwardBladeAngle => -MathHelper.PiOver4;

        public new string LocalizationCategory => "Projectiles.YharimsCrystal";
        public override string Texture => "CalamityMod/Items/Weapons/Melee/Earth";

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 14;
            ProjectileID.Sets.TrailingMode[Type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = 48;
            Projectile.height = 48;
            Projectile.friendly = true;
            Projectile.penetrate = 8;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 15;
            Projectile.timeLeft = 360;
        }

        public override void OnSpawn(Terraria.DataStructures.IEntitySource source)
        {
            if (Projectile.ai[0] == StateRightThrow)
            {
                int size = (int)(110 * Projectile.scale);
                Projectile.Resize(size, size);
                Projectile.penetrate = -1;
                Projectile.timeLeft = RightThrowRiseFrames + RightThrowHoverFrames + RightThrowMaxPullFrames + 30;
                Projectile.localAI[0] = 0f;
                Projectile.localAI[1] = 0f;
                Projectile.rotation = Projectile.ai[2];
            }
        }

        public override bool? CanHitNPC(NPC target)
        {
            if (Projectile.ai[0] == StateStuck)
                return false;

            if (Projectile.ai[0] == StateDirectedThrow && Projectile.localAI[0] <= 48f)
                return false;

            if (Projectile.ai[0] == StateRightThrow)
                return false;

            return null;
        }

        public override void AI()
        {
            if (Projectile.ai[0] == StateRightThrow)
            {
                DoRightThrow();
            }
            else if (Projectile.ai[0] == StateDirectedThrow)
            {
                DoDirectedThrow();
            }
            else if (Projectile.ai[0] == StateStuck)
            {
                Projectile.velocity = Vector2.Zero;
                Projectile.rotation += 0.45f;

                int npcIndex = (int)Projectile.ai[1];
                if (npcIndex < 0 || npcIndex >= Main.maxNPCs)
                {
                    Projectile.Kill();
                    return;
                }

                NPC npc = Main.npc[npcIndex];
                if (!npc.active || npc.dontTakeDamage)
                {
                    Projectile.Kill();
                    return;
                }

                // Lock to the target
                Projectile.Center = npc.Center - Projectile.velocity * 2f;
                Projectile.gfxOffY = npc.gfxOffY;

                Projectile.localAI[0]++;
                if (Main.rand.NextBool(3) && !Main.dedServ)
                {
                    Dust d = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(24f, 24f), DustID.GoldFlame, Main.rand.NextVector2Circular(4f, 4f), 0, default, 1.2f);
                    d.noGravity = true;
                }
            }
            else if (Projectile.ai[0] == StateFlight)
            {
                Projectile.localAI[1] += 0.22f;
                NPC target = FindNearestTarget(1200f);
                if (target != null)
                {
                    float dist = Vector2.Distance(Projectile.Center, target.Center);
                    float closeFactor = Utils.GetLerpValue(600f, 80f, dist, true);
                    float turnRate = MathHelper.ToRadians(MathHelper.Lerp(4.5f, 14f, closeFactor));
                    float newAngle = Projectile.velocity.ToRotation().AngleTowards((target.Center - Projectile.Center).ToRotation(), turnRate);
                    float speed = MathHelper.Lerp(28f, 36f, closeFactor);
                    Projectile.velocity = newAngle.ToRotationVector2() * speed;
                    Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.ToRadians(135f) + Projectile.localAI[1];
                }
                else
                {
                    Projectile.velocity *= 0.98f;
                    Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.ToRadians(135f) + Projectile.localAI[1];
                    if (Projectile.velocity.Length() < 3f)
                        Projectile.Kill();
                }

                if (!Main.dedServ && Main.rand.NextBool(3))
                {
                    Dust d = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(12f, 12f), DustID.GoldFlame, Main.rand.NextVector2Circular(2f, 2f), 0, default, 1.1f);
                    d.noGravity = true;
                }
            }
        }

        private void DoDirectedThrow()
        {
            Projectile.localAI[0]++;
            Projectile.localAI[1] += 0.32f;
            if (Projectile.localAI[0] <= 34f)
            {
                float driftProgress = MathHelper.Clamp(Projectile.localAI[0] / 34f, 0f, 1f);
                Vector2 driftDirection = Projectile.velocity.SafeNormalize(Vector2.UnitX);
                Projectile.velocity = Vector2.Lerp(Projectile.velocity, driftDirection * MathHelper.Lerp(7.5f, 2.6f, driftProgress), 0.1f);
                Projectile.rotation += 0.52f;
                Projectile.Opacity = Utils.GetLerpValue(0f, 8f, Projectile.localAI[0], true);
                Lighting.AddLight(Projectile.Center, new Vector3(1f, 0.62f, 0.12f) * 0.55f);
                EmitDirectedThrowChargeFX(driftProgress, charging: false);
                return;
            }

            if (Projectile.localAI[0] <= 48f)
            {
                float holdProgress = MathHelper.Clamp((Projectile.localAI[0] - 34f) / 14f, 0f, 1f);
                Projectile.velocity *= 0.76f;
                Projectile.rotation += 0.36f + holdProgress * 0.16f;
                Projectile.Opacity = 1f;
                Lighting.AddLight(Projectile.Center, new Vector3(1f, 0.72f, 0.18f) * (0.75f + holdProgress * 0.65f));
                EmitDirectedThrowChargeFX(holdProgress, charging: true);

                if (Projectile.localAI[0] == 42f)
                    SoundEngine.PlaySound(SoundID.Item74 with { Volume = 0.62f, Pitch = -0.32f }, Projectile.Center);

                return;
            }

            float progress = MathHelper.Clamp((Projectile.localAI[0] - 48f) / 18f, 0f, 1f);
            float easedProgress = MathHelper.SmoothStep(0f, 1f, progress);
            NPC target = GetThrowTarget();
            Vector2 targetDirection = target == null
                ? Projectile.velocity.SafeNormalize(Vector2.UnitX)
                : (target.Center - Projectile.Center).SafeNormalize(Projectile.velocity.SafeNormalize(Vector2.UnitX));
            float dist = target == null ? 9999f : Vector2.Distance(Projectile.Center, target.Center);
            float closeFactor = Utils.GetLerpValue(600f, 80f, dist, true);
            float turnRate = MathHelper.ToRadians(MathHelper.Lerp(12f, 28f, easedProgress) + closeFactor * 10f);
            float newAngle = Projectile.velocity.ToRotation().AngleTowards(targetDirection.ToRotation(), turnRate);
            float speed = MathHelper.Lerp(42f, 58f, easedProgress);
            Projectile.velocity = newAngle.ToRotationVector2() * speed;
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.ToRadians(225f) + Projectile.localAI[1];
            Projectile.Opacity = 1f;
            Lighting.AddLight(Projectile.Center, new Vector3(1f, 0.72f, 0.18f) * (0.65f + easedProgress * 0.65f));

            if (!Main.dedServ)
            {
                // 三板斧：大量上升粒子轨迹
                int particleCount = Main.rand.Next(3, 7);
                for (int i = 0; i < particleCount; i++)
                {
                    Dust dust = Dust.NewDustPerfect(
                        Projectile.Center + Main.rand.NextVector2Circular(22f, 22f),
                        DustID.GoldFlame,
                        -Projectile.velocity.SafeNormalize(Vector2.UnitX).RotatedByRandom(0.45f) * Main.rand.NextFloat(2f, 9f + easedProgress * 8f),
                        0,
                        Main.rand.NextBool(3) ? Color.White : new Color(255, 214, 88),
                        Main.rand.NextFloat(1.0f, 1.6f));
                    dust.noGravity = true;
                }

                // 橙金光晕跟随
                if ((int)Projectile.localAI[0] % 2 == 0)
                {
                    GeneralParticleHandler.SpawnParticle(new CustomSpark(
                        Projectile.Center + Main.rand.NextVector2Circular(18f, 18f),
                        -Projectile.velocity.SafeNormalize(Vector2.UnitX).RotatedByRandom(0.32f) * Main.rand.NextFloat(2f, 6f),
                        "CalamityMod/Particles/Sparkle",
                        false,
                        Main.rand.Next(10, 17),
                        Main.rand.NextFloat(0.38f, 0.72f),
                        Main.rand.NextBool(3) ? Color.White : new Color(255, 190, 54),
                        new Vector2(0.22f, 0.9f),
                        true,
                        true,
                        shrinkSpeed: 0.16f));
                }
            }

        }

        private void DoRightThrow()
        {
            Player owner = Main.player[Projectile.owner];
            if (!owner.active || owner.dead)
            {
                Projectile.Kill();
                return;
            }

            Lighting.AddLight(Projectile.Center, new Vector3(1f, 0.85f, 0.35f) * 0.65f);
            PlayTravelWhoosh();

            if (Projectile.ai[1] == RightThrowRising)
            {
                Projectile.localAI[0]++;
                float progress = MathHelper.Clamp(Projectile.localAI[0] / RightThrowRiseFrames, 0f, 1f);
                float easedProgress = SmootherStep(progress);
                float speed = RightThrowPeakSpeed * SmoothBell(progress);
                Projectile.velocity = -Vector2.UnitY * speed;
                Projectile.rotation = Projectile.rotation.AngleTowards(UpwardBladeAngle, MathHelper.ToRadians(MathHelper.Lerp(1.8f, 7.5f, easedProgress)));

                EmitRightThrowFX(easedProgress);

                if (progress >= 1f)
                {
                    Projectile.velocity = Vector2.Zero;
                    Projectile.ai[1] = RightThrowHovering;
                    Projectile.localAI[0] = 0f;
                    Projectile.netUpdate = true;
                    SoundEngine.PlaySound(SoundID.Item74 with { Volume = 0.7f, Pitch = -0.05f }, Projectile.Center);
                }
                return;
            }

            if (Projectile.ai[1] == RightThrowHovering)
            {
                Projectile.localAI[0]++;
                Projectile.velocity = Vector2.Zero;
                float hoverWave = (float)Math.Sin(Projectile.localAI[0] * 0.055f) * 0.055f;
                Projectile.rotation = Projectile.rotation.AngleTowards(UpwardBladeAngle + hoverWave, MathHelper.ToRadians(1.15f));
                EmitRightHoverFX();

                if (Projectile.owner == Main.myPlayer && IsOwnerPressingLeft(owner))
                {
                    Projectile.ai[1] = RightThrowPulling;
                    Projectile.localAI[0] = 0f;
                    Projectile.netUpdate = true;
                    SoundEngine.PlaySound(SoundID.Item84 with { Volume = 0.78f, Pitch = -0.28f }, Projectile.Center);
                    return;
                }

                if (Projectile.localAI[0] >= RightThrowHoverFrames)
                {
                    DissipateRightThrow();
                    Projectile.Kill();
                }
                return;
            }

            Projectile.localAI[0]++;
            Projectile.velocity = Vector2.Zero;
            Projectile.rotation = Projectile.rotation.AngleTowards(UpwardBladeAngle, MathHelper.ToRadians(1.4f));
            PullOwnerToBlade(owner);
            EmitRightPullFX(owner);

            float distanceToOwner = Vector2.Distance(owner.Center, Projectile.Center);
            if (distanceToOwner <= RightThrowArrivalDistance || Projectile.localAI[0] >= RightThrowMaxPullFrames)
            {
                FinishRightThrowPull(owner);
                Projectile.Kill();
            }
        }

        private void PlayTravelWhoosh()
        {
            if ((int)Projectile.localAI[0] % 14 == 0)
                SoundEngine.PlaySound(new SoundStyle("CalamityMod/Sounds/Item/SwooshMid") { Volume = 0.55f, Pitch = -0.25f, MaxInstances = 3 }, Projectile.Center);
        }

        private static bool IsOwnerPressingLeft(Player owner)
        {
            return (Main.mouseLeft || owner.controlUseItem) &&
                Main.myPlayer == owner.whoAmI &&
                !Main.mapFullscreen &&
                !Main.blockMouse &&
                !owner.mouseInterface;
        }

        private static float SmootherStep(float progress)
        {
            progress = MathHelper.Clamp(progress, 0f, 1f);
            return progress * progress * progress * (progress * (progress * 6f - 15f) + 10f);
        }

        private static float SmoothBell(float progress)
        {
            progress = MathHelper.Clamp(progress, 0f, 1f);
            float accelerate = SmootherStep(MathHelper.Clamp(progress / 0.42f, 0f, 1f));
            float decelerate = 1f - SmootherStep(MathHelper.Clamp((progress - 0.58f) / 0.42f, 0f, 1f));
            return accelerate * decelerate;
        }

        private void PullOwnerToBlade(Player owner)
        {
            Vector2 toBlade = Projectile.Center - owner.Center;
            float distance = toBlade.Length();
            if (distance <= 0.001f)
                return;

            float distanceFactor = 1f - MathHelper.Clamp((distance - RightThrowArrivalDistance) / 680f, 0f, 1f);
            float pullSpeed = MathHelper.Lerp(14f, 46f, SmootherStep(distanceFactor));
            Vector2 desiredVelocity = toBlade / distance * pullSpeed;
            owner.velocity = Vector2.Lerp(owner.velocity, desiredVelocity, 0.16f);
            owner.fallStart = (int)(owner.position.Y / 16f);
            owner.immune = true;
            owner.immuneNoBlink = true;
            owner.SetImmuneTimeForAllTypes(8);
        }

        private void FinishRightThrowPull(Player owner)
        {
            if (owner.whoAmI == Main.myPlayer)
                owner.GetModPlayer<YharimsCrystalStatePlayer>().GrantAuricJudgementChain(RightThrowJudgementCharges);

            owner.velocity = Vector2.Lerp(owner.velocity, Vector2.Zero, 0.18f);
            owner.immune = true;
            owner.immuneNoBlink = true;
            owner.SetImmuneTimeForAllTypes(18);
            owner.Calamity().GeneralScreenShakePower = Math.Max(owner.Calamity().GeneralScreenShakePower, 6f);

            SoundEngine.PlaySound(SoundID.Item74 with { Volume = 0.88f, Pitch = -0.24f }, Projectile.Center);
            if (!Main.dedServ)
            {
                GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(Projectile.Center, Vector2.Zero, new Color(255, 214, 88), Vector2.One, Projectile.rotation, 0.12f, 2.1f, 20));
                for (int i = 0; i < 32; i++)
                {
                    Vector2 velocity = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(4f, 16f);
                    Dust dust = Dust.NewDustPerfect(Projectile.Center, DustID.GoldFlame, velocity, 0, Main.rand.NextBool(3) ? Color.White : new Color(255, 210, 70), Main.rand.NextFloat(1f, 1.7f));
                    dust.noGravity = true;
                }
            }
        }

        private void DissipateRightThrow()
        {
            SoundEngine.PlaySound(SoundID.Item10 with { Volume = 0.5f, Pitch = -0.2f }, Projectile.Center);
            if (Main.dedServ)
                return;

            for (int i = 0; i < 18; i++)
            {
                Vector2 velocity = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(2f, 9f);
                Dust dust = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(18f, 18f), DustID.GoldFlame, velocity, 0, Main.rand.NextBool(3) ? Color.White : new Color(255, 214, 88), Main.rand.NextFloat(0.8f, 1.35f));
                dust.noGravity = true;
            }
        }

        private void EmitRightHoverFX()
        {
            if (Main.dedServ)
                return;

            if ((int)Projectile.localAI[0] % 9 == 0)
                GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(Projectile.Center, Vector2.Zero, new Color(255, 214, 88), Vector2.One, Projectile.rotation, 0.035f, 1.15f, 16));

            if (Main.rand.NextBool(3))
            {
                Vector2 velocity = -Vector2.UnitY.RotatedByRandom(0.5f) * Main.rand.NextFloat(1.2f, 3.8f);
                Dust dust = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(24f, 18f), DustID.GoldFlame, velocity, 0, Main.rand.NextBool(4) ? Color.White : new Color(255, 214, 88), Main.rand.NextFloat(0.9f, 1.45f));
                dust.noGravity = true;
            }
        }

        private void EmitRightPullFX(Player owner)
        {
            if (Main.dedServ)
                return;

            Vector2 line = Projectile.Center - owner.Center;
            if (line.LengthSquared() <= 0.001f || !Main.rand.NextBool(2))
                return;

            Vector2 position = Vector2.Lerp(owner.Center, Projectile.Center, Main.rand.NextFloat(0.15f, 0.85f));
            Vector2 velocity = line.SafeNormalize(Vector2.UnitY) * Main.rand.NextFloat(3f, 8f);
            GeneralParticleHandler.SpawnParticle(new CustomSpark(
                position + Main.rand.NextVector2Circular(10f, 10f),
                velocity,
                "CalamityMod/Particles/Sparkle",
                false,
                Main.rand.Next(10, 16),
                Main.rand.NextFloat(0.38f, 0.68f),
                Main.rand.NextBool(3) ? Color.White : new Color(255, 210, 70),
                new Vector2(0.24f, 0.95f),
                true,
                true,
                shrinkSpeed: 0.16f));
        }

        private void EmitRightThrowFX(float intensity)
        {
            if (Main.dedServ)
                return;

            Vector2 travelDirection = GetTravelDirectionForDraw();
            if (Main.rand.NextBool(2))
            {
                Vector2 vel = -travelDirection.RotatedByRandom(0.4f) * Main.rand.NextFloat(3f, 7f + intensity * 6f);
                Dust d = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(14f, 14f), DustID.GoldFlame, vel, 0, Main.rand.NextBool(3) ? Color.White : new Color(255, 214, 88), Main.rand.NextFloat(1.1f, 1.7f));
                d.noGravity = true;
            }

            if ((int)Projectile.localAI[0] % 3 == 0)
            {
                GeneralParticleHandler.SpawnParticle(new CustomSpark(
                    Projectile.Center + Main.rand.NextVector2Circular(10f, 10f),
                    -travelDirection.RotatedByRandom(0.3f) * Main.rand.NextFloat(2f, 6f),
                    "CalamityMod/Particles/Sparkle",
                    false,
                    Main.rand.Next(12, 18),
                    Main.rand.NextFloat(0.42f, 0.78f),
                    Main.rand.NextBool(3) ? Color.White : new Color(255, 190, 54),
                    new Vector2(0.24f, 0.95f),
                    true,
                    true,
                    shrinkSpeed: 0.16f));
            }
        }

        private Vector2 GetTravelDirectionForDraw()
        {
            if (Projectile.velocity.LengthSquared() > 0.001f)
                return Projectile.velocity.SafeNormalize(-Vector2.UnitY);

            return (Projectile.rotation - MathHelper.PiOver4).ToRotationVector2().SafeNormalize(-Vector2.UnitY);
        }

        private void EmitDirectedThrowChargeFX(float progress, bool charging)
        {
            if (Main.dedServ)
                return;

            int interval = charging ? 2 : 4;
            if ((int)Projectile.localAI[0] % interval != 0)
                return;

            Color color = Main.rand.NextBool(3) ? Color.White : Color.Lerp(new Color(255, 100, 28), new Color(255, 220, 88), progress);
            Vector2 radial = Main.rand.NextVector2CircularEdge(1f, 1f);
            Vector2 position = Projectile.Center + radial * Main.rand.NextFloat(charging ? 42f : 24f, charging ? 96f : 62f);
            Vector2 velocity = (Projectile.Center - position).SafeNormalize(Vector2.Zero) * Main.rand.NextFloat(charging ? 4f : 1.6f, charging ? 10f : 4.5f);

            GeneralParticleHandler.SpawnParticle(new CustomSpark(
                position,
                velocity,
                "CalamityMod/Particles/Sparkle",
                false,
                Main.rand.Next(charging ? 12 : 10, charging ? 20 : 16),
                Main.rand.NextFloat(0.36f, charging ? 0.92f : 0.62f),
                color,
                new Vector2(0.22f, charging ? 1.2f : 0.8f),
                true,
                true,
                shrinkSpeed: 0.16f));
        }

        private NPC GetThrowTarget()
        {
            int targetIndex = (int)Projectile.ai[1];
            if (targetIndex >= 0 && targetIndex < Main.maxNPCs)
            {
                NPC target = Main.npc[targetIndex];
                if (target.active && target.CanBeChasedBy(Projectile))
                    return target;
            }

            return Projectile.localAI[0] >= 8f ? FindNearestTarget(1200f) : null;
        }

        private void SpawnSkyJudgement()
        {
            if (Projectile.owner != Main.myPlayer)
                return;

            Vector2 focus = GetJudgementFocus();
            int wave = Projectile.NewProjectile(
                Projectile.GetSource_FromThis(),
                focus - Vector2.UnitY * 720f,
                Vector2.UnitY * 20f,
                ModContent.ProjectileType<YC_AuricJudgementWave>(),
                Math.Max(1, (int)(Projectile.damage * 0.92f)),
                Projectile.knockBack * 0.8f,
                Projectile.owner,
                4f);

            if (Main.projectile.IndexInRange(wave))
            {
                YharimsCrystalHellBladeGlobalProjectile.Mark(Main.projectile[wave], YCWeaponForm.Blade);
                Main.projectile[wave].CritChance = Projectile.CritChance;
            }

            // 三板斧：天降时强烈屏幕震动
            Player owner = Main.player[Projectile.owner];
            owner.Calamity().GeneralScreenShakePower = Math.Max(owner.Calamity().GeneralScreenShakePower, 6f);

            // 三板斧：召唤时大爆炸粒子效果
            if (!Main.dedServ)
            {
                for (int i = 0; i < 40; i++)
                {
                    Vector2 vel = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(5f, 22f);
                    Dust d = Dust.NewDustPerfect(focus + Main.rand.NextVector2Circular(28f, 28f), DustID.GoldFlame, vel, 0,
                        Main.rand.NextBool(3) ? Color.White : new Color(255, 210, 70), Main.rand.NextFloat(1.0f, 1.8f));
                    d.noGravity = true;
                }
            }

            SoundEngine.PlaySound(SoundID.Item74 with { Volume = 0.9f, Pitch = -0.22f }, focus);
            SoundEngine.PlaySound(SoundID.Item100 with { Volume = 0.72f, Pitch = 0.08f }, focus);
        }

        private Vector2 GetJudgementFocus()
        {
            int targetIndex = (int)Projectile.ai[1];
            if (targetIndex >= 0 && targetIndex < Main.maxNPCs)
            {
                NPC target = Main.npc[targetIndex];
                if (target.active && target.CanBeChasedBy(Projectile))
                    return target.Center;
            }

            Player owner = Main.player[Projectile.owner];
            return owner.Center + Vector2.UnitX * owner.direction * 420f;
        }

        private NPC FindNearestTarget(float maxRange)
        {
            NPC nearest = null;
            float maxDistSq = maxRange * maxRange;

            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];
                if (!npc.CanBeChasedBy(Projectile))
                    continue;

                float distSq = Vector2.DistanceSquared(Projectile.Center, npc.Center);
                if (distSq < maxDistSq)
                {
                    maxDistSq = distSq;
                    nearest = npc;
                }
            }
            return nearest;
        }

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            target.AddBuff(new BalanceYharimsCrystal().GetFireDebuffType(), 240);
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            SoundEngine.PlaySound(SoundID.Item22 with { Volume = 0.85f, Pitch = -0.1f }, target.Center);

            bool directedImpact = Projectile.ai[0] == StateDirectedThrow;

            if (Projectile.ai[0] != StateStuck)
            {
                Projectile.ai[0] = StateStuck;
                Projectile.ai[1] = target.whoAmI;
                // Save a small velocity vector representing the entry offset relative to the NPC
                Projectile.velocity = (target.Center - Projectile.Center) * 0.5f;
                Projectile.netUpdate = true;

                Player owner = Main.player[Projectile.owner];
                owner.Calamity().GeneralScreenShakePower = Math.Max(owner.Calamity().GeneralScreenShakePower, 3.5f);
                if (!Main.dedServ)
                {
                    GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(target.Center, Vector2.Zero, new Color(255, 210, 80), Vector2.One, Projectile.rotation, 0.1f, 2.2f, 20));
                    for (int k = 0; k < 18; k++)
                    {
                        Vector2 vel = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(4f, 16f);
                        Dust d = Dust.NewDustPerfect(target.Center + Main.rand.NextVector2Circular(20f, 20f), DustID.GoldFlame, vel, 0, Main.rand.NextBool(3) ? Color.White : new Color(255, 210, 80), Main.rand.NextFloat(1.0f, 1.7f));
                        d.noGravity = true;
                    }
                }
            }

            if (directedImpact)
                SpawnSkyJudgement();

            // Spawn homing tracking missiles on every hit
            if (Projectile.owner == Main.myPlayer)
            {
                Player owner = Main.player[Projectile.owner];
                int count = (Projectile.ai[0] == 1f) ? 2 : 3;
                for (int i = 0; i < count; i++)
                {
                    if (!YC_EssenceFlame.CanSpawnMoreFor(owner, i))
                        break;

                    Vector2 flameVel = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(8f, 14f);
                    int flame = Projectile.NewProjectile(
                        Projectile.GetSource_OnHit(target),
                        target.Center,
                        flameVel,
                        ModContent.ProjectileType<YC_EssenceFlame>(),
                        (int)(Projectile.damage * 0.6f),
                        Projectile.knockBack * 0.2f,
                        Projectile.owner,
                        target.whoAmI,
                        Main.rand.NextFloat(0f, 100f));
                    if (Main.projectile.IndexInRange(flame))
                    {
                        YharimsCrystalHellBladeGlobalProjectile.Mark(Main.projectile[flame], YCWeaponForm.Blade);
                        Main.projectile[flame].CritChance = Projectile.CritChance;
                    }
                }
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = ModContent.Request<Texture2D>(Texture).Value;
            Texture2D glow = ModContent.Request<Texture2D>("CalamityMod/Items/Weapons/Melee/EarthGlow").Value;
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Vector2 origin = texture.Size() * 0.5f;
            Vector2 drawPos = Projectile.Center - Main.screenPosition;
            SpriteEffects effects = SpriteEffects.None;

            if (Projectile.ai[0] == StateDirectedThrow || Projectile.ai[0] == StateRightThrow)
            {
                Vector2 travelDirection = GetTravelDirectionForDraw();
                Vector2 tailDirection = -travelDirection;
                float pulse = 0.9f + 0.1f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 20f);

                Main.spriteBatch.SetBlendState(BlendState.Additive);
                for (int i = 0; i < 5; i++)
                {
                    float progress = i / 5f;
                    Vector2 trailPosition = drawPos + tailDirection * (16f + i * 18f);
                    Main.EntitySpriteDraw(
                        bloom,
                        trailPosition,
                        null,
                        Color.Lerp(new Color(255, 108, 28), Color.Gold, progress) * (0.38f - progress * 0.22f),
                        travelDirection.ToRotation(),
                        bloom.Size() * 0.5f,
                        (0.26f - progress * 0.1f) * pulse,
                        SpriteEffects.None);
                }
                Main.EntitySpriteDraw(glow, drawPos, null, new Color(255, 223, 132, 0) * 0.72f, Projectile.rotation, origin, Projectile.scale * 1.16f, effects, 0);
                Main.spriteBatch.SetBlendState(BlendState.AlphaBlend);
            }

            if (Projectile.ai[0] != StateStuck)
            {
                for (int i = Projectile.oldPos.Length - 1; i >= 0; i--)
                {
                    if (Projectile.oldPos[i] == Vector2.Zero)
                        continue;
                    float progress = 1f - i / (float)Projectile.oldPos.Length;
                    Vector2 oldDraw = Projectile.oldPos[i] + Projectile.Size * 0.5f - Main.screenPosition;
                    Main.EntitySpriteDraw(texture, oldDraw, null, Color.Orange * 0.18f * progress, Projectile.oldRot[i], origin, Projectile.scale * progress, effects, 0);
                }
            }
            else
            {
                float pulse = 0.85f + 0.15f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 16f);
                Main.spriteBatch.SetBlendState(BlendState.Additive);
                Main.EntitySpriteDraw(
                    bloom,
                    drawPos,
                    null,
                    Color.Orange * 0.45f * pulse,
                    0f,
                    bloom.Size() * 0.5f,
                    Projectile.scale * 1.35f * pulse,
                    SpriteEffects.None,
                    0);
                Main.spriteBatch.SetBlendState(BlendState.AlphaBlend);
            }

            Main.EntitySpriteDraw(texture, drawPos, null, lightColor, Projectile.rotation, origin, Projectile.scale, effects, 0);
            Main.EntitySpriteDraw(glow, drawPos, null, new Color(255, 214, 88) * 0.8f, Projectile.rotation, origin, Projectile.scale, effects, 0);
            return false;
        }
    }
}
