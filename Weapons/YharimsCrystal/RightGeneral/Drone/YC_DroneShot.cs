using CalamityLegendsComeBack.Weapons.YharimsCrystal.Passive;
using CalamityMod;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.YharimsCrystal.RightGeneral
{
    // ai[0]: 0 = needle, 1 = orbiting crossfire. ai[1]: cursor-focus command.
    internal sealed class YC_DroneShot : ModProjectile, ILocalizedModType
    {
        private static readonly Color NeedleGold = new(255, 220, 88);
        private static readonly Color CrossfireOrange = new(255, 104, 36);
        private static readonly Color FocusWhite = new(255, 249, 196);

        public new string LocalizationCategory => "Projectiles.YharimsCrystal";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private int Pattern => (int)Projectile.ai[0];
        private bool Focused => Projectile.ai[1] >= 1f;
        private ref float Timer => ref Projectile.localAI[0];
        private ref float Speed => ref Projectile.localAI[1];
        private Color ShotColor => Pattern == 0 ? NeedleGold : CrossfireOrange;

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 9;
            ProjectileID.Sets.TrailingMode[Type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = 16;
            Projectile.height = 16;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 105;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.extraUpdates = 1;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 10;
        }

        public override void OnSpawn(Terraria.DataStructures.IEntitySource source)
        {
            YharimsCrystalHellBladeGlobalProjectile.Mark(Projectile, YCWeaponForm.Crystal);
            Speed = Projectile.velocity.Length();
            if (Speed < 8f)
                Speed = Pattern == 0 ? 21f : 16f;
            Projectile.scale = Pattern == 0 ? 0.82f : 1.05f;
            Projectile.penetrate = Pattern == 0 ? 2 : 1;
        }

        public override void AI()
        {
            Timer++;
            Vector2 currentDirection = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            Vector2 targetPoint = Focused
                ? NewLegendYharimsCrystal.GetMouseWorld(Main.player[Projectile.owner])
                : GetTargetPoint(currentDirection);

            float ramp = Utils.GetLerpValue(0f, Pattern == 0 ? 50f : 72f, Timer, true);
            Vector2 desiredDirection = (targetPoint - Projectile.Center).SafeNormalize(currentDirection);
            if (Pattern == 1 && !Focused && Timer < 22f)
            {
                float side = Projectile.identity % 2 == 0 ? 1f : -1f;
                desiredDirection = desiredDirection.RotatedBy(side * MathHelper.ToRadians(17f) * (1f - Timer / 22f));
            }

            float turnDegrees = Focused
                ? 48f
                : Pattern == 0
                    ? MathHelper.Lerp(4f, 10f, ramp)
                    : MathHelper.Lerp(2f, 13.5f, ramp);
            float targetSpeed = Pattern == 0
                ? MathHelper.Lerp(21f, 29f, ramp)
                : MathHelper.Lerp(16f, 25f, ramp);
            Speed = MathHelper.Lerp(Speed, targetSpeed, Focused ? 0.24f : 0.075f);
            Projectile.velocity = currentDirection.ToRotation().AngleTowards(desiredDirection.ToRotation(), MathHelper.ToRadians(turnDegrees)).ToRotationVector2() * Speed;
            Projectile.rotation = Projectile.velocity.ToRotation();
            Lighting.AddLight(Projectile.Center, ShotColor.ToVector3() * (Pattern == 0 ? 0.34f : 0.46f));
        }

        private Vector2 GetTargetPoint(Vector2 fallbackDirection)
        {
            NPC target = Projectile.Center.ClosestNPCAt(1650f);
            if (target is null)
                return Projectile.Center + fallbackDirection * 900f;

            float leadFrames = MathHelper.Clamp(Projectile.Distance(target.Center) / MathHelper.Max(Speed, 1f), 4f, 24f);
            return target.Center + target.velocity * leadFrames;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(new BalanceYharimsCrystal().GetFireDebuffType(), 120);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            float fade = Utils.GetLerpValue(0f, 5f, Timer, true) * Utils.GetLerpValue(0f, 12f, Projectile.timeLeft, true);
            Color outer = ShotColor with { A = 0 };
            Color core = (Focused ? FocusWhite : Color.White) with { A = 0 };

            // Glow-point afterimages make a solid projectile silhouette without recreating a thin laser.
            for (int i = Projectile.oldPos.Length - 1; i >= 0; i--)
            {
                if (Projectile.oldPos[i] == Vector2.Zero)
                    continue;

                float completion = 1f - i / (float)Projectile.oldPos.Length;
                Vector2 trailPosition = Projectile.oldPos[i] + Projectile.Size * 0.5f - Main.screenPosition;
                Main.EntitySpriteDraw(bloom, trailPosition, null, outer * fade * completion * 0.22f, 0f,
                    bloom.Size() * 0.5f, (0.05f + completion * 0.08f) * Projectile.scale, SpriteEffects.None);
            }

            float pulse = 0.86f + System.MathF.Sin(Timer * 0.34f + Projectile.identity) * 0.14f;
            Main.EntitySpriteDraw(bloom, drawPosition, null, outer * fade * 0.82f, 0f, bloom.Size() * 0.5f,
                (Pattern == 0 ? 0.17f : 0.24f) * Projectile.scale * pulse, SpriteEffects.None);
            Main.EntitySpriteDraw(bloom, drawPosition, null, core * fade * 0.72f, 0f, bloom.Size() * 0.5f,
                (Pattern == 0 ? 0.07f : 0.1f) * Projectile.scale * pulse, SpriteEffects.None);
            return false;
        }
    }
}
