using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Travio.Core.Helpers
{
    public static class OTPEmailGenerator
    {
        public static string GenerateEmailBody(string userName, string otpCode)
        {
            // You can change these colors to match your App's colors
            string primaryColor = "#007BFF"; // Travio Blue
            string backgroundColor = "#f4f4f4";

            return $@"
    <!DOCTYPE html>
<html lang=""en"">

<head>
    <meta charset=""UTF-8"" />
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"" />
    <title>Travio Verification Code</title>
</head>

<body style=""margin:0; padding:0; background:#f2f2f7; font-family: Arial, Helvetica, sans-serif;"">

    <div style=""
        max-width: 600px;
        margin: 30px auto;
        background: #ffffff;
        border: 1px solid #e5e5e5;
        border-radius: 10px;
        overflow: hidden;
    "">

        <!-- Header -->
        <div style=""
            background: #4A6CF7;
            padding: 25px 0;
            text-align: center;
        "">
            <h1 style=""
                margin: 0;
                font-size: 28px;
                font-weight: bold;
                color: #ffffff;
                letter-spacing: 1px;
                text-transform: uppercase;
            "">
                Travio
            </h1>
        </div>

        <!-- Body -->
        <div style=""padding: 30px;"">
            <p style=""font-size: 16px; color: #333; line-height: 1.6;"">
                Hello,
            </p>

            <p style=""font-size: 16px; color: #333; line-height: 1.6;"">
                Here is your <strong>6-digit verification code</strong> to reset your password:
            </p>

            <!-- Code Box -->
            <div style=""
                margin: 25px 0;
                font-size: 32px;
                font-weight: bold;
                text-align: center;
                color: #4A6CF7;
                letter-spacing: 8px;
            "">
                {otpCode}
            </div>

            <p style=""font-size: 15px; color: #555; line-height: 1.6;"">
                This code will expire in <strong>10 minutes</strong>.  
                If you did not request this, you can safely ignore this email.
            </p>

            <p style=""font-size: 15px; color: #555; margin-top: 30px; line-height: 1.6;"">
                Best regards,<br />
                <strong>Travio Team</strong>
            </p>
        </div>

        <!-- Footer -->
        <div style=""
                background: #f5f5f5;
                padding: 20px;
                text-align: center;
                font-size: 13px;
                color: #777;
            "">
            © 2025 Travio. All rights reserved.
        </div>

    </div>

</body>

</html>
";
        }
    }
}
