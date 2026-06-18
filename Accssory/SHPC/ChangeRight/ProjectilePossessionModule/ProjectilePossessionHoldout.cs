using CalamityLegendsComeBack.Accssory.SHPC.Skill.CtrlChip;
using CalamityLegendsComeBack.Weapons.SHPC;
using CalamityMod;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Utilities;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Accssory.SHPC.ChangeRight.ProjectilePossessionModule
{
    internal sealed class ProjectilePossessionHoldout : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.SHPC";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private const int VacuumStartFrames = 78;
        private const int VacuumLoopFrames = 132;
        private const float VacuumRange = 576f;
        private const float VacuumSpread = MathHelper.Pi / 6.66f;
        private const int AbsorptionsPerFrame = 3;
        private static readonly Vector2 DrawOffset = new(27f, -10f);

        private SlotId vacuumSound;
        private bool playedEndSound;
        private bool released;
        private int fullPulseCooldown;

        private Player Owner => Main.player[Projectile.owner];
        private ref float Timer => ref Projectile.localAI[0];

        private Vector2 AimWorld => CtrlChipPlayer.GetAimWorld(Owner, Owner.Calamity().mouseWorld);
        private Vector2 AimDirection => Owner.DirectionTo(AimWorld).SafeNormalize(Vector2.UnitX * Owner.direction);
        private Vector2 TipPosition => Projectile.Center + Vector2.UnitX.RotatedBy(Projectile.rotation) * 62f + Vector2.UnitY * DrawOffset.Y;

        public override void SetDefaults()
        {
            Projectile.width = 70;
            Projectile.height = 70;
            Projectile.friendly = true;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 2;
        }

        public override bool? CanDamage() => false;
        public override bool ShouldUpdatePosition() => false;

        public override void AI()
        {
            if (!CanContinueHolding())
            {
                ReleaseAndKill();
                return;
            }

            if (Timer == 0f)
                vacuumSound = SoundEngine.PlaySound(NewLegendSHPC.VacuumStart, Owner.Center);

            if (SoundEngine.TryGetActiveSound(vacuumSound, out ActiveSound activeSound) && activeSound.IsPlaying)
                activeSound.Position = Owner.Center;

            Timer++;
            Projectile.timeLeft = 2;
            Projectile.Center = Owner.Center;
            Projectile.rotation = AimDirection.ToRotation();
            Projectile.velocity = Vector2.Zero;

            UpdateOwnerHoldout();

            bool wantsRelease = !Owner.Calamity().mouseRight ||
                (Projectile.owner == Main.myPlayer && !NewLegendSHPC.CanUseWorldRightClick(Owner));
            if (wantsRelease)
            {
                ReleaseAndKill();
                return;
            }

            ProjectilePossessionModulePlayer possessionPlayer = Owner.GetModPlayer<ProjectilePossessionModulePlayer>();
            possessionPlayer.RefreshAbsorbedCount();

            if (possessionPlayer.AbsorbedProjectileCount >= ProjectilePossessionModulePlayer.MaxAbsorbedProjectiles)
            {
                PulseFullBar(possessionPlayer);
            }
            else
            {
                TryAbsorbProjectiles(possessionPlayer);
            }

            SpawnVacuumDust();

            if ((Timer - VacuumStartFrames) % VacuumLoopFrames == 0f)
                vacuumSound = SoundEngine.PlaySound(NewLegendSHPC.VacuumLoop, Owner.Center);
        }

        public override void OnKill(int timeLeft)
        {
            if (SoundEngine.TryGetActiveSound(vacuumSound, out ActiveSound activeSound))
                activeSound?.Stop();

            if (!released && Owner.active && !Owner.dead)
                ReleaseCapturedProjectiles();
        }

        private bool CanContinueHolding()
        {
            if (!Owner.active || Owner.dead || Owner.CCed || Owner.noItems)
                return false;

            if (Owner.HeldItem.type != ModContent.ItemType<NewLegendSHPC>())
                return false;

            return Owner.GetModPlayer<ProjectilePossessionModulePlayer>().ProjectilePossessionModuleEquipped;
        }

        private void UpdateOwnerHoldout()
        {
            Owner.heldProj = Projectile.whoAmI;
            Owner.ChangeDir(Math.Sign((AimWorld - Owner.Center).X));
            if (Owner.direction == 0)
                Owner.ChangeDir(1);

            Owner.SetCompositeArmFront(
                true,
                Player.CompositeArmStretchAmount.Full,
                (Owner.Center - AimWorld).ToRotation() * Owner.gravDir + MathHelper.PiOver2);
            Owner.SetDummyItemTime(2);
        }

        private void TryAbsorbProjectiles(ProjectilePossessionModulePlayer possessionPlayer)
        {
            int absorbedThisFrame = 0;
            int count = possessionPlayer.AbsorbedProjectileCount;

            foreach (Projectile projectile in Main.ActiveProjectiles)
            {
                if (absorbedThisFrame >= AbsorptionsPerFrame ||
                    count >= ProjectilePossessionModulePlayer.MaxAbsorbedProjectiles)
                    break;

                if (!SHPCProjectilePossessionGlobalProjectile.CanBePossessed(projectile))
                    continue;

                if (!IsInsideVacuum(projectile))
                    continue;

                if (!SHPCProjectilePossessionGlobalProjectile.TryCreatePossessedClone(projectile, Owner, count, out Projectile possessedClone))
                    continue;

                absorbedThisFrame++;
                count++;
                possessionPlayer.AbsorbedProjectileCount = count;
                possessionPlayer.TriggerPossessionBarPulse(18);
                SpawnAbsorbBurst(projectile.Center);
                SoundEngine.PlaySound(SoundID.Item72 with { Volume = 0.45f, Pitch = 0.25f }, possessedClone.Center);
            }
        }

        private bool IsInsideVacuum(Projectile projectile)
        {
            Rectangle safetyHitbox = new((int)TipPosition.X - Projectile.width / 2, (int)TipPosition.Y - Projectile.height / 2, Projectile.width, Projectile.height);
            if (projectile.Hitbox.Intersects(safetyHitbox))
                return true;

            Vector2 toProjectile = projectile.Center - TipPosition;
            if (toProjectile.LengthSquared() > VacuumRange * VacuumRange)
                return false;

            float angleOffset = MathHelper.WrapAngle(toProjectile.ToRotation() - AimDirection.ToRotation());
            return Math.Abs(angleOffset) <= VacuumSpread;
        }

        private void ReleaseAndKill()
        {
            if (!released)
                ReleaseCapturedProjectiles();

            Projectile.Kill();
        }

        private void ReleaseCapturedProjectiles()
        {
            released = true;
            StopVacuumSoundAndPlayEnd();

            List<Projectile> capturedProjectiles = SHPCProjectilePossessionGlobalProjectile.GetPossessedProjectiles(Projectile.owner);
            if (capturedProjectiles.Count <= 0)
                return;

            Vector2 releaseDirection = AimDirection;
            int baseDamage = Math.Max(1, Projectile.damage);
            float baseSpeed = 20f;
            int count = capturedProjectiles.Count;
            float spread = MathHelper.Lerp(MathHelper.ToRadians(2f), MathHelper.ToRadians(28f), count / (float)ProjectilePossessionModulePlayer.MaxAbsorbedProjectiles);

            for (int i = 0; i < count; i++)
            {
                Projectile projectile = capturedProjectiles[i];
                SHPCProjectilePossessionGlobalProjectile possession = projectile.GetGlobalProjectile<SHPCProjectilePossessionGlobalProjectile>();

                float progress = count <= 1 ? 0.5f : i / (float)(count - 1);
                float angle = MathHelper.Lerp(-spread, spread, progress);
                float speed = MathHelper.Clamp(possession.OriginalSpeed * 1.25f, 12f, 30f);
                if (speed <= 0f)
                    speed = baseSpeed;

                Vector2 velocity = releaseDirection.RotatedBy(angle) * speed;
                projectile.Center = TipPosition + releaseDirection * 16f + releaseDirection.RotatedBy(MathHelper.PiOver2) * MathHelper.Lerp(-18f, 18f, progress);
                possession.Release(projectile, Owner, velocity, Math.Max(baseDamage, possession.OriginalDamage));
            }

            Owner.GetModPlayer<ProjectilePossessionModulePlayer>().RefreshAbsorbedCount();
            SoundEngine.PlaySound(SoundID.Item91 with { Volume = 0.9f, Pitch = -0.18f }, Owner.Center);
        }

        private void StopVacuumSoundAndPlayEnd()
        {
            if (playedEndSound)
                return;

            if (SoundEngine.TryGetActiveSound(vacuumSound, out ActiveSound activeSound))
                activeSound?.Stop();

            SoundEngine.PlaySound(NewLegendSHPC.VacuumEnd, Owner.Center);
            playedEndSound = true;
        }

        private void PulseFullBar(ProjectilePossessionModulePlayer possessionPlayer)
        {
            if (fullPulseCooldown > 0)
            {
                fullPulseCooldown--;
                return;
            }

            possessionPlayer.TriggerPossessionBarPulse(24);
            fullPulseCooldown = 30;
            SoundEngine.PlaySound(SoundID.MaxMana with { Volume = 0.35f, Pitch = 0.45f }, Owner.Center);
        }

        private void SpawnVacuumDust()
        {
            if (Main.dedServ || Main.rand.NextBool(2))
                return;

            Vector2 spawn = TipPosition + AimDirection.RotatedByRandom(VacuumSpread) * VacuumRange * Main.rand.NextFloat(0.35f, 1f);
            Vector2 velocity = spawn.DirectionTo(TipPosition) * Main.rand.NextFloat(3.2f, 6f);
            Dust dust = Dust.NewDustPerfect(spawn, DustID.Electric, velocity, 140, new Color(100, 220, 255), Main.rand.NextFloat(0.65f, 1.05f));
            dust.noGravity = true;
        }

        private void SpawnAbsorbBurst(Vector2 center)
        {
            if (Main.dedServ)
                return;

            for (int i = 0; i < 10; i++)
            {
                Vector2 velocity = center.DirectionTo(TipPosition).RotatedByRandom(0.35f) * Main.rand.NextFloat(2f, 6f);
                Dust dust = Dust.NewDustPerfect(center, DustID.BlueTorch, velocity, 120, new Color(110, 235, 255), Main.rand.NextFloat(0.75f, 1.25f));
                dust.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Vector2 farthestPos = TipPosition + AimDirection * VacuumRange;
            float rotation = AimDirection.ToRotation();

            Texture2D smoke = ModContent.Request<Texture2D>("CalamityMod/Particles/MediumMist").Value;
            Rectangle frame = smoke.Frame(1, 3, 0, (int)(Main.GameUpdateCount / 4 % 3));

            Main.spriteBatch.SetBlendState(BlendState.Additive);
            for (int i = 0; i < 6; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    float distRatio = 1f - (Main.GameUpdateCount + j * 10) % 30 / 30f;
                    Vector2 posOffset = new Vector2(
                        MathF.Sin(MathHelper.TwoPi / 6f * i) * 25f * distRatio,
                        MathF.Cos(Main.GameUpdateCount * MathHelper.Pi / 30f + MathHelper.TwoPi / 6f * i) * 240f * distRatio).RotatedBy(rotation);
                    float colorMult = 0.45f * Utils.GetLerpValue(1f, 0.8f, distRatio, true);

                    Main.EntitySpriteDraw(
                        smoke,
                        Vector2.Lerp(TipPosition, farthestPos, distRatio) + posOffset - Main.screenPosition,
                        frame,
                        new Color(102, 210, 255) * colorMult,
                        rotation + MathHelper.Pi,
                        frame.Size() / 2f,
                        1.35f * distRatio,
                        SpriteEffects.None);
                }
            }
            Main.spriteBatch.SetBlendState(BlendState.AlphaBlend);

            Texture2D shpc = TextureAssets.Item[ModContent.ItemType<NewLegendSHPC>()].Value;
            Vector2 position = Owner.Center - Main.screenPosition + Vector2.UnitX.RotatedBy(rotation) * DrawOffset.X + Vector2.UnitY * DrawOffset.Y;
            SpriteEffects effects = AimWorld.X < Owner.Center.X ? SpriteEffects.FlipVertically : SpriteEffects.None;
            Main.EntitySpriteDraw(shpc, position, null, lightColor, rotation, shpc.Size() / 2f, 1f, effects);

            return false;
        }
    }
}
