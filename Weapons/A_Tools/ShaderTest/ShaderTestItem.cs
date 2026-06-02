using CalamityMod;
using CalamityMod.Enums;
using CalamityMod.Graphics.Primitives;
using CalamityLegendsComeBack.Shader;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Tools.ShaderTest
{
    public sealed class ShaderTestItem : ModItem, ILocalizedModType
    {
        public const string DemoTexture = "CalamityLegendsComeBack/Weapons/A_Tools/CTRLBoss";

        // 直接改这些名字即可切换测试目标。
        public const string TrailShaderEffectName = "TrailBlazingFlame";
        public const string TrailShaderRegistrationName = "TrailBlazingFlameEffect";
        public const string OverlayShaderName = "ScanlineShader";
        public const string ScreenShaderRegistrationName = "BlackHoleDistortion";

        public new string LocalizationCategory => "Items.Weapons";
        public override string Texture => DemoTexture;

        public override bool AltFunctionUse(Player player) => true;

        public override void SetDefaults()
        {
            Item.width = 44;
            Item.height = 48;
            Item.damage = 1;
            Item.knockBack = 0f;
            Item.DamageType = DamageClass.Generic;
            Item.useTime = 18;
            Item.useAnimation = 18;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.noMelee = true;
            Item.autoReuse = false;
            Item.shoot = ModContent.ProjectileType<ShaderTestProjectile>();
            Item.shootSpeed = 13f;
            Item.UseSound = null;
            Item.value = 0;
            Item.rare = ItemRarityID.Cyan;
            Item.Calamity().devItem = true;
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            if (player.altFunctionUse != 2)
                return true;

            player.GetModPlayer<ShaderTestPlayer>().ToggleScreenShader();
            return false;
        }
    }

    internal sealed class ShaderTestPlayer : ModPlayer
    {
        private bool screenShaderEnabled;

        public void ToggleScreenShader()
        {
            SetScreenShaderEnabled(!screenShaderEnabled);
        }

        public override void UpdateDead()
        {
            SetScreenShaderEnabled(false);
        }

        public override void PostUpdate()
        {
            if (Main.dedServ || Player.whoAmI != Main.myPlayer || !screenShaderEnabled)
                return;

            if (Player.dead || Player.HeldItem?.type != ModContent.ItemType<ShaderTestItem>())
            {
                SetScreenShaderEnabled(false);
                return;
            }

            Player.ManageSpecialBiomeVisuals(ShaderGames.SceneFilterKey(ShaderTestItem.ScreenShaderRegistrationName), true);
        }

        private void SetScreenShaderEnabled(bool enabled)
        {
            screenShaderEnabled = enabled;

            if (Main.dedServ || Player.whoAmI != Main.myPlayer)
                return;

            bool shouldShow = enabled &&
                !Player.dead &&
                Player.HeldItem?.type == ModContent.ItemType<ShaderTestItem>();

            Player.ManageSpecialBiomeVisuals(ShaderGames.SceneFilterKey(ShaderTestItem.ScreenShaderRegistrationName), shouldShow);
            screenShaderEnabled = shouldShow;
        }
    }

    internal sealed class ShaderTestProjectile : ModProjectile, ILocalizedModType, IPixelatedPrimitiveRenderer
    {
        public new string LocalizationCategory => "Projectiles.A_Dev";
        public override string Texture => ShaderTestItem.DemoTexture;

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 24;
            ProjectileID.Sets.TrailingMode[Type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = 24;
            Projectile.height = 24;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.DamageType = DamageClass.Generic;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 180;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = true;
            Projectile.extraUpdates = 1;
        }

        public override void AI()
        {
            if (Projectile.velocity.LengthSquared() > 0.01f)
                Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Effect overlayShader = ShaderGames.GetEffect(ShaderTestItem.OverlayShaderName);
            if (overlayShader is null)
                return true;

            overlayShader.Parameters["uTime"]?.SetValue(Main.GlobalTimeWrappedHourly);
            overlayShader.Parameters["uLineDensity"]?.SetValue(92f);
            overlayShader.Parameters["uOpacity"]?.SetValue(1f);

            Texture2D texture = TextureAssets.Projectile[Type].Value;
            Rectangle frame = texture.Frame();
            Vector2 origin = frame.Size() * 0.5f;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(
                SpriteSortMode.Immediate,
                BlendState.AlphaBlend,
                SamplerState.PointClamp,
                DepthStencilState.None,
                Main.Rasterizer,
                overlayShader,
                Main.GameViewMatrix.TransformationMatrix);

            Main.spriteBatch.Draw(
                texture,
                drawPosition,
                frame,
                Color.White,
                Projectile.rotation,
                origin,
                Projectile.scale,
                SpriteEffects.None,
                0f);

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(
                SpriteSortMode.Deferred,
                BlendState.AlphaBlend,
                Main.DefaultSamplerState,
                DepthStencilState.None,
                Main.Rasterizer,
                null,
                Main.GameViewMatrix.TransformationMatrix);

            return false;
        }

        public void RenderPixelatedPrimitives(SpriteBatch spriteBatch, GeneralDrawLayer layer)
        {
            if (!ShaderGames.TryGetMiscShader(ShaderTestItem.TrailShaderRegistrationName, out MiscShaderData trailShader))
                return;

            Vector2[] trailPoints = BuildTrailPoints();
            if (trailPoints.Length < 2)
                return;

            Effect rawTrailEffect = ShaderGames.GetEffect(ShaderTestItem.TrailShaderEffectName);
            rawTrailEffect?.Parameters["uTime"]?.SetValue(Main.GlobalTimeWrappedHourly);
            rawTrailEffect?.Parameters["uFlameIntensity"]?.SetValue(0.55f);

            trailShader
                .SetShaderTexture(ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/Trails/SylvestaffStreak"))
                .UseColor(new Color(255, 125, 35))
                .UseSecondaryColor(new Color(120, 18, 8))
                .UseOpacity(0.95f);

            PrimitiveRenderer.RenderTrail(
                trailPoints,
                new PrimitiveSettings(
                    TrailWidth,
                    TrailColor,
                    (_, _) => Projectile.Size * 0.5f,
                    true,
                    true,
                    trailShader),
                trailPoints.Length * 2);
        }

        private Vector2[] BuildTrailPoints()
        {
            Vector2[] points = new Vector2[Projectile.oldPos.Length + 1];
            points[0] = Projectile.Center;

            for (int i = 0; i < Projectile.oldPos.Length; i++)
            {
                Vector2 oldPosition = Projectile.oldPos[i];
                points[i + 1] = oldPosition == Vector2.Zero
                    ? Projectile.Center - Projectile.velocity * (i + 1)
                    : oldPosition + Projectile.Size * 0.5f;
            }

            return points;
        }

        private static float TrailWidth(float completion, Vector2 _)
        {
            float body = Utils.GetLerpValue(1f, 0.08f, completion, true);
            return MathHelper.SmoothStep(24f, 4f, completion) * body;
        }

        private static Color TrailColor(float completion, Vector2 _)
        {
            Color hot = new(255, 160, 45, 0);
            Color ember = new(200, 35, 18, 0);
            float opacity = Utils.GetLerpValue(1f, 0.12f, completion, true);
            return Color.Lerp(hot, ember, completion) * opacity;
        }
    }
}
