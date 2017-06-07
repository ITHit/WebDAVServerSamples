Imports System
Imports System.Net.Mail
Imports System.Net.Mime
Imports System.Text
Imports System.Collections.Generic
Imports System.Linq
Imports System.Threading.Tasks
Imports ITHit.Collab
Imports ITHit.Collab.Calendar

''' <summary>
''' Sends e-mails to event/to-do attendies using SMTP.
''' </summary>
''' <remarks>This class uses settings from system.net/mailSettings in web.config/app.config.</remarks>
Public Class iMipEventSchedulingTransport

    Private Const ContentTypePattern As String = "text/calendar; method={0}; charset=UTF-8"

    Public Shared Async Function NotifyAttendeesAsync(context As DavContext, calendar As ICalendar2) As Task
        Dim components As IEnumerable(Of IEventBase) = calendar.Events.Cast(Of IEventBase)()
        If Not components.Any() Then
            components = calendar.ToDos.Cast(Of IEventBase)()
        End If

        Dim component As IEventBase = components.First()
        Dim organizer As ICalAddress = component.Organizer
        Dim iCalendarContent As String = New vFormatter().Serialize(calendar)
        For Each attendee As IAttendee In component.Attendees
            Try
                Using mail As MailMessage = New MailMessage()
                    mail.From = GetMailAddress(organizer)
                    mail.To.Add(GetMailAddress(attendee))
                    mail.Subject = String.Format("Event: {0}", component.Summary.Text)
                    Using alternateView As AlternateView = AlternateView.CreateAlternateViewFromString(iCalendarContent, Encoding.UTF8, "text/calendar")
                        alternateView.TransferEncoding = TransferEncoding.EightBit
                        ' Method must be specified both in iCalendar METHOD property and in Content-Type.
                        alternateView.ContentType = New ContentType(String.Format(ContentTypePattern, calendar.Method.Method.Name))
                        mail.AlternateViews.Add(alternateView)
                        Using smtpClient As SmtpClient = New SmtpClient()
                            smtpClient.EnableSsl = True
                            Await smtpClient.SendMailAsync(mail)
                        End Using
                    End Using
                End Using
            Catch ex As Exception
                Dim message As String = "Faled to notify attendees about event change. SMTP server is not configured in web.config/app.config" & Environment.NewLine
                context.Logger.LogError(message, ex)
            End Try
        Next
    End Function

    Private Shared Function GetMailAddress(address As ICalAddress) As MailAddress
        If Not String.IsNullOrEmpty(address.CommonName) Then
            Return New MailAddress(address.Uri.Replace("mailto:", ""), address.CommonName, Encoding.UTF8)
        End If

        Return New MailAddress(address.Uri.Replace("mailto:", ""))
    End Function
End Class
