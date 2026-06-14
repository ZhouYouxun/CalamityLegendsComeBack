using CalamityMod;
using CalamityMod.Buffs.DamageOverTime;
using CalamityMod.Particles;
using CalamityMod.Projectiles.Typeless;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.PristineFury.LeftEffect
{
    internal sealed class PFGoliath_ReaperDrone : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.PristineFury";
        public override string Texture => "CalamityMod/Projectiles/DraedonsArsenal/MountedScannerSummon";

        private const int CurveFrames = 30;
        private const float HomingSpeed = 16.8f;
        private const float TargetRange = 680f;
        private const float ExplosionVisualScale = 0.67f;
        private const float WingVisualScale = 0.2f;
        private bool hasHit;

        private ref float CurveDirection => ref Projectile.ai[0];
        private ref float Phase => ref Projectile.ai[1];
        private ref float Timer => ref Projectile.localAI[0];
        private Color ThemeColor => PFLeftEffectRules.GetThemeColor(Projectile, new Color(255, 224, 92));

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 18;
            ProjectileID.Sets.TrailingMode[Type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = 30;
            Projectile.height = 24;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 170;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
            Projectile.scale = 0.74f;
        }

        public override void AI()
        {
            Timer++;
            Vector2 currentDirection = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            float currentSpeed = Projectile.velocity.Length();

            if (Timer <= CurveFrames)
            {
                float curveStrength = Utils.GetLerpValue(0f, CurveFrames, Timer, true);
                float wobble = (float)Math.Sin(Timer * 0.38f + Phase) * 0.012f;
                Projectile.velocity = Projectile.velocity.RotatedBy(CurveDirection * 0.022f * curveStrength + wobble) * 0.94f;
            }
            else
            {
                NPC target = FindTarget();
                if (target != null)
                {
                    Vector2 sideBias = currentDirection.RotatedBy(MathHelper.PiOver2) *
                        (float)Math.Sin(Timer * 0.08f + Phase) *
                        MathHelper.Lerp(46f, 12f, Utils.GetLerpValue(CurveFrames, CurveFrames + 76f, Timer, true));
                    Vector2 desiredDirection = Projectile.SafeDirectionTo(target.Center + sideBias, currentDirection);
                    Vector2 desiredVelocity = desiredDirection * MathHelper.Lerp(currentSpeed, HomingSpeed, 0.24f);
                    Projectile.velocity = Vector2.Lerp(Projectile.velocity, desiredVelocity, 0.192f + 0.054f * Math.Abs((float)Math.Sin(Phase)));
                }
                else
                    Projectile.velocity *= 0.996f;
            }

            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
            Projectile.Opacity = Utils.GetLerpValue(0f, 10f, Timer, true) * Utils.GetLerpValue(0f, 20f, Projectile.timeLeft, true);
            Lighting.AddLight(Projectile.Center, ThemeColor.ToVector3() * (0.46f + Projectile.Opacity * 0.34f));

            if (!Main.dedServ)
                SpawnFlightEffects();
        }

        private NPC FindTarget()
        {
            NPC closest = null;
            float bestDistance = TargetRange;
            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!npc.CanBeChasedBy(Projectile))
                    continue;

                float distance = Projectile.Distance(npc.Center);
                if (distance >= bestDistance)
                    continue;

                bestDistance = distance;
                closest = npc;
            }

            return closest;
        }

        private void SpawnFlightEffects()
        {
            Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            Vector2 side = direction.RotatedBy(MathHelper.PiOver2);
            Color theme = Color.Lerp(ThemeColor, new Color(180, 255, 80), 0.18f);

            if (Main.rand.NextBool(2))
            {
                Dust dust = Dust.NewDustPerfect(
                    Projectile.Center - direction * Main.rand.NextFloat(4f, 14f) + side * Main.rand.NextFloat(-6f, 6f),
                    Main.rand.NextBool(4) ? DustID.GreenTorch : DustID.GoldFlame,
                    -direction * Main.rand.NextFloat(0.6f, 1.8f) + side * Main.rand.NextFloat(-0.45f, 0.45f),
                    0,
                    Main.rand.NextBool(3) ? Color.Lerp(theme, new Color(160, 255, 74), 0.22f) : theme,
                    Main.rand.NextFloat(0.55f, 1f));

                dust.noGravity = true;
                dust.fadeIn = 1.1f;
            }

            if ((int)Timer % 3 == 0)
                GeneralParticleHandler.SpawnParticle(new GlowOrbParticle(Projectile.Center - direction * Main.rand.NextFloat(4f, 14f) + side * Main.rand.NextFloat(-8f, 8f), -direction * Main.rand.NextFloat(0.45f, 1.2f), false, Main.rand.Next(8, 14), Main.rand.NextFloat(0.18f, 0.34f), Color.Lerp(theme, new Color(180, 255, 80), 0.18f), true, false, true));
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) =>
            CalamityUtils.CircularHitboxCollision(Projectile.Center, 18f * Projectile.scale, targetHitbox);

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            hasHit = true;
            target.AddBuff(ModContent.BuffType<Plague>(), 210);
            SoundEngine.PlaySound(new SoundStyle("CalamityMod/Sounds/Custom/PlagueSounds/PBGAttackSwitchShort") { Volume = 0.38f, Pitch = 0.08f }, target.Center);
        }

        public override void OnKill(int timeLeft)
        {
            if (!hasHit)
            {
                SpawnMissEffects();
                return;
            }

            SpawnPlagueBurst();
        }

        private void SpawnPlagueBurst()
        {
            Vector2 center = Projectile.Center;
            int sourceDamage = Projectile.damage;
            Color theme = Color.Lerp(ThemeColor, new Color(180, 255, 80), 0.12f);

            SoundEngine.PlaySound(new SoundStyle("CalamityMod/Sounds/Custom/PlagueSounds/PlagueBoom" + Main.rand.Next(1, 5)) { Volume = 0.46f, Pitch = 0.15f }, center);

            if (Projectile.owner == Main.myPlayer)
            {
                Projectile.ExpandHitboxBy((int)(30 * ExplosionVisualScale));
                Projectile.damage = Math.Max(1, (int)(sourceDamage * 0.58f));
                Projectile.penetrate = -1;
                Projectile.Damage();

                for (int i = 0; i < 2; i++)
                {
                    Vector2 beeVelocity = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(4.2f, 8.2f);
                    int bee = Projectile.NewProjectile(
                        Projectile.GetSource_FromThis(),
                        center,
                        beeVelocity,
                        ModContent.ProjectileType<BasicPlagueBee>(),
                        Math.Max(1, (int)(sourceDamage * 0.05f)),
                        0f,
                        Projectile.owner);

                    if (bee >= 0 && bee < Main.maxProjectiles)
                    {
                        Main.projectile[bee].penetrate = 1;
                        Main.projectile[bee].DamageType = DamageClass.Ranged;
                    }
                }
            }

            if (Main.dedServ)
                return;

            GeneralParticleHandler.SpawnParticle(new CustomPulse(
                center,
                Vector2.Zero,
                theme * 0.82f,
                "CalamityMod/Particles/FlameExplosion",
                Vector2.One,
                Main.rand.NextFloat(-10f, 10f),
                0.05f,
                0.17f * ExplosionVisualScale,
                16));

            GeneralParticleHandler.SpawnParticle(new CustomPulse(
                center,
                Vector2.Zero,
                Color.Lerp(theme, new Color(155, 255, 70), 0.25f) * 0.65f,
                "CalamityMod/Particles/BloomCircle",
                Vector2.One,
                Main.rand.NextFloat(-10f, 10f),
                0.06f,
                0.23f * ExplosionVisualScale,
                14,
                true));

            for (int i = 0; i < 18; i++)
            {
                Vector2 velocity = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(2.4f, 7.2f);
                GeneralParticleHandler.SpawnParticle(new GlowOrbParticle(
                    center + Main.rand.NextVector2Circular(8f, 8f),
                    velocity,
                    false,
                    Main.rand.Next(18, 32),
                    Main.rand.NextFloat(0.35f, 0.72f) * ExplosionVisualScale,
                    Main.rand.NextBool(4) ? Color.Lerp(theme, new Color(180, 255, 80), 0.24f) : theme,
                    true,
                    true));
            }

            for (int i = 0; i < 8; i++)
            {
                GeneralParticleHandler.SpawnParticle(new HeavySmokeParticle(
                    center + Main.rand.NextVector2Circular(12f, 12f),
                    Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(1.2f, 3.8f),
                    Color.Lerp(theme, Color.DarkOliveGreen, Main.rand.NextFloat(0.2f, 0.48f)),
                    Main.rand.Next(18, 32),
                    Main.rand.NextFloat(0.46f, 0.92f) * ExplosionVisualScale,
                    0.55f,
                    Main.rand.NextFloat(-0.07f, 0.07f),
                    glowing: true));
            }
        }

        private void SpawnMissEffects()
        {
            if (Main.dedServ)
                return;

            Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            Color theme = ThemeColor;
            for (int i = 0; i < 6; i++)
            {
                Dust dust = Dust.NewDustPerfect(
                    Projectile.Center - direction * Main.rand.NextFloat(4f, 18f) + Main.rand.NextVector2Circular(8f, 8f),
                    DustID.GoldFlame,
                    -direction.RotatedByRandom(0.35f) * Main.rand.NextFloat(0.5f, 1.6f),
                    0,
                    theme,
                    Main.rand.NextFloat(0.45f, 0.82f));

                dust.noGravity = true;
            }
        }





        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D body = TextureAssets.Projectile[Type].Value;
            Texture2D frontWing = ModContent.Request<Texture2D>("CalamityMod/Particles/XykWingOrange1").Value;
            Texture2D backWing = ModContent.Request<Texture2D>("CalamityMod/Particles/XykWingOrange2").Value;
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;

            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            Vector2 forward = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            Vector2 side = forward.RotatedBy(MathHelper.PiOver2);
            Vector2 origin = body.Size() * 0.5f;

            // 不要 A = 0，避免翅膀直接变成皇帝的新翅膀
            Color theme = Color.Lerp(ThemeColor, new Color(180, 255, 80), 0.16f) * Projectile.Opacity;
            Color bodyColor = Color.Lerp(ThemeColor, new Color(180, 255, 80), 0.08f) * Projectile.Opacity;

            PFLeftEffectRules.BeginAdditive();

            // 身体外发光
            Main.EntitySpriteDraw(
                bloom,
                drawPosition,
                null,
                theme * 0.32f,
                Projectile.rotation,
                bloom.Size() * 0.5f,
                new Vector2(0.42f, 0.28f) * Projectile.scale,
                SpriteEffects.None,
                0f);

            // 残影
            for (int i = 1; i < Projectile.oldPos.Length; i += 3)
            {
                if (Projectile.oldPos[i] == Vector2.Zero)
                    continue;

                float opacity = (1f - i / (float)Projectile.oldPos.Length) * 0.28f * Projectile.Opacity;
                Vector2 afterimagePosition = Projectile.oldPos[i] + Projectile.Size * 0.5f - Main.screenPosition;

                Main.EntitySpriteDraw(
                    body,
                    afterimagePosition,
                    null,
                    theme * opacity,
                    Projectile.rotation,
                    origin,
                    Projectile.scale * 0.96f,
                    SpriteEffects.None,
                    0f);
            }

            // 四片振动翅膀：前一对 + 后一对
            DrawWingPair(frontWing, drawPosition + forward * 7f, forward, side, 0f, 1.15f, theme);
            DrawWingPair(backWing, drawPosition - forward * 10f, forward, side, MathHelper.Pi, 0.95f, theme * 0.82f);

            PFLeftEffectRules.EndAdditive();

            // 本体最后画，避免被翅膀盖乱
            Main.EntitySpriteDraw(
                body,
                drawPosition,
                null,
                bodyColor,
                Projectile.rotation,
                origin,
                Projectile.scale,
                SpriteEffects.None,
                0f);

            return false;
        }

        private void DrawWingPair(Texture2D wing, Vector2 root, Vector2 forward, Vector2 side, float phaseOffset, float size, Color color)
        {
            float time = Main.GlobalTimeWrappedHourly * 88f + Projectile.identity * 0.41f + phaseOffset;
            float flap = (float)Math.Sin(time);
            float snap = Math.Abs(flap);

            for (int sideSign = -1; sideSign <= 1; sideSign += 2)
            {
                // 翅膀根部位置
                Vector2 drawPosition =
                    root +
                    side * sideSign * (12f + snap * 5f) -
                    forward * (2f + snap * 2f);

                // 翅膀朝外展开，并随时间轻微震动
                Vector2 wingDirection =
                    (side * sideSign * (1.6f + snap * 0.45f) -
                    forward * (0.35f - flap * 0.18f)).SafeNormalize(side * sideSign);

                float rotation = wingDirection.ToRotation() - MathHelper.PiOver2;

                // 原版太小，这里明显放大
                Vector2 wingScale = new Vector2(
                    0.42f * size * Projectile.scale,
                    0.26f * size * Projectile.scale * (0.85f + snap * 0.25f)) * WingVisualScale;

                SpriteEffects effects = sideSign < 0 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;

                // 主翅膀
                Main.EntitySpriteDraw(
                    wing,
                    drawPosition,
                    null,
                    color * 0.95f,
                    rotation,
                    new Vector2(wing.Width * 0.5f, 0f),
                    wingScale,
                    effects,
                    0f);

                // 高频振动残影
                Main.EntitySpriteDraw(
                    wing,
                    drawPosition - forward * 3f + side * sideSign * flap * 3f,
                    null,
                    Color.White * 0.22f * Projectile.Opacity,
                    rotation + flap * 0.08f,
                    new Vector2(wing.Width * 0.5f, 0f),
                    wingScale * new Vector2(0.75f, 0.82f),
                    effects,
                    0f);
            }
        }






    }
}
