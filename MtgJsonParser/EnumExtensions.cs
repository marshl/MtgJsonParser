//-----------------------------------------------------------------------
// <copyright file="EnumExtensions.cs" company="marshl">
// Copyright 2016, Liam Marshall, marshl.
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>
//-----------------------------------------------------------------------
namespace MtgJsonParser
{
    using System;
    using System.Linq;

    /// <summary>
    /// Extensions for accessing attributes of enum values.
    /// </summary>
    public static class EnumExtensions
    {
        /// <summary>
        /// Gets the attribute of the given type for the enum.
        /// </summary>
        /// <typeparam name="TAttribute">The attribute to find.</typeparam>
        /// <param name="value">The enum to parse.</param>
        /// <returns>The attribute, if it exists.</returns>
        public static TAttribute GetAttribute<TAttribute>(this Enum value)
            where TAttribute : Attribute
        {
            Type type = value.GetType();
            string name = Enum.GetName(type, value);
            return type.GetField(name)
                .GetCustomAttributes(false)
                .OfType<TAttribute>()
                .SingleOrDefault();
        }
    }
}
