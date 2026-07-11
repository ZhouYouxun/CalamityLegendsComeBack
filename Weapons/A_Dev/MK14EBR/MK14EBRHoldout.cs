using CalamityMod;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Dev.MK14EBR
{
    internal sealed class MK14EBRHoldout : ModProjectile, ILocalizedModType
    {
        private static readonly SoundStyle FireSound = new("CalamityLegendsComeBack/Sound/SHPC/M14开枪")
        {
            Volume = 0.78f,
            PitchVariance = 0.05f,
            MaxInstances = 6
        };

        private static readonly SoundStyle UniversalSuppressorFireSound = new("CalamityLegendsComeBack/Sound/Other/DeltaForce/M14消音")
        {
            Volume = 0.78f,
            PitchVariance = 0.05f,
            MaxInstances = 6
        };

        private static readonly SoundStyle HeavySuppressorFireSound = new("CalamityLegendsComeBack/Sound/Other/DeltaForce/M250消音")
        {
            Volume = 0.78f,
            PitchVariance = 0.05f,
            MaxInstances = 6
        };

        private static readonly SoundStyle GrenadeLauncherFireSound = new("CalamityLegendsComeBack/Sound/SHPC/解放者机甲左手火箭弹")
        {
            Volume = 0.72f,
            Pitch = -0.18f,
            PitchVariance = 0.04f,
            MaxInstances = 4
        };

        private static readonly SoundStyle DragonBreathFireSound = new("CalamityMod/Sounds/Item/DragonsBreathStrongStart")
        {
            Volume = 0.7f,
            Pitch = 0.12f,
            PitchVariance = 0.04f,
            MaxInstances = 4
        };

        private const float HoldoutForwardOffset = 64f;
        private const float HoldoutVerticalOffset = 9f;

        private float shotAccumulator;
        private int consecutiveShots;
        private int underbarrelCooldown;
        private int emptyClickCooldown;
        private int seenSpreadResetSignal;
        private float recoilOffset;

        public new string LocalizationCategory => "Projectiles.MK14EBR";
        public override string Texture => MK14TextureComposer.BodyPath;

        private Player Owner => Main.player[Projectile.owner];
        private NewLegendMK14EBR Weapon => Owner.HeldItem.ModItem as NewLegendMK14EBR;
        private bool HoldingM14 => Owner.HeldItem.ModItem is NewLegendM14;

        private Vector2 AimDirection => Projectile.velocity.SafeNormalize(Vector2.UnitX * Math.Max(Owner.direction, 1));

        private Vector2 GunTipPosition => Projectile.Center;

        public override void SetDefaults()
        {
            Projectile.width = 186;
            Projectile.height = 48;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.hide = false;
            Projectile.netImportant = true;
        }

        public override bool ShouldUpdatePosition() => false;

        public override bool? CanDamage() => false;

        public override void OnSpawn(IEntitySource source)
        {
            seenSpreadResetSignal = Owner.GetModPlayer<MK14EBRPlayer>().SpreadResetSignal;
        }

        public override void AI()
        {
            NewLegendMK14EBR weapon = Owner.HeldItem.ModItem as NewLegendMK14EBR;
            bool holdingMK14 = weapon != null;
            if (!Owner.active || Owner.dead || (!holdingMK14 && !HoldingM14))
            {
                Projectile.Kill();
                return;
            }

            if (holdingMK14)
                weapon.ClampLockedAttachments();

            Projectile.damage = Owner.GetWeaponDamage(Owner.HeldItem);
            Projectile.knockBack = Owner.HeldItem.knockBack;

            Owner.Calamity().mouseWorldListener = true;
            if (holdingMK14 && Main.myPlayer == Owner.whoAmI)
                Owner.Calamity().rightClickListener = true;

            ManageHoldout();

            if (underbarrelCooldown > 0)
                underbarrelCooldown--;

            if (emptyClickCooldown > 0)
                emptyClickCooldown--;

            MK14EBRPlayer mk14Player = Owner.GetModPlayer<MK14EBRPlayer>();
            if (mk14Player.SpreadResetSignal != seenSpreadResetSignal)
            {
                consecutiveShots = 0;
                seenSpreadResetSignal = mk14Player.SpreadResetSignal;
            }

            bool leftHeld = Main.myPlayer == Projectile.owner &&
                Main.mouseLeft &&
                NewLegendMK14EBR.CanUseWorldInput(Owner) &&
                Owner.ownedProjectileCounts[ModContent.ProjectileType<MK14ModificationPanel>()] <= 0;

            if (!leftHeld)
            {
                ResetFireState();
                return;
            }

            KeepWeaponUseAnimation();

            MK14RuntimeStats stats = holdingMK14
                ? MK14AttachmentDatabase.BuildStats(weapon, Owner)
                : MK14AttachmentDatabase.BuildM14Stats();
            shotAccumulator += Math.Max(0.01f, stats.Rpm / 3600f);

            int guard = 0;
            while (shotAccumulator >= 1f && guard < 3)
            {
                shotAccumulator -= 1f;
                guard++;

                if (!FireOnce(weapon, stats))
                {
                    shotAccumulator = 0f;
                    break;
                }
            }

            if (holdingMK14 && consecutiveShots > 0)
                TryFireUnderbarrel(weapon, stats);
        }

        private void ManageHoldout()
        {
            Vector2 aimWorld = NewLegendMK14EBR.GetMouseWorld(Owner);
            Vector2 aimDirection = (aimWorld - Owner.MountedCenter).SafeNormalize(Vector2.UnitX * Owner.direction);
            int direction = aimDirection.X >= 0f ? 1 : -1;

            recoilOffset = MathHelper.Lerp(recoilOffset, 0f, 0.32f);
            Projectile.velocity = aimDirection;
            Projectile.rotation = aimDirection.ToRotation();
            Projectile.spriteDirection = direction;
            Projectile.Center = Owner.MountedCenter + aimDirection * (HoldoutForwardOffset - recoilOffset) + new Vector2(0f, HoldoutVerticalOffset * Owner.gravDir);

            Owner.ChangeDir(direction);
            Owner.heldProj = Projectile.whoAmI;
            Owner.itemRotation = (Projectile.velocity * Projectile.direction).ToRotation();
            Owner.HeldItem.noUseGraphic = true;

            float armRotation = (Projectile.rotation - MathHelper.PiOver2) * Owner.gravDir +
                (Owner.gravDir == -1f ? MathHelper.Pi : 0f);

            Owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, armRotation);
            Owner.SetCompositeArmBack(true, Player.CompositeArmStretchAmount.Full, armRotation + 0.08f * Owner.direction);

            Projectile.timeLeft = 2;
            if (Projectile.owner == Main.myPlayer)
                Projectile.netUpdate = true;
        }

        private void KeepWeaponUseAnimation()
        {
            Owner.itemTime = Owner.itemAnimation = 2;
        }

        private void ResetFireState()
        {
            consecutiveShots = 0;
            shotAccumulator = 0f;
        }

        private bool FireOnce(NewLegendMK14EBR weapon, MK14RuntimeStats stats)
        {
            if (!Owner.PickAmmo(Owner.HeldItem, out int pickedProjectileType, out float shootSpeed, out int damage, out float knockback, out int ammoItemType))
            {
                if (emptyClickCooldown <= 0)
                {
                    SoundEngine.PlaySound(SoundID.MenuTick with { Volume = 0.25f, Pitch = -0.3f }, Owner.Center);
                    emptyClickCooldown = 20;
                }

                return false;
            }

            consecutiveShots++;

            Vector2 direction = AimDirection;
            float spreadDegrees = MK14AttachmentDatabase.ComputeSpreadDegrees(stats, consecutiveShots);
            Vector2 velocity = direction.RotatedByRandom(MathHelper.ToRadians(spreadDegrees)) *
                Math.Max(4f, shootSpeed * stats.ProjectileSpeedMultiplier);
            float sustainedDamageMultiplier = MK14AttachmentDatabase.ComputeSustainedFireDamageMultiplier(stats, consecutiveShots);
            damage = Math.Max(1, (int)Math.Round(damage * sustainedDamageMultiplier));
            int projectileType = weapon != null && ammoItemType == ItemID.MusketBall
                ? ModContent.ProjectileType<M61Bullet>()
                : pickedProjectileType;

            int projectileIndex = Projectile.NewProjectile(
                Projectile.GetSource_FromThis(),
                Owner.MountedCenter + Main.rand.NextVector2Circular(1.5f, 1.5f),
                velocity,
                projectileType,
                damage,
                knockback * stats.KnockbackMultiplier,
                Projectile.owner);

            if (Main.projectile.IndexInRange(projectileIndex))
            {
                Projectile shot = Main.projectile[projectileIndex];
                shot.DamageType = DamageClass.Ranged;
                shot.CritChance = Owner.GetWeaponCrit(Owner.HeldItem);
                ApplyProjectileScale(shot, stats.ProjectileScaleMultiplier);
                shot.GetGlobalProjectile<MK14GlobalProjectile>().Configure(
                    shot,
                    weapon,
                    stats,
                    spreadDegrees);
            }

            recoilOffset = weapon?.Barrel == MK14Barrel.SniperHeavy ? 10f : 5f;
            Owner.velocity -= direction * (weapon?.Barrel == MK14Barrel.SniperHeavy ? 0.42f : 0.18f);

            if (Main.myPlayer == Owner.whoAmI && weapon?.Barrel == MK14Barrel.SniperHeavy)
            {
                Owner.Calamity().GeneralScreenShakePower = Math.Max(Owner.Calamity().GeneralScreenShakePower, 1.1f);
            }

            SpawnMuzzleDust(direction);
            SoundEngine.PlaySound(GetFireSound(weapon), GunTipPosition);
            return true;
        }

        private static void ApplyProjectileScale(Projectile projectile, float scaleMultiplier)
        {
            if (Math.Abs(scaleMultiplier - 1f) <= 0.01f)
                return;

            Vector2 center = projectile.Center;
            projectile.scale *= scaleMultiplier;
            projectile.width = Math.Max(1, (int)Math.Round(projectile.width * scaleMultiplier));
            projectile.height = Math.Max(1, (int)Math.Round(projectile.height * scaleMultiplier));
            projectile.Center = center;
        }

        private static SoundStyle GetFireSound(NewLegendMK14EBR weapon)
        {
            return weapon?.Muzzle switch
            {
                MK14Muzzle.UniversalSuppressor => UniversalSuppressorFireSound,
                MK14Muzzle.HeavySuppressor => HeavySuppressorFireSound,
                _ => FireSound
            };
        }

        private void TryFireUnderbarrel(NewLegendMK14EBR weapon, MK14RuntimeStats stats)
        {
            if (underbarrelCooldown > 0)
                return;

            switch (weapon.Underbarrel)
            {
                case MK14Underbarrel.GrenadeLauncher:
                    FireGrenade(stats);
                    underbarrelCooldown = ApplyUnderbarrelCooldown(weapon, 76);
                    break;
                case MK14Underbarrel.DragonBreathShotgun:
                    FireDragonBreath(stats);
                    underbarrelCooldown = ApplyUnderbarrelCooldown(weapon, 48);
                    break;
            }
        }

        private static int ApplyUnderbarrelCooldown(NewLegendMK14EBR weapon, int baseCooldown)
        {
            return weapon.Stock == MK14Stock.Skeleton
                ? Math.Max(1, (int)Math.Round(baseCooldown * 0.7f))
                : baseCooldown;
        }

        private void FireGrenade(MK14RuntimeStats stats)
        {
            Vector2 direction = AimDirection;
            int damage = Math.Max(1, (int)(Projectile.damage * 0.92f));
            int grenadeIndex = Projectile.NewProjectile(
                Projectile.GetSource_FromThis(),
                GunTipPosition - direction * 10f + new Vector2(0f, 8f * Owner.gravDir),
                direction * 12f + new Vector2(0f, -1.2f),
                ModContent.ProjectileType<MK14GrenadeProjectile>(),
                damage,
                Projectile.knockBack * 1.25f,
                Projectile.owner);

            if (Main.projectile.IndexInRange(grenadeIndex))
                Main.projectile[grenadeIndex].CritChance = Owner.GetWeaponCrit(Owner.HeldItem);

            SoundEngine.PlaySound(GrenadeLauncherFireSound, GunTipPosition);
        }

        private void FireDragonBreath(MK14RuntimeStats stats)
        {
            Vector2 direction = AimDirection;
            int damage = Math.Max(1, (int)(Projectile.damage * 0.38f));
            for (int i = 0; i < 3; i++)
            {
                Vector2 velocity = direction.RotatedBy(MathHelper.ToRadians(MathHelper.Lerp(-7f, 7f, i / 2f))) * Main.rand.NextFloat(27f, 33f);
                int pelletIndex = Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    GunTipPosition - direction * 8f,
                    velocity,
                    ModContent.ProjectileType<MK14DragonBreathPellet>(),
                    damage,
                    Projectile.knockBack * 0.55f,
                    Projectile.owner);

                if (Main.projectile.IndexInRange(pelletIndex))
                    Main.projectile[pelletIndex].CritChance = Owner.GetWeaponCrit(Owner.HeldItem);
            }

            SoundEngine.PlaySound(DragonBreathFireSound, GunTipPosition);
        }

        private bool HasCloseTarget(float range)
        {
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];
                if (!npc.CanBeChasedBy(this))
                    continue;

                if (Vector2.DistanceSquared(npc.Center, Owner.Center) <= range * range)
                    return true;
            }

            return false;
        }

        private void SpawnMuzzleDust(Vector2 direction)
        {
            for (int i = 0; i < 5; i++)
            {
                Dust spark = Dust.NewDustPerfect(
                    GunTipPosition,
                    DustID.Smoke,
                    -direction.RotatedByRandom(0.45f) * Main.rand.NextFloat(0.8f, 2.2f),
                    120,
                    Color.Lerp(Color.Gray, Color.Orange, Main.rand.NextFloat(0.15f, 0.35f)),
                    Main.rand.NextFloat(0.55f, 0.9f));

                spark.noGravity = true;
            }

            Dust flash = Dust.NewDustPerfect(
                GunTipPosition + direction * 4f,
                DustID.Torch,
                direction * Main.rand.NextFloat(1.5f, 3.5f),
                80,
                Color.Gold,
                1.05f);
            flash.noGravity = true;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            NewLegendMK14EBR weapon = Weapon;
            if (weapon == null && !HoldingM14)
                return false;

            SpriteEffects effects = Projectile.spriteDirection == -1 ? SpriteEffects.FlipVertically : SpriteEffects.None;
            if (weapon != null)
            {
                MK14TextureComposer.DrawComposite(
                    Main.spriteBatch,
                    weapon,
                    Projectile.Center - Main.screenPosition,
                    lightColor,
                    Projectile.rotation,
                    Projectile.scale,
                    effects);
            }
            else
            {
                Texture2D texture = ModContent.Request<Texture2D>("CalamityLegendsComeBack/Weapons/A_Dev/MK14EBR/M14/m14").Value;
                Main.EntitySpriteDraw(
                    texture,
                    Projectile.Center - Main.screenPosition,
                    null,
                    lightColor,
                    Projectile.rotation,
                    texture.Size() * 0.5f,
                    Projectile.scale,
                    effects,
                    0);
            }

            return false;
        }
    }
}
