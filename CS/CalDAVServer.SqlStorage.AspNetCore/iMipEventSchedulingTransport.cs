using System;
using System.Net.Mail;
using System.Net.Mime;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using ITHit.Collab;
using ITHit.Collab.Calendar;

namespace CalDAVServer.SqlStorage.AspNetCore
{
    /// <summary>
    /// Sends e-mails to event/to-do attendies using SMTP.
    /// </summary>
    /// <remarks>This class uses settings from system.net/mailSettings in web.config/app.config.</remarks>
    public class iMipEventSchedulingTransport
    {
        private const string ContentTypePattern = "text/calendar; method={0}; charset=UTF-8";

        /// <summary>
        /// Sends an e-mail with event/to-do attachment to every attendee found in <see cref="ICalendar2.Attendees"/> list.
        /// </summary>
        /// <param name="calendar">Calendar object with event or to-do. The <see cref="ICalendar2.Method"/> property must be specified in this calendar object.</param>
        public static async Task NotifyAttendeesAsync(DavContext context, ICalendar2 calendar)
        {
            IEnumerable<IEventBase> components = calendar.Events.Cast<IEventBase>();
            if (!components.Any())
            {
                components = calendar.ToDos.Cast<IEventBase>();
            }

            IEventBase component = components.First();

            ICalAddress organizer = component.Organizer;

            string iCalendarContent = new vFormatter().Serialize(calendar);

            foreach (IAttendee attendee in component.Attendees)
            {

                try
                {
                    using (MailMessage mail = new MailMessage())
                    {                        
                        mail.From = GetMailAddress(organizer);
                        mail.To.Add(GetMailAddress(attendee));
                        mail.Subject = string.Format("Event: {0}", component.Summary.Text);
                        using (AlternateView alternateView = AlternateView.CreateAlternateViewFromString(iCalendarContent, Encoding.UTF8, "text/calendar"))
                        {
                            alternateView.TransferEncoding = TransferEncoding.EightBit;

                            // Method must be specified both in iCalendar METHOD property and in Content-Type.
                            alternateView.ContentType = new ContentType(string.Format(ContentTypePattern, calendar.Method.Method.Name));

                            mail.AlternateViews.Add(alternateView);
                            using (SmtpClient smtpClient = new SmtpClient())
                            {
                                smtpClient.EnableSsl = true;                                
                                await smtpClient.SendMailAsync(mail);                                                    
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Probably SMTP server is not configured in web.config/app.config
                    string message = "Faled to notify attendees about event change. SMTP server is not configured in web.config/app.config" + Environment.NewLine;
                    context.Logger.LogError(message, ex);
                }
            }
        }

        /// <summary>
        /// Creates <see cref="MailAddress"/> from <see cref="ICalAddress"/> that contains e-mail.
        /// </summary>
        /// <param name="address"><see cref="ICalAddress"/> that contains e-mail.</param>
        /// <returns>New instance of <see cref="MailAddress"/>.</returns>
        private static MailAddress GetMailAddress(ICalAddress address)
        {
            if(!string.IsNullOrEmpty(address.CommonName))
            {
                return new MailAddress(address.Uri.Replace("mailto:", ""), address.CommonName, Encoding.UTF8);
            }

            return new MailAddress(address.Uri.Replace("mailto:", ""));
        }

    }
}
