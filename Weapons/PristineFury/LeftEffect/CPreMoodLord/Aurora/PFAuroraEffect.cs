using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.PristineFury.LeftEffect
{
    internal static class PFAuroraEffect
    {
        internal static void Update(NewLegendPristineFuryHoldOut holdout, bool held, bool justPressed, bool justReleased)
        {
            if (!held)
            {
                KillExistingLaser(holdout);
                PristineFuryLeftEffectRegistry.Reset(holdout);
                return;
            }

            Vector2 muzzle = holdout.GunTipPosition + holdout.AimDirection * 14f;
            Vector2 toMouse = holdout.GetMouseWorld() - muzzle;
            Vector2 direction = toMouse.SafeNormalize(holdout.AimDirection);
            float length = MathHelper.Clamp(toMouse.Length(), 180f, 1900f);

            int laserType = ModContent.ProjectileType<PFAurora_Flame>();
            foreach (Projectile projectile in Main.ActiveProjectiles)
            {
                if (projectile.owner != holdout.Projectile.owner || projectile.type != laserType || (int)projectile.ai[1] != holdout.Projectile.whoAmI)
                    continue;

                projectile.Center = muzzle;
                projectile.velocity = direction;
                projectile.ai[0] = length;
                projectile.timeLeft = 2;
                projectile.netUpdate = true;
                PFLeftEffectRules.ApplyTheme(projectile.whoAmI, holdout.CurrentMark);
                EmitHoldEffects(holdout, muzzle, justPressed);
                return;
            }

            int laser = Projectile.NewProjectile(
                holdout.Projectile.GetSource_FromThis(),
                muzzle,
                direction,
                ModContent.ProjectileType<PFAurora_Flame>(),
                holdout.GetScaledDamage(2.65f),
                holdout.Projectile.knockBack,
                holdout.Projectile.owner,
                length,
                holdout.Projectile.whoAmI);
            PFLeftEffectRules.ApplyTheme(laser, holdout.CurrentMark);
            EmitHoldEffects(holdout, muzzle, true);
        }

        private static void EmitHoldEffects(NewLegendPristineFuryHoldOut holdout, Vector2 muzzle, bool justStarted)
        {
            holdout.ApplyRecoil(justStarted ? 7f : 0.55f);
            holdout.TriggerMuzzleFlash(4);
            if (holdout.LeftTimer++ % 8 == 0)
                holdout.SpawnMuzzleBurst(PristineFuryMarkHelper.GetColor(holdout.CurrentMark), 0.62f);

            if (justStarted)
                SoundEngine.PlaySound(SoundID.Item68 with { Volume = 0.72f, Pitch = 0.2f }, muzzle);
        }

        private static void KillExistingLaser(NewLegendPristineFuryHoldOut holdout)
        {
            int laserType = ModContent.ProjectileType<PFAurora_Flame>();
            foreach (Projectile projectile in Main.ActiveProjectiles)
            {
                if (projectile.owner == holdout.Projectile.owner && projectile.type == laserType && (int)projectile.ai[1] == holdout.Projectile.whoAmI)
                    projectile.Kill();
            }
        }
    }
}
