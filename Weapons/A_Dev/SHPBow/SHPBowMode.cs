using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;

namespace CalamityLegendsComeBack.Weapons.A_Dev.SHPBow
{
    internal enum SHPBowMode
    {
        Pierce = 0,
        Ricochet = 1,
        Scatter = 2,
        Homing = 3
    }

    internal static class SHPBowModeHelpers
    {
        public const int Count = 4;
        public const int MaxSequenceLength = 4;

        public static string IconTexturePath(SHPBowMode mode) => mode switch
        {
            SHPBowMode.Pierce => "CalamityLegendsComeBack/Weapons/A_Dev/SHPBow/\u7a7f\u900f",
            SHPBowMode.Ricochet => "CalamityLegendsComeBack/Weapons/A_Dev/SHPBow/\u53cd\u5f39",
            SHPBowMode.Scatter => "CalamityLegendsComeBack/Weapons/A_Dev/SHPBow/\u6563\u5c04",
            SHPBowMode.Homing => "CalamityLegendsComeBack/Weapons/A_Dev/SHPBow/\u8ffd\u8e2a",
            _ => "CalamityLegendsComeBack/Weapons/A_Dev/SHPBow/\u7a7f\u900f"
        };

        public static Color MainColor(SHPBowMode mode) => mode switch
        {
            SHPBowMode.Pierce => new Color(96, 226, 255),
            SHPBowMode.Ricochet => new Color(255, 212, 100),
            SHPBowMode.Scatter => new Color(255, 122, 170),
            SHPBowMode.Homing => new Color(164, 118, 255),
            _ => Color.White
        };

        public static Color AccentColor(SHPBowMode mode) => mode switch
        {
            SHPBowMode.Pierce => new Color(214, 255, 255),
            SHPBowMode.Ricochet => new Color(255, 252, 174),
            SHPBowMode.Scatter => new Color(255, 207, 226),
            SHPBowMode.Homing => new Color(219, 204, 255),
            _ => Color.White
        };

        public static int DustType(SHPBowMode mode) => mode switch
        {
            SHPBowMode.Pierce => DustID.Electric,
            SHPBowMode.Ricochet => DustID.GoldFlame,
            SHPBowMode.Scatter => DustID.PinkTorch,
            SHPBowMode.Homing => DustID.PurpleTorch,
            _ => DustID.TintableDustLighted
        };

        public static SHPBowMode ClampMode(int mode)
        {
            if (mode < 0)
                return SHPBowMode.Pierce;

            if (mode >= Count)
                return SHPBowMode.Homing;

            return (SHPBowMode)mode;
        }

        public static int PackSequence(SHPBowMode[] sequence, int length)
        {
            int clampedLength = Utils.Clamp(length, 1, MaxSequenceLength);
            int packed = clampedLength << 8;

            for (int i = 0; i < MaxSequenceLength; i++)
            {
                SHPBowMode mode = i < clampedLength ? sequence[i] : SHPBowMode.Pierce;
                packed |= ((int)ClampMode((int)mode) & 3) << (i * 2);
            }

            return packed;
        }

        public static int SequenceLength(float packedSequence)
        {
            int length = ((int)packedSequence >> 8) & 7;
            return Utils.Clamp(length, 1, MaxSequenceLength);
        }

        public static SHPBowMode SequenceMode(float packedSequence, int index)
        {
            int length = SequenceLength(packedSequence);
            int safeIndex = Utils.Clamp(index, 0, length - 1);
            return ClampMode(((int)packedSequence >> (safeIndex * 2)) & 3);
        }

        public static int CountMode(float packedSequence, SHPBowMode mode)
        {
            int count = 0;
            int length = SequenceLength(packedSequence);
            for (int i = 0; i < length; i++)
            {
                if (SequenceMode(packedSequence, i) == mode)
                    count++;
            }

            return count;
        }

        public static Color SequenceColor(float packedSequence, float completion)
        {
            int length = SequenceLength(packedSequence);
            if (length <= 1)
                return MainColor(SequenceMode(packedSequence, 0));

            float scaled = MathHelper.Clamp(completion, 0f, 1f) * (length - 1);
            int index = (int)scaled;
            int nextIndex = Utils.Clamp(index + 1, 0, length - 1);
            float localCompletion = scaled - index;
            return Color.Lerp(MainColor(SequenceMode(packedSequence, index)), MainColor(SequenceMode(packedSequence, nextIndex)), localCompletion);
        }
    }
}
