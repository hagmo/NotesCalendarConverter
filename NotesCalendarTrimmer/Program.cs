using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;

namespace NotesCalendarTrimmer
{
    class Program
    {
        static void Main(string[] args)
        {
            var stringBuilder = new StringBuilder();
            AntlrInputStream input = new AntlrInputStream(new System.IO.StreamReader(@"..\..\NotesCalendar.ics"));
            ITokenSource lexer = new ICalendarLexer(input);
            ITokenStream tokens = new CommonTokenStream(lexer);
            ICalendarParser parser = new ICalendarParser(tokens);
            DateTime start = DateTime.Now;
            var tree = parser.parse();
            var span = DateTime.Now - start;
            ParseTreeWalker.Default.Walk(new ICalendarTrimmerListener(stringBuilder), tree);
            Console.WriteLine("Took {0:g} to parse the file.", span);

            using (var writer = new System.IO.StreamWriter(@"..\..\NotesCalendarTrimmed.ics"))
            {
                writer.WriteLine(stringBuilder.ToString());
            }

            Console.ReadKey();
        }
    }
}
