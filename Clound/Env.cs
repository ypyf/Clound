using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System.Reflection.Emit;

namespace Clound
{
    abstract class Symbol
    {
        public abstract void Load(ILGenerator il);
        public abstract void Store(ILGenerator il);
        public abstract Type SymbolType { get; }
    }

    // Module目前表示导入的名字空间
    class Module : Symbol
    {
        private string name;
        private Assembly assembly;    // 程序集路径

        public Module(string name, Assembly assembly)
        {
            this.name = name;
            this.assembly = assembly;
        }

        public string Name { get { return name; } }

        public Assembly Assembly { get { return assembly; } }

        public override void Load(ILGenerator il)
        {
            throw new NotSupportedException(); 
        }

        public override void Store(ILGenerator il)
        {
            throw new NotSupportedException();
        }

        public override Type SymbolType
        {
            get { throw new NotSupportedException(); }
        }
    }

    class LocalVariable : Symbol
    { 
        private LocalBuilder lb;

        public LocalVariable(LocalBuilder lb)
        {
            this.lb = lb;
        }

        public override void Load(ILGenerator il)
        {
            il.Emit(OpCodes.Ldloc, lb);
        }

        public override void Store(ILGenerator il)
        {
            il.Emit(OpCodes.Stloc, lb);
        }

        public override Type SymbolType
        {
            get { return lb.LocalType; }
        }
    }

    class Parameter : Symbol
    {
        private ParameterBuilder pb;
        private Type type;

        public Parameter(ParameterBuilder pb, Type type)
        {
            this.pb = pb;
            this.type = type;
        }

        public override void Load(ILGenerator il)
        {
            // 注意指令中的形参列表位置从0开始
            //il.Emit(OpCodes.Ldarg_S, pb.Position-1);
            int pos = pb.Position - 1;
            switch (pos)
            { 
                case 0:
                    il.Emit(OpCodes.Ldarg_0);
                    break;
                case 1:
                    il.Emit(OpCodes.Ldarg_1);
                    break;
                case 2:
                    il.Emit(OpCodes.Ldarg_2);
                    break;
                case 3:
                    il.Emit(OpCodes.Ldarg_3);
                    break;
                default:
                    il.Emit(OpCodes.Ldarg, pos);
                    break;
            }
        }

        public override void Store(ILGenerator il)
        {
            il.Emit(OpCodes.Starg, pb.Position-1);
        }

        public override Type SymbolType
        {
            get { return this.type; }
        }
    }

    class Env : Dictionary<string, Symbol>
    {
        private List<Module> modules = new List<Module>();
        protected Env outer;

        public Env(Env outer = null)
        {
            this.outer = outer;
            //this.parameters = parameters ?? new ParameterInfo[0];
        }

        // 添加模块
        public void AddModule(Module module)
        {
            if (!IsImported(module.Name))
                modules.Add(module);
        }

        // 指定的模块名是否已经存在
        public bool IsImported(string name)
        {
            for (Env e = this; e != null; e = e.outer)
            {
                foreach (var m in e.modules)
                {
                    if (name.Equals(m.Name))
                        return true;
                }
            }
            return false;
        }

        // 遍历所有导入的模块
        public Module[] GetModules()
        {
            List<Module> modules = new List<Module>();
            for (Env e = this; e != null; e = e.outer)
            {
                foreach (var m in e.modules)
                {
                    modules.Add(m);
                }
            }
            return modules.ToArray();
        }

        // 在当前所有环境下查找实例变量符号
        public bool HasInstance(string name)
        {
            for (Env e = this; e != null; e = e.outer)
            {
                if (IsExist(e, name))
                    return true;
            }
            return false;
        }

        // 符号是否在本层环境中已经声明
        public bool IsDeclared(string name)
        {
            return IsExist(this, name);
        }

        // 检查环境中是否存在指定的符号
        private bool IsExist(Env e, string name)
        {
            //foreach (var p in e.parameters)
            //{
            //    if (p.Name.Equals(name))
            //        return true;
            //}
            return e.ContainsKey(name);
        }
    }
}
