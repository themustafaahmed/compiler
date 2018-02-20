using System;
using System.Collections.Generic;

namespace SplitAndMerge
{
  
  
  //Continue Word
	class ContinueStatement : ParserFunction
	{
		protected override Variable Evaluate(string data, ref int from)
		{

      return new Variable(Variable.VarType.CONTINUE);
		}
	}
  
  //Break Word
	class BreakStatement : ParserFunction
	{
		protected override Variable Evaluate(string data, ref int from)
		{
      return new Variable(Variable.VarType.BREAK);
		}
	}
  
  //Return Word
  class ReturnStatement : ParserFunction
  {
    
    protected override Variable Evaluate(string data, ref int from)
    {
      Utils.MoveForwardIf(data, ref from, Constants.SPACE);

      Variable result = Utils.GetItem(data, ref from);

      // Ако сме в завръщането, ние сме готови:
      from = data.Length;

      return result;
    }
  }
  
   //Function Creator Word
  class FunctionCreator : ParserFunction
  {
    public FunctionCreator(Interpreter interpreter)
    {
      _interpreter = interpreter;
    }

    protected override Variable Evaluate(string data, ref int from)
    {
      string funcName = Utils.GetToken(data, ref from, Constants.TOKEN_SEPARATION);
      _interpreter.AppendOutput("Registering function " + funcName +"()");

      string[] args = Utils.GetFunctionSignature(data, ref from);
      if (args.Length == 1 && string.IsNullOrWhiteSpace(args[0])) {
        args = new string[0];
      }

      Utils.MoveForwardIf(data, ref from, Constants.START_GROUP, Constants.SPACE);

      string body = Utils.GetBodyBetween(data, ref from, Constants.START_GROUP, Constants.END_GROUP);

      CustomFunction customFunc = new CustomFunction(funcName, body, args);
      ParserFunction.AddGlobal(funcName, customFunc);

      return new Variable(funcName);
    }

    private Interpreter _interpreter;
  }
  
  //Custom Function
  class CustomFunction : ParserFunction
  {
    public CustomFunction(string funcName, string body, string[] args)
    {
      _name = funcName;
      _body = body;
      _args = args;
    }

    protected override Variable Evaluate(string data, ref int from)
    {
      bool isList;
      List<Variable> functionArgs = Utils.GetArgs(data, ref from,
        Constants.START_ARG, Constants.END_ARG, out isList);

      Utils.MoveBackIf(data, ref from, Constants.START_GROUP);
      if (functionArgs.Count != _args.Length) {   
        throw new ArgumentException("Функция [" + _name + "] аргументи несъответствие: " +
          _args.Length + " обявен, " + functionArgs.Count + " доставен");
      }

      // 1. Добавете предаваните аргументи като локални променливи към анализатора.
      StackLevel stackLevel = new StackLevel(_name);
      for (int i = 0; i < _args.Length; i++) {
        stackLevel.Variables[_args[i]] = new GetVarFunction(functionArgs[i]);
      }

      ParserFunction.AddLocalVariables(stackLevel);

      // 2. Изпълнете тялото на функцията.
      int temp = 0;
      Variable result = null;

      while (temp < _body.Length - 1)
      { 
        result = Parser.LoadAndCalculate(_body, ref temp, Constants.END_PARSE_ARRAY);
        Utils.GoToNextStatement(_body, ref temp);
      }

      ParserFunction.PopLocalVariables();
      return result;
    }

    private string      _body;
    private string[]    _args;
  }
  
  // String Or Number Function
 	class StringOrNumberFunction : ParserFunction
	{
		protected override Variable Evaluate(string data, ref int from)
		{
		  // Първо проверете дали преминалият израз е низ между кавички.
			if (Item.Length > 1 &&
				Item[0] == Constants.QUOTE &&
				Item[Item.Length - 1]  == Constants.QUOTE) {
			  return new Variable(Item.Substring(1, Item.Length - 2));
			}


		  // В противен случай това трябва да е число.
			double num;
			if (!Double.TryParse(Item, out num))
			{
        throw new ArgumentException("Не може да се анализира означението [" + Item + "]");
			}
			return new Variable(num);
		}
    
		public string Item { private get; set; }
	}
  
    // Identity Function
    // Функция за идентификация
	class IdentityFunction : ParserFunction
  {
		protected override Variable Evaluate(string data, ref int from)
		{
			return Parser.LoadAndCalculate(data, ref from, Constants.END_ARG_ARRAY);
		}
	}
  
  // If Statement
  class IfStatement : ParserFunction
  {
    public IfStatement(Interpreter interpreter)
    {
      if_interpreter = interpreter;
    }

    protected override Variable Evaluate(string data, ref int from)
    {
      Variable result = if_interpreter.ProcessIf(data, ref from);

      return result;
    }

    private Interpreter if_interpreter;
  }
  
  //While Statment
  class WhileStatement : ParserFunction
  {
    public WhileStatement(Interpreter interpreter)
    {
      while_interpreter = interpreter;
    }

    protected override Variable Evaluate(string data, ref int from)
    {
      return while_interpreter.ProcessWhile(data, ref from);
    }

    private Interpreter while_interpreter;
  }

  
    // Include File
  class IncludeFile : ParserFunction
  {
    protected override Variable Evaluate(string data, ref int from)
    {
      string filename = Utils.GetItem(data, ref from).AsString();
      string[] lines = Utils.GetFileLines(filename);

      string includeFile = string.Join(Environment.NewLine, lines);
      string includeScript = Utils.ConvertToScript(includeFile);

      int filePtr = 0;
      while (filePtr < includeScript.Length)
      {
        Parser.LoadAndCalculate(includeScript, ref filePtr,
                                Constants.END_LINE_ARRAY);
        Utils.GoToNextStatement(includeScript, ref filePtr);
      }
      return Variable.EmptyInstance;
    }
  }

  // Get Var Function
  // Получете стойност на променлива или елемент от масив
  class GetVarFunction : ParserFunction
  {
    public GetVarFunction(Variable value)
    {
      _value = value;
    }

    protected override Variable Evaluate(string data, ref int from)
    {
      // Първо проверете дали този елемент е част от масив:
      if (from - 1 < data.Length && data[from - 1] == Constants.START_ARRAY)
      {
        // Даден е индекс - може да е за елемент от сорта.
        if (_value.Tuple == null || _value.Tuple.Count == 0)
        {
          throw new ArgumentException("Няма нищо за индекса");
        }

        Variable index = Parser.LoadAndCalculate(data, ref from,
          Constants.END_ARRAY_ARRAY);

        Utils.CheckInteger(index);

        if (index.Value < 0 || index.Value >= _value.Tuple.Count)
        {
          throw new ArgumentException("Неправилен индекс [" + index.Value +
            "] за малкия размер " + _value.Tuple.Count);
        }

        Utils.MoveForwardIf(data, ref from, Constants.END_ARRAY);
        return _value.Tuple[(int)index.Value];
      }

      return _value;
    }

    private Variable _value;
  }
  
  // Increment Decrement Function
  // увеличение/намаляване функция
  class IncrementDecrementFunction : ActionFunction
  {
    protected override Variable Evaluate(string data, ref int from)
    {
      bool prefix = string.IsNullOrWhiteSpace(_name);
      if (prefix) // Ако е префикс, все още нямаме име на променлива.
      {
        _name = Utils.GetToken(data, ref from, Constants.TOKEN_SEPARATION);

      }

      // Стойност, която трябва да се добави към променливата:
      int valueDelta = _action == Constants.INCREMENT ? 1 : -1;
      int returnDelta = prefix ? valueDelta : 0;

      // Проверете дали променливата, която трябва да бъде настроена, има формата на x (0),
      // означава, че това е масив елемент.
      double newValue = 0;
      int arrayIndex = Utils.ExtractArrayElement(ref _name);
      bool exists = ParserFunction.FunctionExists(_name);
      if (!exists)
      {
        throw new ArgumentException("Variable [" + _name + "] не съществува");
      }

      Variable currentValue = ParserFunction.GetFunction(_name).GetValue(data, ref from);
      if (arrayIndex >= 0) // Променлива с индекс (масив елемент).
      {
        if (currentValue.Tuple == null)
        {
          throw new ArgumentException("Tuple [" + _name + "] не съществува");
        }
        if (currentValue.Tuple.Count <= arrayIndex)
        {
          throw new ArgumentException("Tuple [" + _name + "] има само " +
            currentValue.Tuple.Count + " елементи");
        }
        newValue = currentValue.Tuple[arrayIndex].Value + returnDelta;
        currentValue.Tuple[arrayIndex].Value += valueDelta;
      }
      else // Нормална променлива.
      {
        newValue = currentValue.Value + returnDelta;
        currentValue.Value += valueDelta;
      }

      Variable varValue = new Variable(newValue);
      ParserFunction.AddGlobalOrLocalVariable(_name, new GetVarFunction(currentValue));

      return varValue;
    }

    override public ParserFunction NewInstance()
    {
      return new IncrementDecrementFunction();
    }
  }
  
  // Operator Assign Function
  // Определяне на оператора
  class OperatorAssignFunction : ActionFunction
  {
    protected override Variable Evaluate(string data, ref int from)
    {
      // Стойност, която трябва да се добави към променливата:
      Variable valueB  = Utils.GetItem(data, ref from);

      // Проверете дали променливата, която трябва да бъде настроена, има формата на x (0),
      // означава, че това е масив елемент.
      int arrayIndex = Utils.ExtractArrayElement(ref _name);
      
      bool exists = ParserFunction.FunctionExists(_name);
      
      if (!exists)
      {
        throw new ArgumentException("Променлив [" + _name + "] не съществува");
      }

      Variable currentValue = ParserFunction.GetFunction(_name).GetValue(data, ref from);
      
      Variable valueA = currentValue;
      
      if (arrayIndex >= 0) // Променлива с индекс.
      {
        if (currentValue.Tuple == null)
        {
          throw new ArgumentException("Tuple [" + _name + "] не съществува");
        }
        if (currentValue.Tuple.Count <= arrayIndex)
        {
          throw new ArgumentException("Tuple [" + _name + "] има само " +
            currentValue.Tuple.Count + " елементи");
        }
        valueA = currentValue.Tuple[arrayIndex];
      }

      if (valueA.Type == Variable.VarType.NUMBER)
      {
        NumberOperator (valueA, valueB, _action);
      }
      else
      {
        StringOperator (valueA, valueB, _action);
      }

      Variable varValue = new Variable(valueA);
      ParserFunction.AddGlobalOrLocalVariable(_name, new GetVarFunction(varValue));
      return valueA;
    }

    static void NumberOperator(Variable valueA,
                               Variable valueB, string action)
    {
      switch (action) {
      case "+=":
        valueA.Value += valueB.Value;
        break;
      case "-=":
        valueA.Value -= valueB.Value;
        break;
      case "*=":
        valueA.Value *= valueB.Value;
        break;
      case "/=":
        valueA.Value /= valueB.Value;
        break;
      case "%=":
        valueA.Value %= valueB.Value;
        break;
      case "&=":
        valueA.Value = (int)valueA.Value & (int)valueB.Value;
        break;
      case "|=":
        valueA.Value = (int)valueA.Value | (int)valueB.Value;
        break;
      case "^=":
        valueA.Value = (int)valueA.Value ^ (int)valueB.Value;
        break;
      }
    }
    
    // += String Operator
    private void StringOperator(Variable valueA,
      Variable valueB, string action)
    {
      switch (action)
      {
      case "+=":
        if (valueB.Type == Variable.VarType.STRING) {
          valueA.String += valueB.AsString();
        } else {
          valueA.String += valueB.Value;
        }
        break;
      }
    }

    override public ParserFunction NewInstance()
    {
      return new OperatorAssignFunction();
    }
  }
  
  // Assign Funtion
  //Възлага/=
  class AssignFunction : ActionFunction
  {
    protected override Variable Evaluate(string data, ref int from)
    {
      Variable varValue = Utils.GetItem(data, ref from);

      // Специален случай за добавяне на низ (или число) към низ.

      while (varValue.Type != Variable.VarType.NUMBER &&
             from > 0 && data[from - 1] == '+') {
        Variable addition = Utils.GetItem(data, ref from);
        varValue.String += addition.AsString();
      }

      // Проверете дали променливата, която трябва да бъде настроена, има формата на x (0),
      // означава, че това е масив елемент.
      int arrayIndex = Utils.ExtractArrayElement(ref _name);
      
      if (arrayIndex < 0) {
        ParserFunction.AddGlobalOrLocalVariable(_name, new GetVarFunction(varValue));
        return varValue;
      }

      Variable currentValue;
 
      ParserFunction pf = ParserFunction.GetFunction(_name);
      if (pf != null) {
        currentValue = pf.GetValue(data, ref from);
      } else {
        currentValue = new Variable();
      }

      List<Variable> tuple = currentValue.Tuple == null ?
                            new List<Variable>() :
                            currentValue.Tuple;
      if (tuple.Count > arrayIndex) {
          tuple[arrayIndex] = varValue;
      } else {
          for (int i = tuple.Count; i < arrayIndex; i++) {
            tuple.Add(Variable.EmptyInstance);
          }
          tuple.Add(varValue);
      }
      currentValue.Tuple = tuple;

      ParserFunction.AddGlobalOrLocalVariable(_name, new GetVarFunction(currentValue));
      return currentValue;
    }

    override public ParserFunction NewInstance()
    {
      return new AssignFunction();
    }
  }
  
  // Size Function
  // Функция за размер
  class SizeFunction : ParserFunction
  {
    protected override Variable Evaluate(string data, ref int from)
    {
      // 1. Получете името на променливата.
      string varName = Utils.GetToken(data, ref from, Constants.END_ARG_ARRAY);
      if (from >= data.Length)
      {
        throw new ArgumentException("Променливата не можа да се получи");
      }

      // 2. Получете текущата стойност на променливата.
      ParserFunction func = ParserFunction.GetFunction(varName);
      Variable currentValue = func.GetValue(data, ref from);

      // 3. Вземете дължината на основната тона или
      // низовата част, ако е дефинирана,
      // или цифровата част, превърната в низ по друг начин.
      int size = currentValue.Tuple != null ? currentValue.Tuple.Count : 
        currentValue.AsString().Length;


      Utils.MoveForwardIf(data, ref from, Constants.END_ARG, Constants.SPACE);

      Variable newValue = new Variable(size);
      return newValue;
    }
  }
  
  // Print Function
  // Отпечатва списък с аргументи
  class PrintFunction : ParserFunction
  {
    public PrintFunction(Interpreter interpreter, bool newLine = true)
    {
      print_interpreter = interpreter;
      _newLine = newLine;
    }
   
    protected override Variable Evaluate(string data, ref int from)
    {
      bool isList;
            
      List<Variable> args = Utils.GetArgs(data, ref from,
                
        Constants.START_ARG, Constants.END_ARG, out isList);

      string output = string.Empty;
            
      for (int i = 0; i < args.Count; i++)
      {
        output += args[i].AsString();
      }

      print_interpreter.AppendOutput(output, _newLine);

      return Variable.EmptyInstance;
    }

    private Interpreter print_interpreter;
        
    private bool _newLine;
  }

}
