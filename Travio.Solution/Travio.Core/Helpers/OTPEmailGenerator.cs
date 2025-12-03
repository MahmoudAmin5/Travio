using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Travio.Core.Helpers
{
    public static class OTPEmailGenerator
    {
        public static string GeneratePasswordResetEmailBody(string userName, string otpCode)
        {
            // You can change these colors to match your App's colors
            string primaryColor = "#007BFF"; // Travio Blue
            string backgroundColor = "#f4f4f4";

            return $@"
    <!DOCTYPE html>
    <html>
    <head>
        <meta charset='UTF-8'>
        <meta name='viewport' content='width=device-width, initial-scale=1.0'>
        <style>
            body {{ font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; background-color: {backgroundColor}; margin: 0; padding: 0; }}
            .container {{ max-width: 600px; margin: 20px auto; background: #ffffff; border-radius: 8px; box-shadow: 0 4px 8px rgba(0,0,0,0.1); overflow: hidden; }}
            .header {{ background-color: {primaryColor}; padding: 20px; text-align: center; color: #ffffff; }}
            .header h1 {{ margin: 0; font-size: 24px; }}
            .content {{ padding: 30px; color: #333333; line-height: 1.6; }}
            .otp-box {{ background-color: #eef6ff; border: 2px dashed {primaryColor}; color: {primaryColor}; font-size: 32px; font-weight: bold; text-align: center; padding: 15px; margin: 20px 0; border-radius: 5px; letter-spacing: 5px; }}
            .footer {{ background-color: #eeeeee; padding: 15px; text-align: center; font-size: 12px; color: #666666; }}
            .link {{ color: {primaryColor}; text-decoration: none; }}
        </style>
    </head>
    <body>
        <div class='container'>
            <div class='header'>
                <h1>Travio</h1>
            </div>
            <div class='content'>
                <h2>Password Reset Request</h2>
                <p>Hi <strong>{userName}</strong>,</p>
                <p>We received a request to reset your password for your Travio account. Please use the verification code below to complete the process:</p>
                
                <div class='otp-box'>
                    {otpCode}
                </div>

                <p>This code is valid for <strong>15 minutes</strong>.</p>
                <p>If you did not request a password reset, please ignore this email or contact support if you have questions.</p>
                <p>Safe travels,<br>The Travio Team</p>
            </div>
            <div class='footer'>
                <p>&copy; {DateTime.UtcNow.Year} Travio Inc. All rights reserved.</p>
                <p>This is an automated message, please do not reply.</p>
            </div>
        </div>
    </body>
    </html>";
        }
    }
}
