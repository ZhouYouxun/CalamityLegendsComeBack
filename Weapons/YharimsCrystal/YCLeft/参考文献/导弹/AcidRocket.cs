using System;
using CalamityMod.Buffs.DamageOverTime;
using CalamityMod.NPCs;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityMod.Projectiles.Ranged
{
    [PierceResistException]
    public class AcidRocket : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.Ranged";
        public NPC chosenTarget;
        public bool stuckInTarget = false;
        public bool stuckInGround = false;
        public int stuckTimer = 200;
        public bool canDamage = true;
        public bool canStick = true;
        public Vector2 vibrate = Vector2.Zero;
        public Vector2 placementCenter;
        float placementDistance;
        public Vector2 storedVelocity;
        Vector2 placementVelocity;

        public ref float RocketID => ref Projectile.ai[0];
        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.CultistIsResistantTo[Type] = true;
        }

        public override void SetDefaults()
        {
            Projectile.width = 36;
            Projectile.height = 36;
            Projectile.friendly = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.penetrate = -1;
            Projectile.alpha = 255;
            Projectile.MaxUpdates = 3;
            Projectile.timeLeft = 600;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = Projectile.MaxUpdates * 11;
        }

        public override void AI()
        {
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
            float power = 3;
            vibrate = Main.rand.NextVector2Circular(power, power);
            // Home in on enemies if not sticking to anything.
            if (!stuckInTarget)
            {
                CalamityUtils.HomeInOnNPC(Projectile, true, 720f, 16f, Projectile.MaxUpdates * 20f);
                // Trailing effects, only applied when homing
                if (!Main.dedServ && Projectile.FinalExtraUpdate() && Projectile.velocity.Length() > 3f)
                {
                    Color color = new Color(136, 211, 113, 127);
                    Color fadeColor = new Color(165, 165, 86);
                    Vector2 gasSpawnPosition = Projectile.Center + Main.rand.NextVector2Circular(8f, 8f);
                    Vector2 gasVelocity = Projectile.velocity * 1.2f + Projectile.velocity.RotatedBy(0.75f) * 0.3f;
                    gasVelocity *= Main.rand.NextFloat(0.24f, 0.6f);

                    Particle gas = new MediumMistParticle(gasSpawnPosition, gasVelocity, color, fadeColor, Main.rand.NextFloat(0.5f, 1f), 205 - Main.rand.Next(50), 0.02f);
                    GeneralParticleHandler.SpawnParticle(gas);
                    for (int i = 0; i < 2; i++)
                    {
                        Particle spark = new GlowSparkParticle(Projectile.Center - Projectile.velocity.SafeNormalize(Vector2.UnitX) * 10, -Projectile.velocity, false, 8, 0.13f * (i == 0 ? 0.2f : 0.5f), Color.Lerp(Color.SeaGreen, Color.PaleGreen, 0.25f) * 0.5f, new Vector2(0.3f, 1f), false, false, 0.8f);
                        GeneralParticleHandler.SpawnParticle(spark);
                    }
                    for (int i = 0; i < 2; i++)
                    {
                        Color bubbleColor = Main.rand.NextBool() ? Color.SeaGreen : Color.YellowGreen;
                        Vector2 bubbleSpawnPos = Projectile.Center + Main.rand.NextVector2Circular(50, 50);
                        Vector2 bubbleVelocity = -Projectile.velocity * Main.rand.NextFloat(0.2f, 0.8f);
                        Particle bubble = new DirectionalPulseRing(bubbleSpawnPos, bubbleVelocity, bubbleColor, new Vector2(0.8f, 1), 0, 0.1f, 0f, 75);
                        GeneralParticleHandler.SpawnParticle(bubble);
                    }
                }
            }
            else if (stuckInTarget)
            {
                placementCenter = chosenTarget.Center + placementVelocity * placementDistance + storedVelocity;

                Projectile.Center = placementCenter;
                Projectile.rotation = (storedVelocity).SafeNormalize(Vector2.UnitX).ToRotation() + MathHelper.PiOver2;
                stuckTimer--;
                if (chosenTarget.life <= 0 || chosenTarget == null)
                    stuckTimer = 0;
                if (stuckTimer == 0)
                {
                    Projectile.Kill();
                }
                if (stuckTimer == 40)
                {
                    SoundStyle Primed = new("CalamityMod/Sounds/Custom/PlagueSounds/PBGAttackSwitchShort");
                    SoundEngine.PlaySound(Primed with { Volume = 0.4f , PitchVariance = 0.2f }, Projectile.Center);
                }
            }
            Projectile.Opacity = 1f;
        }
        public override bool? CanDamage() => canDamage ? null : false;
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (target == chosenTarget)
                target.AddBuff(ModContent.BuffType<SulphuricPoisoning>(), 180);
            if (!stuckInTarget && canStick)
            {
                canDamage = false;
                Projectile.rotation = (Projectile.velocity).SafeNormalize(Vector2.UnitX).ToRotation() + MathHelper.PiOver2;
                placementDistance = -Vector2.Distance(target.Center, Projectile.Center);
                placementVelocity = (target.Center - Projectile.Center).SafeNormalize(Vector2.UnitX);
                placementCenter = placementVelocity * (placementDistance * 0.01f);
                chosenTarget = target;
                stuckInTarget = true;
                storedVelocity = Projectile.velocity.SafeNormalize(Vector2.UnitX) * 8;
                Projectile.velocity = Vector2.Zero;
                Projectile.netUpdate = true;
            }
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info) => target.AddBuff(ModContent.BuffType<SulphuricPoisoning>(), 180);

        public override void OnSpawn(IEntitySource source)
        {
            float rotation = Projectile.velocity.ToRotation();
            Particle pulse = new DirectionalPulseRing(Projectile.Center, Projectile.velocity / 2, Color.Gray, new Vector2(1f, 3f), rotation, 0.08f, 0.2f, 15);
            GeneralParticleHandler.SpawnParticle(pulse);
        }

        public override void OnKill(int timeLeft)
        {
            SoundStyle fire = new("CalamityMod/Sounds/Custom/PlagueSounds/PlagueBoom", 4);
            var info = new CalamityUtils.RocketBehaviorInfo((int)RocketID)
            {
                // Since we use our own spawning method for the cluster rockets, we don't need them to shoot anything,
                // we'll do it ourselves.
                clusterProjectileID = ProjectileID.None,
                destructiveClusterProjectileID = ProjectileID.None,
            };
            bool isClusterRocket = (RocketID == ItemID.ClusterRocketI || RocketID == ItemID.ClusterRocketII);
            SoundEngine.PlaySound(fire with { Volume = 0.9f, Pitch = -0.3f }, Projectile.Center);
            SoundEngine.PlaySound(SoundID.Item107, Projectile.Center);
            if (Projectile.owner == Main.myPlayer)
            {
                //The explosion has a different damage scaling depending on which rocket type you have. Left is Cluster Rocket, right is Non-Cluster.
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, Vector2.Zero, ModContent.ProjectileType<SulphuricAcidCannonExplosion>(), (int)(Projectile.damage * (isClusterRocket ? 1.5f : 2)), Projectile.knockBack, Projectile.owner);
                if (isClusterRocket)
                {
                    for (int k = 0; k < 3; k++)
                    {
                        Vector2 acidVelocity = (Vector2.UnitY * (-12f + Main.rand.NextFloat(-3f, 4f))).RotatedByRandom((double)MathHelper.ToRadians(40f));
                        Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, Projectile.velocity + acidVelocity, ModContent.ProjectileType<SulphuricDrop>(), (int)(Projectile.damage * 0.15f), Projectile.knockBack, Projectile.owner, RocketID);
                    }
                }
            }
            // Circular spread of clouds and bubbles for impact
            for (int i = 0; i < 35; i++)
            {
                Vector2 smokeVel = Main.rand.NextVector2Unit() * Main.rand.NextVector2Circular(32f, 32f);
                Color smokeColor = Main.rand.NextBool() ? Color.SeaGreen : Color.PaleGreen;
                Particle smoke = new MediumMistParticle(Projectile.Center, smokeVel, smokeColor, Color.Black, Main.rand.NextFloat(1.4f, 4f), (200 - Main.rand.Next(60)), 0.08f);
                GeneralParticleHandler.SpawnParticle(smoke);
            }
            for (int i = 0; i < 8; i++)
            {
                Vector2 bubbleVel = Main.rand.NextVector2Circular(26f, 26f);
                Color bubbleColor = Main.rand.NextBool() ? Color.OliveDrab : Color.SeaGreen;
                DirectionalPulseRing pulse = new DirectionalPulseRing(Projectile.Center, bubbleVel, bubbleColor, new Vector2(0.8f, 1), 0, 0.21f, 0f, 50);
                GeneralParticleHandler.SpawnParticle(pulse);
            }          
            //Explosion effect
            Particle blastRing = new CustomPulse(Projectile.Center, Vector2.Zero, Color.SeaGreen, "CalamityMod/Particles/FlameExplosion", Vector2.One, Main.rand.NextFloat(-10, 10), 0f, 0.2f, 20, true, 1f);
            GeneralParticleHandler.SpawnParticle(blastRing);
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {     
            if (targetHitbox.Width > 8 && targetHitbox.Height > 8)
            {
                targetHitbox.Inflate(-targetHitbox.Width / 8, -targetHitbox.Height / 8);
            }
            return null;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            float glowOutwardness = MathHelper.SmoothStep(3f, 0f, Utils.GetLerpValue(90f, 270f, stuckTimer, true));
            Texture2D Texture = Terraria.GameContent.TextureAssets.Projectile[Projectile.type].Value;
            Rectangle frame = Texture.Frame(1, Main.projFrames[Projectile.type], 0, Projectile.frame);
            Vector2 drawPosition;
            Vector2 origin = frame.Size() * 0.5f;
            Color glowColor = Color.Lerp(Color.YellowGreen, Color.Lime, (float)Math.Cos(Main.GlobalTimeWrappedHourly * 5f) * 0.5f + 0.5f);
            glowColor.A = 0;

            if (stuckInTarget)
            {
                for (int i = 0; i < 8; i++)
                {
                    drawPosition = Projectile.Center + (MathHelper.TwoPi * i / 8f + Main.GlobalTimeWrappedHourly * 4f).ToRotationVector2() * glowOutwardness - Main.screenPosition;
                    Main.EntitySpriteDraw(Texture, drawPosition, frame, Projectile.GetAlpha(glowColor), Projectile.rotation, origin, Projectile.scale, SpriteEffects.None, 0);
                }
            }
            drawPosition = Projectile.Center - Main.screenPosition;
            Main.EntitySpriteDraw(Texture, drawPosition + vibrate, frame, Projectile.GetAlpha(lightColor), Projectile.rotation, origin, Projectile.scale, SpriteEffects.None, 0);
            return false;
        }
    }
}
