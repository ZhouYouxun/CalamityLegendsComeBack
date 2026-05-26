using CalamityLegendsComeBack.Accssory.BF.Common;
using CalamityLegendsComeBack.Weapons.BlossomFlux;
using CalamityLegendsComeBack.Weapons.BlossomFlux.LeftClick;
using CalamityLegendsComeBack.Weapons.BlossomFlux.RightClick;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Accssory.BF.SeedOfSilva
{
    internal sealed class SeedOfSilvaSeed : ModProjectile
    {
        public const int SeedCount = 5;

        private const float OrbitRadius = 222f;
        private const float OrbitSpeed = 0.018f;
        private const float DrawScaleFactor = 0.3f;
        private const float DaisyMaxLife = 15f;
        private const float DaisyRegenPerFrame = 1.2f / 60f;
        private const int TorchflowerExplosionCooldownFrames = 15;

        private float daisyLife = DaisyMaxLife;
        private float daisyStoredHeal;
        private int daisyHitCooldown;
        private int delphiniumCooldown;
        private int torchflowerCooldown;
        private int mandrakeSporeCooldown;
        private int mandrakeDartCooldown;

        public BlossomFluxChloroplastPresetType CurrentPreset { get; private set; }
        public bool IsBlooming { get; private set; }

        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetDefaults()
        {
            Projectile.width = 28;
            Projectile.height = 28;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 2;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 30;
        }

        public override bool? CanDamage() => Projectile.damage > 0 ? null : false;

        public override void AI()
        {
            Player owner = Main.player[Projectile.owner];
            if (!owner.active || owner.dead || !owner.GetModPlayer<BFAccessoryPlayer>().SeedOfSilvaEquipped)
            {
                Projectile.Kill();
                return;
            }

            Projectile.timeLeft = 2;
            BFAccessoryPlayer accessoryPlayer = owner.GetModPlayer<BFAccessoryPlayer>();

            int slot = (int)Projectile.ai[0];
            if (slot < 0 || slot >= SeedCount)
            {
                Projectile.Kill();
                return;
            }

            CurrentPreset = PresetFromSlot(slot);
            IsBlooming = accessoryPlayer.HoldingBlossomFlux && accessoryPlayer.CurrentPreset == CurrentPreset;
            Projectile.damage = GetSeedContactDamage(owner, accessoryPlayer.HoldingBlossomFlux);

            float angle = Main.GameUpdateCount * OrbitSpeed + MathHelper.TwoPi * slot / SeedCount;
            float pulseRadius = OrbitRadius + (float)System.Math.Sin(Main.GlobalTimeWrappedHourly * 1.1f + slot) * 5f;
            Projectile.Center = owner.Center + angle.ToRotationVector2() * pulseRadius + new Vector2(0f, owner.gfxOffY - 8f);
            Lighting.AddLight(Projectile.Center, GetFlowerColor(CurrentPreset).ToVector3() * (IsBlooming ? 0.38f : 0.12f));

            if (torchflowerCooldown > 0)
                torchflowerCooldown--;

            if (!IsBlooming)
                return;

            switch (CurrentPreset)
            {
                case BlossomFluxChloroplastPresetType.Chlo_BRecov:
                    UpdateDaisy(owner);
                    break;
                case BlossomFluxChloroplastPresetType.Chlo_CDetec:
                    UpdateDelphinium(owner);
                    break;
                case BlossomFluxChloroplastPresetType.Chlo_DBomb:
                    UpdateTorchflower();
                    break;
                case BlossomFluxChloroplastPresetType.Chlo_EPlague:
                    UpdateMandrake(owner);
                    break;
            }
        }

        public bool TryTriggerTorchflowerExplosion(Projectile triggeringProjectile)
        {
            if (!IsBlooming || CurrentPreset != BlossomFluxChloroplastPresetType.Chlo_DBomb || torchflowerCooldown > 0)
                return false;

            torchflowerCooldown = TorchflowerExplosionCooldownFrames;

            if (Projectile.owner == Main.myPlayer)
            {
                int damage = System.Math.Max(1, (int)(triggeringProjectile.damage * 0.55f));
                Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    Projectile.Center,
                    Vector2.Zero,
                    ModContent.ProjectileType<BFLeafBombExplosion>(),
                    damage,
                    triggeringProjectile.knockBack * 0.4f,
                    Projectile.owner,
                    116f,
                    1f);
            }

            EmitFlowerBurst(new Color(255, 138, 72), 14);
            return true;
        }

        private void UpdateDaisy(Player owner)
        {
            daisyLife = System.Math.Min(DaisyMaxLife, daisyLife + DaisyRegenPerFrame);
            daisyStoredHeal += DaisyRegenPerFrame;

            if (daisyHitCooldown > 0)
            {
                daisyHitCooldown--;
                return;
            }

            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!npc.CanBeChasedBy() || !npc.Hitbox.Intersects(Projectile.Hitbox))
                    continue;

                int incomingDamage = System.Math.Max(1, npc.damage);
                if (incomingDamage < 20)
                    continue;

                daisyHitCooldown = 30;
                daisyLife = System.Math.Max(0f, daisyLife - System.Math.Max(1f, incomingDamage * (1f - owner.endurance) - owner.statDefense * 0.5f));
                int healAmount = (int)System.Math.Floor(daisyStoredHeal);
                if (healAmount > 0 && owner.statLife < owner.statLifeMax2)
                {
                    owner.statLife = System.Math.Min(owner.statLifeMax2, owner.statLife + healAmount);
                    owner.HealEffect(healAmount, true);
                    daisyStoredHeal -= healAmount;
                }

                EmitFlowerBurst(new Color(150, 255, 174), 8);
                break;
            }
        }

        private void UpdateDelphinium(Player owner)
        {
            if (delphiniumCooldown > 0)
            {
                delphiniumCooldown--;
                return;
            }

            NPC target = FindTarget(680f);
            if (target is null)
                return;

            delphiniumCooldown = 90;
            if (Projectile.owner == Main.myPlayer)
            {
                int damage = System.Math.Max(1, (int)(owner.GetWeaponDamage(owner.HeldItem) * 0.28f));
                Vector2 velocity = (target.Center - Projectile.Center).SafeNormalize(Vector2.UnitY) * 10f;
                Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    Projectile.Center,
                    velocity.RotatedByRandom(0.32f),
                    ModContent.ProjectileType<SeedOfSilvaDelphiniumArc>(),
                    damage,
                    0.5f,
                    Projectile.owner,
                    target.whoAmI);
            }
        }

        private void UpdateTorchflower()
        {
            Lighting.AddLight(Projectile.Center, new Vector3(0.48f, 0.16f, 0.04f));
            if (!Main.dedServ && Main.rand.NextBool(5))
                EmitFlowerBurst(new Color(255, 136, 58), 1);
        }

        private void UpdateMandrake(Player owner)
        {
            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!npc.CanBeChasedBy() || Vector2.DistanceSquared(npc.Center, Projectile.Center) > 230f * 230f)
                    continue;

                npc.GetGlobalNPC<BFAccessoryGlobalNPC>().ApplyMandrakeSlow(Projectile.owner, 12);
            }

            if (mandrakeSporeCooldown > 0)
                mandrakeSporeCooldown--;

            if (mandrakeDartCooldown > 0)
                mandrakeDartCooldown--;

            NPC target = FindTarget(620f);
            if (target is null || Projectile.owner != Main.myPlayer)
                return;

            if (mandrakeSporeCooldown <= 0)
            {
                mandrakeSporeCooldown = 42;
                int damage = System.Math.Max(1, (int)(owner.GetWeaponDamage(owner.HeldItem) * 0.16f));
                Vector2 velocity = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(2.2f, 4.2f);
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, velocity, ModContent.ProjectileType<SeedOfSilvaMandrakeSpore>(), damage, 0.2f, Projectile.owner, target.whoAmI);
            }

            if (mandrakeDartCooldown <= 0)
            {
                mandrakeDartCooldown = 128;
                int damage = System.Math.Max(1, (int)(owner.GetWeaponDamage(owner.HeldItem) * 0.34f));
                Vector2 velocity = (target.Center - Projectile.Center).SafeNormalize(Vector2.UnitY).RotatedByRandom(0.28f) * 9.5f;
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, velocity, ModContent.ProjectileType<SeedOfSilvaMandrakeDart>(), damage, 0.6f, Projectile.owner, target.whoAmI);
            }
        }

        private NPC FindTarget(float range)
        {
            NPC bestTarget = null;
            float bestDistance = range;
            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!npc.CanBeChasedBy())
                    continue;

                float distance = Vector2.Distance(npc.Center, Projectile.Center);
                if (distance >= bestDistance)
                    continue;

                bestTarget = npc;
                bestDistance = distance;
            }

            return bestTarget;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Texture2D magic = ModContent.Request<Texture2D>("CalamityLegendsComeBack/Texture/KsTexture/magic_03").Value;
            Texture2D circle = ModContent.Request<Texture2D>("CalamityLegendsComeBack/Texture/KsTexture/circle_04").Value;
            Texture2D spark = ModContent.Request<Texture2D>("CalamityMod/Particles/GlowSpark").Value;
            Vector2 center = Projectile.Center - Main.screenPosition;
            Color flowerColor = GetFlowerColor(CurrentPreset);
            Color accent = GetFlowerAccentColor(CurrentPreset);
            float pulse = 0.9f + 0.1f * (float)System.Math.Sin(Main.GlobalTimeWrappedHourly * 3.2f + Projectile.identity);

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);

            if (!IsBlooming)
            {
                Color seedColor = Color.Lerp(new Color(74, 118, 70), flowerColor, 0.28f);
                Main.EntitySpriteDraw(bloom, center, null, seedColor * 0.22f, 0f, bloom.Size() * 0.5f, 0.055f * pulse * DrawScaleFactor, SpriteEffects.None, 0);
                Main.EntitySpriteDraw(magic, center, null, seedColor * 0.34f, Main.GlobalTimeWrappedHourly * 0.26f, magic.Size() * 0.5f, 0.038f * DrawScaleFactor, SpriteEffects.None, 0);
                Main.EntitySpriteDraw(spark, center, null, Color.Lerp(seedColor, Color.White, 0.25f) * 0.48f, Main.GlobalTimeWrappedHourly, spark.Size() * 0.5f, new Vector2(0.035f, 0.095f) * DrawScaleFactor, SpriteEffects.None, 0);

                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
                return false;
            }

            float flowerOpen = 1f;
            Main.EntitySpriteDraw(bloom, center, null, flowerColor * 0.46f, 0f, bloom.Size() * 0.5f, 0.085f * pulse * DrawScaleFactor, SpriteEffects.None, 0);
            Main.EntitySpriteDraw(circle, center, null, accent * 0.32f, -Main.GlobalTimeWrappedHourly * 0.32f, circle.Size() * 0.5f, 0.075f * flowerOpen * DrawScaleFactor, SpriteEffects.None, 0);

            int petals = CurrentPreset == BlossomFluxChloroplastPresetType.Chlo_ABreak ? 10 : 8;
            for (int i = 0; i < petals; i++)
            {
                float angle = MathHelper.TwoPi * i / petals + Main.GlobalTimeWrappedHourly * 0.18f;
                Vector2 offset = angle.ToRotationVector2() * (8f + 7f * flowerOpen) * pulse * DrawScaleFactor;
                Main.EntitySpriteDraw(spark, center + offset, null, Color.Lerp(flowerColor, accent, 0.45f) * 0.62f, angle + MathHelper.PiOver2, spark.Size() * 0.5f, new Vector2(0.05f, 0.16f + 0.05f * flowerOpen) * DrawScaleFactor, SpriteEffects.None, 0);
            }

            Main.EntitySpriteDraw(magic, center, null, flowerColor * 0.42f, Main.GlobalTimeWrappedHourly * 0.44f, magic.Size() * 0.5f, 0.058f * flowerOpen * DrawScaleFactor, SpriteEffects.None, 0);

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
            return false;
        }

        private static BlossomFluxChloroplastPresetType PresetFromSlot(int slot) => slot switch
        {
            1 => BlossomFluxChloroplastPresetType.Chlo_BRecov,
            2 => BlossomFluxChloroplastPresetType.Chlo_CDetec,
            3 => BlossomFluxChloroplastPresetType.Chlo_DBomb,
            4 => BlossomFluxChloroplastPresetType.Chlo_EPlague,
            _ => BlossomFluxChloroplastPresetType.Chlo_ABreak
        };

        private static int GetSeedContactDamage(Player owner, bool holdingBlossomFlux)
        {
            if (holdingBlossomFlux)
                return System.Math.Max(1, (int)(owner.GetWeaponDamage(owner.HeldItem) * 0.16f));

            return System.Math.Max(1, (int)owner.GetTotalDamage(DamageClass.Ranged).ApplyTo(18f));
        }

        private void EmitFlowerBurst(Color color, int amount)
        {
            if (Main.dedServ)
                return;

            for (int i = 0; i < amount; i++)
            {
                Dust dust = Dust.NewDustPerfect(Projectile.Center, DustID.GemEmerald, Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(0.8f, 2.2f), 100, color, Main.rand.NextFloat(0.7f, 1f));
                dust.noGravity = true;
            }
        }

        private static Color GetFlowerColor(BlossomFluxChloroplastPresetType preset) => preset switch
        {
            BlossomFluxChloroplastPresetType.Chlo_ABreak => new Color(255, 220, 70),
            BlossomFluxChloroplastPresetType.Chlo_BRecov => new Color(185, 255, 196),
            BlossomFluxChloroplastPresetType.Chlo_CDetec => new Color(126, 170, 255),
            BlossomFluxChloroplastPresetType.Chlo_DBomb => new Color(255, 126, 70),
            BlossomFluxChloroplastPresetType.Chlo_EPlague => new Color(178, 92, 220),
            _ => new Color(124, 255, 148)
        };

        private static Color GetFlowerAccentColor(BlossomFluxChloroplastPresetType preset) => preset switch
        {
            BlossomFluxChloroplastPresetType.Chlo_ABreak => new Color(255, 255, 210),
            BlossomFluxChloroplastPresetType.Chlo_BRecov => new Color(240, 255, 238),
            BlossomFluxChloroplastPresetType.Chlo_CDetec => new Color(220, 244, 255),
            BlossomFluxChloroplastPresetType.Chlo_DBomb => new Color(255, 214, 132),
            BlossomFluxChloroplastPresetType.Chlo_EPlague => new Color(202, 255, 92),
            _ => new Color(220, 255, 230)
        };
    }

    internal sealed class SeedOfSilvaDelphiniumArc : ModProjectile
    {
        private ref float TargetIndex => ref Projectile.ai[0];
        private ref float Timer => ref Projectile.localAI[0];

        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 24;
            ProjectileID.Sets.TrailingMode[Type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = 18;
            Projectile.height = 18;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 72;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }

        public override void AI()
        {
            Timer++;
            NPC target = GetTarget();
            if (target != null)
            {
                Vector2 desired = (target.Center - Projectile.Center).SafeNormalize(Projectile.velocity.SafeNormalize(Vector2.UnitY)) * 13.5f;
                Projectile.velocity = Vector2.Lerp(Projectile.velocity.RotatedBy((float)System.Math.Sin(Timer * 0.22f) * 0.035f), desired, 0.18f);
            }

            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
            Lighting.AddLight(Projectile.Center, new Vector3(0.08f, 0.2f, 0.46f));

            if (!Main.dedServ && Main.rand.NextBool(2))
            {
                Vector2 sideVelocity = Projectile.velocity.SafeNormalize(Vector2.UnitY).RotatedBy(MathHelper.PiOver2 * Main.rand.NextFromList(-1, 1));
                Dust dust = Dust.NewDustPerfect(
                    Projectile.Center + Main.rand.NextVector2Circular(5f, 5f),
                    DustID.Electric,
                    sideVelocity * Main.rand.NextFloat(0.8f, 2.1f) - Projectile.velocity * 0.04f,
                    100,
                    new Color(128, 208, 255),
                    Main.rand.NextFloat(0.65f, 1.05f));
                dust.noGravity = true;
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.GetGlobalNPC<BFAccessoryGlobalNPC>().ApplyDelphiniumStun(Projectile.owner, 60);

            if (BFArrowCommon.InBounds(Projectile.owner, Main.maxPlayers))
            {
                Player owner = Main.player[Projectile.owner];
                owner.wingTime = System.Math.Min(owner.wingTimeMax, owner.wingTime + 30f);
            }

            SoundEngine.PlaySound(SoundID.Item9 with { Volume = 0.24f, Pitch = 0.58f }, target.Center);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            DrawTrail(new Color(106, 166, 255), new Color(226, 246, 255));
            return false;
        }

        private NPC GetTarget()
        {
            if (TargetIndex >= 0f && TargetIndex < Main.maxNPCs && Main.npc[(int)TargetIndex].CanBeChasedBy())
                return Main.npc[(int)TargetIndex];

            return null;
        }

        private void DrawTrail(Color mainColor, Color accentColor)
        {
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Texture2D spark = ModContent.Request<Texture2D>("CalamityMod/Particles/GlowSpark").Value;
            for (int i = 0; i < Projectile.oldPos.Length; i++)
            {
                float completion = 1f - i / (float)Projectile.oldPos.Length;
                Vector2 drawPosition = Projectile.oldPos[i] + Projectile.Size * 0.5f - Main.screenPosition;
                Color trailColor = Color.Lerp(mainColor, accentColor, completion);
                Main.EntitySpriteDraw(bloom, drawPosition, null, trailColor * (0.34f * completion), 0f, bloom.Size() * 0.5f, 0.035f + completion * 0.03f, SpriteEffects.None, 0);
                Main.EntitySpriteDraw(spark, drawPosition, null, trailColor * (0.52f * completion), Projectile.rotation, spark.Size() * 0.5f, new Vector2(0.045f, 0.2f * completion), SpriteEffects.None, 0);
            }

            Vector2 center = Projectile.Center - Main.screenPosition;
            Main.EntitySpriteDraw(bloom, center, null, accentColor * 0.55f, 0f, bloom.Size() * 0.5f, 0.055f, SpriteEffects.None, 0);
            Main.EntitySpriteDraw(spark, center, null, Color.White * 0.82f, Projectile.rotation, spark.Size() * 0.5f, new Vector2(0.06f, 0.24f), SpriteEffects.None, 0);
        }
    }

    internal sealed class SeedOfSilvaTorchFlame : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetDefaults()
        {
            Projectile.width = 26;
            Projectile.height = 26;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.penetrate = 2;
            Projectile.timeLeft = 34;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 12;
        }

        public override void AI()
        {
            Projectile.velocity *= 0.985f;
            Lighting.AddLight(Projectile.Center, new Vector3(0.52f, 0.18f, 0.04f));
            if (Main.rand.NextBool(2))
            {
                Dust dust = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(10f, 10f), DustID.Torch, -Projectile.velocity * 0.04f + Main.rand.NextVector2Circular(0.5f, 0.5f), 100, new Color(255, 150, 70), Main.rand.NextFloat(1f, 1.35f));
                dust.noGravity = true;
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.OnFire, 180);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Main.EntitySpriteDraw(bloom, Projectile.Center - Main.screenPosition, null, new Color(255, 110, 42, 0) * 0.62f, 0f, bloom.Size() * 0.5f, 0.085f, SpriteEffects.None, 0);
            return false;
        }
    }

    internal sealed class SeedOfSilvaMandrakeSpore : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetDefaults()
        {
            Projectile.width = 22;
            Projectile.height = 22;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 150;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
        }

        public override void AI()
        {
            HomeToTarget(0.08f, 8f);
            Projectile.velocity *= 0.992f;
            Lighting.AddLight(Projectile.Center, new Vector3(0.16f, 0.22f, 0.04f));
            if (Main.rand.NextBool(3))
            {
                Dust dust = Dust.NewDustPerfect(Projectile.Center, DustID.GreenTorch, Main.rand.NextVector2Circular(0.55f, 0.55f), 100, new Color(184, 235, 80), Main.rand.NextFloat(0.6f, 0.9f));
                dust.noGravity = true;
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.GetGlobalNPC<BFAccessoryGlobalNPC>().ApplyMandrakeSlow(Projectile.owner, 180);
            BFPlaguePollutionNPC pollution = target.GetGlobalNPC<BFPlaguePollutionNPC>();
            pollution.ApplyPollution(target);
            pollution.ApplyPlagueDebuffs(target, false);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Main.EntitySpriteDraw(bloom, Projectile.Center - Main.screenPosition, null, new Color(178, 255, 68, 0) * 0.48f, 0f, bloom.Size() * 0.5f, 0.06f, SpriteEffects.None, 0);
            return false;
        }

        private void HomeToTarget(float responsiveness, float speed)
        {
            NPC target = GetTarget();
            if (target is null)
                return;

            Vector2 desired = (target.Center - Projectile.Center).SafeNormalize(Projectile.velocity.SafeNormalize(Vector2.UnitY)) * speed;
            Projectile.velocity = Vector2.Lerp(Projectile.velocity, desired, responsiveness);
        }

        private NPC GetTarget()
        {
            if (Projectile.ai[0] >= 0f && Projectile.ai[0] < Main.maxNPCs && Main.npc[(int)Projectile.ai[0]].CanBeChasedBy())
                return Main.npc[(int)Projectile.ai[0]];

            return null;
        }
    }

    internal sealed class SeedOfSilvaMandrakeDart : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

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
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 110;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
        }

        public override void AI()
        {
            NPC target = GetTarget();
            if (target != null)
            {
                Vector2 desired = (target.Center - Projectile.Center).SafeNormalize(Projectile.velocity.SafeNormalize(Vector2.UnitY)) * 15f;
                Projectile.velocity = Vector2.Lerp(Projectile.velocity, desired, 0.18f);
            }

            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
            Lighting.AddLight(Projectile.Center, new Vector3(0.16f, 0.05f, 0.2f));
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.GetGlobalNPC<BFAccessoryGlobalNPC>().ApplyMandrakeSlow(Projectile.owner, 180);
            target.AddBuff(BuffID.Poisoned, 180);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D spark = ModContent.Request<Texture2D>("CalamityMod/Particles/GlowSpark").Value;
            for (int i = 0; i < Projectile.oldPos.Length; i++)
            {
                float completion = 1f - i / (float)Projectile.oldPos.Length;
                Main.EntitySpriteDraw(spark, Projectile.oldPos[i] + Projectile.Size * 0.5f - Main.screenPosition, null, new Color(188, 92, 228, 0) * (0.4f * completion), Projectile.rotation, spark.Size() * 0.5f, new Vector2(0.05f, 0.18f * completion), SpriteEffects.None, 0);
            }

            Main.EntitySpriteDraw(spark, Projectile.Center - Main.screenPosition, null, new Color(205, 255, 92, 0) * 0.75f, Projectile.rotation, spark.Size() * 0.5f, new Vector2(0.07f, 0.24f), SpriteEffects.None, 0);
            return false;
        }

        private NPC GetTarget()
        {
            if (Projectile.ai[0] >= 0f && Projectile.ai[0] < Main.maxNPCs && Main.npc[(int)Projectile.ai[0]].CanBeChasedBy())
                return Main.npc[(int)Projectile.ai[0]];

            return null;
        }
    }

    internal sealed class PastLingeringShard : ModProjectile
    {
        private ref float TargetIndex => ref Projectile.ai[0];
        private ref float Timer => ref Projectile.localAI[0];

        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 16;
            ProjectileID.Sets.TrailingMode[Type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = 18;
            Projectile.height = 18;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 110;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.extraUpdates = 1;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }

        public override void AI()
        {
            Timer++;
            NPC target = FindTarget();
            if (target != null)
            {
                Vector2 aimPoint = target.Center + target.velocity * 0.2f;
                Vector2 desired = (aimPoint - Projectile.Center).SafeNormalize(Projectile.velocity.SafeNormalize(Vector2.UnitY)) * 18f;
                Projectile.velocity = Vector2.Lerp(Projectile.velocity, desired, 0.2f);
            }

            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
            Lighting.AddLight(Projectile.Center, new Vector3(0.18f, 0.06f, 0.34f));
        }

        public override void OnKill(int timeLeft)
        {
            if (Main.dedServ)
                return;

            for (int i = 0; i < 7; i++)
            {
                Dust dust = Dust.NewDustPerfect(Projectile.Center, DustID.PurpleTorch, Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(1f, 3.2f), 100, new Color(188, 98, 255), Main.rand.NextFloat(0.72f, 1f));
                dust.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D spark = ModContent.Request<Texture2D>("CalamityMod/Particles/GlowSpark").Value;
            for (int i = 0; i < Projectile.oldPos.Length; i++)
            {
                float completion = 1f - i / (float)Projectile.oldPos.Length;
                Main.EntitySpriteDraw(spark, Projectile.oldPos[i] + Projectile.Size * 0.5f - Main.screenPosition, null, new Color(168, 92, 255, 0) * (0.46f * completion), Projectile.rotation, spark.Size() * 0.5f, new Vector2(0.045f, 0.2f * completion), SpriteEffects.None, 0);
            }

            Main.EntitySpriteDraw(spark, Projectile.Center - Main.screenPosition, null, new Color(232, 248, 255, 0) * 0.74f, Projectile.rotation, spark.Size() * 0.5f, new Vector2(0.06f, 0.24f), SpriteEffects.None, 0);
            return false;
        }

        private NPC FindTarget()
        {
            if (TargetIndex >= 0f && TargetIndex < Main.maxNPCs && Main.npc[(int)TargetIndex].CanBeChasedBy())
                return Main.npc[(int)TargetIndex];

            NPC bestTarget = null;
            float bestDistance = 780f;
            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!npc.CanBeChasedBy())
                    continue;

                float distance = Vector2.Distance(npc.Center, Projectile.Center);
                if (distance >= bestDistance)
                    continue;

                bestDistance = distance;
                bestTarget = npc;
            }

            return bestTarget;
        }
    }
}
