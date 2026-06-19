using CalamityMod;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Dev.PeaShooter
{
    internal sealed class PeaShooterASSHold : ModProjectile, ILocalizedModType
    {
        private const float DrawScale = 0.792f;
        private const float MuzzleDistance = 23f;

        private readonly BalancePeaShooter balance = new();
        private int burstCooldown;
        private int queuedPulseCount;
        private int queuedPulseTimer;
        private int queuedLineCount;
        private int adrenalineFireTimer;
        private int blinkTimer;

        public new string LocalizationCategory => "Projectiles.A_Dev";
        public override string Texture => NewLegendPeaShooter.TextureAssetPath;

        private Player Owner => Main.player[Projectile.owner];

        public override void SetDefaults()
        {
            Projectile.width = 56;
            Projectile.height = 28;
            Projectile.friendly = false;
            Projectile.hostile = false;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.netImportant = true;
        }

        public override bool ShouldUpdatePosition() => false;

        public override bool? CanDamage() => false;

        public override void DrawBehind(int index, System.Collections.Generic.List<int> behindNPCsAndTiles, System.Collections.Generic.List<int> behindNPCs, System.Collections.Generic.List<int> behindProjectiles, System.Collections.Generic.List<int> overPlayers, System.Collections.Generic.List<int> overWiresUI)
        {
            overPlayers.Add(index);
        }

        public override void AI()
        {
            PeaShooterPlayer peaPlayer = Owner.GetModPlayer<PeaShooterPlayer>();
            if (!Owner.active || Owner.dead || !peaPlayer.AccessoryEquipped)
            {
                Projectile.Kill();
                return;
            }

            Projectile.timeLeft = 2;
            Projectile.Center = GetHeadCenter();
            Projectile.velocity = Vector2.UnitX * Owner.direction;
            Projectile.spriteDirection = Owner.direction;
            Projectile.rotation = 0f;

            if (blinkTimer > 0)
                blinkTimer--;

            if (Main.myPlayer == Projectile.owner && !Owner.CCed && !Owner.noItems)
                HandleAutoFire();
        }

        private Vector2 GetHeadCenter()
        {
            Vector2 center = Owner.headPosition + Owner.MountedCenter;
            float verticalOffset = Owner.gravDir == 1f ? -9f : 9f;
            return center + new Vector2(Owner.direction * 2f, Owner.gfxOffY + verticalOffset);
        }

        private void HandleAutoFire()
        {
            if (Owner.Calamity().adrenalineModeActive)
            {
                HandleAdrenalineStorm();
                return;
            }

            adrenalineFireTimer = 0;

            if (queuedPulseCount > 0)
            {
                TryFireQueuedPulse();
                return;
            }

            if (burstCooldown > 0)
            {
                burstCooldown--;
                return;
            }

            QueueCurrentBurst();
            TryFireQueuedPulse();
            burstCooldown = BalancePeaShooter.BaseBurstInterval;
        }

        private void HandleAdrenalineStorm()
        {
            queuedPulseCount = 0;
            queuedPulseTimer = 0;
            burstCooldown = 0;

            if (adrenalineFireTimer > 0)
            {
                adrenalineFireTimer--;
                return;
            }

            FireAdrenalineStormVolley();
            adrenalineFireTimer = BalancePeaShooter.AdrenalineStormFireInterval - 1;
        }

        private void QueueCurrentBurst()
        {
            PeaShooterFireStyle style = BalancePeaShooter.GetFireStyle(balance.GetCompletedStageIndex());
            switch (style)
            {
                case PeaShooterFireStyle.Double:
                    queuedPulseCount = 2;
                    queuedLineCount = 1;
                    break;

                case PeaShooterFireStyle.ThreeLine:
                    queuedPulseCount = 1;
                    queuedLineCount = 3;
                    break;

                case PeaShooterFireStyle.Gatling:
                    queuedPulseCount = 4;
                    queuedLineCount = 1;
                    break;

                case PeaShooterFireStyle.WildGatling:
                    queuedPulseCount = 4;
                    queuedLineCount = 5;
                    break;

                default:
                    queuedPulseCount = 1;
                    queuedLineCount = 1;
                    break;
            }

            queuedPulseTimer = 0;
        }

        private void TryFireQueuedPulse()
        {
            if (queuedPulseTimer > 0)
            {
                queuedPulseTimer--;
                return;
            }

            FireRandomPeaPulse();
            queuedPulseCount--;
            queuedPulseTimer = BalancePeaShooter.BurstShotSpacing;
        }

        private void FireRandomPeaPulse()
        {
            int stageIndex = balance.GetCompletedStageIndex();
            int damage = GetAccessoryDamage();
            float speed = balance.GetShootSpeed();
            float startDegrees = -BalancePeaShooter.LineSpreadDegrees * (queuedLineCount - 1) * 0.5f;
            Vector2 aim = Vector2.UnitX * Owner.direction;
            Vector2 muzzle = Projectile.Center + aim * MuzzleDistance + new Vector2(0f, -2f * Owner.gravDir);

            for (int i = 0; i < queuedLineCount; i++)
            {
                PeaShooterPeaType peaType = (PeaShooterPeaType)Main.rand.Next((int)PeaShooterPeaType.Rock + 1);
                float angleDegrees = startDegrees + i * BalancePeaShooter.LineSpreadDegrees;
                Vector2 velocity = aim.RotatedBy(MathHelper.ToRadians(angleDegrees) + Main.rand.NextFloat(-0.012f, 0.012f)) * speed;
                Vector2 spawnPosition = muzzle + aim.RotatedBy(MathHelper.PiOver2) * (i - (queuedLineCount - 1) * 0.5f) * 3f;

                int peaIndex = Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    spawnPosition,
                    velocity,
                    ModContent.ProjectileType<PeaShooterPea>(),
                    damage,
                    Owner.HeldItem.knockBack,
                    Projectile.owner,
                    (float)peaType,
                    stageIndex);

                if (Main.projectile.IndexInRange(peaIndex))
                {
                    Projectile pea = Main.projectile[peaIndex];
                    pea.CritChance = Owner.GetWeaponCrit(Owner.HeldItem);
                    pea.originalDamage = pea.damage;
                    pea.netUpdate = true;
                }

                SpawnMuzzleDust(peaType, muzzle, aim);
            }

            blinkTimer = 8;
            SoundEngine.PlaySound(SoundID.Item17 with { Volume = 0.22f, Pitch = Main.rand.NextFloat(0.24f, 0.48f), MaxInstances = 8 }, muzzle);
        }

        private void FireAdrenalineStormVolley()
        {
            int stageIndex = balance.GetCompletedStageIndex();
            int damage = GetAccessoryDamage(BalancePeaShooter.AdrenalineStormDamageMultiplier);
            float speed = balance.GetShootSpeed();
            Vector2 aim = Vector2.UnitX * Owner.direction;
            Vector2 muzzle = Projectile.Center + aim * MuzzleDistance + new Vector2(0f, -2f * Owner.gravDir);
            int peaCount = Main.rand.Next(BalancePeaShooter.AdrenalineStormMinPeas, BalancePeaShooter.AdrenalineStormMaxPeas + 1);

            for (int i = 0; i < peaCount; i++)
            {
                PeaShooterPeaType peaType = (PeaShooterPeaType)Main.rand.Next((int)PeaShooterPeaType.Rock + 1);
                float angle = MathHelper.ToRadians(Main.rand.NextFloat(-BalancePeaShooter.AdrenalineStormSpreadDegrees, BalancePeaShooter.AdrenalineStormSpreadDegrees));
                float speedMultiplier = Main.rand.NextFloat(BalancePeaShooter.AdrenalineStormMinSpeedMultiplier, BalancePeaShooter.AdrenalineStormMaxSpeedMultiplier);
                Vector2 velocity = aim.RotatedBy(angle) * speed * speedMultiplier;
                Vector2 spawnPosition = muzzle + Main.rand.NextVector2Circular(6f, 5f) + aim * Main.rand.NextFloat(-2f, 5f);

                int peaIndex = Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    spawnPosition,
                    velocity,
                    ModContent.ProjectileType<PeaShooterPea>(),
                    damage,
                    Owner.HeldItem.knockBack * 0.35f,
                    Projectile.owner,
                    (float)peaType,
                    stageIndex);

                if (Main.projectile.IndexInRange(peaIndex))
                {
                    Projectile pea = Main.projectile[peaIndex];
                    pea.CritChance = Owner.GetWeaponCrit(Owner.HeldItem);
                    pea.originalDamage = pea.damage;
                    pea.scale = Main.rand.NextFloat(BalancePeaShooter.AdrenalineStormMinScale, BalancePeaShooter.AdrenalineStormMaxScale);
                    pea.netUpdate = true;
                }

                if (Main.rand.NextBool(2))
                    SpawnMuzzleDust(peaType, muzzle, aim);
            }

            blinkTimer = 12;
            SoundEngine.PlaySound(SoundID.Item17 with { Volume = 0.32f, Pitch = Main.rand.NextFloat(0.48f, 0.72f), MaxInstances = 12 }, muzzle);
        }

        private int GetAccessoryDamage(float damageMultiplier = BalancePeaShooter.AccessoryDamageMultiplier)
        {
            Item heldItem = Owner.HeldItem;
            int sourceDamage = heldItem.IsAir ? balance.GetBaseDamage() : Owner.GetWeaponDamage(heldItem);
            return Math.Max(1, (int)Math.Round(sourceDamage * damageMultiplier));
        }

        private static void SpawnMuzzleDust(PeaShooterPeaType peaType, Vector2 muzzle, Vector2 aim)
        {
            Dust dust = Dust.NewDustPerfect(
                muzzle + Main.rand.NextVector2Circular(3f, 3f),
                PeaShooterPea.GetDustType(peaType),
                aim.RotatedByRandom(0.36f) * Main.rand.NextFloat(0.6f, 1.8f),
                110,
                Color.Lerp(PeaShooterPea.GetPeaColor(peaType), Color.White, 0.2f),
                Main.rand.NextFloat(0.52f, 0.86f));
            dust.noGravity = true;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Type].Value;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            Vector2 origin = texture.Size() * 0.5f;
            SpriteEffects effects = Owner.direction < 0 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
            if (Owner.gravDir == -1f)
                effects |= SpriteEffects.FlipVertically;

            if (blinkTimer > 0)
            {
                float flash = blinkTimer / 8f;
                Color glow = (new Color(124, 255, 104) with { A = 0 }) * (0.18f + flash * 0.26f);
                for (int i = 0; i < 6; i++)
                {
                    Vector2 offset = (MathHelper.TwoPi * i / 6f).ToRotationVector2() * (1.1f + flash * 2f);
                    Main.EntitySpriteDraw(texture, drawPosition + offset, null, glow, 0f, origin, DrawScale, effects, 0);
                }
            }

            Main.EntitySpriteDraw(texture, drawPosition, null, Projectile.GetAlpha(lightColor), 0f, origin, DrawScale, effects, 0);
            return false;
        }
    }
}
