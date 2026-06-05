using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.MainMenu
{
    internal sealed class MatrixNullSurfaceBackground : ModSurfaceBackgroundStyle
    {
        public override void Load()
        {
            if (!Main.dedServ)
                BackgroundTextureLoader.AddBackgroundTexture(Mod, MatrixMenuAssets.BlankPixelFullPath);
        }

        public override void ModifyFarFades(float[] fades, float transitionSpeed)
        {
            for (int i = 0; i < fades.Length; i++)
            {
                if (i == Slot)
                    fades[i] = System.Math.Min(fades[i] + transitionSpeed, 1f);
                else
                    fades[i] = System.Math.Max(fades[i] - transitionSpeed, 0f);
            }
        }

        public override int ChooseFarTexture() => GetBlankBackgroundSlot();

        public override int ChooseMiddleTexture() => GetBlankBackgroundSlot();

        public override int ChooseCloseTexture(ref float scale, ref double parallax, ref float a, ref float b)
        {
            scale = 1f;
            parallax = 0.0;
            a = 0f;
            b = 0f;
            return GetBlankBackgroundSlot();
        }

        public override bool PreDrawCloseBackground(SpriteBatch spriteBatch) => false;

        private int GetBlankBackgroundSlot()
        {
            if (BackgroundTextureLoader.TryGetBackgroundSlot(Mod, MatrixMenuAssets.BlankPixel, out int slot))
                return slot;

            return 0;
        }
    }
}
