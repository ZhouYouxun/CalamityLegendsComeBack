using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using CalamityMod;
using CalamityMod.Graphics.Primitives;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;


namespace CalamityLegendsComeBack.Weapons.GaelsGreatsword
{
    internal static class GaelGreatswordRageInterop
    {
        private const BindingFlags InstanceFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        private const BindingFlags StaticFlags = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

        private static Type calamityPlayerType;
        private static Type calamityKeybindsType;
        private static readonly Dictionary<string, FieldInfo> PlayerFields = new();
        private static readonly Dictionary<string, PropertyInfo> PlayerProperties = new();

        public static float GetRageRatio(Player player)
        {
            float rageMax = GetRageMax(player);
            return rageMax <= 0f ? 0f : MathHelper.Clamp(GetRage(player) / rageMax, 0f, 1f);
        }

        public static void AddRage(Player player, float amount)
        {
            if (amount <= 0f)
                return;

            object calamityPlayer = player.Calamity();
            FieldInfo rageField = GetPlayerField(calamityPlayer, "rage");
            if (rageField == null)
                return;

            float rage = ReadFloat(calamityPlayer, rageField);
            float rageMax = Math.Max(1f, GetRageMax(player));
            WriteFloat(calamityPlayer, rageField, MathHelper.Clamp(rage + amount, 0f, rageMax));
        }

        public static bool RageHotKeyJustPressed()
        {
            object keybind = GetRageKeybind();
            if (keybind == null)
                return false;

            PropertyInfo justPressed = keybind.GetType().GetProperty("JustPressed", InstanceFlags);
            if (justPressed?.GetValue(keybind) is bool pressed)
                return pressed;

            PropertyInfo current = keybind.GetType().GetProperty("Current", InstanceFlags);
            return current?.GetValue(keybind) is bool held && held;
        }

        public static string GetRageKeyText()
        {
            object keybind = GetRageKeybind();
            if (keybind == null)
                return "Rage";

            if (TryGetAssignedKeys(keybind) is IEnumerable keys)
            {
                foreach (object key in keys)
                    return key?.ToString() ?? "Rage";
            }

            return "Rage";
        }

        private static IEnumerable TryGetAssignedKeys(object keybind)
        {
            foreach (MethodInfo method in keybind.GetType().GetMethods(InstanceFlags))
            {
                if (method.Name != "GetAssignedKeys" || method.ContainsGenericParameters)
                    continue;

                object[] arguments = BuildDefaultArguments(method);
                if (arguments == null)
                    continue;

                try
                {
                    if (method.Invoke(keybind, arguments) is IEnumerable keys)
                        return keys;
                }
                catch (TargetInvocationException)
                {
                }
                catch (TargetParameterCountException)
                {
                }
                catch (ArgumentException)
                {
                }
            }

            return null;
        }

        private static object[] BuildDefaultArguments(MethodInfo method)
        {
            ParameterInfo[] parameters = method.GetParameters();
            object[] arguments = new object[parameters.Length];

            for (int i = 0; i < parameters.Length; i++)
            {
                ParameterInfo parameter = parameters[i];
                if (parameter.HasDefaultValue && parameter.DefaultValue != DBNull.Value)
                {
                    arguments[i] = parameter.DefaultValue;
                    continue;
                }

                Type parameterType = parameter.ParameterType;
                if (parameterType.IsEnum)
                {
                    arguments[i] = GetDefaultEnumValue(parameterType);
                    if (arguments[i] == null)
                        return null;
                    continue;
                }

                arguments[i] = parameterType.IsValueType ? Activator.CreateInstance(parameterType) : null;
            }

            return arguments;
        }

        private static object GetDefaultEnumValue(Type enumType)
        {
            if (Enum.IsDefined(enumType, "Keyboard"))
                return Enum.Parse(enumType, "Keyboard");

            Array values = Enum.GetValues(enumType);
            return values.Length > 0 ? values.GetValue(0) : null;
        }

        private static float GetRage(Player player)
        {
            object calamityPlayer = player.Calamity();
            FieldInfo rageField = GetPlayerField(calamityPlayer, "rage");
            return rageField == null ? 0f : ReadFloat(calamityPlayer, rageField);
        }

        private static float GetRageMax(Player player)
        {
            object calamityPlayer = player.Calamity();
            FieldInfo rageMaxField = GetPlayerField(calamityPlayer, "rageMax");
            if (rageMaxField != null)
                return ReadFloat(calamityPlayer, rageMaxField);

            PropertyInfo rageMaxProperty = GetPlayerProperty(calamityPlayer, "RageMax");
            if (rageMaxProperty?.GetValue(calamityPlayer) is float rageMax)
                return rageMax;

            return 100f;
        }

        private static FieldInfo GetPlayerField(object calamityPlayer, string name)
        {
            if (calamityPlayer == null)
                return null;

            calamityPlayerType ??= calamityPlayer.GetType();
            if (PlayerFields.TryGetValue(name, out FieldInfo cached))
                return cached;

            FieldInfo field = calamityPlayerType.GetField(name, InstanceFlags);
            PlayerFields[name] = field;
            return field;
        }

        private static PropertyInfo GetPlayerProperty(object calamityPlayer, string name)
        {
            if (calamityPlayer == null)
                return null;

            calamityPlayerType ??= calamityPlayer.GetType();
            if (PlayerProperties.TryGetValue(name, out PropertyInfo cached))
                return cached;

            PropertyInfo property = calamityPlayerType.GetProperty(name, InstanceFlags);
            PlayerProperties[name] = property;
            return property;
        }

        private static float ReadFloat(object target, FieldInfo field)
        {
            object value = field.GetValue(target);
            return value switch
            {
                float f => f,
                double d => (float)d,
                int i => i,
                _ => 0f,
            };
        }

        private static void WriteFloat(object target, FieldInfo field, float value)
        {
            if (field.FieldType == typeof(float))
                field.SetValue(target, value);
            else if (field.FieldType == typeof(double))
                field.SetValue(target, (double)value);
            else if (field.FieldType == typeof(int))
                field.SetValue(target, (int)value);
        }

        private static object GetRageKeybind()
        {
            calamityKeybindsType ??= FindType("CalamityMod.CalamityKeybinds");
            if (calamityKeybindsType == null)
                return null;

            PropertyInfo property = calamityKeybindsType.GetProperty("RageHotKey", StaticFlags);
            if (property != null)
                return property.GetValue(null);

            FieldInfo field = calamityKeybindsType.GetField("RageHotKey", StaticFlags);
            return field?.GetValue(null);
        }

        private static Type FindType(string fullName)
        {
            Type direct = Type.GetType(fullName + ", CalamityMod");
            if (direct != null)
                return direct;

            string shortName = fullName[(fullName.LastIndexOf('.') + 1)..];
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type type = assembly.GetType(fullName);
                if (type != null)
                    return type;

                try
                {
                    foreach (Type candidate in assembly.GetTypes())
                    {
                        if (candidate.Name == shortName)
                            return candidate;
                    }
                }
                catch (ReflectionTypeLoadException ex)
                {
                    foreach (Type candidate in ex.Types)
                    {
                        if (candidate?.Name == shortName)
                            return candidate;
                    }
                }
            }

            return null;
        }
    }
}
