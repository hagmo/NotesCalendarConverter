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
            var calendarContents = new StringBuilder();
            Console.WriteLine("Preprocessing calendar file...");
            int nbrIncluded = 0;
            int nbrEvents = 0;
            using (var reader = new System.IO.StreamReader(@"..\..\NotesCalendar.ics"))
            {
                while (!reader.EndOfStream)
                {
                    var line = reader.ReadLine();
                    if (line.Trim() == "BEGIN:VEVENT")
                    {
                        nbrEvents++;
                        var currentEvent = new StringBuilder();
                        bool includeEvent = false;
                        while (line.Trim() != "END:VEVENT")
                        {
                            currentEvent.AppendLine(line);
                            if (line.StartsWith("DTSTART"))
                            {
                                int dateIndex = line.IndexOf(':') + 1;
                                var startDate = DateTime.ParseExact(line.Substring(dateIndex), "yyyyMMddTHHmmss", System.Globalization.CultureInfo.InvariantCulture);
                                if (startDate >= DateTime.Now.AddDays(-7))
                                {
                                    includeEvent = true;
                                }
                            }
                            line = reader.ReadLine();
                        }
                        currentEvent.AppendLine(line);
                        if (includeEvent)
                        {
                            nbrIncluded++;
                            calendarContents.Append(currentEvent.ToString());
                        }
                    }
                    else
                    {
                        calendarContents.AppendLine(line);
                    }
                }
            }
            Console.WriteLine("Skipped {0} events. Parsing {1} events...", nbrEvents - nbrIncluded, nbrIncluded);

            var stringBuilder = new StringBuilder();
            AntlrInputStream input = new AntlrInputStream(calendarContents.ToString());
            ITokenSource lexer = new ICalendarLexer(input);
            ITokenStream tokens = new CommonTokenStream(lexer);
            ICalendarParser parser = new ICalendarParser(tokens);
            DateTime start = DateTime.Now;
            var tree = parser.parse();
            var span = DateTime.Now - start;
            ParseTreeWalker.Default.Walk(new ICalendarTrimmerListener(stringBuilder), tree);
            Console.WriteLine("Parsing finished after {0:g}.", span);

            using (var writer = new System.IO.StreamWriter(@"..\..\NotesCalendarTrimmed.ics"))
            {
                writer.WriteLine(stringBuilder.ToString());
            }

            Console.ReadKey();
        }
    }
}
