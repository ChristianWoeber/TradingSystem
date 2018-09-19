using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;

namespace HelperLibrary.Util.MailHelper
{
    public static class MailHelper
    {
        public static void SendMail(string body, string subject, params string[] to)
        {
            using (var mail = new MailMessage(Settings.Default.From, "cw@arts.co.at"))
            {
                using (var client = new SmtpClient("mail.gmx.net", 587))
                {
                    client.Credentials = new NetworkCredential(Settings.Default.From, "Klampfn#1");
                    client.EnableSsl = true;
                    var recipients = "";
                    if (to.Length > 1)
                    {
                        foreach (var adress in to)
                        {
                            recipients += adress + ",";
                        }
                    }
                    else if (to.Length == 1)
                    {
                        recipients = to[0];
                    }
                    else
                        recipients = "cw@arts.co.at";

                    client.Send(Settings.Default.From, recipients.TrimEnd(','), subject, body);
                }
            }
        }
    }
}
