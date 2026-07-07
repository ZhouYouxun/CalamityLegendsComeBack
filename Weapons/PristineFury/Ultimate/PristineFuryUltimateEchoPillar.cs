using CalamityLegendsComeBack.Accssory.PF;
using CalamityLegendsComeBack.Weapons.PristineFury.LeftEffect;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.PristineFury
{
    // 大招「劫火重燃」群芒齐发阶段的单枚回响立柱：先于地面预警，再轰下一道圣焰光柱，
    // 命中后直接把目标纯化等级顶到当前上限，呼应百合系列的纯化机制。
    // 视觉上固定使用大招自身的金色圣焰主题，不再依附任何左键印记。
    internal sealed class PristineFuryUltimateEchoPillar : ModProjectile, ILocalizedModType
    {
        internal const int ColumnWidth = 46;
        internal const int ColumnHeight = 320;
        private const int TelegraphFrames = 10;
        private const int ImpactFrames = 10;
        private const int FadeFrames = 16;

        private static readonly Color HolyGold = new(255, 224, 92);

        public new string LocalizationCategory => "Projectiles.PristineFury";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private ref float Timer => ref Projectile.localAI[0];

        public override void SetDefaults()
        {
            Projectile.width = ColumnWidth;
            Projectile.height = ColumnHeight;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
            Projectile.timeLeft = TelegraphFrames + ImpactFrames + FadeFrames;
        }

        public override bool? CanDamage() => Timer >= TelegraphFrames && Timer < TelegraphFrames + ImpactFrames;

        public override void AI()
        {
            Timer++;
            Vector2 groundSpot = Projectile.Bottom;

            if (Timer == 1f && !Main.dedServ)
            {
                GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(
                    groundSpot, Vector2.Zero, HolyGold * 0.9f,
                    new Vector2(1.6f, 1.6f), 0f, 0.9f, 0.08f, TelegraphFrames + 4));
            }

            if (Timer < TelegraphFrames)
            {
                SpawnTelegraphMotes(groundSpot, Timer / (float)TelegraphFrames);
                Lighting.AddLight(groundSpot, HolyGold.ToVector3() * 0.5f * (Timer / (float)TelegraphFrames));
                return;
            }

            if (Timer == TelegraphFrames)
                SpawnSlamBurst(groundSpot);

            if (Timer < TelegraphFrames + ImpactFrames)
            {
                SpawnColumnFlames(groundSpot);
                for (int step = 0; step <= ColumnHeight; step += 48)
                    Lighting.AddLight(groundSpot - Vector2.UnitY * step, HolyGold.ToVector3() * 1.1f);
            }
            else
            {
                Lighting.AddLight(groundSpot, HolyGold.ToVector3() * 0.6f);
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.OnFire3, 180);

            Player owner = Main.player[Projectile.owner];
            int cap = owner.GetModPlayer<PFAccessoryPlayer>().PurificationCap;
            PFPurificationGlobalNPC purification = target.GetGlobalNPC<PFPurificationGlobalNPC>();
            if (purification.PurificationLevel < cap)
                purification.PurificationLevel = cap;
        }

        private void SpawnTelegraphMotes(Vector2 groundSpot, float progress)
        {
            if (Main.dedServ)
                return;

            for (int i = 0; i < 3; i++)
            {
                float angle = MathHelper.TwoPi * Main.rand.NextFloat();
                Vector2 pos = groundSpot + angle.ToRotationVector2() * MathHelper.Lerp(34f, 6f, progress);
                Dust dust = Dust.NewDustPerfect(pos, DustID.AncientLight, -Vector2.UnitY * Main.rand.NextFloat(1.5f, 3.5f), 0, HolyGold, Main.rand.NextFloat(0.9f, 1.4f));
                dust.noGravity = true;
            }
        }

        private void SpawnSlamBurst(Vector2 groundSpot)
        {
            if (Main.dedServ)
                return;

            SoundEngine.PlaySound(SoundID.Item122 with { Volume = 0.55f, Pitch = 0.25f, MaxInstances = 6 }, groundSpot);
            SoundEngine.PlaySound(new SoundStyle("CalamityMod/Sounds/Custom/Providence/ProvidenceHolyBlastShoot") { Volume = 0.4f, Pitch = 0.3f, MaxInstances = 4 }, groundSpot);

            for (int i = 0; i < 14; i++)
            {
                float angle = MathHelper.TwoPi * i / 14f;
                Vector2 vel = angle.ToRotationVector2() * Main.rand.NextFloat(3f, 7f);
                GeneralParticleHandler.SpawnParticle(new CritSpark(groundSpot, vel, Color.Lerp(HolyGold, Color.White, 0.3f), Color.White, 0.9f, 20));
            }

            for (int i = 0; i < 2; i++)
            {
                GeneralParticleHandler.SpawnParticle(new CustomSpark(
                    groundSpot, Main.rand.NextVector2Circular(1.5f, 1.5f) - Vector2.UnitY * 2f,
                    "CalamityMod/Particles/GlowSquareParticle", false, 30, Main.rand.NextFloat(0.4f, 0.65f),
                    HolyGold, Vector2.One, true, true, MathHelper.PiOver4, spin: Main.rand.NextFloat(-0.1f, 0.1f)));
            }
        }

        private void SpawnColumnFlames(Vector2 groundSpot)
        {
            if (Main.dedServ)
                return;

            for (int i = 0; i < 2; i++)
            {
                Vector2 pos = groundSpot + new Vector2(Main.rand.NextFloat(-18f, 18f), -Main.rand.NextFloat(0f, 300f));
                Dust dust = Dust.NewDustPerfect(pos, DustID.AncientLight, new Vector2(Main.rand.NextFloat(-0.4f, 0.4f), -Main.rand.NextFloat(2f, 5f)), 0, HolyGold, Main.rand.NextFloat(1f, 1.8f));
                dust.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.dedServ)
                return false;

            Vector2 groundSpot = Projectile.Bottom;
            float beamHeight;
            float widthScale;
            float opacity;

            if (Timer < TelegraphFrames)
            {
                float t = Timer / (float)TelegraphFrames;
                beamHeight = MathHelper.Lerp(24f, ColumnHeight * 0.55f, t);
                widthScale = MathHelper.Lerp(0.03f, 0.16f, t);
                opacity = t;
            }
            else if (Timer < TelegraphFrames + ImpactFrames)
            {
                float t = (Timer - TelegraphFrames) / (float)ImpactFrames;
                beamHeight = ColumnHeight;
                widthScale = MathHelper.Lerp(1.35f, 0.95f, MathHelper.Clamp(t * 2f, 0f, 1f));
                opacity = 1f;
            }
            else
            {
                float t = (Timer - TelegraphFrames - ImpactFrames) / (float)FadeFrames;
                beamHeight = ColumnHeight;
                widthScale = MathHelper.Lerp(0.95f, 0f, t);
                opacity = MathHelper.Lerp(1f, 0f, t);
            }

            if (widthScale <= 0.01f || opacity <= 0.01f)
                return false;

            DrawHolyBeam(groundSpot, beamHeight, widthScale, opacity);
            return false;
        }

        // 用圣光射线贴图沿竖直方向拼出一根从地面升起的光柱，替代纯粒子堆砌的火焰效果。
        private void DrawHolyBeam(Vector2 groundSpot, float beamHeight, float widthScale, float opacity)
        {
            Texture2D startTex = ModContent.Request<Texture2D>("CalamityMod/Projectiles/Boss/ProvidenceHolyRay", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
            Texture2D midTex = ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/Lasers/ProvidenceHolyRayMid", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
            Texture2D endTex = ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/Lasers/ProvidenceHolyRayEnd", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Texture2D bloomRing = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomRing").Value;

            Vector2 beamVector = -Vector2.UnitY;
            Vector2 start = groundSpot - Main.screenPosition;
            float rotation = beamVector.ToRotation() - MathHelper.PiOver2;
            float pulse = 0.9f + 0.1f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 11f + Projectile.identity);
            float drawScale = widthScale * pulse;
            Color theme = (HolyGold with { A = 0 }) * opacity;
            Color white = (Color.White with { A = 0 }) * opacity;

            PFLeftEffectRules.BeginAdditive();

            Main.spriteBatch.Draw(startTex, start, null, theme, rotation, startTex.Size() / 2f, drawScale, SpriteEffects.None, 0f);

            float currentLength = beamHeight - (startTex.Height / 2f + endTex.Height) * drawScale;
            Vector2 center = groundSpot + beamVector * drawScale * startTex.Height / 2f;

            if (currentLength > 0f)
            {
                float lengthDrawn = 0f;
                const int frameHeight = 36;
                int frameY = frameHeight * ((int)(Main.GameUpdateCount / 3) % 4);
                Rectangle sourceRect = new(0, frameY, midTex.Width, frameHeight);

                while (lengthDrawn + 1f < currentLength)
                {
                    if (currentLength - lengthDrawn < frameHeight * drawScale)
                        sourceRect.Height = (int)((currentLength - lengthDrawn) / drawScale);

                    if (sourceRect.Height <= 0)
                        break;

                    Main.spriteBatch.Draw(midTex, center - Main.screenPosition, sourceRect, theme, rotation, new Vector2(sourceRect.Width / 2f, 0f), drawScale, SpriteEffects.None, 0f);
                    lengthDrawn += sourceRect.Height * drawScale;
                    center += beamVector * sourceRect.Height * drawScale;

                    sourceRect.Y += frameHeight;
                    if (sourceRect.Y + sourceRect.Height > midTex.Height)
                        sourceRect.Y = 0;
                }
            }

            Vector2 endPos = center - Main.screenPosition;
            Main.spriteBatch.Draw(endTex, endPos, null, theme, rotation, new Vector2(endTex.Width / 2f, 0f), drawScale, SpriteEffects.None, 0f);

            Main.EntitySpriteDraw(bloom, start, null, theme * 0.9f, 0f, bloom.Size() * 0.5f, 1.65f * drawScale, SpriteEffects.None, 0);
            Main.EntitySpriteDraw(bloomRing, start, null, theme * 0.65f, 0f, bloomRing.Size() * 0.5f, 2.55f * drawScale, SpriteEffects.None, 0);
            Main.EntitySpriteDraw(bloom, endPos, null, white * 0.5f, 0f, bloom.Size() * 0.5f, 0.84f * drawScale, SpriteEffects.None, 0);

            PFLeftEffectRules.EndAdditive();
        }
    }
}
