using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Dev.MK14EBR
{
    internal static class MK14TextureComposer
    {
        public const string Root = "CalamityLegendsComeBack/Weapons/A_Dev/MK14EBR/Pic/";
        public const string BodyPath = Root + "m14枪身";

        public static string BarrelPath(MK14Barrel barrel, MK14Muzzle muzzle)
        {
            string prefix = barrel switch
            {
                MK14Barrel.SniperHeavy => "m14长枪管",
                MK14Barrel.CQBShort => "m14短枪管",
                _ => "m14枪管"
            };

            return Root + prefix + ((int)muzzle + 1);
        }

        public static string StockPath(MK14Stock stock) => stock switch
        {
            MK14Stock.Heavy => Root + "枪托/m14枪托2-重型",
            MK14Stock.Skeleton => Root + "枪托/m14枪托3-骨架",
            _ => Root + "枪托/m14枪托1-EBR"
        };

        public static string UnderbarrelPath(MK14Underbarrel underbarrel) => underbarrel switch
        {
            MK14Underbarrel.GrenadeLauncher => Root + "下挂/m14下挂1-榴弹发射器",
            MK14Underbarrel.DragonBreathShotgun => Root + "下挂/m14下挂2-龙息霰弹发射器",
            MK14Underbarrel.FoldingBipod => Root + "下挂/m14下挂3-折叠脚架",
            MK14Underbarrel.LaserPointer => Root + "下挂/m14下挂4-激光指示器",
            _ => null
        };

        public static string SightPath(MK14Sight sight) => sight switch
        {
            MK14Sight.FireControl => Root + "瞄具/m14瞄具2-火控瞄准镜",
            MK14Sight.Thermal => Root + "瞄具/m14瞄具3-红外热成像",
            MK14Sight.HighPower => Root + "瞄具/m14瞄具4-高倍率瞄具",
            _ => Root + "瞄具/m14瞄具1-红点瞄具"
        };

        public static Texture2D BodyTexture => ModContent.Request<Texture2D>(BodyPath).Value;

        public static void DrawComposite(
            SpriteBatch spriteBatch,
            NewLegendMK14EBR weapon,
            Vector2 position,
            Color color,
            float rotation,
            float scale,
            SpriteEffects effects)
        {
            Texture2D body = BodyTexture;
            Vector2 origin = new(body.Width * 0.5f, body.Height * 0.5f);

            DrawLayer(spriteBatch, BodyPath, position, color, rotation, origin, scale, effects);
            DrawLayer(spriteBatch, StockPath(weapon.Stock), position, color, rotation, origin, scale, effects);
            DrawLayer(spriteBatch, BarrelPath(weapon.Barrel, weapon.Muzzle), position, color, rotation, origin, scale, effects);

            string underbarrel = UnderbarrelPath(weapon.Underbarrel);
            if (!string.IsNullOrEmpty(underbarrel))
                DrawLayer(spriteBatch, underbarrel, position, color, rotation, origin, scale, effects);

            DrawLayer(spriteBatch, SightPath(weapon.Sight), position, color, rotation, origin, scale, effects);
        }

        private static void DrawLayer(
            SpriteBatch spriteBatch,
            string path,
            Vector2 position,
            Color color,
            float rotation,
            Vector2 origin,
            float scale,
            SpriteEffects effects)
        {
            Texture2D texture = ModContent.Request<Texture2D>(path).Value;
            spriteBatch.Draw(texture, position, null, color, rotation, origin, scale, effects, 0f);
        }
    }
}
