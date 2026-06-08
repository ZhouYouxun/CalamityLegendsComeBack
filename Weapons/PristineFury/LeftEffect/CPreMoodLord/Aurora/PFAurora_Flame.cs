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
            Projectile.width = Projectile.height = 64;
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
            Utils.PlotTileLine(Projectile.Center, BeamEnd, 64f, DelegateMethods.CastLight);
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

            for (int i = 0; i < 2; i++)
            {
                float completion = Main.rand.NextFloat();
                Vector2 point = Projectile.Center + Direction * BeamLength * completion;
                GeneralParticleHandler.SpawnParticle(new CustomSpark(point, Direction * 0.2f, "CalamityMod/Particles/ThinEndedLine", false, 10, Main.rand.NextFloat(0.1f, 0.14f), ThemeColor, new Vector2(0.5f, 1.8f), true, true));
            }

            // Spawn SparkParticles along the laser length
            if (Main.rand.NextBool(2))
            {
                for (float offset = 0f; offset < BeamLength; offset += Main.rand.NextFloat(100f, 200f))
                {
                    Vector2 sparkPos = Projectile.Center + Direction * offset + Main.rand.NextVector2Circular(8f, 8f);
                    Vector2 sparkVelocity = Direction * Main.rand.NextFloat(2f, 5f);
                    GeneralParticleHandler.SpawnParticle(new SparkParticle(
                        sparkPos,
                        sparkVelocity,
                        false,
                        5,
                        Main.rand.NextFloat(0.4f, 1.0f),
                        Color.Lerp(ThemeColor, Color.White, Main.rand.NextFloat(0.1f, 0.4f))
                    ));
                }
            }
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            float collisionPoint = 0f;
            return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), Projectile.Center, BeamEnd, 64f, ref collisionPoint);
        }

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            bool atWeldingPoint = Vector2.Distance(target.Center, BeamEnd) <= 96f + target.Size.Length() * 0.25f;
            modifiers.SourceDamage *= atWeldingPoint ? 6.8f : 0.16f;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(ModContent.BuffType<HolyFlames>(), 240);
            if (Main.rand.NextBool(4))
            {
                Terraria.Audio.SoundEngine.PlaySound(new Terraria.Audio.SoundStyle("CalamityMod/Sounds/Custom/AstralBeaconOrbPulse") { Volume = 0.22f, Pitch = 0.12f }, target.Center);
            }
        }

        public override void OnKill(int timeLeft)
        {
            Terraria.Audio.SoundEngine.PlaySound(new Terraria.Audio.SoundStyle("CalamityMod/Sounds/Custom/AstrumDeus/DeusMineExplode") { Volume = 0.45f, Pitch = -0.1f }, Projectile.Center);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Texture2D bloomRing = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomRing").Value;
            Color theme = ThemeColor;
            float opacity = Projectile.Opacity;
            Vector2 start = Projectile.Center - Main.screenPosition;
            Vector2 end = BeamEnd - Main.screenPosition;

            PFLeftEffectRules.BeginAdditive();

            Texture2D startTex = ModContent.Request<Texture2D>("CalamityMod/Projectiles/Boss/ProvidenceHolyRay", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
            Texture2D midTex = ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/Lasers/ProvidenceHolyRayMid", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
            Texture2D endTex = ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/Lasers/ProvidenceHolyRayEnd", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;

            float drawScale = Projectile.scale * 1.1f;
            float rotation = Direction.ToRotation() - MathHelper.PiOver2;
            Vector2 scaleVec = new Vector2(drawScale, drawScale);

            // Draw start piece
            Main.spriteBatch.Draw(startTex, start, null, theme * opacity, rotation, startTex.Size() / 2f, scaleVec, SpriteEffects.None, 0f);

            float currentLength = BeamLength;
            currentLength -= (startTex.Height / 2 + endTex.Height) * drawScale;
            Vector2 center = Projectile.Center + Direction * drawScale * startTex.Height / 2f;

            if (currentLength > 0f)
            {
                float lengthDrawn = 0f;
                int frameHeight = 36;
                int frameY = frameHeight * (Projectile.timeLeft / 3 % 4);
                Rectangle sourceRect = new Rectangle(0, frameY, midTex.Width, frameHeight);

                while (lengthDrawn + 1f < currentLength)
                {
                    if (currentLength - lengthDrawn < frameHeight * drawScale)
                    {
                        sourceRect.Height = (int)((currentLength - lengthDrawn) / drawScale);
                    }
                    if (sourceRect.Height <= 0)
                        break;

                    Main.spriteBatch.Draw(midTex, center - Main.screenPosition, sourceRect, theme * opacity, rotation, new Vector2(sourceRect.Width / 2f, 0f), scaleVec, SpriteEffects.None, 0f);
                    lengthDrawn += sourceRect.Height * drawScale;
                    center += Direction * sourceRect.Height * drawScale;

                    sourceRect.Y += frameHeight;
                    if (sourceRect.Y + sourceRect.Height > midTex.Height)
                    {
                        sourceRect.Y = 0;
                    }
                }
            }

            Vector2 endPos = center - Main.screenPosition;
            Main.spriteBatch.Draw(endTex, endPos, null, theme * opacity, rotation, new Vector2(endTex.Width / 2f, 0f), scaleVec, SpriteEffects.None, 0f);

            // Origin glow
            Main.EntitySpriteDraw(bloom, start, null, theme * (0.86f * opacity), 0f, bloom.Size() * 0.5f, 0.5f * drawScale, SpriteEffects.None, 0);
            Main.EntitySpriteDraw(bloomRing, start, null, theme * (0.65f * opacity), 0f, bloomRing.Size() * 0.5f, 0.8f * drawScale, SpriteEffects.None, 0);

            // End glow
            Main.EntitySpriteDraw(bloom, end, null, theme * (0.9f * opacity), 0f, bloom.Size() * 0.5f, 0.7f * drawScale, SpriteEffects.None, 0);
            Main.EntitySpriteDraw(bloom, end, null, (Color.White with { A = 0 }) * (0.52f * opacity), 0f, bloom.Size() * 0.5f, 0.3f * drawScale, SpriteEffects.None, 0);

            PFLeftEffectRules.EndAdditive();
            return false;
        }
    }
}

