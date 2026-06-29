using CalamityMod;
using CalamityMod.Buffs.DamageOverTime;
using CalamityMod.Particles;
using CalamityMod.Projectiles.Typeless;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SHPC.Effects.CPreMoodLord.PlagueCell
{
    public class PlagueCell_Nuke : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.SHPC";
        public override string Texture => "CalamityMod/Projectiles/Ranged/HiveNuke";

        private const float InitialSpeed = 28f;
        private const float SpeedAcceleration = 0.36f;
        private const float MaxSpeed = 84f;

        private ref float CurrentSpeed => ref Projectile.localAI[0];
        private ref float ConfirmedImpact => ref Projectile.localAI[1];

        private bool exploded;

        public override void SetDefaults()
        {
            Projectile.width = 34;
            Projectile.height = 34;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 180;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 10;
        }

        public override void AI()
        {
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
            Projectile.timeLeft = System.Math.Max(Projectile.timeLeft, 60);

            int targetIndex = (int)Projectile.ai[0];
            if (!Main.npc.IndexInRange(targetIndex) || !Main.npc[targetIndex].active)
                return;

            NPC target = Main.npc[targetIndex];

            if (CurrentSpeed < InitialSpeed)
                CurrentSpeed = InitialSpeed;
            CurrentSpeed = System.Math.Min(CurrentSpeed + SpeedAcceleration, MaxSpeed);

            float turnRate = MathHelper.Clamp(0.18f + CurrentSpeed * 0.0035f, 0.18f, 0.36f);
            float desiredAngle = (target.Center - Projectile.Center).ToRotation();
            float newAngle = Projectile.velocity.ToRotation().AngleTowards(desiredAngle, turnRate);
            Projectile.velocity = newAngle.ToRotationVector2() * CurrentSpeed;

            if (Projectile.Distance(target.Center) < 28f)
            {
                ConfirmTargetImpact();
                Projectile.Kill();
            }

            bool isBig = Projectile.ai[1] != 0f;
            if (isBig || Main.rand.NextBool(2))
            {
                Color smokeColor = Color.Lerp(Color.Black, Color.Lime, 0.25f);
                GeneralParticleHandler.SpawnParticle(new HeavySmokeParticle(
                    Projectile.Center - Projectile.velocity * (isBig ? 2.8f : 2f),
                    -Projectile.velocity * Main.rand.NextFloat(isBig ? 0.18f : 0.12f, isBig ? 0.54f : 0.38f),
                    smokeColor * (isBig ? 0.82f : 0.58f),
                    isBig ? 15 : 11,
                    Main.rand.NextFloat(isBig ? 0.54f : 0.36f, isBig ? 0.92f : 0.58f),
                    isBig ? 0.28f : 0.21f,
                    Main.rand.NextFloat(-0.2f, 0.2f),
                    false));
            }

            if (isBig && !Main.dedServ)
            {
                Vector2 back = -Projectile.velocity.SafeNormalize(Vector2.UnitY);
                GeneralParticleHandler.SpawnParticle(new SparkParticle(
                    Projectile.Center + Main.rand.NextVector2Circular(8f, 8f),
                    back.RotatedByRandom(0.32f) * Main.rand.NextFloat(3.2f, 7.8f),
                    false,
                    Main.rand.Next(10, 18),
                    Main.rand.NextFloat(0.8f, 1.35f),
                    Main.rand.NextBool(3) ? Color.White : Color.LimeGreen));

                Dust dust = Dust.NewDustPerfect(
                    Projectile.Center - Projectile.velocity.SafeNormalize(Vector2.UnitY) * Main.rand.NextFloat(8f, 22f),
                    Main.rand.NextBool() ? DustID.GemEmerald : DustID.GreenTorch,
                    back.RotatedByRandom(0.42f) * Main.rand.NextFloat(1.2f, 4.2f),
                    60,
                    Color.Lerp(Color.LimeGreen, Color.White, Main.rand.NextFloat(0.1f, 0.35f)),
                    Main.rand.NextFloat(1.0f, 1.65f));
                dust.noGravity = true;
            }

            Lighting.AddLight(Projectile.Center, new Vector3(0.18f, 0.7f, 0.08f));
        }

        public override bool? CanHitNPC(NPC target)
        {
            return target.whoAmI == (int)Projectile.ai[0] ? null : false;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (target.whoAmI != (int)Projectile.ai[0])
                return;

            ConfirmTargetImpact();
            target.AddBuff(ModContent.BuffType<Plague>(), 120);
            Projectile.Kill();
        }

        public override void OnKill(int timeLeft)
        {
            if (exploded)
                return;

            exploded = true;
            bool isBig = Projectile.ai[1] != 0f;
            SoundEngine.PlaySound(new SoundStyle("CalamityMod/Sounds/Item/TheHiveNuke") { Volume = isBig ? 0.86f : 0.55f }, Projectile.Center);
            SpawnNukeExplosionEffects(isBig);

            int oldWidth = Projectile.width;
            int oldHeight = Projectile.height;
            int explosionSize = isBig ? 480 : 240;
            Projectile.Resize(explosionSize, explosionSize);
            Projectile.penetrate = -1;
            Projectile.Damage();
            Projectile.Resize(oldWidth, oldHeight);

            if (isBig && Projectile.owner == Main.myPlayer)
            {
                Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    Projectile.Center,
                    Vector2.Zero,
                    ModContent.ProjectileType<PlagueCell_Fog>(),
                    System.Math.Max(1, Projectile.damage / 8),
                    0f,
                    Projectile.owner);

                int beeCount = (Main.player[Projectile.owner].strongBees ? 9 : 7) + 3;
                for (int i = 0; i < beeCount; i++)
                {
                    float delayFactor = Main.rand.NextFloat(0.7f, 1.4f);
                    float initialHomingCounter = 30f - 30f * delayFactor;
                    Vector2 velocity = (MathHelper.TwoPi * i / beeCount + Main.rand.NextFloat(-0.14f, 0.14f)).ToRotationVector2() * Main.rand.NextFloat(3.5f, 8f);
                    int bee = Projectile.NewProjectile(
                        Projectile.GetSource_FromThis(),
                        Projectile.Center,
                        velocity,
                        ModContent.ProjectileType<BasicPlagueBee>(),
                        System.Math.Max(1, (int)(Projectile.damage * 0.06f)),
                        0f,
                        Projectile.owner,
                        initialHomingCounter,
                        120f,
                        1.5f);

                    if (Main.projectile.IndexInRange(bee))
                    {
                        Projectile plagueBee = Main.projectile[bee];
                        plagueBee.DamageType = DamageClass.Magic;
                        plagueBee.penetrate = 1;
                        plagueBee.scale *= 1.35f;
                        plagueBee.light = MathHelper.Max(plagueBee.light, 0.35f);
                    }
                }
            }

            for (int i = 0; i < 34; i++)
            {
                Vector2 velocity = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(4f, 16f);
                Dust dust = Dust.NewDustPerfect(Projectile.Center, Main.rand.NextBool(3) ? DustID.GemEmerald : DustID.SteampunkSteam, velocity, 0, Color.LimeGreen, Main.rand.NextFloat(1f, 1.8f));
                dust.noGravity = true;
                dust.alpha = Main.rand.Next(70, 190);
            }
        }

        private void ConfirmTargetImpact()
        {
            if (ConfirmedImpact > 0f)
                return;

            ConfirmedImpact = 1f;
            int boundMarkIdentity = (int)Projectile.ai[2] - 1;
            if (boundMarkIdentity < 0)
                return;

            int markType = ModContent.ProjectileType<PlagueCell_Marked>();
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile mark = Main.projectile[i];
                if (!mark.active || mark.type != markType || mark.owner != Projectile.owner || mark.identity != boundMarkIdentity)
                    continue;

                mark.ai[2] = 1f;
                mark.netUpdate = true;
                break;
            }
        }

        private void SpawnNukeExplosionEffects(bool isBig)
        {
            Vector2 center = Projectile.Center;
            Color plagueGreen = new(74, 255, 92);
            Color deepGreen = new(12, 92, 24);
            Color toxicYellow = new(190, 255, 70);
            float effectScale = isBig ? 1f : 0.72f;
            float countScale = isBig ? 1f : 0.72f;

            GeneralParticleHandler.SpawnParticle(new CustomPulse(
                center,
                Vector2.Zero,
                plagueGreen,
                "CalamityMod/Particles/BloomCircle",
                Vector2.One,
                Main.rand.NextFloat(MathHelper.TwoPi),
                0.18f,
                2.4f * effectScale,
                isBig ? 24 : 18));

            GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(
                center,
                Vector2.Zero,
                plagueGreen,
                Vector2.One,
                0f,
                0.18f,
                5.2f * effectScale,
                isBig ? 28 : 21));

            GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(
                center,
                Vector2.Zero,
                toxicYellow,
                new Vector2(1.15f, 1.15f),
                0f,
                0.1f,
                3.6f * effectScale,
                isBig ? 22 : 16));

            for (int i = 0; i < (int)(46 * countScale); i++)
            {
                Vector2 direction = Main.rand.NextVector2CircularEdge(1f, 1f);
                Vector2 velocity = direction * Main.rand.NextFloat(3.5f, 18f);
                Dust dust = Dust.NewDustPerfect(
                    center + direction * Main.rand.NextFloat(6f, 28f) * effectScale,
                    Main.rand.NextBool(3) ? DustID.GemEmerald : DustID.GreenTorch,
                    velocity,
                    Main.rand.Next(40, 130),
                    Main.rand.NextBool(4) ? toxicYellow : plagueGreen,
                    Main.rand.NextFloat(1.15f, 2.25f) * effectScale);
                dust.noGravity = true;
            }

            for (int i = 0; i < (int)(18 * countScale); i++)
            {
                Vector2 velocity = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(2.2f, 8.4f) * effectScale;
                GeneralParticleHandler.SpawnParticle(new HeavySmokeParticle(
                    center + Main.rand.NextVector2Circular(22f, 22f) * effectScale,
                    velocity,
                    Color.Lerp(deepGreen, Color.Black, Main.rand.NextFloat(0.25f, 0.55f)) * 0.82f,
                    Main.rand.Next(isBig ? 30 : 22, isBig ? 50 : 38),
                    Main.rand.NextFloat(0.72f, 1.45f) * effectScale,
                    0.42f,
                    Main.rand.NextFloat(-0.08f, 0.08f),
                    false));
            }

            for (int i = 0; i < (int)(14 * countScale); i++)
            {
                Vector2 velocity = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(7f, 19f) * effectScale;
                GeneralParticleHandler.SpawnParticle(new SparkParticle(
                    center,
                    velocity,
                    false,
                    Main.rand.Next(isBig ? 16 : 12, isBig ? 26 : 20),
                    Main.rand.NextFloat(1.1f, 1.9f) * effectScale,
                    Main.rand.NextBool(3) ? toxicYellow : plagueGreen));
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Type].Value;
            Main.EntitySpriteDraw(
                texture,
                Projectile.Center - Main.screenPosition,
                null,
                lightColor,
                Projectile.rotation,
                texture.Size() * 0.5f,
                Projectile.scale,
                SpriteEffects.None);
            return false;
        }
    }
}
