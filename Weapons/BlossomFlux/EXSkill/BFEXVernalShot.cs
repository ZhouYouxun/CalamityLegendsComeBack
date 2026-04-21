using System;
using CalamityMod;
using CalamityMod.Graphics.Primitives;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.BlossomFlux.EXSkill
{
    internal sealed class BFEXVernalShot : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.BlossomFlux";

        private static readonly Color TrailStartColor = new(70, 255, 132);
        private static readonly Color TrailMidColor = new(154, 255, 188);
        private static readonly Color TrailEndColor = new(226, 255, 232);

        private float HomingDelay
        {
            get => Projectile.ai[0];
            set => Projectile.ai[0] = value;
        }

        private float WobbleOffset
        {
            get => Projectile.ai[1];
            set => Projectile.ai[1] = value;
        }

        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 18;
            ProjectileID.Sets.TrailingMode[Type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = 16;
            Projectile.height = 16;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 150;
            Projectile.ignoreWater = true;
            Projectile.extraUpdates = 2;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 10;
        }

        public override void OnSpawn(IEntitySource source)
        {
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
        }

        private float WidthFunction(float completionRatio, Vector2 vertexPosition)
        {
            float arrowheadCutoff = 0.38f;
            float width = 16f;
            if (completionRatio <= arrowheadCutoff)
                width = MathHelper.Lerp(0.02f, width, Utils.GetLerpValue(0f, arrowheadCutoff, completionRatio, true));

            return width;
        }

        private Color ColorFunction(float completionRatio, Vector2 vertexPosition)
        {
            float pulse = 0.5f + 0.5f * (float)Math.Cos(completionRatio * 2.8f - Main.GlobalTimeWrappedHourly * 6.2f + WobbleOffset);
            Color bodyColor = Color.Lerp(TrailStartColor, TrailMidColor, pulse * 0.65f);
            return Color.Lerp(bodyColor, TrailEndColor, Utils.GetLerpValue(0f, 0.42f, completionRatio, true));
        }

        public override bool PreDraw(ref Color lightColor)
        {
            GameShaders.Misc["CalamityMod:TrailStreak"].SetShaderTexture(
                ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/Trails/SylvestaffStreak"));

            Vector2 offset = Projectile.Size * 0.5f + Projectile.velocity * 1.25f;
            PrimitiveRenderer.RenderTrail(
                Projectile.oldPos,
                new PrimitiveSettings(
                    WidthFunction,
                    ColorFunction,
                    (_, _) => offset,
                    true,
                    true,
                    GameShaders.Misc["CalamityMod:TrailStreak"]),
                42);

            return false;
        }

        public override void AI()
        {
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
            Lighting.AddLight(Projectile.Center, TrailStartColor.ToVector3() * 0.45f);

            if (HomingDelay > 0f)
            {
                HomingDelay--;
                float wobble = (float)Math.Sin((Projectile.identity * 0.53f + Main.GameUpdateCount * 0.32f) + WobbleOffset) * 0.015f;
                Projectile.velocity = Projectile.velocity.RotatedBy(wobble);
            }
            else
            {
                NPC target = FindClosestTarget(1150f);
                if (target != null)
                {
                    float targetSpeed = MathHelper.Max(Projectile.velocity.Length(), 15f);
                    Vector2 desiredVelocity = (target.Center - Projectile.Center).SafeNormalize(Projectile.velocity) * targetSpeed;
                    Projectile.velocity = Vector2.Lerp(Projectile.velocity, desiredVelocity, 0.085f);
                }
            }

            if (Projectile.timeLeft % 4 == 0 && Main.rand.NextBool(2))
            {
                GeneralParticleHandler.SpawnParticle(new SquishyLightParticle(
                    Projectile.Center,
                    -Projectile.velocity * 0.08f,
                    Main.rand.NextFloat(0.16f, 0.24f),
                    Color.Lerp(TrailStartColor, Color.White, 0.2f),
                    Main.rand.Next(8, 12)));
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            SoundEngine.PlaySound(SoundID.Item27 with { Volume = 0.26f, Pitch = 0.3f }, Projectile.Center);
        }

        public override void OnKill(int timeLeft)
        {
            if (Main.dedServ)
                return;

            for (int i = 0; i < 4; i++)
            {
                GeneralParticleHandler.SpawnParticle(new SquishyLightParticle(
                    Projectile.Center,
                    Main.rand.NextVector2Circular(2.4f, 2.4f),
                    Main.rand.NextFloat(0.22f, 0.32f),
                    Color.Lerp(TrailMidColor, Color.White, 0.35f),
                    Main.rand.Next(10, 14)));
            }
        }

        private NPC FindClosestTarget(float maxDistance)
        {
            NPC bestTarget = null;
            float bestDistance = maxDistance;

            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!npc.CanBeChasedBy(Projectile))
                    continue;

                float distance = Vector2.Distance(Projectile.Center, npc.Center);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    bestTarget = npc;
                }
            }

            return bestTarget;
        }
    }
}
