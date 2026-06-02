using Microsoft.Xna.Framework.Graphics;

namespace CalamityLegendsComeBack.Shader
{
    public sealed partial class ShaderGames
    {
        public static Effect TailFirst => GetEffect("TailFirst");
        public static Effect TailSecond => GetEffect("TailSecond");
        public static Effect TailMagic => GetEffect("TailMagic");
        public static Effect TailModern => GetEffect("TailModern");
        public static Effect TailTechnology => GetEffect("TailTechnology");
        public static Effect TrailFrostCrystal => GetEffect("TrailFrostCrystal");
        public static Effect TrailGhostlyPhantom => GetEffect("TrailGhostlyPhantom");
        public static Effect TrailBlazingFlame => GetEffect("TrailBlazingFlame");
        public static Effect TrailWarpDistortion => GetEffect("TrailWarpDistortion");
        public static Effect ArtAttackTrail => GetEffect("ArtAttackTrail");

        private static readonly ShaderDefinition[] TrailShaders =
        [
            // 拖尾类：用于 PrimitiveRenderer、刀光、弹幕运动轨迹。
            new("TailFirst", TrailCategory, "TrailPass", "TailFirstEffect"),
            new("TailSecond", TrailCategory, "TrailPass", "TailSecondEffect"),
            new("TailMagic", TrailCategory, "TrailPass", "TailMagicEffect"),
            new("TailModern", TrailCategory, "TrailPass", "TailModernEffect"),
            new("TailTechnology", TrailCategory, "TrailPass", "TailTechnologyEffect"),
            new("TrailFrostCrystal", TrailCategory, "TrailPass", "TrailFrostCrystalEffect"),
            new("TrailGhostlyPhantom", TrailCategory, "TrailPass", "TrailGhostlyPhantomEffect"),
            new("TrailBlazingFlame", TrailCategory, "TrailPass", "TrailBlazingFlameEffect"),
            new("TrailWarpDistortion", TrailCategory, "TrailPass", "TrailWarpDistortionEffect"),
            new("ArtAttackTrail", TrailCategory, "TrailPass", "ArtAttackTrail")
        ];

        private static void RegisterTrailShaders()
        {
            foreach (ShaderDefinition shader in TrailShaders)
                RegisterMiscShader(shader.Name, shader.PassName, shader.RegistrationName);
        }
    }
}
