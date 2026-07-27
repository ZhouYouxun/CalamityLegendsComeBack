using System.IO;
using CalamityLegendsComeBack.Weapons.BrinyBaron.CommonAttack.ForShuriken;
using CalamityLegendsComeBack.Weapons.BrinyBaron.SkillA_ShortDash;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.BrinyBaron.Passive_QuickDash
{
    // A cardinal portal dash: leave the cursor portal, cut while exiting it, then
    // return through the same line. Its visible movement deliberately reuses the
    // ordinary right-click dash language so the two variants never drift apart.
    internal class BrinyBaron_VortexEyeDash : ModProjectile
    {
        private const int ExitFrames = 12;
        private const int ReturnFrames = 12;
        private const float ExitDistance = 300f;

        private Vector2 origin;
        private Vector2 portal;
        private Vector2 exit;
        private int state;
        private int timer;
        private bool initialized;
        private bool spawnedExitSlash;
        private bool spawnedReturnSlash;
        private bool spawnedTornado;

        private Player Owner => Main.player[Projectile.owner];
        private Vector2 Direction => Projectile.velocity.SafeNormalize(Vector2.UnitX);

        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetDefaults()
        {
            Projectile.width = 40;
            Projectile.height = 40;
            Projectile.friendly = false;
            Projectile.hostile = false;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = ExitFrames + ReturnFrames + 20;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.netImportant = true;
        }

        public override bool ShouldUpdatePosition() => false;

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.WriteVector2(origin);
            writer.WriteVector2(portal);
            writer.WriteVector2(exit);
            writer.Write((byte)state);
            writer.Write((byte)timer);
            writer.Write(initialized);
            writer.Write(spawnedExitSlash);
            writer.Write(spawnedReturnSlash);
            writer.Write(spawnedTornado);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            origin = reader.ReadVector2();
            portal = reader.ReadVector2();
            exit = reader.ReadVector2();
            state = reader.ReadByte();
            timer = reader.ReadByte();
            initialized = reader.ReadBoolean();
            spawnedExitSlash = reader.ReadBoolean();
            spawnedReturnSlash = reader.ReadBoolean();
            spawnedTornado = reader.ReadBoolean();
        }

        public override void AI()
        {
            Player owner = Owner;
            if (!owner.active || owner.dead)
            {
                Projectile.Kill();
                return;
            }

            if (!initialized)
                Initialize(owner);

            owner.immune = true;
            owner.immuneTime = 2;
            owner.noKnockback = true;
            owner.velocity = Vector2.Zero;
            owner.ChangeDir(Direction.X == 0f ? owner.direction : System.Math.Sign(Direction.X));

            if (state == 0)
            {
                timer++;
                float progress = Utils.GetLerpValue(0f, ExitFrames, timer, true);
                Vector2 previousCenter = owner.Center;
                owner.Center = Vector2.Lerp(portal, exit, SmoothStep(progress));
                Projectile.velocity = owner.Center - previousCenter;
                if (!spawnedExitSlash)
                {
                    SpawnDashSlash(owner, Direction);
                    BrinyBaron_SkillDashTornado_FlightEffects.SpawnDashStartEffects(Projectile, Direction);
                    spawnedExitSlash = true;
                }
                BrinyBaron_SkillDashTornado_FlightEffects.SpawnDashFlightEffects(Projectile, Direction, Direction.ToRotation() + MathHelper.PiOver4, timer * 0.24f, timer);

                if (timer >= ExitFrames)
                {
                    state = 1;
                    timer = 0;
                    Projectile.netUpdate = true;
                }
            }
            else
            {
                timer++;
                float progress = Utils.GetLerpValue(0f, ReturnFrames, timer, true);
                Vector2 previousCenter = owner.Center;
                owner.Center = Vector2.Lerp(exit, origin, SmoothStep(progress));
                Projectile.velocity = owner.Center - previousCenter;
                if (!spawnedReturnSlash)
                {
                    SpawnDashSlash(owner, -Direction);
                    spawnedReturnSlash = true;
                }
                BrinyBaron_SkillDashTornado_FlightEffects.SpawnReboundFlightEffects(Projectile, Direction, Direction.ToRotation() + MathHelper.PiOver4, timer * 0.24f, timer);

                if (timer >= ReturnFrames)
                {
                    if (!spawnedTornado)
                        SpawnReturnTornado(owner);
                    Projectile.Kill();
                }
            }

            Projectile.Center = owner.Center;
        }

        private void Initialize(Player owner)
        {
            initialized = true;
            origin = owner.Center;
            portal = Main.MouseWorld;
            Vector2 portalTopLeft = portal - owner.Size * 0.5f;
            if (Collision.SolidCollision(portalTopLeft, owner.width, owner.height))
                portal = origin + Direction * 240f;

            exit = portal + Direction * ExitDistance;
            Vector2 exitTopLeft = exit - owner.Size * 0.5f;
            if (Collision.SolidCollision(exitTopLeft, owner.width, owner.height))
                exit = portal + Direction * 160f;

            owner.Center = portal;
            Projectile.netUpdate = true;
            SoundEngine.PlaySound(SoundID.Item6 with { Volume = 0.75f, Pitch = 0.18f }, portal);
        }

        private void SpawnDashSlash(Player owner, Vector2 direction)
        {
            if (Main.myPlayer != Projectile.owner)
                return;

            Projectile.NewProjectile(
                Projectile.GetSource_FromThis(),
                owner.MountedCenter,
                direction,
                ModContent.ProjectileType<BrinyBaron_SkillSlashDash_SlashDash>(),
                Projectile.damage,
                Projectile.knockBack,
                Projectile.owner,
                0f,
                direction.X < 0f ? -1f : 1f);
        }

        private void SpawnReturnTornado(Player owner)
        {
            spawnedTornado = true;
            if (Main.myPlayer != Projectile.owner)
                return;

            // One enlarged water tornado sits in the middle of the return cut.
            SpawnTornado(Vector2.Lerp(origin, exit, 0.5f));
        }

        private void SpawnTornado(Vector2 position)
        {
            int index = Projectile.NewProjectile(
                Projectile.GetSource_FromThis(),
                position,
                Vector2.Zero,
                ModContent.ProjectileType<CalamityMod.Projectiles.Melee.BrinyTyphoonBubble>(),
                Projectile.damage,
                Projectile.knockBack,
                Projectile.owner);

            if (Main.projectile.IndexInRange(index))
                Main.projectile[index].scale = 2f;
        }

        private static float SmoothStep(float value) => value * value * (3f - 2f * value);
    }

    // The portal is visible while the weapon is held. It deliberately has no hitbox.
    internal class BrinyBaron_VortexEyePortalPreview : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 36;
            Projectile.friendly = false;
            Projectile.hostile = false;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 2;
        }

        public override bool ShouldUpdatePosition() => false;

        public override void AI()
        {
            if (Main.myPlayer != Projectile.owner)
                return;

            Projectile.timeLeft = 2;
            Projectile.Center = Main.MouseWorld;
            if (Main.GameUpdateCount % 4 != 0)
                return;

            for (int i = 0; i < 3; i++)
            {
                Vector2 velocity = Main.rand.NextVector2Circular(1.6f, 1.6f);
                Dust dust = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(20f, 20f), DustID.Water, velocity, 90, new Color(80, 210, 255), Main.rand.NextFloat(0.85f, 1.25f));
                dust.noGravity = true;
            }
        }
    }
}
