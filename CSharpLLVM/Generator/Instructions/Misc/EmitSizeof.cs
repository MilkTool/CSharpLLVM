﻿using Swigged.LLVM;
using CSharpLLVM.Compilation;
using Mono.Cecil;
using Mono.Cecil.Cil;
using CSharpLLVM.Helpers;
using CSharpLLVM.Stack;

namespace CSharpLLVM.Generator.Instructions.Misc
{
    [InstructionHandler(Code.Sizeof)]
    class EmitSizeof : ICodeEmitter
    {
        /// <summary>
        /// Emits a sizeof instruction
        /// </summary>
        /// <param name="instruction">The instruction</param>
        /// <param name="context">The context</param>
        /// <param name="builder">The builder</param>
        public void Emit(Instruction instruction, MethodContext context, BuilderRef builder)
        {
            TypeDefinition type = (TypeDefinition)instruction.Operand;
            TypeRef typeRef = TypeHelper.GetTypeRefFromType(type);
            ulong size = LLVM.SizeOfTypeInBits(context.Compiler.TargetData, typeRef) / 8;
            context.CurrentStack.Push(new StackElement(LLVM.ConstInt(TypeHelper.Int32, size, false), typeof(int).GetTypeReference(), TypeHelper.Int32));
        }
    }
}
