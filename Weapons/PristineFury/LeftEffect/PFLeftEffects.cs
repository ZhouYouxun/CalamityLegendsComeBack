using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.PristineFury.LeftEffect
{
    internal abstract class PristineFuryLeftEffect
    {
        internal virtual int FireInterval => 5;
        internal virtual float Recoil => 3f;

        internal virtual void Update(NewLegendPristineFuryHoldOut holdout, bool held, bool justPressed, bool justReleased)
        {
            if (!held)
            {
                holdout.LeftTimer = 0;
                holdout.LeftChargeTimer = 0;
                return;
            }

            holdout.LeftTimer++;
            if (holdout.LeftTimer < FireInterval)
                return;

            holdout.LeftTimer = 0;
            Fire(holdout);
        }

        protected abstract void Fire(NewLegendPristineFuryHoldOut holdout);

        protected static void FireBreath(NewLegendPristineFuryHoldOut holdout, int style, float speed, float damageMultiplier, float randomSpread = 0.04f)
        {
            Vector2 direction = holdout.AimDirection.RotatedByRandom(randomSpread);
            Projectile.NewProjectile(
                holdout.Projectile.GetSource_FromThis(),
                holdout.GunTipPosition + direction * 12f,
                direction * speed,
                ModContent.ProjectileType<PristineFuryBreath>(),
                holdout.GetScaledDamage(damageMultiplier),
                holdout.Projectile.knockBack,
                holdout.Projectile.owner,
                style);

            holdout.ApplyRecoil(3f);
            holdout.TriggerMuzzleFlash();
            holdout.SpawnMuzzleBurst(PristineFuryMarkHelper.GetColor(holdout.CurrentMark), 0.55f);
        }
    }

    internal static class PristineFuryLeftEffectRegistry
    {
        private static readonly PristineFuryLeftEffect Idle = new IdleEffect();
        private static readonly PristineFuryLeftEffect Evil = new EvilT2Effect();
        private static readonly PristineFuryLeftEffect Slime = new SlimeGodEffect();
        private static readonly PristineFuryLeftEffect Wall = new HardModeEffect();
        private static readonly PristineFuryLeftEffect Prime = new PrimeEffect();
        private static readonly PristineFuryLeftEffect Brimstone = new BrimstoneElementalEffect();
        private static readonly PristineFuryLeftEffect Plantera = new PlanteraEffect();
        private static readonly PristineFuryLeftEffect Aurora = new AuroraEffect();
        private static readonly PristineFuryLeftEffect Goliath = new GoliathEffect();
        private static readonly PristineFuryLeftEffect Moonlord = new MoonlordEffect();
        private static readonly PristineFuryLeftEffect Providence = new ProvidenceEffect();
        private static readonly PristineFuryLeftEffect Polterghast = new PolterghastEffect();
        private static readonly PristineFuryLeftEffect Dog = new DogEffect();
        private static readonly PristineFuryLeftEffect Dragon = new DragonEffect();

        internal static PristineFuryLeftEffect Get(PristineFuryMark mark)
        {
            return mark switch
            {
                PristineFuryMark.EvilT2 => Evil,
                PristineFuryMark.SlimeGod => Slime,
                PristineFuryMark.HardMode => Wall,
                PristineFuryMark.Prime => Prime,
                PristineFuryMark.BrimstoneElemental => Brimstone,
                PristineFuryMark.Plantera => Plantera,
                PristineFuryMark.Aurora => Aurora,
                PristineFuryMark.Goliath => Goliath,
                PristineFuryMark.Moonlord => Moonlord,
                PristineFuryMark.Providence => Providence,
                PristineFuryMark.Polterghast => Polterghast,
                PristineFuryMark.Dog => Dog,
                PristineFuryMark.Dragon => Dragon,
                _ => Idle
            };
        }
    }

    internal sealed class IdleEffect : PristineFuryLeftEffect
    {
        internal override int FireInterval => 4;
        protected override void Fire(NewLegendPristineFuryHoldOut holdout) => FireBreath(holdout, 0, 11.5f, 0.45f, 0.025f);
    }

    internal sealed class EvilT2Effect : PristineFuryLeftEffect
    {
        internal override int FireInterval => 5;
        protected override void Fire(NewLegendPristineFuryHoldOut holdout)
        {
            FireBreath(holdout, 1, 11.8f, 0.46f, 0.03f);
            FireBreath(holdout, 2, 11.8f, 0.46f, 0.03f);
        }
    }

    internal sealed class SlimeGodEffect : PristineFuryLeftEffect
    {
        internal override int FireInterval => 60;
        protected override void Fire(NewLegendPristineFuryHoldOut holdout)
        {
            Vector2 direction = holdout.AimDirection;
            Vector2 muzzle = holdout.GunTipPosition + direction * 12f;

            for (int i = 0; i < 5; i++)
            {
                float offset = MathHelper.Lerp(-0.28f, 0.28f, i / 4f);
                Projectile.NewProjectile(
                    holdout.Projectile.GetSource_FromThis(),
                    muzzle,
                    direction.RotatedBy(offset) * 12f,
                    ModContent.ProjectileType<PristineFuryOverloadedBolt>(),
                    holdout.GetScaledDamage(0.88f),
                    holdout.Projectile.knockBack,
                    holdout.Projectile.owner);
            }

            holdout.ApplyRecoil(10f);
            holdout.TriggerMuzzleFlash(18);
            holdout.SpawnMuzzleBurst(new Color(196, 82, 255), 0.95f);
            SoundEngine.PlaySound(SoundID.Item92 with { Volume = 0.75f, Pitch = 0.15f }, holdout.GunTipPosition);
        }
    }

    internal sealed class HardModeEffect : PristineFuryLeftEffect
    {
        internal override int FireInterval => 25;
        protected override void Fire(NewLegendPristineFuryHoldOut holdout)
        {
            Vector2 direction = holdout.AimDirection;
            for (int i = 0; i < 3; i++)
            {
                Projectile.NewProjectile(
                    holdout.Projectile.GetSource_FromThis(),
                    holdout.GunTipPosition + direction * 16f,
                    direction * (9.5f + i * 1.2f),
                    ModContent.ProjectileType<PristineFuryPressureWave>(),
                    holdout.GetScaledDamage(0.7f + i * 0.18f),
                    holdout.Projectile.knockBack,
                    holdout.Projectile.owner,
                    i * 7f,
                    0.9f + i * 0.28f);
            }

            holdout.ApplyRecoil(13f);
            holdout.TriggerMuzzleFlash(18);
            holdout.SpawnMuzzleBurst(new Color(255, 172, 72), 1f);
        }
    }

    internal sealed class PrimeEffect : PristineFuryLeftEffect
    {
        internal override int FireInterval => 4;
        protected override void Fire(NewLegendPristineFuryHoldOut holdout) => FireBreath(holdout, 3, 12.2f, 0.42f, 0.06f);
    }

    internal sealed class BrimstoneElementalEffect : PristineFuryLeftEffect
    {
        internal override void Update(NewLegendPristineFuryHoldOut holdout, bool held, bool justPressed, bool justReleased)
        {
            if (!held)
            {
                holdout.LeftChargeTimer = 0;
                return;
            }

            holdout.LeftChargeTimer++;
            holdout.TriggerMuzzleFlash(8);

            if (holdout.LeftChargeTimer < 180)
                return;

            Vector2 direction = holdout.AimDirection;
            Projectile.NewProjectile(
                holdout.Projectile.GetSource_FromThis(),
                holdout.GunTipPosition + direction * 18f,
                direction,
                ModContent.ProjectileType<PristineFuryBrimstoneBeam>(),
                holdout.GetScaledDamage(1.75f),
                holdout.Projectile.knockBack,
                holdout.Projectile.owner);

            holdout.ApplyRecoil(18f);
            holdout.TriggerMuzzleFlash(24);
            holdout.SpawnMuzzleBurst(new Color(255, 52, 68), 1.25f);
            SoundEngine.PlaySound(SoundID.Item122 with { Volume = 0.85f, Pitch = -0.22f }, holdout.GunTipPosition);
            holdout.LeftChargeTimer = 0;
        }

        protected override void Fire(NewLegendPristineFuryHoldOut holdout)
        {
        }
    }

    internal sealed class PlanteraEffect : PristineFuryLeftEffect
    {
        internal override void Update(NewLegendPristineFuryHoldOut holdout, bool held, bool justPressed, bool justReleased)
        {
            if (held)
            {
                holdout.LeftChargeTimer++;
                holdout.TriggerMuzzleFlash(8);
                return;
            }

            if (!justReleased)
                return;

            if (holdout.LeftChargeTimer < 120)
            {
                holdout.LeftChargeTimer = 0;
                return;
            }

            Vector2 direction = holdout.AimDirection;
            for (int i = 0; i < 4; i++)
            {
                Projectile.NewProjectile(
                    holdout.Projectile.GetSource_FromThis(),
                    holdout.GunTipPosition + direction * 12f,
                    direction.RotatedBy(MathHelper.Lerp(-0.22f, 0.22f, i / 3f)) * 12.5f,
                    ModContent.ProjectileType<PristineFuryStickySpore>(),
                    holdout.GetScaledDamage(1.1f),
                    holdout.Projectile.knockBack,
                    holdout.Projectile.owner);
            }

            holdout.ApplyRecoil(20f);
            holdout.TriggerMuzzleFlash(24);
            holdout.SpawnMuzzleBurst(new Color(100, 255, 112), 1.15f);
            SoundEngine.PlaySound(SoundID.Item17 with { Volume = 0.8f, Pitch = -0.35f }, holdout.GunTipPosition);
            holdout.LeftChargeTimer = 0;
        }

        protected override void Fire(NewLegendPristineFuryHoldOut holdout)
        {
        }
    }

    internal sealed class AuroraEffect : PristineFuryLeftEffect
    {
        internal override int FireInterval => 4;
        protected override void Fire(NewLegendPristineFuryHoldOut holdout) => FireBreath(holdout, 4, 10.8f, 0.48f, 0.065f);
    }

    internal sealed class GoliathEffect : PristineFuryLeftEffect
    {
        internal override int FireInterval => 7;
        protected override void Fire(NewLegendPristineFuryHoldOut holdout)
        {
            Vector2 direction = holdout.AimDirection.RotatedByRandom(0.12f);
            Projectile.NewProjectile(
                holdout.Projectile.GetSource_FromThis(),
                holdout.GunTipPosition + direction * 12f,
                direction * Main.rand.NextFloat(8f, 11.5f),
                ModContent.ProjectileType<PristineFuryPlagueSmoke>(),
                holdout.GetScaledDamage(0.55f),
                holdout.Projectile.knockBack,
                holdout.Projectile.owner);

            holdout.ApplyRecoil(4f);
            holdout.TriggerMuzzleFlash(10);
            holdout.SpawnMuzzleBurst(new Color(142, 255, 74), 0.75f);
        }
    }

    internal sealed class MoonlordEffect : PristineFuryLeftEffect
    {
        internal override int FireInterval => 5;
        protected override void Fire(NewLegendPristineFuryHoldOut holdout) => FireBreath(holdout, 5 + Main.rand.Next(4), 12.2f, 0.52f, 0.08f);
    }

    internal sealed class ProvidenceEffect : PristineFuryLeftEffect
    {
        internal override int FireInterval => 80;
        protected override void Fire(NewLegendPristineFuryHoldOut holdout)
        {
            Vector2 direction = holdout.AimDirection;
            Projectile.NewProjectile(
                holdout.Projectile.GetSource_FromThis(),
                holdout.GunTipPosition + direction * 16f,
                direction * 13.5f,
                ModContent.ProjectileType<PristineFuryProfanedRocket>(),
                holdout.GetScaledDamage(2.5f),
                holdout.Projectile.knockBack,
                holdout.Projectile.owner,
                holdout.GetMouseWorld().X,
                holdout.GetMouseWorld().Y);

            holdout.ApplyRecoil(24f);
            holdout.TriggerMuzzleFlash(28);
            holdout.SpawnMuzzleBurst(new Color(255, 220, 112), 1.45f);
            SoundEngine.PlaySound(SoundID.Item14 with { Volume = 0.9f, Pitch = -0.25f }, holdout.GunTipPosition);
        }
    }

    internal sealed class PolterghastEffect : PristineFuryLeftEffect
    {
        internal override int FireInterval => 7;
        protected override void Fire(NewLegendPristineFuryHoldOut holdout)
        {
            Vector2 direction = holdout.AimDirection.RotatedByRandom(0.05f);
            Projectile.NewProjectile(
                holdout.Projectile.GetSource_FromThis(),
                holdout.GunTipPosition + direction * 12f,
                direction * 15.5f,
                ModContent.ProjectileType<PristineFuryPhantomStar>(),
                holdout.GetScaledDamage(0.62f),
                holdout.Projectile.knockBack,
                holdout.Projectile.owner,
                Main.rand.Next(1, 7));

            holdout.ApplyRecoil(5f);
            holdout.TriggerMuzzleFlash(12);
            holdout.SpawnMuzzleBurst(new Color(218, 126, 255), 0.82f);
            SoundEngine.PlaySound(SoundID.Item33 with { Volume = 0.44f, Pitch = 0.35f }, holdout.GunTipPosition);
        }
    }

    internal sealed class DogEffect : PristineFuryLeftEffect
    {
        internal override int FireInterval => 4;
        protected override void Fire(NewLegendPristineFuryHoldOut holdout)
        {
            Vector2 direction = holdout.AimDirection.RotatedByRandom(0.035f);
            Projectile.NewProjectile(
                holdout.Projectile.GetSource_FromThis(),
                holdout.GunTipPosition + direction * 12f,
                direction * 13.8f,
                ModContent.ProjectileType<PristineFuryVoidStream>(),
                holdout.GetScaledDamage(0.47f),
                holdout.Projectile.knockBack,
                holdout.Projectile.owner);

            if (Main.rand.NextBool(6))
            {
                Projectile.NewProjectile(
                    holdout.Projectile.GetSource_FromThis(),
                    holdout.GunTipPosition + direction * Main.rand.NextFloat(40f, 120f),
                    direction.RotatedByRandom(1.2f) * 0.1f,
                    ModContent.ProjectileType<PristineFuryVoidRift>(),
                    holdout.GetScaledDamage(0.65f),
                    holdout.Projectile.knockBack,
                    holdout.Projectile.owner,
                    direction.ToRotation());
            }

            holdout.ApplyRecoil(4f);
            holdout.TriggerMuzzleFlash(12);
            holdout.SpawnMuzzleBurst(new Color(92, 76, 224), 0.85f);
        }
    }

    internal sealed class DragonEffect : PristineFuryLeftEffect
    {
        internal override int FireInterval => 11;
        protected override void Fire(NewLegendPristineFuryHoldOut holdout)
        {
            Vector2 direction = holdout.AimDirection;
            Vector2 muzzle = holdout.GunTipPosition + direction * 12f;

            for (int i = 0; i < 18; i++)
            {
                Projectile.NewProjectile(
                    holdout.Projectile.GetSource_FromThis(),
                    muzzle,
                    direction.RotatedBy(MathHelper.Lerp(-0.36f, 0.36f, i / 17f) + Main.rand.NextFloat(-0.035f, 0.035f)) * Main.rand.NextFloat(12f, 16f),
                    ModContent.ProjectileType<PristineFuryDragonPellet>(),
                    holdout.GetScaledDamage(0.34f),
                    holdout.Projectile.knockBack,
                    holdout.Projectile.owner);
            }

            holdout.ApplyRecoil(16f);
            holdout.TriggerMuzzleFlash(18);
            holdout.SpawnMuzzleBurst(new Color(255, 98, 38), 1.1f);
            SoundEngine.PlaySound(SoundID.Item34 with { Volume = 0.75f, Pitch = -0.1f }, holdout.GunTipPosition);
        }
    }
}
