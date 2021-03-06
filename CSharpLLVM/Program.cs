﻿using CommandLine;
using CSharpLLVM.Compilation;
using System;
using System.IO;

namespace CSharpLLVM
{
    class Program
    {
        /// <summary>
        /// Entrypoint.
        /// </summary>
        /// <param name="args">Arguments.</param>
        static void Main(string[] args)
        {
            Options options = new Options();
            Parser parser = new Parser(setSettings);
            if (parser.ParseArguments(args, options))
            {
                Logger.Init(options.Verbose);
                string moduleName = Path.GetFileNameWithoutExtension(options.InputFile);
                Compiler compiler = new Compiler(options);
                compiler.Compile(moduleName);
            }
            else
            {
                Console.WriteLine(options.GetUsage());
            }
        }

        /// <summary>
        /// Sets the settings.
        /// </summary>
        /// <param name="settings">The settings.</param>
        private static void setSettings(ParserSettings settings)
        {
            settings.MutuallyExclusive = true;
        }
    }
}
