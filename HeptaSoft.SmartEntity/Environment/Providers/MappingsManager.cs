﻿using HeptaSoft.SmartEntity.Mapping;
using HeptaSoft.SmartEntity.Mapping.Accessors;
using HeptaSoft.SmartEntity.Mapping.Engines;
using HeptaSoft.SmartEntity.Mapping.Mappings;
using System;
using System.Collections.Generic;

namespace HeptaSoft.SmartEntity.Environment.Providers
{
    internal class MappingsManager : IMappingsManager
    {
        /// <summary>
        /// The mappings dictionary.
        /// </summary>
        private readonly Dictionary<int, IMapping> mappings;

        /// <summary>
        /// The mapping factory.
        /// </summary>
        private readonly IMappingFactory mappingFactory;

        /// <summary>
        /// The property matcher.
        /// </summary>
        private readonly IPropertyMatcher propertyMatcher;

        /// <summary>
        /// The property accessors provider.
        /// </summary>
        private readonly IPropertyAccessorsProvider propertyAccessorsProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="MappingsManager" /> class.
        /// </summary>
        /// <param name="mappingFactory">The mapping factory.</param>
        /// <param name="propertyMatcher">The property matcher.</param>
        /// <param name="propertyAccessorsProvider">The property accessors provider.</param>
        public MappingsManager(IMappingFactory mappingFactory, IPropertyMatcher propertyMatcher, IPropertyAccessorsProvider propertyAccessorsProvider)
        {
            mappings = new Dictionary<int, IMapping>();
            this.mappingFactory = mappingFactory;
            this.propertyMatcher = propertyMatcher;
            this.propertyAccessorsProvider = propertyAccessorsProvider;
        }

        #region IMappingsManager

        /// <inheritdoc />
        public void AddMapping(IPropertyAccessor sourcePropertyAccessor, IPropertyAccessor targetPropertyAccessor)
        {
            this.AddMapping(sourcePropertyAccessor, sourcePropertyAccessor.DtoType, targetPropertyAccessor);
        }

        /// <inheritdoc />
        public void AddMapping(IValueGetter sourceValueGetter, Type sourceDtoType, IPropertyAccessor targetPropertyAccessor)
        {
            var newMapping = mappingFactory.Create(sourceValueGetter, targetPropertyAccessor);
            var mappingHashCode = this.BuildMappingHashCode(sourceDtoType, targetPropertyAccessor.DtoType, targetPropertyAccessor.PropertyName);

            if (!mappings.ContainsKey(mappingHashCode))
            {
                mappings.Add(mappingHashCode, newMapping);
            }
        }

        /// <inheritdoc />
        public IMapping GetMapping(Type targetDtoType, Type sourceDtoType, PropertyPath targetPropertyPath)
        {
            var mappingHashCode = this.BuildMappingHashCode(targetDtoType, sourceDtoType, targetPropertyPath.AbsolutePath);

            if (!mappings.ContainsKey(mappingHashCode))
            {
                var generatedMapping = this.GenerateMapping(targetDtoType, sourceDtoType, targetPropertyPath);
                mappings.Add(mappingHashCode, generatedMapping);
            }

            return mappings[mappingHashCode];
        }

        #endregion

        /// <summary>
        /// Builds the hash code for the mapping identification.
        /// </summary>
        /// <param name="targetDtoType">Type of the target dto.</param>
        /// <param name="sourceDtoType">Type of the source dto.</param>
        /// <param name="targetPropertyPath">The target property name.</param>
        /// <returns></returns>
        private int BuildMappingHashCode(Type targetDtoType, Type sourceDtoType, string targetPropertyPath)
        {
            string asString = targetDtoType.ToString() + sourceDtoType.ToString() + targetPropertyPath;
            return asString.GetHashCode();
        }

        /// <summary>
        /// Generates the mapping for the specified property name.
        /// All the public properties (but not nested) from <paramref name="sourceDtoType"/> are considered as candidates.
        /// </summary>
        /// <param name="targetDtoType">Type of the target dto.</param>
        /// <param name="sourceDtoType">Type of the source dto.</param>
        /// <param name="targetPropertyPath">The path of the target property.</param>
        /// <returns>The generated mapping.</returns>
        private IMapping GenerateMapping(Type targetDtoType, Type sourceDtoType, PropertyPath targetPropertyPath)
        {
            var allSourcePropertyAccessors = this.propertyAccessorsProvider.GetPropertyAccessors(sourceDtoType);

            foreach (var sourcePropertyAccessor in allSourcePropertyAccessors)
            {
                if (this.propertyMatcher.PropertyNamesMatch(targetPropertyPath.AbsolutePath, sourcePropertyAccessor.PropertyName))
                {
                    var targetPropertyAccessor = this.propertyAccessorsProvider.GetPropertyAccessor(targetDtoType, targetPropertyPath.PropertyName);
                    return this.mappingFactory.Create(sourcePropertyAccessor, targetPropertyAccessor);
                }
            }

            return null;
        }
    }
}
