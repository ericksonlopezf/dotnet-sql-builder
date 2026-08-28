// Copyright © Erickson Lopez. MIT License.
using System;

namespace EricksonLopez.SqlBuilder.Abstractions
{
    /// <summary>
    /// Specifies that a method or class requires a specific database provider capability to function correctly.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public sealed class RequiresCapabilityAttribute : Attribute
    {
        /// <summary>
        /// Gets the required provider capability.
        /// </summary>
        public ProviderCapability Capability { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="RequiresCapabilityAttribute"/> class.
        /// </summary>
        /// <param name="capability">The required capability.</param>
        public RequiresCapabilityAttribute(ProviderCapability capability)
        {
            Capability = capability;
        }
    }
}

