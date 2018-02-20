using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SplitAndMerge
{
    public class Parser
    {
        public static bool Verbose { get; set; }

        // Load And Calculate
        // Зареди и Изчисли
        public static Variable LoadAndCalculate(string data, ref int from, char[] to)
        {
            // Първа стъпка: Процесът преминава през израз, като се разделя на списък от клетки.
            List<Variable> listToMerge = Calculate(data, ref from, to);
            
           // Console.WriteLine(to);
            
            if (listToMerge.Count == 0)
            {
                throw new ArgumentException("Couldn't parse [" +
                                            data.Substring(from) + "]");
            }

            // Ако има само една получена клетка, няма нужда
            //, за да извършите втората стъпка за сливане на символи.
            if (listToMerge.Count == 1)
            {
                return listToMerge[0];
            }

            Variable baseCell = listToMerge[0];
            int index = 1;

            // Втора стъпка: сливане на списък с клетки, за да получите резултата от израз.
            Variable result = Merge(baseCell, ref index, listToMerge);
            return result;
        }

        // Calculate
        // Изчисли
        private static List<Variable> Calculate(string data, ref int from, char[] to)
        {
            List<Variable> listToMerge = new List<Variable>(); // 32
            
           // Console.WriteLine(forum);

            if (from >= data.Length || to.Contains(data[from]))
            {
                listToMerge.Add(Variable.EmptyInstance);
                return listToMerge;
            }

            StringBuilder item = new StringBuilder();
            int negated = 0; // отказване
            bool inQuotes = false;  // в Цитати

            do
            {
                // Основен цикъл на обработка на първата част.
//                string negateSymbol = Utils.IsNotSign(data.Substring(from));
//                
//                //Console.WriteLine(negateSymbol);
//                
//                if (negateSymbol != null)
//                {
//                    negated++;
//                    from += negateSymbol.Length;
//                    continue;
//                }

                char ch = data[from++];
              
                inQuotes = ch == Constants.QUOTE && (from == 0 || data[from] != '\\') ? !inQuotes : inQuotes;
                
                string action = null;

                bool keepCollecting = inQuotes || StillCollecting(item.ToString(), to, data, from, ref action);
                
                //Console.WriteLine(ch);
                if (keepCollecting)
                {
                    // Char отново принадлежи към предишния операнд.
                    item.Append(ch);

                    if (from < data.Length && (inQuotes || !to.Contains(data[from])))
                    {
                        continue;
                    }
                }

                string parsingItem = item.ToString();

               // CheckConsistency(parsingItem, listToMerge, data, from);

                Utils.MoveForwardIf(data, ref from, Constants.SPACE);

                if (action != null && action.Length > 1)
                {
                    from += (action.Length - 1);
                }

                // Ние сме готови да получим следващия знак. Призивът getValue () може да е по-долу
                // рекурсивно извиква loadAndCalculate (). Това ще стане, ако се извлече
                // елементът е функция или ако следващият елемент започва с START_ARG '('.

                ParserFunction func = new ParserFunction(data, ref from, parsingItem, ch, ref action);

                Variable current = func.GetValue(data, ref from);

                if (negated > 0 && current.Type == Variable.VarType.NUMBER)
                {
                    // Ако има знак NOT, това е булев.
                    // Използвайте XOR (вярно, ако точно един от аргументите е вярно).
                    bool boolRes = !((negated % 2 == 0) ^ Convert.ToBoolean(current.Value));
                    current = new Variable(Convert.ToDouble(boolRes));
                    negated = 0;
                }

                if (action == null)
                {
                    action = UpdateAction(data, ref from, to);
                }


                char next = from < data.Length ? data[from] : Constants.EMPTY;
                bool done = listToMerge.Count == 0 &&
                            ((action == Constants.END_ARG_STR &&
                              current.Type != Variable.VarType.NUMBER) ||
                             next == Constants.END_STATEMENT);
                if (done)
                {
                    // Ако няма числен резултат, ние не сме в математически израз.
                    listToMerge.Add(current);
                    return listToMerge;
                }

                Variable cell = new Variable(current);
                cell.Action = action;
                listToMerge.Add(cell);
                item.Clear();
            } while (from < data.Length && (inQuotes || !to.Contains(data[from])));

            // Това се случва, когато се нарича рекурсивно вътре в математическия израз:
            Utils.MoveForwardIf(data, ref from, Constants.END_ARG);

            return listToMerge;
        }

        // Still Collecting
        // Все още събиране
        private static bool StillCollecting(string item, char[] to, string data, int from,
            ref string action)
        {
            char ch = from > 0 ? data[from - 1] : Constants.EMPTY;
            char next = from < data.Length ? data[from] : Constants.EMPTY;
            char prev = from > 1 ? data[from - 2] : Constants.EMPTY;

            if (to.Contains(ch) || ch == Constants.START_ARG ||
                ch == Constants.START_GROUP ||
                next == Constants.EMPTY)
            {
                return false;
            }

            // Случай с отрицателно число или със затваряща скоба:
            if (item.Length == 0 &&
                ((ch == '-' && next != '-') || ch == Constants.END_ARG))
            {
                return true;
            }

            // Случай с научно обозначение 1.2e + 5 или 1.2e-5 или 1e5:
            if (Char.ToUpper(prev) == 'E' &&
                (ch == '-' || ch == '+' || Char.IsDigit(ch)) &&
                item.Length > 1 && Char.IsDigit(item[item.Length - 2]))
            {
                return true;
            }

            // В противен случай, ако е действие (+, -, * и т.н.) или интервал
            // свършихме събирането на ток.
            if ((action = Utils.ValidAction(data, from - 1)) != null ||
                (item.Length > 0 && ch == Constants.SPACE))
            {
                return false;
            }

            return true;
        }

        //Update Action
        //Актуализиране на действието
        private static string UpdateAction(string data, ref int from, char[] to)
        {
            // Търсим валидно действие, докато стигнем края на аргумента ')'
            // или да премине края на низа.
            if (from >= data.Length || data[from] == Constants.END_ARG ||
                to.Contains(data[from]))
            {
                return Constants.END_ARG.ToString();
            }

            // Започнете търсенето от предишния знак.
            int index = from; // - 1;

            string action = Utils.ValidAction(data, index);

            //while (action == null && index < data.Length)
            while (action == null && index < data.Length &&
                   data[index] == Constants.END_ARG)
            {
                // Потърсете следващия знак в низ, докато се намери валидно действие.
                action = Utils.ValidAction(data, ++index);

                //if (index >= data.Length || data [index] != Constants.END_ARG) {
                //	break;
                //}
            }

            // Необходимо е да напредваме напред не само с дължината на действие, но и с всички
            // героите, които пропуснахме преди да получим действието.
            int advance = action == null ? 0 : action.Length + Math.Max(0, index - from);
            from += advance;
            return action == null ? Constants.END_ARG.ToString() : action;
        }

        // Отвън тази функция се нарича с mergeOneOnly = false.
        // Той също така се нарича рекурсивно с mergeOneOnly = true, meaning
        //, че ще се върне след само едно сливане.    
        // Variable svrizvane
        // Variable Merge
        private static Variable Merge(Variable current, ref int index, List<Variable> listToMerge,
            bool mergeOneOnly = false)
        {
            if (Verbose)
            {
                Utils.PrintList(listToMerge, index - 1);
            }

            while (index < listToMerge.Count)
            {
                Variable next = listToMerge[index++];

                while (!CanMergeCells(current, next))
                {
                    // Ако все още не можем да обединим клетките, отидете до следващата клетка и се слеете
                    // следващите клетки първо. Например ако имаме 1 + 2 * 3, първо се слеем
                    // клетки, т.е. 2 * 3, получаване 6, след което можем да обединим 1 + 6.
                    Merge(next, ref index, listToMerge, true /* mergeOneOnly */);
                }

                MergeCells(current, next);
                if (mergeOneOnly)
                {
                    break;
                }
            }

            if (Verbose)
            {
                Console.WriteLine("Calculated: {0} {1}",
                    current.Value, current.String);
            }

            return current;
        }

        // Сливане на клетки
        // Merge Cells
        private static void MergeCells(Variable leftCell, Variable rightCell)
        {
            if (leftCell.Type == Variable.VarType.BREAK ||
                leftCell.Type == Variable.VarType.CONTINUE)
            {
                // Done!
                return;
            }

            if (leftCell.Type == Variable.VarType.NUMBER ||
                rightCell.Type == Variable.VarType.NUMBER)
            {
                MergeNumbers(leftCell, rightCell);
            }
            else
            {
                MergeStrings(leftCell, rightCell);
            }

            leftCell.Action = rightCell.Action;
        }

        // Сливане на Numbers
        // MergeNumbers
        private static void MergeNumbers(Variable leftCell, Variable rightCell)
        {
            if (leftCell.Action != "+" &&
                rightCell.Type != Variable.VarType.NUMBER)
            {
                throw new ArgumentException("Can't merge a number " +
                                            leftCell.Value + " with [" + rightCell.AsString() + "]");
            }

            switch (leftCell.Action)
            {
                case "^":
                    leftCell.Value = Math.Pow(leftCell.Value, rightCell.Value);
                    break;
                case "%":
                    leftCell.Value %= rightCell.Value;
                    break;
                case "*":
                    leftCell.Value *= rightCell.Value;
                    break;
                case "/":
                    if (rightCell.Value == 0)
                    {
                        throw new ArgumentException("Division by zero");
                    }

                    leftCell.Value /= rightCell.Value;
                    break;
                case "+":
                    if (rightCell.Type != Variable.VarType.NUMBER)
                    {
                        leftCell.String = leftCell.AsString() + rightCell.String;
                    }
                    else
                    {
                        leftCell.Value += rightCell.Value;
                    }

                    break;
                case "-":
                    leftCell.Value -= rightCell.Value;
                    break;
                case "<":
                    leftCell.Value = Convert.ToDouble(leftCell.Value < rightCell.Value);
                    break;
                case ">":
                    leftCell.Value = Convert.ToDouble(leftCell.Value > rightCell.Value);
                    break;
                case "<=":
                    leftCell.Value = Convert.ToDouble(leftCell.Value <= rightCell.Value);
                    break;
                case ">=":
                    leftCell.Value = Convert.ToDouble(leftCell.Value >= rightCell.Value);
                    break;
                case "==":
                    leftCell.Value = Convert.ToDouble(leftCell.Value == rightCell.Value);
                    break;
                case "!=":
                    leftCell.Value = Convert.ToDouble(leftCell.Value != rightCell.Value);
                    break;
                case "&&":
                    leftCell.Value = Convert.ToDouble(
                        Convert.ToBoolean(leftCell.Value) && Convert.ToBoolean(rightCell.Value));
                    break;
                case "||":
                    leftCell.Value = Convert.ToDouble(
                        Convert.ToBoolean(leftCell.Value) || Convert.ToBoolean(rightCell.Value));
                    break;
            }
        }

        // Merge Strings
        // Сливане на Strings
        private static void MergeStrings(Variable leftCell, Variable rightCell)
        {
            switch (leftCell.Action)
            {
                case "+":
                    leftCell.String += rightCell.AsString();
                    break;
                case "<":
                    leftCell.Value = Convert.ToDouble(
                        string.Compare(leftCell.String, rightCell.String) < 0);
                    break;
                case ">":
                    leftCell.Value = Convert.ToDouble(
                        string.Compare(leftCell.String, rightCell.String) > 0);
                    break;
                case "<=":
                    leftCell.Value = Convert.ToDouble(
                        string.Compare(leftCell.String, rightCell.String) <= 0);
                    break;
                case ">=":
                    leftCell.Value = Convert.ToDouble(
                        string.Compare(leftCell.String, rightCell.String) >= 0);
                    break;
                case "==":
                    leftCell.Value = Convert.ToDouble(
                        string.Compare(leftCell.String, rightCell.String) == 0);
                    break;
                case "!=":
                    leftCell.Value = Convert.ToDouble(
                        string.Compare(leftCell.String, rightCell.String) != 0);
                    break;
                default:

                    throw new ArgumentException("Can't perform action [" +
                                                leftCell.Action + "] on strings");
            }
        }

        private static bool CanMergeCells(Variable leftCell, Variable rightCell)
        {
            return GetPriority(leftCell.Action) >= GetPriority(rightCell.Action);
        }

        //Приоритет
        private static int GetPriority(string action)
        {
            switch (action)
            {
                case "++":
                case "--": return 10;
                case "^": return 9;
                case "%":
                case "*":
                case "/": return 8;
                case "+":
                case "-": return 7;
                case "<":
                case ">":
                case ">=":
                case "<=": return 6;
                case "==":
                case "!=": return 5;
                case "&&": return 4;
                case "||": return 3;
                case "+=":
                case "=": return 2;
            }

            return 0;
        }
    }
}