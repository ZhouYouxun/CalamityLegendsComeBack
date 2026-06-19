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
                case PristineFuryMark.DesertScourge:
                    PFDesertScourgeEffect.Update(holdout, held, justPressed, justReleased);
                    break;
                case PristineFuryMark.EyeOfCthulhu:
                    PFEyeOfCthulhuEffect.Update(holdout, held, justPressed, justReleased);
                    break;
                case PristineFuryMark.Skeletron:
                    PFSkeletronEffect.Update(holdout, held, justPressed, justReleased);
                    break;
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
                case PristineFuryMark.FakeCalamity:
                    PFFakeCalamityEffect.Update(holdout, held, justPressed, justReleased);
                    break;
                case PristineFuryMark.Plantera:
                    PFPlanteraEffect.Update(holdout, held, justPressed, justReleased);
                    break;
                case PristineFuryMark.Golem:
                    PFAuroraEffect.Update(holdout, held, justPressed, justReleased);
                    break;
                case PristineFuryMark.Goliath:
                    PFGoliathEffect.Update(holdout, held, justPressed, justReleased);
                    break;
                case PristineFuryMark.Empress:
                    PFEmpressEffect.Update(holdout, held, justPressed, justReleased);
                    break;
                case PristineFuryMark.Moonlord:
                    PFMoonlordEffect.Update(holdout, held, justPressed, justReleased);
                    break;
                case PristineFuryMark.Providence:
                    PFProvidenceEffect.Update(holdout, held, justPressed, justReleased);
                    break;
                case PristineFuryMark.Ravager:
                    PFRavagerEffect.Update(holdout, held, justPressed, justReleased);
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

            int projectileIndex = Projectile.NewProjectile(
                holdout.Projectile.GetSource_FromThis(),
                muzzle,
                direction * speed,
                projectileType,
                holdout.GetScaledDamage(damageMultiplier),
                holdout.Projectile.knockBack,
                holdout.Projectile.owner,
                ai0,
                holdout.LeftBurstIndex++);
            ApplyTheme(projectileIndex, holdout.CurrentMark);

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

                int projectileIndex = Projectile.NewProjectile(
                    holdout.Projectile.GetSource_FromThis(),
                    muzzle + direction * (i * 3f),
                    velocity,
                    projectileType,
                    holdout.GetScaledDamage(damageMultiplier),
                    holdout.Projectile.knockBack,
                    holdout.Projectile.owner,
                    ai0Base + i,
                    holdout.LeftBurstIndex + i);
                ApplyTheme(projectileIndex, holdout.CurrentMark);
            }

            holdout.LeftBurstIndex += count;
            holdout.ApplyRecoil(recoil);
            holdout.TriggerMuzzleFlash(muzzleFlashFrames);
            holdout.SpawnMuzzleBurst(muzzleColor, muzzleScale);
        }

        private static BlendState customAdditive;
        internal static void BeginAdditive()
        {
            if (customAdditive == null)
            {
                customAdditive = new BlendState
                {
                    ColorSourceBlend = Blend.One,
                    ColorDestinationBlend = Blend.One,
                    ColorBlendFunction = BlendFunction.Add,
                    AlphaSourceBlend = Blend.One,
                    AlphaDestinationBlend = Blend.One,
                    AlphaBlendFunction = BlendFunction.Add
                };
            }
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, customAdditive, SamplerState.LinearClamp, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
        }

        internal static void EndAdditive()
        {
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
        }

        internal static Color GetThemeColor(Projectile projectile, Color fallback)
        {
            int markValue = (int)projectile.ai[2];
            return System.Enum.IsDefined(typeof(PristineFuryMark), markValue)
                ? PristineFuryMarkHelper.GetColor((PristineFuryMark)markValue)
                : fallback;
        }

        internal static void ApplyTheme(int projectileIndex, PristineFuryMark mark)
        {
            if (projectileIndex < 0 || projectileIndex >= Main.maxProjectiles)
                return;

            Main.projectile[projectileIndex].ai[2] = (int)mark;
            Main.projectile[projectileIndex].netUpdate = true;
        }
    }
}
