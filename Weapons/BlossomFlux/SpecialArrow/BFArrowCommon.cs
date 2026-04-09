using CalamityLegendsComeBack.Weapons.BlossomFlux.Chloroplast;
using CalamityMod;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.BlossomFlux.SpecialArrow
{
    // 五种特种箭共用的基础工具，统一处理朝向、拖影和默认箭矢参数。
    internal static class BFArrowCommon
    {
        public static void SetBaseArrowDefaults(Projectile projectile, int width = 14, int height = 34, int timeLeft = 180, int penetrate = 1, int extraUpdates = 1, bool tileCollide = true)
        {
            projectile.width = width;
            projectile.height = height;
            projectile.friendly = true;
            projectile.hostile = false;
            projectile.DamageType = DamageClass.Ranged;
            projectile.ignoreWater = true;
            projectile.tileCollide = tileCollide;
            projectile.arrow = true;
            projectile.penetrate = penetrate;
            projectile.timeLeft = timeLeft;
            projectile.extraUpdates = extraUpdates;
            projectile.usesLocalNPCImmunity = true;
            projectile.localNPCHitCooldown = 12;
        }

        public static void FaceForward(Projectile projectile, float rotationOffset = MathHelper.PiOver2 + MathHelper.Pi)
        {
            if (projectile.velocity != Vector2.Zero)
                projectile.rotation = projectile.velocity.ToRotation() + rotationOffset;
        }

        public static void MaintainSpeed(Projectile projectile, float targetSpeed, float interpolation = 0.08f)
        {
            if (projectile.velocity == Vector2.Zero)
                return;

            float speed = MathHelper.Lerp(projectile.velocity.Length(), targetSpeed, interpolation);
            projectile.velocity = projectile.velocity.SafeNormalize(Vector2.UnitX) * speed;
        }

        public static void WeakHomeTowards(Projectile projectile, NPC target, float inertia = 24f, float targetSpeed = -1f)
        {
            if (target is null || !target.active)
                return;

            float speed = targetSpeed > 0f ? targetSpeed : System.Math.Max(projectile.velocity.Length(), 10f);
            Vector2 desiredVelocity = (target.Center - projectile.Center).SafeNormalize(projectile.velocity.SafeNormalize(Vector2.UnitX)) * speed;
            projectile.velocity = (projectile.velocity * (inertia - 1f) + desiredVelocity) / inertia;
        }

        public static bool Bounce(Projectile projectile, Vector2 oldVelocity, ref float bounceCounter, int maxBounces, float velocityRetention = 1f)
        {
            bounceCounter++;
            if (bounceCounter > maxBounces)
                return true;

            if (projectile.velocity.X != oldVelocity.X)
                projectile.velocity.X = -oldVelocity.X * velocityRetention;

            if (projectile.velocity.Y != oldVelocity.Y)
                projectile.velocity.Y = -oldVelocity.Y * velocityRetention;

            projectile.netUpdate = true;
            return false;
        }

        public static void DrawAfterimagesThenProjectile(Projectile projectile, Color lightColor, float scale = 1f)
        {
            CalamityUtils.DrawAfterimagesCentered(projectile, ProjectileID.Sets.TrailingMode[projectile.type], lightColor, 1);
            DrawProjectile(projectile, TextureAssets.Projectile[projectile.type].Value, projectile.GetAlpha(lightColor), projectile.rotation, projectile.scale * scale);
        }

        public static void DrawProjectile(Projectile projectile, Texture2D texture, Color color, float rotation, float scale = 1f)
        {
            Main.EntitySpriteDraw(
                texture,
                projectile.Center - Main.screenPosition,
                null,
                color,
                rotation,
                texture.Size() * 0.5f,
                scale,
                SpriteEffects.None,
                0);
        }

        public static void TagBlossomFluxLeftArrow(Projectile projectile)
        {
            projectile.arrow = true;
            projectile.noDropItem = true;
            projectile.GetGlobalProjectile<BFArrow_CDetecEffect>().BlossomFluxLeftArrow = true;
        }

        public static bool InBounds(int index, int max) => index >= 0 && index < max;

        public static bool InBounds(float index, int max) => index >= 0f && index < max;

        public static bool TryPickBlossomFluxAmmo(Player player, out int projectileType, out float speed, out int damage, out float knockback, bool dontConsume = true)
        {
            Item blossomFlux = new();
            blossomFlux.SetDefaults(ModContent.ItemType<NewLegendBlossomFlux>());
            return player.PickAmmo(blossomFlux, out projectileType, out speed, out damage, out knockback, out _, dontConsume);
        }

        public static Player FindLowestHealthPlayer(Player owner, float maxDistance = 1800f)
        {
            Player bestPlayer = owner;
            float bestRatio = owner.statLifeMax2 > 0 ? owner.statLife / (float)owner.statLifeMax2 : 1f;

            for (int i = 0; i < Main.maxPlayers; i++)
            {
                Player candidate = Main.player[i];
                if (!candidate.active || candidate.dead)
                    continue;

                if (Vector2.Distance(owner.Center, candidate.Center) > maxDistance)
                    continue;

                float ratio = candidate.statLifeMax2 > 0 ? candidate.statLife / (float)candidate.statLifeMax2 : 1f;
                if (ratio < bestRatio)
                {
                    bestRatio = ratio;
                    bestPlayer = candidate;
                }
            }

            return bestPlayer;
        }

        public static string GetTexturePathForPreset(BlossomFluxChloroplastPresetType preset) => preset switch
        {
            BlossomFluxChloroplastPresetType.Chlo_ABreak => "CalamityLegendsComeBack/Weapons/BlossomFlux/SpecialArrow/BFArrow_ABreak",
            BlossomFluxChloroplastPresetType.Chlo_BRecov => "CalamityLegendsComeBack/Weapons/BlossomFlux/SpecialArrow/BFArrow_BRecov",
            BlossomFluxChloroplastPresetType.Chlo_CDetec => "CalamityLegendsComeBack/Weapons/BlossomFlux/SpecialArrow/BFArrow_CDetec",
            BlossomFluxChloroplastPresetType.Chlo_DBomb => "CalamityLegendsComeBack/Weapons/BlossomFlux/SpecialArrow/BFArrow_DBomb",
            BlossomFluxChloroplastPresetType.Chlo_EPlague => "CalamityLegendsComeBack/Weapons/BlossomFlux/SpecialArrow/BFArrow_EPlague",
            _ => "CalamityLegendsComeBack/Weapons/BlossomFlux/SpecialArrow/BFArrow_ABreak"
        };

        public static Color GetPresetColor(BlossomFluxChloroplastPresetType preset) => preset switch
        {
            BlossomFluxChloroplastPresetType.Chlo_ABreak => new Color(140, 255, 140),
            BlossomFluxChloroplastPresetType.Chlo_BRecov => new Color(120, 255, 184),
            BlossomFluxChloroplastPresetType.Chlo_CDetec => new Color(255, 92, 92),
            BlossomFluxChloroplastPresetType.Chlo_DBomb => new Color(255, 188, 96),
            BlossomFluxChloroplastPresetType.Chlo_EPlague => new Color(172, 228, 92),
            _ => Color.White
        };
    }
}
