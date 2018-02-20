using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;


namespace SplitAndMerge
{
    public class Program
    {
       
        static void Main(string[] args)
        {
            string script;

            script = GetFileContents("scripts/test.cscs");
            

            // След всяко успешно изявление ще се задейства събитие за печат
            // екзекуция. При грешка ще бъде отхвърлено изключение.
            Interpreter.Instance.GetOutput += Print;

            if (!string.IsNullOrWhiteSpace(script))
            {
                ProcessScript(script);
                return;
            }
        }
        //Получете съдържанието на файла
        private static string GetFileContents(string filename)
        {
            string[] readText = File.ReadAllLines(filename);
            
            string text = string.Join("\n", readText);

            return text;
        }

        private static void ProcessScript(string script)
        {
            string error;
            Variable result;
            
            try
            {
                result = Interpreter.Instance.Process(script);
                
              //  Console.WriteLine(result);
            }
            catch (Exception exc)
            {
                error = exc.Message;
                ParserFunction.InvalidateStacksAfterLevel(0);
            }
        }

        static void Print(object sender, OutputAvailableEventArgs e)
        {
            Console.Write(e.Output);
        }  
    }
}