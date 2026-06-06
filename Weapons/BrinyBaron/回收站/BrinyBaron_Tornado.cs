//using Microsoft.Xna.Framework;
//using Microsoft.Xna.Framework.Graphics;
//using Terraria;
//using Terraria.GameContent;
//using Terraria.ID;
//using Terraria.ModLoader;
//
//namespace CalamityLegendsComeBack.Weapons.BrinyBaron.CommonAttack
//{
//    internal class BrinyBaron_Tornado : ModProjectile, ILocalizedModType
//    {
//        public new string LocalizationCategory => "Projectiles.BrinyBaron";
//        public override string Texture => "CalamityMod/Projectiles/TornadoProj";
//
//        public override void SetDefaults()
//        {
//            Projectile.width = 128;
//            Projectile.height = 176;
//            Projectile.friendly = true;
//            Projectile.penetrate = -1;
//            Projectile.timeLeft = 120;
//            Projectile.tileCollide = false;
//            Projectile.ignoreWater = true;
//            Projectile.DamageType = DamageClass.Melee;
//            Projectile.usesLocalNPCImmunity = true;
//            Projectile.localNPCHitCooldown = 16;
//        }
//
//        public override void OnSpawn(Terraria.DataStructures.IEntitySource source)
//        {
//            if (Projectile.ai[0] > 0f)
//                Projectile.timeLeft = (int)Projectile.ai[0];
//        }
//
//        public override void AI()
//        {
//            Projectile.rotation += 0.12f;
//            Projectile.Opacity = Utils.GetLerpValue(0f, 18f, Projectile.timeLeft, true);
//            Lighting.AddLight(Projectile.Center, 0.04f, 0.16f, 0.22f);
//
//            if (Main.rand.NextBool(2))
//            {
//                float height = Main.rand.NextFloat(-Projectile.height * 0.44f, Projectile.height * 0.36f);
//                float radius = MathHelper.Lerp(Projectile.width * 0.12f, Projectile.width * 0.45f, Main.rand.NextFloat());
//                Vector2 offset = new Vector2(radius, height).RotatedBy(Main.GlobalTimeWrappedHourly * 7f + height * 0.04f);
//                Dust dust = Dust.NewDustPerfect(
//                    Projectile.Center + offset,
//                    Main.rand.NextBool(3) ? DustID.Frost : DustID.Water,
//                    offset.SafeNormalize(Vector2.UnitY).RotatedBy(MathHelper.PiOver2) * Main.rand.NextFloat(0.6f, 2.2f),
//                    100,
//                    new Color(100, 210, 255),
//                    Main.rand.NextFloat(0.85f, 1.2f));
//                dust.noGravity = true;
//            }
//        }
//
//        public override bool PreDraw(ref Color lightColor)
//        {
//            Texture2D texture = TextureAssets.Projectile[Type].Value;
//            Vector2 origin = texture.Size() * 0.5f;
//            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
//            float pulse = 1f + 0.08f * (float)System.Math.Sin(Main.GlobalTimeWrappedHourly * 12f + Projectile.identity);
//            Color drawColor = Projectile.GetAlpha(lightColor) * Projectile.Opacity;
//
//            Main.EntitySpriteDraw(
//                texture,
//                drawPosition,
//                null,
//                drawColor,
//                Projectile.rotation,
//                origin,
//                Projectile.scale * pulse,
//                SpriteEffects.None,
//                0);
//
//            Main.EntitySpriteDraw(
//                texture,
//                drawPosition,
//                null,
//                new Color(80, 210, 255, 0) * 0.38f * Projectile.Opacity,
//                -Projectile.rotation * 0.7f,
//                origin,
//                Projectile.scale * 1.15f,
//                SpriteEffects.FlipHorizontally,
//                0);
//
//            return false;
//        }
//    }
//}
