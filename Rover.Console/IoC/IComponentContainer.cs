﻿using System;

namespace REstate.IoC
{
    /// <summary>
    /// A simple container interface for plugging in DI containers.
    /// </summary>
    public interface IComponentContainer
    {
        /// <summary>
        /// Resolves the specified type.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="name">The name.</param>
        /// <returns>T.</returns>
        T Resolve<T>(string name = null)
            where T : class;

        /// <summary>
        /// Registers the specified instance.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="instance">The instance.</param>
        /// <param name="name">The name.</param>
        void Register<T>(T instance, string name = null)
            where T : class;

        /// <summary>
        /// Registers the specified factory method.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="resolver">The resolver.</param>
        /// <param name="name">The name.</param>
        void Register<T>(Func<IComponentContainer, T> resolver, string name = null)
            where T : class;
    }
}