// <copyright file="CustomColorComponentsBridge.cs" company="Yenyang's Mods. MIT License">
// Copyright (c) Yenyang's Mods. MIT License. All rights reserved.
// </copyright>

namespace Recolor.Bridge
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using Recolor.Domain;
    using Unity.Entities;

    public static class CustomColorComponentsBridge
    {
        /// <summary>
        /// An HashSet that starts with recolor component types and adds component types from other mods that register.
        /// </summary>
        private static readonly HashSet<ComponentType> m_AllCustomColorComponentTypes = new HashSet<ComponentType>()
        {
            ComponentType.ReadOnly<CustomMeshColor>(),
            ComponentType.ReadOnly<RouteVehicleColor>(),
            ComponentType.ReadOnly<ServiceVehicleColor>(),
            ComponentType.ReadOnly<MeshColorRecord>(),
        };

        private static HashSet<ComponentType> m_IgnoreComponentTypes = new HashSet<ComponentType>();

        public static HashSet<ComponentType> SetRecoloringQueriesExceptions(Assembly modAssembly, HashSet<ComponentType> typesToIgnore, Action<Entity> onTakeoverToRecolor, Action<HashSet<ComponentType>> onOtherModRegistered)
        {
            foreach (ComponentType type in typesToIgnore)
            {
                if (!m_IgnoreComponentTypes.Contains(type))
                {
                    m_IgnoreComponentTypes.Add(type);
                    m_AllCustomColorComponentTypes.Add(type);
                }
            }

            

            return m_AllCustomColorComponentTypes;
        }
    }
}
