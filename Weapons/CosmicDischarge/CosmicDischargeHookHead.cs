using CalamityMod;
using CalamityMod.Buffs.DamageOverTime;
using CalamityMod.Buffs.StatBuffs;
using CalamityMod.Buffs.StatDebuffs;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.CosmicDischarge
{
    public class CosmicDischargeHookHead : ModProjectile, ILocalizedModType
    {
        private enum HookState
        {
            Firing,
            Latched,
            Reeling
        }

        private const int DefaultLatchTime = 60;
        private const float MaxFireDistance = 850f;
        private const float ReelSpeed = 34f;

        public new string LocalizationCategory => "Projectiles.Melee";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private HookState State
        {
            get => (HookState)(int)Projectile.ai[0];
            set => Projectile.ai[0] = (int)value;
        }

        private ref float StateTimer => ref Projectile.ai[1];
        private ref float TargetIndex => ref Projectile.ai[2];
        private Vector2 latchOffset;
        private int explosionCooldown;

        private Player Owner => Main.player[Projectile.owner];

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
            Projectile.localNPCHitCooldown = 6;
            Projectile.ownerHitCheck = true;
            Projectile.coldDamage = true;
        }

        public override void AI()
        {
            if (!Owner.active || Owner.dead || Owner.HeldItem.type != ModContent.ItemType<NewLegendCosmicDischarge>())
            {
                Projectile.Kill();
                return;
            }

            Projectile.timeLeft = 2;
            StateTimer++;
            if (explosionCooldown > 0)
                explosionCooldown--;

            switch (State)
            {
                case HookState.Firing:
                    UpdateFiring();
                    break;
                case HookState.Latched:
                    UpdateLatched();
                    break;
                case HookState.Reeling:
                    UpdateReeling();
                    break;
            }
        }

        private void UpdateFiring()
        {
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
            if (Projectile.Distance(Owner.MountedCenter) > MaxFireDistance || StateTimer > 36f)
                BeginReel();

            if (Main.rand.NextBool(2))
            {
                Dust dust = Dust.NewDustPerfect(
                    Projectile.Center,
                    Main.rand.NextBool() ? 67 : 187,
                    Projectile.velocity.RotatedByRandom(0.35f) * Main.rand.NextFloat(0.25f, 0.9f),
                    120,
                    CosmicDischargeCommon.FrostCoreColor,
                    Main.rand.NextFloat(1f, 1.35f));
                dust.noGravity = true;
            }
        }

        private void UpdateLatched()
        {
            int npcIndex = (int)TargetIndex;
            if (npcIndex < 0 || npcIndex >= Main.maxNPCs)
            {
                BeginReel();
                return;
            }

            NPC target = Main.npc[npcIndex];
            if (!target.active || target.dontTakeDamage || target.friendly)
            {
                BeginReel();
                return;
            }

            Projectile.Center = target.Center + latchOffset;
            Projectile.velocity = Vector2.Zero;
            Projectile.rotation += 0.5f;

            bool sustain = Main.myPlayer == Projectile.owner && Owner.channel && Owner.Calamity().mouseRight;
            if (!sustain && StateTimer >= DefaultLatchTime)
            {
                BeginReel();
                return;
            }

            if (sustain && Main.myPlayer == Projectile.owner && (int)StateTimer % 10 == 0)
            {
                for (int i = 0; i < 2; i++)
                {
                    Vector2 velocity = (MathHelper.TwoPi * i / 2f + Main.rand.NextFloat(-0.35f, 0.35f)).ToRotationVector2() * Main.rand.NextFloat(7f, 10f);
                    Projectile.NewProjectile(
                        Projectile.GetSource_FromThis(),
                        Projectile.Center,
                        velocity,
                        ModContent.ProjectileType<CosmicDischargeHookLimb>(),
                        (int)(Projectile.damage * 0.34f),
                        0f,
                        Projectile.owner);
                }
            }

            if (Main.rand.NextBool(2))
            {
                Dust dust = Dust.NewDustPerfect(
                    Projectile.Center + Main.rand.NextVector2Circular(8f, 8f),
                    Main.rand.NextBool() ? 67 : 187,
                    Main.rand.NextVector2Circular(0.8f, 0.8f),
                    120,
                    CosmicDischargeCommon.FrostGlowColor,
                    Main.rand.NextFloat(1.05f, 1.45f));
                dust.noGravity = true;
            }
        }

        private void UpdateReeling()
        {
            Vector2 toOwner = Owner.MountedCenter - Projectile.Center;
            if (toOwner.LengthSquared() < 28f * 28f)
            {
                Projectile.Kill();
                return;
            }

            Vector2 direction = toOwner.SafeNormalize(Vector2.UnitX * Owner.direction);
            Projectile.velocity = Vector2.Lerp(Projectile.velocity, direction * ReelSpeed, 0.16f);
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
        }

        private void BeginReel()
        {
            State = HookState.Reeling;
            StateTimer = 0f;
            Projectile.localNPCHitCooldown = 12;
            Projectile.netUpdate = true;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(ModContent.BuffType<Nightwither>(), 240);
            target.AddBuff(ModContent.BuffType<GlacialState>(), 120);
            Owner.AddBuff(ModContent.BuffType<CosmicFreeze>(), 180);

            if (State == HookState.Firing)
            {
                State = HookState.Latched;
                StateTimer = 0f;
                TargetIndex = target.whoAmI;
                latchOffset = Projectile.Center - target.Center;
                Projectile.Center = target.Center + latchOffset;
                Projectile.velocity = Vector2.Zero;
                Projectile.netUpdate = true;
                SoundEngine.PlaySound(SoundID.NPCHit5 with { Pitch = -0.2f, Volume = 0.8f }, Projectile.Center);
            }

            if (State == HookState.Latched && explosionCooldown <= 0 && Main.myPlayer == Projectile.owner)
            {
                Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    target.Center,
                    Vector2.Zero,
                    ModContent.ProjectileType<CalamityMod.Projectiles.Melee.CosmicIceBurst>(),
                    (int)(Projectile.damage * 0.55f),
                    0f,
                    Projectile.owner,
                    0f,
                    1.15f);

                explosionCooldown = 10;
            }
        }

        public override bool? CanDamage()
        {
            return State != HookState.Reeling;
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            BeginReel();
            return false;
        }
    }
}
