using System;
using CalamityLegendsComeBack.Weapons.YharimsCrystal.Passive;
using CalamityMod;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Utilities;
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

        // StateRightThrow now follows SHPCKFast's two-beat "throw, hang, then rocket in" feel
        // instead of NeptunesBounty's spinning gravity arc: it is flung toward the aim point,
        // decelerates to a near-stop in the air while turning its tip to lock onto the nearest
        // enemy (RightThrowAiming), then a power surge grants extraUpdates=3 + doubled damage and
        // launches it in a hard, gently-steered dash straight at that enemy (RightThrowDashing).
        // The blade never free-spins now — it always points where it is going / at its target.
        // On real contact it impales into the target for repeated ticks (RightThrowImpaled).
        private const int RightThrowAiming = 0;
        private const int RightThrowDashing = 1;
        private const int RightThrowImpaled = 2;
        private const int ImpaleTickCount = 10;
        private const int ImpaleTickInterval = 12;
        private const float RightThrowLaunchSpeed = 28f;      // initial toss speed toward the aim point
        private const float RightThrowDecelRate = 0.87f;      // per-frame slowdown of phase 1 (SHPCKFast uses 0.88)
        private const float RightThrowHoverSpeed = 2.4f;      // speed at/under which phase 1 counts as "stopped in air"
        private const int RightThrowAimHoldFrames = 10;       // brief mid-air charge/aim-lock before the dash
        private const int RightThrowMaxAimFrames = 70;        // fallback so it never hangs if it can't stall/aim
        private const float RightThrowDashSpeed = 26f;        // fixed dash speed (x extraUpdates = the "big acceleration")
        private const float RightThrowDashSteer = 4.5f;       // deg/subframe course-correction so the lunge connects
        private const int RightThrowExtraUpdates = 3;
        private const int RightThrowJudgementCharges = 9;
        private static float UpwardBladeAngle => -MathHelper.PiOver4;

        public new string LocalizationCategory => "Projectiles.YharimsCrystal";
        public override string Texture => "CalamityMod/Items/Weapons/Melee/Earth";

        // 钉入时刀刃相对目标中心的偏移（视觉用，不参与netcode）
        private Vector2 impaleOffset;
        private int startDamage;
        private SlotId spinSoundSlot;
        private int spinSoundRefreshDelay;
        private bool hasExploded;

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
                Projectile.timeLeft = 300; // matches NeptunesBounty's own Lifetime literally
                Projectile.localAI[0] = 0f;
                Projectile.localAI[1] = 0f;
                Projectile.rotation = Projectile.ai[2];
                Projectile.ai[2] = -1f; // repurposed after spawn: locked target NPC index
                startDamage = Projectile.damage;
                spinSoundSlot = SoundEngine.PlaySound(new SoundStyle("CalamityMod/Sounds/Item/SpinningWoosh") { Volume = 0.65f, Pitch = -0.1f }, Projectile.Center);
            }
        }

        public override bool? CanHitNPC(NPC target)
        {
            if (Projectile.ai[0] == StateStuck)
                return false;

            if (Projectile.ai[0] == StateDirectedThrow && Projectile.localAI[0] <= 48f)
                return false;

            // Neptune's blade can damage anything in its path the instant it's thrown —
            // no positioning-only grace period. 钉入后只对被钉住的目标持续跳伤。
            if (Projectile.ai[0] == StateRightThrow && Projectile.ai[1] == RightThrowImpaled && target.whoAmI != (int)Projectile.ai[2])
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

            // Dimmer while it hangs and aims, brighter once it surges into the dash.
            Vector3 lightColor = Projectile.ai[1] == RightThrowDashing
                ? new Vector3(1f, 0.85f, 0.35f) * 0.9f
                : new Vector3(1f, 0.85f, 0.35f) * 0.5f;
            Lighting.AddLight(Projectile.Center, lightColor);

            if (Projectile.ai[1] == RightThrowImpaled)
            {
                RunRightThrowImpaled(owner);
                return;
            }

            MaintainRightThrowSpinSound(Projectile.ai[1] == RightThrowDashing ? 0.2f : -0.1f);
            PlayTravelWhoosh();

            if (Projectile.ai[1] == RightThrowAiming)
                RunRightThrowAiming();
            else
                RunRightThrowDashing();
        }

        // Phase 1 (SHPCKFast's opening beat): flung toward the aim point at launch speed, then
        // bled off with a flat per-frame deceleration until it hangs almost still in the air.
        // Instead of free-spinning it continuously turns its tip to lock onto the nearest enemy,
        // and once it has stalled + aimed (or the fallback timer trips) it fires the power surge.
        private void RunRightThrowAiming()
        {
            Projectile.localAI[0]++; // Time
            // localAI[1] = frames spent stalled/charging in the air before the dash

            if (Projectile.localAI[0] == 1f)
            {
                Player launchOwner = Main.player[Projectile.owner];
                Vector2 aimDirection = (launchOwner.Calamity().mouseWorld - Projectile.Center).SafeNormalize(Vector2.UnitX * launchOwner.direction);
                Projectile.velocity = aimDirection * RightThrowLaunchSpeed;
                Projectile.rotation = aimDirection.ToRotation() + MathHelper.PiOver4;
            }

            // 始终把刀尖对准最近的敌人（不再自旋）
            NPC target = FindNearestTarget(1600f);
            float aimAngle = target != null
                ? (target.Center - Projectile.Center).ToRotation() + MathHelper.PiOver4
                : Projectile.velocity.ToRotation() + MathHelper.PiOver4;
            Projectile.rotation = Projectile.rotation.AngleLerp(aimAngle, 0.25f);
            if (Projectile.velocity.X != 0f)
                Projectile.direction = Projectile.velocity.X > 0f ? 1 : -1;

            // 第一阶段减速：逐渐停在空中
            bool stalled = Projectile.velocity.Length() <= RightThrowHoverSpeed;
            if (!stalled)
            {
                Projectile.velocity *= RightThrowDecelRate;
                EmitRightThrowFlightTrail(postTrigger: false);
            }
            else
            {
                // 悬停蓄力，把能量向刀身聚拢
                Projectile.velocity *= 0.55f;
                Projectile.localAI[1]++;
                EmitRightThrowAimCharge(target);
            }

            bool aimed = target == null || Math.Abs(MathHelper.WrapAngle(aimAngle - Projectile.rotation)) < 0.16f;
            if ((stalled && aimed && Projectile.localAI[1] >= RightThrowAimHoldFrames) || Projectile.localAI[0] >= RightThrowMaxAimFrames)
            {
                Projectile.ai[1] = RightThrowDashing;
                Projectile.localAI[0] = 0f;
                Projectile.localAI[1] = 0f;
                Projectile.netUpdate = true;
                TriggerRightThrowPowerSurge();
            }
        }

        // The mid-air power surge (SHPCKFast's "then it rockets in"): extraUpdates=3 + doubled
        // damage make the dash feel explosively fast, the aim/whoosh sound restarts pitched up,
        // and velocity is redirected to lead the nearest enemy at the fixed dash speed. The tip
        // is snapped to face the dash so it launches pointing dead at the target; phase 2 keeps
        // gently steering after this so the committed lunge actually connects.
        private void TriggerRightThrowPowerSurge()
        {
            Player owner = Main.player[Projectile.owner];
            Projectile.extraUpdates = RightThrowExtraUpdates;
            Projectile.damage = startDamage * 2;
            Projectile.numHits = 0;

            SoundEngine.PlaySound(new SoundStyle("CalamityMod/Sounds/Item/VividClarityBeamAppear") { Volume = 0.65f, PitchVariance = 0.3f }, Projectile.Center);
            SoundEngine.PlaySound(SoundID.ShimmerWeak1 with { Pitch = 0.15f }, Projectile.Center);

            if (SoundEngine.TryGetActiveSound(spinSoundSlot, out ActiveSound previousSpin))
                previousSpin.Stop();
            spinSoundSlot = SoundEngine.PlaySound(new SoundStyle("CalamityMod/Sounds/Item/SpinningWoosh") { Volume = 0.65f, Pitch = 0.2f }, Projectile.Center);

            NPC target = FindNearestTarget(2000f);
            Vector2 desiredDirection = target != null
                ? (target.Center + target.velocity * 5f - Projectile.Center).SafeNormalize(Vector2.UnitX * Projectile.direction)
                : (owner.Calamity().mouseWorld - Projectile.Center).SafeNormalize(Vector2.UnitX * Projectile.direction);
            Projectile.velocity = desiredDirection * RightThrowDashSpeed;
            Projectile.rotation = desiredDirection.ToRotation() + MathHelper.PiOver4;
            Projectile.direction = desiredDirection.X >= 0f ? 1 : -1;

            if (Main.dedServ)
                return;

            GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(Projectile.Center, Vector2.Zero, new Color(255, 214, 88), new Vector2(2f, 2f), Main.rand.NextFloat(MathHelper.TwoPi), 0.01f, 0.9f, 22));
            GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(Projectile.Center, Vector2.Zero, new Color(255, 111, 34), new Vector2(2f, 2f), Main.rand.NextFloat(MathHelper.TwoPi), 0.01f, 0.83f, 15));
        }

        // Phase 2: the committed dash. It flies at the fixed dash speed (x extraUpdates = very
        // fast) with a gentle course-correction toward the locked enemy so the lunge lands, and
        // the blade's tip stays pointed exactly where it is going — never free-spinning.
        private void RunRightThrowDashing()
        {
            Projectile.localAI[0]++; // Time, reset at the surge

            NPC target = FindNearestTarget(2200f);
            if (target != null)
            {
                float steered = Projectile.velocity.ToRotation().AngleTowards(
                    (target.Center - Projectile.Center).ToRotation(),
                    MathHelper.ToRadians(RightThrowDashSteer));
                Projectile.velocity = steered.ToRotationVector2() * Projectile.velocity.Length();
            }

            if (Projectile.velocity.X != 0f)
                Projectile.direction = Projectile.velocity.X > 0f ? 1 : -1;
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver4;

            EmitRightThrowFlightTrail(postTrigger: true);
        }

        // 钉入阶段：锁死在被命中的敌人身上，靠本地无敌帧持续跳伤（共10跳），期间完全不追踪。
        private void RunRightThrowImpaled(Player owner)
        {
            int npcIndex = (int)Projectile.ai[2];
            if (npcIndex < 0 || npcIndex >= Main.maxNPCs)
            {
                SelfDestructIntoFireballs(owner);
                Projectile.Kill();
                return;
            }

            NPC npc = Main.npc[npcIndex];
            if (!npc.active || npc.dontTakeDamage)
            {
                SelfDestructIntoFireballs(owner);
                Projectile.Kill();
                return;
            }

            Projectile.localAI[0]++;
            Projectile.velocity = Vector2.Zero;
            Projectile.Center = npc.Center - impaleOffset;
            Projectile.gfxOffY = npc.gfxOffY;
            Projectile.timeLeft = Math.Max(Projectile.timeLeft, 10);

            if (!Main.dedServ)
            {
                if (Main.rand.NextBool(2))
                {
                    Dust d = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(20f, 20f), DustID.GoldFlame, Main.rand.NextVector2Circular(3.5f, 3.5f), 0, default, 1.2f);
                    d.noGravity = true;
                }

                // Energy absorbing effect into the impaled blade
                Vector2 absorbSpawn = Projectile.Center + Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(30f, 65f);
                Vector2 absorbVel = (Projectile.Center - absorbSpawn).SafeNormalize(Vector2.Zero) * Main.rand.NextFloat(2f, 5.5f);
                GeneralParticleHandler.SpawnParticle(new GlowOrbParticle(
                    absorbSpawn,
                    absorbVel,
                    false,
                    Main.rand.Next(8, 14),
                    Main.rand.NextFloat(0.22f, 0.42f),
                    Main.rand.NextBool(3) ? Color.White : new Color(255, 214, 88)));

                if ((int)Projectile.localAI[0] % ImpaleTickInterval == 0)
                {
                    GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(npc.Center, Vector2.Zero, new Color(255, 214, 88), Vector2.One, Main.rand.NextFloat(MathHelper.TwoPi), 0.05f, 0.9f, 14));
                    GeneralParticleHandler.SpawnParticle(new GenericSparkle(
                        npc.Center + Main.rand.NextVector2Circular(10f, 10f),
                        Vector2.Zero,
                        Color.White,
                        new Color(255, 214, 88),
                        Main.rand.NextFloat(0.45f, 0.75f),
                        12,
                        0f,
                        2.4f));
                }
            }

            // 10跳打满或滞留超时便引爆成燃烧碎片
            if (Projectile.localAI[1] >= ImpaleTickCount || Projectile.localAI[0] >= ImpaleTickInterval * (ImpaleTickCount + 2))
            {
                SelfDestructIntoFireballs(owner);
                Projectile.Kill();
            }
        }

        private void TriggerSlamImpact(Player owner, NPC target)
        {
            owner.Calamity().GeneralScreenShakePower = Math.Max(owner.Calamity().GeneralScreenShakePower, 9f);
            SoundEngine.PlaySound(SoundID.Item14 with { Volume = 0.9f, Pitch = -0.15f }, Projectile.Center);
            SoundEngine.PlaySound(SoundID.Item70 with { Volume = 0.8f, Pitch = -0.3f }, Projectile.Center);

            if (owner.whoAmI == Main.myPlayer)
                owner.GetModPlayer<YharimsCrystalStatePlayer>().GrantAuricJudgementChain(RightThrowJudgementCharges);

            if (Main.dedServ)
                return;

            Vector2 impactPoint = target?.Center ?? Projectile.Center;
            GeneralParticleHandler.SpawnParticle(new StrongBloom(impactPoint, Vector2.Zero, new Color(255, 224, 90), 1.05f, 16));
            GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(impactPoint, Vector2.Zero, new Color(255, 214, 88), Vector2.One, Projectile.rotation, 0.14f, 2.6f, 22));
            GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(impactPoint, Vector2.Zero, new Color(255, 110, 36), Vector2.One, Projectile.rotation + MathHelper.PiOver2, 0.08f, 1.9f, 16));

            for (int i = 0; i < 34; i++)
            {
                Vector2 velocity = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(4f, 17f);
                Dust dust = Dust.NewDustPerfect(impactPoint, DustID.GoldFlame, velocity, 0, Main.rand.NextBool(3) ? Color.White : new Color(255, 210, 70), Main.rand.NextFloat(1f, 1.8f));
                dust.noGravity = true;
            }

            for (int i = 0; i < 14; i++)
            {
                Vector2 sparkVel = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(5f, 16f);
                GeneralParticleHandler.SpawnParticle(new CustomSpark(
                    impactPoint,
                    sparkVel,
                    "CalamityMod/Particles/Sparkle",
                    false,
                    Main.rand.Next(14, 24),
                    Main.rand.NextFloat(0.6f, 1.1f),
                    Main.rand.NextBool(3) ? Color.White : new Color(255, 214, 88),
                    new Vector2(0.3f, 1.2f),
                    true, true,
                    shrinkSpeed: 0.12f));
            }

            for (int i = 0; i < 8; i++)
            {
                float angle = MathHelper.TwoPi * i / 8f;
                GeneralParticleHandler.SpawnParticle(new GenericSparkle(
                    impactPoint + angle.ToRotationVector2() * 16f,
                    angle.ToRotationVector2() * Main.rand.NextFloat(2f, 6f),
                    Color.White,
                    new Color(255, 214, 88),
                    Main.rand.NextFloat(0.5f, 0.85f),
                    14,
                    0f,
                    2.5f));
            }
        }

        private void SelfDestructIntoFireballs(Player owner)
        {
            if (hasExploded)
                return;
            hasExploded = true;

            owner.Calamity().GeneralScreenShakePower = Math.Max(owner.Calamity().GeneralScreenShakePower, 7f);
            SoundEngine.PlaySound(SoundID.Item62 with { Volume = 0.85f, Pitch = -0.1f }, Projectile.Center);
            SoundEngine.PlaySound(new SoundStyle("CalamityMod/Sounds/Item/DeadSunExplosion") { Volume = 0.6f, Pitch = -0.15f }, Projectile.Center);

            if (!Main.dedServ)
            {
                GeneralParticleHandler.SpawnParticle(new StrongBloom(Projectile.Center, Vector2.Zero, new Color(255, 214, 88), 1.15f, 18));
                GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(Projectile.Center, Vector2.Zero, new Color(255, 214, 88), Vector2.One, Projectile.rotation, 0.14f, 2.6f, 24));
                for (int i = 0; i < 36; i++)
                {
                    Vector2 vel = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(4f, 16f);
                    Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.GoldFlame, vel, 0, Main.rand.NextBool(3) ? Color.White : new Color(255, 210, 70), Main.rand.NextFloat(1.0f, 1.8f));
                    d.noGravity = true;
                }
                for (int i = 0; i < 12; i++)
                {
                    Vector2 vel = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(6f, 18f);
                    GeneralParticleHandler.SpawnParticle(new CustomSpark(
                        Projectile.Center,
                        vel,
                        "CalamityMod/Particles/Sparkle",
                        false,
                        Main.rand.Next(16, 26),
                        Main.rand.NextFloat(0.6f, 1.1f),
                        Main.rand.NextBool(3) ? Color.White : new Color(255, 214, 88),
                        new Vector2(0.35f, 1.3f),
                        true, true,
                        shrinkSpeed: 0.12f));
                }
            }

            if (Projectile.owner != Main.myPlayer)
                return;

            int count = 6;
            for (int i = 0; i < count; i++)
            {
                float angle = MathHelper.TwoPi * i / count;
                Vector2 velocity = angle.ToRotationVector2() * Main.rand.NextFloat(7f, 10f);
                int shard = Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    Projectile.Center,
                    velocity,
                    ModContent.ProjectileType<YC_BurningShard>(),
                    Math.Max(1, (int)(Projectile.damage * 0.6f)),
                    Projectile.knockBack * 0.3f,
                    Projectile.owner);

                if (Main.projectile.IndexInRange(shard))
                    Main.projectile[shard].CritChance = Projectile.CritChance;
            }
        }

        private void PlayTravelWhoosh()
        {
            if ((int)Projectile.localAI[0] % 14 == 0)
                SoundEngine.PlaySound(new SoundStyle("CalamityMod/Sounds/Item/SwooshMid") { Volume = 0.55f, Pitch = -0.25f, MaxInstances = 3 }, Projectile.Center);
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

        // Gold/orange sparks streaming off behind the blade as it travels — the same CustomSpark
        // + Sparkle vocabulary the spinning version used, but now shed along the flight path
        // instead of flung off a spinning rim. Denser and brighter once it is dashing.
        private void EmitRightThrowFlightTrail(bool postTrigger)
        {
            if (Main.dedServ)
                return;

            Player owner = Main.player[Projectile.owner];
            if (Vector2.Distance(owner.Center, Projectile.Center) >= 1400f)
                return;

            float speed = Projectile.velocity.Length();
            float speedFactor = Utils.GetLerpValue(2f, 26f, speed, true);
            Vector2 travel = Projectile.velocity.SafeNormalize(Vector2.UnitX * Projectile.direction);

            // 1. High quality trailing dots (PointParticle) along flight axis when moving fast
            if (speed > 3f)
            {
                Vector2 pointPos = Projectile.Center - travel * Main.rand.NextFloat(8f, 50f * (1f + speedFactor));
                GeneralParticleHandler.SpawnParticle(new PointParticle(
                    pointPos,
                    -travel * Main.rand.NextFloat(1f, 4f),
                    false,
                    Main.rand.Next(8, 15),
                    Main.rand.NextFloat(0.6f, 1.1f) * (0.6f + speedFactor * 0.5f),
                    Main.rand.NextBool(3) ? Color.White : new Color(255, 214, 88),
                    true));
            }

            // 2. CustomSpark particles scaled by speed
            int count = postTrigger ? (int)(2 + speedFactor * 3) : (int)(1 + speedFactor * 2);
            for (int i = 0; i < count; i++)
            {
                Vector2 pos = Projectile.Center - travel * Main.rand.NextFloat(6f, 44f * (1f + speedFactor)) + Main.rand.NextVector2Circular(12f, 12f);
                Vector2 sparkVelocity = -travel * Main.rand.NextFloat(1f, 3f + speedFactor * 8f) + Main.rand.NextVector2Circular(1.6f, 1.6f);

                GeneralParticleHandler.SpawnParticle(new CustomSpark(
                    pos,
                    sparkVelocity,
                    "CalamityMod/Particles/Sparkle",
                    false,
                    postTrigger ? Main.rand.Next(10, 16) : Main.rand.Next(7, 15),
                    postTrigger ? Main.rand.NextFloat(0.35f, 0.75f) * (0.8f + speedFactor * 0.4f) : Main.rand.NextFloat(0.4f, 0.7f),
                    (postTrigger ? new Color(255, 214, 88) : new Color(255, 111, 34)) * (postTrigger ? 0.55f : 0.45f),
                    new Vector2(1f, 1.1f + speedFactor * 0.8f),
                    true,
                    false,
                    Main.rand.NextFloat(-10, 10)));
            }

            if (postTrigger && Main.rand.NextBool(2))
            {
                GeneralParticleHandler.SpawnParticle(new CustomSpark(
                    Projectile.Center + Main.rand.NextVector2Circular(60f, 60f),
                    -Projectile.velocity * Main.rand.NextFloat(0.2f, 1f),
                    "CalamityMod/Particles/Sparkle",
                    false,
                    Main.rand.Next(24, 36),
                    Main.rand.NextFloat(1.0f, 1.3f),
                    new Color(255, 214, 88),
                    new Vector2(0.4f, 1.2f)));
            }
        }

        // Mid-air charge while it hangs and locks aim: gold motes and dust are drawn inward to
        // the blade, selling the "gathering power before it rockets in" beat.
        private void EmitRightThrowAimCharge(NPC target)
        {
            if (Main.dedServ)
                return;

            if (Main.rand.NextBool())
            {
                Vector2 spawn = Projectile.Center + Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(52f, 104f);
                Vector2 velocity = (Projectile.Center - spawn).SafeNormalize(Vector2.Zero) * Main.rand.NextFloat(3f, 7.5f);
                GeneralParticleHandler.SpawnParticle(new CustomSpark(
                    spawn,
                    velocity,
                    "CalamityMod/Particles/Sparkle",
                    false,
                    Main.rand.Next(10, 18),
                    Main.rand.NextFloat(0.4f, 0.8f),
                    Main.rand.NextBool(3) ? Color.White : new Color(255, 200, 70),
                    new Vector2(0.3f, 1f),
                    true,
                    true,
                    shrinkSpeed: 0.15f));
            }

            if (Main.rand.NextBool(2))
            {
                Vector2 spawn = Projectile.Center + Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(40f, 82f);
                Dust d = Dust.NewDustPerfect(spawn, DustID.GoldFlame, (Projectile.Center - spawn).SafeNormalize(Vector2.Zero) * Main.rand.NextFloat(2f, 5f), 0, default, 1.1f);
                d.noGravity = true;
            }
        }

        private void MaintainRightThrowSpinSound(float pitch)
        {
            if (Main.dedServ)
                return;

            if (SoundEngine.TryGetActiveSound(spinSoundSlot, out ActiveSound spinSound) && spinSound.IsPlaying)
            {
                spinSound.Position = Projectile.Center;
                spinSoundRefreshDelay = 10;
                return;
            }

            if (spinSoundRefreshDelay > 0)
            {
                spinSoundRefreshDelay--;
                return;
            }

            spinSoundSlot = SoundEngine.PlaySound(new SoundStyle("CalamityMod/Sounds/Item/SpinningWoosh")
            {
                Volume = 0.72f,
                Pitch = pitch,
                MaxInstances = 4
            }, Projectile.Center);
            spinSoundRefreshDelay = 18;
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
            bool rushImpact = Projectile.ai[0] == StateRightThrow;

            if (Projectile.ai[0] != StateStuck && !rushImpact)
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
            else if (rushImpact)
            {
                Player owner = Main.player[Projectile.owner];

                if (Projectile.ai[1] != RightThrowImpaled)
                {
                    // 首次命中：立刻钉入目标，停止一切追踪；本次命中记为第1跳
                    Projectile.ai[1] = RightThrowImpaled;
                    Projectile.ai[2] = target.whoAmI;
                    Projectile.localAI[0] = 0f;
                    Projectile.localAI[1] = 1f;
                    impaleOffset = (target.Center - Projectile.Center) * 0.6f;
                    Projectile.velocity = Vector2.Zero;
                    Projectile.localNPCHitCooldown = ImpaleTickInterval;
                    Projectile.timeLeft = ImpaleTickInterval * (ImpaleTickCount + 3);
                    Projectile.netUpdate = true;
                    if (SoundEngine.TryGetActiveSound(spinSoundSlot, out ActiveSound impaleSpin))
                        impaleSpin.Stop();
                    TriggerSlamImpact(owner, target);
                }
                else
                {
                    Projectile.localAI[1]++;
                }

                owner.Calamity().GeneralScreenShakePower = Math.Max(owner.Calamity().GeneralScreenShakePower, 2.2f);
                if (!Main.dedServ)
                {
                    for (int k = 0; k < 8; k++)
                    {
                        Vector2 vel = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(3f, 10f);
                        Dust d = Dust.NewDustPerfect(target.Center + Main.rand.NextVector2Circular(14f, 14f), DustID.GoldFlame, vel, 0, Main.rand.NextBool(3) ? Color.White : new Color(255, 210, 80), Main.rand.NextFloat(0.9f, 1.4f));
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

        public override void OnKill(int timeLeft)
        {
            if (SoundEngine.TryGetActiveSound(spinSoundSlot, out ActiveSound spin))
                spin.Stop();

            // Neptune's OnKill always bursts dust regardless of cause (hit vs. timing out);
            // hasExploded guards against double-firing when a state already triggered it.
            if (Projectile.ai[0] == StateRightThrow)
                SelfDestructIntoFireballs(Main.player[Projectile.owner]);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = ModContent.Request<Texture2D>(Texture).Value;
            Texture2D glow = ModContent.Request<Texture2D>("CalamityMod/Items/Weapons/Melee/EarthGlow").Value;
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Vector2 origin = texture.Size() * 0.5f;
            Vector2 drawPos = Projectile.Center - Main.screenPosition;
            SpriteEffects effects = SpriteEffects.None;
            bool impaled = Projectile.ai[0] == StateRightThrow && Projectile.ai[1] == RightThrowImpaled;

            if (Projectile.ai[0] == StateDirectedThrow || (Projectile.ai[0] == StateRightThrow && !impaled))
            {
                Vector2 travelDirection = GetTravelDirectionForDraw();
                Vector2 tailDirection = -travelDirection;
                float pulse = 0.9f + 0.1f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 20f);

                Main.spriteBatch.SetBlendState(BlendState.Additive);
                if (Projectile.ai[0] == StateRightThrow)
                    DrawRightThrowMotionSmear(drawPos);

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
                // Additive 混合下颜色必须保留不透明度，A=0 会被 SourceAlpha 因子乘成全透明
                Main.EntitySpriteDraw(glow, drawPos, null, new Color(255, 223, 132) * 0.72f, Projectile.rotation, origin, Projectile.scale * 1.16f, effects, 0);
                Main.spriteBatch.SetBlendState(BlendState.AlphaBlend);
            }

            if (Projectile.ai[0] != StateStuck && !impaled)
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

            // 右键投掷的金色包边：刀刃本体沿环形偏移用纯金色重绘一圈
            if (Projectile.ai[0] == StateRightThrow)
            {
                Color outlineGold = new Color(255, 214, 88) with { A = 0 };
                float outlinePulse = 0.8f + 0.2f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 9f);
                float outlineRadius = 3f * outlinePulse;
                for (int i = 0; i < 8; i++)
                {
                    Vector2 outlineOffset = (MathHelper.TwoPi * i / 8f).ToRotationVector2() * outlineRadius;
                    Main.EntitySpriteDraw(texture, drawPos + outlineOffset, null, outlineGold * 0.55f, Projectile.rotation, origin, Projectile.scale, effects, 0);
                }
            }

            Main.EntitySpriteDraw(texture, drawPos, null, lightColor, Projectile.rotation, origin, Projectile.scale, effects, 0);
            Main.EntitySpriteDraw(glow, drawPos, null, new Color(255, 214, 88) * 0.8f, Projectile.rotation, origin, Projectile.scale, effects, 0);
            return false;
        }

        // Replaces the old spinning smear-disc now that the blade no longer free-spins: a soft
        // energy sheath wraps the blade (gentle wall-clock pulse, brighter once dashing), and a
        // directional motion streak stretches along the travel axis, growing with speed so it
        // only streaks while actually moving fast. Same CircularSmearSmokey + gold/orange palette
        // the disc used — just directional instead of rotational.
        private void DrawRightThrowMotionSmear(Vector2 drawPos)
        {
            Texture2D smoke = ModContent.Request<Texture2D>("CalamityMod/Particles/CircularSmearSmokey").Value;
            Texture2D smear = ModContent.Request<Texture2D>("CalamityMod/Particles/VerticalSmearLarge").Value;
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            bool postTrigger = Projectile.ai[1] == RightThrowDashing;
            Color gold = new Color(255, 214, 88) with { A = 0 };
            Color orange = new Color(255, 111, 34) with { A = 0 };

            float speed = Projectile.velocity.Length();
            float speedFactor = Utils.GetLerpValue(2f, 24f, speed, true); // 0 hovering, 1 dashing
            float travelAngle = Projectile.velocity.ToRotation();
            float pulse = 0.82f + 0.18f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 12f);

            // Energy sheath around the blade (always present; the base of the glow/包边).
            Main.EntitySpriteDraw(smoke, drawPos, null,
                (postTrigger ? gold : orange) * (0.55f * pulse),
                Main.GlobalTimeWrappedHourly * 2.4f,
                smoke.Size() * 0.5f,
                Projectile.scale * (0.95f + speedFactor * 0.55f),
                SpriteEffects.None);

            // Directional motion streak stretched along the travel axis, scaled by speed.
            if (speedFactor > 0.05f)
            {
                Main.EntitySpriteDraw(smoke, drawPos, null,
                    gold * (0.25f + 0.4f * speedFactor),
                    travelAngle,
                    smoke.Size() * 0.5f,
                    new Vector2(1.1f + 1.9f * speedFactor, 0.6f) * Projectile.scale,
                    SpriteEffects.None,
                    0);

                // High speed vertical smear trail
                Main.EntitySpriteDraw(smear, drawPos - Projectile.velocity.SafeNormalize(Vector2.UnitX) * 22f * speedFactor, null,
                    gold * 0.45f * speedFactor,
                    travelAngle + MathHelper.PiOver2,
                    smear.Size() * 0.5f,
                    new Vector2(0.5f + 0.7f * speedFactor, 0.35f + 0.5f * speedFactor) * Projectile.scale,
                    SpriteEffects.None,
                    0);

                // Bright leading edge tip bloom
                Vector2 tipPos = drawPos + Projectile.velocity.SafeNormalize(Vector2.UnitX) * 36f * Projectile.scale;
                Main.EntitySpriteDraw(bloom, tipPos, null,
                    gold * 0.65f * speedFactor,
                    0f,
                    bloom.Size() * 0.5f,
                    0.28f * speedFactor,
                    SpriteEffects.None,
                    0);
            }
        }
    }
}
