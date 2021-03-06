﻿using Swigged.LLVM;
using Mono.Cecil;
using Mono.Cecil.Cil;
using CSharpLLVM.Compilation;
using CSharpLLVM.Stack;

namespace CSharpLLVM.Generator.Instructions.StoreLoad
{
    [InstructionHandler(Code.Ldloca, Code.Ldloca_S)]
    class EmitLdloca : ICodeEmitter
    {
        /// <summary>
        /// Emits a ldloca instruction.
        /// </summary>
        /// <param name="instruction">The instruction.</param>
        /// <param name="context">The context.</param>
        /// <param name="builder">The builder.</param>
        public void Emit(Instruction instruction, MethodContext context, BuilderRef builder)
        {
            Code code = instruction.OpCode.Code;
            
            VariableDefinition def = (VariableDefinition)instruction.Operand;
            int index = def.Index;
            
            context.CurrentStack.Push(new StackElement(context.LocalValues[index], new PointerType(context.LocalILTypes[index])));
        }
    }
}
