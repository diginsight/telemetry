using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;

namespace Diginsight.Diagnostics
{
    /// <summary>
    /// Central registry for component observability.
    /// Provides early access to logging for static methods across all components.
    /// </summary>
    public static class ObservabilityRegistry
    {
        private static readonly List<Action<ILoggerFactory>> _registeredComponents = new();
        private static ILoggerFactory? loggerFactory;

        /// <summary>
        /// Registers a component's LoggerFactory setter for initialization.
        /// </summary>
        public static void RegisterComponent(Action<ILoggerFactory> setter)
        {
            if (loggerFactory != null)
            {
                // Initialize immediately if logger factory already exists
                setter(loggerFactory);
            }
            _registeredComponents.Add(setter);
        }

        /// <summary>
        /// Sets the logger factory for all registered components.
        /// Use this at application startup or whenever the logger factory changes.
        /// </summary>
        public static void RegisterLoggerFactory(ILoggerFactory loggerFactory)
        {
            ObservabilityRegistry.loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));

            // Initialize all components with this logger factory
            foreach (var setter in _registeredComponents)
            {
                setter(loggerFactory);
            }
        }
    }
}
