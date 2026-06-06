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
        public static Effect SHPCLaserDyeTrail => GetEffect("SHPCLaserDyeTrail");
        public static Effect SHPCMiniLaserDyeTrail => GetEffect("SHPCMiniLaserDyeTrail");

        private static readonly ShaderDefinition[] TrailShaders =
        [
            // Trail shaders are rendered by PrimitiveRenderer.
            new("TailFirst", ShaderCategory.Trail, "TrailPass", "TailFirstEffect"),
            new("TailSecond", ShaderCategory.Trail, "TrailPass", "TailSecondEffect"),
            new("TailMagic", ShaderCategory.Trail, "TrailPass", "TailMagicEffect"),
            new("TailModern", ShaderCategory.Trail, "TrailPass", "TailModernEffect"),
            new("TailTechnology", ShaderCategory.Trail, "TrailPass", "TailTechnologyEffect"),
            new("TrailFrostCrystal", ShaderCategory.Trail, "TrailPass", "TrailFrostCrystalEffect"),
            new("TrailGhostlyPhantom", ShaderCategory.Trail, "TrailPass", "TrailGhostlyPhantomEffect"),
            new("TrailBlazingFlame", ShaderCategory.Trail, "TrailPass", "TrailBlazingFlameEffect"),
            new("TrailWarpDistortion", ShaderCategory.Trail, "TrailPass", "TrailWarpDistortionEffect"),
            new("ArtAttackTrail", ShaderCategory.Trail, "TrailPass", "ArtAttackTrail"),
            new("SHPCLaserDyeTrail", ShaderCategory.Trail, "TrailPass", "SHPCLaserDyeTrail"),
            new("SHPCMiniLaserDyeTrail", ShaderCategory.Trail, "TrailPass", "SHPCMiniLaserDyeTrail")
        ];

        private static void RegisterTrailShaders()
        {
            foreach (ShaderDefinition shader in TrailShaders)
                RegisterMiscShader(shader.Name, shader.PassName, shader.RegistrationName);
        }
    }
}
