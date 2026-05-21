using CalamityMod.Particles;
using CalamityMod;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.PristineFury
{
    internal sealed class PristineFuryHook : ModProjectile, ILocalizedModType
    {
        private enum HookState
        {
            Firing,
            Returning
        }

        public new string LocalizationCategory => "Projectiles.PristineFury";
        public override string Texture => "CalamityMod/Projectiles/DraedonsArsenal/ShortCircuitHook";

        private HookState State
        {
            get => (HookState)(int)Projectile.ai[0];
            set => Projectile.ai[0] = (int)value;
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

            Projectile.rotation = Projectile.velocity.ToRotation();
            Lighting.AddLight(Projectile.Center, new Vector3(0.26f, 0.54f, 0.72f));
        }

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            if (!PristineFuryMarkHelper.TryGetMarkFromNPC(target, out _))
                modifiers.SourceDamage *= 5f;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            Player owner = Main.player[Projectile.owner];
            if (PristineFuryMarkHelper.TryGetMarkFromNPC(target, out PristineFuryMark mark))
            {
                owner.GetModPlayer<PristineFuryPlayer>().ExtractMark(mark);
                SpawnHitBurst(PristineFuryMarkHelper.GetColor(mark));
                SoundEngine.PlaySound(SoundID.Item93 with { Volume = 0.9f, Pitch = 0.2f }, Projectile.Center);
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

            Projectile.penetrate = -1;
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
                Particle spark = i % 2 == 0
                    ? new SparkParticle(
                        Projectile.Center,
                        velocity,
                        false,
                        Main.rand.Next(12, 22),
                        Main.rand.NextFloat(0.65f, 1.15f),
                        Color.Lerp(color, Color.White, Main.rand.NextFloat(0.1f, 0.32f)))
                    : new CustomSpark(
                        Projectile.Center,
                        velocity,
                        "CalamityMod/Particles/GlowSpark2",
                        false,
                        Main.rand.Next(10, 18),
                        Main.rand.NextFloat(0.04f, 0.08f),
                        color,
                        new Vector2(0.45f, 1.45f),
                        glowCenter: true,
                        shrinkSpeed: 0.64f,
                        extraRotation: velocity.ToRotation());
                GeneralParticleHandler.SpawnParticle(spark);
            }

            GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(Projectile.Center, Vector2.Zero, color * 0.7f, Vector2.One, 0f, 0.08f, 0.46f, 18));
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Player owner = Main.player[Projectile.owner];
            Texture2D texture = TextureAssets.Projectile[Type].Value;
            Texture2D line = ModContent.Request<Texture2D>("CalamityMod/Particles/ThinEndedLine").Value;
            Vector2 start = owner.MountedCenter - Main.screenPosition;
            Vector2 end = Projectile.Center - Main.screenPosition;
            Vector2 between = end - start;
            float length = between.Length();
            Vector2 direction = between.SafeNormalize(Vector2.UnitX);
            Color chainColor = new Color(120, 236, 255, 0) * 0.82f;

            for (float i = 14f; i < length; i += 12f)
            {
                Vector2 drawPosition = start + direction * i;
                Main.EntitySpriteDraw(line, drawPosition, null, chainColor, direction.ToRotation() + MathHelper.PiOver2, line.Size() * 0.5f, new Vector2(0.012f, 0.75f), SpriteEffects.None, 0);
            }

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
