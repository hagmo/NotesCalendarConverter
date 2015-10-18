# NotesCalendarConverter
Google Calendar does not play nice with ICS files exported from IBM Lotus Notes, which is a pain if you like to keep your personal calendar in sync with your work calendar. This simple utility uses a parser generated with [ANTLR](http://www.antlr.org) to process the (often huge) ICS file, unroll repeating events and perform some restructuring so that the result can be imported correctly in Google Calendar.

The ICAL grammar is taken from [this Github repository](https://github.com/bkiers/ICalParser).

## What it does
So far, it seems the following actions make the file possible to import into Google Calendar:
* Remove all calendar properties.
* Remove all VEVENT properties except DTSTART, DTEND, DESCRIPTION, SUMMARY and LOCATION.
* If a calendar entry contains a RDATE property, "unroll" the repetitions into separate VEVENT entries.
* Sort the events in ascending order according to start date.
