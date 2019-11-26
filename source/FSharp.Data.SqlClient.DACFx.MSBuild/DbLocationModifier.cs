﻿// Based on https://github.com/Microsoft/DACExtensions/blob/master/Samples/Contributors/DbLocationModifier.cs

//------------------------------------------------------------------------------
//<copyright company="Microsoft">
//
//    The MIT License (MIT)
//    
//    Copyright (c) 2015 Microsoft
//    
//    Permission is hereby granted, free of charge, to any person obtaining a copy
//    of this software and associated documentation files (the "Software"), to deal
//    in the Software without restriction, including without limitation the rights
//    to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//    copies of the Software, and to permit persons to whom the Software is
//    furnished to do so, subject to the following conditions:
//    
//    The above copyright notice and this permission notice shall be included in all
//    copies or substantial portions of the Software.
//    
//    THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//    IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//    FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//    AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//    LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//    OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
//    SOFTWARE.
//</copyright>
//------------------------------------------------------------------------------
using System.IO;
using Microsoft.SqlServer.Dac.Deployment;
using System.Collections.Generic;
using System.Text;

namespace FSharp.Data.SqlClient.DACFx.MSBuild
{
    /// <summary>
    /// Supports overriding the location databases are created at. This contributor does so by modifying the 
    /// "DefaultDataPath", "DefaultLogPath" and "DefaultFilePrefix" values used when creating the database. This
    /// is a fairly lightweight solution but works quite well in practice - these are defined near the start of the plan
    /// and so only a few steps are ever examined. It's also easy to implement and reliable in practice. 
    /// </summary>
    [ExportDeploymentPlanModifier(ContributorId, "1.0.0.0")]
    public class DbLocationModifier : DeploymentPlanModifier
    {
        public const string ContributorId = "FSharp.Data.SqlClient.DACFx.MSBuild.DbLocationModifier";

        /// <summary>
        /// Contributor argument defining the directory to save the MDF and LDF files for the database
        /// </summary>
        public const string DbSaveLocationArg = "DbLocationModifier.SaveLocation";

        /// <summary>
        /// Contributor argument defining the prefix to use for the database files
        /// </summary>
        public const string DbFilePrefixArg = "DbLocationModifier.FilePrefix";

        /// <summary>
        /// Iterates over the deployment plan to find the definition for 
        /// </summary>
        /// <param name="context"></param>
        protected override void OnExecute(DeploymentPlanContributorContext context)
        {
            // Run only if a location is defined and we're targeting a serverless (LocalDB) instance
            if (context.Arguments.TryGetValue(DbSaveLocationArg, out string location)
                && context.Arguments.TryGetValue(DbFilePrefixArg, out string filePrefix))
            {
                location = new DirectoryInfo(location).FullName + "\\";
                ChangeNewDatabaseLocation(context, location, filePrefix);
            }
        }

        private void ChangeNewDatabaseLocation(DeploymentPlanContributorContext context, string location, string filePrefix)
        {
            DeploymentStep nextStep = context.PlanHandle.Head;

            // Loop through all steps in the deployment plan
            bool foundSetVars = false;
            while (nextStep != null && !foundSetVars)
            {
                // Increment the step pointer, saving both the current and next steps
                DeploymentStep currentStep = nextStep;

                // Only interrogate up to BeginPreDeploymentScriptStep - setvars must be done before that
                // We know this based on debugging a new deployment and examining the output script
                if (currentStep is BeginPreDeploymentScriptStep)
                {
                    break;
                }

                DeploymentScriptStep scriptStep = currentStep as DeploymentScriptStep;
                if (scriptStep != null)
                {
                    IList<string> scripts = scriptStep.GenerateTSQL();
                    foreach (string script in scripts)
                    {
                        if (script.Contains("DefaultDataPath"))
                        {
                            // This is the step that sets the default data path and log path.
                            foundSetVars = true;

                            // Override setvars before the deployment begins
                            StringBuilder sb = new StringBuilder();
                            sb.AppendFormat(":setvar DefaultDataPath \"{0}\"", location)
                                .AppendLine()
                                .AppendFormat(":setvar DefaultLogPath \"{0}\"", location)
                                .AppendLine()
                                .AppendFormat(":setvar DefaultFilePrefix \"{0}\"", filePrefix)
                                .AppendLine();

                            // Create a new step for the setvar statements, and add it after the existing step.
                            // That ensures that the updated values are used instead of the defaults
                            DeploymentScriptStep setVarsStep = new DeploymentScriptStep(sb.ToString());
                            this.AddAfter(context.PlanHandle, scriptStep, setVarsStep);
                        }
                    }
                }

                nextStep = currentStep.Next;
            }
        }
    }
}