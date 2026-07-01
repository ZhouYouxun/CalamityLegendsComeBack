using System;
using System.Collections.Generic;
using System.IO;
using CalamityLegendsComeBack.Weapons.YharimsCrystal.Passive;
using CalamityMod;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.YharimsCrystal.RightGeneral
{
    // Six-unit air-defense battery: slots 0-1 are forward tracking-rocket interceptors,
    // slots 2-3 are mid-line penetrator autocannons, slots 4-5 are rear precision zap lasers.
    // Every slot shares the same heavy-attack ordnance (twin gold bombs).
    internal sealed class YC_RightDrone : ModProjectile, ILocalizedModType
    {
        private enum DroneRole { Rocket, Penetrator, Beam }

        private static readonly Color DroneGold = new(255, 218, 88);
        private static readonly Color DroneOrange = new(255, 104, 36);

        public new string LocalizationCategory => "Projectiles.YharimsCrystal";
        public override string Texture => "CalamityLegendsComeBack/Weapons/YharimsCrystal/YC_Right_Drone";

        private const int ShutdownFrames = 120;
        private const float LungeDurationFrames = 16f;
        private const float LungeLightDistance = 95f;
        private const float LungeHeavyDistance = 210f;

        public int SlotIndex => (int)Projectile.ai[0];
        public int ParentHoldoutIndex => (int)Projectile.ai[1];
        public bool HeavyCommanded => Projectile.ai[2] == 1f;

        private DroneRole Role => SlotIndex switch
        {
            0 or 1 => DroneRole.Rocket,
            2 or 3 => DroneRole.Penetrator,
            _ => DroneRole.Beam,
        };

        private float FiringTime => Role switch
        {
            DroneRole.Rocket => 26f,
            DroneRole.Penetrator => 13f,
            _ => 20f,
        };

        // On firing, the drone darts toward the target and back — an attack lunge,
        // not a recoil kick. Counts down from LungeDurationFrames to 0.
        private ref float LungeTimer => ref Projectile.localAI[0];
        private ref float ShootingTimer => ref Projectile.localAI[1];
        private float lungeDistance;

        private Player Owner;
        private int time;
        private int firingDelay = 15;
        private float postFireCooldown;

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 9;
            ProjectileID.Sets.TrailingMode[Type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 142;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.netImportant = true;
            Projectile.hide = true;
            Projectile.friendly = false;
            Projectile.hostile = false;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 2;
        }

        public override void OnSpawn(IEntitySource source)
        {
            LungeTimer = 0f;
        }

        public override bool ShouldUpdatePosition() => false;

        public override bool? CanDamage() => false;

        public override void AI()
        {
            Owner ??= Main.player[Projectile.owner];
            if (!Owner.active || Owner.dead || Owner.HeldItem.type != ModContent.ItemType<NewLegendYharimsCrystal>())
            {
                Projectile.Kill();
                return;
            }

            bool hasHoldout = TryGetHoldout(out Projectile holdoutProj, out YC_RightCrystalHoldout holdout);
            if (!hasHoldout && postFireCooldown <= 0f && time > 1)
            {
                Projectile.Kill();
                return;
            }

            Projectile.timeLeft = 2;
            Projectile.damage = hasHoldout ? holdoutProj.damage : Owner.GetWeaponDamage(Owner.HeldItem);
            Lighting.AddLight(Projectile.Center, DroneGold.ToVector3() * 0.22f);

            if (HeavyCommanded)
                TriggerHeavyAttack();

            firingDelay--;
            if (postFireCooldown > 0f)
                PostFiringCooldown();
            else if (firingDelay <= 0 && ShootingTimer >= FiringTime)
            {
                if (Projectile.owner == Main.myPlayer && Owner.CheckMana(Owner.HeldItem, -1, false, false))
                    Shoot(false);
                ShootingTimer = 0f;
            }

            Vector2 ownerToMouse = NewLegendYharimsCrystal.GetMouseWorld(Owner) - Owner.MountedCenter;
            UpdateFormationPosition(ownerToMouse);

            if (LungeTimer > 0f)
                LungeTimer--;

            ShootingTimer++;
            time++;
            Projectile.soundDelay--;
            Projectile.ForceNetUpdate();
        }

        // Lays the six drones out like an air-defense battery: rockets forward and narrow,
        // autocannons mid-line, zap lasers held back and spread wide. Pulled in much
        // tighter/closer to the player than the original layout, and shifted rearward
        // overall so the rear pair sits just behind the player instead of out front.
        private static (float along, float perp) GetFormationOffset(int slot) => slot switch
        {
            0 => (65f, -45f),
            1 => (65f, 45f),
            2 => (20f, -100f),
            3 => (20f, 100f),
            4 => (-25f, -155f),
            5 => (-25f, 155f),
            _ => (30f, 0f),
        };

        private void UpdateFormationPosition(Vector2 ownerToMouse)
        {
            Vector2 aimDir = ownerToMouse.SafeNormalize(Vector2.UnitX * Owner.direction);
            int direction = Math.Sign(ownerToMouse.X);
            if (direction == 0)
                direction = Owner.direction;

            (float along, float perp) = GetFormationOffset(SlotIndex);
            Vector2 perpDir = aimDir.RotatedBy(MathHelper.PiOver2);
            float bob = MathF.Sin(time * 0.045f + SlotIndex * 1.7f) * 14f;

            Vector2 formationCenter = Owner.MountedCenter + aimDir * along + perpDir * (perp + bob);

            // Attack lunge: darts toward the target's direction on firing, then eases back —
            // a bell curve so it's a genuine there-and-back dart, not a linear snap.
            float lungeProgress = MathHelper.Clamp(1f - LungeTimer / LungeDurationFrames, 0f, 1f);
            float lungeBell = LungeTimer > 0f ? MathF.Sin(lungeProgress * MathHelper.Pi) : 0f;
            Vector2 lungeOffset = aimDir * lungeDistance * lungeBell;

            Projectile.Center = formationCenter + lungeOffset;
            Projectile.velocity = Projectile.velocity.ToRotation().AngleTowards(aimDir.ToRotation(), 0.2f).ToRotationVector2();
            Projectile.rotation = (NewLegendYharimsCrystal.GetMouseWorld(Owner) - Projectile.Center)
                .SafeNormalize(Vector2.UnitX).ToRotation();
            Projectile.spriteDirection = Projectile.direction = direction;
        }

        private void TriggerHeavyAttack()
        {
            Projectile.ai[2] = 0f;
            postFireCooldown = ShutdownFrames;
            ShootingTimer = 0f;
            firingDelay = 15;
            Shoot(true);
            Projectile.netUpdate = true;
        }

        private void Shoot(bool isGrenade)
        {
            Vector2 shootDirection = (NewLegendYharimsCrystal.GetMouseWorld(Owner) - Projectile.Center).SafeNormalize(Vector2.UnitX);
            Vector2 tipPosition = Projectile.Center + Projectile.velocity.SafeNormalize(Vector2.Zero)
                .RotatedBy(-0.05f * Projectile.direction) * 12f;

            if (isGrenade)
            {
                FireHeavyBombs(shootDirection, tipPosition);
            }
            else
            {
                switch (Role)
                {
                    case DroneRole.Rocket:
                        FireRocket(shootDirection, tipPosition);
                        break;
                    case DroneRole.Penetrator:
                        FirePenetratorStream(shootDirection, tipPosition);
                        break;
                    default:
                        FireZapBeam(shootDirection, tipPosition);
                        break;
                }
            }

            if (Main.dedServ)
                return;

            for (int k = 0; k < 6; k++)
            {
                Vector2 shootVel = (shootDirection * 10f).RotatedByRandom(0.4f) * Main.rand.NextFloat(0.1f, 1.6f);
                Dust dust = Dust.NewDustPerfect(tipPosition, Main.rand.NextBool(4) ? 264 : DustID.GoldFlame, shootVel);
                dust.scale = Main.rand.NextFloat(1.1f, 1.4f);
                dust.noGravity = true;
                dust.color = Main.rand.NextBool() ? Color.Lerp(DroneGold, Color.White, 0.45f) : DroneOrange;
            }

            GeneralParticleHandler.SpawnParticle(new GlowSparkParticle(tipPosition - shootDirection * 14f,
                shootDirection * 20f, false, Main.rand.Next(7, 12), 0.035f,
                DroneGold, new Vector2(1.5f, 0.9f), true));

            LungeTimer = LungeDurationFrames;
            lungeDistance = isGrenade ? LungeHeavyDistance : LungeLightDistance;
        }

        private void FireRocket(Vector2 direction, Vector2 origin)
        {
            SoundEngine.PlaySound(SoundID.Item38 with { Volume = 0.4f, Pitch = -0.15f }, Projectile.Center);

            if (Main.myPlayer != Projectile.owner)
                return;

            int rocket = Projectile.NewProjectile(Projectile.GetSource_FromThis(), origin, direction * 9f,
                ModContent.ProjectileType<YC_TrackingRocket>(), Projectile.damage, Projectile.knockBack, Projectile.owner);
            if (Main.projectile.IndexInRange(rocket))
            {
                YharimsCrystalHellBladeGlobalProjectile.Mark(Main.projectile[rocket], YCWeaponForm.Crystal);
                Main.projectile[rocket].CritChance = Projectile.CritChance;
            }
        }

        // Three rounds at descending speed instead of a symmetric instant fan — they string
        // out into a stuttering stream rather than arriving as one synchronized volley.
        private void FirePenetratorStream(Vector2 direction, Vector2 origin)
        {
            SoundEngine.PlaySound(new SoundStyle("CalamityMod/Sounds/Item/MagnaCannonShot")
            { Volume = 0.25f, Pitch = 1f, PitchVariance = 0.35f }, Projectile.Center);

            if (Main.myPlayer != Projectile.owner)
                return;

            for (int i = 0; i < 3; i++)
            {
                float speed = MathHelper.Lerp(17f, 10f, i / 2f);
                float curve = MathHelper.ToRadians(i - 1) * 2.2f;
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), origin, (direction * speed).RotatedBy(curve),
                    ModContent.ProjectileType<YC_DroneShot>(), Projectile.damage, Projectile.knockBack, Projectile.owner);
            }
        }

        private void FireZapBeam(Vector2 direction, Vector2 origin)
        {
            SoundEngine.PlaySound(new SoundStyle("CalamityMod/Sounds/Item/PlasmaBolt") { Volume = 0.4f, Pitch = 0.15f }, Projectile.Center);

            if (Main.myPlayer != Projectile.owner)
                return;

            float crossfireAngle = MathHelper.ToRadians(SlotIndex == 4 ? -3f : 3f);
            int beam = Projectile.NewProjectile(Projectile.GetSource_FromThis(), origin, direction.RotatedBy(crossfireAngle),
                ModContent.ProjectileType<YC_DroneZapBeam>(), Projectile.damage, Projectile.knockBack, Projectile.owner);
            if (Main.projectile.IndexInRange(beam))
            {
                YharimsCrystalHellBladeGlobalProjectile.Mark(Main.projectile[beam], YCWeaponForm.Crystal);
                Main.projectile[beam].CritChance = Projectile.CritChance;
            }
        }

        private void FireHeavyBombs(Vector2 direction, Vector2 origin)
        {
            SoundEngine.PlaySound(new SoundStyle("CalamityMod/Sounds/Item/DeadSunExplosion")
            { Volume = 0.48f, Pitch = -0.45f, PitchVariance = 0.12f }, Projectile.Center);
            Owner.Calamity().GeneralScreenShakePower = Math.Max(Owner.Calamity().GeneralScreenShakePower, 4.8f);

            if (!Main.dedServ)
            {
                GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(origin, Vector2.Zero, DroneGold, Vector2.One, direction.ToRotation(), 0.12f, 2.2f, 18));
                for (int i = 0; i < 10; i++)
                {
                    Vector2 blastVel = -direction.RotatedByRandom(0.5f) * Main.rand.NextFloat(4f, 12f);
                    Dust dust = Dust.NewDustPerfect(origin, DustID.GoldFlame, blastVel, 0, Main.rand.NextBool(3) ? Color.White : DroneOrange, Main.rand.NextFloat(1.15f, 1.8f));
                    dust.noGravity = true;
                }
            }

            if (Main.myPlayer != Projectile.owner)
                return;

            Vector2 firingVelocity = direction * 12.5f;

            Projectile bomb = Projectile.NewProjectileDirect(Projectile.GetSource_FromThis(), origin, firingVelocity.RotatedBy(MathHelper.ToRadians(-2.5f)),
                ModContent.ProjectileType<YC_GoldBomb>(), Projectile.damage * 18, Projectile.knockBack * 7.5f, Projectile.owner);
            bomb.timeLeft = 420;
            bomb.scale = 1.18f;
            bomb.CritChance = Projectile.CritChance;
            YharimsCrystalHellBladeGlobalProjectile.Mark(bomb, YCWeaponForm.Crystal);

            int bomb2 = Projectile.NewProjectile(Projectile.GetSource_FromThis(), origin, (firingVelocity * 1.12f).RotatedBy(MathHelper.ToRadians(2.5f)),
                ModContent.ProjectileType<YC_GoldBomb>(), Projectile.damage * 18, Projectile.knockBack * 7.5f, Projectile.owner);
            if (Main.projectile.IndexInRange(bomb2))
            {
                Main.projectile[bomb2].scale = 1.18f;
                Main.projectile[bomb2].CritChance = Projectile.CritChance;
                YharimsCrystalHellBladeGlobalProjectile.Mark(Main.projectile[bomb2], YCWeaponForm.Crystal);
            }
        }

        private void PostFiringCooldown()
        {
            Owner.channel = true;
            Vector2 tipPosition = Projectile.Center + Projectile.velocity.SafeNormalize(Vector2.Zero)
                .RotatedBy(-0.05f * Projectile.direction) * 12f;

            if (!Main.dedServ && Main.rand.NextBool())
            {
                Vector2 smokeVel = new Vector2(0f, -8f) * Main.rand.NextFloat(0.1f, 1.1f);
                GeneralParticleHandler.SpawnParticle(new HeavySmokeParticle(tipPosition, smokeVel, Color.Lerp(DroneOrange, DroneGold, 0.45f),
                    Main.rand.Next(30, 51), Main.rand.NextFloat(0.1f, 0.4f), 0.5f,
                    Main.rand.NextFloat(-0.2f, 0.2f), Main.rand.NextBool(), required: true));

                Dust dust = Dust.NewDustPerfect(tipPosition, DustID.SteampunkSteam, smokeVel.RotatedByRandom(0.1f),
                    80, default, Main.rand.NextFloat(0.2f, 0.8f));
                dust.noGravity = false;
                dust.color = DroneGold;
            }

            ShootingTimer = 0f;
            firingDelay = 15;
            postFireCooldown--;
        }

        private bool TryGetHoldout(out Projectile holdoutProj, out YC_RightCrystalHoldout holdout)
        {
            holdoutProj = null;
            holdout = null;

            if (ParentHoldoutIndex < 0 || ParentHoldoutIndex >= Main.maxProjectiles)
                return false;

            Projectile candidate = Main.projectile[ParentHoldoutIndex];
            if (candidate.active &&
                candidate.owner == Projectile.owner &&
                candidate.type == ModContent.ProjectileType<YC_RightCrystalHoldout>() &&
                candidate.ModProjectile is YC_RightCrystalHoldout holdoutMod)
            {
                holdoutProj = candidate;
                holdout = holdoutMod;
                return true;
            }

            return false;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (time <= 0)
                return false;

            Texture2D texture = ModContent.Request<Texture2D>("CalamityLegendsComeBack/Weapons/YharimsCrystal/YC_Right_Drone").Value;
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;

            Vector2 drawPosition = Projectile.Center - Main.screenPosition + new Vector2(0f, Owner.gfxOffY);
            Color drawColor = Projectile.GetAlpha(lightColor);
            float drawRotation = Projectile.rotation + MathHelper.PiOver2;
            Vector2 rotationPoint = texture.Size() * 0.5f;
            SpriteEffects flipSprite = Projectile.spriteDirection == -1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;

            // Soft gold ambient glow behind the drone
            Main.EntitySpriteDraw(bloom, drawPosition, null, DroneGold with { A = 0 } * 0.18f, 0f, bloom.Size() * 0.5f, Projectile.scale * 0.72f, SpriteEffects.None);

            // Gold afterimage trail
            for (int i = 1; i < Projectile.oldPos.Length; i++)
            {
                Vector2 oldDrawPosition = Projectile.oldPos[i] + Projectile.Size * 0.5f - Main.screenPosition + new Vector2(0f, Owner.gfxOffY);
                Color trailColor = Color.Lerp(DroneOrange, DroneGold, 0.5f) with { A = 0 } * (0.18f * (Projectile.oldPos.Length - i) / Projectile.oldPos.Length);
                Main.EntitySpriteDraw(texture, oldDrawPosition, null, trailColor, Projectile.oldRot[i] + MathHelper.PiOver2, rotationPoint, Projectile.scale, flipSprite);
            }

            // Gold border outline (pulsing)
            float pulse = 0.82f + 0.18f * MathF.Sin(Main.GlobalTimeWrappedHourly * 5f);
            Color borderColor = DroneGold with { A = 0 };
            float borderRadius = 3.2f * pulse;
            for (int i = 0; i < 6; i++)
            {
                float angle = MathHelper.TwoPi * i / 6f;
                Vector2 offset = angle.ToRotationVector2() * borderRadius;
                Main.EntitySpriteDraw(texture, drawPosition + offset, null, borderColor * 0.38f, drawRotation, rotationPoint, Projectile.scale, flipSprite);
            }

            // Main body
            Main.EntitySpriteDraw(texture, drawPosition, null, drawColor, drawRotation, rotationPoint,
                Projectile.scale, flipSprite);

            return false;
        }

        public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs,
            List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI)
        {
            behindNPCs.Add(index);
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(Projectile.rotation);
            writer.Write(Projectile.spriteDirection);
            writer.Write(LungeTimer);
            writer.Write(lungeDistance);
            writer.Write(ShootingTimer);
            writer.Write(time);
            writer.Write(firingDelay);
            writer.Write(postFireCooldown);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            Projectile.rotation = reader.ReadSingle();
            Projectile.spriteDirection = reader.ReadInt32();
            LungeTimer = reader.ReadSingle();
            lungeDistance = reader.ReadSingle();
            ShootingTimer = reader.ReadSingle();
            time = reader.ReadInt32();
            firingDelay = reader.ReadInt32();
            postFireCooldown = reader.ReadSingle();
        }
    }
}
