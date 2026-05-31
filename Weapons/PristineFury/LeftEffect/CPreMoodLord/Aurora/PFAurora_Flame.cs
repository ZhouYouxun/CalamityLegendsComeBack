using CalamityMod.Buffs.DamageOverTime;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.PristineFury.LeftEffect
{
    internal sealed class PFAurora_Flame : ModProjectile, ILocalizedModType
    {
        private Color ThemeColor => PFLeftEffectRules.GetThemeColor(Projectile, new Color(255, 224, 92));
        private float BeamLength => Projectile.ai[0];
        private int HoldoutIndex => (int)Projectile.ai[1];
        private Vector2 Direction => Projectile.velocity.SafeNormalize(Vector2.UnitX);
        private Vector2 BeamEnd => Projectile.Center + Direction * BeamLength;

        public new string LocalizationCategory => "Projectiles.PristineFury";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 16;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 12;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 8;
        }

        public override bool ShouldUpdatePosition() => false;

        public override void AI()
        {
            if (!Main.projectile.IndexInRange(HoldoutIndex) || !Main.projectile[HoldoutIndex].active || Main.projectile[HoldoutIndex].ModProjectile is not NewLegendPristineFuryHoldOut holdout || holdout.CurrentMark != PristineFuryMark.Aurora)
            {
                Projectile.Kill();
                return;
            }

            Projectile.timeLeft = System.Math.Min(Projectile.timeLeft, 2);
            Projectile.rotation = Direction.ToRotation();
            DelegateMethods.v3_1 = ThemeColor.ToVector3() * 0.82f;
            Utils.PlotTileLine(Projectile.Center, BeamEnd, 20f, DelegateMethods.CastLight);
            EmitWeldingEffects();
        }

        private void EmitWeldingEffects()
        {
            if (Main.dedServ)
                return;

            for (int i = 0; i < 7; i++)
            {
                Vector2 velocity = Main.rand.NextVector2Circular(9f, 9f);
                GeneralParticleHandler.SpawnParticle(new SparkParticle(BeamEnd, velocity, true, Main.rand.Next(10, 18), Main.rand.NextFloat(0.75f, 1.25f), Color.Lerp(ThemeColor, Color.White, Main.rand.NextFloat(0.22f, 0.72f))));
            }

            if (Main.rand.NextBool(2))
            {
                float completion = Main.rand.NextFloat();
                Vector2 point = Projectile.Center + Direction * BeamLength * completion;
                GeneralParticleHandler.SpawnParticle(new CustomSpark(point, Direction * 0.2f, "CalamityMod/Particles/BloomLineSoftEdge", false, 8, 0.08f, ThemeColor, new Vector2(0.5f, 1.8f), true, true));
            }
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            float collisionPoint = 0f;
            return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), Projectile.Center, BeamEnd, 22f, ref collisionPoint);
        }

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            bool atWeldingPoint = Vector2.Distance(target.Center, BeamEnd) <= 96f + target.Size.Length() * 0.25f;
            modifiers.SourceDamage *= atWeldingPoint ? 6.8f : 0.16f;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) =>
            target.AddBuff(ModContent.BuffType<HolyFlames>(), 240);

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D pixel = TextureAssets.MagicPixel.Value;
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Color theme = ThemeColor with { A = 0 };
            PFLeftEffectRules.BeginAdditive();
            DrawLine(pixel, Projectile.Center, BeamEnd, theme * 0.28f, 30f);
            DrawLine(pixel, Projectile.Center, BeamEnd, theme * 0.62f, 13f);
            DrawLine(pixel, Projectile.Center, BeamEnd, Color.White with { A = 0 } * 0.78f, 4f);
            Main.EntitySpriteDraw(bloom, BeamEnd - Main.screenPosition, null, theme * 0.88f, Projectile.rotation, bloom.Size() * 0.5f, 0.68f, SpriteEffects.None, 0);
            Main.EntitySpriteDraw(bloom, BeamEnd - Main.screenPosition, null, Color.White with { A = 0 } * 0.68f, Projectile.rotation, bloom.Size() * 0.5f, 0.34f, SpriteEffects.None, 0);
            PFLeftEffectRules.EndAdditive();
            return false;
        }

        private static void DrawLine(Texture2D pixel, Vector2 start, Vector2 end, Color color, float width)
        {
            Vector2 edge = end - start;
            Main.EntitySpriteDraw(pixel, start - Main.screenPosition, new Rectangle(0, 0, 1, 1), color, edge.ToRotation(), new Vector2(0f, 0.5f), new Vector2(edge.Length(), width), SpriteEffects.None, 0);
        }
    }
}
