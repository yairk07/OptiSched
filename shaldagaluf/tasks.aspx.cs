using System;
using System.Data;
using System.Web.UI.WebControls;

public partial class tasks : System.Web.UI.Page
{
    calnderservice calnderService = new calnderservice();
    private DataSet allEvents;

    protected void Page_Load(object sender, EventArgs e)
    {
        if (!IsPostBack)
        {
            allEvents = calnderService.GetAllEvents();
            ViewState["AllEvents"] = allEvents;

            calendar.SelectedDate = DateTime.Today;
            lblSelectedDate.Text = "בחר תאריך להוסיף לו אירוע";
            ShowEvents(calendar.SelectedDate);
        }
        else
        {
            allEvents = (DataSet)ViewState["AllEvents"];
        }
    }

    protected void calendar_SelectionChanged(object sender, EventArgs e)
    {
        DateTime selectedDate = calendar.SelectedDate;
        lblSelectedDate.Text = $"תאריך נבחר: {selectedDate.ToShortDateString()}";
        ShowEvents(selectedDate);
    }

    protected void AddEvent(object sender, EventArgs e)
    {
        DateTime selectedDate = calendar.SelectedDate.Date;
        string title = txtTitle.Text.Trim();
        string time = txtTime.Text.Trim();
        string note = txtNote.Text.Trim();

        if (!string.IsNullOrEmpty(title))
        {
            calnderService.InsertEvent(title, selectedDate, time, note);

            txtTitle.Text = "";
            txtTime.Text = "";
            txtNote.Text = "";

            // רענון מידע
            allEvents = calnderService.GetAllEvents();
            ViewState["AllEvents"] = allEvents;

            ShowEvents(selectedDate);
        }
    }

    private void ShowEvents(DateTime date)
    {
        lblEvents.Text = "";
        int count = 0;

        foreach (DataRow row in allEvents.Tables[0].Rows)
        {
            if (row.IsNull("date"))
                continue;

            DateTime eventDate;
            if (!DateTime.TryParse(row["date"].ToString(), out eventDate))
                continue;
            if (eventDate.Date == date.Date)
            {
                string title = row["title"].ToString();
                string time = row["time"].ToString();
                string note = row["notes"].ToString();

                lblEvents.Text += $@"
                    <div style='margin-bottom:10px; padding:10px; border:1px solid #ccc; border-radius:5px; background-color:#f9f9f9;'>
                        <strong style='color:#333;'>📌 {title}</strong><br/>";

                if (!string.IsNullOrEmpty(time))
                    lblEvents.Text += $"<span style='color:#555;'>⏰ בשעה: {time}</span><br/>";

                if (!string.IsNullOrEmpty(note))
                    lblEvents.Text += $"<span style='color:#777;'>📝 {note}</span><br/>";

                lblEvents.Text += "</div>";
                count++;
            }
        }

        if (count == 0)
        {
            lblEvents.Text = "<div style='color:gray;'>אין אירועים לתאריך הזה.</div>";
        }
    }

    protected void calendar_DayRender(object sender, DayRenderEventArgs e)
    {
        DateTime currentDay = e.Day.Date;
        int dayCount = 0;

        foreach (DataRow row in allEvents.Tables[0].Rows)
        {
            if (row.IsNull("date"))
                continue;

            DateTime eventDate;
            if (!DateTime.TryParse(row["date"].ToString(), out eventDate))
                continue;
            if (eventDate.Date == currentDay.Date)
            {
                dayCount++;
            }
        }

        if (dayCount > 0)
        {
            e.Cell.Controls.Add(new Literal
            {
                Text = $"<br /><span style='color:blue; font-size:10px;'>({dayCount} אירועים)</span>"
            });
        }
    }
}
