using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.BossAI.NewDiff.Content.BehaviorOverrides.BossAIs.WeaponAttacks
{
    internal static class IUMWWeaponBossVisuals
    {
        public static int PackColor(Color color)
        {
            return color.R << 16 | color.G << 8 | color.B;
        }

        public static Color UnpackColor(float packed)
        {
            int value = Math.Max(0, (int)packed);
            return new Color((value >> 16) & 255, (value >> 8) & 255, value & 255);
        }

        public static Vector2 SafeDirection(Vector2 from, Vector2 to, Vector2 fallback)
        {
            Vector2 direction = to - from;
            return direction.LengthSquared() < 0.0001f ? fallback : Vector2.Normalize(direction);
        }

        public static void DrawLine(SpriteBatch spriteBatch, Vector2 start, Vector2 end, Color color, float width)
        {
            Vector2 delta = end - start;
            if (delta.LengthSquared() <= 0.001f)
                return;

            float length = delta.Length();
            float rotation = delta.ToRotation();
            // MagicPixel is 1x1000, NOT 1x1 — a null source rect scales the whole sheet, turning a thin
            // warning line into a screen-sized wall. Must sample a single pixel. (Shared helper — this one
            // fix covers every boss that calls DrawLine: arena borders, warning lines, drone lasers, etc.)
            spriteBatch.Draw(
                TextureAssets.MagicPixel.Value,
                start - Main.screenPosition,
                new Rectangle(0, 0, 1, 1),
                color,
                rotation,
                new Vector2(0f, 0.5f),
                new Vector2(length, width),
                SpriteEffects.None,
                0f);
        }
    }

    public sealed class IUMWWeaponTelegraphProjectile : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.DrawScreenCheckFluff[Type] = 9999;
        }

        public override void SetDefaults()
        {
            Projectile.width = 96;
            Projectile.height = 96;
            Projectile.timeLeft = 54;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.hide = false;
        }

        public override bool? CanDamage() => false;

        public override void AI()
        {
            Projectile.velocity *= 0.92f;
            Projectile.rotation += 0.035f * Math.Sign(Projectile.ai[2] == 0f ? 1f : Projectile.ai[2]);
            Projectile.scale = MathHelper.Lerp(Projectile.scale, Projectile.ai[2] <= 0f ? 1f : Projectile.ai[2] / 100f, 0.16f);
            Lighting.AddLight(Projectile.Center, IUMWWeaponBossVisuals.UnpackColor(Projectile.ai[1]).ToVector3() * 0.45f);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            int itemType = (int)Projectile.ai[0];
            Color outline = IUMWWeaponBossVisuals.UnpackColor(Projectile.ai[1]);
            float opacity = MathHelper.Clamp(Projectile.timeLeft / 18f, 0f, 1f);
            opacity = Math.Min(opacity, MathHelper.Clamp((54f - Projectile.timeLeft) / 8f, 0f, 1f));
            float scale = Projectile.scale * (1f + 0.05f * MathF.Sin(Main.GlobalTimeWrappedHourly * 8f));
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;

            if (itemType > 0 && itemType < TextureAssets.Item.Length)
            {
                Main.instance.LoadItem(itemType);
                Texture2D texture = TextureAssets.Item[itemType].Value;
                Rectangle frame = texture.Frame();
                Vector2 origin = frame.Size() * 0.5f;

                for (int i = 0; i < 12; i++)
                {
                    Vector2 offset = (MathHelper.TwoPi * i / 12f).ToRotationVector2() * 3.2f * scale;
                    Main.spriteBatch.Draw(texture, drawPosition + offset, frame, outline * (0.72f * opacity), Projectile.rotation, origin, scale, SpriteEffects.None, 0f);
                }

                Main.spriteBatch.Draw(texture, drawPosition, frame, Color.White * opacity, Projectile.rotation, origin, scale, SpriteEffects.None, 0f);
            }
            else
            {
                Rectangle box = new((int)drawPosition.X - 18, (int)drawPosition.Y - 18, 36, 36);
                Main.spriteBatch.Draw(TextureAssets.MagicPixel.Value, box, outline * (0.45f * opacity));
                IUMWWeaponBossVisuals.DrawLine(Main.spriteBatch, Projectile.Center + new Vector2(-24f, -24f), Projectile.Center + new Vector2(24f, 24f), Color.White * opacity, 2f);
                IUMWWeaponBossVisuals.DrawLine(Main.spriteBatch, Projectile.Center + new Vector2(-24f, 24f), Projectile.Center + new Vector2(24f, -24f), Color.White * opacity, 2f);
            }

            return false;
        }
    }

    public sealed class IUMWWeaponHostileBolt : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetDefaults()
        {
            Projectile.width = 14;
            Projectile.height = 14;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 300;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
        }

        public override void AI()
        {
            int style = (int)Projectile.ai[0];
            Color color = IUMWWeaponBossVisuals.UnpackColor(Projectile.ai[1]);

            if (style == 1)
            {
                float turn = MathHelper.Clamp(Projectile.ai[2], 0f, 0.055f);
                Player target = Main.player[Player.FindClosest(Projectile.Center, 1, 1)];
                if (target.active && !target.dead)
                {
                    float speed = Math.Max(4f, Projectile.velocity.Length());
                    Vector2 desired = IUMWWeaponBossVisuals.SafeDirection(Projectile.Center, target.Center, Vector2.UnitY) * speed;
                    Projectile.velocity = Vector2.Lerp(Projectile.velocity, desired, turn);
                }
            }
            else if (style == 2)
            {
                Projectile.velocity.Y += 0.09f;
                Projectile.velocity.X *= 0.995f;
            }
            else if (style == 3)
            {
                Projectile.velocity.Y += 0.06f;
                Projectile.velocity *= 0.992f;
            }

            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
            Lighting.AddLight(Projectile.Center, color.ToVector3() * 0.35f);

            if (Main.rand.NextBool(4))
                Dust.NewDustPerfect(Projectile.Center, DustID.FireworkFountain_Blue, -Projectile.velocity * 0.08f, 120, color, 0.75f).noGravity = true;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Color color = IUMWWeaponBossVisuals.UnpackColor(Projectile.ai[1]);
            Vector2 center = Projectile.Center - Main.screenPosition;
            Texture2D pixel = TextureAssets.MagicPixel.Value;
            float pulse = 0.85f + 0.2f * MathF.Sin(Main.GlobalTimeWrappedHourly * 12f + Projectile.whoAmI);
            Rectangle core = new((int)center.X - 5, (int)center.Y - 5, 10, 10);
            Rectangle glow = new((int)center.X - 10, (int)center.Y - 10, 20, 20);
            Main.spriteBatch.Draw(pixel, glow, color * 0.28f);
            Main.spriteBatch.Draw(pixel, core, Color.Lerp(Color.White, color, 0.45f) * pulse);

            Vector2 tail = Projectile.Center - Projectile.velocity.SafeNormalize(Vector2.UnitY) * 34f;
            IUMWWeaponBossVisuals.DrawLine(Main.spriteBatch, tail, Projectile.Center, color * 0.55f, 3f);
            return false;
        }
    }

    public sealed class IUMWWeaponLineHazard : ModProjectile
    {
        private const int TelegraphTime = 30;
        private const int Lifetime = 62;

        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetDefaults()
        {
            Projectile.width = 8;
            Projectile.height = 8;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = Lifetime;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
        }

        public override bool? CanDamage() => Projectile.localAI[0] >= TelegraphTime;

        public override void AI()
        {
            Projectile.localAI[0]++;
            Projectile.velocity *= 0.98f;
            Lighting.AddLight(Projectile.Center, IUMWWeaponBossVisuals.UnpackColor(Projectile.ai[1]).ToVector3() * 0.65f);
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            if (Projectile.localAI[0] < TelegraphTime)
                return false;

            Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            float length = Projectile.ai[2] <= 0f ? 1800f : Projectile.ai[2];
            Vector2 start = Projectile.Center - direction * length * 0.5f;
            Vector2 end = Projectile.Center + direction * length * 0.5f;
            float collisionPoint = 0f;
            return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), start, end, 20f, ref collisionPoint);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Color color = IUMWWeaponBossVisuals.UnpackColor(Projectile.ai[1]);
            Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            float length = Projectile.ai[2] <= 0f ? 1800f : Projectile.ai[2];
            Vector2 start = Projectile.Center - direction * length * 0.5f;
            Vector2 end = Projectile.Center + direction * length * 0.5f;
            bool active = Projectile.localAI[0] >= TelegraphTime;
            float fade = Projectile.timeLeft / 18f;
            float opacity = active ? MathHelper.Clamp(fade, 0f, 1f) : 0.42f + 0.18f * MathF.Sin(Main.GlobalTimeWrappedHourly * 18f);

            IUMWWeaponBossVisuals.DrawLine(Main.spriteBatch, start, end, active ? color * (0.8f * opacity) : color * opacity, active ? 18f : 4f);
            IUMWWeaponBossVisuals.DrawLine(Main.spriteBatch, start, end, Color.White * (active ? 0.48f * opacity : 0.32f), active ? 5f : 2f);
            return false;
        }
    }

    public sealed class IUMWWeaponSummonCore : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetDefaults()
        {
            Projectile.width = 48;
            Projectile.height = 48;
            Projectile.hostile = false;
            Projectile.friendly = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 168;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
        }

        public override bool? CanDamage() => false;

        public override void AI()
        {
            Color color = IUMWWeaponBossVisuals.UnpackColor(Projectile.ai[0]);
            Player target = Main.player[Player.FindClosest(Projectile.Center, 1, 1)];
            if (target.active && !target.dead)
            {
                float angle = Projectile.ai[2] + Projectile.localAI[0] * 0.025f;
                Vector2 desired = target.Center + angle.ToRotationVector2() * 260f + new Vector2(0f, -80f);
                Projectile.velocity = Vector2.Lerp(Projectile.velocity, (desired - Projectile.Center) * 0.06f, 0.08f);
            }

            Projectile.localAI[0]++;
            Projectile.rotation += 0.08f;
            Lighting.AddLight(Projectile.Center, color.ToVector3() * 0.52f);

            if (Main.netMode != NetmodeID.MultiplayerClient && Projectile.localAI[0] % 36f == 12f && target.active && !target.dead)
            {
                Vector2 velocity = IUMWWeaponBossVisuals.SafeDirection(Projectile.Center, target.Center, Vector2.UnitY) * 7.5f;
                Projectile.NewProjectile(
                    Projectile.GetSource_FromAI(),
                    Projectile.Center,
                    velocity,
                    ModContent.ProjectileType<IUMWWeaponHostileBolt>(),
                    Projectile.damage,
                    0f,
                    Main.myPlayer,
                    0f,
                    Projectile.ai[0],
                    0.018f);
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Color color = IUMWWeaponBossVisuals.UnpackColor(Projectile.ai[0]);
            Vector2 center = Projectile.Center - Main.screenPosition;
            Texture2D pixel = TextureAssets.MagicPixel.Value;
            float pulse = 0.8f + 0.2f * MathF.Sin(Main.GlobalTimeWrappedHourly * 7f + Projectile.whoAmI);

            Rectangle outer = new((int)center.X - 20, (int)center.Y - 20, 40, 40);
            Rectangle inner = new((int)center.X - 9, (int)center.Y - 9, 18, 18);
            Main.spriteBatch.Draw(pixel, outer, color * (0.16f * pulse));
            Main.spriteBatch.Draw(pixel, inner, Color.White * 0.72f);

            for (int i = 0; i < 4; i++)
            {
                Vector2 spoke = (Projectile.rotation + MathHelper.PiOver2 * i).ToRotationVector2() * 28f;
                IUMWWeaponBossVisuals.DrawLine(Main.spriteBatch, Projectile.Center - spoke, Projectile.Center + spoke, color * 0.62f, 2f);
            }

            return false;
        }
    }
}
