using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SHPC.Effects.CPreMoodLord.PlagueCell
{
    public class PlagueCell_Marked : ModProjectile
    {
        public new string LocalizationCategory => "Projectiles.SHPC";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private const int FadeDuration = 45;
        private const int GrowDuration = 30;

        // ai[0]: locked NPC index
        // ai[1]: big missile flag
        // ai[2]: set to 1 by the bound missile only after it impacts the locked NPC
        private bool ImpactConfirmed => Projectile.ai[2] > 0f;

        // localAI[0]: missile whoAmI + 1 (0 = not yet spawned, -1 = spawn failed)
        // localAI[1]: fade timer
        // localAI[2]: grow-in timer
        private ref float MissileSlot => ref Projectile.localAI[0];
        private ref float FadeTimer => ref Projectile.localAI[1];
        private ref float GrowTimer => ref Projectile.localAI[2];

        public override void SetDefaults()
        {
            Projectile.width = 10;
            Projectile.height = 10;
            Projectile.timeLeft = 3600;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.friendly = false;
            Projectile.hostile = false;
        }

        public override void AI()
        {
            Projectile.timeLeft = System.Math.Max(Projectile.timeLeft, FadeDuration + 2);

            int targetIndex = (int)Projectile.ai[0];
            if (!Main.npc.IndexInRange(targetIndex) || !Main.npc[targetIndex].active)
            {
                if (ImpactConfirmed)
                    AdvanceFade();
                return;
            }

            NPC target = Main.npc[targetIndex];
            Projectile.Center = target.Center;

            if (MissileSlot == 0f && Projectile.owner == Main.myPlayer)
                SpawnBoundMissile(target);

            if (FadeTimer <= 0f)
                GrowTimer = System.Math.Min(GrowTimer + 1f, GrowDuration);

            if (ImpactConfirmed)
                AdvanceFade();
        }

        private void SpawnBoundMissile(NPC target)
        {
            SoundStyle fullCharge = new("CalamityMod/Sounds/Custom/PlagueSounds/PBGAttackSwitchShort");
            SoundEngine.PlaySound(fullCharge with { Volume = 0.9f }, Projectile.Center);

            for (int i = 0; i < 12; i++)
            {
                Vector2 vel = -Vector2.UnitY * Main.rand.NextFloat(4f, 10f) + Main.rand.NextVector2Circular(0.6f, 0.6f);
                Dust dust = Dust.NewDustPerfect(Projectile.Center, DustID.GemRuby, vel);
                dust.scale = Main.rand.NextFloat(0.45f, 0.75f);
                dust.noGravity = true;
            }

            bool isBig = Projectile.ai[1] != 0f;
            Vector2 spawnPos = target.Center + new Vector2(Main.rand.NextFloat(-16f, 16f) * 16f, -36f * 16f);
            Vector2 velocity = (target.Center - spawnPos).SafeNormalize(Vector2.UnitY) * 36f;

            int projID = Projectile.NewProjectile(
                Projectile.GetSource_FromThis(),
                spawnPos,
                velocity,
                ModContent.ProjectileType<PlagueCell_Nuke>(),
                (int)(Projectile.damage * 3),
                0f,
                Projectile.owner,
                target.whoAmI,
                isBig ? 1f : 0f,
                Projectile.identity + 1f);

            if (Main.projectile.IndexInRange(projID))
            {
                Projectile missile = Main.projectile[projID];
                missile.friendly = true;
                missile.hostile = false;
                missile.DamageType = DamageClass.Magic;
                missile.tileCollide = false;
                missile.ignoreWater = true;
                missile.usesLocalNPCImmunity = true;
                missile.localNPCHitCooldown = 10;
                MissileSlot = projID + 1f;
            }
            else
            {
                MissileSlot = -1f;
            }
        }

        private void AdvanceFade()
        {
            FadeTimer++;
            if (FadeTimer >= FadeDuration)
                Projectile.Kill();
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (MissileSlot == 0f)
                return false;

            Texture2D reticle = ModContent.Request<Texture2D>("CalamityMod/Particles/DestroyerReticleTelegraph").Value;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            Vector2 origin = reticle.Size() * 0.5f;

            float progress = Utils.GetLerpValue(0f, GrowDuration, GrowTimer, true);
            float pulse = 0.88f + 0.12f * (float)System.Math.Sin(Main.GlobalTimeWrappedHourly * 11f + Projectile.identity);
            float opacity = FadeTimer > 0f ? Utils.GetLerpValue(FadeDuration, 0f, FadeTimer, true) : 1f;

            float scaleMult = Projectile.ai[1] != 0f ? 1.5f : 1f;
            float outerScale = MathHelper.Lerp(0.22f, 0.34f, progress) * pulse * scaleMult;
            float innerScale = MathHelper.Lerp(0.18f, 0.27f, progress) * pulse * scaleMult;

            Color green = new(74, 255, 92);
            Color paleGreen = new(210, 255, 218);

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(
                SpriteSortMode.Deferred,
                BlendState.Additive,
                Main.DefaultSamplerState,
                DepthStencilState.None,
                Main.Rasterizer,
                null,
                Main.GameViewMatrix.TransformationMatrix);

            Main.EntitySpriteDraw(
                reticle,
                drawPosition,
                null,
                Color.Lerp(green, paleGreen, progress * 0.35f) * 0.92f * opacity,
                Main.GlobalTimeWrappedHourly * 1.5f,
                origin,
                outerScale,
                SpriteEffects.None,
                0f);

            Main.EntitySpriteDraw(
                reticle,
                drawPosition,
                null,
                paleGreen * 0.72f * opacity,
                -Main.GlobalTimeWrappedHourly * 1.1f,
                origin,
                innerScale,
                SpriteEffects.FlipHorizontally,
                0f);

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(
                SpriteSortMode.Deferred,
                BlendState.AlphaBlend,
                Main.DefaultSamplerState,
                DepthStencilState.None,
                Main.Rasterizer,
                null,
                Main.GameViewMatrix.TransformationMatrix);

            return false;
        }

        public override bool? CanDamage() => false;
    }
}
