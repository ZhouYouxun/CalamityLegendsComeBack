using System;
using CalamityLegendsComeBack.Weapons.YharimsCrystal.Passive;
using CalamityMod;
using CalamityMod.Graphics.Primitives;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.Enums;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.YharimsCrystal.EXSkill
{
    public class YC_EX_VIP : ModProjectile, ILocalizedModType
    {
        private const int BladeChargeTime = 180;
        private const int BladeCleanupTime = 48;
        private const int CrystalChargeTime = 120;
        private const int CrystalStormTime = 600;
        private readonly BalanceYharimsCrystal balance = new();

        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";
        public new string LocalizationCategory => "Projectiles.YharimsCrystal";

        private YCWeaponForm Mode => (YCWeaponForm)(int)Projectile.ai[0];
        private ref float Timer => ref Projectile.localAI[0];
        private ref float Fired => ref Projectile.localAI[1];

        public int CurrentStateTimer => (int)Timer;

        public override void SetDefaults()
        {
            Projectile.width = 18;
            Projectile.height = 18;
            Projectile.friendly = false;
            Projectile.hostile = false;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.hide = true;
            Projectile.netImportant = true;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.timeLeft = 2;
        }

        public override bool ShouldUpdatePosition() => false;
        public override bool? CanDamage() => false;

        public override void OnSpawn(Terraria.DataStructures.IEntitySource source)
        {
            Player owner = Main.player[Projectile.owner];
            if (balance.UltimateEmpowersAfterUse())
                owner.GetModPlayer<YharimsCrystalStatePlayer>().EmpowerLastWeapon(8 * 60);

            SoundEngine.PlaySound(Mode == YCWeaponForm.Blade
                ? new SoundStyle("CalamityMod/Sounds/Item/FinalDawnSlash") { Volume = 0.72f, Pitch = -0.25f }
                : SoundID.Item122 with { Volume = 0.62f, Pitch = -0.25f }, owner.Center);
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
            Projectile.Center = owner.Center;
            Projectile.timeLeft = 2;

            if (Mode == YCWeaponForm.Blade)
                DoBladeUltimate(owner);
            else
                DoCrystalUltimate(owner);
        }

        private void DoBladeUltimate(Player owner)
        {
            Vector2 aim = GetAimDirection(owner);
            Projectile.velocity = aim;

            owner.heldProj = Projectile.whoAmI;
            owner.itemTime = 2;
            owner.itemAnimation = 2;
            owner.ChangeDir(aim.X >= 0f ? 1 : -1);
            owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, aim.ToRotation() * owner.gravDir);

            float charge = MathHelper.Clamp(Timer / BladeChargeTime, 0f, 1f);
            EmitBladeChargeFX(owner, aim, charge);

            if (Timer == BladeChargeTime && Projectile.owner == Main.myPlayer)
            {
                SpawnBladeFinisher(owner, aim);
                SpawnFirePillars(owner, aim);
                owner.Calamity().GeneralScreenShakePower = Math.Max(owner.Calamity().GeneralScreenShakePower, 11f);
            }

            if (Timer > BladeChargeTime + BladeCleanupTime)
                Projectile.Kill();
        }

        private void DoCrystalUltimate(Player owner)
        {
            Vector2 aim = GetAimDirection(owner);
            Projectile.velocity = aim;
            float charge = MathHelper.Clamp(Timer / CrystalChargeTime, 0f, 1f);
            EmitCrystalChargeFX(owner, aim, charge);

            if (Timer == CrystalChargeTime)
            {
                SoundEngine.PlaySound(new SoundStyle("CalamityMod/Sounds/Custom/Providence/ProvidenceHolyRay") { Volume = 0.66f, Pitch = -0.16f }, owner.Center);
                owner.Calamity().GeneralScreenShakePower = Math.Max(owner.Calamity().GeneralScreenShakePower, 7f);
            }

            if (Timer >= CrystalChargeTime && Timer <= CrystalChargeTime + CrystalStormTime && Projectile.owner == Main.myPlayer)
            {
                int interval = balance.GetRightLaserTier() >= YCRightLaserVisualTier.Providence ? 6 : 8;
                if ((int)(Timer - CrystalChargeTime) % interval == 0)
                    SpawnStormLaser(owner);
            }

            if (Timer > CrystalChargeTime + CrystalStormTime + 24)
                Projectile.Kill();
        }

        private void SpawnBladeFinisher(Player owner, Vector2 aim)
        {
            int slash = Projectile.NewProjectile(
                Projectile.GetSource_FromThis(),
                owner.Center + aim * 210f - Vector2.UnitY * 40f,
                aim,
                ModContent.ProjectileType<YC_EXBladeGrandSlash>(),
                Projectile.damage,
                Projectile.knockBack,
                Projectile.owner,
                aim.ToRotation());

            if (Main.projectile.IndexInRange(slash))
            {
                YharimsCrystalHellBladeGlobalProjectile.Mark(Main.projectile[slash], YCWeaponForm.Blade);
                Main.projectile[slash].CritChance = Projectile.CritChance;
            }
        }

        private void SpawnFirePillars(Player owner, Vector2 aim)
        {
            int count = balance.GetCompletedStageIndex() >= 8 ? 9 : 7;
            for (int i = 0; i < count; i++)
            {
                float distance = 160f + i * 92f;
                Vector2 spawn = owner.Center + aim * distance + Vector2.UnitY * 240f;
                int pillar = Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    spawn,
                    -Vector2.UnitY,
                    ModContent.ProjectileType<YC_EXFirePillar>(),
                    Math.Max(1, (int)(Projectile.damage * 0.42f)),
                    Projectile.knockBack * 0.35f,
                    Projectile.owner,
                    i * 5f,
                    MathHelper.Lerp(0.75f, 1.25f, i / (float)Math.Max(1, count - 1)));

                if (Main.projectile.IndexInRange(pillar))
                {
                    YharimsCrystalHellBladeGlobalProjectile.Mark(Main.projectile[pillar], YCWeaponForm.Blade);
                    Main.projectile[pillar].CritChance = Projectile.CritChance;
                }
            }
        }

        private void SpawnStormLaser(Player owner)
        {
            Vector2 focus = NewLegendYharimsCrystal.GetMouseWorld(owner);
            NPC target = focus.ClosestNPCAt(1150f);
            if (target != null)
                focus = Vector2.Lerp(focus, target.Center, 0.72f);

            float angle = Main.rand.NextFloat(MathHelper.TwoPi);
            float distance = Main.rand.NextFloat(720f, 1220f);
            Vector2 spawn = focus + angle.ToRotationVector2() * distance;
            Vector2 direction = (focus - spawn).SafeNormalize(Vector2.UnitY).RotatedByRandom(0.16f);

            int laser = Projectile.NewProjectile(
                Projectile.GetSource_FromThis(),
                spawn,
                direction,
                ModContent.ProjectileType<YC_EXStormLaser>(),
                Math.Max(1, (int)(Projectile.damage * 0.36f)),
                Projectile.knockBack * 0.2f,
                Projectile.owner,
                (float)balance.GetRightLaserTier());

            if (Main.projectile.IndexInRange(laser))
            {
                YharimsCrystalHellBladeGlobalProjectile.Mark(Main.projectile[laser], YCWeaponForm.Crystal);
                Main.projectile[laser].CritChance = Projectile.CritChance;
            }
        }

        private Vector2 GetAimDirection(Player owner)
        {
            if (Projectile.owner == Main.myPlayer)
            {
                Vector2 aim = (NewLegendYharimsCrystal.GetMouseWorld(owner) - owner.MountedCenter).SafeNormalize(Vector2.UnitX * owner.direction);
                Projectile.velocity = aim;
                Projectile.netUpdate = true;
                return aim;
            }

            return Projectile.velocity.SafeNormalize(Vector2.UnitX * owner.direction);
        }

        private void EmitBladeChargeFX(Player owner, Vector2 aim, float charge)
        {
            if (Main.dedServ)
                return;

            if (Main.GameUpdateCount % Math.Max(2, (int)MathHelper.Lerp(8f, 2f, charge)) == 0)
            {
                Vector2 orbit = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(80f, 190f);
                Vector2 position = owner.Center + aim * MathHelper.Lerp(40f, 150f, charge) + orbit * (1f - charge * 0.45f);
                Vector2 velocity = -orbit.SafeNormalize(Vector2.UnitY) * Main.rand.NextFloat(2f, 6f + charge * 6f);
                Color color = Main.rand.NextBool(4) ? Color.White : Color.Lerp(new Color(255, 80, 32), new Color(255, 222, 90), Main.rand.NextFloat());

                GeneralParticleHandler.SpawnParticle(new CustomSpark(
                    position,
                    velocity,
                    "CalamityMod/Particles/BloomCircle",
                    false,
                    Main.rand.Next(14, 24),
                    Main.rand.NextFloat(0.26f, 0.5f) * (0.65f + charge),
                    color,
                    new Vector2(0.62f, 1.3f),
                    shrinkSpeed: 0.72f));
            }

            if ((int)Timer % 38 == 0)
                SoundEngine.PlaySound(new SoundStyle("CalamityMod/Sounds/Item/HellkiteSwing", 2) { Volume = 0.32f + charge * 0.24f, Pitch = -0.3f + charge * 0.24f }, owner.Center);
        }

        private void EmitCrystalChargeFX(Player owner, Vector2 aim, float charge)
        {
            if (Main.dedServ)
                return;

            if (Main.GameUpdateCount % Math.Max(2, (int)MathHelper.Lerp(7f, 2f, charge)) == 0)
            {
                Vector2 normal = aim.RotatedBy(MathHelper.PiOver2);
                Vector2 position = owner.Center + aim * Main.rand.NextFloat(60f, 220f) + normal * Main.rand.NextFloat(-120f, 120f) * (1f - charge * 0.45f);
                Vector2 velocity = aim * Main.rand.NextFloat(2f, 8f + charge * 8f);
                GeneralParticleHandler.SpawnParticle(new GlowOrbParticle(
                    position,
                    velocity,
                    false,
                    Main.rand.Next(12, 20),
                    Main.rand.NextFloat(0.32f, 0.62f) * (0.5f + charge),
                    Main.rand.NextBool(4) ? Color.White : Color.Lerp(new Color(255, 76, 32), new Color(255, 220, 92), Main.rand.NextFloat()),
                    true,
                    false,
                    true));
            }

            if ((int)Timer % 30 == 0)
                SoundEngine.PlaySound(SoundID.Item34 with { Volume = 0.18f + charge * 0.18f, Pitch = -0.35f + charge * 0.12f, MaxInstances = 5 }, owner.Center);
        }
    }

    internal sealed class YC_EXBladeGrandSlash : ModProjectile, ILocalizedModType
    {
        private static readonly Color Gold = new(255, 218, 92);
        private static readonly Color Orange = new(255, 84, 36);

        public new string LocalizationCategory => "Projectiles.YharimsCrystal";
        public override string Texture => "CalamityMod/Items/Weapons/Melee/Earth";

        private ref float Timer => ref Projectile.localAI[0];

        public override void SetDefaults()
        {
            Projectile.width = 180;
            Projectile.height = 180;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 42;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
            Projectile.scale = 2.1f;
        }

        public override void OnSpawn(Terraria.DataStructures.IEntitySource source)
        {
            Projectile.rotation = Projectile.ai[0] + MathHelper.PiOver4;
            SoundEngine.PlaySound(new SoundStyle("CalamityMod/Sounds/Item/FinalDawnSlash") { Volume = 0.95f, Pitch = -0.18f }, Projectile.Center);
            YharimsCrystalHellBladeGlobalProjectile.Mark(Projectile, YCWeaponForm.Blade);
        }

        public override bool? CanDamage() => Timer > 5f && Timer < 30f ? null : false;

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            Vector2 start = Projectile.Center - direction * 90f - direction.RotatedBy(MathHelper.PiOver2) * 190f;
            Vector2 end = Projectile.Center + direction * 470f + direction.RotatedBy(MathHelper.PiOver2) * 120f;
            float collisionPoint = 0f;
            return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), start, end, 120f, ref collisionPoint);
        }

        public override void AI()
        {
            Timer++;
            Projectile.velocity *= 0.94f;
            Projectile.rotation += MathHelper.ToRadians(7.5f) * (Projectile.velocity.X >= 0f ? 1f : -1f);
            Lighting.AddLight(Projectile.Center, Gold.ToVector3() * 1.1f);

            if (Main.dedServ || Timer % 2f != 0f)
                return;

            Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            Vector2 tangent = direction.RotatedBy(MathHelper.PiOver2);
            for (int i = 0; i < 3; i++)
            {
                Vector2 position = Projectile.Center + direction * Main.rand.NextFloat(-60f, 420f) + tangent * Main.rand.NextFloat(-180f, 120f);
                GeneralParticleHandler.SpawnParticle(new SparkParticle(position, tangent * Main.rand.NextFloat(2f, 8f), false, Main.rand.Next(14, 26), Main.rand.NextFloat(0.8f, 1.5f), Main.rand.NextBool(3) ? Color.White : Gold));
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(new BalanceYharimsCrystal().GetFireDebuffType(), 360);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = Terraria.GameContent.TextureAssets.Projectile[Type].Value;
            Texture2D ghost = ModContent.Request<Texture2D>("CalamityMod/Projectiles/Melee/EarthGhost").Value;
            Texture2D smear = ModContent.Request<Texture2D>("CalamityMod/Particles/VerticalSmearLarge").Value;
            Vector2 origin = new(0f, 186f);
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            float opacity = Utils.GetLerpValue(0f, 8f, Timer, true) * Utils.GetLerpValue(0f, 12f, Projectile.timeLeft, true);
            SpriteEffects effects = Projectile.velocity.X < 0f ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
            if (effects == SpriteEffects.FlipHorizontally)
                origin.X = texture.Width - origin.X;

            Main.EntitySpriteDraw(smear, drawPosition, null, Orange with { A = 0 } * 0.88f * opacity, Projectile.rotation, smear.Size() * 0.5f, Projectile.scale * 0.85f, SpriteEffects.None);
            for (int i = 0; i < 14; i++)
            {
                Vector2 offset = (MathHelper.TwoPi * i / 14f).ToRotationVector2() * 6f * opacity;
                Main.EntitySpriteDraw(ghost, drawPosition + offset, null, Gold with { A = 0 } * 0.13f * opacity, Projectile.rotation, origin, Projectile.scale, effects);
            }

            Main.EntitySpriteDraw(texture, drawPosition, null, Color.White * opacity, Projectile.rotation, origin, Projectile.scale, effects);
            return false;
        }
    }

    internal sealed class YC_EXFirePillar : ModProjectile, ILocalizedModType
    {
        private static readonly Color Gold = new(255, 218, 92);
        private static readonly Color Orange = new(255, 84, 36);

        public new string LocalizationCategory => "Projectiles.YharimsCrystal";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private ref float Timer => ref Projectile.localAI[0];
        private float Height => 620f * Math.Max(0.55f, Projectile.ai[1]);
        private float Width => 38f * Math.Max(0.55f, Projectile.ai[1]);

        public override void SetDefaults()
        {
            Projectile.width = 50;
            Projectile.height = 50;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 82;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 8;
        }

        public override void OnSpawn(Terraria.DataStructures.IEntitySource source)
        {
            Projectile.localAI[0] = -Projectile.ai[0];
            YharimsCrystalHellBladeGlobalProjectile.Mark(Projectile, YCWeaponForm.Blade);
        }

        public override bool? CanDamage() => Timer > 4f && Timer < 54f ? null : false;

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            float collisionPoint = 0f;
            return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), Projectile.Center, Projectile.Center - Vector2.UnitY * Height, Width, ref collisionPoint);
        }

        public override void AI()
        {
            Timer++;
            if (Timer == 1f)
                SoundEngine.PlaySound(new SoundStyle("CalamityMod/Sounds/Custom/Yharon/YharonInfernado") { Volume = 0.24f, Pitch = 0.35f, MaxInstances = 8 }, Projectile.Center);

            Lighting.AddLight(Projectile.Center - Vector2.UnitY * Height * 0.45f, Gold.ToVector3() * 0.75f);
            if (!Main.dedServ && Main.rand.NextBool(2))
            {
                Vector2 position = Projectile.Center - Vector2.UnitY * Main.rand.NextFloat(0f, Height);
                Vector2 velocity = -Vector2.UnitY.RotatedByRandom(0.35f) * Main.rand.NextFloat(2f, 8f);
                GeneralParticleHandler.SpawnParticle(new CustomSpark(position, velocity, "CalamityMod/Particles/BloomCircle", false, Main.rand.Next(12, 22), Main.rand.NextFloat(0.26f, 0.52f), Main.rand.NextBool(3) ? Color.White : Color.Lerp(Orange, Gold, Main.rand.NextFloat()), new Vector2(0.64f, 1.2f), shrinkSpeed: 0.74f));
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(new BalanceYharimsCrystal().GetFireDebuffType(), 300);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Vector2[] points =
            {
                Projectile.Center,
                Projectile.Center - Vector2.UnitY * Height * 0.33f + Vector2.UnitX * (float)Math.Sin(Timer * 0.09f) * 18f,
                Projectile.Center - Vector2.UnitY * Height * 0.66f - Vector2.UnitX * (float)Math.Sin(Timer * 0.12f) * 18f,
                Projectile.Center - Vector2.UnitY * Height
            };

            Main.spriteBatch.EnterShaderRegion();
            GameShaders.Misc["CalamityMod:Bordernado"].UseSaturation(-0.1f);
            GameShaders.Misc["CalamityMod:Bordernado"].UseOpacity(Utils.GetLerpValue(0f, 10f, Timer, true) * Utils.GetLerpValue(0f, 18f, Projectile.timeLeft, true));
            GameShaders.Misc["CalamityMod:Bordernado"].SetShaderTexture(ModContent.Request<Texture2D>("Terraria/Images/Misc/Perlin"));
            PrimitiveRenderer.RenderTrail(points, new PrimitiveSettings(WidthFunction, ColorFunction, shader: GameShaders.Misc["CalamityMod:Bordernado"]), 70);
            Main.spriteBatch.ExitShaderRegion();
            return false;
        }

        private float WidthFunction(float completionRatio, Vector2 vertexPosition)
        {
            float bulge = (float)Math.Sin(completionRatio * MathHelper.Pi);
            return Width * MathHelper.Lerp(0.72f, 1.2f, bulge);
        }

        private Color ColorFunction(float completionRatio, Vector2 vertexPosition)
        {
            return Color.Lerp(Gold, Orange, completionRatio * 0.5f);
        }
    }

    internal sealed class YC_EXStormLaser : ModProjectile, ILocalizedModType
    {
        private const float MaxLength = 1800f;
        private static readonly Color Gold = new(255, 218, 92);
        private static readonly Color Orange = new(255, 84, 36);

        public new string LocalizationCategory => "Projectiles.YharimsCrystal";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private ref float Timer => ref Projectile.localAI[0];
        private ref float BeamLength => ref Projectile.localAI[1];
        private YCRightLaserVisualTier Tier => (YCRightLaserVisualTier)(int)Projectile.ai[0];

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.DrawScreenCheckFluff[Type] = 10000;
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 16;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 38;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 8;
        }

        public override bool ShouldUpdatePosition() => false;

        public override void OnSpawn(Terraria.DataStructures.IEntitySource source)
        {
            YharimsCrystalHellBladeGlobalProjectile.Mark(Projectile, YCWeaponForm.Crystal);
        }

        public override void AI()
        {
            Timer++;
            Projectile.velocity = Projectile.velocity.SafeNormalize(Vector2.UnitY);
            Projectile.rotation = Projectile.velocity.ToRotation();
            BeamLength = MathHelper.Lerp(BeamLength <= 0f ? MaxLength : BeamLength, MaxLength, 0.72f);
            Lighting.AddLight(Projectile.Center, Gold.ToVector3() * 0.52f);

            if (Timer == 1f && Main.rand.NextBool(3))
                SoundEngine.PlaySound(SoundID.Item12 with { Volume = 0.28f, Pitch = -0.25f, MaxInstances = 8 }, Projectile.Center);
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            float collisionPoint = 0f;
            float width = Tier >= YCRightLaserVisualTier.Providence ? 26f : 18f;
            return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), Projectile.Center, Projectile.Center + Projectile.velocity * BeamLength, width, ref collisionPoint);
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(new BalanceYharimsCrystal().GetFireDebuffType(), 240);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            float opacity = Utils.GetLerpValue(0f, 8f, Timer, true) * Utils.GetLerpValue(0f, 12f, Projectile.timeLeft, true);
            if (Tier >= YCRightLaserVisualTier.Providence)
            {
                DrawProvidenceStorm(opacity);
                return false;
            }

            YC_YharimBeamVisuals.DrawYharimBeam(Projectile, BeamLength, (Tier == YCRightLaserVisualTier.MoonLord ? 0.58f : 0.38f), opacity, Color.Lerp(Orange, Gold, 0.5f));
            return false;
        }

        private void DrawProvidenceStorm(float opacity)
        {
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Texture2D line = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomLineFade").Value;
            Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            Vector2 center = Projectile.Center + direction * BeamLength * 0.5f - Main.screenPosition;
            float lengthScale = BeamLength / 1000f;
            Color color = Color.Lerp(Orange, Gold, 0.58f) with { A = 0 };
            Main.EntitySpriteDraw(line, center, null, color * opacity, Projectile.rotation + MathHelper.PiOver2, line.Size() * 0.5f, new Vector2(2.4f, 62f * lengthScale) * 0.012f, SpriteEffects.FlipVertically);
            Main.EntitySpriteDraw(bloom, Projectile.Center - Main.screenPosition, null, Color.White with { A = 0 } * 0.34f * opacity, Projectile.rotation, bloom.Size() * 0.5f, 0.08f, SpriteEffects.None);
        }
    }
}
