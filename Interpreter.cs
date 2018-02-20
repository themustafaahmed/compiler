using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SplitAndMerge
{
    // Print Output
    public class OutputAvailableEventArgs : EventArgs
    {
        public string Output { get; set; }
    }

    public class Interpreter
    {
        
        private static Interpreter instance;

        public static Interpreter Instance // ako instansiata e null suzdavame nov obekt
        {
            get
            {
                if (instance == null)
                {
                    instance = new Interpreter();
                }

                return instance;
            }
        }
       
        private StringBuilder _output = new StringBuilder();

//        public string Output
//        {
//            get
//            {
//                string output = _output.ToString().Trim();
//                _output.Clear();
//                return output;
//            }
//        }
        
        private int MAX_LOOPS;
        
        private Interpreter()
        {
            Init();
        }

        public event EventHandler<OutputAvailableEventArgs> GetOutput;

        public void AppendOutput(string text, bool newLine = true)
        {
            EventHandler<OutputAvailableEventArgs> handler = GetOutput;
            if (handler != null)
            {
                OutputAvailableEventArgs args = new OutputAvailableEventArgs();
                args.Output = text + (newLine ? Environment.NewLine : string.Empty);
                handler(this, args);
            }
        }

        public void Init()
        {
            ParserFunction.AddGlobal(Constants.IF, new IfStatement(this));
            ParserFunction.AddGlobal(Constants.WHILE, new WhileStatement(this));
            ParserFunction.AddGlobal(Constants.BREAK, new BreakStatement());
            ParserFunction.AddGlobal(Constants.CONTINUE, new ContinueStatement());
            ParserFunction.AddGlobal(Constants.RETURN, new ReturnStatement());
            ParserFunction.AddGlobal(Constants.FUNCTION, new FunctionCreator(this));
            ParserFunction.AddGlobal(Constants.INCLUDE, new IncludeFile());

            ParserFunction.AddGlobal(Constants.SIZE, new SizeFunction());
            ParserFunction.AddGlobal(Constants.WRITE, new PrintFunction(this, false));
            ParserFunction.AddGlobal(Constants.WRITELN, new PrintFunction(this, true));

            ParserFunction.AddAction(Constants.ASSIGNMENT, new AssignFunction());
            ParserFunction.AddAction(Constants.INCREMENT, new IncrementDecrementFunction());
            ParserFunction.AddAction(Constants.DECREMENT, new IncrementDecrementFunction());

            for (int i = 0; i < Constants.OPER_ACTIONS.Length; i++)
            {
                ParserFunction.AddAction(Constants.OPER_ACTIONS[i], new OperatorAssignFunction());
            }

            Constants.ELSE_LIST.Add(Constants.ELSE);
            Constants.ELSE_IF_LIST.Add(Constants.ELSE_IF);

            ReadConfig();
        }

        public void ReadConfig()
        {
            MAX_LOOPS = ReadConfig("maxLoops", 256000);

            if (ConfigurationManager.GetSection("Languages") == null)
            {
                return;
            }

            var languagesSection = ConfigurationManager.GetSection("Languages") as NameValueCollection;

            if (languagesSection.Count == 0)
            {
                return;
            }

            string languages = languagesSection["languages"];

            string[] supportedLanguages = languages.Split(",".ToCharArray());

            foreach (string language in supportedLanguages)
            {
                var languageSection = ConfigurationManager.GetSection(language) as NameValueCollection;

                AddTranslation(languageSection, Constants.IF);
                AddTranslation(languageSection, Constants.WHILE);
                AddTranslation(languageSection, Constants.BREAK);
                AddTranslation(languageSection, Constants.CONTINUE);
                AddTranslation(languageSection, Constants.RETURN);
                AddTranslation(languageSection, Constants.FUNCTION);
                AddTranslation(languageSection, Constants.INCLUDE);

                AddTranslation(languageSection, Constants.SIZE);
                AddTranslation(languageSection, Constants.WRITE);
                AddTranslation(languageSection, Constants.WRITELN);

                // Специални сделки за други, elif, тъй като те не са отделни
                // функции, но са част от блок-декларацията if.
                // Същото и, или не.
                AddSubstatementTranslation(languageSection, Constants.ELSE, Constants.ELSE_LIST);
                AddSubstatementTranslation(languageSection, Constants.ELSE_IF, Constants.ELSE_IF_LIST);
            }
        }

        // Read Config
        public int ReadConfig(string configName, int defaultValue = 0)
        {
            string config = ConfigurationManager.AppSettings[configName];

            int value = defaultValue;

            if (string.IsNullOrWhiteSpace(config) || !Int32.TryParse(config, out value))
            {
                return defaultValue;
            }

            return value;
        }

        // Translation
        public void AddTranslation(NameValueCollection languageDictionary, string originalName)
        {
            string translation = languageDictionary[originalName];
            if (string.IsNullOrWhiteSpace(translation))
            {
                // Преводът не е предвиден за тази функция.
                return;
            }

            if (translation.IndexOfAny((" \t\r\n").ToCharArray()) >= 0)
            {
                throw new ArgumentException("Превод на [" + translation + "] съдържа бели полета");
            }

            ParserFunction originalFunction = ParserFunction.GetFunction(originalName);
            ParserFunction.AddGlobal(translation, originalFunction);

            // Ако списъкът с функции, след които може да има интервал (освен скоби)
            // съдържа оригиналната функция, също добавете превод към списъка.
            if (Constants.FUNCT_WITH_SPACE.Contains(originalName))
            {
                Constants.FUNCT_WITH_SPACE.Add(translation);
            }
        }

        public void AddSubstatementTranslation(NameValueCollection languageDictionary,
            string originalName, List<string> keywordsArray)
        {
            string translation = languageDictionary[originalName];
            if (string.IsNullOrWhiteSpace(translation))
            {
                // Преводът не е предоставен за този под-отчет.
                return;
            }

            if (translation.IndexOfAny((" \t\r\n").ToCharArray()) >= 0)
            {
                throw new ArgumentException("Превод на [" + translation + "] съдържа бели полета");
            }

            keywordsArray.Add(translation);
        }

        public Variable Process(string script)
        {
          //  Console.WriteLine(script);
            string data = Utils.ConvertToScript(script);
            
           // Console.WriteLine(data);
            
            if (string.IsNullOrWhiteSpace(data))
            {
                return null;
            }

            int currentChar = 0;
            
            Variable result = null;

            while (currentChar < data.Length)
            {
                result = Parser.LoadAndCalculate(data, ref currentChar, Constants.END_PARSE_ARRAY);//  ;)}\n
               // Console.WriteLine(currentChar); //761
                Utils.GoToNextStatement(data, ref currentChar);
            }

            return result;
        }

        public Variable ProcessWhile(string data, ref int from)
        {
            int startWhileCondition = from;

            // Проверка срещу безкраен цикъл.
            int cycles = 0;
            bool stillValid = true;

            while (stillValid)
            {
                from = startWhileCondition;

                //int startSkipOnBreakChar = from;
                Variable condResult = Parser.LoadAndCalculate(data, ref from, Constants.END_ARG_ARRAY);
                stillValid = Convert.ToBoolean(condResult.Value);

                if (!stillValid)
                {
                    break;
                }

                // Проверете за безкраен цикъл, ако сравняваме едни и същи стойности:
                if (MAX_LOOPS > 0 && ++cycles >= MAX_LOOPS)
                {
                    throw new ArgumentException("Изглежда като безкраен цикъл след " +
                                                cycles + " цикли.");
                }

                Variable result = ProcessBlock(data, ref from);
                if (result.Type == Variable.VarType.BREAK)
                {
                    from = startWhileCondition;
                    break;
                }
            }

            // Състоянието на времето вече не е вярно: трябва да прескочите цялото време
            // блок, преди да продължите с следващите изявления.
            SkipBlock(data, ref from);
            return new Variable();
        }

        public Variable ProcessIf(string data, ref int from)
        {
            int startIfCondition = from;

            Variable result = Parser.LoadAndCalculate(data, ref from, Constants.END_ARG_ARRAY);
            bool isTrue = Convert.ToBoolean(result.Value);

            if (isTrue)
            {
                result = ProcessBlock(data, ref from);

                if (result.Type == Variable.VarType.BREAK ||
                    result.Type == Variable.VarType.CONTINUE)
                {
                    // Имате тук от средата на блока. Пропуснете го.
                    from = startIfCondition;
                    SkipBlock(data, ref from);
                }

                SkipRestBlocks(data, ref from);

                return result;
            }

            // Пропуснете всичко в израза "Ако".
            SkipBlock(data, ref from);

            int endOfToken = from;
            string nextToken = Utils.GetNextToken(data, ref endOfToken);

            if (Constants.ELSE_IF_LIST.Contains(nextToken))
            {
                from = endOfToken + 1;
                result = ProcessIf(data, ref from);
            }
            else if (Constants.ELSE_LIST.Contains(nextToken))
            {
                from = endOfToken + 1;
                result = ProcessBlock(data, ref from);
            }

            return Variable.EmptyInstance;
        }


        private Variable ProcessBlock(string data, ref int from)
        {
            int blockStart = from;
            Variable result = null;

            while (from < data.Length)
            {
                int endGroupRead = Utils.GoToNextStatement(data, ref from);
                if (endGroupRead > 0)
                {
                    return result != null ? result : new Variable();
                }

                if (from >= data.Length)
                {
                    throw new ArgumentException("Блок не можа да бъде обработен [" +
                                                data.Substring(blockStart) + "]");
                }

                result = Parser.LoadAndCalculate(data, ref from, Constants.END_PARSE_ARRAY);


                if (result.Type == Variable.VarType.BREAK ||
                    result.Type == Variable.VarType.CONTINUE)
                {
                    return result;
                }
            }

            return result;
        }

        private void SkipBlock(string data, ref int from)
        {
            int blockStart = from;
            int startCount = 0;
            int endCount = 0;
            while (startCount == 0 || startCount > endCount)
            {
                if (from >= data.Length)
                {
                    throw new ArgumentException("Блок не можа да бъде прескочен [" + data.Substring(blockStart) + "]");
                }

                char currentChar = data[from++];
                switch (currentChar)
                {
                    case Constants.START_GROUP:
                        startCount++;
                        break;
                    case Constants.END_GROUP:
                        endCount++;
                        break;
                }
            }

            if (startCount != endCount)
            {
                throw new ArgumentException("Несъответстващи скоби");
            }
        }

        // Прескачане на останалите блокове
        private void SkipRestBlocks(string data, ref int from)
        {
            while (from < data.Length)
            {
                int endOfToken = from;
                string nextToken = Utils.GetNextToken(data, ref endOfToken);
                if (!Constants.ELSE_IF_LIST.Contains(nextToken) &&
                    !Constants.ELSE_LIST.Contains(nextToken))
                {
                    return;
                }

                from = endOfToken;
                SkipBlock(data, ref from);
            }
        }
    }
}