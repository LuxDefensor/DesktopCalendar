using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Globalization;
using System.IO;

namespace DesktopCalendar
{
    class WrongArgsException : ApplicationException
    {
        public WrongArgsException():base("Use this syntax:" + Environment.NewLine + "desktopcalendar back_color fore_color weekend_color")
        {            
        }
    }

    class Program
    {
        #region Base sizes
        private static int baseWidth = 1280;
        private static int baseHeight = 1024;
        private static int monthHeight = 300;
        private static int monthTitleWidth = 50;
        private static int dayTitleHeight = 60;
        private static int dayWidth = 50;        
        private static int calendarLeft = 700;
        private static int calendarTop = 50;
        #endregion

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern Int32 SystemParametersInfo(UInt32 uiAction, UInt32 uiParam, String pvParam, UInt32 fWinIni);
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern Int32 GetSystemMetrics(int nIndex);
        private static UInt32 SPI_SETDESKWALLPAPER = 20;
        private static UInt32 SPIF_UPDATEINIFILE = 0x1;
        private static Int32 SM_CXSCREEN = 0;
        private static Int32 SM_CYSCREEN = 1;

        private static string fileName = "DesktopCalendar.bmp";
        private static Brush background = Brushes.DarkSlateGray;
        private static Brush weekend = Brushes.Orange;
        private static Brush foreground = Brushes.WhiteSmoke;
        private static Pen currentFrame = new Pen(Color.Orange);
        private static CultureInfo culture = CultureInfo.CurrentCulture;


        private static void SetImage(string filename)
        {
            SystemParametersInfo(SPI_SETDESKWALLPAPER, 0, filename, SPIF_UPDATEINIFILE);
        }

        static void Main(string[] args)
        {
            //culture = CultureInfo.GetCultureInfo("en-US");
            string folder = AppDomain.CurrentDomain.BaseDirectory;
            try
            {
            if (!Directory.Exists(folder))
                throw new WrongArgsException();
                    int width, height;
                width = GetSystemMetrics(SM_CXSCREEN);
                height = GetSystemMetrics(SM_CYSCREEN);
                if (args.Length == 3)
                {
                    int colorBack, colorFront, colorWeekday;
                    if (int.TryParse(args[0], out colorBack))
                        background = new SolidBrush(Color.FromArgb(colorBack));
                    else
                        background = new SolidBrush(Color.FromName(args[0]));
                    if (int.TryParse(args[1], out colorFront))
                        foreground = new SolidBrush(Color.FromArgb(colorFront));
                    else
                        foreground = new SolidBrush(Color.FromName(args[1]));
                    if (int.TryParse(args[2], out colorWeekday))
                    {
                        weekend = new SolidBrush(Color.FromArgb(colorWeekday));
                        currentFrame = new Pen(Color.FromArgb(colorWeekday));
                    }
                    else
                    {
                        weekend = new SolidBrush(Color.FromName(args[2]));
                        currentFrame = new Pen(Color.FromName(args[2]));
                    }
                }
                else if (args.Length > 0)
                {
                    throw new WrongArgsException();
                }
                    CreateImage(width, height, folder);
                    SetImage(Path.Combine(folder, fileName));
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return;
            }
        }

        private static void CreateImage(int width, int height, string folderName)
        {
            using (Bitmap bmp = new Bitmap(width, height))
            {
                using (Graphics c = Graphics.FromImage(bmp))
                {
                    c.FillRectangle(background, c.ClipBounds);
                    DrawMonth(c, DateTime.Now.AddMonths(-1).Date, 1, width, height);
                    DrawMonth(c, DateTime.Now.Date, 2, width, height);
                    DrawMonth(c, DateTime.Now.AddMonths(1).Date, 3, width, height);
                    bmp.Save(Path.Combine(folderName, fileName));
                }
            }
        }

        private static void DrawMonth(Graphics c, DateTime monthDay, int order,int width,int height)
        {
            float scaleWidth = (float)width / baseWidth;
            float scaleHeight = (float)height / baseHeight;
            Brush brush = foreground;
            StringFormat format = new StringFormat();
            
            // Month's title    
            format.Alignment = StringAlignment.Center;
            format.FormatFlags = StringFormatFlags.DirectionVertical;
            RectangleF monthTitleFrame = new RectangleF(calendarLeft * scaleWidth + dayWidth * 7.5f * scaleWidth,
                    calendarTop * scaleHeight + (order - 1) * monthHeight * scaleHeight,
                    monthTitleWidth * scaleWidth, monthHeight * scaleHeight);
            c.DrawString(monthDay.ToString("MMMM yyyy", culture.DateTimeFormat),
                    new Font("Arial", 16), brush,
                    monthTitleFrame,
                    format);

            // Days' titles
            format = new StringFormat();
            format.Alignment = StringAlignment.Center;
            int fdw = (int)culture.DateTimeFormat.FirstDayOfWeek;
            int idx = fdw;
            for (int i = 0; i < 7; i++)
            {
                brush = (idx == 0 || idx == 6) ? weekend : foreground;
                c.DrawString(culture.DateTimeFormat.ShortestDayNames[idx],
                    new Font("Arial", 14), brush,
                    new RectangleF(calendarLeft * scaleWidth + i * dayWidth * scaleWidth,
                        calendarTop * scaleHeight + (order - 1) * monthHeight * scaleHeight,
                        dayWidth * scaleWidth, dayTitleHeight * scaleHeight),
                    format);
                idx++;
                if (idx > 6)
                    idx = 0;
            }

            // Month's days
            int x = 0, y = 1;
            int weeks = NumberOfWeeksInMonth(monthDay);
            float weekHeight = (monthHeight - dayTitleHeight-20) / weeks;
            DateTime current = new DateTime(monthDay.Year, monthDay.Month, 1);
            DateTime end = current.AddMonths(1);
            RectangleF dayFrame;
            while (current < end)
            {
                x = (int)current.DayOfWeek - (int)culture.DateTimeFormat.FirstDayOfWeek;
                if (x <0)
                    x = 6;
                brush = (current.DayOfWeek == DayOfWeek.Saturday || current.DayOfWeek == DayOfWeek.Sunday)
                         ? weekend : foreground;
                dayFrame = new RectangleF(calendarLeft * scaleWidth + x * dayWidth * scaleWidth,
                        calendarTop * scaleHeight + (order - 1) * monthHeight * scaleHeight + dayTitleHeight * scaleHeight + (y - 1) * weekHeight * scaleHeight,
                        dayWidth * scaleWidth, weekHeight * scaleHeight);
                c.DrawString(current.Day.ToString(),
                    new Font("Arial", 14), brush,
                    dayFrame,
                    format);
                if (current == DateTime.Now.Date)
                {
                    c.DrawRectangle(currentFrame, dayFrame.Left,
                                                  dayFrame.Top - dayFrame.Height / 4,
                                                  dayFrame.Width, dayFrame.Height);
                }
                current = current.AddDays(1);
                if (x == 6)
                    y++;
            }

            // Month's frame
            c.DrawRectangle(currentFrame,
                            calendarLeft * scaleWidth,
                            (calendarTop + (order - 1) * monthHeight - dayTitleHeight / 2) * scaleHeight,
                            (dayWidth * 7.5f + monthTitleWidth) * scaleWidth,
                            monthHeight * scaleHeight);
        }

        private static int NumberOfWeeksInMonth(DateTime monthDay)
        {
            Calendar cal = culture.Calendar;
            DateTime d1 = new DateTime(monthDay.Year, monthDay.Month, 1);
            DateTime d2 = d1.AddMonths(1).AddDays(-1);
            int firstWeek = cal.GetWeekOfYear(d1,
                CalendarWeekRule.FirstDay,
                culture.DateTimeFormat.FirstDayOfWeek);
            int lastWeek= cal.GetWeekOfYear(d2,
                CalendarWeekRule.FirstDay,
                culture.DateTimeFormat.FirstDayOfWeek);
            return lastWeek - firstWeek + 1;
        }
    }
}
