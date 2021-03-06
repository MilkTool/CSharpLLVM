﻿using Swigged.LLVM;
using Mono.Cecil.Cil;
using CSharpLLVM.Compilation;
using System;
using Mono.Cecil;
using CSharpLLVM.Stack;
using CSharpLLVM.Helpers;
using System.Linq;

namespace CSharpLLVM.Generator.Instructions.StoreLoad
{
    [InstructionHandler(Code.Stsfld)]
    class EmitStsfld : ICodeEmitter
    {
        /// <summary>
        /// Emits a stsfld instruction.
        /// </summary>
        /// <param name="instruction">The instruction.</param>
        /// <param name="context">The context.</param>
        /// <param name="builder">The builder.</param>
        public void Emit(Instruction instruction, MethodContext context, BuilderRef builder)
        {
            bool isCctor = (context.Method.Name == ".cctor");

            StackElement data = context.CurrentStack.Pop();

            FieldReference field = (FieldReference)instruction.Operand;
            TypeReference fieldType = field.FieldType;
            ValueRef fieldValue = context.Compiler.Lookup.GetStaticField(field);

            // Possible cast needed.
            TypeRef destType = TypeHelper.GetTypeRefFromType(field.FieldType);
            if (data.Type != destType)
                CastHelper.HelpIntAndPtrCast(builder, ref data.Value, ref data.Type, destType, "stsfldcast");
            
            string[] useInitializer =
            {
                "System.Byte",
                "System.SByte",
                "System.Int16",
                "System.Int32",
                "System.Int64",
                "System.UInt16",
                "System.UInt32",
                "System.UInt64",
                "System.Single",
                "System.Double",
                "System.Boolean",
                "System.String"
            };

            // If we're in a cctor and it is a number or string, we can just set the initializer.
            if (isCctor && useInitializer.Contains(fieldType.FullName))
            {
                LLVM.SetInitializer(fieldValue, data.Value);
            }
            else
            {
                ValueRef store = LLVM.BuildStore(builder, data.Value, fieldValue);
                if (instruction.HasPrefix(Code.Volatile))
                    LLVM.SetVolatile(store, true);
            }
        }
    }
}
