using Microsoft.Xna.Framework;
using CalamityLegendsComeBack.BossAI.NewDiff.Content.BehaviorOverrides.BossAIs.Common;

namespace CalamityLegendsComeBack.BossAI.NewDiff.Content.BehaviorOverrides.BossAIs.BStage4.OldDuke
{
    // SlitheringEels, SkyfinBombers, SpentFuelContainer and SulphurousGrabber are shared with Aquatic
    // Scourge per the design docs, so their held-weapon classes (ScourgeHeldSlitheringEels, etc.) are
    // reused directly rather than duplicated here.

    internal sealed class DukeHeldInsidiousImpaler : BossHeldSwingWeapon
    {
        public override string WeaponName => "InsidiousImpaler";
        public override float SpriteScale => 1.4f;
        public override int WindupTime => 18;
        public override int SlashTime => 10;
        public override int RecoveryTime => 14;
        public override Color GlowColor => new Color(150, 200, 90) * 0.5f;
    }

    internal sealed class DukeHeldFetidEmesis : BossHeldAimedWeapon
    {
        public override string WeaponName => "FetidEmesis";
        public override float SpriteScale => 1.3f;
        public override int PulseDuration => 12;
        public override Color GlowColor => new Color(150, 220, 100) * 0.5f;
    }

    internal sealed class DukeHeldSepticSkewer : BossHeldAimedWeapon
    {
        public override string WeaponName => "SepticSkewer";
        public override float SpriteScale => 1.3f;
        public override int PulseDuration => 14;
        public override Color GlowColor => new Color(150, 220, 100) * 0.5f;
    }

    internal sealed class DukeHeldVitriolicViper : BossHeldAimedWeapon
    {
        public override string WeaponName => "VitriolicViper";
        public override float SpriteScale => 1.3f;
        public override float AimLerp => 0.07f;
        public override int PulseDuration => 12;
        public override Color GlowColor => new Color(120, 255, 130) * 0.5f;
    }

    internal sealed class DukeHeldMutatedTruffle : BossHeldAimedWeapon
    {
        public override string WeaponName => "MutatedTruffle";
        public override float SpriteScale => 1.3f;
        public override int PulseDuration => 14;
        public override Color GlowColor => new Color(200, 150, 120) * 0.5f;
    }

    internal sealed class DukeHeldCadaverousCarrion : BossHeldAimedWeapon
    {
        public override string WeaponName => "CadaverousCarrion";
        public override float SpriteScale => 1.3f;
        public override int PulseDuration => 14;
        public override Color GlowColor => new Color(200, 190, 160) * 0.5f;
    }

    internal sealed class DukeHeldToxicantTwister : BossHeldSwingWeapon
    {
        public override string WeaponName => "ToxicantTwister";
        public override float SpriteScale => 1.3f;
        public override int WindupTime => 14;
        public override int SlashTime => 8;
        public override int RecoveryTime => 10;
        public override Color GlowColor => new Color(150, 255, 120) * 0.5f;
    }

    internal sealed class DukeHeldOldReaper : BossHeldSwingWeapon
    {
        public override string WeaponName => "TheOldReaper";
        public override float SpriteScale => 1.35f;
        public override int WindupTime => 16;
        public override int SlashTime => 9;
        public override int RecoveryTime => 12;
        public override Color GlowColor => new Color(120, 200, 90) * 0.5f;
    }

    internal sealed class DukeHeldSulphuricAcid : BossHeldAimedWeapon
    {
        public override string WeaponName => "SulphuricAcidCannon";
        public override float SpriteScale => 1.35f;
        public override int PulseDuration => 14;
        public override Color GlowColor => new Color(150, 255, 100) * 0.5f;
    }

    internal sealed class DukeHeldGammaHeart : BossHeldAimedWeapon
    {
        public override string WeaponName => "GammaHeart";
        public override float SpriteScale => 1.3f;
        public override int PulseDuration => 14;
        public override Color GlowColor => new Color(160, 255, 110) * 0.5f;
    }

    internal sealed class DukeHeldPhosphorescentGauntlet : BossHeldSwingWeapon
    {
        public override string WeaponName => "PhosphorescentGauntlet";
        public override float SpriteScale => 1.35f;
        public override int WindupTime => 14;
        public override int SlashTime => 7;
        public override int RecoveryTime => 10;
        public override Color GlowColor => new Color(160, 255, 160) * 0.5f;
    }
}
