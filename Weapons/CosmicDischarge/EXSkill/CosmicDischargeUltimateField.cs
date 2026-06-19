using System;
using CalamityMod;
using CalamityMod.Graphics.Primitives;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.CosmicDischarge
{
    public class CosmicDischargeUltimateField : ModProjectile, ILocalizedModType
    {
        private const int ChargeDuration = 60;
        private const float RiftBurstRadius = 25f * 16f;
        private const float FieldRadius = 8f * 16f;

        public new string LocalizationCategory => "Projectiles.Melee";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private ref float Time => ref Projectile.ai[0];
        private Player Owner => Main.player[Projectile.owner];

        public override void SetDefaults()
        {
            Projectile.width = (int)(FieldRadius * 2f);
            Projectile.height = (int)(FieldRadius * 2f);
            Projectile.penetrate = -1;
            Projectile.timeLeft = CosmicDischargePlayer.UltimateFieldDuration + ChargeDuration;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.friendly = false;
            Projectile.hostile = false;
        }

        public override bool? CanDamage() => false;
        public override bool ShouldUpdatePosition() => false;

        public override void AI()
        {
            if (!Owner.active || Owner.dead)
            {
                Projectile.Kill();
                return;
            }

            Time++;
            Projectile.Center = Owner.Center;
            Projectile.Opacity = Utils.GetLerpValue(0f, 18f, Time, true) * Utils.GetLerpValue(0f, 25f, Projectile.timeLeft, true);
            Owner.AddBuff(ModContent.BuffType<CosmicDischargeUltimateGuardBuff>(), 4);

            if (Time == 1f)
            {
                SoundEngine.PlaySound(new SoundStyle("CalamityMod/Sounds/Custom/DevourerRiftBuilding") { Volume = 0.78f, Pitch = -0.1f }, Projectile.Center);
                SoundEngine.PlaySound(SoundID.Item92 with { Volume = 0.5f, Pitch = -0.18f }, Projectile.Center);
            }

            if (Time <= ChargeDuration)
            {
                CosmicDischargeCommon.SpawnUltimateCharge(Owner, Time);
                ApplyScreenShake(Time / ChargeDuration * 5f);
                return;
            }

            if (Time == ChargeDuration + 1)
                ApplyRiftBurst();

            SlowEnemiesInField();
            SpawnFieldDust();
        }

        private void ApplyRiftBurst()
        {
            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!npc.active || npc.friendly || npc.dontTakeDamage || Vector2.DistanceSquared(npc.Center, Projectile.Center) > RiftBurstRadius * RiftBurstRadius)
                    continue;

                CosmicDischargeCommon.ApplyDoGDebuffs(npc, 10 * 60);
            }

            if (!Main.dedServ)
            {
                SoundEngine.PlaySound(new SoundStyle("CalamityMod/Sounds/Custom/DevourerRiftOpen") { Volume = 0.82f, Pitch = -0.05f }, Projectile.Center);
                SoundEngine.PlaySound(new SoundStyle("CalamityMod/Sounds/Custom/DevourerSpawn") { Volume = 0.46f, Pitch = 0.18f }, Projectile.Center);
                CosmicDischargeCommon.SpawnUltimateBurst(Projectile.GetSource_FromThis(), Owner, Projectile.Center, Projectile.damage, Projectile.knockBack);
                ApplyScreenShake(18f);
            }
        }

        private void SlowEnemiesInField()
        {
            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!npc.active || npc.friendly || npc.dontTakeDamage)
                    continue;

                npc.velocity *= 0.85f;
                CosmicDischargeCommon.ApplyDoGDebuffs(npc, 90);
            }

            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile proj = Main.projectile[i];
                if (!proj.active || proj.friendly || !proj.hostile)
                    continue;

                proj.velocity *= 0.85f;
            }
        }

        private void SpawnFieldDust()
        {
            if (Main.dedServ)
                return;

            CosmicDischargeCommon.SpawnUltimateFieldIdle(Projectile.Center, FieldRadius, Time);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Texture2D portal = ModContent.Request<Texture2D>("CalamityMod/Projectiles/Melee/StreamGougePortal").Value;
            Vector2 origin = bloom.Size() * 0.5f;
            Vector2 portalOrigin = portal.Size() * 0.5f;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            float pulse = 1f + 0.035f * MathF.Sin(Main.GlobalTimeWrappedHourly * MathHelper.TwoPi);
            float portalRotation = Main.GlobalTimeWrappedHourly * 5.5f + Projectile.identity * 0.18f;
            float chargeInterpolant = Utils.GetLerpValue(0f, ChargeDuration, Time, true);
            float fieldOpacity = Time <= ChargeDuration ? chargeInterpolant : Projectile.Opacity;

            Main.spriteBatch.SetBlendState(BlendState.Additive);

            if (Time <= ChargeDuration)
            {
                Texture2D star = ModContent.Request<Texture2D>("CalamityMod/Projectiles/StarProj").Value;
                Main.EntitySpriteDraw(star, drawPosition, null, Color.White * 0.65f * chargeInterpolant, 0f, star.Size() * 0.5f, new Vector2(1f, 8f) * (0.45f + chargeInterpolant * 1.35f), SpriteEffects.None);
                Main.EntitySpriteDraw(star, drawPosition, null, Color.White * 0.65f * chargeInterpolant, MathHelper.PiOver2, star.Size() * 0.5f, new Vector2(1f, 5f) * (0.45f + chargeInterpolant * 1.35f), SpriteEffects.None);
                Main.EntitySpriteDraw(bloom, drawPosition, null, CosmicDischargeCommon.Transparent(CosmicDischargeCommon.DoGCyanColor) * (0.35f * chargeInterpolant), 0f, origin, (1f - chargeInterpolant) * 2f, SpriteEffects.None);
            }

            Main.EntitySpriteDraw(
                bloom,
                drawPosition,
                null,
                CosmicDischargeCommon.Transparent(CosmicDischargeCommon.DoGFuchsiaColor) * 0.15f * fieldOpacity,
                0f,
                origin,
                FieldRadius / bloom.Width * 2f * pulse,
                SpriteEffects.None);

            Main.EntitySpriteDraw(portal, drawPosition, null, Color.Black * 0.28f * fieldOpacity, portalRotation, portalOrigin, 0.42f * pulse, SpriteEffects.None);
            Main.EntitySpriteDraw(portal, drawPosition, null, CosmicDischargeCommon.Transparent(CosmicDischargeCommon.DoGCyanColor) * 0.34f * fieldOpacity, portalRotation * 0.6f, portalOrigin, 0.42f * pulse, SpriteEffects.None);
            Main.EntitySpriteDraw(portal, drawPosition, null, CosmicDischargeCommon.Transparent(CosmicDischargeCommon.DoGFuchsiaColor) * 0.34f * fieldOpacity, -portalRotation * 0.7f, portalOrigin, 0.42f * pulse, SpriteEffects.None);

            DrawDoGFireFieldRing(FieldRadius * pulse, fieldOpacity);

            Main.spriteBatch.SetBlendState(BlendState.AlphaBlend);

            return false;
        }

        private void DrawDoGFireFieldRing(float radius, float opacity)
        {
            Vector2[] ring = new Vector2[48];
            for (int i = 0; i < ring.Length; i++)
            {
                float angle = MathHelper.TwoPi * i / (ring.Length - 1) + Main.GlobalTimeWrappedHourly * 0.35f;
                ring[i] = Projectile.Center + angle.ToRotationVector2() * radius;
            }

            float OuterWidth(float completion, Vector2 _) => MathHelper.Lerp(50f, 32f, completion);
            Color OuterColor(float completion, Vector2 _) => Color.Lerp(CosmicDischargeCommon.DoGCyanColor, Color.Transparent, completion) * (0.44f * opacity);
            float InnerWidth(float completion, Vector2 _) => MathHelper.Lerp(20f, 10f, completion);
            Color InnerColor(float completion, Vector2 _) => Color.Lerp(CosmicDischargeCommon.DoGFuchsiaColor, Color.Transparent, completion) * (0.58f * opacity);

            GameShaders.Misc["CalamityMod:ImpFlameTrail"].SetShaderTexture(ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/Trails/ScarletDevilStreak"));
            PrimitiveRenderer.RenderTrail(ring, new PrimitiveSettings(OuterWidth, OuterColor, smoothen: true, pixelate: false, shader: GameShaders.Misc["CalamityMod:ImpFlameTrail"]), ring.Length + 8);

            GameShaders.Misc["CalamityMod:ImpFlameTrail"].SetShaderTexture(ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/Trails/SylvestaffStreak"));
            PrimitiveRenderer.RenderTrail(ring, new PrimitiveSettings(InnerWidth, InnerColor, smoothen: true, pixelate: false, shader: GameShaders.Misc["CalamityMod:ImpFlameTrail"]), ring.Length + 8);
        }

        private void ApplyScreenShake(float power)
        {
            if (Main.dedServ)
                return;

            float distanceFactor = Utils.GetLerpValue(1600f, 120f, Vector2.Distance(Main.LocalPlayer.Center, Projectile.Center), true);
            Main.LocalPlayer.Calamity().GeneralScreenShakePower = Math.Max(Main.LocalPlayer.Calamity().GeneralScreenShakePower, power * distanceFactor);
        }
    }
}
