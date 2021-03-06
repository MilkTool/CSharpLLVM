﻿using Swigged.LLVM;
using Mono.Cecil.Cil;
using CSharpLLVM.Compilation;
using CSharpLLVM.Stack;

namespace CSharpLLVM.Generator.Instructions.StoreLoad
{
    [InstructionHandler(Code.Ldarg, Code.Ldarg_S, Code.Ldarg_0, Code.Ldarg_1, Code.Ldarg_2, Code.Ldarg_3)]
    class EmitLdarg : ICodeEmitter
    {
        /// <summary>
        /// Emits a ldarg instruction.
        /// </summary>
        /// <param name="instruction">The instruction.</param>
        /// <param name="context">The context.</param>
        /// <param name="builder">The builder.</param>
        public void Emit(Instruction instruction, MethodContext context, BuilderRef builder)
        {
            Code code = instruction.OpCode.Code;

            int index;
            if (code >= Code.Ldarg_0 && code <= Code.Ldarg_3)
            {
                index = instruction.OpCode.Code - Code.Ldarg_0;
            }
            else
            {
                VariableDefinition def = (VariableDefinition)instruction.Operand;
                index = def.Index;
            }
            
            ValueRef arg = context.ArgumentValues[index];
            ValueRef value = LLVM.BuildLoad(builder, arg, "ldargval");
            context.CurrentStack.Push(new StackElement(value, context.ArgumentILTypes[index]));
        }
    }
}
