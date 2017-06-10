﻿namespace CSharpLLVM.Compilation
{
    enum OptimizationLevel
    {
        O0,
        O1,
        O2,
        O3
    }

    struct CompilerSettings
    {
        public string InputFile;
        public string ModuleName;
        public OptimizationLevel Optimization;
    }
}
