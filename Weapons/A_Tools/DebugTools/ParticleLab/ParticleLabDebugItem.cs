using System;
using System.Collections.Generic;
using CalamityMod;
using CalamityMod.DataStructures;
using CalamityMod.Graphics.Metaballs;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Tools.DebugTools.ParticleLab
{
    public sealed class ParticleLabDebugItem : ModItem, ILocalizedModType
    {
        public static readonly string DemoTexture = "Terraria/Images/Item_" + ItemID.CrystalSerpent;

        private static int PanelType => ModContent.ProjectileType<ParticleLabPanel>();
        private static int TestProjectileType => ModContent.ProjectileType<ParticleLabProjectile>();

        public new string LocalizationCategory => "Items.Weapons";
        public override string Texture => DemoTexture;

        public override bool AltFunctionUse(Player player) => true;

        public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame,
            Color drawColor, Color itemColor, Vector2 origin, float scale)
        {
            DebugToolOutline.Draw(spriteBatch, TextureAssets.Item[Type].Value, position, frame, origin, scale, new Color(150, 120, 255));
            return true;
        }

        public override void SetDefaults()
        {
            Item.width = 44;
            Item.height = 48;
            Item.damage = 0;
            Item.knockBack = 0f;
            Item.DamageType = DamageClass.Generic;
            Item.useTime = 15;
            Item.useAnimation = 15;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.noMelee = true;
            Item.autoReuse = true;
            Item.reuseDelay = 0;
            Item.shoot = TestProjectileType;
            Item.shootSpeed = 13f;
            Item.UseSound = SoundID.Item8;
            Item.value = 0;
            Item.rare = ItemRarityID.Cyan;
            Item.Calamity().devItem = true;
        }

        public override bool CanUseItem(Player player)
        {
            return Main.myPlayer == player.whoAmI &&
                !Main.mapFullscreen &&
                !Main.blockMouse &&
                !player.mouseInterface &&
                !(Main.playerInventory && Main.HoverItem.type == Type);
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            if (player.altFunctionUse == 2)
            {
                TogglePanel(player, source);
                return false;
            }

            int effectIndex = player.GetModPlayer<ParticleLabPlayer>().SelectedEffectIndex;
            Projectile.NewProjectile(source, player.MountedCenter, velocity, TestProjectileType, 0, 0f, player.whoAmI, effectIndex);
            return false;
        }

        private static void TogglePanel(Player player, IEntitySource source)
        {
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile projectile = Main.projectile[i];
                if (!projectile.active || projectile.owner != player.whoAmI || projectile.type != PanelType)
                    continue;

                ParticleLabPanel.RequestClose(projectile);
                SoundEngine.PlaySound(SoundID.MenuClose with { Volume = 0.58f, Pitch = 0.05f }, player.Center);
                return;
            }

            Projectile.NewProjectile(source, player.Center, Vector2.Zero, PanelType, 0, 0f, player.whoAmI, 0f, Main.MouseScreen.X, Main.MouseScreen.Y);
            SoundEngine.PlaySound(SoundID.MenuOpen with { Volume = 0.68f, Pitch = 0.08f }, player.Center);
        }
    }

    internal sealed class ParticleLabPlayer : ModPlayer
    {
        private int selectedEffectIndex;
        private int lastParticleLabPage;

        public int SelectedEffectIndex
        {
            get
            {
                if (!ParticleEffectCatalog.IsValidIndex(selectedEffectIndex))
                    selectedEffectIndex = 0;
                return selectedEffectIndex;
            }
        }

        public void SelectEffect(int effectIndex)
        {
            if (ParticleEffectCatalog.IsValidIndex(effectIndex))
                selectedEffectIndex = effectIndex;
        }

        public int LastParticleLabPage => lastParticleLabPage;

        public void SetLastParticleLabPage(int page) => lastParticleLabPage = Math.Max(0, page);
    }

    internal sealed class ParticleLabProjectile : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.A_Dev";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private const int EffectInterval = 2;
        private const int RingInterval = 60;
        private const int RingParticleCount = 10;

        private int EffectIndex => (int)Projectile.ai[0];

        public override void SetDefaults()
        {
            Projectile.width = 10;
            Projectile.height = 10;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.DamageType = DamageClass.Generic;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 300;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.hide = true;
        }

        public override bool? CanDamage() => false;

        public override void AI()
        {
            int elapsedFrames = (int)++Projectile.localAI[0];
            if (elapsedFrames % EffectInterval == 0)
                ParticleEffectCatalog.Spawn(EffectIndex, Projectile);

            if (elapsedFrames % RingInterval == 0)
                SpawnRing();
        }

        private void SpawnRing()
        {
            float speed = Math.Max(Projectile.velocity.Length(), 1f);
            Vector2 originalVelocity = Projectile.velocity;

            for (int i = 0; i < RingParticleCount; i++)
            {
                Projectile.velocity = (MathHelper.TwoPi * i / RingParticleCount).ToRotationVector2() * speed;
                ParticleEffectCatalog.Spawn(EffectIndex, Projectile);
            }

            Projectile.velocity = originalVelocity;
        }
    }

    internal readonly record struct ParticleEffectDefinition(string Name);
    internal readonly record struct ParticleEffectCategory(string Name);

    internal static class ParticleEffectCatalog
    {
        public static readonly IReadOnlyList<ParticleEffectDefinition> Effects = new ParticleEffectDefinition[]
        {
            new("SparkParticle"),
            new("AltSparkParticle"),
            new("CustomSpark"),
            new("GlowSparkParticle"),
            new("RainbowGlowSparkParticle"),
            new("VelChangingSpark"),
            new("VoidSparkParticle"),
            new("LineParticle"),
            new("AltLineParticle"),
            new("BloomLineVFX"),
            new("StaticGlowLine"),
            new("ThunderBoltVFX"),
            new("ElectricSpark"),
            new("PointParticle"),
            new("GenericSparkle"),
            new("CritSpark"),
            new("SparkleParticle"),
            new("RoundedStarParticle"),
            new("SnowflakeSparkle"),
            new("FancyStars"),
            new("FlareShine"),
            new("CuteManaStarParticle"),
            new("PearlParticle"),
            new("StrongBloom"),
            new("GenericBloom"),
            new("BloomParticle"),
            new("BloomRing"),
            new("GlowOrbParticle"),
            new("FlatGlow"),
            new("DirectionalPulseRing"),
            new("CustomPulse"),
            new("PulseRing"),
            new("StaticPulseRing"),
            new("AuraPulseRing"),
            new("PlayerCenteredPulseRing"),
            new("ConstellationRingVFX"),
            new("DetailedExplosion"),
            new("FlameExplosion"),
            new("PlasmaExplosion"),
            new("ImpactParticle"),
            new("BossRoar"),
            new("HeavySmokeParticle"),
            new("TimedSmokeParticle"),
            new("PlagueHumidifierMist"),
            new("FlameParticle"),
            new("WaterFlavoredParticle"),
            new("WaterFoamParticle"),
            new("WaterGlobParticle"),
            new("SeaPrismParticle"),
            new("GenericBubbleParticle"),
            new("BloodParticle"),
            new("BloodParticle2"),
            new("ChumBone"),
            new("BrokenTendril"),
            new("BrainOfCthulhuAfterImage"),
            new("Jaws"),
            new("StoneDebrisParticle"),
            new("TitaniumRailgunShell"),
            new("WulfrumBastionPartsParticle"),
            new("AresSummonCrateParticle"),
            new("UrchinSpikeParticle"),
            new("HealingPlus"),
            new("StatChangeArrow"),
            new("EmoteExpressionParticle"),
            new("WulfrumDroidEmote"),
            new("WulfrumDroidSweatEmote"),
            new("WulfrumHatParticle"),
            new("ManaDrainBlob"),
            new("LiliesOfFinalityHeartParticle"),
            new("CircularSmearVFX"),
            new("CircularSmearSmokeyVFX"),
            new("SemiCircularSmearVFX"),
            new("SemiCircularSmearFade"),
            new("TrientCircularSmear"),
            new("SlashThrough"),
            new("MantisPunch"),
            new("SquareParticle"),
            new("GlowSquareParticle"),
            new("TechyHoloysquareParticle"),
            new("NanoParticle"),
            new("DestroyerReticleTelegraph"),
            new("DestroyerSparkTelegraph"),
            new("FireParticleSet"),
            new("ChargingEnergyParticleSet"),
            new("AresCannonChargeParticleSet"),
            new("ThanatosSmokeParticleSet"),
            new("SquishyLightParticle"),
            new("CrackParticle"),
            new("DesertProwlerSkullParticle"),
            new("MediumMistParticle"),
            new("SmallSmokeParticle"),
            new("BoltParticle"),
            new("RancorLavaMetaball"),
            new("CalamitasMetaball"),
            new("StreamGougeMetaball")
        };

        // These are intentionally category pages, not a flat list cut into arbitrary 36-item
        // chunks. Each page keeps comparable particles together and the metaballs always finish
        // the book, which makes visual lookup much faster during effect work.
        private static readonly ParticleEffectCategory[] Categories =
        {
            new("Sparks & Trails"),
            new("Small Glows & Stars"),
            new("Large Glows & Pulses"),
            new("Smoke, Flame & Fluid"),
            new("Organic, Debris & Emotes"),
            new("Tech, Telegraph & Utility"),
            new("Mist & Specialty"),
            new("Metaballs")
        };

        private static readonly int[][] CategoryEffectIndices =
        {
            CreateIndices(0, 14),
            CreateIndices(14, 30),
            CreateIndices(30, 48),
            CreateIndices(48, 62),
            CreateIndices(62, 75),
            CreateIndices(75, 88),
            CreateIndices(88, 92),
            CreateIndices(92, 95)
        };

        public static int Count => Effects.Count;
        public static int CategoryCount => Categories.Length;

        public static bool IsValidIndex(int index) => index >= 0 && index < Effects.Count;

        public static string GetCategoryName(int categoryIndex)
        {
            categoryIndex = Math.Clamp(categoryIndex, 0, CategoryCount - 1);
            return Categories[categoryIndex].Name;
        }

        public static IReadOnlyList<int> GetEffectIndicesForCategory(int categoryIndex)
        {
            categoryIndex = Math.Clamp(categoryIndex, 0, CategoryCount - 1);
            return CategoryEffectIndices[categoryIndex];
        }

        private static int[] CreateIndices(int startInclusive, int endExclusive)
        {
            int[] indices = new int[endExclusive - startInclusive];
            for (int i = 0; i < indices.Length; i++)
                indices[i] = startInclusive + i;
            return indices;
        }

        public static void Spawn(int effectIndex, Projectile projectile)
        {
            if (!IsValidIndex(effectIndex))
                effectIndex = 0;

            Player owner = Main.player[projectile.owner];
            NPC target = FindNearestTarget(projectile.Center);
            Vector2 forward = projectile.velocity.SafeNormalize(Vector2.UnitX);
            if (projectile.velocity.LengthSquared() < 0.01f)
                forward = Vector2.UnitX;

            switch (effectIndex)
            {
            case 0:
            {
                Particle sparkParticle = new SparkParticle(
                                projectile.Center, // 生成位置
                                projectile.velocity * 0.2f, // 初始速度
                                false, // 是否受重力影响
                                60, // 生命周期，单位是帧
                                1.0f, // 缩放大小
                                Color.Orange // 颜色
                            );
                            GeneralParticleHandler.SpawnParticle(sparkParticle);
                break;
            }
            case 1:
            {
                Particle altSparkParticle = new AltSparkParticle(
                                projectile.Center, // 生成位置
                                projectile.velocity * 0.05f, // 初始速度
                                false, // 是否受重力影响
                                24, // 生命周期，单位是帧
                                1.2f, // 缩放大小
                                Color.Cyan * 0.8f // 颜色
                            );
                            GeneralParticleHandler.SpawnParticle(altSparkParticle);
                break;
            }
            case 2:
            {
                Particle customSpark = new CustomSpark(
                                projectile.Center, // 生成位置
                                projectile.velocity * 0.15f, // 初始速度
                                "CalamityMod/Particles/BloomLineSoftEdge", // 是否受重力影响
                                false, // 生命周期，单位是帧
                                3, // 缩放大小
                                0.9f, // 颜色
                                Color.Orange * 0.85f, // 拉伸或压缩比例
                                new Vector2(2.8f, 0.7f), // 开关参数
                                true, // 开关参数
                                true, // 旋转角度或方向
                                0f, // 开关参数
                                false, // 开关参数
                                false, // 数值参数
                                0.65f, // 数值参数
                                1f, // 数值参数
                                1f, // 数值参数
                                false, // 开关参数
                                false, // 开关参数
                                0f // 旋转速度
                            );
                            GeneralParticleHandler.SpawnParticle(customSpark);
                break;
            }
            case 3:
            {
                Particle glowSparkParticle = new GlowSparkParticle(
                                projectile.Center, // 生成位置
                                projectile.velocity * 0.1f, // 初始速度
                                false, // 是否受重力影响
                                22, // 生命周期，单位是帧
                                0.8f, // 缩放大小
                                Color.DeepSkyBlue, // 颜色
                                new Vector2(0.01f, 0.05f), // 拉伸或压缩比例
                                false, // 开关参数
                                true, // 开关参数
                                0.8f // 旋转角度或方向
                            );
                            GeneralParticleHandler.SpawnParticle(glowSparkParticle);
                break;
            }
            case 4:
            {
                Particle rainbowGlowSparkParticle = new RainbowGlowSparkParticle(
                                projectile.Center, // 生成位置
                                projectile.velocity * 0.12f, // 初始速度
                                false, // 是否受重力影响
                                26, // 生命周期，单位是帧
                                0.7f, // 缩放大小
                                Color.Magenta, // 颜色
                                new Vector2(0.05f, 0.5f), // 拉伸或压缩比例
                                false, // 开关参数
                                true, // 开关参数
                                0.8f, // 旋转角度或方向
                                0.03f // 开关参数
                            );
                            GeneralParticleHandler.SpawnParticle(rainbowGlowSparkParticle);
                break;
            }
            case 5:
            {
                Particle velChangingSpark = new VelChangingSpark(
                                projectile.Center, // 生成位置
                                forward * 6f, // 初始速度
                                forward * 0.5f, // 目标减速速度
                                "CalamityMod/Particles/BloomLineSoftEdge", // 贴图路径
                                28, // 生命周期
                                0.7f, // 缩放
                                Color.Lime, // 颜色
                                new Vector2(0.05f, 0.5f), // 拉伸比例
                                true,
                                true,
                                0f,
                                false,
                                0.55f,
                                0.08f
                            );
                            GeneralParticleHandler.SpawnParticle(velChangingSpark);
                break;
            }
            case 6:
            {
                Particle voidSparkParticle = new VoidSparkParticle(
                                projectile.Center, // 生成位置
                                projectile.velocity * 0.08f, // 初始速度
                                false, // 是否受重力影响
                                24, // 生命周期，单位是帧
                                0.9f, // 缩放大小
                                Color.Purple, // 颜色
                                0.975f // 数字越大越拉的长
                            );
                            GeneralParticleHandler.SpawnParticle(voidSparkParticle);
                break;
            }
            case 7:
            {
                Particle lineParticle = new LineParticle(
                                projectile.Center, // 生成位置
                                projectile.velocity * 0.1f, // 初始速度
                                false, // 是否受重力影响
                                20, // 生命周期，单位是帧
                                1.0f, // 缩放大小
                                Color.White // 颜色
                            );
                            GeneralParticleHandler.SpawnParticle(lineParticle);
                break;
            }
            case 8:
            {
                Particle altLineParticle = new AltLineParticle(
                                projectile.Center, // 生成位置
                                projectile.velocity * 0.08f, // 初始速度
                                false, // 是否受重力影响
                                20, // 生命周期，单位是帧
                                1.0f, // 缩放大小
                                Color.LightBlue // 颜色
                            );
                            GeneralParticleHandler.SpawnParticle(altLineParticle);
                break;
            }
            case 9:
            {
                Particle bloomLineVFX = new BloomLineVFX(
                                projectile.Center, // 生成位置
                                forward * 240f, // 初始速度
                                1.4f, // 是否受重力影响
                                Color.Lime, // 生命周期，单位是帧
                                40, // 缩放大小
                                false, // 颜色
                                false // 拉伸或压缩比例
                            );
                            GeneralParticleHandler.SpawnParticle(bloomLineVFX);
                break;
            }
            case 10:
            {
                Particle staticGlowLine = new StaticGlowLine(
                                projectile.Center, // 生成位置
                                projectile.Center + forward * 180f, // 初始速度
                                Vector2.Zero, // 是否受重力影响
                                30, // 生命周期，单位是帧
                                1.2f, // 缩放大小
                                0.03f, // 颜色
                                Color.Cyan, // 拉伸或压缩比例
                                true // 开关参数
                            );
                            GeneralParticleHandler.SpawnParticle(staticGlowLine);
                break;
            }
            case 11:
            {
                Particle thunderBoltVFX = new ThunderBoltVFX(
                                projectile.Center, // 生成位置
                                forward.ToRotation() + MathHelper.PiOver2, // 初始速度
                                1.0f, // 是否受重力影响
                                Color.Cyan, // 生命周期，单位是帧
                                18, // 缩放大小
                                5f, // 颜色
                                0.9f, // 拉伸或压缩比例
                                new Vector2(1f, 1.2f) // 开关参数
                            );
                            GeneralParticleHandler.SpawnParticle(thunderBoltVFX);
                break;
            }
            case 12:
            {
                Particle electricSpark = new ElectricSpark(
                                projectile.Center, // 生成位置
                                projectile.velocity * 0.1f, // 初始速度
                                Color.White, // 是否受重力影响
                                Color.Cyan, // 生命周期，单位是帧
                                0.9f, // 缩放大小
                                24, // 颜色
                                MathHelper.PiOver4, // 拉伸或压缩比例
                                6f, // 开关参数
                                1f, // 开关参数
                                1.2f // 旋转角度或方向
                            );
                            GeneralParticleHandler.SpawnParticle(electricSpark);
                break;
            }
            case 13:
            {
                Particle pointParticle = new PointParticle(
                                projectile.Center, // 生成位置
                                -projectile.velocity * 0.2f, // 初始速度
                                false, // 是否受重力影响
                                15, // 生命周期，单位是帧
                                1.1f, // 缩放大小
                                Color.Orange // 颜色
                            );
                            GeneralParticleHandler.SpawnParticle(pointParticle);
                break;
            }
            case 14:
            {
                Particle genericSparkle = new GenericSparkle(
                                projectile.Center, // 生成位置
                                Vector2.Zero, // 初始速度
                                Color.Gold, // 是否受重力影响
                                Color.Cyan, // 生命周期，单位是帧
                                1.8f, // 缩放大小
                                16, // 颜色
                                0.02f, // 拉伸或压缩比例
                                1.4f, // 开关参数
                                false // 开关参数
                            );
                            GeneralParticleHandler.SpawnParticle(genericSparkle);
                break;
            }
            case 15:
            {
                Particle critSpark = new CritSpark(
                                projectile.Center, // 生成位置
                                forward.RotatedByRandom(0.4f) * 4f, // 初始速度
                                Color.White, // 是否受重力影响
                                Color.LightBlue, // 生命周期，单位是帧
                                1f, // 缩放大小
                                16, // 颜色
                                1f, // 拉伸或压缩比例
                                1.2f, // 开关参数
                                0f // 开关参数
                            );
                            GeneralParticleHandler.SpawnParticle(critSpark);
                break;
            }
            case 16:
            {
                Particle sparkleParticle = new SparkleParticle(
                                projectile.Center, // 生成位置
                                Main.rand.NextVector2Circular(2f, 2f), // 初始速度
                                Color.White, // 是否受重力影响
                                Color.HotPink, // 生命周期，单位是帧
                                1.1f, // 缩放大小
                                18, // 颜色
                                0.05f, // 拉伸或压缩比例
                                1.3f, // 开关参数
                                true, // 开关参数
                                false // 旋转角度或方向
                            );
                            GeneralParticleHandler.SpawnParticle(sparkleParticle);
                break;
            }
            case 17:
            {
                Particle roundedStarParticle = new RoundedStarParticle(
                                projectile.Center, // 生成位置
                                Main.rand.NextVector2Circular(2f, 2f), // 初始速度
                                Color.LightGoldenrodYellow, // 是否受重力影响
                                0.8f, // 生命周期，单位是帧
                                24, // 缩放大小
                                0.04f, // 颜色
                                0.96f, // 拉伸或压缩比例
                                false, // 开关参数
                                projectile.Center, // 开关参数
                                projectile.owner // 旋转角度或方向
                            );
                            GeneralParticleHandler.SpawnParticle(roundedStarParticle);
                break;
            }
            case 18:
            {
                Particle snowflakeSparkle = new SnowflakeSparkle(
                                projectile.Center, // 生成位置
                                Main.rand.NextVector2Circular(1.5f, 1.5f), // 初始速度
                                Color.White, // 是否受重力影响
                                Color.LightCyan, // 生命周期，单位是帧
                                1.1f, // 缩放大小
                                24, // 颜色
                                0.04f, // 拉伸或压缩比例
                                1.2f, // 开关参数
                                6 // 开关参数
                            );
                            GeneralParticleHandler.SpawnParticle(snowflakeSparkle);
                break;
            }
            case 19:
            {
                Particle fancyStars = new FancyStars(
                                projectile.Center, // 生成位置
                                Main.rand.NextFloat(MathHelper.TwoPi), // 初始速度
                                0.8f, // 是否受重力影响
                                Main.rand.NextVector2Circular(2f, 2f), // 生命周期，单位是帧
                                0.03f, // 缩放大小
                                30, // 颜色
                                Color.Yellow // 拉伸或压缩比例
                            );
                            GeneralParticleHandler.SpawnParticle(fancyStars);
                break;
            }
            case 20:
            {
                Particle flareShine = new FlareShine(
                                projectile.Center, // 生成位置
                                Vector2.Zero, // 初始速度
                                Color.White, // 是否受重力影响
                                Color.Orange, // 生命周期，单位是帧
                                forward.ToRotation() + MathHelper.PiOver2, // 缩放大小
                                new Vector2(0.4f, 1.8f), // 颜色
                                new Vector2(0.5f, 6.8f), // 拉伸或压缩比例
                                18, // 开关参数
                                0.01f, // 开关参数
                                1.4f, // 旋转角度或方向
                                0f, // 开关参数
                                0 // 开关参数
                            );
                            GeneralParticleHandler.SpawnParticle(flareShine);
                break;
            }
            case 21:
            {
                Particle cuteManaStarParticle = new CuteManaStarParticle(
                                projectile.Center, // 生成位置
                                Main.rand.NextVector2Circular(1f, 1f), // 初始速度
                                1f, // 是否受重力影响
                                0.9f, // 生命周期，单位是帧
                                24 // 缩放大小
                            );
                            GeneralParticleHandler.SpawnParticle(cuteManaStarParticle);
                break;
            }
            case 22:
            {
                Particle pearlParticle = new PearlParticle(
                                projectile.Center, // 生成位置
                                Main.rand.NextVector2Circular(1.5f, 1.5f), // 初始速度
                                false, // 是否受重力影响
                                28, // 生命周期，单位是帧
                                0.9f, // 缩放大小
                                Color.Pink, // 颜色
                                0.95f, // 拉伸或压缩比例
                                0.03f, // 开关参数
                                false // 开关参数
                            );
                            GeneralParticleHandler.SpawnParticle(pearlParticle);
                break;
            }
            case 23:
            {
                Particle strongBloom = new StrongBloom(
                                projectile.Center, // 生成位置
                                Vector2.Zero, // 初始速度
                                Color.LimeGreen, // 是否受重力影响
                                1.8f, // 生命周期，单位是帧
                                40 // 缩放大小
                            );
                            GeneralParticleHandler.SpawnParticle(strongBloom);
                break;
            }
            case 24:
            {
                Particle genericBloom = new GenericBloom(
                                projectile.Center, // 生成位置
                                Vector2.Zero, // 初始速度
                                Color.GreenYellow, // 是否受重力影响
                                1.3f, // 生命周期，单位是帧
                                36, // 缩放大小
                                true, // 颜色
                                true // 拉伸或压缩比例
                            );
                            GeneralParticleHandler.SpawnParticle(genericBloom);
                break;
            }
            case 25:
            {
                Particle bloomParticle = new BloomParticle(
                                projectile.Center, // 生成位置
                                Vector2.Zero, // 初始速度
                                Color.LightGreen, // 是否受重力影响
                                0.4f, // 生命周期，单位是帧
                                2.0f, // 缩放大小
                                45, // 颜色
                                true // 拉伸或压缩比例
                            );
                            GeneralParticleHandler.SpawnParticle(bloomParticle);
                break;
            }
            case 26:
            {
                Particle bloomRing = new BloomRing(
                                projectile.Center, // 生成位置
                                Vector2.Zero, // 初始速度
                                Color.ForestGreen, // 是否受重力影响
                                1.6f, // 生命周期，单位是帧
                                38 // 缩放大小
                            );
                            GeneralParticleHandler.SpawnParticle(bloomRing);
                break;
            }
            case 27:
            {
                Particle glowOrbParticle = new GlowOrbParticle(
                                projectile.Center, // 生成位置
                                Vector2.Zero, // 初始速度
                                false, // 是否受重力影响
                                20, // 生命周期，单位是帧
                                0.9f, // 缩放大小
                                Color.Red, // 颜色
                                true, // 拉伸或压缩比例
                                false, // 开关参数
                                true // 开关参数
                            );
                            GeneralParticleHandler.SpawnParticle(glowOrbParticle);
                break;
            }
            case 28:
            {
                Particle flatGlow = new FlatGlow(
                                projectile.Center, // 生成位置
                                Vector2.Zero, // 初始速度
                                Color.Cyan, // 是否受重力影响
                                forward.ToRotation(), // 生命周期，单位是帧
                                new Vector2(0.2f, 1.4f), // 缩放大小
                                new Vector2(0.02f, 2.4f), // 颜色
                                20 // 拉伸或压缩比例
                            );
                            GeneralParticleHandler.SpawnParticle(flatGlow);
                break;
            }
            case 29:
            {
                Particle directionalPulseRing = new DirectionalPulseRing(
                                projectile.Center, // 生成位置
                                projectile.velocity * 0.1f, // 初始速度
                                Color.Green, // 是否受重力影响
                                new Vector2(1f, 2.5f), // 生命周期，单位是帧
                                forward.ToRotation(), // 缩放大小
                                0.2f, // 颜色
                                0.03f, // 拉伸或压缩比例
                                20 // 开关参数
                            );
                            GeneralParticleHandler.SpawnParticle(directionalPulseRing);
                break;
            }
            case 30:
            {
                Particle customPulse = new CustomPulse(
                                projectile.Center, // 生成位置
                                Vector2.Zero, // 初始速度
                                Color.Aqua, // 是否受重力影响
                                "CalamityMod/Particles/HighResFoggyCircleHardEdge", // 生命周期，单位是帧
                                Vector2.One, // 缩放大小
                                Main.rand.NextFloat(-1f, 1f), // 颜色
                                0.03f, // 拉伸或压缩比例
                                0.16f, // 开关参数
                                16, // 开关参数
                                true, // 旋转角度或方向
                                1f, // 开关参数
                                true, // 开关参数
                                1f, // 数值参数
                                SpriteEffects.None // 数值参数
                            );
                            GeneralParticleHandler.SpawnParticle(customPulse);
                break;
            }
            case 31:
            {
                Particle pulseRing = new PulseRing(
                                projectile.Center, // 生成位置
                                Vector2.Zero, // 初始速度
                                Color.Cyan, // 是否受重力影响
                                0.1f, // 生命周期，单位是帧
                                1.0f, // 缩放大小
                                24 // 颜色
                            );
                            GeneralParticleHandler.SpawnParticle(pulseRing);
                break;
            }
            case 32:
            {
                Particle staticPulseRing = new StaticPulseRing(
                                projectile.Center, // 生成位置
                                Vector2.Zero, // 初始速度
                                Color.Gold, // 是否受重力影响
                                Vector2.One, // 生命周期，单位是帧
                                0f, // 缩放大小
                                0.1f, // 颜色
                                1.2f, // 拉伸或压缩比例
                                26 // 开关参数
                            );
                            GeneralParticleHandler.SpawnParticle(staticPulseRing);
                break;
            }
            case 33:
            {
                if (target is null || !target.active)
                                return;


                            Particle auraPulseRing = new AuraPulseRing(
                                Color.Violet, // 生成位置
                                Vector2.One * 0.2f, // 初始速度
                                Vector2.One * 1.4f, // 是否受重力影响
                                30, // 生命周期，单位是帧
                                target // 缩放大小
                            );
                            GeneralParticleHandler.SpawnParticle(auraPulseRing);
                break;
            }
            case 34:
            {
                Particle playerCenteredPulseRing = new PlayerCenteredPulseRing(
                                owner, // 生成位置
                                Vector2.Zero, // 初始速度
                                Color.LightBlue, // 是否受重力影响
                                Vector2.One, // 生命周期，单位是帧
                                0f, // 缩放大小
                                0.1f, // 颜色
                                1.1f, // 拉伸或压缩比例
                                24 // 开关参数
                            );
                            GeneralParticleHandler.SpawnParticle(playerCenteredPulseRing);
                break;
            }
            case 35:
            {
                Particle constellationRingVFX = new ConstellationRingVFX(
                                projectile.Center, // 生成位置
                                Color.GreenYellow * 0.8f, // 初始速度
                                Main.rand.NextFloat(-MathHelper.Pi, MathHelper.Pi), // 是否受重力影响
                                1.2f, // 生命周期，单位是帧
                                Vector2.One, // 缩放大小
                                0.9f, // 颜色
                                5, // 拉伸或压缩比例
                                1.5f, // 开关参数
                                0.06f, // 开关参数
                                false // 旋转角度或方向
                            );
                            GeneralParticleHandler.SpawnParticle(constellationRingVFX);
                break;
            }
            case 36:
            {
                Particle detailedExplosion = new DetailedExplosion(
                                projectile.Center, // 生成位置
                                Vector2.Zero, // 初始速度
                                Color.OrangeRed * 0.9f, // 是否受重力影响
                                Vector2.One, // 生命周期，单位是帧
                                Main.rand.NextFloat(-0.3f, 0.3f), // 缩放大小
                                0f, // 颜色
                                0.28f, // 拉伸或压缩比例
                                16, // 开关参数
                                true // 开关参数
                            );
                            GeneralParticleHandler.SpawnParticle(detailedExplosion);
                break;
            }
            case 37:
            {
                Particle flameExplosion = new FlameExplosion(
                                projectile.Center, // 生成位置
                                Vector2.Zero, // 初始速度
                                Color.Orange, // 是否受重力影响
                                Vector2.One, // 生命周期，单位是帧
                                Main.rand.NextFloat(-0.4f, 0.4f), // 缩放大小
                                0.1f, // 颜色
                                0.9f, // 拉伸或压缩比例
                                20, // 开关参数
                                0.9f // 开关参数
                            );
                            GeneralParticleHandler.SpawnParticle(flameExplosion);
                break;
            }
            case 38:
            {
                Particle plasmaExplosion = new PlasmaExplosion(
                                projectile.Center, // 生成位置
                                Vector2.Zero, // 初始速度
                                Color.Cyan, // 是否受重力影响
                                Vector2.One, // 生命周期，单位是帧
                                Main.rand.NextFloat(-0.4f, 0.4f), // 缩放大小
                                0.05f, // 初始大小
                                0.18f, // 最终大小
                                18 // 持续时间
                            );
                            GeneralParticleHandler.SpawnParticle(plasmaExplosion);
                break;
            }
            case 39:
            {
                Particle impactParticle = new ImpactParticle(
                                projectile.Center, // 生成位置
                                0.08f, // 初始速度
                                18, // 是否受重力影响
                                1.0f, // 生命周期，单位是帧
                                Color.White // 缩放大小
                            );
                            GeneralParticleHandler.SpawnParticle(impactParticle);
                break;
            }
            case 40:
            {
                Particle bossRoar = new BossRoar(
                                projectile.Center, // 生成位置
                                Color.Red, // 初始速度
                                0f, // 是否受重力影响
                                0.2f, // 生命周期，单位是帧
                                1.8f, // 缩放大小
                                40, // 颜色
                                0.8f // 拉伸或压缩比例
                            );
                            GeneralParticleHandler.SpawnParticle(bossRoar);
                break;
            }
            case 41:
            {
                Particle heavySmokeParticle = new HeavySmokeParticle(
                                projectile.Center, // 生成位置
                                Main.rand.NextVector2Circular(1f, 1f), // 初始速度
                                Color.Gray, // 是否受重力影响
                                35, // 生命周期，单位是帧
                                1.0f, // 缩放大小
                                0.8f, // 颜色
                                0.02f, // 拉伸或压缩比例
                                false, // 开关参数
                                0f, // 开关参数
                                false, // 旋转角度或方向
                                false // 开关参数
                            );
                            GeneralParticleHandler.SpawnParticle(heavySmokeParticle);
                break;
            }
            case 42:
            {
                Particle timedSmokeParticle = new TimedSmokeParticle(
                                projectile.Center, // 生成位置
                                Main.rand.NextVector2Circular(0.8f, 0.8f), // 初始速度
                                Color.WhiteSmoke, // 是否受重力影响
                                Color.DarkGray, // 生命周期，单位是帧
                                0.9f, // 缩放大小
                                0.65f, // 颜色
                                32, // 拉伸或压缩比例
                                0.02f // 开关参数
                            );
                            GeneralParticleHandler.SpawnParticle(timedSmokeParticle);
                break;
            }
            case 43:
            {
                Particle plagueHumidifierMist = new PlagueHumidifierMist(
                                projectile.Center, // 生成位置
                                35, // 初始速度
                                1.0f, // 是否受重力影响
                                Main.rand.NextVector2Circular(1f, 1f) // 生命周期，单位是帧
                            );
                            GeneralParticleHandler.SpawnParticle(plagueHumidifierMist);
                break;
            }
            case 44:
            {
                Particle flameParticle = new FlameParticle(
                                projectile.Center, // 生成位置
                                28, // 初始速度
                                1.0f, // 是否受重力影响
                                0.9f, // 生命周期，单位是帧
                                Color.Yellow, // 缩放大小
                                Color.OrangeRed // 颜色
                            );
                            GeneralParticleHandler.SpawnParticle(flameParticle);
                break;
            }
            case 45:
            {
                Particle waterFlavoredParticle = new WaterFlavoredParticle(
                                projectile.Center, // 生成位置
                                Main.rand.NextVector2Circular(1f, 1f), // 初始速度
                                false, // 是否受重力影响
                                24, // 生命周期，单位是帧
                                1.0f, // 缩放大小
                                Color.LightBlue // 颜色
                            );
                            GeneralParticleHandler.SpawnParticle(waterFlavoredParticle);
                break;
            }
            case 46:
            {
                Particle waterFoamParticle = new WaterFoamParticle(
                                projectile.Center, // 生成位置
                                Main.rand.NextVector2Circular(1f, 1f), // 初始速度
                                30, // 是否受重力影响
                                1.0f, // 生命周期，单位是帧
                                Color.White // 缩放大小
                            );
                            GeneralParticleHandler.SpawnParticle(waterFoamParticle);
                break;
            }
            case 47:
            {
                Particle waterGlobParticle = new WaterGlobParticle(
                                projectile.Center, // 生成位置
                                Main.rand.NextVector2Circular(1f, 1f), // 初始速度
                                1.0f, // 是否受重力影响
                                0.03f, // 生命周期，单位是帧
                                40 // 缩放大小
                            );
                            GeneralParticleHandler.SpawnParticle(waterGlobParticle);
                break;
            }
            case 48:
            {
                Particle seaPrismParticle = new SeaPrismParticle(
                                projectile.Center, // 生成位置
                                projectile.velocity * 0.1f, // 初始速度
                                false, // 是否受重力影响
                                24, // 生命周期，单位是帧
                                0.9f, // 缩放大小
                                Color.Cyan, // 颜色
                                new Vector2(1.5f, 0.5f), // 拉伸或压缩比例
                                true, // 开关参数
                                forward.ToRotation(), // 开关参数
                                0.95f, // 旋转角度或方向
                                false, // 开关参数
                                true, // 开关参数
                                0.6f // 数值参数
                            );
                            GeneralParticleHandler.SpawnParticle(seaPrismParticle);
                break;
            }
            case 49:
            {
                Particle genericBubbleParticle = new GenericBubbleParticle(
                                projectile.Center, // 生成位置
                                new Vector2(0f, -1f), // 初始速度
                                1.0f, // 是否受重力影响
                                0f, // 生命周期，单位是帧
                                50 // 缩放大小
                            );
                            GeneralParticleHandler.SpawnParticle(genericBubbleParticle);
                break;
            }
            case 50:
            {
                Particle bloodParticle = new BloodParticle(
                                projectile.Center, // 生成位置
                                Main.rand.NextVector2Circular(2f, 2f), // 初始速度
                                30, // 是否受重力影响
                                1.0f, // 生命周期，单位是帧
                                Color.Red // 缩放大小
                            );
                            GeneralParticleHandler.SpawnParticle(bloodParticle);
                break;
            }
            case 51:
            {
                Particle bloodParticle2 = new BloodParticle2(
                                projectile.Center, // 生成位置
                                Main.rand.NextVector2Circular(2f, 2f), // 初始速度
                                30, // 是否受重力影响
                                1.0f, // 生命周期，单位是帧
                                Color.DarkRed // 缩放大小
                            );
                            GeneralParticleHandler.SpawnParticle(bloodParticle2);
                break;
            }
            case 52:
            {
                Particle chumBone = new ChumBone(
                                projectile.Center, // 生成位置
                                Main.rand.NextVector2Circular(2f, 2f), // 初始速度
                                Color.White, // 是否受重力影响
                                Main.rand.NextFloat(MathHelper.TwoPi), // 生命周期，单位是帧
                                1.0f, // 缩放大小
                                45, // 颜色
                                false // 拉伸或压缩比例
                            );
                            GeneralParticleHandler.SpawnParticle(chumBone);
                break;
            }
            case 53:
            {
                Particle brokenTendril = new BrokenTendril(
                                projectile.Center, // 生成位置
                                Main.rand.NextVector2Circular(2f, 2f), // 初始速度
                                Main.rand.NextFloat(MathHelper.TwoPi), // 是否受重力影响
                                Vector2.One, // 生命周期，单位是帧
                                45 // 缩放大小
                            );
                            GeneralParticleHandler.SpawnParticle(brokenTendril);
                break;
            }
            case 54:
            {
                BezierCurve curve = new BezierCurve(new Vector2[] { projectile.Center, projectile.Center + new Vector2(20f, -30f), projectile.Center + forward * 80f });
                            Particle brainOfCthulhuAfterImage = new BrainOfCthulhuAfterImage(
                                curve, // 生成位置
                                forward.ToRotation(), // 初始速度
                                Vector2.One, // 是否受重力影响
                                30, // 生命周期，单位是帧
                                new Rectangle(0, 0, 64, 64), // 缩放大小
                                0.1f // 颜色
                            );
                            GeneralParticleHandler.SpawnParticle(brainOfCthulhuAfterImage);
                break;
            }
            case 55:
            {
                Particle jaws = new Jaws(
                                projectile.Center, // 生成位置
                                Vector2.Zero, // 初始速度
                                Color.Red, // 是否受重力影响
                                Vector2.One, // 生命周期，单位是帧
                                forward.ToRotation(), // 缩放大小
                                0.1f, // 颜色
                                1.0f, // 拉伸或压缩比例
                                24 // 开关参数
                            );
                            GeneralParticleHandler.SpawnParticle(jaws);
                break;
            }
            case 56:
            {
                Particle stoneDebrisParticle = new StoneDebrisParticle(
                                projectile.Center, // 生成位置
                                Main.rand.NextVector2Circular(2f, 2f), // 初始速度
                                Color.Gray, // 是否受重力影响
                                1.0f, // 生命周期，单位是帧
                                50, // 缩放大小
                                0.05f // 颜色
                            );
                            GeneralParticleHandler.SpawnParticle(stoneDebrisParticle);
                break;
            }
            case 57:
            {
                Particle titaniumRailgunShell = new TitaniumRailgunShell(
                                projectile.Center, // 生成位置
                                projectile.Center.ToTileCoordinates(), // 初始速度
                                forward.ToRotation(), // 是否受重力影响
                                Color.Cyan, // 生命周期，单位是帧
                                80 // 缩放大小
                            );
                            GeneralParticleHandler.SpawnParticle(titaniumRailgunShell);
                break;
            }
            case 58:
            {
                Particle wulfrumBastionPartsParticle = new WulfrumBastionPartsParticle(
                                owner, // 生成位置
                                0, // 初始速度
                                60 // 是否受重力影响
                            );
                            GeneralParticleHandler.SpawnParticle(wulfrumBastionPartsParticle);
                break;
            }
            case 59:
            {
                Particle aresSummonCrateParticle = new AresSummonCrateParticle(
                                owner, // 生成位置
                                new Vector2(0f, -2f), // 初始速度
                                60 // 是否受重力影响
                            );
                            GeneralParticleHandler.SpawnParticle(aresSummonCrateParticle);
                break;
            }
            case 60:
            {
                Particle urchinSpikeParticle = new UrchinSpikeParticle(
                                projectile.Center, // 生成位置
                                Main.rand.NextVector2Circular(2f, 2f), // 初始速度
                                forward.ToRotation(), // 是否受重力影响
                                1.0f, // 生命周期，单位是帧
                                0.9f, // 缩放大小
                                30 // 颜色
                            );
                            GeneralParticleHandler.SpawnParticle(urchinSpikeParticle);
                break;
            }
            case 61:
            {
                Particle healingPlus = new HealingPlus(
                                projectile.Center, // 生成位置
                                1.0f, // 初始速度
                                new Vector2(0f, -1f), // 是否受重力影响
                                Color.Lime, // 生命周期，单位是帧
                                Color.Transparent, // 缩放大小
                                40 // 颜色
                            );
                            GeneralParticleHandler.SpawnParticle(healingPlus);
                break;
            }
            case 62:
            {
                Particle statChangeArrow = new StatChangeArrow(
                                projectile.Center, // 生成位置
                                new Vector2(0f, -1f), // 初始速度
                                -MathHelper.PiOver2, // 是否受重力影响
                                Color.Lime, // 生命周期，单位是帧
                                Color.Transparent, // 缩放大小
                                1.0f, // 颜色
                                40 // 拉伸或压缩比例
                            );
                            GeneralParticleHandler.SpawnParticle(statChangeArrow);
                break;
            }
            case 63:
            {
                Particle emoteExpressionParticle = new EmoteExpressionParticle(
                                projectile.Center, // 生成位置
                                new Vector2(0f, -0.5f), // 初始速度
                                1.0f, // 是否受重力影响
                                Color.White, // 生命周期，单位是帧
                                40, // 缩放大小
                                EmoteExpressionParticle.EmoteType.Exclamation // 颜色
                            );
                            GeneralParticleHandler.SpawnParticle(emoteExpressionParticle);
                break;
            }
            case 64:
            {
                Particle wulfrumDroidEmote = new WulfrumDroidEmote(
                                projectile.Center, // 生成位置
                                new Vector2(0f, -0.6f), // 初始速度
                                45, // 是否受重力影响
                                1.0f, // 生命周期，单位是帧
                                0 // 缩放大小
                            );
                            GeneralParticleHandler.SpawnParticle(wulfrumDroidEmote);
                break;
            }
            case 65:
            {
                Particle wulfrumDroidSweatEmote = new WulfrumDroidSweatEmote(
                                projectile.Center, // 生成位置
                                new Vector2(0f, -0.6f), // 初始速度
                                45, // 是否受重力影响
                                1.0f // 生命周期，单位是帧
                            );
                            GeneralParticleHandler.SpawnParticle(wulfrumDroidSweatEmote);
                break;
            }
            case 66:
            {
                Particle wulfrumHatParticle = new WulfrumHatParticle(
                                owner, // 生成位置
                                new Vector2(0f, -2f), // 初始速度
                                60 // 是否受重力影响
                            );
                            GeneralParticleHandler.SpawnParticle(wulfrumHatParticle);
                break;
            }
            case 67:
            {
                Particle manaDrainBlob = new ManaDrainBlob(
                                owner, // 生成位置
                                projectile.Center, // 初始速度
                                new Vector2(0f, -1f), // 是否受重力影响
                                1.0f, // 生命周期，单位是帧
                                Color.Blue // 缩放大小
                            );
                            GeneralParticleHandler.SpawnParticle(manaDrainBlob);
                break;
            }
            case 68:
            {
                Particle liliesOfFinalityHeartParticle = new LiliesOfFinalityHeartParticle(
                                projectile.Center, // 生成位置
                                new Vector2(0f, -1f), // 初始速度
                                35, // 是否受重力影响
                                1.0f // 生命周期，单位是帧
                            );
                            GeneralParticleHandler.SpawnParticle(liliesOfFinalityHeartParticle);
                break;
            }
            case 69:
            {
                Particle circularSmearVFX = new CircularSmearVFX(
                                projectile.Center, // 生成位置
                                Color.Orange, // 初始速度
                                forward.ToRotation(), // 是否受重力影响
                                1.0f // 生命周期，单位是帧
                            );
                            GeneralParticleHandler.SpawnParticle(circularSmearVFX);
                break;
            }
            case 70:
            {
                Particle circularSmearSmokeyVFX = new CircularSmearSmokeyVFX(
                                projectile.Center, // 生成位置
                                Color.Gray, // 初始速度
                                forward.ToRotation(), // 是否受重力影响
                                1.0f // 生命周期，单位是帧
                            );
                            GeneralParticleHandler.SpawnParticle(circularSmearSmokeyVFX);
                break;
            }
            case 71:
            {
                Particle semiCircularSmearVFX = new SemiCircularSmearVFX(
                                projectile.Center, // 生成位置
                                Color.Cyan, // 初始速度
                                forward.ToRotation(), // 是否受重力影响
                                1.0f, // 生命周期，单位是帧
                                new Vector2(1f, 0.8f), // 缩放大小
                                false // 颜色
                            );
                            GeneralParticleHandler.SpawnParticle(semiCircularSmearVFX);
                break;
            }
            case 72:
            {
                Particle semiCircularSmearFade = new SemiCircularSmearFade(
                                projectile.Center, // 生成位置
                                projectile.velocity * 0.05f, // 初始速度
                                Color.LightBlue, // 是否受重力影响
                                forward.ToRotation(), // 生命周期，单位是帧
                                1.0f, // 缩放大小
                                new Vector2(1f, 0.8f), // 颜色
                                24, // 拉伸或压缩比例
                                false, // 开关参数
                                false, // 开关参数
                                true, // 旋转角度或方向
                                1 // 开关参数
                            );
                            GeneralParticleHandler.SpawnParticle(semiCircularSmearFade);
                break;
            }
            case 73:
            {
                Particle trientCircularSmear = new TrientCircularSmear(
                                projectile.Center, // 生成位置
                                Color.Yellow, // 初始速度
                                forward.ToRotation(), // 是否受重力影响
                                1.0f // 生命周期，单位是帧
                            );
                            GeneralParticleHandler.SpawnParticle(trientCircularSmear);
                break;
            }
            case 74:
            {
                Particle slashThrough = new SlashThrough(
                                Color.Red, // 生成位置
                                projectile.Center, // 初始速度
                                forward.ToRotation(), // 是否受重力影响
                                24, // 生命周期，单位是帧
                                target // 缩放大小
                            );
                            GeneralParticleHandler.SpawnParticle(slashThrough);
                break;
            }
            case 75:
            {
                Particle mantisPunch = new MantisPunch(
                                projectile.Center, // 生成位置
                                forward.ToRotation() // 初始速度
                            );
                            GeneralParticleHandler.SpawnParticle(mantisPunch);
                break;
            }
            case 76:
            {
                Particle squareParticle = new SquareParticle(
                                projectile.Center, // 生成位置
                                Main.rand.NextVector2Circular(1f, 1f), // 初始速度
                                false, // 是否受重力影响
                                24, // 生命周期，单位是帧
                                1.0f, // 缩放大小
                                Color.Cyan, // 颜色
                                0.1f // 拉伸或压缩比例
                            );
                            GeneralParticleHandler.SpawnParticle(squareParticle);
                break;
            }
            case 77:
            {
                Particle glowSquareParticle = new GlowSquareParticle(
                                projectile.Center, // 生成位置
                                Main.rand.NextVector2Circular(1f, 1f), // 初始速度
                                false, // 是否受重力影响
                                24, // 生命周期，单位是帧
                                1.0f, // 缩放大小
                                Color.DeepSkyBlue, // 颜色
                                true, // 拉伸或压缩比例
                                0.1f // 开关参数
                            );
                            GeneralParticleHandler.SpawnParticle(glowSquareParticle);
                break;
            }
            case 78:
            {
                Particle techyHoloysquareParticle = new TechyHoloysquareParticle(
                                projectile.Center, // 生成位置
                                Main.rand.NextVector2Circular(1f, 1f), // 初始速度
                                1.0f, // 是否受重力影响
                                Color.Cyan, // 生命周期，单位是帧
                                30, // 缩放大小
                                0.9f // 颜色
                            );
                            GeneralParticleHandler.SpawnParticle(techyHoloysquareParticle);
                break;
            }
            case 79:
            {
                Particle nanoParticle = new NanoParticle(
                                projectile.Center, // 生成位置
                                Main.rand.NextVector2Circular(1f, 1f), // 初始速度
                                Color.Cyan, // 是否受重力影响
                                1.0f, // 生命周期，单位是帧
                                30, // 缩放大小
                                false, // 颜色
                                true, // 拉伸或压缩比例
                                true, // 开关参数
                                new Vector2(0f, 0.02f) // 开关参数
                            );
                            GeneralParticleHandler.SpawnParticle(nanoParticle);
                break;
            }
            case 80:
            {
                Particle destroyerReticleTelegraph = new DestroyerReticleTelegraph(
                                target, // 生成位置
                                Color.Red, // 初始速度
                                0.2f, // 是否受重力影响
                                1.2f, // 生命周期，单位是帧
                                40 // 缩放大小
                            );
                            GeneralParticleHandler.SpawnParticle(destroyerReticleTelegraph);
                break;
            }
            case 81:
            {
                Particle destroyerSparkTelegraph = new DestroyerSparkTelegraph(
                                target, // 生成位置
                                Color.Red, // 初始速度
                                Color.Orange, // 是否受重力影响
                                1.0f, // 生命周期，单位是帧
                                40, // 缩放大小
                                0.02f, // 颜色
                                1.2f // 拉伸或压缩比例
                            );
                            GeneralParticleHandler.SpawnParticle(destroyerSparkTelegraph);
                break;
            }
            case 82:
            {
                var fireParticleSet = new FireParticleSet(
                                90, // 生成位置
                                3, // 初始速度
                                Color.Orange, // 是否受重力影响
                                Color.Red, // 生命周期，单位是帧
                                36f, // 缩放大小
                                0.9f // 颜色
                            );
                            fireParticleSet.Update();
                break;
            }
            case 83:
            {
                var chargingEnergyParticleSet = new ChargingEnergyParticleSet(
                                90, // 生成位置
                                3, // 初始速度
                                Color.Cyan, // 是否受重力影响
                                Color.White, // 生命周期，单位是帧
                                0.08f, // 缩放大小
                                24f // 颜色
                            );
                            chargingEnergyParticleSet.Update();
                break;
            }
            case 84:
            {
                var aresCannonChargeParticleSet = new AresCannonChargeParticleSet(
                                90, // 生成位置
                                3, // 初始速度
                                48f, // 是否受重力影响
                                Color.Red // 生命周期，单位是帧
                            );
                            aresCannonChargeParticleSet.Update();
                break;
            }
            case 85:
            {
                var thanatosSmokeParticleSet = new ThanatosSmokeParticleSet(
                                90, // 生成位置
                                4, // 初始速度
                                forward.ToRotation(), // 是否受重力影响
                                36f, // 生命周期，单位是帧
                                0.8f // 缩放大小
                            );
                            thanatosSmokeParticleSet.Update();
                break;
            }
            case 86:
            {
                Particle squishyLightParticle = new SquishyLightParticle(
                                projectile.Center,
                                -Vector2.UnitY.RotatedByRandom(0.39f) * Main.rand.NextFloat(0.4f, 1.6f),
                                0.28f,
                                Color.Orange,
                                25,
                                opacity: 1f,
                                squishStrenght: 1f,
                                maxSquish: 3f,
                                hueShift: 0f);
                            GeneralParticleHandler.SpawnParticle(squishyLightParticle);
                break;
            }
            case 87:
            {
                Particle crackParticle = new CrackParticle(
                                projectile.Center,
                                new Vector2(0f, -4f),
                                Color.DarkOliveGreen,
                                Vector2.One,
                                Main.rand.NextFloat(-MathHelper.Pi, MathHelper.Pi),
                                0.8f,
                                2.0f,
                                35);
                            GeneralParticleHandler.SpawnParticle(crackParticle);
                break;
            }
            case 88:
            {
                Particle desertProwlerSkullParticle = new DesertProwlerSkullParticle(
                                projectile.Center,
                                projectile.velocity * 0.5f,
                                Color.DarkGray * 0.8f,
                                Color.LightGray,
                                Main.rand.NextFloat(0.5f, 1.0f),
                                150);
                            GeneralParticleHandler.SpawnParticle(desertProwlerSkullParticle);
                break;
            }
            case 89:
            {
                Particle mediumMistParticle = new MediumMistParticle(
                                projectile.Center,
                                new Vector2(Main.rand.NextFloat(-1.2f, 1.2f), Main.rand.NextFloat(-4f, -1.5f)),
                                Color.White,
                                Color.Transparent,
                                Main.rand.NextFloat(0.7f, 1.1f),
                                Main.rand.NextFloat(180f, 240f));
                            GeneralParticleHandler.SpawnParticle(mediumMistParticle);
                break;
            }
            case 90:
            {
                Particle smallSmokeParticle = new SmallSmokeParticle(
                                projectile.Center,
                                projectile.velocity * 0.12f + Main.rand.NextVector2Circular(1.1f, 1.1f),
                                Color.DarkGray,
                                Color.Black,
                                0.65f,
                                0.7f,
                                18,
                                false);
                            GeneralParticleHandler.SpawnParticle(smallSmokeParticle);
                break;
            }
            case 91:
            {
                Particle boltParticle = new BoltParticle(
                                projectile.Center,
                                forward * 3f,
                                false,
                                12,
                                0.55f,
                                Color.Yellow,
                                new Vector2(0.45f, 0.85f),
                                true);
                            GeneralParticleHandler.SpawnParticle(boltParticle);
                break;
            }
            case 92:
            {
                RancorLavaMetaball.SpawnParticle(
                                projectile.Center + Main.rand.NextVector2Circular(32f, 32f),
                                Main.rand.NextFloat(60f, 100f));
                break;
            }
            case 93:
            {
                CalamitasMetaball.SpawnParticle(
                                projectile.Center + projectile.velocity,
                                Main.rand.NextVector2Circular(2f, 2f),
                                64f);
                break;
            }
            case 94:
            {
                StreamGougeMetaball.SpawnParticle(
                                projectile.Center,
                                Vector2.Zero,
                                30f);
                break;
            }
            }
        }

        private static NPC FindNearestTarget(Vector2 position)
        {
            NPC bestTarget = null;
            float bestDistance = 1200f;

            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];
                if (!npc.active || npc.friendly || npc.dontTakeDamage)
                    continue;

                float distance = Vector2.Distance(position, npc.Center);
                if (distance >= bestDistance)
                    continue;

                bestDistance = distance;
                bestTarget = npc;
            }

            return bestTarget;
        }
    }

    internal sealed class ParticleLabPanel : ModProjectile, ILocalizedModType
    {
        private const int Columns = 6;
        private const int Rows = 6;
        private const int SlotSize = 92;
        private const int SlotGap = 7;
        private const int PanelPadding = 14;
        private const int HeaderHeight = 34;
        private const int FooterHeight = 34;
        private const int BorderThickness = 2;

        private Vector2 panelTopLeft;
        private bool panelPositionInitialized;
        private bool pageInitialized;
        private int page;

        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";
        public new string LocalizationCategory => "Projectiles.A_Dev";

        private bool FadeOut
        {
            get => Projectile.ai[0] == 1f;
            set => Projectile.ai[0] = value ? 1f : 0f;
        }

        private static int ItemsPerPage => Columns * Rows;
        private static int PageCount => ParticleEffectCatalog.CategoryCount;
        private static int PanelWidth => PanelPadding * 2 + Columns * SlotSize + (Columns - 1) * SlotGap;
        private static int PanelHeight => PanelPadding * 2 + HeaderHeight + Rows * SlotSize + (Rows - 1) * SlotGap + FooterHeight;
        private static Rectangle MouseRectangle => new((int)Main.MouseScreen.X, (int)Main.MouseScreen.Y, 2, 2);

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.DrawScreenCheckFluff[Type] = 9999999;
        }

        public override void SetDefaults()
        {
            Projectile.width = 2;
            Projectile.height = 2;
            Projectile.penetrate = -1;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.Opacity = 0f;
        }

        public override bool ShouldUpdatePosition() => false;

        public override bool? CanDamage() => false;

        public override void AI()
        {
            Player owner = Main.player[Projectile.owner];
            if (!owner.active || owner.dead)
            {
                Projectile.Kill();
                return;
            }

            if (owner.HeldItem.type != ModContent.ItemType<ParticleLabDebugItem>())
                FadeOut = true;

            if (!panelPositionInitialized && Main.myPlayer == Projectile.owner)
            {
                Vector2 requestedTopLeft = Projectile.ai[1] != 0f || Projectile.ai[2] != 0f
                    ? new Vector2(Projectile.ai[1], Projectile.ai[2]) - new Vector2(PanelWidth, PanelHeight) * 0.5f
                    : Main.MouseScreen;
                panelTopLeft = GetClampedPanelTopLeft(requestedTopLeft);
                panelPositionInitialized = true;
            }

            if (!pageInitialized && Main.myPlayer == Projectile.owner)
            {
                page = Math.Clamp(owner.GetModPlayer<ParticleLabPlayer>().LastParticleLabPage, 0, PageCount - 1);
                pageInitialized = true;
            }

            page = Math.Clamp(page, 0, PageCount - 1);
            Vector2 panelCenter = panelTopLeft + new Vector2(PanelWidth, PanelHeight) * 0.5f;
            Projectile.Center = Main.myPlayer == Projectile.owner ? Main.screenPosition + panelCenter : owner.Center;
            Projectile.timeLeft = 2;
            Projectile.Opacity = MathHelper.Clamp(Projectile.Opacity + (FadeOut ? -0.14f : 0.18f), 0f, 1f);

            if (FadeOut && Projectile.Opacity <= 0f)
                Projectile.Kill();
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.myPlayer != Projectile.owner)
                return false;

            Player owner = Main.player[Projectile.owner];
            ParticleLabPlayer labPlayer = owner.GetModPlayer<ParticleLabPlayer>();
            Rectangle panelArea = new((int)panelTopLeft.X, (int)panelTopLeft.Y, PanelWidth, PanelHeight);
            bool mouseOverPanel = panelArea.Intersects(MouseRectangle);
            bool leftClickPressed = Main.mouseLeft && Main.mouseLeftRelease;
            bool rightClickPressed = Main.mouseRight && Main.mouseRightRelease;
            string hoveredEffectName = null;

            IReadOnlyList<int> categoryEffects = ParticleEffectCatalog.GetEffectIndicesForCategory(page);
            DrawPanel(panelArea, Projectile.Opacity);
            DrawHeader(panelArea, ParticleEffectCatalog.GetCategoryName(page), categoryEffects.Count, Projectile.Opacity);

            for (int localIndex = 0; localIndex < categoryEffects.Count; localIndex++)
            {
                int index = categoryEffects[localIndex];
                Rectangle slotArea = GetSlotArea(localIndex);
                bool hovered = slotArea.Intersects(MouseRectangle);
                bool selected = labPlayer.SelectedEffectIndex == index;
                string name = ParticleEffectCatalog.Effects[index].Name;

                if (hovered)
                {
                    mouseOverPanel = true;
                    Main.hoverItemName = name;
                    hoveredEffectName = name;

                    if (leftClickPressed && Projectile.Opacity >= 0.95f)
                    {
                        labPlayer.SelectEffect(index);
                        SoundEngine.PlaySound(SoundID.MenuTick with { Volume = 0.48f, Pitch = 0.16f }, owner.Center);
                    }
                }

                DrawSlot(name, slotArea, selected, hovered, Projectile.Opacity);
            }

            if (hoveredEffectName != null)
                DrawHoveredEffectName(panelArea, hoveredEffectName, Projectile.Opacity);

            Rectangle previousPageArea = GetPreviousPageArea(panelArea);
            Rectangle nextPageArea = GetNextPageArea(panelArea);
            bool canPageLeft = page > 0;
            bool canPageRight = page + 1 < PageCount;
            bool previousHovered = previousPageArea.Intersects(MouseRectangle);
            bool nextHovered = nextPageArea.Intersects(MouseRectangle);
            mouseOverPanel |= previousHovered || nextHovered;

            DrawPager(previousPageArea, "<", canPageLeft, previousHovered, Projectile.Opacity);
            DrawPager(nextPageArea, ">", canPageRight, nextHovered, Projectile.Opacity);
            DrawFitText($"{page + 1} / {PageCount}  {ParticleEffectCatalog.GetCategoryName(page)}", GetPageTextArea(panelArea), Color.White, 0.72f, 0.42f, Projectile.Opacity);

            if (leftClickPressed && Projectile.Opacity >= 0.95f)
            {
                if (canPageLeft && previousHovered)
                {
                    page--;
                    labPlayer.SetLastParticleLabPage(page);
                    SoundEngine.PlaySound(SoundID.MenuTick with { Volume = 0.42f, Pitch = -0.08f }, owner.Center);
                }
                else if (canPageRight && nextHovered)
                {
                    page++;
                    labPlayer.SetLastParticleLabPage(page);
                    SoundEngine.PlaySound(SoundID.MenuTick with { Volume = 0.42f, Pitch = 0.08f }, owner.Center);
                }
            }

            if (!mouseOverPanel && !FadeOut && Projectile.Opacity >= 0.95f && (leftClickPressed || rightClickPressed))
            {
                FadeOut = true;
                SoundEngine.PlaySound(SoundID.MenuClose with { Volume = 0.58f, Pitch = 0.05f }, owner.Center);
            }

            if (mouseOverPanel)
            {
                Main.blockMouse = true;
                owner.mouseInterface = true;
            }

            return false;
        }

        public static void RequestClose(Projectile projectile)
        {
            if (projectile.ModProjectile is ParticleLabPanel panel)
                panel.FadeOut = true;
            else
                projectile.ai[0] = 1f;
        }

        private static Vector2 GetClampedPanelTopLeft(Vector2 desiredTopLeft)
        {
            const float screenMargin = 12f;
            float maxX = Math.Max(screenMargin, Main.screenWidth - PanelWidth - screenMargin);
            float maxY = Math.Max(screenMargin, Main.screenHeight - PanelHeight - screenMargin);

            return new Vector2(
                MathHelper.Clamp(desiredTopLeft.X, screenMargin, maxX),
                MathHelper.Clamp(desiredTopLeft.Y, screenMargin, maxY));
        }

        private Rectangle GetSlotArea(int localIndex)
        {
            int column = localIndex % Columns;
            int row = localIndex / Columns;
            int x = (int)panelTopLeft.X + PanelPadding + column * (SlotSize + SlotGap);
            int y = (int)panelTopLeft.Y + PanelPadding + HeaderHeight + row * (SlotSize + SlotGap);
            return new Rectangle(x, y, SlotSize, SlotSize);
        }

        private static Rectangle GetPreviousPageArea(Rectangle panelArea)
        {
            int y = panelArea.Bottom - PanelPadding - 26;
            return new Rectangle(panelArea.X + PanelPadding, y, 42, 26);
        }

        private static Rectangle GetNextPageArea(Rectangle panelArea)
        {
            int y = panelArea.Bottom - PanelPadding - 26;
            return new Rectangle(panelArea.Right - PanelPadding - 42, y, 42, 26);
        }

        private static Rectangle GetPageTextArea(Rectangle panelArea)
        {
            int y = panelArea.Bottom - PanelPadding - 26;
            return new Rectangle(panelArea.X + PanelPadding + 50, y, panelArea.Width - PanelPadding * 2 - 100, 26);
        }

        private static void DrawPanel(Rectangle panelArea, float opacity)
        {
            DrawRectangle(panelArea, new Color(13, 15, 20, 232) * opacity);
            DrawBorder(panelArea, new Color(94, 110, 132) * opacity, BorderThickness);
        }

        private static void DrawHeader(Rectangle panelArea, string categoryName, int categoryCount, float opacity)
        {
            Rectangle headerArea = new(panelArea.X + PanelPadding, panelArea.Y + PanelPadding, panelArea.Width - PanelPadding * 2, HeaderHeight - 8);
            DrawRectangle(new Rectangle(headerArea.X, headerArea.Bottom + 4, headerArea.Width, 2), new Color(88, 218, 202) * (opacity * 0.78f));
            DrawFitText($"Particle Lab  |  {categoryName}  ({categoryCount})", headerArea, Color.White, 0.8f, 0.44f, opacity);
        }

        private static void DrawHoveredEffectName(Rectangle panelArea, string effectName, float opacity)
        {
            const int displayWidth = 300;
            const int displayHeight = 88;
            const int displayGap = 12;
            const int screenMargin = 12;

            int x = Math.Min(panelArea.Right + displayGap, Main.screenWidth - displayWidth - screenMargin);
            int y = Math.Clamp(panelArea.Center.Y - displayHeight / 2, screenMargin, Main.screenHeight - displayHeight - screenMargin);
            Rectangle displayArea = new(x, y, displayWidth, displayHeight);

            DrawRectangle(displayArea, new Color(16, 22, 30, 240) * opacity);
            DrawBorder(displayArea, new Color(88, 218, 202) * opacity, BorderThickness);
            DrawFitText(effectName, new Rectangle(displayArea.X + 12, displayArea.Y + 12, displayArea.Width - 24, displayArea.Height - 24),
                Color.White, 1.2f, 0.5f, opacity);
        }

        private static void DrawSlot(string name, Rectangle slotArea, bool selected, bool hovered, float opacity)
        {
            Color accent = new(88, 218, 202);
            Color backColor = selected
                ? Color.Lerp(new Color(32, 38, 48), accent, 0.34f)
                : new Color(42, 46, 56);
            Color borderColor = selected
                ? Color.Lerp(accent, Color.White, 0.34f)
                : new Color(112, 120, 136);

            if (hovered)
            {
                backColor = Color.Lerp(backColor, new Color(76, 86, 104), 0.58f);
                borderColor = Color.Lerp(borderColor, Color.White, 0.34f);
            }

            DrawRectangle(slotArea, backColor * (opacity * 0.96f));
            DrawBorder(slotArea, borderColor * opacity, selected ? 3 : 2);
            DrawFitText(name, new Rectangle(slotArea.X + 7, slotArea.Y + 7, slotArea.Width - 14, slotArea.Height - 14), Color.White, 0.62f, 0.24f, opacity);
        }

        private static void DrawPager(Rectangle area, string symbol, bool enabled, bool hovered, float opacity)
        {
            Color backColor = enabled ? new Color(42, 46, 56) : new Color(28, 30, 36);
            Color borderColor = enabled ? new Color(88, 218, 202) : new Color(68, 72, 82);
            Color textColor = enabled ? Color.White : new Color(112, 116, 126);

            if (enabled && hovered)
                backColor = Color.Lerp(backColor, new Color(82, 92, 108), 0.58f);

            DrawRectangle(area, backColor * (opacity * 0.94f));
            DrawBorder(area, borderColor * opacity, 2);
            DrawFitText(symbol, new Rectangle(area.X + 5, area.Y + 3, area.Width - 10, area.Height - 6), textColor, 0.88f, 0.58f, opacity);
        }

        private static void DrawFitText(string text, Rectangle area, Color color, float maxScale, float minScale, float opacity)
        {
            if (string.IsNullOrWhiteSpace(text) || area.Width <= 0 || area.Height <= 0)
                return;

            var font = FontAssets.MouseText.Value;
            Vector2 size = font.MeasureString(text);
            if (size.X <= 0f || size.Y <= 0f)
                return;

            float scale = Math.Min(maxScale, Math.Min(area.Width / size.X, area.Height / size.Y));
            scale = MathHelper.Clamp(scale, minScale, maxScale);
            Vector2 position = new(
                area.X + Math.Max(0f, (area.Width - size.X * scale) * 0.5f),
                area.Y + Math.Max(0f, (area.Height - size.Y * scale) * 0.5f));

            CalamityUtils.DrawBorderStringEightWay(
                Main.spriteBatch,
                font,
                text,
                position,
                color * opacity,
                Color.Black * (0.76f * opacity),
                scale);
        }

        private static void DrawRectangle(Rectangle rectangle, Color color)
        {
            Main.spriteBatch.Draw(TextureAssets.MagicPixel.Value, rectangle, color);
        }

        private static void DrawBorder(Rectangle rectangle, Color color, int thickness)
        {
            DrawRectangle(new Rectangle(rectangle.X, rectangle.Y, rectangle.Width, thickness), color);
            DrawRectangle(new Rectangle(rectangle.X, rectangle.Bottom - thickness, rectangle.Width, thickness), color);
            DrawRectangle(new Rectangle(rectangle.X, rectangle.Y, thickness, rectangle.Height), color);
            DrawRectangle(new Rectangle(rectangle.Right - thickness, rectangle.Y, thickness, rectangle.Height), color);
        }

        public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI)
        {
        }
    }
}
