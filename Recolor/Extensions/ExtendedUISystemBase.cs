﻿// <copyright file="ExtendedUISystemBase.cs" company="Yenyang's Mods. MIT License">
// Copyright (c) Yenyang's Mods. MIT License. All rights reserved.
// </copyright>

namespace Recolor.Extensions
{
    using System;
    using Colossal.UI.Binding;
    using Game.UI;

    public abstract partial class ExtendedUISystemBase : UISystemBase
    {
        public ValueBindingHelper<T> CreateBinding<T>(string key, T initialValue)
        {
            var helper = new ValueBindingHelper<T>(new (Mod.Id, key, initialValue, new GenericUIWriter<T>()));

            AddBinding(helper.Binding);

            return helper;
        }

        public ValueBindingHelper<T> CreateBinding<T>(string key, string setterKey, T initialValue, Action<T> updateCallBack = null)
        {
            var helper = new ValueBindingHelper<T>(new (Mod.Id, key, initialValue, new GenericUIWriter<T>()), updateCallBack);
            var trigger = new TriggerBinding<T>(Mod.Id, setterKey, helper.UpdateCallback, initialValue is Enum ? new EnumReader<T>() : null);

            AddBinding(helper.Binding);
            AddBinding(trigger);

            return helper;
        }

        public GetterValueBinding<T> CreateBinding<T>(string key, Func<T> getterFunc)
        {
            var binding = new GetterValueBinding<T>(Mod.Id, key, getterFunc, new GenericUIWriter<T>());

            AddBinding(binding);

            return binding;
        }

        public TriggerBinding CreateTrigger(string key, Action action)
        {
            var binding = new TriggerBinding(Mod.Id, key, action);

            AddBinding(binding);

            return binding;
        }

        public TriggerBinding<T1> CreateTrigger<T1>(string key, Action<T1> action)
        {
            var binding = new TriggerBinding<T1>(Mod.Id, key, action);

            AddBinding(binding);

            return binding;
        }

        public TriggerBinding<T1, T2> CreateTrigger<T1, T2>(string key, Action<T1, T2> action)
        {
            var binding = new TriggerBinding<T1, T2>(Mod.Id, key, action);

            AddBinding(binding);

            return binding;
        }

        public TriggerBinding<T1, T2, T3> CreateTrigger<T1, T2, T3>(string key, Action<T1, T2, T3> action)
        {
            var binding = new TriggerBinding<T1, T2, T3>(Mod.Id, key, action);

            AddBinding(binding);

            return binding;
        }

        public TriggerBinding<T1, T2, T3, T4> CreateTrigger<T1, T2, T3, T4>(string key, Action<T1, T2, T3, T4> action)
        {
            var binding = new TriggerBinding<T1, T2, T3, T4>(Mod.Id, key, action);

            AddBinding(binding);

            return binding;
        }
    }
}
