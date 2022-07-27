using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace MerinoClient;

/*
 * Partly built from: https://github.com/RequiDev/ReModCE/blob/b86f2526ac92cc0534881c51f23ffa73691efc9e/ReModCE/ReModCE.cs#L327
 * mostly methods InitializeMenus() and InitializeFeatures() with slight changes to my personal preferences
 */

internal static class InstanceCreator
{
    public static void LoadInstance(Type typeClass)
    {
        try
        {
            var assembly = Assembly.GetExecutingAssembly();
            foreach (var type in assembly.GetTypes())
                if (type.IsSubclassOf(typeClass))
                    Activator.CreateInstance(type);
        }
        catch (Exception e)
        {
            MerinoLogger.Error($"An exception occurred while trying to create a {typeClass.Name} with exception of:\n",
                e);
        }
    }

    private static IEnumerable<Type> GetAssemblyTypes()
    {
        var assembly = Assembly.GetExecutingAssembly();
        IEnumerable<Type> types;
        try
        {
            types = assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException reflectionTypeLoadException)
        {
            types = reflectionTypeLoadException.Types.Where(t => t != null);
        }

        return types;
    }

    public static void InitializeMenus(List<MenuComponent> menuComponents)
    {
        var types = GetAssemblyTypes();

        if (types == null) return;

        var loadableMenuComponents = (from t in types
            where !t.IsAbstract
            where t.BaseType == typeof(MenuComponent)
            where
                !t.IsDefined(typeof(MenuDisabled), false)
            select new LoadableComponent { Component = t }).ToList();

        foreach (var menuComp in loadableMenuComponents)
            try
            {
                if (Activator.CreateInstance(menuComp.Component) is not MenuComponent newMenuComponent) return;
                menuComponents.Add(newMenuComponent);
            }
            catch (Exception e)
            {
                MerinoLogger.Error($"Failed creating {menuComp.Component.Name}:\n{e}");
            }
    }

    public static void InitializeFeatures(List<FeatureComponent> featureComponents)
    {
        if (GetAssemblyTypes() == null) return;

        var types = GetAssemblyTypes().ToList();

        var loadableFeatureComponents = (from t in types
            where !t.IsAbstract
            where t.BaseType == typeof(FeatureComponent)
            where
                !t.IsDefined(typeof(FeatureDisabled), false)
            select new LoadableComponent { Component = t }).ToList();

        if (Main.streamerMode)
            loadableFeatureComponents = (from t in types
                where !t.IsAbstract
                where t.Namespace != null && t.Namespace != "MerinoClient.Features.QoL.UI;"
                where t.BaseType == typeof(FeatureComponent)
                where
                    !t.IsDefined(typeof(FeatureDisabled), false)
                select new LoadableComponent { Component = t }).ToList();

        foreach (var featureComp in loadableFeatureComponents)
            try
            {
                if (Activator.CreateInstance(featureComp.Component) is not FeatureComponent newFeatureComponent) return;
                featureComponents.Add(newFeatureComponent);
#if DEBUG
                MerinoLogger.Msg(ConsoleColor.Cyan,
                    $"Loaded \"{newFeatureComponent.FeatureName}\" by {newFeatureComponent.OriginalAuthor}");
#endif
            }
            catch (Exception e)
            {
                MerinoLogger.Error($"Failed creating {featureComp.Component.Name}:\n{e}");
            }
    }

    private class LoadableComponent
    {
        public Type Component;
    }
}