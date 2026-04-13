using CalamityMod;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.BlossomFlux.TurretMode
{
    // 炮台形态的预瞄线，只负责高速绘制准星，不承担伤害判定。
    internal sealed class BFTurretAimBeam : ModProjectile
    {
        public new string LocalizationCategory => "Projectiles.BlossomFlux";
        private const float MaxBeamLength = 2200f;

        private float BeamLength
        {
            get => Projectile.localAI[0];
            set => Projectile.localAI[0] = value;
        }

        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.DrawScreenCheckFluff[Type] = 10000;
        }

        public override void SetDefaults()
        {
            Projectile.width = 10;
            Projectile.height = 10;
            Projectile.friendly = false;
            Projectile.hostile = false;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.hide = true;
            Projectile.timeLeft = 18000;
        }

        public override bool ShouldUpdatePosition() => false;
        public override bool? CanDamage() => false;

        public override void DrawBehind(
            int index,
            System.Collections.Generic.List<int> behindNPCsAndTiles,
            System.Collections.Generic.List<int> behindNPCs,
            System.Collections.Generic.List<int> behindProjectiles,
            System.Collections.Generic.List<int> overPlayers,
            System.Collections.Generic.List<int> overWiresUI)
        {
            overPlayers.Add(index);
        }

        public override void AI()
        {
            Player owner = Main.player[Projectile.owner];
            BFTurretModePlayer turretPlayer = owner.GetModPlayer<BFTurretModePlayer>();
            if (!owner.active || owner.dead || !turretPlayer.TurretModeActive || owner.HeldItem.type != ModContent.ItemType<NewLegendBlossomFlux>())
            {
                Projectile.Kill();
                return;
            }

            Vector2 mouseWorld = owner.Calamity().mouseWorld;
            if (mouseWorld == Vector2.Zero)
                mouseWorld = Main.MouseWorld;

            Vector2 direction = mouseWorld - owner.MountedCenter;
            if (direction == Vector2.Zero)
                direction = Vector2.UnitX * owner.direction;

            BeamLength = MathHelper.Clamp(direction.Length(), 48f, MaxBeamLength);
            Projectile.velocity = direction.SafeNormalize(Vector2.UnitX * owner.direction);
            Projectile.rotation = Projectile.velocity.ToRotation();
            Projectile.Center = owner.MountedCenter + Projectile.velocity * 18f;
            Projectile.timeLeft = 2;

            Lighting.AddLight(Projectile.Center, new Color(90, 255, 150).ToVector3() * 0.26f);

            if (Main.GameUpdateCount % 8 == 0)
            {
                Dust dust = Dust.NewDustPerfect(
                    Projectile.Center + Projectile.velocity * Main.rand.NextFloat(12f, 30f),
                    DustID.GemEmerald,
                    Projectile.velocity.RotatedByRandom(0.16f) * Main.rand.NextFloat(0.55f, 1.2f),
                    0,
                    new Color(120, 255, 165),
                    Main.rand.NextFloat(0.75f, 1f));
                dust.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Projectile.velocity == Vector2.Zero || BeamLength <= 0f)
                return false;

            Texture2D outer = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomLineFade").Value;
            Texture2D inner = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomLineThick").Value;
            Texture2D glow = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Vector2 start = Projectile.Center - Main.screenPosition;
            Vector2 unit = Projectile.rotation.ToRotationVector2();
            Vector2 beamCenter = start + unit * BeamLength * 0.5f;
            float beamLengthScale = BeamLength / 1000f;
            Color outerColor = new Color(90, 255, 150, 0);
            Color innerColor = new Color(220, 255, 220, 0);
            float pulse = 0.8f + 0.18f * (float)System.Math.Sin(Main.GlobalTimeWrappedHourly * 10f);

            Main.EntitySpriteDraw(
                outer,
                beamCenter,
                null,
                outerColor * 0.5f,
                Projectile.rotation + MathHelper.PiOver2,
                outer.Size() * 0.5f,
                new Vector2(2.25f * pulse, 55f * beamLengthScale) * Projectile.scale * 0.01f,
                SpriteEffects.FlipVertically,
                0f);

            Main.EntitySpriteDraw(
                inner,
                beamCenter,
                null,
                innerColor * 0.72f,
                Projectile.rotation + MathHelper.PiOver2,
                inner.Size() * 0.5f,
                new Vector2(0.36f * pulse, 55f * beamLengthScale) * Projectile.scale * 0.01f,
                SpriteEffects.FlipVertically,
                0f);

            for (int i = 0; i < 2; i++)
            {
                Main.EntitySpriteDraw(
                    glow,
                    start + unit * 7f,
                    null,
                    Color.Lerp(outerColor, Color.White with { A = 0 }, i) * 0.42f,
                    Projectile.rotation + MathHelper.PiOver2,
                    glow.Size() * 0.5f,
                    new Vector2(1.8f + 0.2f * i, 1f) * Projectile.scale * 0.03f,
                    SpriteEffects.FlipVertically,
                    0f);
            }

            return false;
        }
    }
}
