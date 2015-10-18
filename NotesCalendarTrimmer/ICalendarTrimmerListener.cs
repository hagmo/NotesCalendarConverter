using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NotesCalendarTrimmer
{
    class ICalendarTrimmerListener : ICalendarBaseListener
    {
        private static readonly string dateTimeFormat = "yyyyMMddTHHmmss";
        private bool m_HasWrittenHeader;
        private StringBuilder m_SB;
        private StringBuilder m_CalendarPreamble;
        private StringBuilder m_CurrentEvent;
        private List<IcalEvent> m_Events;

        private List<Tuple<DateTime, DateTime>> m_RDates;
        private string m_CurrentDTSTARTPrefix;
        private string m_CurrentDTENDPrefix;
        private DateTime m_CurrentEventDateTime;

        public ICalendarTrimmerListener(StringBuilder sb)
        {
            m_SB = sb;
            m_CalendarPreamble = new StringBuilder();
            m_RDates = new List<Tuple<DateTime, DateTime>>();
            m_CurrentEvent = new StringBuilder();
            m_Events = new List<IcalEvent>();
        }

        public override void ExitIcalstream(ICalendarParser.IcalstreamContext context)
        {
            m_SB.AppendLine("BEGIN:VCALENDAR");
            m_SB.Append(m_CalendarPreamble.ToString());
            m_Events.Sort(delegate(IcalEvent x, IcalEvent y)
            {
                return x.StartTime.CompareTo(y.StartTime);
            });
            foreach (var vevent in m_Events)
            {
                m_SB.Append(vevent.Text);
            }
            m_SB.AppendLine("END:VCALENDAR");
        }

        /// <summary>
        /// Save the text of the VTIMEZONE entry.
        /// </summary>
        /// <param name="context"></param>
        public override void ExitTimezonec(ICalendarParser.TimezonecContext context)
        {
            if (!m_HasWrittenHeader)
                m_CalendarPreamble.Append(context.GetText());
        }

        /// <summary>
        /// Initialization of data holders.
        /// </summary>
        /// <param name="context"></param>
        public override void EnterEventc(ICalendarParser.EventcContext context)
        {
            m_RDates.Clear();
            m_CurrentEvent.Clear();
            m_CurrentEventDateTime = DateTime.MinValue;

            //If we have found the first event, it means we are done with the header.
            m_HasWrittenHeader = true;
        }

        /// <summary>
        /// Write a copy of the entire event for each repeated date.
        /// </summary>
        /// <remarks>
        /// It seems that neither Google Calendar nor Lotus Notes handle the RDATE property properly.
        /// We need to turn all events that contain the RDATE property into several copies of the same
        /// event with different start and end times.
        /// </remarks>
        /// <param name="context"></param>
        public override void ExitEventc(ICalendarParser.EventcContext context)
        {

            var pre = string.Format("{0}:{1}",
                                    context.k_begin().GetText(),
                                    context.k_vevent()[0].GetText());


            var post = string.Format("{0}:{1}",
                                    context.k_end().GetText(),
                                    context.k_vevent()[1].GetText());


            var sb = new StringBuilder();
            foreach (var dates in m_RDates)
            {
                sb.Clear();
                sb.AppendLine(pre);
                sb.Append(m_CurrentEvent.ToString());

                sb.Append(m_CurrentDTSTARTPrefix).Append(dates.Item1.ToString(dateTimeFormat));
                sb.AppendLine();

                sb.Append(m_CurrentDTENDPrefix).Append(dates.Item2.ToString(dateTimeFormat));
                sb.AppendLine();

                sb.AppendLine(post);
                m_Events.Add(new IcalEvent()
                    {
                        StartTime = dates.Item1,
                        Text = sb.ToString()
                    });
            }
        }

        /// <summary>
        /// Save the description text.
        /// </summary>
        /// <param name="context"></param>
        public override void ExitDescription(ICalendarParser.DescriptionContext context)
        {
            m_CurrentEvent.Append(context.GetText());
        }

        /// <summary>
        /// Save the location text.
        /// </summary>
        /// <param name="context"></param>
        public override void ExitLocation(ICalendarParser.LocationContext context)
        {
            m_CurrentEvent.Append(context.GetText());
        }

        /// <summary>
        /// Save the summary text.
        /// </summary>
        /// <param name="context"></param>
        public override void ExitSummary(ICalendarParser.SummaryContext context)
        {
            m_CurrentEvent.Append(context.GetText());
        }

        /// <summary>
        /// Save the date and relevant text.
        /// </summary>
        /// <param name="context"></param>
        public override void ExitDtstart(ICalendarParser.DtstartContext context)
        {
            var sb = new StringBuilder();
            sb.Append(context.k_dtstart().GetText());
            foreach (var dtstparam in context.dtstparam())
            {
                sb.Append(dtstparam.GetText());
            }
            sb.Append(':');
            m_CurrentDTSTARTPrefix = sb.ToString();

            var dateTime = ParseDateTime(context.date_time_date().GetText());
            if (m_CurrentEventDateTime == DateTime.MinValue)
            {
                m_CurrentEventDateTime = dateTime;
            }
            else
            {
                m_RDates.Add(new Tuple<DateTime, DateTime>(dateTime, m_CurrentEventDateTime));
            }
        }

        /// <summary>
        /// Save the date and relevant text.
        /// </summary>
        /// <param name="context"></param>
        public override void ExitDtend(ICalendarParser.DtendContext context)
        {
            var sb = new StringBuilder();
            sb.Append(context.k_dtend().GetText());
            foreach (var dtendparam in context.dtendparam())
            {
                sb.Append(dtendparam.GetText());
            }
            sb.Append(':');
            m_CurrentDTENDPrefix = sb.ToString();

            var dateTime = ParseDateTime(context.date_time_date().GetText());
            if (m_CurrentEventDateTime == DateTime.MinValue)
            {
                m_CurrentEventDateTime = dateTime;
            }
            else
            {
                m_RDates.Add(new Tuple<DateTime, DateTime>(m_CurrentEventDateTime, dateTime));
            }
        }

        /// <summary>
        /// Save the repeating dates.
        /// </summary>
        /// <param name="context"></param>
        public override void ExitRdtval(ICalendarParser.RdtvalContext context)
        {
            var startTime = ParseDateTime(context.period().period_explicit().date_time()[0].GetText());
            var endTime = ParseDateTime(context.period().period_explicit().date_time()[1].GetText());
            m_RDates.Add(new Tuple<DateTime, DateTime>(startTime, endTime));
        }

        /// <summary>
        /// Parse a <c>DateTime</c> according to <c>dateTimeFormat</c>. This is the only format I have come across
        /// in my calendar files and it doesn't seem to correspond to any standard format strings in .NET.
        /// </summary>
        /// <param name="dateTime"></param>
        /// <returns></returns>
        private DateTime ParseDateTime(string dateTime)
        {
            if (dateTime.EndsWith("Z"))
                dateTime = dateTime.Remove(dateTime.Length - 1);
            return DateTime.ParseExact(dateTime, dateTimeFormat, System.Globalization.CultureInfo.InvariantCulture);
        }

        private class IcalEvent
        {
            public DateTime StartTime { get; set; }
            public string Text { get; set; }
        }
    }
}
