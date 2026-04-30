using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.YharimsCrystal.MainAttack
{
    public class YC_ModeSwitchGlyph : ModProjectile, ILocalizedModType
    {
        private const int Lifetime = 34;

        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";
        public new string LocalizationCategory => "Projectiles.YharimsCrystal";

        private YharimsCrystalAttackMode Mode => (YharimsCrystalAttackMode)(int)Projectile.ai[0];
        private float CycleDirection => Projectile.ai[1] == 0f ? 1f : Projectile.ai[1];
        private float Progress => 1f - Projectile.timeLeft / (float)Lifetime;

        public override void SetDefaults()
        {
            Projectile.width = 2;
            Projectile.height = 2;
            Projectile.friendly = false;
            Projectile.hostile = false;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.hide = true;
            Projectile.timeLeft = Lifetime;
        }

        public override bool ShouldUpdatePosition() => false;
        public override bool? CanDamage() => false;

        public override void OnSpawn(Terraria.DataStructures.IEntitySource source)
        {
            SoundEngine.PlaySound(SoundID.MenuTick with { Volume = 0.6f, Pitch = 0.15f * CycleDirection }, Projectile.Center);
        }

        public override void AI()
        {
            Player owner = Main.player[Projectile.owner];
            if (!owner.active || owner.dead)
            {
                Projectile.Kill();
                return;
            }

            Projectile.Center = owner.Center;

            if (Main.dedServ || Main.GameUpdateCount % 3 != 0)
                return;

            Color color = GetModeColor(Mode);
            float radius = MathHelper.Lerp(24f, 58f, Progress);
            Vector2 offset = (Main.GlobalTimeWrappedHourly * 5.2f * CycleDirection + Main.rand.NextFloat(MathHelper.TwoPi)).ToRotationVector2() * radius;
            Dust dust = Dust.NewDustPerfect(Projectile.Center + offset, DustID.GoldFlame, -offset.SafeNormalize(Vector2.UnitY) * 0.8f, 0, color, Main.rand.NextFloat(0.85f, 1.2f));
            dust.noGravity = true;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Projectile.timeLeft <= 1)
                return false;

            Texture2D line = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomLineFade").Value;
            Texture2D circle = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Vector2[] points = GetModePoints(Mode);
            Color color = GetModeColor(Mode) with { A = 0 };
            float opacity = Utils.GetLerpValue(0f, 0.25f, Progress, true) * Utils.GetLerpValue(1f, 0.62f, Progress, true);
            float scale = MathHelper.Lerp(0.58f, 1.08f, Utils.GetLerpValue(0f, 0.45f, Progress, true));
            float rotation = CycleDirection * MathHelper.Lerp(-0.55f, 0.28f, Progress);
            Vector2 center = Projectile.Center - Main.screenPosition;

            for (int i = 0; i < points.Length; i++)
            {
                Vector2 current = points[i].RotatedBy(rotation) * scale;
                Vector2 next = points[(i + 1) % points.Length].RotatedBy(rotation) * scale;
                DrawLine(line, center + current, center + next, color * (0.56f * opacity), 0.55f);
            }

            for (int i = 0; i < points.Length; i++)
            {
                Vector2 position = center + points[i].RotatedBy(rotation) * scale;
                float pulse = 0.18f + 0.05f * (float)System.Math.Sin(Main.GlobalTimeWrappedHourly * 9f + i);
                Main.EntitySpriteDraw(circle, position, null, color * opacity, 0f, circle.Size() * 0.5f, pulse, SpriteEffects.None, 0);
                Main.EntitySpriteDraw(circle, position, null, (Color.White with { A = 0 }) * (opacity * 0.35f), 0f, circle.Size() * 0.5f, pulse * 0.42f, SpriteEffects.None, 0);
            }

            return false;
        }

        private static void DrawLine(Texture2D texture, Vector2 start, Vector2 end, Color color, float width)
        {
            Vector2 difference = end - start;
            float length = difference.Length();
            if (length <= 0.01f)
                return;

            Main.EntitySpriteDraw(
                texture,
                start + difference * 0.5f,
                null,
                color,
                difference.ToRotation() + MathHelper.PiOver2,
                texture.Size() * 0.5f,
                new Vector2(width, length / texture.Height),
                SpriteEffects.None,
                0);
        }

        public static Color GetModeColor(YharimsCrystalAttackMode mode)
        {
            return mode switch
            {
                YharimsCrystalAttackMode.Drill => new Color(255, 190, 95),
                YharimsCrystalAttackMode.Flamethrower => new Color(255, 104, 58),
                YharimsCrystalAttackMode.Warships => new Color(110, 225, 255),
                _ => new Color(255, 232, 150)
            };
        }

        private static Vector2[] GetModePoints(YharimsCrystalAttackMode mode)
        {
            return mode switch
            {
                YharimsCrystalAttackMode.Drill => new[]
                {
                    new Vector2(-18f, -30f),
                    new Vector2(22f, -22f),
                    new Vector2(36f, 12f),
                    new Vector2(4f, 38f),
                    new Vector2(-30f, 18f)
                },
                YharimsCrystalAttackMode.Flamethrower => new[]
                {
                    new Vector2(-34f, -16f),
                    new Vector2(-6f, -36f),
                    new Vector2(32f, -12f),
                    new Vector2(18f, 30f),
                    new Vector2(-24f, 30f),
                    new Vector2(6f, 2f)
                },
                YharimsCrystalAttackMode.Warships => new[]
                {
                    new Vector2(0f, -42f),
                    new Vector2(36f, -14f),
                    new Vector2(28f, 30f),
                    new Vector2(-28f, 30f),
                    new Vector2(-36f, -14f)
                },
                _ => new[]
                {
                    new Vector2(-42f, 0f),
                    new Vector2(-18f, -32f),
                    new Vector2(18f, -32f),
                    new Vector2(42f, 0f),
                    new Vector2(18f, 32f),
                    new Vector2(-18f, 32f)
                }
            };
        }
    }
}
