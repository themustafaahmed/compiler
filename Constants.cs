
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SplitAndMerge
{
    public class Constants
    {
        public const char START_ARG = '(';
        public const char END_ARG = ')';
        public const char START_ARRAY = '[';
        public const char END_ARRAY = ']';
        public const char END_LINE = '\n';
        public const char NEXT_ARG = ',';
        public const char QUOTE = '"';
        public const char SPACE = ' ';
        public const char START_GROUP = '{';
        public const char END_GROUP = '}';
        public const char END_STATEMENT = ';';
        public const char EMPTY = '\0';

        public const string ASSIGNMENT = "=";
      //  public const string NOT = "!";
        public const string INCREMENT = "++";
        public const string DECREMENT = "--";

        public const string IF = "if";
        public const string ELSE = "else";
        public const string ELSE_IF = "elif";
        public const string WHILE = "while";
        public const string BREAK = "break";
        public const string CONTINUE = "continue";
        public const string FUNCTION = "function";
        public const string RETURN = "return";
        public const string INCLUDE = "include";
       // public const string COMMENT = "//";

        public const string SIZE = "size";
        public const string WRITE = "write";
        public const string WRITELN = "writeln";

        public static string END_ARG_STR = END_ARG.ToString();

        public static string[] OPER_ACTIONS = {"+=", "-=", "*=", "/=", "%=", "&=", "|=", "^="};

        public static string[] MATH_ACTIONS =
        {
            "&&", "||", "==", "!=", "<=", ">=", "++", "--",
            "%", "*", "/", "+", "-", "^", "<", ">", "="
        };

        // Действия: винаги намалява с броя на знаците.
        public static string[] ACTIONS = (OPER_ACTIONS.Union(MATH_ACTIONS)).ToArray();

        public static char[] NEXT_ARG_ARRAY = NEXT_ARG.ToString().ToCharArray();
        public static char[] END_ARG_ARRAY = END_ARG.ToString().ToCharArray();
        public static char[] END_ARRAY_ARRAY = END_ARRAY.ToString().ToCharArray();
        public static char[] END_LINE_ARRAY = END_LINE.ToString().ToCharArray();
        public static char[] QUOTE_ARRAY = QUOTE.ToString().ToCharArray();

        public static char[] END_PARSE_ARRAY = " ;)}\n".ToCharArray();
        
        public static char[] NEXT_OR_END_ARRAY = {NEXT_ARG, END_ARG, END_GROUP, END_STATEMENT, SPACE};

        // ОТДЕЛЯНЕ НА символи
        public static char[] TOKEN_SEPARATION = ("<>=+-*/%&|^\t " + Environment.NewLine +
                                                 
                          /*NOT + */START_ARG + END_ARG + START_GROUP + END_GROUP + NEXT_ARG +
                                                 
                          END_STATEMENT).ToCharArray();

        // Функции, които позволяват разделител на пространството след тях, в горната част на скобите. Най-
        // аргументите за функциите също могат да имат интервали, напр. a.txt b.txt

        public static List<string> FUNCT_WITH_SPACE = new List<string>
        {
            FUNCTION,
            WRITE,
            WRITELN
        };

        // Функции, които позволяват разделител на пространството след тях, в горната част на скобите, но
        // само веднъж, т.е. не се допускат интервали на функционалните аргументи
        // между тях, напр. return a * b;
        public static List<string> FUNCT_WITH_SPACE_ONCE = new List<string>
        {
            RETURN
        };


        public static List<string> ELSE_LIST = new List<string>();
        public static List<string> ELSE_IF_LIST = new List<string>();
        public static int MAX_ERROR_CHARS = 20;
    }
}