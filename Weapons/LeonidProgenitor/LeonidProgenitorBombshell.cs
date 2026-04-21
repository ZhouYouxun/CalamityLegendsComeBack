using CalamityLegendsComeBack.Weapons.LeonidProgenitor.Core;
using CalamityMod;
using CalamityMod.Projectiles.Rogue;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.LeonidProgenitor
{
    public class LeonidProgenitorBombshell : ModProjectile, ILocalizedModType
    {
        public override string Texture => "CalamityMod/Items/Weapons/Rogue/LeonidProgenitor";
        public new string LocalizationCategory => "Projectiles.LeonidProgenitor";

        private const int AnticipationFrames = 6;

        private Vector2 launchVelocity;
        private bool initialized;

        public bool StealthVariant => Projectile.ai[0] >= 1f || Projectile.Calamity().stealthStrike;
        public int PrimaryEffectID => (int)Projectile.ai[1];
        public int SecondaryEffectID => (int)Projectile.ai[2];
        private Player Owner => Main.player[Projectile.owner];

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 10;
            ProjectileID.Sets.TrailingMode[Type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = 36;
            Projectile.height = 36;
            Projectile.friendly = true;
            Projectile.ignoreWater = true;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 240;
            Projectile.tileCollide = false;
        }

        public override bool ShouldUpdatePosition() => Projectile.localAI[0] >= AnticipationFrames;

        public override bool? CanDamage() => Projectile.localAI[0] >= AnticipationFrames;

        public override void AI()
        {
            if (!initialized)
            {
                launchVelocity = Projectile.velocity * (StealthVariant ? 1.2f : 1f);
                Projectile.velocity = Vector2.Zero;
                Projectile.DamageType = Owner.HeldItem.DamageType;
                initialized = true;
            }

            if (Projectile.localAI[0] < AnticipationFrames)
            {
                DoThrowAnticipation();
                return;
            }

            if (Projectile.localAI[1] == 0f)
            {
                Projectile.localAI[1] = 1f;
                Projectile.tileCollide = true;
                Projectile.velocity = launchVelocity;
                SoundEngine.PlaySound(SoundID.Item1 with { Pitch = 0.08f }, Projectile.Center);
            }

            Color trailColor = StealthVariant
                ? Color.Lerp(new Color(140, 224, 255), new Color(255, 165, 255), 0.5f + 0.5f * (float)System.Math.Sin(Main.GlobalTimeWrappedHourly * 4f))
                : new Color(105, 210, 255);

            Lighting.AddLight(Projectile.Center, trailColor.ToVector3() * (StealthVariant ? 0.95f : 0.55f));

            if (Main.rand.NextBool(StealthVariant ? 1 : 2))
            {
                Dust dust = Dust.NewDustPerfect(
                    Projectile.Center + Main.rand.NextVector2Circular(12f, 12f),
                    DustID.TintableDustLighted,
                    -Projectile.velocity * Main.rand.NextFloat(0.03f, 0.16f),
                    100,
                    trailColor,
                    Main.rand.NextFloat(0.9f, 1.45f));
                dust.noGravity = true;
            }

            if (!StealthVariant)
                Projectile.velocity.Y += 0.22f;
            else
                Projectile.velocity *= 1.003f;

            Projectile.rotation += (0.25f + Projectile.velocity.Length() * 0.015f) * System.Math.Sign(Projectile.velocity.X == 0f ? 1f : Projectile.velocity.X);
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            SpawnImpactBurst();
            return true;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            SpawnImpactBurst();

            if (StealthVariant)
            {
                int markID = Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    target.Center,
                    Vector2.Zero,
                    ModContent.ProjectileType<LeonidCometStealthMark>(),
                    Projectile.damage,
                    Projectile.knockBack,
                    Projectile.owner,
                    target.whoAmI,
                    PrimaryEffectID,
                    SecondaryEffectID);

                if (markID.WithinBounds(Main.maxProjectiles))
                    Main.projectile[markID].netUpdate = true;
            }
            else
            {
                SpawnSmallMeteor(target.Center, false);
            }
        }

        public override void OnKill(int timeLeft)
        {
            SpawnImpactBurst();
            SoundEngine.PlaySound(SoundID.Item62 with { Pitch = -0.18f }, Projectile.Center);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = ModContent.Request<Texture2D>(Texture).Value;
            Texture2D glow = ModContent.Request<Texture2D>("CalamityMod/Items/Weapons/Rogue/LeonidProgenitorGlow").Value;
            Color drawColor = StealthVariant ? Color.White : lightColor;

            for (int i = 0; i < Projectile.oldPos.Length; i++)
            {
                Vector2 oldDrawPosition = Projectile.oldPos[i] + Projectile.Size * 0.5f - Main.screenPosition;
                float completion = 1f - i / (float)Projectile.oldPos.Length;
                Color afterimageColor = (StealthVariant ? new Color(116, 233, 255) : new Color(88, 188, 255)) * completion * (StealthVariant ? 0.6f : 0.25f);
                Main.EntitySpriteDraw(texture, oldDrawPosition, null, afterimageColor, Projectile.rotation, texture.Size() * 0.5f, Projectile.scale, SpriteEffects.None, 0f);
            }

            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            Main.EntitySpriteDraw(texture, drawPosition, null, drawColor, Projectile.rotation, texture.Size() * 0.5f, Projectile.scale, SpriteEffects.None, 0f);
            Main.EntitySpriteDraw(glow, drawPosition, null, Color.White, Projectile.rotation, glow.Size() * 0.5f, Projectile.scale, SpriteEffects.None, 0f);
            LeonidVisualUtils.DrawBloom(Projectile.Center, (StealthVariant ? new Color(112, 230, 255) : new Color(82, 192, 255)) * 0.35f, StealthVariant ? 0.42f : 0.28f);
            return false;
        }

        private void DoThrowAnticipation()
        {
            Projectile.localAI[0]++;
            Owner.ChangeDir(System.Math.Sign(Main.MouseWorld.X - Owner.Center.X));

            float progress = Projectile.localAI[0] / AnticipationFrames;
            float armRotation = MathHelper.Lerp(-0.95f, 0.55f, progress) * Owner.direction;

            Owner.heldProj = Projectile.whoAmI;
            Owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, MathHelper.Pi + armRotation);

            Projectile.direction = Owner.direction;
            Projectile.spriteDirection = Owner.direction;
            Projectile.Center = Owner.MountedCenter + Vector2.UnitY.RotatedBy(armRotation * Owner.gravDir) * -34f * Owner.gravDir + new Vector2(Owner.direction * 8f, -6f);
            Projectile.rotation = armRotation + (Owner.direction == 1 ? MathHelper.PiOver4 : MathHelper.Pi * 0.75f);
        }

        private void SpawnImpactBurst()
        {
            Color impactColor = StealthVariant ? new Color(156, 233, 255) : new Color(110, 208, 255);
            LeonidVisualUtils.SpawnDustBurst(Projectile.Center, impactColor, StealthVariant ? 16 : 10, StealthVariant ? 6f : 4f, StealthVariant ? 1.35f : 1f);
            LeonidVisualUtils.DrawBloom(Projectile.Center, impactColor * 0.55f, StealthVariant ? 0.65f : 0.45f);
        }

        private void SpawnSmallMeteor(Vector2 targetCenter, bool fromStealthMark)
        {
            Vector2 spawnPosition = new(targetCenter.X + Main.rand.Next(-180, 181), targetCenter.Y - 660f - Main.rand.Next(50, 120));
            Vector2 velocity = (targetCenter - spawnPosition).SafeNormalize(Vector2.UnitY) * (fromStealthMark ? 19.5f : 17f);

            Projectile.NewProjectile(
                Projectile.GetSource_FromThis(),
                spawnPosition,
                velocity,
                ModContent.ProjectileType<LeonidCometSmall>(),
                Projectile.damage / (fromStealthMark ? 2 : 3),
                Projectile.knockBack,
                Projectile.owner,
                PrimaryEffectID,
                SecondaryEffectID,
                fromStealthMark ? 1f : 0f);
        }
    }
}
