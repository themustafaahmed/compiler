using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace SplitAndMerge
{
    public partial class Utils
    {
        public static Variable GetItem(string data, ref int from)
        {
            MoveForwardIf(data, ref from, Constants.NEXT_ARG, Constants.SPACE);

            if (data.Length <= from)
            {
                throw new ArgumentException("Непълно дефиниране на функциите");
            }

            Variable value = new Variable();

            if (data[from] == Constants.QUOTE)
            {
                // Извличаме низ между котировките.
                from++; // Пропуснете първата оферта.
                if (from < data.Length && data[from] == Constants.QUOTE)
                {
                    // аргументът е ""
                    value.String = "";
                }
                else
                {
                    value.String = Utils.GetToken(data, ref from, Constants.QUOTE_ARRAY);
                }

                from++; // пропуснете следващото разделяне Чар
            }
            else if (data[from] == Constants.START_GROUP)
            {
                // Извличаме списък от къдрави скоби.
                from++; // Прескачане на първата скоба.
                bool isList = true;
                value.Tuple = GetArgs(data, ref from,
                    Constants.START_GROUP, Constants.END_GROUP, out isList);

                return value;
            }
            else
            {
                // Променлива, функция или число.
                Variable var = Parser.LoadAndCalculate(data, ref from, Constants.NEXT_OR_END_ARRAY);
                value.Copy(var);
            }

            MoveForwardIf(data, ref from, Constants.END_ARG, Constants.SPACE);
            return value;
        }

        public static string GetToken(string data, ref int from, char[] to)
        {
            char curr = from < data.Length ? data[from] : Constants.EMPTY;
            char prev = from > 0 ? data[from - 1] : Constants.EMPTY;

            if (!to.Contains(Constants.SPACE))
            {
                // Пропуснете водещо място, освен ако не сме в кавички
                while (curr == Constants.SPACE && prev != Constants.QUOTE)
                {
                    from++;
                    curr = from < data.Length ? data[from] : Constants.EMPTY;
                    prev = from > 0 ? data[from - 1] : Constants.EMPTY;
                }
            }

            MoveForwardIf(data, ref from, Constants.QUOTE);

            int end = data.IndexOfAny(to, from);
            if (from >= end)
            {
                from++;
                return string.Empty;
            }

            // Пропуснете намерените знаци, които имат обратна наклонена черта преди.
            while ((end > 0 && data[end - 1] == '\\') && end + 1 < data.Length)
            {
                end = data.IndexOfAny(to, end + 1);
            }

            if (end < from)
            {
                throw new ArgumentException("Не може да се извлече означението от " + data.Substring(from));
            }

            if (data[end - 1] == Constants.QUOTE)
            {
                end--;
            }

            string var = data.Substring(from, end - from);
            // \"yes\" --> "yes"
            var = var.Replace("\\\"", "\"");
            //from = end + 1;
            from = end;

            MoveForwardIf(data, ref from, Constants.QUOTE, Constants.SPACE);

            return var;
        }

        public static string GetNextToken(string data, ref int from)
        {
            if (from >= data.Length)
            {
                return "";
            }

            int end = data.IndexOfAny(Constants.TOKEN_SEPARATION, from);

            if (end < from)
            {
                return "";
            }

            string var = data.Substring(from, end - from);
            from = end;
            return var;
        }

        public static int GoToNextStatement(string data, ref int from)
        {
            int endGroupRead = 0;
            while (from < data.Length)
            {
                char currentChar = data[from];
                switch (currentChar)
                {
                    case Constants.END_GROUP:
                        endGroupRead++;
                        from++;
                        return endGroupRead;
                    case Constants.START_GROUP:
                    case Constants.QUOTE:
                    case Constants.SPACE:
                    case Constants.END_STATEMENT:
                    case Constants.END_ARG:
                        from++;
                        break;
                    default: return endGroupRead;
                }
            }

            return endGroupRead;
        }


        public static List<Variable> GetArgs(string data, ref int from,
            char start, char end, out bool isList)
        {
            List<Variable> args = new List<Variable>();

            isList = from < data.Length && data[from] == Constants.START_GROUP;

            if (from >= data.Length || data[from] == Constants.END_STATEMENT)
            {
                return args;
            }

            int lastChar = from;
            Utils.GetBodyBetween(data, ref lastChar, start, end);

            while (from < lastChar)
            {
                Variable item = Utils.GetItem(data, ref from);
                if (item.Equals(Variable.EmptyInstance))
                {
                    break;
                }

                args.Add(item);
            }


            MoveForwardIf(data, ref from, end, Constants.SPACE);

            return args;
        }

        public static string[] GetFunctionSignature(string data, ref int from)
        {
            MoveForwardIf(data, ref from, Constants.START_ARG, Constants.SPACE);

            int endArgs = data.IndexOf(Constants.END_ARG, from);
            if (endArgs < 0)
            {
                throw new ArgumentException("Не може да се извлече подпис на функцията");
            }

            string argStr = data.Substring(from, endArgs - from);
            string[] args = argStr.Split(Constants.NEXT_ARG_ARRAY);

            from = endArgs + 1;

            return args;
        }

        public static int ExtractArrayElement(ref string varName)
        {
            int argStart = varName.IndexOf(Constants.START_ARRAY);
            if (argStart <= 0)
            {
                return -1;
            }

            int argEnd = varName.IndexOf(Constants.END_ARRAY, argStart + 1);
            if (argEnd <= argStart + 1)
            {
                return -1;
            }

            int getIndexFrom = argStart;
            Utils.MoveForwardIf(varName, ref getIndexFrom,
                Constants.START_ARG, Constants.START_ARRAY);

            Variable existing = Parser.LoadAndCalculate(varName, ref getIndexFrom,
                Constants.END_ARRAY_ARRAY);

            if (existing.Type == Variable.VarType.NUMBER && existing.Value >= 0)
            {
                varName = varName.Substring(0, argStart);
                return (int) existing.Value;
            }

            return -1;
        }

        public static bool EndsWithFunction(string buffer, List<string> functions)
        {
            foreach (string key in functions)
            {
                if (buffer.EndsWith(key))
                {
                    char prev = key.Length >= buffer.Length
                        ? Constants.END_STATEMENT
                        : buffer[buffer.Length - key.Length - 1];
                    if (Constants.TOKEN_SEPARATION.Contains(prev))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public static bool SpaceNotNeeded(char next)
        {
            return (next == Constants.SPACE || next == Constants.START_ARG ||
                    next == Constants.START_GROUP || next == Constants.START_ARRAY ||
                    next == Constants.EMPTY);
        }

        public static bool KeepSpace(StringBuilder sb, char next)
        {
            if (SpaceNotNeeded(next))
            {
                return false;
            }

            return EndsWithFunction(sb.ToString(), Constants.FUNCT_WITH_SPACE);
        }

        public static bool KeepSpaceOnce(StringBuilder sb, char next)
        {
            if (SpaceNotNeeded(next))
            {
                return false;
            }

            return EndsWithFunction(sb.ToString(), Constants.FUNCT_WITH_SPACE_ONCE);
        }

        public static string ConvertToScript(string source)
        {
            StringBuilder sb = new StringBuilder(source.Length);

            bool inQuotes = false;  // "
            
            bool spaceOK = false; // space
            
            bool inComments = false; // comment
            
            char previous = Constants.EMPTY; // \0 

            int parentheses = 0; // []
            
            int groups = 0; // {}

            for (int i = 0; i < source.Length; i++)
            {
                char ch = source[i];
                char next = i + 1 < source.Length ? source[i + 1] : Constants.EMPTY;

                if (inComments && ch != '\n')
                {
                    continue;
                }

                switch (ch)
                {
                    case '/':
                        if (inComments || next == '/')
                        {
                            inComments = true;
                            continue;
                        }

                        break;
                    case '“':
                    case '”':
                    case '"':
                        ch = '"';
                        if (!inComments)
                        {
                            if (previous != '\\') inQuotes = !inQuotes;
                        }

                        break;
                    case ' ':
                        if (inQuotes)
                        {
                            sb.Append(ch);
                        }
                        else
                        {
                            bool keepSpace = KeepSpace(sb, next);
                            spaceOK = keepSpace ||
                                      (previous != Constants.EMPTY && previous != Constants.NEXT_ARG && spaceOK);
                            bool spaceOKonce = KeepSpaceOnce(sb, next);
                            if (spaceOK || spaceOKonce)
                            {
                                sb.Append(ch);
                            }
                        }

                        continue;
                    case '\t':
                    case '\r':
                        if (inQuotes) sb.Append(ch);
                        continue;
                    case '\n':
                        inComments = false;
                        spaceOK = false;
                        continue;
                    case Constants.END_ARG:
                        if (!inQuotes)
                        {
                            parentheses--;
                            spaceOK = false;
                        }

                        break;
                    case Constants.START_ARG:
                        if (!inQuotes)
                        {
                            parentheses++;
                        }

                        break;
                    case Constants.END_GROUP:
                        if (!inQuotes)
                        {
                            groups--;
                            spaceOK = false;
                        }

                        break;
                    case Constants.START_GROUP:
                        if (!inQuotes)
                        {
                            groups++;
                        }

                        break;
                    case Constants.END_STATEMENT:
                        if (!inQuotes)
                        {
                            spaceOK = false;
                        }

                        break;
                    default: break;
                }

                if (!inComments)
                {
                    sb.Append(ch);
                }

                previous = ch;
            }

            if (parentheses != 0)
            {
                throw new ArgumentException("Неравномерни скоби " + Constants.START_ARG + Constants.END_ARG);
            }

            if (groups != 0)
            {
                throw new ArgumentException("Неравнопоставени групи " + Constants.START_GROUP + Constants.END_GROUP);
            }

            return sb.ToString();
        }

        // Вземи тялото между
        public static string GetBodyBetween(string data, ref int from, char open, char close)
        {
            // Ние трябва да бъдем един знак след началото на низа, т.е.
            // не трябва да имаме началния знак като първият.
            StringBuilder sb = new StringBuilder(data.Length);
            int braces = 0;

            for (; from < data.Length; from++)
            {
                char ch = data[from];

                if (string.IsNullOrWhiteSpace(ch.ToString()) && sb.Length == 0)
                {
                    continue;
                }
                else if (ch == open)
                {
                    braces++;
                }
                else if (ch == close)
                {
                    braces--;
                }

                sb.Append(ch);
                if (braces == -1)
                {
                    if (ch == close)
                    {
                        sb.Remove(sb.Length - 1, 1);
                    }

                    break;
                }
            }

            return sb.ToString();
        }


//        public static string IsNotSign(string data)
//        {
//            return data.StartsWith(Constants.NOT) ? Constants.NOT : null;
//        }

        // Валидно действие
        public static string ValidAction(string data, int from)
        {
            if (from < 0 || from >= data.Length)
            {
                return null;
            }

            string action = Utils.StartsWith(data.Substring(from), Constants.ACTIONS);
            return action;
        }

        // Започва с
        public static string StartsWith(string data, string[] items)
        {
            foreach (string item in items)
            {
                if (data.StartsWith(item))
                {
                    return item;
                }
            }

            return null;
        }
    
        // Преместване напред
        public static bool MoveForwardIf(string data, ref int from, char expected,
            char expected2 = Constants.EMPTY)
        {
            if (from < data.Length &&
                (data[from] == expected || data[from] == expected2))
            {
                from++;
                return true;
            }

            return false;
        }

        public static bool MoveBackIf(string data, ref int from, char notExpected)
        {
            if (from < data.Length && from > 0 && data[from] == notExpected)
            {
                from--;
                return true;
            }

            return false;
        }   
        
        //Check Integer
        //Проверка на Integer
        public static void CheckInteger(Variable variable)
        {
            CheckNumber(variable);
            
            if (variable.Value % 1 != 0)
            {
                throw new ArgumentException("Очаква се цяло число вместо [" +
                                            variable.Value + "]");
            }
        }
        
        //Check Number
        //Проверка на номер
        public static void CheckNumber(Variable variable)
        {
            if (variable.Type != Variable.VarType.NUMBER)
            {
                throw new ArgumentException("Очаква се число вместо [" +
                                            variable.String + "]");
            }
        }  
 
        // Вземи файловите линии
        // Get File Lines
        // for Include File System
        public static string[] GetFileLines(string filename)
        {
            try
            {
                string[] lines = File.ReadAllLines(filename);
                return lines;
            }
            catch (Exception ex)
            {
                throw new ArgumentException("Файлът не може да се чете от диска: " + ex.Message);
            }
        } 
        
        //Print List
        // Печат списък
        public static void PrintList(List<Variable> list, int from)
        {
            Console.Write("смесвам в списъка:");
            
            for (int i = from; i < list.Count; i++)
            {
                Console.Write(" ({0}, '{1}')", list[i].Value, list[i].Action);
            }

            Console.WriteLine();
        }
    }
}