using CalamityMod;
using CalamityMod.Buffs.DamageOverTime;
using CalamityMod.Buffs.StatBuffs;
using CalamityMod.Buffs.StatDebuffs;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.CosmicDischarge
{
    public class CosmicDischargeComboHoldout : ModProjectile, ILocalizedModType
    {
        private enum ComboPhase
        {
            SwingClockwiseOne,
            SwingCounterClockwiseOne,
            ForwardThrustOne,
            WaitOne,
            SwingClockwiseTwo,
            SwingCounterClockwiseTwo,
            ForwardThrustTwo,
            WaitTwo,
            HookAttack
        }

        private const int SwingDuration = 18;
        private const int ThrustDuration = 20;
        private const int WaitDuration = 5;
        private const float BaseSwingArc = MathHelper.Pi / 6f;
        private const float ExpandedSwingArc = MathHelper.TwoPi;
        private const float SwingReach = 228f;
        private const float ThrustReach = 310f;
        private const float CollisionWidth = 24f;

        public new string LocalizationCategory => "Projectiles.Melee";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private ComboPhase Phase
        {
            get => (ComboPhase)(int)Projectile.ai[0];
            set => Projectile.ai[0] = (int)value;
        }

        private ref float PhaseTimer => ref Projectile.ai[1];
        private ref float StoredAimAngle => ref Projectile.localAI[1];
        private ref float HookProjectileIdentity => ref Projectile.localAI[0];

        private Vector2 OwnerCenter => Owner.MountedCenter;
        private Player Owner => Main.player[Projectile.owner];
        private bool RightHeld => Main.myPlayer == Projectile.owner && Owner.Calamity().mouseRight;

        public override void SetDefaults()
        {
            Projectile.width = 24;
            Projectile.height = 24;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.DamageType = DamageClass.MeleeNoSpeed;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 8;
            Projectile.ownerHitCheck = true;
            Projectile.coldDamage = true;
        }

        public override void OnSpawn(Terraria.DataStructures.IEntitySource source)
        {
            StoredAimAngle = Projectile.velocity.SafeNormalize(Vector2.UnitX * Owner.direction).ToRotation();
            HookProjectileIdentity = -1f;
            Projectile.timeLeft = 2;
        }

        public override void AI()
        {
            if (!Owner.active || Owner.dead || Owner.HeldItem.type != ModContent.ItemType<NewLegendCosmicDischarge>())
            {
                Projectile.Kill();
                return;
            }

            if (Main.myPlayer == Projectile.owner && !Owner.channel)
            {
                Projectile.Kill();
                return;
            }

            Projectile.timeLeft = 2;
            Owner.Calamity().mouseWorldListener = true;
            Owner.Calamity().rightClickListener = true;

            Vector2 aimDirection = CosmicDischargeCommon.GetAimDirection(Owner, StoredAimAngle.ToRotationVector2());
            CosmicDischargeCommon.HoldPlayer(Owner, Projectile, aimDirection);

            if (PhaseTimer == 0f)
                StoredAimAngle = aimDirection.ToRotation();

            if (Phase == ComboPhase.HookAttack)
            {
                UpdateHookPhase();
                return;
            }

            PhaseTimer++;
            switch (Phase)
            {
                case ComboPhase.SwingClockwiseOne:
                case ComboPhase.SwingClockwiseTwo:
                    UpdateSwing(true);
                    if (PhaseTimer >= SwingDuration)
                        AdvancePhase();
                    break;

                case ComboPhase.SwingCounterClockwiseOne:
                case ComboPhase.SwingCounterClockwiseTwo:
                    UpdateSwing(false);
                    if (PhaseTimer >= SwingDuration)
                        AdvancePhase();
                    break;

                case ComboPhase.ForwardThrustOne:
                case ComboPhase.ForwardThrustTwo:
                    UpdateThrust();
                    if (PhaseTimer >= ThrustDuration)
                        AdvancePhase();
                    break;

                case ComboPhase.WaitOne:
                case ComboPhase.WaitTwo:
                    Projectile.Center = OwnerCenter;
                    Projectile.rotation = StoredAimAngle + MathHelper.PiOver2;
                    if (PhaseTimer >= WaitDuration)
                        AdvancePhase();
                    break;
            }
        }

        private void UpdateSwing(bool clockwise)
        {
            float arc = RightHeld ? ExpandedSwingArc : BaseSwingArc;
            float eased = Utils.GetLerpValue(0f, SwingDuration, PhaseTimer, true);
            float start = clockwise ? arc * 0.5f : -arc * 0.5f;
            float end = clockwise ? -arc * 0.5f : arc * 0.5f;
            float angle = StoredAimAngle + MathHelper.Lerp(start, end, CalamityUtils.SineBumpEasing(eased, 1));
            Vector2 chainDirection = angle.ToRotationVector2();

            Projectile.Center = OwnerCenter + chainDirection * SwingReach;
            Projectile.rotation = chainDirection.ToRotation() + MathHelper.PiOver2;
            Owner.ChangeDir(chainDirection.X >= 0f ? 1 : -1);

            if (PhaseTimer == 1f)
            {
                SoundEngine.PlaySound(SoundID.Item71 with { Pitch = clockwise ? -0.15f : 0.1f, Volume = 0.55f }, Projectile.Center);
            }

            if (PhaseTimer >= 4f && PhaseTimer <= SwingDuration - 3f && (int)PhaseTimer % 6 == 0 && Main.myPlayer == Projectile.owner)
            {
                Vector2 scytheVelocity = chainDirection.RotatedBy(clockwise ? -0.25f : 0.25f) * 11f;
                Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    Projectile.Center,
                    scytheVelocity,
                    ModContent.ProjectileType<CosmicDischargeScytheProjectile>(),
                    (int)(Projectile.damage * 0.55f),
                    Projectile.knockBack,
                    Projectile.owner,
                    0f);
            }

            if (Main.rand.NextBool(2))
            {
                Dust dust = Dust.NewDustPerfect(
                    Projectile.Center,
                    Main.rand.NextBool() ? 67 : 187,
                    chainDirection.RotatedByRandom(0.25f) * Main.rand.NextFloat(0.8f, 2.4f),
                    120,
                    CosmicDischargeCommon.FrostCoreColor,
                    Main.rand.NextFloat(1f, 1.5f));
                dust.noGravity = true;
            }
        }

        private void UpdateThrust()
        {
            Vector2 direction = StoredAimAngle.ToRotationVector2();
            float progress = Utils.GetLerpValue(0f, ThrustDuration, PhaseTimer, true);
            float reachRatio = progress <= 0.5f ? progress / 0.5f : 1f - (progress - 0.5f) / 0.5f;
            float reach = MathHelper.Lerp(72f, ThrustReach, CalamityUtils.SineBumpEasing(reachRatio, 1));

            Projectile.Center = OwnerCenter + direction * reach;
            Projectile.rotation = direction.ToRotation() + MathHelper.PiOver2;
            Owner.ChangeDir(direction.X >= 0f ? 1 : -1);

            if (PhaseTimer == 1f)
                SoundEngine.PlaySound(SoundID.Item122 with { Pitch = 0.18f, Volume = 0.62f }, Projectile.Center);

            if (PhaseTimer >= 3f && PhaseTimer <= ThrustDuration - 4f && (int)PhaseTimer % 3 == 0 && Main.myPlayer == Projectile.owner)
            {
                Vector2 spawnPosition = OwnerCenter + direction * MathHelper.Lerp(60f, reach, Main.rand.NextFloat());
                Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    spawnPosition,
                    direction * Main.rand.NextFloat(7.5f, 10.5f),
                    ModContent.ProjectileType<CosmicDischargeScytheProjectile>(),
                    (int)(Projectile.damage * 0.48f),
                    Projectile.knockBack,
                    Projectile.owner,
                    1f);
            }

            if (Main.rand.NextBool(2))
            {
                Dust dust = Dust.NewDustPerfect(
                    Projectile.Center,
                    Main.rand.NextBool() ? 67 : 187,
                    direction * Main.rand.NextFloat(0.5f, 1.8f),
                    120,
                    CosmicDischargeCommon.FrostCoreColor,
                    Main.rand.NextFloat(1.1f, 1.6f));
                dust.noGravity = true;
            }
        }

        private void UpdateHookPhase()
        {
            if (PhaseTimer == 0f && Main.myPlayer == Projectile.owner)
            {
                Vector2 direction = StoredAimAngle.ToRotationVector2();
                Projectile hook = Projectile.NewProjectileDirect(
                    Projectile.GetSource_FromThis(),
                    OwnerCenter,
                    direction * 24f,
                    ModContent.ProjectileType<CosmicDischargeHookHead>(),
                    (int)(Projectile.damage * 1.15f),
                    Projectile.knockBack,
                    Projectile.owner);
                HookProjectileIdentity = hook.identity;
                SoundEngine.PlaySound(SoundID.Item125 with { Pitch = 0.15f, Volume = 0.72f }, Projectile.Center);
            }

            Projectile.Center = OwnerCenter;
            Projectile.rotation = StoredAimAngle + MathHelper.PiOver2;
            PhaseTimer++;

            Projectile hookProjectile = GetOwnedHookProjectile();
            if (hookProjectile != null)
            {
                Projectile.Center = hookProjectile.Center;
                Projectile.rotation = hookProjectile.velocity.LengthSquared() > 1f
                    ? hookProjectile.velocity.ToRotation() + MathHelper.PiOver2
                    : (hookProjectile.Center - OwnerCenter).ToRotation() + MathHelper.PiOver2;
                return;
            }

            HookProjectileIdentity = -1f;
            Phase = ComboPhase.SwingClockwiseOne;
            PhaseTimer = 0f;
        }

        private Projectile GetOwnedHookProjectile()
        {
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile projectile = Main.projectile[i];
                if (!projectile.active || projectile.owner != Projectile.owner || projectile.type != ModContent.ProjectileType<CosmicDischargeHookHead>())
                    continue;

                if ((int)HookProjectileIdentity == projectile.identity)
                    return projectile;
            }

            return null;
        }

        private void AdvancePhase()
        {
            PhaseTimer = 0f;
            Phase = Phase switch
            {
                ComboPhase.SwingClockwiseOne => ComboPhase.SwingCounterClockwiseOne,
                ComboPhase.SwingCounterClockwiseOne => ComboPhase.ForwardThrustOne,
                ComboPhase.ForwardThrustOne => ComboPhase.WaitOne,
                ComboPhase.WaitOne => ComboPhase.SwingClockwiseTwo,
                ComboPhase.SwingClockwiseTwo => ComboPhase.SwingCounterClockwiseTwo,
                ComboPhase.SwingCounterClockwiseTwo => ComboPhase.ForwardThrustTwo,
                ComboPhase.ForwardThrustTwo => ComboPhase.WaitTwo,
                ComboPhase.WaitTwo => ComboPhase.HookAttack,
                _ => ComboPhase.SwingClockwiseOne
            };
        }

        public override bool? CanDamage()
        {
            return Phase switch
            {
                ComboPhase.SwingClockwiseOne or ComboPhase.SwingCounterClockwiseOne or ComboPhase.SwingClockwiseTwo or ComboPhase.SwingCounterClockwiseTwo
                    => PhaseTimer >= 4f && PhaseTimer <= SwingDuration - 3f,
                ComboPhase.ForwardThrustOne or ComboPhase.ForwardThrustTwo
                    => PhaseTimer >= 3f && PhaseTimer <= ThrustDuration - 3f,
                _ => false
            };
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            if (Phase == ComboPhase.HookAttack)
                return false;

            Vector2 start = OwnerCenter;
            Vector2 end = Projectile.Center;
            float collisionPoint = 0f;
            return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), start, end, CollisionWidth, ref collisionPoint);
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(ModContent.BuffType<Nightwither>(), 180);
            target.AddBuff(ModContent.BuffType<GlacialState>(), 60);
            Owner.AddBuff(ModContent.BuffType<CosmicFreeze>(), 180);

            if (Phase == ComboPhase.ForwardThrustOne || Phase == ComboPhase.ForwardThrustTwo)
            {
                if (Main.myPlayer == Projectile.owner)
                {
                    Projectile.NewProjectile(
                        Projectile.GetSource_FromThis(),
                        target.Center,
                        Vector2.Zero,
                        ModContent.ProjectileType<CalamityMod.Projectiles.Melee.CosmicIceBurst>(),
                        (int)(Projectile.damage * 0.45f),
                        0f,
                        Projectile.owner,
                        0f,
                        1.05f);
                }
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Color drawColor = Color.Lerp(CosmicDischargeCommon.FrostDarkColor, CosmicDischargeCommon.FrostCoreColor, 0.65f);
            CosmicDischargeCommon.DrawChain(Main.spriteBatch, OwnerCenter, Projectile.Center, drawColor, 1f, Phase == ComboPhase.HookAttack, Owner.gfxOffY);

            if (RightHeld && Phase != ComboPhase.HookAttack && (Phase == ComboPhase.SwingClockwiseOne || Phase == ComboPhase.SwingCounterClockwiseOne || Phase == ComboPhase.SwingClockwiseTwo || Phase == ComboPhase.SwingCounterClockwiseTwo))
                CosmicDischargeCommon.DrawRightHoldIndicator(Main.spriteBatch, Owner, 1f);

            return false;
        }
    }
}
