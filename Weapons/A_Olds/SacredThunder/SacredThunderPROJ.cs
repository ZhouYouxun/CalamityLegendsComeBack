using System;
using CalamityMod;
using CalamityMod.Buffs.DamageOverTime;
using CalamityMod.Projectiles.DraedonsArsenal;
using CalamityMod.Projectiles.Rogue;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Olds.SacredThunder
{
    public class SacredThunderPROJ : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.SacredThunder";

        private const int NormalHitGoal = 5;
        private const int StealthSettleTime = 54;
        private const int StealthHoverTime = 900;
        private const float ReturnSpeed = 29f;
        private const float ReturnInertia = 10f;

        private int normalHits;
        private int electricTimer;
        private int seraphTimer;
        private int exorcismTimer;
        private bool initialized;
        private bool finalBurstDone;

        private bool StealthStrike => Projectile.ai[0] >= 1f || Projectile.Calamity().stealthStrike;
        private bool Returning => Projectile.ai[2] == 1f;
        private bool Hovering => Projectile.ai[2] == 2f;
        private Player Owner => Main.player[Projectile.owner];

        public override string Texture => "CalamityLegendsComeBack/Weapons/A_Dev/SacredThunder/SacredThunder";

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 14;
            ProjectileID.Sets.TrailingMode[Type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = 52;
            Projectile.height = 52;
            Projectile.friendly = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 720;
            Projectile.extraUpdates = 0;
            Projectile.DamageType = ModContent.GetInstance<RogueDamageClass>();
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 16;
        }

        public override void AI()
        {
            if (!initialized)
            {
                initialized = true;
                Projectile.Calamity().stealthStrike = StealthStrike;
                Projectile.timeLeft = StealthStrike ? StealthSettleTime + StealthHoverTime + 90 : 720;
                Projectile.scale = StealthStrike ? 1.18f : 1f;
                SoundEngine.PlaySound(SoundID.Item1 with { Pitch = StealthStrike ? -0.2f : 0.08f }, Projectile.Center);
            }

            if (!Owner.active || Owner.dead)
            {
                Projectile.Kill();
                return;
            }

            DoCommonVisuals();

            if (StealthStrike)
                StealthAI();
            else
                NormalAI();

            Projectile.ai[1]++;
        }

        private void NormalAI()
        {
            Projectile.rotation += 0.42f * Math.Sign(Projectile.velocity.X == 0f ? Owner.direction : Projectile.velocity.X);

            if (Returning)
            {
                Projectile.localNPCHitCooldown = 8;
                ReturnToPlayer();
                return;
            }

            if (Projectile.ai[1] > 18f)
                HomeTowardEnemy(980f, 0.085f, 23f);

            if (Projectile.ai[1] > 210f || Vector2.DistanceSquared(Projectile.Center, Owner.Center) > 1500f * 1500f)
                BeginReturn();
        }

        private void StealthAI()
        {
            Projectile.rotation += 0.55f;
            Projectile.localNPCHitCooldown = 12;

            if (!Hovering)
            {
                Projectile.velocity *= 0.925f;
                Projectile.scale = MathHelper.Lerp(Projectile.scale, 1.35f, 0.04f);

                if (Projectile.ai[1] >= StealthSettleTime || Projectile.velocity.Length() <= 0.35f)
                {
                    Projectile.ai[2] = 2f;
                    Projectile.ai[1] = 0f;
                    Projectile.velocity = Vector2.Zero;
                    Projectile.netUpdate = true;
                    SoundEngine.PlaySound(SoundID.Item74 with { Volume = 0.75f, Pitch = 0.1f }, Projectile.Center);
                }

                return;
            }

            float hoverProgress = MathHelper.Clamp(Projectile.ai[1] / StealthHoverTime, 0f, 1f);
            Projectile.velocity = Vector2.Zero;
            Projectile.scale = 1.3f + 0.08f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 8f);
            Projectile.Center += new Vector2(0f, (float)Math.Sin(Projectile.ai[1] * 0.045f) * 0.15f);

            ReleaseStealthBarrage(hoverProgress);

            if (Projectile.ai[1] >= StealthHoverTime)
            {
                DoStealthFinalBurst();
                Projectile.Kill();
            }
        }

        private void ReturnToPlayer()
        {
            Vector2 toPlayer = Owner.Center - Projectile.Center;
            float distance = toPlayer.Length();

            if (distance > 3000f)
            {
                Projectile.Kill();
                return;
            }

            if (distance < 36f && Main.myPlayer == Projectile.owner)
            {
                Projectile.Kill();
                return;
            }

            Vector2 desiredVelocity = toPlayer.SafeNormalize(Vector2.UnitY) * ReturnSpeed;
            Projectile.velocity = (Projectile.velocity * (ReturnInertia - 1f) + desiredVelocity) / ReturnInertia;
            Projectile.rotation += 0.7f * Math.Sign(Projectile.velocity.X == 0f ? Owner.direction : Projectile.velocity.X);
        }

        private void BeginReturn()
        {
            Projectile.ai[2] = 1f;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.netUpdate = true;
            SoundEngine.PlaySound(SoundID.Item7 with { Volume = 0.55f, Pitch = 0.15f }, Projectile.Center);
        }

        private void HomeTowardEnemy(float range, float turnStrength, float maxSpeed)
        {
            NPC target = FindClosestNPC(range);
            if (target == null)
                return;

            float speed = MathHelper.Clamp(Projectile.velocity.Length(), 14f, maxSpeed);
            Vector2 desiredVelocity = (target.Center - Projectile.Center).SafeNormalize(Projectile.velocity.SafeNormalize(Vector2.UnitY)) * speed;
            Projectile.velocity = Vector2.Lerp(Projectile.velocity, desiredVelocity, turnStrength);
        }

        private NPC FindClosestNPC(float range)
        {
            NPC target = null;
            float sqrRange = range * range;

            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!npc.CanBeChasedBy(Projectile))
                    continue;

                float sqrDistance = Vector2.DistanceSquared(npc.Center, Projectile.Center);
                if (sqrDistance > sqrRange)
                    continue;

                sqrRange = sqrDistance;
                target = npc;
            }

            return target;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(ModContent.BuffType<StaticDischarge>(), 180);

            if (!StealthStrike)
            {
                SpawnStormfrontLightning(target.Center, Math.Max(1, (int)(Projectile.damage * 0.85f)));
                SpawnDynamicExplosion(target.Center, Math.Max(1, Projectile.damage / 4), 0.75f);

                if (!Returning)
                {
                    normalHits++;
                    if (normalHits >= NormalHitGoal)
                        BeginReturn();
                    else
                        RedirectAfterHit(target);
                }
            }
        }

        private void RedirectAfterHit(NPC lastTarget)
        {
            NPC nextTarget = null;
            float sqrRange = 1100f * 1100f;

            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!npc.CanBeChasedBy(Projectile) || npc.whoAmI == lastTarget.whoAmI)
                    continue;

                float sqrDistance = Vector2.DistanceSquared(npc.Center, Projectile.Center);
                if (sqrDistance > sqrRange)
                    continue;

                sqrRange = sqrDistance;
                nextTarget = npc;
            }

            if (nextTarget == null)
                nextTarget = lastTarget;

            float speed = MathHelper.Clamp(Projectile.velocity.Length() + 2.5f, 18f, 25f);
            Projectile.velocity = (nextTarget.Center - Projectile.Center).SafeNormalize(-Projectile.velocity.SafeNormalize(Vector2.UnitY)) * speed;
            Projectile.netUpdate = true;
        }

        private void ReleaseStealthBarrage(float progress)
        {
            if (Main.myPlayer != Projectile.owner)
                return;

            int electricInterval = (int)MathHelper.Lerp(42f, 10f, progress);
            int seraphInterval = (int)MathHelper.Lerp(68f, 18f, progress);
            int exorcismInterval = (int)MathHelper.Lerp(110f, 34f, progress);

            if (++electricTimer >= electricInterval)
            {
                electricTimer = 0;
                int count = 1 + (int)(progress * 4f);
                for (int i = 0; i < count; i++)
                    SpawnDynamicElectricity(Math.Max(1, (int)(Projectile.damage * 0.18f)), i, count);
            }

            if (++seraphTimer >= seraphInterval)
            {
                seraphTimer = 0;
                int count = 1 + (int)(progress * 3f);
                for (int i = 0; i < count; i++)
                    SpawnSeraphimLight(Math.Max(1, (int)(Projectile.damage * 0.16f)), i, count);
            }

            if (++exorcismTimer >= exorcismInterval)
            {
                exorcismTimer = 0;
                int count = progress > 0.72f ? 2 : 1;
                for (int i = 0; i < count; i++)
                    SpawnExorcismShockwave(Math.Max(1, (int)(Projectile.damage * 0.22f)), i, count, true);
            }
        }

        private void SpawnStormfrontLightning(Vector2 targetCenter, int damage)
        {
            if (Main.myPlayer != Projectile.owner)
                return;

            Vector2 spawnPosition = targetCenter - Vector2.UnitY.RotatedByRandom(0.2f) * Main.rand.NextFloat(850f, 1120f);
            Vector2 velocity = (targetCenter - spawnPosition + Main.rand.NextVector2Circular(40f, 20f)).SafeNormalize(Vector2.UnitY) * 15f;
            int lightningID = Projectile.NewProjectile(
                Projectile.GetSource_FromThis(),
                spawnPosition,
                velocity,
                ModContent.ProjectileType<StormfrontLightning>(),
                damage,
                0f,
                Projectile.owner,
                velocity.ToRotation(),
                Main.rand.Next(100));

            if (lightningID.WithinBounds(Main.maxProjectiles))
            {
                Main.projectile[lightningID].CritChance = Projectile.CritChance;
                Main.projectile[lightningID].DamageType = ModContent.GetInstance<RogueDamageClass>();
            }
        }

        private void SpawnDynamicExplosion(Vector2 center, int damage, float scale)
        {
            if (Main.myPlayer != Projectile.owner)
                return;

            int explosionID = Projectile.NewProjectile(
                Projectile.GetSource_FromThis(),
                center,
                Vector2.Zero,
                ModContent.ProjectileType<PlasmaGrenadeSmallExplosion>(),
                damage,
                Projectile.knockBack * 0.5f,
                Projectile.owner);

            if (explosionID.WithinBounds(Main.maxProjectiles))
            {
                Projectile explosion = Main.projectile[explosionID];
                explosion.scale = scale;
                explosion.CritChance = Projectile.CritChance;
                explosion.DamageType = ModContent.GetInstance<RogueDamageClass>();
            }
        }

        private void SpawnDynamicElectricity(int damage, int index, int count)
        {
            NPC target = FindClosestNPC(1200f);
            float angle = MathHelper.TwoPi * (index / (float)Math.Max(1, count)) + Main.rand.NextFloat(-0.35f, 0.35f);
            Vector2 spawnOffset = angle.ToRotationVector2() * Main.rand.NextFloat(36f, 96f);
            Vector2 aimDirection = target != null
                ? (target.Center - Projectile.Center).SafeNormalize(angle.ToRotationVector2())
                : angle.ToRotationVector2();
            Vector2 velocity = aimDirection.RotatedByRandom(0.35f) * 3f;

            Projectile.NewProjectile(
                Projectile.GetSource_FromThis(),
                Projectile.Center + spawnOffset,
                velocity,
                ModContent.ProjectileType<DynamicPursuerElectricity>(),
                damage,
                Projectile.knockBack * 0.25f,
                Projectile.owner,
                velocity.ToRotation(),
                Main.rand.Next(100));
        }

        private void SpawnSeraphimLight(int damage, int index, int count)
        {
            float angle = MathHelper.TwoPi * (index / (float)Math.Max(1, count)) + Main.rand.NextFloat(-0.2f, 0.2f);
            Vector2 position = Projectile.Center + angle.ToRotationVector2() * Main.rand.NextFloat(72f, 180f);
            Vector2 velocity = (Projectile.Center - position).SafeNormalize(Vector2.UnitY).RotatedBy(MathHelper.PiOver2) * 8f;

            Projectile.NewProjectile(
                Projectile.GetSource_FromThis(),
                position,
                velocity,
                ModContent.ProjectileType<SeraphimAngelicLight2>(),
                damage,
                Projectile.knockBack * 0.25f,
                Projectile.owner,
                1f);
        }

        private void SpawnExorcismShockwave(int damage, int index, int count, bool stealth)
        {
            Vector2 position = Projectile.Center;
            if (count > 1)
                position += (MathHelper.TwoPi * index / count).ToRotationVector2() * 170f;

            Projectile shockwave = Projectile.NewProjectileDirect(
                Projectile.GetSource_FromThis(),
                position,
                Vector2.Zero,
                ModContent.ProjectileType<ExorcismShockwave>(),
                damage,
                0f,
                Projectile.owner);

            shockwave.CritChance = Projectile.CritChance;
            shockwave.DamageType = ModContent.GetInstance<RogueDamageClass>();
            if (stealth)
                shockwave.Calamity().stealthStrike = true;
        }

        private void DoStealthFinalBurst()
        {
            if (finalBurstDone)
                return;

            finalBurstDone = true;

            for (int i = 0; i < 96; i++)
            {
                Color color = Main.rand.NextBool() ? new Color(109, 232, 255) : new Color(255, 231, 120);
                Dust dust = Dust.NewDustPerfect(
                    Projectile.Center,
                    Main.rand.NextBool() ? DustID.Electric : DustID.GoldFlame,
                    Main.rand.NextVector2CircularEdge(22f, 22f) * Main.rand.NextFloat(0.35f, 1.15f),
                    80,
                    color,
                    Main.rand.NextFloat(1.15f, 2.1f));
                dust.noGravity = true;
            }

            SoundEngine.PlaySound(SoundID.Item74 with { Volume = 1f, Pitch = -0.25f }, Projectile.Center);
            SoundEngine.PlaySound(SoundID.Item122 with { Volume = 0.85f, Pitch = 0.1f }, Projectile.Center);
            Owner.Calamity().GeneralScreenShakePower = Math.Max(Owner.Calamity().GeneralScreenShakePower, 8f);

            if (Main.myPlayer != Projectile.owner)
                return;

            SpawnDynamicExplosion(Projectile.Center, Math.Max(1, (int)(Projectile.damage * 1.15f)), 2.2f);
            for (int i = 0; i < 6; i++)
            {
                Vector2 offset = (MathHelper.TwoPi * i / 6f).ToRotationVector2() * 150f;
                SpawnDynamicExplosion(Projectile.Center + offset, Math.Max(1, (int)(Projectile.damage * 0.45f)), 1.45f);
            }

            SpawnExorcismShockwave(Math.Max(1, (int)(Projectile.damage * 1.4f)), 0, 1, true);
            for (int i = 0; i < 8; i++)
                SpawnDynamicElectricity(Math.Max(1, (int)(Projectile.damage * 0.22f)), i, 8);

            for (int i = 0; i < 6; i++)
                SpawnSeraphimLight(Math.Max(1, (int)(Projectile.damage * 0.22f)), i, 6);
        }

        private void DoCommonVisuals()
        {
            Color electricColor = new Color(92, 225, 255);
            Color holyColor = new Color(255, 230, 110);
            Color lightColor = Color.Lerp(electricColor, holyColor, 0.45f + 0.25f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 5f));

            Lighting.AddLight(Projectile.Center, lightColor.ToVector3() * (StealthStrike ? 0.95f : 0.55f));

            if (Main.rand.NextBool(StealthStrike ? 1 : 2))
            {
                Dust dust = Dust.NewDustPerfect(
                    Projectile.Center + Main.rand.NextVector2Circular(24f, 24f),
                    Main.rand.NextBool() ? DustID.Electric : DustID.GoldFlame,
                    -Projectile.velocity * Main.rand.NextFloat(0.04f, 0.14f) + Main.rand.NextVector2Circular(1.6f, 1.6f),
                    90,
                    lightColor,
                    Main.rand.NextFloat(0.85f, StealthStrike ? 1.55f : 1.2f));
                dust.noGravity = true;
            }

            if (StealthStrike && Main.rand.NextBool(3))
            {
                Dust halo = Dust.NewDustPerfect(
                    Projectile.Center + Main.rand.NextVector2CircularEdge(46f, 46f),
                    DustID.TintableDustLighted,
                    Main.rand.NextVector2Circular(0.6f, 0.6f),
                    100,
                    holyColor,
                    Main.rand.NextFloat(0.8f, 1.25f));
                halo.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Type].Value;
            Vector2 origin = texture.Size() * 0.5f;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            Color trailColor = StealthStrike ? new Color(255, 232, 118) : new Color(90, 226, 255);

            for (int i = Projectile.oldPos.Length - 1; i >= 0; i--)
            {
                Vector2 oldDrawPosition = Projectile.oldPos[i] + Projectile.Size * 0.5f - Main.screenPosition;
                float completion = 1f - i / (float)Projectile.oldPos.Length;
                Color afterimageColor = trailColor * completion * (StealthStrike ? 0.48f : 0.32f);
                Main.EntitySpriteDraw(texture, oldDrawPosition, null, afterimageColor, Projectile.rotation, origin, Projectile.scale, SpriteEffects.None, 0f);
            }

            Main.EntitySpriteDraw(texture, drawPosition, null, Color.White, Projectile.rotation, origin, Projectile.scale, SpriteEffects.None, 0f);
            return false;
        }
    }
}
