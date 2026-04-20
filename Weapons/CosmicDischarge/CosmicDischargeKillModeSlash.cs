using CalamityMod;
using CalamityMod.Buffs.DamageOverTime;
using CalamityMod.Buffs.StatBuffs;
using CalamityMod.Buffs.StatDebuffs;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.CosmicDischarge
{
    public class CosmicDischargeKillModeSlash : ModProjectile, ILocalizedModType
    {
        private const int SlashDuration = 34;
        private const float SlashReach = 520f;
        private const float SlashArc = 4.537856f;

        public new string LocalizationCategory => "Projectiles.Melee";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private ref float AimAngle => ref Projectile.ai[0];
        private ref float Time => ref Projectile.ai[1];
        private Player Owner => Main.player[Projectile.owner];

        public override void SetDefaults()
        {
            Projectile.width = 40;
            Projectile.height = 40;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.DamageType = DamageClass.MeleeNoSpeed;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 12;
            Projectile.ownerHitCheck = true;
            Projectile.coldDamage = true;
        }

        public override void OnSpawn(Terraria.DataStructures.IEntitySource source)
        {
            Owner.SetImmuneTimeForAllTypes(10);
            Owner.velocity *= 0.4f;
            SoundStyle activateSound = new("CalamityMod/Sounds/Item/DemonSwordSwing");
            SoundEngine.PlaySound(activateSound with { Volume = 0.9f, Pitch = -0.35f }, Owner.Center);

            GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(
                Owner.Center,
                Vector2.Zero,
                CosmicDischargeCommon.FrostGlowColor * 0.35f,
                Vector2.One,
                0f,
                0.04f,
                0.3f,
                18));

            for (int i = 0; i < 18; i++)
            {
                Vector2 velocity = (MathHelper.TwoPi * i / 18f).ToRotationVector2() * Main.rand.NextFloat(4f, 10f);
                Dust dust = Dust.NewDustPerfect(Owner.Center, Main.rand.NextBool() ? 67 : 187, velocity, 120, CosmicDischargeCommon.FrostCoreColor, Main.rand.NextFloat(1.15f, 1.7f));
                dust.noGravity = true;
            }
        }

        public override void AI()
        {
            if (!Owner.active || Owner.dead || Owner.HeldItem.type != ModContent.ItemType<NewLegendCosmicDischarge>())
            {
                Projectile.Kill();
                return;
            }

            Projectile.timeLeft = 2;
            Time++;

            Vector2 aimDirection = AimAngle.ToRotationVector2();
            CosmicDischargeCommon.HoldPlayer(Owner, Projectile, aimDirection);

            float progress = Utils.GetLerpValue(0f, SlashDuration, Time, true);
            float swingDirection = Owner.direction;
            float angle = AimAngle + MathHelper.Lerp(-SlashArc * 0.5f * swingDirection, SlashArc * 0.5f * swingDirection, CalamityUtils.SineBumpEasing(progress, 1));
            Vector2 slashDirection = angle.ToRotationVector2();

            Projectile.Center = Owner.MountedCenter + slashDirection * SlashReach;
            Projectile.rotation = slashDirection.ToRotation() + MathHelper.PiOver2;

            if (Time == 1f)
                Owner.SetScreenshake(9f);

            if (Time >= 7f && Time <= SlashDuration - 5f)
            {
                if (Main.rand.NextBool(2))
                {
                    Dust dust = Dust.NewDustPerfect(
                        Projectile.Center + Main.rand.NextVector2Circular(18f, 18f),
                        Main.rand.NextBool() ? 67 : 187,
                        slashDirection.RotatedByRandom(0.35f) * Main.rand.NextFloat(1.5f, 5.5f),
                        120,
                        CosmicDischargeCommon.FrostCoreColor,
                        Main.rand.NextFloat(1.15f, 1.8f));
                    dust.noGravity = true;
                }

                if (Main.rand.NextBool(5))
                {
                    GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(
                        Projectile.Center,
                        slashDirection * 0.3f,
                        CosmicDischargeCommon.FrostGlowColor * 0.3f,
                        Vector2.One,
                        slashDirection.ToRotation(),
                        0.02f,
                        0.12f,
                        10));
                }
            }

            if (Time >= SlashDuration)
                Projectile.Kill();
        }

        public override bool? CanDamage()
        {
            return Time >= 7f && Time <= SlashDuration - 4f;
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            Vector2 start = Owner.MountedCenter;
            Vector2 end = Projectile.Center;
            float collisionPoint = 0f;
            return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), start, end, 46f, ref collisionPoint);
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(ModContent.BuffType<Nightwither>(), 240);
            target.AddBuff(ModContent.BuffType<GlacialState>(), 180);
            Owner.AddBuff(ModContent.BuffType<CosmicFreeze>(), 240);
            Owner.SetScreenshake(8f);

            SoundStyle hitSound = new("CalamityMod/Sounds/Item/DemonSwordInsaneImpact");
            SoundEngine.PlaySound(hitSound with { Volume = 0.7f, Pitch = -0.1f }, target.Center);

            GeneralParticleHandler.SpawnParticle(new StrongBloom(
                target.Center,
                Vector2.Zero,
                CosmicDischargeCommon.FrostCoreColor * 0.4f,
                0.55f,
                24));

            GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(
                target.Center,
                Vector2.Zero,
                Color.White * 0.32f,
                Vector2.One,
                0f,
                0.035f,
                0.22f,
                16));

            if (Main.myPlayer == Projectile.owner)
            {
                int shardCount = Main.rand.Next(5, 8);
                for (int i = 0; i < shardCount; i++)
                {
                    Vector2 velocity = (MathHelper.TwoPi * i / shardCount + Main.rand.NextFloat(-0.3f, 0.3f)).ToRotationVector2() * Main.rand.NextFloat(5f, 9f);
                    Projectile.NewProjectile(
                        Projectile.GetSource_FromThis(),
                        target.Center,
                        velocity,
                        ModContent.ProjectileType<CosmicDischargeEndothermicSplit>(),
                        (int)(Projectile.damage * 0.32f),
                        0f,
                        Projectile.owner);
                }
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Color drawColor = Color.Lerp(CosmicDischargeCommon.FrostDarkColor, CosmicDischargeCommon.FrostCoreColor, 0.75f);
            CosmicDischargeCommon.DrawChain(Main.spriteBatch, Owner.MountedCenter, Projectile.Center, drawColor, 1.06f, true, Owner.gfxOffY);
            CosmicDischargeCommon.DrawRightHoldIndicator(Main.spriteBatch, Owner, 1.35f);
            return false;
        }
    }
}
