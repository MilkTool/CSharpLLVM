﻿using CSharpLLVM.Helpers;
using Mono.Cecil;
using Swigged.LLVM;
using System;

namespace CSharpLLVM.Compiler
{
    class MethodCompiler
    {
        private Compiler mCompiler;

        /// <summary>
        /// Creates a new MethodCompiler
        /// </summary>
        /// <param name="compiler">The compiler</param>
        public MethodCompiler(Compiler compiler)
        {
            mCompiler = compiler;
        }

        /// <summary>
        /// Compiles a method
        /// </summary>
        /// <param name="methodDef">The method</param>
        /// <returns>The function</returns>
        public ValueRef? Compile(MethodDefinition methodDef)
        {
            // TODO: fixme
            if (methodDef.Name.Contains(".ctor"))
            {
                Console.WriteLine("SKIPPED .ctor");
                return null;
            }

            // Do we need to create a new function for this, or is there already been a reference to this function?
            // If there is already a reference, use that empty function instead of creating a new one
            string methodName = NameHelper.CreateMethodName(methodDef);
            ValueRef? function = mCompiler.Lookup.GetFunction(methodName);
            if (!function.HasValue)
            {
                int paramCount = methodDef.Parameters.Count;
                TypeRef[] argTypes = new TypeRef[paramCount];
                Console.Write(methodDef.Name+": ");
                for (int i = 0; i < paramCount; i++)
                {
                    Console.Write(methodDef.Parameters[i].ParameterType+", ");
                    argTypes[i] = TypeHelper.GetTypeRefFromType(methodDef.Parameters[i].ParameterType);
                }
                Console.WriteLine("");
                TypeRef functionType = LLVM.FunctionType(TypeHelper.GetTypeRefFromType(methodDef.ReturnType), argTypes, false);
                function = LLVM.AddFunction(mCompiler.Module, methodName, functionType);
                mCompiler.Lookup.AddFunction(methodName, function.Value);
            }

            // Private only visible for us
            if (methodDef.IsPrivate)
                LLVM.SetLinkage(function.Value, Linkage.InternalLinkage);

            // Only generate if it has a body
            if (methodDef.Body == null)
            {
                LLVM.SetLinkage(function.Value, Linkage.ExternalLinkage);
                return null;
            }

            // Compile instructions
            MethodContext ctx = new MethodContext(mCompiler, methodDef, function.Value);
            InstructionEmitter emitter = new InstructionEmitter(ctx);
            emitter.EmitInstructions(mCompiler.CodeGen);

            // Verify & optimize
            mCompiler.VerifyAndOptimizeFunction(function.Value);

            return function;
        }
    }
}
