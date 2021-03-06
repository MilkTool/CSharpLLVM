﻿using Swigged.LLVM;
using Mono.Cecil.Cil;
using CSharpLLVM.Compilation;
using CSharpLLVM.Stack;

namespace CSharpLLVM.Generator.Instructions.BitwiseOps
{
    [InstructionHandler(Code.Not)]
    class EmitNot : ICodeEmitter
    {
        /// <summary>
        /// Emits a not instruction.
        /// </summary>
        /// <param name="instruction">The instruction.</param>
        /// <param name="context">The context.</param>
        /// <param name="builder">The builder.</param>
        public void Emit(Instruction instruction, MethodContext context, BuilderRef builder)
        {
            StackElement value = context.CurrentStack.Pop();
            ValueRef result = LLVM.BuildNot(builder, value.Value, "not");
            context.CurrentStack.Push(new StackElement(result, value.ILType, value.Type));
        }
    }
}
