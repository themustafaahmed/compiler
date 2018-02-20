using System;
using System.Collections.Generic;
using System.Text;

namespace SplitAndMerge
{
    public class Variable
    {
        public enum VarType
        {
            NONE,
            NUMBER,
            STRING,
            ARRAY,
            BREAK,
            CONTINUE
        };

        public Variable()

        {
            Reset();
        }

        public Variable(VarType type)
        {
            Type = type;
        }

        public Variable(double d)
        {
            Value = d;
        }

        public Variable(string s)
        {
            String = s;
        }

        public Variable(List<Variable> a)
        {
            Tuple = a;
        }

        public Variable(Variable other)
        {
            Copy(other);
        }

        public void Copy(Variable other)
        {
            Reset();
            Action = other.Action;
            Type = other.Type;

            switch (other.Type)
            {
                case VarType.NUMBER:
                    Value = other.Value;
                    break;
                case VarType.STRING:
                    String = other.String;
                    break;
                case VarType.ARRAY:
                    Tuple = other.Tuple;
                    break;
            }
        }

        public void Reset()
        {
            Value = Double.NaN;
            String = null;
            Tuple = null;
            Action = null;
            Type = VarType.NONE;
        }

//        public static Variable ResetOnBreak(Variable v)
//        {
//            if (v.Type == Variable.VarType.BREAK ||
//                v.Type == Variable.VarType.CONTINUE)
//            {
//                return v;
//            }
//
//            return EmptyInstance;
//        }

        public bool Equals(Variable other)
        {
            if (Type != other.Type)
            {
                return false;
            }

            if (Double.IsNaN(Value) != Double.IsNaN(other.Value) ||
                (!Double.IsNaN(Value) && Value != other.Value))
            {
                return false;
            }

            if (!String.Equals(this.String, other.String, StringComparison.Ordinal))
            {
                return false;
            }

            if (!String.Equals(this.Action, other.Action, StringComparison.Ordinal))
            {
                return false;
            }

            if ((this.Tuple == null) != (other.Tuple == null))
            {
                return false;
            }

            if (this.Tuple != null && !this.Tuple.Equals(other.Tuple))
            {
                return false;
            }

            return true;
        }

        public string AsString(bool isList = true,
            bool sameLine = true)
        {
            if (Type == VarType.NUMBER)
            {
                return Value.ToString();
            }

            if (Type == VarType.STRING)
            {
                return String;
            }

            if (_tuple == null)
            {
                return null;
            }

            StringBuilder sb = new StringBuilder();


            if (isList)
            {
                sb.Append(Constants.START_GROUP.ToString() +
                          (sameLine ? "" : Environment.NewLine));
            }

            for (int i = 0; i < _tuple.Count; i++)
            {
                Variable arg = _tuple[i];
                sb.Append(arg.AsString(isList, sameLine));
                if (i != _tuple.Count - 1)
                {
                    sb.Append(sameLine ? " " : Environment.NewLine);
                }
            }

            if (isList)
            {
                sb.Append(Constants.END_GROUP.ToString() +
                          (sameLine ? " " : Environment.NewLine));
            }

            return sb.ToString();
        }

        public double Value
        {
            get { return _value; }
            set
            {
                _value = value;
                Type = VarType.NUMBER;
            }
        }

        public string String
        {
            get { return _string; }
            set
            {
                _string = value;
                Type = VarType.STRING;
            }
        }

        public List<Variable> Tuple
        {
            get { return _tuple; }
            set
            {
                _tuple = value;
                Type = VarType.ARRAY;
            }
        }

        public string Action { get; set; }
        public VarType Type { get; set; }

        public static Variable EmptyInstance = new Variable();

        private double _value;
        private string _string;
        private List<Variable> _tuple;
    }
}