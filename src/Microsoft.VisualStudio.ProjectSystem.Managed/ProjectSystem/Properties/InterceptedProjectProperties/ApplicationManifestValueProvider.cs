﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Threading.Tasks;
using System;
using System.IO;

// CPS calls the IProjectPropertiesProvider under a write lock. If we try to read a property from the 
// project, we will try to acquire a read lock. Taking a read lock from the same thread as the write lock
// is fine but ConfigureAwait(false) will put us in a different thread and cause the lock-taking code to blow up.
#pragma warning disable CA2007 // Do not directly await a Task

namespace Microsoft.VisualStudio.ProjectSystem.Properties
{
    [ExportInterceptingPropertyValueProvider("ApplicationManifest")]
    internal sealed class ApplicationManifestValueProvider : InterceptingPropertyValueProviderBase
    {
        private readonly UnconfiguredProject _unconfiguredProject;

        private const string NoManifestMSBuildProperty = "NoWin32Manifest";
        private const string ApplicationManifestMSBuildProperty = "ApplicationManifest";
        private const string NoManifestValue = "NoManifest";
        private const string DefaultManifestValue = "DefaultManifest";

        [ImportingConstructor]
        public ApplicationManifestValueProvider(UnconfiguredProject unconfiguredProject)
        {
            Requires.NotNull(unconfiguredProject, nameof(unconfiguredProject));

            _unconfiguredProject = unconfiguredProject;
        }

        /// <summary>
        /// Gets the application manifest property
        /// </summary>
        /// <remarks>
        /// The Application Manifest's value is one of three possibilites:
        ///     - It's either a path to file that is the manifest
        ///     - It's the value "NoManifest" which means the application doesn't have a manifest.
        ///     - It's the value "DefaultManifest" which means that the application will have a default manifest.
        ///     
        /// These three values map to two MSBuild properties - ApplicationManifest (specified if it's a path) or NoWin32Manfiest 
        /// which is true for the second case and false or non-existent for the third.
        /// </remarks>
        public override async Task<string> OnGetEvaluatedPropertyValueAsync(string evaluatedPropertyValue, IProjectProperties defaultProperties)
        {
            if (!string.IsNullOrEmpty(evaluatedPropertyValue))
            {
                return evaluatedPropertyValue;
            }

            string noManifestPropertyValue = await defaultProperties.GetEvaluatedPropertyValueAsync(NoManifestMSBuildProperty);
            if (noManifestPropertyValue?.Equals("true", StringComparison.InvariantCultureIgnoreCase) == true)
            {
                return NoManifestValue;
            }

            // It doesnt matter if it is set to false or the value is not present. We default to "DefaultManifest" scenario.
            return DefaultManifestValue;
        }

        /// <summary>
        /// Sets the application manifest property
        /// </summary>
        public override async Task<string> OnSetPropertyValueAsync(string unevaluatedPropertyValue, IProjectProperties defaultProperties, IReadOnlyDictionary<string, string> dimensionalConditions = null)
        {
            string returnValue = null;

            // We treat NULL/empty value as reset to default and remove the two properties from the project.
            if (string.IsNullOrEmpty(unevaluatedPropertyValue) || string.Equals(unevaluatedPropertyValue, DefaultManifestValue, StringComparison.InvariantCultureIgnoreCase))
            {
                await defaultProperties.DeletePropertyAsync(ApplicationManifestMSBuildProperty);
                await defaultProperties.DeletePropertyAsync(NoManifestMSBuildProperty);
            }
            else if (string.Equals(unevaluatedPropertyValue, NoManifestValue, StringComparison.InvariantCultureIgnoreCase))
            {
                await defaultProperties.DeletePropertyAsync(ApplicationManifestMSBuildProperty);
                await defaultProperties.SetPropertyValueAsync(NoManifestMSBuildProperty, "true");
            }
            else
            {
                await defaultProperties.DeletePropertyAsync(NoManifestMSBuildProperty);

                // If we can make the path relative to the project folder do so. Otherwise just use the given path.
                string relativePath;
                if (Path.IsPathRooted(unevaluatedPropertyValue) &&
                    PathHelper.TryMakeRelativeToProjectDirectory(_unconfiguredProject, unevaluatedPropertyValue, out relativePath))
                {
                    returnValue = relativePath;
                }
                else
                {
                    returnValue = unevaluatedPropertyValue;
                }
            }

            return returnValue;
        }
    }
}
