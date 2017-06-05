﻿using Mono.Cecil;
using Swigged.LLVM;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CSharpLLVM.Lookups
{
    class Lookup
    {
        private Dictionary<string, ValueRef> mFunctionLookup = new Dictionary<string, ValueRef>();
        private Dictionary<FieldReference, ValueRef> mStaticFieldLookup = new Dictionary<FieldReference, ValueRef>();
        private Dictionary<TypeReference, TypeRef> mTypeLookup = new Dictionary<TypeReference, TypeRef>();
        private Dictionary<TypeReference, List<IStructEntry>> mLayoutLookup = new Dictionary<TypeReference, List<IStructEntry>>();
        private Dictionary<TypeReference, VTable> mVTableLookup = new Dictionary<TypeReference, VTable>();
        private Dictionary<TypeDefinition, ValueRef> mNewobjFunctions = new Dictionary<TypeDefinition, ValueRef>();

        private List<MethodDefinition> mCctors = new List<MethodDefinition>();

        public Dictionary<TypeReference, VTable>.ValueCollection VTables { get { return mVTableLookup.Values; } }

        /// <summary>
        /// Gets a function
        /// </summary>
        /// <param name="name">The name</param>
        /// <returns>The function</returns>
        public ValueRef? GetFunction(string name)
        {
            if (mFunctionLookup.ContainsKey(name))
                return mFunctionLookup[name];

            return null;
        }

        /// <summary>
        /// Adds a function
        /// </summary>
        /// <param name="name">The name</param>
        /// <param name="function">The function</param>
        public void AddFunction(string name, ValueRef function)
        {
            mFunctionLookup.Add(name, function);
        }

        /// <summary>
        /// Gets a static field
        /// </summary>
        /// <param name="field">The static field</param>
        /// <returns>The field</returns>
        public ValueRef? GetStaticField(FieldReference field)
        {
            if (mStaticFieldLookup.ContainsKey(field))
                return mStaticFieldLookup[field];

            return null;
        }

        /// <summary>
        /// Adds a static field
        /// </summary>
        /// <param name="field">The field</param>
        /// <param name="val">The value</param>
        public void AddStaticField(FieldReference field, ValueRef val)
        {
            mStaticFieldLookup.Add(field, val);
        }

        /// <summary>
        /// Checks if we already have this static field
        /// </summary>
        /// <param name="field">The field</param>
        /// <returns>If we already have this static field</returns>
        public bool HasStaticField(FieldReference field)
        {
            return mStaticFieldLookup.ContainsKey(field);
        }

        /// <summary>
        /// Adds a .cctor
        /// </summary>
        /// <param name="method">The method</param>
        public void AddCctor(MethodDefinition method)
        {
            mCctors.Add(method);
        }

        /// <summary>
        /// Adds a type
        /// </summary>
        /// <param name="type">The type</param>
        /// <param name="typeRef">The type</param>
        public void AddType(TypeReference type, TypeRef typeRef)
        {
            mTypeLookup.Add(type, typeRef);
        }

        /// <summary>
        /// Gets a type
        /// </summary>
        /// <param name="type">The type</param>
        /// <returns>The type</returns>
        public TypeRef GetTypeRef(TypeReference type)
        {
            if (!mTypeLookup.ContainsKey(type))
                throw new InvalidOperationException("Type " + type + " not found");

            return mTypeLookup[type];
        }

        /// <summary>
        /// Adds a new VTable
        /// </summary>
        /// <param name="table">The table</param>
        public void AddVTable(VTable table)
        {
            mVTableLookup.Add(table.Type, table);
        }

        /// <summary>
        /// Gets a VTable
        /// </summary>
        /// <param name="type">The type for which the VTable is</param>
        /// <returns>The VTable</returns>
        public VTable GetVTable(TypeReference type)
        {
            if (!mVTableLookup.ContainsKey(type))
                throw new InvalidOperationException("VTable for type " + type + " not found");

            return mVTableLookup[type];
        }
        
        /// <summary>
        /// Returns the static constructors (.cctor)
        /// </summary>
        /// <returns>An array containing the static constructors</returns>
        public MethodDefinition[] GetStaticConstructors()
        {
            return mCctors.ToArray();
        }

        /// <summary>
        /// Adds a new "newobj" method for a type
        /// </summary>
        /// <param name="type">The type</param>
        /// <param name="func">The function</param>
        public void AddNewobjMethod(TypeDefinition type, ValueRef func)
        {
            mNewobjFunctions.Add(type, func);
        }

        /// <summary>
        /// Gets a "newobj" method
        /// </summary>
        /// <param name="type">The type</param>
        /// <returns>The function</returns>
        public ValueRef GetNewobjMethod(TypeDefinition type)
        {
            return mNewobjFunctions[type];
        }
        
        /// <summary>
        /// Gets the struct layout of a type
        /// </summary>
        /// <param name="type">The type</param>
        /// <returns>The list of fields</returns>
        public List<IStructEntry> GetStructLayout(TypeDefinition type)
        {
            // Cached?
            if (mLayoutLookup.ContainsKey(type))
                return mLayoutLookup[type];

            List<IStructEntry> fields = new List<IStructEntry>();
            TypeDefinition typeDef = type.Resolve();
            if (typeDef.BaseType == null)
                return fields;

            TypeDefinition parent = typeDef.BaseType.Resolve();

            // First add parent fields, then our own fields
            fields.AddRange(GetStructLayout(parent));
            fields.AddRange(typeDef.Fields.Where(f => f.Name[0] != '<').Select(f => new StructFieldEntry(f)));
            fields.Add(new StructBarrierEntry(type));

            // Add to cache
            mLayoutLookup.Add(type, fields);

            return fields;
        }

        /// <summary>
        /// Gets the index in a type struct of a class vtable
        /// </summary>
        /// <param name="type">The type</param>
        /// <returns>The index</returns>
        public uint GetClassVTableIndex(TypeDefinition type)
        {
            List<IStructEntry> fields = GetStructLayout(type);

            uint i = 0;
            foreach (IStructEntry child in fields)
            {
                // Barrier? Might be the barrier of the type we're looking for
                if (child.IsBarrier)
                {
                    StructBarrierEntry barrier = (StructBarrierEntry)child;
                    if (barrier.Type == type)
                        return i;
                    
                    continue;
                }

                StructFieldEntry fieldEntry = (StructFieldEntry)child;
                if (fieldEntry.Field.IsStatic)
                    continue;

                i++;
            }

            throw new Exception("Could not find VTable index for: " + type);
        }

        /// <summary>
        /// Gets the field index of a field
        /// </summary>
        /// <param name="field">The field</param>
        /// <returns>The field index</returns>
        public uint GetFieldIndex(FieldReference field)
        {
            List<IStructEntry> fields = GetStructLayout(field.DeclaringType.Resolve());

            uint i = 0;
            foreach (IStructEntry child in fields)
            {
                if (child.IsBarrier)
                {
                    i++;
                    continue;
                }

                StructFieldEntry fieldEntry = (StructFieldEntry)child;
                if (fieldEntry.Field.IsStatic)
                    continue;

                // Found
                if (fieldEntry.Field == field)
                    return i;

                i++;
            }

            throw new Exception("Could not find field index for: " + field);
        }
    }
}
