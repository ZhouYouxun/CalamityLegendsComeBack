using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using CalamityLegendsComeBack.Weapons.YharimsCrystal.YCLeft;

namespace CalamityLegendsComeBack.Weapons.YharimsCrystal
{
    public class YC_WarshipMissile : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.YharimsCrystal";
        public override string Texture => "CalamityLegendsComeBack/Weapons/YharimsCrystal/YCLeft/YC_Left_Rocket";

        private ref float TurnRate => ref Projectile.ai[0];
        private ref float SupportMarker => ref Projectile.ai[1];
        private ref float Timer => ref Projectile.localAI[0];
        private ref float CachedTargetIndex => ref Projectile.localAI[1];

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 10;
            ProjectileID.Sets.TrailingMode[Type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = 18;
            Projectile.height = 18;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 180;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = true;
            Projectile.extraUpdates = 1;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }

        public override void OnSpawn(Terraria.DataStructures.IEntitySource source)
        {
            Projectile.velocity = Projectile.velocity.RotatedByRandom(MathHelper.ToRadians(4f));
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2 + MathHelper.Pi;
            CachedTargetIndex = -1f;
        }

        public override void AI()
        {
            Player owner = Main.player[Projectile.owner];
            if (!owner.active || owner.dead)
            {
                Projectile.Kill();
                return;
            }

            Timer++;
            Vector2 forward = Projectile.velocity.SafeNormalize(Vector2.UnitY);

            if (Timer < 8f)
            {
                Projectile.velocity *= 1.014f;
            }
            else
            {
                NPC target = AcquireTarget(owner, 1500f);
                Vector2 desiredDirection = target != null
                    ? (target.Center - Projectile.Center).SafeNormalize(forward)
                    : forward;

                float speed = MathHelper.Clamp(Projectile.velocity.Length() * 1.008f, 10f, 18f + TurnRate * 45f);
                Projectile.velocity = RotateTowards(forward, desiredDirection, MathHelper.Max(0.012f, TurnRate)) * speed;
            }

            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2 + MathHelper.Pi;
            Lighting.AddLight(Projectile.Center, new Color(95, 205, 255).ToVector3() * 0.45f);
            EmitFlightFX();
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            Projectile.Kill();
            return false;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            Projectile.Kill();
        }

        public override void OnKill(int timeLeft)
        {
            if (Projectile.owner == Main.myPlayer)
            {
                Projectile.Resize(78, 78);
                Projectile.penetrate = -1;
                Projectile.maxPenetrate = -1;
                Projectile.usesLocalNPCImmunity = true;
                Projectile.localNPCHitCooldown = -1;
                Projectile.Damage();
            }

            SoundEngine.PlaySound(SoundID.Item14 with { Volume = 0.32f, Pitch = 0.12f }, Projectile.Center);

            if (Main.dedServ)
                return;

            for (int i = 0; i < 16; i++)
            {
                Vector2 velocity = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(2.4f, 5.2f);
                Dust smoke = Dust.NewDustPerfect(
                    Projectile.Center,
                    Main.rand.NextBool(3) ? DustID.Smoke : DustID.RainbowTorch,
                    velocity,
                    0,
                    Color.Lerp(new Color(255, 180, 105), new Color(120, 220, 255), Main.rand.NextFloat()),
                    Main.rand.NextFloat(0.85f, 1.35f));
                smoke.noGravity = true;
            }

            DirectionalPulseRing pulse = new DirectionalPulseRing(
                Projectile.Center,
                Vector2.Zero,
                new Color(115, 220, 255) * 0.85f,
                new Vector2(1f, 1.75f),
                0f,
                0.1f,
                0.03f,
                14);
            GeneralParticleHandler.SpawnParticle(pulse);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Type].Value;
            Vector2 origin = texture.Size() * 0.5f;
            Color outlineColor = new(110, 225, 255, 0);

            for (int i = Projectile.oldPos.Length - 1; i >= 0; i--)
            {
                if (Projectile.oldPos[i] == Vector2.Zero)
                    continue;

                float completion = 1f - i / (float)Projectile.oldPos.Length;
                Vector2 drawPosition = Projectile.oldPos[i] + Projectile.Size * 0.5f - Main.screenPosition;
                Main.EntitySpriteDraw(
                    texture,
                    drawPosition,
                    null,
                    outlineColor * (0.08f + completion * 0.18f),
                    Projectile.rotation,
                    origin,
                    Projectile.scale * (0.9f + completion * 0.16f),
                    SpriteEffects.None,
                    0);
            }

            Main.EntitySpriteDraw(
                texture,
                Projectile.Center - Main.screenPosition,
                null,
                Color.White,
                Projectile.rotation,
                origin,
                Projectile.scale,
                SpriteEffects.None,
                0);

            return false;
        }

        private NPC AcquireTarget(Player owner, float range)
        {
            int cachedIndex = (int)CachedTargetIndex;
            if (cachedIndex >= 0 && cachedIndex < Main.maxNPCs)
            {
                NPC cached = Main.npc[cachedIndex];
                if (cached.active &&
                    cached.CanBeChasedBy(Projectile) &&
                    Vector2.DistanceSquared(Projectile.Center, cached.Center) <= range * range)
                {
                    return cached;
                }
            }

            NPC target = YC_LeftSquadronHelper.FindPriorityTarget(owner, Projectile.Center, range, Projectile.velocity.SafeNormalize(Vector2.UnitY), 120f, false);
            CachedTargetIndex = target?.whoAmI ?? -1;
            return target;
        }

        private void EmitFlightFX()
        {
            if (Main.dedServ)
                return;

            Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitY);
            Vector2 exhaustOrigin = Projectile.Center - direction * 10f;

            if (Main.rand.NextBool(2))
            {
                Dust smoke = Dust.NewDustPerfect(
                    exhaustOrigin,
                    DustID.Smoke,
                    -direction * Main.rand.NextFloat(1.5f, 3f) + Main.rand.NextVector2Circular(0.45f, 0.45f),
                    100,
                    default,
                    Main.rand.NextFloat(0.75f, 1.15f));
                smoke.noGravity = true;
            }

            Dust flare = Dust.NewDustPerfect(
                exhaustOrigin,
                DustID.RainbowTorch,
                -direction.RotatedByRandom(0.25f) * Main.rand.NextFloat(1.2f, 3.2f),
                0,
                Color.Lerp(new Color(255, 165, 90), new Color(125, 220, 255), Main.rand.NextFloat(0.2f, 0.7f)),
                Main.rand.NextFloat(0.7f, 1.1f));
            flare.noGravity = true;

            if (Timer > 10f && Main.rand.NextBool(4))
            {
                DirectionalPulseRing pulse = new DirectionalPulseRing(
                    Projectile.Center + direction * 4f,
                    Projectile.velocity * 0.05f,
                    new Color(120, 220, 255) * 0.7f,
                    new Vector2(0.8f, 1.4f),
                    Projectile.rotation,
                    0.05f,
                    0.02f,
                    8);
                GeneralParticleHandler.SpawnParticle(pulse);
            }
        }

        private static Vector2 RotateTowards(Vector2 currentDirection, Vector2 desiredDirection, float maxTurnRadians)
        {
            float currentAngle = currentDirection.ToRotation();
            float desiredAngle = desiredDirection.ToRotation();
            float delta = MathHelper.WrapAngle(desiredAngle - currentAngle);
            delta = MathHelper.Clamp(delta, -maxTurnRadians, maxTurnRadians);
            return (currentAngle + delta).ToRotationVector2();
        }
    }
}
