namespace CalamityLegendsComeBack.Shader
{
    public sealed partial class ShaderGames
    {
        // These TrailPass registrations are supplied by Calamity itself through GameShaders.Misc.
        // They are intentionally not loaded or registered by this mod.
        private static readonly ShaderDefinition[] CalamityTrailShaders =
        [
            new("Cal: Trail Streak", ShaderCategory.CalamityTrail, "TrailPass", "TrailStreak"),
            new("Cal: Flame", ShaderCategory.CalamityTrail, "TrailPass", "Flame"),
            new("Cal: Fading Solid", ShaderCategory.CalamityTrail, "TrailPass", "FadingSolidTrail"),
            new("Cal: Prismatic Streak", ShaderCategory.CalamityTrail, "TrailPass", "PrismaticStreak"),
            new("Cal: Imp Flame", ShaderCategory.CalamityTrail, "TrailPass", "ImpFlameTrail"),
            new("Cal: Art Attack", ShaderCategory.CalamityTrail, "TrailPass", "ArtAttack"),
            new("Cal: Artemis Laser", ShaderCategory.CalamityTrail, "TrailPass", "ArtemisLaser"),
            new("Cal: Exoblade Slash", ShaderCategory.CalamityTrail, "TrailPass", "ExobladeSlash"),
            new("Cal: Side Streak", ShaderCategory.CalamityTrail, "TrailPass", "SideStreakTrail"),
            new("Cal: Gale Lightning", ShaderCategory.CalamityTrail, "TrailPass", "HeavenlyGaleLightningArc"),
            new("Cal: Primitive Texture", ShaderCategory.CalamityTrail, "TrailPass", "PrimitiveTexture"),
            new("Cal: Standard Primitive", ShaderCategory.CalamityTrail, "PrimitivePass", "StandardPrimitiveShader"),
            new("Cal: Waterfall", ShaderCategory.CalamityTrail, "TrailPass", "Waterfall"),
            new("Cal: Sylvestaff", ShaderCategory.CalamityTrail, "TrailPass", "SylvestaffProjectile"),
            new("Cal: Galeforce Arrow", ShaderCategory.CalamityTrail, "TrailPass", "GaleforceArrowTrail"),
            new("Cal: Tesla", ShaderCategory.CalamityTrail, "TrailPass", "TeslaTrail")
        ];
    }
}
