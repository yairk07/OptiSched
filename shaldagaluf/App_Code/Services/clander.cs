using System;

public class Calendar
{
    public int id { get; set; }
    public string title { get; set; }
    public DateTime date { get; set; }
    public string time { get; set; }
    public string notes { get; set; }
    public string category { get; set; }
    public Calendar()
    {
        id = 0;
        title = "";
        date = DateTime.Today;
        time = "00:00";
        notes = "";
        category = "אחר";
    }

    public Calendar(string title, DateTime date, string time, string notes)
    {
        id = 0;
        title = title;
        date = date;
        time = time;
        notes = notes;
        category = "אחר";
    }

    public void InsertIntoDb(int? userId = null)
    {
        CalendarService cs = new CalendarService();

        cs.InsertEvent(this.title, this.date, this.time, this.notes, this.category, userId);
    }
}
