using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;

namespace CalamityLegendsComeBack.Weapons.SHPC
{
    internal static class SHPCLeftClickSounds
    {
        private static readonly SoundStyle ArtAttackCast = new("CalamityMod/Sounds/Item/ArtAttackCast");
        private static readonly SoundStyle ArcNovaSmallShot = new("CalamityMod/Sounds/Item/ArcNovaDiffuserSmallShot");
        private static readonly SoundStyle HadalUrnOpen = new("CalamityMod/Sounds/Item/HadalUrnOpen");
        private static readonly SoundStyle IceBarrageCast = new("CalamityMod/Sounds/Item/IceBarrageCast");
        private static readonly SoundStyle MechGaussRifle = new("CalamityMod/Sounds/Item/MechGaussRifle");
        private static readonly SoundStyle MeldExplosion = new("CalamityMod/Sounds/Item/MeldExplosion");
        private static readonly SoundStyle TeslaCannonFire = new("CalamityMod/Sounds/Item/TeslaCannonFire");

        public static void PlayForEffect(int effectID, Vector2 position)
        {
            if (Main.dedServ)
                return;

            if (UsesDefaultOnlyLeftClickSound(effectID))
                return;

            switch (effectID)
            {
                case 1:
                    Play(SoundID.Item15 with { Volume = 0.24f, Pitch = 0.18f, MaxInstances = 5 }, position);
                    Play(SoundID.Item93 with { Volume = 0.18f, Pitch = 0.32f, MaxInstances = 5 }, position);
                    break;
                case 2:
                    Play(SoundID.Item122 with { Volume = 0.34f, Pitch = 0.22f, PitchVariance = 0.08f, MaxInstances = 5 }, position);
                    break;
                case 3:
                    Play(SoundID.Item13 with { Volume = 0.32f, Pitch = -0.16f, PitchVariance = 0.08f, MaxInstances = 5 }, position);
                    break;
                case 4:
                    Play(SoundID.Item75 with { Volume = 0.22f, Pitch = 0.34f, MaxInstances = 5 }, position);
                    Play(SoundID.Item93 with { Volume = 0.24f, Pitch = 0.48f, MaxInstances = 5 }, position);
                    break;
                case 5:
                    Play(SoundID.Item20 with { Volume = 0.3f, Pitch = -0.18f, PitchVariance = 0.08f, MaxInstances = 5 }, position);
                    break;
                case 6:
                    Play(SoundID.Item30 with { Volume = 0.34f, Pitch = 0.16f, PitchVariance = 0.08f, MaxInstances = 5 }, position);
                    break;
                case 7:
                    Play(SoundID.Item72 with { Volume = 0.32f, Pitch = 0.18f, MaxInstances = 5 }, position);
                    Play(SoundID.Item34 with { Volume = 0.18f, Pitch = 0.32f, MaxInstances = 5 }, position);
                    break;
                case 8:
                    Play(SoundID.Item14 with { Volume = 0.34f, Pitch = -0.18f, PitchVariance = 0.08f, MaxInstances = 4 }, position);
                    break;
                case 9:
                    Play(SoundID.Item9 with { Volume = 0.28f, Pitch = 0.36f, MaxInstances = 5 }, position);
                    break;
                case 10:
                    Play(SoundID.Item8 with { Volume = 0.28f, Pitch = -0.22f, MaxInstances = 5 }, position);
                    break;
                case 11:
                    Play(SoundID.Item24 with { Volume = 0.24f, Pitch = 0.28f, PitchVariance = 0.08f, MaxInstances = 5 }, position);
                    break;
                case 12:
                    Play(SoundID.Item103 with { Volume = 0.28f, Pitch = -0.32f, PitchVariance = 0.08f, MaxInstances = 5 }, position);
                    break;
                case 13:
                    Play(SoundID.Item14 with { Volume = 0.36f, Pitch = -0.28f, PitchVariance = 0.08f, MaxInstances = 4 }, position);
                    break;
                case 14:
                    Play(SoundID.Item33 with { Volume = 0.3f, Pitch = 0.1f, PitchVariance = 0.08f, MaxInstances = 5 }, position);
                    break;
                case 15:
                    Play(SoundID.Item60 with { Volume = 0.26f, Pitch = 0.22f, PitchVariance = 0.08f, MaxInstances = 5 }, position);
                    break;
                case 17:
                    Play(HadalUrnOpen with { Volume = 0.24f, Pitch = 0.08f, PitchVariance = 0.08f, MaxInstances = 5 }, position);
                    break;
                case 18:
                    Play(SoundID.Item43 with { Volume = 0.28f, Pitch = -0.08f, PitchVariance = 0.08f, MaxInstances = 5 }, position);
                    Play(SoundID.Item14 with { Volume = 0.16f, Pitch = 0.2f, MaxInstances = 4 }, position);
                    break;
                case 19:
                    Play(ArtAttackCast with { Volume = 0.28f, Pitch = -0.08f, PitchVariance = 0.08f, MaxInstances = 5 }, position);
                    break;
                case 21:
                    Play(SoundID.Item68 with { Volume = 0.32f, Pitch = 0.02f, PitchVariance = 0.08f, MaxInstances = 5 }, position);
                    break;
                case 22:
                    Play(SoundID.Item61 with { Volume = 0.3f, Pitch = -0.04f, PitchVariance = 0.08f, MaxInstances = 5 }, position);
                    break;
                case 23:
                    Play(SoundID.Item88 with { Volume = 0.26f, Pitch = 0.2f, PitchVariance = 0.08f, MaxInstances = 5 }, position);
                    break;
                case 24:
                    Play(SoundID.Item44 with { Volume = 0.26f, Pitch = 0.18f, PitchVariance = 0.08f, MaxInstances = 5 }, position);
                    break;
                case 25:
                    Play(MeldExplosion with { Volume = 0.24f, Pitch = 0.18f, PitchVariance = 0.08f, MaxInstances = 4 }, position);
                    break;
                case 26:
                    Play(SoundID.DD2_FlameburstTowerShot with { Volume = 0.3f, Pitch = 0.12f, PitchVariance = 0.08f, MaxInstances = 5 }, position);
                    break;
                case 28:
                    Play(SoundID.Item72 with { Volume = 0.28f, Pitch = 0.32f, MaxInstances = 5 }, position);
                    Play(SoundID.Item68 with { Volume = 0.18f, Pitch = 0.28f, MaxInstances = 5 }, position);
                    break;
                case 29:
                    Play(SoundID.Item20 with { Volume = 0.3f, Pitch = -0.28f, PitchVariance = 0.08f, MaxInstances = 5 }, position);
                    break;
                case 30:
                    Play(SoundID.Item105 with { Volume = 0.24f, Pitch = -0.18f, PitchVariance = 0.08f, MaxInstances = 5 }, position);
                    break;
                case 31:
                    Play(SoundID.Item8 with { Volume = 0.3f, Pitch = -0.18f, PitchVariance = 0.08f, MaxInstances = 5 }, position);
                    Play(SoundID.Item109 with { Volume = 0.18f, Pitch = 0.22f, MaxInstances = 5 }, position);
                    break;
                case 32:
                    Play(SoundID.Item84 with { Volume = 0.28f, Pitch = -0.16f, PitchVariance = 0.08f, MaxInstances = 5 }, position);
                    break;
                case 33:
                    Play(SoundID.Item45 with { Volume = 0.26f, Pitch = -0.12f, PitchVariance = 0.08f, MaxInstances = 5 }, position);
                    Play(SoundID.Item68 with { Volume = 0.18f, Pitch = -0.06f, MaxInstances = 5 }, position);
                    break;
                case 34:
                    Play(IceBarrageCast with { Volume = 0.32f, Pitch = 0.1f, PitchVariance = 0.08f, MaxInstances = 5 }, position);
                    break;
                case 35:
                    Play(MechGaussRifle with { Volume = 0.34f, Pitch = -0.06f, PitchVariance = 0.08f, MaxInstances = 4 }, position);
                    break;
                case 36:
                    Play(TeslaCannonFire with { Volume = 0.28f, Pitch = 0.18f, PitchVariance = 0.08f, MaxInstances = 5 }, position);
                    Play(SoundID.Item92 with { Volume = 0.2f, Pitch = 0.22f, MaxInstances = 5 }, position);
                    break;
                case 37:
                    Play(SoundID.Item74 with { Volume = 0.34f, Pitch = -0.18f, PitchVariance = 0.08f, MaxInstances = 4 }, position);
                    break;
                case 38:
                    Play(ArcNovaSmallShot with { Volume = 0.34f, Pitch = 0.12f, PitchVariance = 0.08f, MaxInstances = 5 }, position);
                    break;
                case 39:
                    Play(SoundID.Item103 with { Volume = 0.32f, Pitch = -0.3f, PitchVariance = 0.08f, MaxInstances = 4 }, position);
                    break;
                case 40:
                    Play(SoundID.Item38 with { Volume = 0.3f, Pitch = -0.12f, PitchVariance = 0.08f, MaxInstances = 5 }, position);
                    Play(SoundID.Item122 with { Volume = 0.18f, Pitch = 0.16f, MaxInstances = 5 }, position);
                    break;
                case 44:
                    Play(ArcNovaSmallShot with { Volume = 0.42f, Pitch = -0.2f, PitchVariance = 0.08f, MaxInstances = 5 }, position);
                    Play(SoundID.Item84 with { Volume = 0.2f, Pitch = -0.34f, MaxInstances = 4 }, position);
                    break;
                default:
                    break;
            }
        }

        private static bool UsesDefaultOnlyLeftClickSound(int effectID)
        {
            return effectID >= 9 && effectID <= 14;
        }

        private static void Play(SoundStyle sound, Vector2 position)
        {
            SoundEngine.PlaySound(sound, position);
        }
    }
}
