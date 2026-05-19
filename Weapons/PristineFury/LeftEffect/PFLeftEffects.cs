using CalamityLegendsComeBack.Weapons.PristineFury;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.PristineFury.LeftEffect
{
    internal static class PristineFuryLeftEffectRegistry
    {
        internal static void Update(PristineFuryMark mark, NewLegendPristineFuryHoldOut holdout, bool held, bool justPressed, bool justReleased)
        {
            switch (mark)
            {
                case PristineFuryMark.EvilT2:
                    PFEvilT2Effect.Update(holdout, held, justPressed, justReleased);
                    break;
                case PristineFuryMark.SlimeGod:
                    PFSlimeGodEffect.Update(holdout, held, justPressed, justReleased);
                    break;
                case PristineFuryMark.HardMode:
                    PFHardModeEffect.Update(holdout, held, justPressed, justReleased);
                    break;
                case PristineFuryMark.Prime:
                    PFPrimeEffect.Update(holdout, held, justPressed, justReleased);
                    break;
                case PristineFuryMark.BrimstoneElemental:
                    PFBrimstoneElementalEffect.Update(holdout, held, justPressed, justReleased);
                    break;
                case PristineFuryMark.Plantera:
                    PFPlanteraEffect.Update(holdout, held, justPressed, justReleased);
                    break;
                case PristineFuryMark.Aurora:
                    PFAuroraEffect.Update(holdout, held, justPressed, justReleased);
                    break;
                case PristineFuryMark.Goliath:
                    PFGoliathEffect.Update(holdout, held, justPressed, justReleased);
                    break;
                case PristineFuryMark.Moonlord:
                    PFMoonlordEffect.Update(holdout, held, justPressed, justReleased);
                    break;
                case PristineFuryMark.Providence:
                    PFProvidenceEffect.Update(holdout, held, justPressed, justReleased);
                    break;
                case PristineFuryMark.Polterghast:
                    PFPolterghastEffect.Update(holdout, held, justPressed, justReleased);
                    break;
                case PristineFuryMark.Dog:
                    PFDogEffect.Update(holdout, held, justPressed, justReleased);
                    break;
                case PristineFuryMark.Dragon:
                    PFDragonEffect.Update(holdout, held, justPressed, justReleased);
                    break;
                default:
                    PFIdleEffect.Update(holdout, held, justPressed, justReleased);
                    break;
            }
        }

        internal static void Reset(NewLegendPristineFuryHoldOut holdout)
        {
            holdout.LeftTimer = 0;
            holdout.LeftChargeTimer = 0;
            holdout.LeftAuxTimer = 0;
            holdout.LeftBurstIndex = 0;
        }
    }

    internal static class PFLeftEffectRules
    {
        internal static void FireSingle(
            NewLegendPristineFuryHoldOut holdout,
            int projectileType,
            float speed,
            float spreadRadians,
            float damageMultiplier,
            float recoil,
            int muzzleFlashFrames,
            Color muzzleColor,
            float muzzleScale,
            float forwardOffset = 14f,
            float ai0 = 0f)
        {
            Vector2 direction = holdout.AimDirection.RotatedBy(Main.rand.NextFloat(-spreadRadians, spreadRadians));
            Vector2 muzzle = holdout.GunTipPosition + direction * forwardOffset;

            Projectile.NewProjectile(
                holdout.Projectile.GetSource_FromThis(),
                muzzle,
                direction * speed,
                projectileType,
                holdout.GetScaledDamage(damageMultiplier),
                holdout.Projectile.knockBack,
                holdout.Projectile.owner,
                ai0,
                holdout.LeftBurstIndex++);

            holdout.ApplyRecoil(recoil);
            holdout.TriggerMuzzleFlash(muzzleFlashFrames);
            holdout.SpawnMuzzleBurst(muzzleColor, muzzleScale);
        }

        internal static void FireSpread(
            NewLegendPristineFuryHoldOut holdout,
            int projectileType,
            int count,
            float fanRadians,
            float speed,
            float speedStep,
            float damageMultiplier,
            float recoil,
            int muzzleFlashFrames,
            Color muzzleColor,
            float muzzleScale,
            float forwardOffset = 14f,
            float ai0Base = 0f)
        {
            Vector2 direction = holdout.AimDirection;
            Vector2 muzzle = holdout.GunTipPosition + direction * forwardOffset;

            for (int i = 0; i < count; i++)
            {
                float ratio = count == 1 ? 0.5f : i / (float)(count - 1);
                float spread = MathHelper.Lerp(-fanRadians, fanRadians, ratio);
                Vector2 velocity = direction.RotatedBy(spread) * (speed + speedStep * i);

                Projectile.NewProjectile(
                    holdout.Projectile.GetSource_FromThis(),
                    muzzle + direction * (i * 3f),
                    velocity,
                    projectileType,
                    holdout.GetScaledDamage(damageMultiplier),
                    holdout.Projectile.knockBack,
                    holdout.Projectile.owner,
                    ai0Base + i,
                    holdout.LeftBurstIndex + i);
            }

            holdout.LeftBurstIndex += count;
            holdout.ApplyRecoil(recoil);
            holdout.TriggerMuzzleFlash(muzzleFlashFrames);
            holdout.SpawnMuzzleBurst(muzzleColor, muzzleScale);
        }

        internal static void BeginAdditive()
        {
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, SamplerState.LinearClamp, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
        }

        internal static void EndAdditive()
        {
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
        }
    }
}
