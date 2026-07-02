using System;
using CalamityLegendsComeBack.Accssory.TS;
using CalamityLegendsComeBack.Weapons.A_Dev.AzureThunder.ZhuangFangYiPet;
using CalamityMod;
using CalamityMod.Graphics.Primitives;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Dev.AzureThunder
{
    internal sealed class AzureThunderHarmonyImpactMark : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.AzureThunder";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private const int Lifetime = 14;

        private int TargetIndex => (int)Projectile.ai[0];
        private float VisualScale => Projectile.ai[1] <= 0f ? 1f : Projectile.ai[1];

        public override void SetDefaults()
        {
            Projectile.width = 16;
            Projectile.height = 16;
            Projectile.friendly = false;
            Projectile.hostile = false;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = Lifetime;
        }

        public override bool? CanDamage() => false;

        public override void AI()
        {
            if (Main.npc.IndexInRange(TargetIndex))
            {
                NPC target = Main.npc[TargetIndex];
                if (target.active && !target.dontTakeDamage)
                    Projectile.Center = target.Center;
            }

            Projectile.velocity = Vector2.Zero;
            Lighting.AddLight(Projectile.Center, new Vector3(0.15f, 0.95f, 0.78f) * 0.55f);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            float elapsed = Lifetime - Projectile.timeLeft;
            float lifeProgress = elapsed / Lifetime;
            float fade = Utils.GetLerpValue(0f, 3f, elapsed, true) * Utils.GetLerpValue(0f, 4f, Projectile.timeLeft, true);
            float shrink = MathHelper.Lerp(1.55f, 0.28f, lifeProgress);
            float expand = MathHelper.Lerp(0.72f, 1.95f, lifeProgress);
            float pulse = 0.5f + 0.5f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 22f + Projectile.identity * 0.37f);
            float baseRotation = elapsed * 0.34f + Projectile.identity * 0.07f;

            Texture2D thinSquare = ModContent.Request<Texture2D>("CalamityMod/Particles/GlowSquareParticleBig").Value;
            Texture2D thickSquare = ModContent.Request<Texture2D>("CalamityMod/Particles/GlowSquareParticleThick").Value;
            Texture2D triangle = ModContent.Request<Texture2D>("CalamityMod/Particles/GlowTriangle").Value;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;

            Color teal = new(70, 255, 220, 0);
            Color green = new(95, 255, 142, 0);
            Color pale = new(230, 255, 244, 0);
            Color gold = new(255, 235, 136, 0);

            Main.spriteBatch.EnterShaderRegion(BlendState.Additive);
            Main.EntitySpriteDraw(thinSquare, drawPosition, null, Color.Lerp(teal, pale, 0.32f + pulse * 0.38f) * fade * 0.56f, MathHelper.PiOver4 + baseRotation, thinSquare.Size() * 0.5f, VisualScale * expand * 0.88f, SpriteEffects.None);
            Main.EntitySpriteDraw(thinSquare, drawPosition, null, Color.Lerp(green, pale, 0.2f + pulse * 0.45f) * fade * 0.42f, -MathHelper.PiOver4 - baseRotation * 1.35f, thinSquare.Size() * 0.5f, VisualScale * shrink * 1.08f, SpriteEffects.None);
            Main.EntitySpriteDraw(thickSquare, drawPosition, null, Color.Lerp(gold, teal, 0.45f) * fade * 0.32f, baseRotation * 0.72f, thickSquare.Size() * 0.5f, VisualScale * MathHelper.Lerp(0.58f, 1.26f, lifeProgress), SpriteEffects.None);

            for (int i = 0; i < 4; i++)
            {
                float angle = MathHelper.PiOver2 * i + baseRotation * (i % 2 == 0 ? 1.15f : -0.95f);
                Vector2 offset = angle.ToRotationVector2() * VisualScale * (12f + pulse * 8f);
                Main.EntitySpriteDraw(
                    triangle,
                    drawPosition + offset,
                    null,
                    Color.Lerp(green, pale, i / 4f) * fade * 0.24f,
                    angle + MathHelper.PiOver4,
                    triangle.Size() * 0.5f,
                    VisualScale * MathHelper.Lerp(0.96f, 0.42f, lifeProgress),
                    SpriteEffects.None);
            }

            Main.spriteBatch.ExitShaderRegion();
            return false;
        }
    }

    internal sealed class AzureThunderFinalJudgementBolt : ModProjectile, ILocalizedModType
    {
        private static readonly Color Teal = new(58, 255, 214);
        private static readonly Color Green = new(92, 255, 154);
        private static readonly Color Pale = new(232, 255, 246);

        public new string LocalizationCategory => "Projectiles.AzureThunder";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private int TargetIndex => (int)Projectile.ai[0];
        private Vector2 StoredImpactPosition => new(Projectile.ai[1], Projectile.ai[2]);

        private int timer;
        private bool exploding;
        private bool burstSpawned;
        private Vector2 impactPosition;

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.DrawScreenCheckFluff[Type] = 3000;
        }

        public override void SetDefaults()
        {
            Projectile.width = 90;
            Projectile.height = 90;
            Projectile.friendly = false;
            Projectile.hostile = false;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 96;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }

        public override bool? CanDamage() => exploding && timer <= 9;

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            if (!exploding)
                return false;

            float collisionPoint = 0f;
            bool pillarHit = Collision.CheckAABBvLineCollision(
                targetHitbox.TopLeft(),
                targetHitbox.Size(),
                Projectile.Center - Vector2.UnitY * 720f,
                Projectile.Center + Vector2.UnitY * 110f,
                82f,
                ref collisionPoint);

            return pillarHit || CalamityUtils.CircularHitboxCollision(Projectile.Center, 260f, targetHitbox);
        }

        public override void AI()
        {
            timer++;

            if (impactPosition == Vector2.Zero)
            {
                impactPosition = ResolveImpactPosition();
                Projectile.Center = impactPosition - Vector2.UnitY * 920f;
                Projectile.velocity = Vector2.UnitY * 58f;
            }

            impactPosition = ResolveImpactPosition();

            if (!exploding)
            {
                Vector2 aimPoint = impactPosition - Vector2.UnitY * 80f;
                Projectile.velocity.X = MathHelper.Lerp(Projectile.velocity.X, (aimPoint.X - Projectile.Center.X) * 0.1f, 0.2f);
                Projectile.velocity.Y = MathHelper.Lerp(Projectile.velocity.Y, 76f, 0.12f);
                Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;

                SpawnFallingVfx();
                Lighting.AddLight(Projectile.Center, Teal.ToVector3() * 0.7f);

                if (Projectile.Center.Y >= impactPosition.Y - 90f || timer >= 24)
                    BeginExplosion(impactPosition);

                return;
            }

            Projectile.velocity = Vector2.Zero;
            Projectile.Center = impactPosition;
            Projectile.friendly = true;
            Projectile.width = 520;
            Projectile.height = 520;
            Projectile.Center = impactPosition;
            Projectile.rotation += 0.035f;
            Lighting.AddLight(Projectile.Center, new Vector3(0.18f, 1f, 0.82f) * 1.35f);

            if (!burstSpawned)
            {
                burstSpawned = true;
                SpawnImpactBurst();
            }

            if (timer % 3 == 0)
                SpawnUpwardResidue();

            if (timer >= 28)
                Projectile.Kill();
        }

        private Vector2 ResolveImpactPosition()
        {
            if (Main.npc.IndexInRange(TargetIndex))
            {
                NPC target = Main.npc[TargetIndex];
                if (target.active && target.CanBeChasedBy(Projectile))
                    return target.Center;
            }

            return StoredImpactPosition == Vector2.Zero ? Projectile.Center + Vector2.UnitY * 760f : StoredImpactPosition;
        }

        private void BeginExplosion(Vector2 center)
        {
            exploding = true;
            timer = 0;
            impactPosition = center;
            Projectile.Center = center;
            Projectile.velocity = Vector2.Zero;
            Projectile.friendly = true;
            Projectile.netUpdate = true;

            SoundEngine.PlaySound(new SoundStyle("CalamityMod/Sounds/Custom/Yharon/YharonInfernado") { Volume = 0.5f, Pitch = 0.42f, MaxInstances = 6 }, center);
            SoundEngine.PlaySound(SoundID.Item122 with { Volume = 0.92f, Pitch = 0.22f, MaxInstances = 4 }, center);
        }

        private void SpawnFallingVfx()
        {
            if (Main.dedServ)
                return;

            Vector2 beamDirection = Vector2.UnitY;
            Vector2 normal = beamDirection.RotatedBy(MathHelper.PiOver2);
            for (int i = 0; i < 3; i++)
            {
                Vector2 position = Projectile.Center - Vector2.UnitY * Main.rand.NextFloat(20f, 260f) + normal * Main.rand.NextFloat(-58f, 58f);
                Vector2 velocity = Vector2.UnitY * Main.rand.NextFloat(7f, 18f) + normal * Main.rand.NextFloat(-1.2f, 1.2f);
                Color color = Main.rand.NextBool(3) ? Pale : (Main.rand.NextBool() ? Teal : Green);
                GeneralParticleHandler.SpawnParticle(new LineParticle(position, velocity, false, Main.rand.Next(14, 24), Main.rand.NextFloat(0.62f, 1.1f), color));
            }

            if (Main.rand.NextBool(2))
            {
                Vector2 sidePosition = Vector2.Lerp(Projectile.Center, impactPosition, Main.rand.NextFloat(0.15f, 0.85f));
                sidePosition += normal * Main.rand.NextFloat(-122f, 122f);
                GeneralParticleHandler.SpawnParticle(new CustomSpark(
                    sidePosition,
                    -Vector2.UnitY * Main.rand.NextFloat(0.7f, 2.6f),
                    "CalamityMod/Particles/ForwardSmear",
                    false,
                    Main.rand.Next(10, 17),
                    Main.rand.NextFloat(0.17f, 0.26f),
                    Main.rand.NextBool() ? Teal : Green,
                    Vector2.One,
                    shrinkSpeed: 0.42f));
            }
        }

        private void SpawnImpactBurst()
        {
            if (Main.myPlayer == Projectile.owner)
            {
                AzureThunderPlayer.SpawnHarmonyHitMark(Projectile.GetSource_FromThis(), Projectile.Center, Projectile.owner, TargetIndex, 1.9f);

                int flags = AzureThunderFlatLightning.VisualOnlyFlag |
                    AzureThunderFlatLightning.BigLightningFlag |
                    AzureThunderFlatLightning.SpeedLineFlag |
                    AzureThunderFlatLightning.OneThirdVisualIntensityFlag;

                for (int i = 0; i < 12; i++)
                {
                    float angle = -MathHelper.PiOver2 + MathHelper.Lerp(-0.78f, 0.78f, i / 11f) + Main.rand.NextFloat(-0.08f, 0.08f);
                    Vector2 direction = angle.ToRotationVector2();
                    AzureThunderPlayer.SpawnFlatLightning(
                        Projectile.GetSource_FromThis(),
                        Projectile.Center + Main.rand.NextVector2Circular(36f, 26f),
                        direction,
                        1,
                        0f,
                        Projectile.owner,
                        1.45f,
                        flags);
                }
            }

            AzureThunderPlayer.SpawnUpwardThunderBoltBurst(Projectile.Center, 16, 2.45f);
            SpawnUpwardResidue();

            if (Main.dedServ)
                return;

            for (int i = 0; i < 60; i++)
            {
                Vector2 velocity = -Vector2.UnitY.RotatedByRandom(0.9f) * Main.rand.NextFloat(4f, 19f);
                Color color = Main.rand.NextBool(4) ? Color.White : Color.Lerp(Teal, Green, Main.rand.NextFloat());
                GeneralParticleHandler.SpawnParticle(new LineParticle(
                    Projectile.Center + Main.rand.NextVector2Circular(42f, 24f),
                    velocity,
                    false,
                    Main.rand.Next(16, 30),
                    Main.rand.NextFloat(0.55f, 1.25f),
                    color));
            }

            for (int i = 0; i < 18; i++)
            {
                Dust dust = Dust.NewDustPerfect(
                    Projectile.Center + Main.rand.NextVector2Circular(180f, 80f),
                    DustID.FireworksRGB,
                    -Vector2.UnitY.RotatedByRandom(1.05f) * Main.rand.NextFloat(3f, 12f),
                    0,
                    Main.rand.NextBool() ? Teal : Green,
                    Main.rand.NextFloat(0.9f, 1.65f));
                dust.noGravity = true;
            }
        }

        private void SpawnUpwardResidue()
        {
            if (Main.dedServ)
                return;

            for (int i = 0; i < 8; i++)
            {
                Vector2 position = Projectile.Center + Main.rand.NextVector2Circular(110f, 42f) - Vector2.UnitY * Main.rand.NextFloat(0f, 260f);
                Vector2 velocity = -Vector2.UnitY * Main.rand.NextFloat(5f, 15f) + Main.rand.NextVector2Circular(1.8f, 1.8f);
                GeneralParticleHandler.SpawnParticle(new CustomSpark(
                    position,
                    velocity,
                    "CalamityMod/Particles/DrainLineBloom",
                    false,
                    Main.rand.Next(22, 38),
                    Main.rand.NextFloat(0.8f, 1.35f),
                    Main.rand.NextBool(3) ? Pale : Teal,
                    new Vector2(0.72f, 3.6f),
                    true,
                    true,
                    shrinkSpeed: 0.62f));
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.Electrified, 360);
            AzureThunderPlayer.ApplyUltimateDot(target, 300);
            AzureThunderAccessoryPlayer.ApplyAzureThunderAccessoryOnHit(Projectile, target);

            if (Main.myPlayer == Projectile.owner)
            {
                AzureThunderPlayer.SpawnHarmonyHitMark(Projectile.GetSource_FromThis(), target.Center, Projectile.owner, target.whoAmI, 1.35f);
                Main.player[Projectile.owner].GetModPlayer<ZhuangFangYiPetPlayer>().QueueStrongAttackCandidate(target);
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (impactPosition == Vector2.Zero)
                impactPosition = ResolveImpactPosition();

            float opacity = exploding
                ? Utils.GetLerpValue(28f, 0f, timer, true)
                : Utils.GetLerpValue(0f, 10f, timer, true);

            DrawJudgementPillar(opacity);
            DrawMagicCircles(opacity);
            DrawGuideLines(opacity);
            return false;
        }

        private void DrawJudgementPillar(float opacity)
        {
            Vector2 bottom = exploding ? Projectile.Center : impactPosition;
            Vector2 top = exploding ? Projectile.Center - Vector2.UnitY * 780f : Projectile.Center - Vector2.UnitY * 120f;
            Vector2[] points =
            {
                bottom + Vector2.UnitX * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 9f) * 12f,
                Vector2.Lerp(bottom, top, 0.33f) + Vector2.UnitX * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 8f + 1.7f) * 22f,
                Vector2.Lerp(bottom, top, 0.66f) - Vector2.UnitX * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 7f + 0.4f) * 18f,
                top
            };

            Main.spriteBatch.EnterShaderRegion();
            GameShaders.Misc["CalamityMod:Bordernado"].UseSaturation(-0.12f);
            GameShaders.Misc["CalamityMod:Bordernado"].UseOpacity(opacity * (exploding ? 0.78f : 0.42f));
            GameShaders.Misc["CalamityMod:Bordernado"].SetShaderTexture(ModContent.Request<Texture2D>("Terraria/Images/Misc/Perlin"));
            PrimitiveRenderer.RenderTrail(points, new PrimitiveSettings(PillarWidth, PillarColor, shader: GameShaders.Misc["CalamityMod:Bordernado"]), 72);
            Main.spriteBatch.ExitShaderRegion();
        }

        private float PillarWidth(float completionRatio, Vector2 vertexPosition)
        {
            float bulge = (float)Math.Sin(completionRatio * MathHelper.Pi);
            float baseWidth = exploding ? 76f : 42f;
            return baseWidth * MathHelper.Lerp(0.55f, 1.24f, bulge);
        }

        private Color PillarColor(float completionRatio, Vector2 vertexPosition)
        {
            return Color.Lerp(Pale, Color.Lerp(Teal, Green, 0.55f), completionRatio * 0.82f);
        }

        private void DrawMagicCircles(float opacity)
        {
            Texture2D circle = ModContent.Request<Texture2D>("CalamityLegendsComeBack/Texture/KsTexture/circle_04").Value;
            Vector2 drawPosition = impactPosition - Main.screenPosition;
            float pulse = 1f + (float)Math.Sin(Main.GlobalTimeWrappedHourly * 6f) * 0.06f;
            float explosionScale = exploding ? MathHelper.Lerp(1.35f, 2.35f, Utils.GetLerpValue(0f, 22f, timer, true)) : 1.15f;

            Main.spriteBatch.EnterShaderRegion(BlendState.Additive);
            for (int i = 0; i < 4; i++)
            {
                float rotation = Main.GlobalTimeWrappedHourly * (i % 2 == 0 ? 0.65f : -0.82f) + MathHelper.PiOver4 * i;
                Color color = (i % 2 == 0 ? Teal : Green) with { A = 0 };
                Main.EntitySpriteDraw(
                    circle,
                    drawPosition,
                    null,
                    color * opacity * (0.18f + i * 0.055f),
                    rotation,
                    circle.Size() * 0.5f,
                    (explosionScale + i * 0.2f) * pulse,
                    SpriteEffects.None);
            }
            Main.spriteBatch.ExitShaderRegion();
        }

        private void DrawGuideLines(float opacity)
        {
            Texture2D pixel = TextureAssets.MagicPixel.Value;
            Vector2 center = impactPosition - Main.screenPosition;
            float height = exploding ? 600f : Math.Max(120f, impactPosition.Y - Projectile.Center.Y);

            Main.spriteBatch.EnterShaderRegion(BlendState.Additive);
            for (int i = -2; i <= 2; i++)
            {
                float side = i * 34f + (float)Math.Sin(Main.GlobalTimeWrappedHourly * 5f + i) * 12f;
                Vector2 start = center + new Vector2(side, -height);
                Vector2 end = center + new Vector2(side * 0.35f, 60f);
                Vector2 edge = end - start;
                if (edge.LengthSquared() <= 1f)
                    continue;

                Color color = (i % 2 == 0 ? Teal : Green) with { A = 0 };
                Main.EntitySpriteDraw(
                    pixel,
                    (start + end) * 0.5f,
                    new Rectangle(0, 0, 1, 1),
                    color * opacity * 0.24f,
                    edge.ToRotation(),
                    new Vector2(0.5f),
                    new Vector2(edge.Length(), 2.4f + Math.Abs(i)),
                    SpriteEffects.None);
            }
            Main.spriteBatch.ExitShaderRegion();
        }
    }
}
