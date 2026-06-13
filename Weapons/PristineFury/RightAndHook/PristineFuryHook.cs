using CalamityLegendsComeBack.Weapons.PristineFury.LeftEffect;
using CalamityMod.Particles;
using CalamityMod;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.PristineFury
{
    internal sealed class PristineFuryHook : ModProjectile, ILocalizedModType
    {
        private const int AttachDurationFrames = 180;

        private enum HookState
        {
            Firing,
            Attached,
            Returning
        }

        public new string LocalizationCategory => "Projectiles.PristineFury";
        public override string Texture => "CalamityMod/Projectiles/DraedonsArsenal/ShortCircuitHook";

        private HookState State
        {
            get => (HookState)(int)Projectile.ai[0];
            set => Projectile.ai[0] = (int)value;
        }

        private ref float AttachTimer => ref Projectile.ai[1];

        private int AttachedTargetIndex
        {
            get => (int)Projectile.localAI[0];
            set => Projectile.localAI[0] = value;
        }

        public override void SetDefaults()
        {
            Projectile.width = 14;
            Projectile.height = 10;
            Projectile.friendly = true;
            Projectile.penetrate = 1;
            Projectile.extraUpdates = 3;
            Projectile.tileCollide = false;
            Projectile.ownerHitCheck = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.timeLeft = 120;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 18;
        }

        public override void AI()
        {
            Player owner = Main.player[Projectile.owner];
            if (!owner.active || owner.dead)
            {
                Projectile.Kill();
                return;
            }

            if (State == HookState.Firing && Projectile.Distance(owner.Center) > 860f)
                StartReturning();

            if (State == HookState.Attached)
            {
                if (!TryGetAttachedTarget(out NPC target))
                {
                    StartReturning();
                }
                else
                {
                    Projectile.friendly = true;
                    Projectile.tileCollide = false;
                    Projectile.Center = target.Center;
                    Projectile.velocity = Vector2.Zero;
                    Projectile.timeLeft = Math.Max(Projectile.timeLeft, 30);

                    AttachTimer++;
                    if (AttachTimer >= AttachDurationFrames)
                    {
                        CompleteAttachment(owner, target);
                        return;
                    }
                }
            }

            if (State == HookState.Returning)
            {
                Projectile.friendly = false;
                Projectile.tileCollide = false;
                Vector2 toOwner = Projectile.SafeDirectionTo(owner.MountedCenter);
                Projectile.velocity = Vector2.Lerp(Projectile.velocity, toOwner * 24f, 0.16f);

                if (Projectile.Hitbox.Intersects(owner.Hitbox))
                {
                    Projectile.Kill();
                    return;
                }
            }

            Vector2 rotationVector = State == HookState.Attached
                ? owner.MountedCenter.DirectionTo(Projectile.Center)
                : Projectile.velocity;
            Projectile.rotation = rotationVector.SafeNormalize(Vector2.UnitX * owner.direction).ToRotation();

            Color markColor = PristineFuryMarkHelper.GetColor(owner.GetModPlayer<PristineFuryPlayer>().CurrentMark);
            float lightIntensity = State == HookState.Attached ? 0.72f : 0.44f;
            Lighting.AddLight(Projectile.Center, markColor.ToVector3() * lightIntensity);

            if (State == HookState.Attached && !Main.dedServ && (int)AttachTimer % 5 == 0)
                SpawnAttachedParticles(markColor);
        }

        private void SpawnAttachedParticles(Color markColor)
        {
            if (!TryGetAttachedTarget(out NPC target))
                return;

            for (int i = 0; i < 2; i++)
            {
                GeneralParticleHandler.SpawnParticle(new SparkParticle(
                    target.Center + Main.rand.NextVector2Circular(target.width * 0.3f, target.height * 0.3f),
                    Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(1.8f, 4.5f),
                    false, Main.rand.Next(10, 18),
                    Main.rand.NextFloat(0.48f, 0.88f),
                    Color.Lerp(markColor, Color.White, Main.rand.NextFloat(0.15f, 0.5f))));
            }

            GeneralParticleHandler.SpawnParticle(new PointParticle(
                Projectile.Center + Main.rand.NextVector2Circular(8f, 8f),
                Main.rand.NextVector2Circular(1.5f, 1.5f),
                false, Main.rand.Next(8, 16),
                Main.rand.NextFloat(0.55f, 0.92f),
                Color.Lerp(markColor, Color.White, Main.rand.NextFloat(0.25f, 0.6f)), true));
        }

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            if (State == HookState.Firing &&
                !PristineFuryMarkHelper.TryGetMarkFromNPC(target, out _) &&
                !PristineFuryMarkHelper.IsProvidenceLockedRavager(target))
            {
                modifiers.SourceDamage *= 5f;
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (State == HookState.Attached)
            {
                if (target.whoAmI == AttachedTargetIndex)
                    target.AddBuff(BuffID.Electrified, 90);
                return;
            }

            if (State != HookState.Firing)
                return;

            if (PristineFuryMarkHelper.IsProvidenceLockedRavager(target))
            {
                StartReturning();
                return;
            }

            Color burstColor = PristineFuryMarkHelper.TryGetMarkFromNPC(target, out PristineFuryMark mark)
                ? PristineFuryMarkHelper.GetColor(mark)
                : new Color(255, 180, 74);
            StartAttached(target);
            SpawnHitBurst(burstColor);
            SoundEngine.PlaySound(SoundID.Item93 with { Volume = 0.9f, Pitch = 0.2f }, Projectile.Center);
        }

        public override bool? CanHitNPC(NPC target)
        {
            if (State == HookState.Returning)
                return false;

            if (State == HookState.Attached)
                return target.whoAmI == AttachedTargetIndex;

            return null;
        }

        private bool TryGetAttachedTarget(out NPC target)
        {
            int targetIndex = AttachedTargetIndex;
            if (targetIndex < 0 || targetIndex >= Main.maxNPCs)
            {
                target = null;
                return false;
            }

            target = Main.npc[targetIndex];
            return target.active && target.life > 0;
        }

        private void StartAttached(NPC target)
        {
            AttachedTargetIndex = target.whoAmI;
            AttachTimer = 0f;
            Projectile.Center = target.Center;
            Projectile.velocity = Vector2.Zero;
            Projectile.penetrate = -1;
            Projectile.extraUpdates = 0;
            Projectile.tileCollide = false;
            Projectile.timeLeft = AttachDurationFrames + 90;
            Projectile.netUpdate = true;
            State = HookState.Attached;
        }

        private void CompleteAttachment(Player owner, NPC target)
        {
            if (PristineFuryMarkHelper.TryGetMarkFromNPC(target, out PristineFuryMark mark))
            {
                owner.GetModPlayer<PristineFuryPlayer>().ExtractMark(mark);
                SpawnHitBurst(PristineFuryMarkHelper.GetColor(mark));
                SoundEngine.PlaySound(SoundID.Item93 with { Volume = 0.9f, Pitch = -0.05f }, Projectile.Center);
            }
            else
            {
                SpawnHitBurst(new Color(255, 180, 74));
                Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    Projectile.Center,
                    Vector2.Zero,
                    ModContent.ProjectileType<PristineFuryImpactExplosion>(),
                    Projectile.damage,
                    0f,
                    Projectile.owner,
                    90f);
                SoundEngine.PlaySound(SoundID.Item14 with { Volume = 0.8f, Pitch = 0.35f }, Projectile.Center);
            }

            StartReturning();
        }

        private void StartReturning()
        {
            if (State == HookState.Returning)
                return;

            AttachTimer = 0f;
            Projectile.penetrate = -1;
            Projectile.extraUpdates = 3;
            Projectile.timeLeft = Math.Max(Projectile.timeLeft, 90);
            Projectile.netUpdate = true;
            State = HookState.Returning;
        }

        private void SpawnHitBurst(Color color)
        {
            if (Main.dedServ)
                return;

            for (int i = 0; i < 24; i++)
            {
                Vector2 velocity = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(2f, 7f);
                Particle spark = new SparkParticle(
                    Projectile.Center,
                    velocity,
                    false,
                    Main.rand.Next(12, 22),
                    Main.rand.NextFloat(0.65f, 1.15f),
                    Color.Lerp(color, Color.White, Main.rand.NextFloat(0.1f, 0.32f)));
                GeneralParticleHandler.SpawnParticle(spark);
            }

            GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(Projectile.Center, Vector2.Zero, color * 0.7f, Vector2.One, 0f, 0.08f, 0.46f, 18));
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Player owner = Main.player[Projectile.owner];
            Texture2D texture = TextureAssets.Projectile[Type].Value;
            Texture2D line = ModContent.Request<Texture2D>("CalamityMod/Particles/ThinEndedLine").Value;
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Texture2D halfStar = ModContent.Request<Texture2D>("CalamityMod/Particles/HalfStar").Value;

            Color markColor = PristineFuryMarkHelper.GetColor(owner.GetModPlayer<PristineFuryPlayer>().CurrentMark);
            Vector2 start = owner.MountedCenter - Main.screenPosition;
            Vector2 end = Projectile.Center - Main.screenPosition;
            Vector2 between = end - start;
            float length = between.Length();
            Vector2 dir = between.SafeNormalize(Vector2.UnitX);

            float attachProgress = State == HookState.Attached ? MathHelper.Clamp(AttachTimer / 25f, 0f, 1f) : 0f;
            float pulse = 0.80f + 0.20f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 7.5f + Projectile.identity * 0.42f);
            Color chainOuter = (Color.Lerp(markColor, Color.White, 0.32f) with { A = 0 }) * (0.46f + attachProgress * 0.34f) * pulse;
            Color chainInner = (markColor with { A = 0 }) * (0.28f + attachProgress * 0.18f);
            float chainStep = 11f;

            PFLeftEffectRules.BeginAdditive();

            // Outer glow chain
            for (float i = 10f; i < length; i += chainStep)
            {
                Vector2 pos = start + dir * i;
                Main.EntitySpriteDraw(line, pos, null, chainOuter, dir.ToRotation() + MathHelper.PiOver2,
                    line.Size() * 0.5f, new Vector2(0.022f + attachProgress * 0.012f, 0.70f), SpriteEffects.None, 0);
            }

            // Inner bright core chain
            for (float i = 10f; i < length; i += chainStep)
            {
                Vector2 pos = start + dir * i;
                Main.EntitySpriteDraw(line, pos, null, chainInner, dir.ToRotation() + MathHelper.PiOver2,
                    line.Size() * 0.5f, new Vector2(0.008f, 0.72f), SpriteEffects.None, 0);
            }

            // Hook head bloom
            Main.EntitySpriteDraw(bloom, end, null, (markColor with { A = 0 }) * (0.62f + attachProgress * 0.52f) * pulse,
                Projectile.rotation, bloom.Size() * 0.5f, new Vector2(0.26f, 0.18f), SpriteEffects.None, 0);
            Main.EntitySpriteDraw(bloom, end, null, (Color.White with { A = 0 }) * (0.38f + attachProgress * 0.28f),
                Projectile.rotation, bloom.Size() * 0.5f, new Vector2(0.10f, 0.07f), SpriteEffects.None, 0);

            // Rotating HalfStars at hook head
            int starCount = State == HookState.Attached ? 4 : 3;
            for (int i = 0; i < starCount; i++)
            {
                float rot = Projectile.rotation + MathHelper.TwoPi * i / starCount + Main.GlobalTimeWrappedHourly * (1.3f + attachProgress * 1.5f);
                Main.EntitySpriteDraw(halfStar, end, null, (markColor with { A = 0 }) * (0.52f + attachProgress * 0.38f) * pulse,
                    rot, halfStar.Size() * 0.5f, new Vector2(0.14f, (0.55f + attachProgress * 0.48f) * pulse), SpriteEffects.None, 0);
            }

            // Origin glow at player hand
            Main.EntitySpriteDraw(bloom, start, null, (markColor with { A = 0 }) * 0.28f * pulse,
                0f, bloom.Size() * 0.5f, 0.14f, SpriteEffects.None, 0);

            PFLeftEffectRules.EndAdditive();

            // Hook texture on top
            Main.EntitySpriteDraw(texture, end, null, lightColor, Projectile.rotation, texture.Size() * 0.5f, Projectile.scale, SpriteEffects.None, 0);
            return false;
        }
    }

    internal static class PristineFuryTargeting
    {
        internal static NPC FindTarget(Vector2 origin, float range, Player owner)
        {
            NPC closest = null;
            float closestDistance = range * range;

            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];
                if (!npc.CanBeChasedBy(owner))
                    continue;

                float distance = Vector2.DistanceSquared(origin, npc.Center);
                if (distance >= closestDistance)
                    continue;

                closestDistance = distance;
                closest = npc;
            }

            return closest;
        }
    }
}
