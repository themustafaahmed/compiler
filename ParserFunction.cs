using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace SplitAndMerge
{
    public class ParserFunction
    {
        public ParserFunction()
        {
            _impl = this;
        }
         
        //Parse Function
        // "Виртуален" конструктор
        // internal
        public ParserFunction(string data, ref int from, string item, char ch, ref string action)
        {
            if (item.Length == 0 && (ch == Constants.START_ARG || from >= data.Length))
            {
                // Няма функция, просто израз в скоби
                _impl = _idFunction;
                return;
            }

            _impl = GetArrayFunction(item, ref from, action);
            if (_impl != null)
            {
                return;
            }

            _impl = GetFunctionOrAction(item, ref action);

            if (_impl == _strOrNumFunction && string.IsNullOrWhiteSpace(item))
            {
                string problem = (!string.IsNullOrWhiteSpace(action) ? action : ch.ToString());
                
                string restData = ch.ToString() +
                data.Substring(from, Math.Min(data.Length - from - 1, Constants.MAX_ERROR_CHARS));
                
                throw new ArgumentException("Не може да се анализира [" + problem + "] in " + restData + "...");
            }
        }
        
        //Get Array Function
        public static ParserFunction GetArrayFunction(string name, ref int from, string action)
        {
            if (!string.IsNullOrWhiteSpace(action))
            {
                return null;
            }

            int arrayStart = name.IndexOf(Constants.START_ARRAY);
            if (arrayStart <= 0)
            {
                return null;
            }

            int origLength = name.Length;
            int arrayIndex = Utils.ExtractArrayElement(ref name);
            if (arrayIndex < 0)
            {
                return null;
            }

            ParserFunction pf = ParserFunction.GetFunction(name);
            if (pf == null)
            {
                return null;
            }

            from -= (origLength - arrayStart - 1);
            return pf;
        }
        
        //Get Function Or Action
        public static ParserFunction GetFunctionOrAction(string item, ref string action)
        {
            ActionFunction actionFunction = GetAction(action);

            // Ако преминато действие съществува и е регистрирано, ние сме готови.
            if (actionFunction != null)
            {
                ActionFunction theAction = actionFunction.NewInstance() as ActionFunction;
                theAction.Name = item;
                theAction.Action = action;

                action = null;
                return theAction;
            }

            // В противен случай търсейте местни и глобални функции.
            ParserFunction pf = GetFunction(item);

            if (pf != null)
            {
                return pf;
            }

            // функцията не е намерена, ще се опита да анализира това като низ в кавички или число.
            _strOrNumFunction.Item = item;
            return _strOrNumFunction;
        }

        // Get Function
        public static ParserFunction GetFunction(string item)
        {
            ParserFunction impl;
            // Първо търсене в локалните променливи.

            if (_locals.Count > 0)
            {
                Dictionary<string, ParserFunction> local = _locals.Peek().Variables;
                if (local.TryGetValue(item, out impl))
                {
                    // Локална функция съществува (локална променлива)
                    return impl;
                }
            }

            if (_functions.TryGetValue(item, out impl))
            {
                // Глобалната функция съществува и е регистрирана (например pi, exp или променлива)
                return impl.NewInstance();
            }

            return null;
        }

        //Get Action
        public static ActionFunction GetAction(string action)
        {
            if (string.IsNullOrWhiteSpace(action))
            {
                return null;
            }

            ActionFunction impl;
            
            if (_actions.TryGetValue(action, out impl))
            {
                // Действието съществува и е регистрирано (например =, + =, - и т.н.)
                return impl;
            }

            return null;
        }

        // Function Exists
        public static bool FunctionExists(string item)
        {
            bool exists = false;
            // Първо проверете дали локалната функционална купчинка има тази дефинирана променлива.
            if (_locals.Count > 0)
            {
                Dictionary<string, ParserFunction> local = _locals.Peek().Variables;
                exists = local.ContainsKey(item);
            }

            // Ако не е определено локално, след това проверете глобално:
            return exists || _functions.ContainsKey(item);
        }

        // Add Global Variable
        public static void AddGlobalOrLocalVariable(string name, ParserFunction function)
        {
            function.Name = name;
            if (_locals.Count > 0)
            {
                AddLocalVariable(function);
            }
            else
            {
                AddGlobal(name, function);
            }
        }

        // Add Global
        public static void AddGlobal(string name, ParserFunction function)
        {
            _functions[name] = function;
            function.Name = name;
        }

        // Add Action
        public static void AddAction(string name, ActionFunction action)
        {
            _actions[name] = action;
        }
        
        // Add Local Variables
        public static void AddLocalVariables(StackLevel locals)
        {
            _locals.Push(locals);
        }

        // Add Local Variable
        public static void AddLocalVariable(ParserFunction local)
        {
            StackLevel locals = null;
            if (_locals.Count == 0)
            {
                locals = new StackLevel();
                _locals.Push(locals);
            }
            else
            {
                locals = _locals.Peek();
            }

            locals.Variables[local.Name] = local;
        }

        // Pop Local Variable
        public static void PopLocalVariables()
        {
            _locals.Pop();
        }


        // Invalidate Stacks After Level
        public static void InvalidateStacksAfterLevel(int level)
        {
            while (_locals.Count > level)
            {
                _locals.Pop();
            }
        }

    
        // Get Value
        public Variable GetValue(string data, ref int from)
        {
            return _impl.Evaluate(data, ref from);
        }

        
        protected virtual Variable Evaluate(string data, ref int from)
        {
            // Реалното внедряване ще бъде в извлечените класове.
            return new Variable();
        }

        public virtual ParserFunction NewInstance()
        {
            return this;
        }
    

        protected string _name;

        public string Name
        {
            get { return _name; }
            set { _name = value; }
        }

        private ParserFunction _impl;

        // Global functions:
        private static Dictionary<string, ParserFunction> _functions = new Dictionary<string, ParserFunction>();

        // Global actions - function:
        private static Dictionary<string, ActionFunction> _actions = new Dictionary<string, ActionFunction>();

        public class StackLevel
        {
            public StackLevel(string name = null)
            {
                Name = name;
                Variables = new Dictionary<string, ParserFunction>();
            }

            public string Name { get; set; }
            public Dictionary<string, ParserFunction> Variables { get; set; }
        }

        // Местни променливи:
        // Стейк на изпълняваните функции:
        private static Stack<StackLevel> _locals = new Stack<StackLevel>();

        public static Stack<StackLevel> ExecutionStack
        {
            get { return _locals; }
        }

        private static StringOrNumberFunction _strOrNumFunction =
            new StringOrNumberFunction();

        private static IdentityFunction _idFunction =
            new IdentityFunction();
    }

    //Action Function
    public abstract class ActionFunction : ParserFunction
    {
        protected string _action;

        public string Action
        {
            set { _action = value; }
        }
    }
}