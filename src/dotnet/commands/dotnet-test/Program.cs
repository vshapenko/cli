﻿// Copyright(c) .NET Foundation and contributors.All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.DotNet.Cli;
using Microsoft.DotNet.Cli.CommandLine;
using Microsoft.DotNet.Cli.Utils;
using Microsoft.DotNet.Tools.MSBuild;
using Parser = Microsoft.DotNet.Cli.Parser;

namespace Microsoft.DotNet.Tools.Test
{
    public class TestCommand : RestoringCommand
    {
        public TestCommand(IEnumerable<string> msbuildArgs, bool noRestore, string msbuildPath = null)
            : base(msbuildArgs, noRestore, msbuildPath)
        {
        }

        public static TestCommand FromArgs(string[] args, string msbuildPath = null)
        {
            var msbuildArgs = new List<string>()
            {
                "/t:VSTest",
                "/v:quiet",
                "/nologo"
            };

            var parser = Parser.Instance;

            var result = parser.ParseFrom("dotnet test", args);

            UpdateRunSettingsArgumentsText();
            result.ShowHelpOrErrorIfAppropriate();

            var parsedTest = result["dotnet"]["test"];

            msbuildArgs.AddRange(parsedTest.OptionValuesToBeForwarded());

            msbuildArgs.AddRange(parsedTest.Arguments);

            var runSettingsOptions =
                result.UnparsedTokens
                    .Select(GetSemiColonEscapedString);

            if (runSettingsOptions.Any())
            {
                var runSettingsArg = string.Join(";", runSettingsOptions);

                msbuildArgs.Add($"/p:VSTestCLIRunSettings=\"{runSettingsArg}\"");
            }

            var verbosityArg = msbuildArgs.LastOrDefault(arg => arg.StartsWith("/verbosity"));

            if (!string.IsNullOrEmpty(verbosityArg))
            {
                var verbosity = verbosityArg.Split(':');
                if (verbosity.Length == 2)
                {
                    msbuildArgs.Add($"/p:VSTestVerbosity={verbosity[1]}");
                }
            }

            bool noRestore = parsedTest.HasOption("--no-restore");

            return new TestCommand(msbuildArgs, noRestore, msbuildPath);
        }

        public static int Run(string[] args)
        {
            DebugHelper.HandleDebugSwitch(ref args);

            TestCommand cmd;

            try
            {
                cmd = FromArgs(args);
            }
            catch (CommandCreationException e)
            {
                return e.ExitCode;
            }

            return cmd.Execute();
        }

        private static string GetSemiColonEscapedString(string arg)
        {
            if (arg.IndexOf(";") != -1)
            {
                return arg.Replace(";", "%3b");
            }

            return arg;
        }

        private static string[] GetSemiColonEscapedArgs(List<string> args)
        {
            int counter = 0;
            string[] array = new string[args.Count];

            foreach (string arg in args)
            {
                array[counter++] = GetSemiColonEscapedString(arg);
            }

            return array;
        }

        private static void UpdateRunSettingsArgumentsText()
        {
            DefaultHelpViewText.Synopsis.AdditionalArguments = " [[--] <RunSettings arguments>...]]";
            DefaultHelpViewText.AdditionalArgumentsSection = LocalizableStrings.RunSettingsArgumentsDescription;
        }
    }
}
