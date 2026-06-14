using CalamityMod;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SHPC.Effects.DPreDog
{
    internal class BloodstoneCore_BloodOrb : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.SHPC";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private const int HealAmount = 30;
        private const int WindupFrames = 36;

        private int timer;
        private int choiceState; // 0: undecided, 1: heal owner, 2: attack NPC.
        private int chosenPlayerIndex = -1;
        private int chosenNPCIndex = -1;

        public override bool? CanDamage() => timer > WindupFrames && choiceState == 2 ? null : false;

        public override void SetDefaults()
        {
            Projectile.width = 24;
            Projectile.height = 24;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 210;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 8;
        }

        public override void OnSpawn(IEntitySource source)
        {
            if (Projectile.velocity.LengthSquared() < 0.25f)
                Projectile.velocity = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(3.2f, 5.4f);
        }

        public override void AI()
        {
            timer++;

            if (timer <= WindupFrames)
            {
                UpdateWindup();
                DrawSharedEffects();
                return;
            }

            if (choiceState == 0)
                ChooseBehavior();

            if (choiceState == 1)
            {
                UpdateHealingBehavior();
            }
            else if (choiceState == 2)
            {
                UpdateAttackBehavior();
            }
            else
            {
                DriftInPlace();
            }

            DrawSharedEffects();
        }

        private void UpdateWindup()
        {
            Projectile.velocity *= 0.99f;
            Projectile.rotation += 0.18f * (Projectile.velocity.X >= 0f ? 1f : -1f);

            if (timer % 3 == 0)
            {
                Dust dust = Dust.NewDustPerfect(
                    Projectile.Center + Main.rand.NextVector2Circular(4f, 4f),
                    Main.rand.NextBool() ? DustID.Blood : DustID.RedTorch,
                    -Projectile.velocity * Main.rand.NextFloat(0.08f, 0.22f),
                    40,
                    Color.Lerp(Color.Red, Color.DarkRed, Main.rand.NextFloat()),
                    Main.rand.NextFloat(0.9f, 1.35f));
                dust.noGravity = true;
            }
        }

        private void DrawSharedEffects()
        {
            Projectile.rotation = Projectile.velocity.LengthSquared() > 0.01f
                ? Projectile.velocity.ToRotation()
                : Projectile.rotation + 0.05f;

            Lighting.AddLight(Projectile.Center, new Color(220, 20, 20).ToVector3() * 0.42f);
            SpawnFlightDust();
        }

        private void ChooseBehavior()
        {
            Player owner = Main.player[Projectile.owner];
            bool shouldHeal = owner.active && !owner.dead && Main.rand.NextFloat() < CalculateHealingChance(owner);

            if (shouldHeal)
            {
                choiceState = 1;
                chosenPlayerIndex = Projectile.owner;
                return;
            }

            NPC target = FindNearestTarget();
            if (target != null)
            {
                choiceState = 2;
                chosenNPCIndex = target.whoAmI;
                return;
            }

            if (owner.active && !owner.dead)
            {
                choiceState = 1;
                chosenPlayerIndex = Projectile.owner;
            }
        }

        private static float CalculateHealingChance(Player owner)
        {
            if (owner.statLifeMax2 <= 0)
                return 0.01f;

            float lifeRatio = MathHelper.Clamp(owner.statLife / (float)owner.statLifeMax2, 0f, 1f);
            return MathHelper.Lerp(0.20f, 0.01f, lifeRatio);
        }

        private void UpdateHealingBehavior()
        {
            if (chosenPlayerIndex < 0 || chosenPlayerIndex >= Main.maxPlayers)
            {
                choiceState = 0;
                return;
            }

            Player targetPlayer = Main.player[chosenPlayerIndex];
            if (!targetPlayer.active || targetPlayer.dead)
            {
                choiceState = 0;
                return;
            }

            int actionTimer = timer - WindupFrames;
            float trackingPower = Utils.GetLerpValue(0f, 50f, actionTimer, true);
            Vector2 desired = (targetPlayer.Center - Projectile.Center).SafeNormalize(Projectile.velocity.SafeNormalize(Vector2.UnitX)) *
                MathHelper.Lerp(16f, 25f, trackingPower);
            Projectile.velocity = Vector2.Lerp(Projectile.velocity, desired, MathHelper.Lerp(0.12f, 0.34f, trackingPower));

            if (Projectile.Distance(targetPlayer.Center) >= 28f && !Projectile.Hitbox.Intersects(targetPlayer.Hitbox))
                return;

            targetPlayer.statLife = System.Math.Min(targetPlayer.statLifeMax2, targetPlayer.statLife + HealAmount);
            targetPlayer.HealEffect(HealAmount);

            for (int j = 0; j < 10; j++)
            {
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.Blood, Main.rand.NextVector2Circular(3f, 3f));
                d.noGravity = true;
            }

            Projectile.Kill();
        }

        private void UpdateAttackBehavior()
        {
            NPC targetNPC = GetChosenNPC();
            if (targetNPC is null)
            {
                targetNPC = FindNearestTarget();
                if (targetNPC is null)
                {
                    DriftInPlace();
                    return;
                }

                chosenNPCIndex = targetNPC.whoAmI;
            }

            int actionTimer = timer - WindupFrames;
            float trackingPower = Utils.GetLerpValue(0f, 60f, actionTimer, true);
            Vector2 desired = (targetNPC.Center - Projectile.Center).SafeNormalize(Projectile.velocity.SafeNormalize(Vector2.UnitX)) *
                MathHelper.Lerp(16f, 25f, trackingPower);
            Projectile.velocity = Vector2.Lerp(Projectile.velocity, desired, MathHelper.Lerp(0.12f, 0.34f, trackingPower));
        }

        private NPC GetChosenNPC()
        {
            if (chosenNPCIndex < 0 || chosenNPCIndex >= Main.maxNPCs)
                return null;

            NPC npc = Main.npc[chosenNPCIndex];
            return npc.active && npc.CanBeChasedBy(Projectile) ? npc : null;
        }

        private NPC FindNearestTarget()
        {
            NPC bestTarget = null;
            float bestDistance = 1500f;

            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!npc.CanBeChasedBy(Projectile))
                    continue;

                float distance = Projectile.Distance(npc.Center);
                if (distance >= bestDistance)
                    continue;

                bestDistance = distance;
                bestTarget = npc;
            }

            return bestTarget;
        }

        private void DriftInPlace()
        {
            if (timer < WindupFrames + 60)
                Projectile.velocity *= 0.99f;
            else
                Projectile.velocity *= 0.85f;
        }

        private void SpawnFlightDust()
        {
            if (!Main.rand.NextBool(2))
                return;

            Dust dust = Dust.NewDustPerfect(
                Projectile.Center + Main.rand.NextVector2Circular(3f, 3f),
                Main.rand.NextBool() ? DustID.Blood : DustID.RedTorch,
                -Projectile.velocity * Main.rand.NextFloat(0.05f, 0.22f),
                0,
                Color.Lerp(Color.Red, Color.DarkRed, Main.rand.NextFloat()),
                Main.rand.NextFloat(0.8f, 1.25f));
            dust.noGravity = true;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            float pulse = 0.85f + (float)System.Math.Sin(timer * 0.22f) * 0.12f;
            float windupPulse = timer <= WindupFrames
                ? MathHelper.Lerp(0.85f, 1.25f, Utils.GetLerpValue(0f, WindupFrames, timer, true))
                : 1f;

            Main.spriteBatch.SetBlendState(BlendState.Additive);
            Main.EntitySpriteDraw(
                bloom,
                drawPosition,
                null,
                new Color(220, 24, 24, 120) * 0.72f,
                Projectile.rotation,
                bloom.Size() * 0.5f,
                0.42f * pulse * windupPulse,
                SpriteEffects.None);

            Main.EntitySpriteDraw(
                bloom,
                drawPosition,
                null,
                new Color(255, 210, 210, 100) * 0.32f,
                Projectile.rotation,
                bloom.Size() * 0.5f,
                0.18f * pulse * windupPulse,
                SpriteEffects.None);

            Main.spriteBatch.SetBlendState(BlendState.AlphaBlend);
            return false;
        }
    }
}
