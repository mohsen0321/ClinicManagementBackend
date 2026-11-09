using System.Net;
using System.Net.Mail;

namespace clinicsystembackend.Services
{
    public class EmailService
    {
        private readonly IConfiguration _configuration;

        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task SendOtpEmailAsync(string toEmail, string otpCode, string userName)
        {
            // قراءة إعدادات SMTP من appsettings.json
            var smtpSettings = _configuration.GetSection("SmtpSettings");
            var host = smtpSettings["Host"];
            var port = smtpSettings["Port"];
            var senderEmail = smtpSettings["SenderEmail"];
            var senderPassword = smtpSettings["SenderPassword"];

            // طباعة تنبيه في الكونسول أيضاً
            Console.WriteLine($"[EMAIL] محاولة إرسال الرمز {otpCode} إلى {toEmail}");

            if (string.IsNullOrEmpty(host) || string.IsNullOrEmpty(senderEmail) || string.IsNullOrEmpty(senderPassword))
            {
                Console.WriteLine("[EMAIL ERROR] بيانات الـ SMTP غير مكتملة في appsettings.json");
                return;
            }

            try
            {
                using var client = new SmtpClient(host, int.Parse(port ?? "587"))
                {
                    Credentials = new NetworkCredential(senderEmail, senderPassword),
                    EnableSsl = true,
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    UseDefaultCredentials = false
                };

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(senderEmail, " مركز عيادتي الطبي"),
                    Subject = "رمز إعادة تعيين كلمة المرور - عيادتي",
                    Body = $@"
                        <div style='font-family: Arial, Tahoma, sans-serif; direction: rtl; text-align: right; padding: 25px; background-color: #f8fafc; border-radius: 12px; border: 1px solid #e2e8f0; max-width: 500px; margin: 0 auto;'>
                            <h2 style='color: #0284c7; margin-top: 0;'>مرحباً {userName} 👋</h2>
                            <p style='font-size: 15px; color: #334155; line-height: 1.6;'>
                                لقد تلقينا طلباً لإعادة تعيين كلمة المرور لحسابك في مركز عيادتي الطبي.
                            </p>
                            <p style='font-size: 15px; color: #334155;'>رمز التحقق الخاص بك هو:</p>
                            <div style='background-color: #0284c7; color: #ffffff; font-size: 32px; font-weight: bold; letter-spacing: 6px; padding: 16px; text-align: center; border-radius: 8px; margin: 20px 0;'>
                                {otpCode}
                            </div>
                            <p style='font-size: 13px; color: #64748b;'>
                                ⚠️ هذا الرمز صالح لمدة <strong>15 دقيقة</strong> فقط. إذا لم تقم بطلب هذا الرمز، يمكنك تجاهل هذه الرسالة بأمان.
                            </p>
                            <hr style='border: none; border-top: 1px solid #e2e8f0; margin: 20px 0;' />
 <p style='font-size: 13px; color: #64748b;  text-align: center'>
                                 <strong>تم الإنشاء بواسطة Eng/Mohsen Khaled</strong>
                            </p>
                            <small style='color: #94a3b8; display: block; text-align: center;'>نظام عيادتي لإدارة العيادات وحجز المواعيد</small>
                        </div>",
                    IsBodyHtml = true
                };

                mailMessage.To.Add(toEmail);
                await client.SendMailAsync(mailMessage);
                Console.WriteLine($"[EMAIL SUCCESS] تم إرسال الرسالة بنجاح إلى: {toEmail}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[EMAIL EXCEPTION] فشل الإرسال: {ex.Message}");
            }
        }
    }
}