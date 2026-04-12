using CalamityLegendsComeBack.Weapons.BrinyBaron.SkillD_SuperDash;
using CalamityMod;
using CalamityMod.Graphics.Primitives;
using CalamityMod.Projectiles.Typeless;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.BrinyBaron.CommonAttack
{
    internal class BBSwing_Wave : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles";

        private const int BaseSize = 200;

        private int lifeTimer;
        private float initialSpeed;

        private int SpawnStage => (int)Projectile.localAI[0];
        private BB_Balance.WaveProfile waveProfile;
        private float StageScale => waveProfile.SizeScale;
        private float StageIntensity => 1f + SpawnStage * 0.26f;

        public override string Texture => "Terraria/Images/Projectile_0";

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 8;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = BaseSize;
            Projectile.height = BaseSize;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.penetrate = -1;
            Projectile.light = 0.45f;
            Projectile.scale = 0.9f;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = true;
            Projectile.extraUpdates = 3;
            Projectile.timeLeft = 90 * Projectile.extraUpdates;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 18;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (lifeTimer < 10)
                return true;

            SpriteBatch sb = Main.spriteBatch;
            Texture2D tex = ModContent.Request<Texture2D>(Projectile.ModProjectile.Texture).Value;
            Vector2 origin = tex.Size() * 0.5f;

            Color[] palette = new Color[]
            {
                new Color(220, 250, 255),
                new Color(115, 215, 255),
                new Color(48, 146, 235),
                new Color(12, 54, 110),
            };

            sb.End();
            sb.Begin(SpriteSortMode.Deferred, BlendState.Additive);

            Vector2 forward = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            int trailLength = Projectile.oldPos.Length;
            float segmentLength = Projectile.width * 0.09f;
            Vector2[] shiftedOldPos = new Vector2[trailLength];

            for (int i = 0; i < trailLength; i++)
                shiftedOldPos[i] = Projectile.Center - forward * segmentLength * i;

            GameShaders.Misc["CalamityMod:SideStreakTrail"].UseImage1("Images/Misc/Perlin");

            float baseWidth = Projectile.width;

            float WidthFunc(float t, Vector2 v)
            {
                float shape = (float)Math.Sin(t * MathHelper.Pi);
                shape = (float)Math.Pow(shape, 0.6f);
                shape = MathHelper.Lerp(0.25f, 1f, shape);
                return baseWidth * shape;
            }

            Color ColorFunc(float t, Vector2 v)
            {
                int idx = (int)(t * (palette.Length - 1));
                idx = Utils.Clamp(idx, 0, palette.Length - 1);

                Color c = palette[idx];
                c *= (1f - t) * Projectile.Opacity * 1.2f;
                c.A = 0;
                return c;
            }

            Vector2 offsetFunc(float t, Vector2 v) => Vector2.Zero;

            PrimitiveRenderer.RenderTrail(
                shiftedOldPos,
                new PrimitiveSettings(
                    WidthFunc,
                    ColorFunc,
                    offsetFunc,
                    shader: GameShaders.Misc["CalamityMod:SideStreakTrail"]),
                60);

            sb.End();
            sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);

            Main.spriteBatch.Draw(
                tex,
                Projectile.Center - Main.screenPosition,
                null,
                lightColor,
                Projectile.rotation,
                origin,
                Projectile.scale,
                SpriteEffects.None,
                0f);

            return false;
        }

        public override void OnSpawn(Terraria.DataStructures.IEntitySource source)
        {
            initialSpeed = Projectile.velocity.Length();
            waveProfile = BB_Balance.GetWaveProfile();
            Projectile.localAI[0] = waveProfile.GrowthTier;
            ApplyStageStats();
        }

        public override void AI()
        {
            lifeTimer++;

            //Projectile.velocity *= waveProfile.SpeedDrag;

            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver4;
            Lighting.AddLight(Projectile.Center, new Vector3(0.08f, 0.34f, 0.52f) * (1f + SpawnStage * 0.12f));
            BBSwing_Wave_Effect.SpawnFlightEffects(Projectile, lifeTimer, SpawnStage, StageIntensity, initialSpeed);

            Vector2 forward = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            Vector2 right = forward.RotatedBy(MathHelper.PiOver2);
            float visualRadius = Projectile.width * 0.5f;
            TrySpawnTrailingStars(forward, right, visualRadius);
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            BBSwing_Wave_Effect.SpawnHitEffects(Projectile, SpawnStage, StageIntensity);
        }

        public override void OnKill(int timeLeft)
        {
        }

        private void ApplyStageStats()
        {
            Vector2 center = Projectile.Center;
            int size = (int)(BaseSize * StageScale);
            Projectile.width = size;
            Projectile.height = size;
            Projectile.scale = 0.9f + SpawnStage * 0.06f;
            Projectile.tileCollide = waveProfile.TileCollide;
            Projectile.Center = center;
        }

        private void TrySpawnTrailingStars(Vector2 forward, Vector2 right, float visualRadius)
        {
            if (SpawnStage < 2 || Projectile.numUpdates != 0 || Main.myPlayer != Projectile.owner)
                return;

            int starInterval = SpawnStage >= 3 ? 7 : 10;
            if (lifeTimer % starInterval != 0)
                return;

            int spawnCount = SpawnStage >= 3 && Main.rand.NextBool(3) ? 2 : 1;
            for (int i = 0; i < spawnCount; i++)
            {
                Vector2 spawnPos =
                    Projectile.Center -
                    forward * Main.rand.NextFloat(visualRadius * 0.18f, visualRadius * 0.42f) +
                    right * Main.rand.NextFloat(-visualRadius * 0.38f, visualRadius * 0.38f);

                Vector2 launchVelocity =
                    (-forward * Main.rand.NextFloat(6.8f, 10.2f) +
                    right * Main.rand.NextFloat(-1.4f, 1.4f) -
                    Vector2.UnitY * Main.rand.NextFloat(0.2f, 1.1f)).SafeNormalize(-forward) *
                    Main.rand.NextFloat(6.2f, 9.4f);

                int starDamage = Math.Max(1, (int)(Projectile.damage * waveProfile.TrailingStarDamageFactor));
                Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    spawnPos,
                    launchVelocity,
                    ModContent.ProjectileType<BBSD_Star>(),
                    starDamage,
                    Projectile.knockBack * 0.35f,
                    Projectile.owner);
            }
        }
    }
}
