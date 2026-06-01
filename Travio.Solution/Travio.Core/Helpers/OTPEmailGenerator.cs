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
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Password Reset - Travlo</title>
    <!--[if mso]>
    <noscript>
        <xml>
            <o:OfficeDocumentSettings>
                <o:PixelsPerInch>96</o:PixelsPerInch>
            </o:OfficeDocumentSettings>
        </xml>
    </noscript>
    <![endif]-->
</head>
<body style=""margin: 0; padding: 0; font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif; background-color: #ffffff; color: #333333;"">
    
    <!-- Preheader Text (Hidden Preview) -->
    <div style=""display: none; max-height: 0; overflow: hidden; mso-hide: all;"">
        Your password reset code is ready. This code expires in 15 minutes.
    </div>
    
    <!-- Main Container -->
    <table role=""presentation"" cellspacing=""0"" cellpadding=""0"" border=""0"" width=""100%"" style=""background-color: #ffffff;"">
        <tr>
            <td align=""center"" style=""padding: 0;"">
                <table role=""presentation"" cellspacing=""0"" cellpadding=""0"" border=""0"" width=""100%"" style=""max-width: 480px;"">
                    
                    <!-- Logo Section -->
                    <tr>
                        <td align=""center"" style=""padding: 48px 24px 32px 24px;"">
                            <img src=""https://res.cloudinary.com/dn8tsma3t/image/upload/v1773413577/travlo22_b5j7mo.png"" alt=""Travlo"" width=""150"" style=""display: block; width: 150px; max-width: 100%; height: auto;"">
                        </td>
                    </tr>
                    
                    <!-- Main Content -->
                    <tr>
                        <td style=""padding: 0 32px;"">
                            <h1 style=""margin: 0 0 16px 0; font-size: 24px; font-weight: 600; color: #1a1a1a; text-align: center; line-height: 1.3;"">
                                Reset Your Password
                            </h1>
                            
                            <p style=""margin: 0 0 32px 0; font-size: 16px; color: #666666; text-align: center; line-height: 1.5;"">
                                Hello {userName},<br>
                                Use this verification code to reset your password:
                            </p>
                        </td>
                    </tr>
                    
                    <!-- OTP Code Section - Simplified -->
                    <tr>
                        <td align=""center"" style=""padding: 0 32px 32px 32px;"">
                            <table role=""presentation"" cellspacing=""0"" cellpadding=""0"" border=""0"" style=""background-color: #f7f9fc; border-radius: 12px; width: 100%;"">
                                <tr>
                                    <td align=""center"" style=""padding: 32px;"">
                                        <div style=""font-family: 'Courier New', monospace; font-size: 36px; font-weight: bold; color: #1a7c7e; letter-spacing: 12px; line-height: 1;"">
                                            {otpCode} 
                                        </div>
                                        <p style=""margin: 16px 0 0 0; font-size: 13px; color: #999999;"">
                                            Valid for 15 minutes
                                        </p>
                                    </td>
                                </tr>
                            </table>
                        </td>
                    </tr>
                    
                    <!-- Instructions - Clean List -->
                    <tr>
                        <td style=""padding: 0 32px 32px 32px;"">
                            <table role=""presentation"" cellspacing=""0"" cellpadding=""0"" border=""0"" width=""100%"" style=""border-top: 1px solid #e5e7eb; padding-top: 24px;"">
                                <tr>
                                    <td>
                                        <p style=""margin: 0 0 16px 0; font-size: 14px; color: #999999; font-weight: 600; text-transform: uppercase; letter-spacing: 0.5px;"">
                                            How it works
                                        </p>
                                        <table role=""presentation"" cellspacing=""0"" cellpadding=""0"" border=""0"">
                                            <tr>
                                                <td style=""vertical-align: top; padding-right: 12px;"">
                                                    <span style=""display: inline-block; width: 24px; height: 24px; background-color: #f0fdf4; color: #16a34a; border-radius: 50%; text-align: center; line-height: 24px; font-size: 12px; font-weight: 600;"">1</span>
                                                </td>
                                                <td style=""padding-bottom: 12px;"">
                                                    <p style=""margin: 0; font-size: 14px; color: #666666; line-height: 1.5;"">
                                                        Click the button above or go to reset page
                                                    </p>
                                                </td>
                                            </tr>
                                            <tr>
                                                <td style=""vertical-align: top; padding-right: 12px;"">
                                                    <span style=""display: inline-block; width: 24px; height: 24px; background-color: #f0fdf4; color: #16a34a; border-radius: 50%; text-align: center; line-height: 24px; font-size: 12px; font-weight: 600;"">2</span>
                                                </td>
                                                <td style=""padding-bottom: 12px;"">
                                                    <p style=""margin: 0; font-size: 14px; color: #666666; line-height: 1.5;"">
                                                        Enter this 6-digit code
                                                    </p>
                                                </td>
                                            </tr>
                                            <tr>
                                                <td style=""vertical-align: top; padding-right: 12px;"">
                                                    <span style=""display: inline-block; width: 24px; height: 24px; background-color: #f0fdf4; color: #16a34a; border-radius: 50%; text-align: center; line-height: 24px; font-size: 12px; font-weight: 600;"">3</span>
                                                </td>
                                                <td>
                                                    <p style=""margin: 0; font-size: 14px; color: #666666; line-height: 1.5;"">
                                                        Create your new password
                                                    </p>
                                                </td>
                                            </tr>
                                        </table>
                                    </td>
                                </tr>
                            </table>
                        </td>
                    </tr>
                    
                    <!-- Security Notice - Minimal -->
                    <tr>
                        <td style=""padding: 0 32px 32px 32px;"">
                            <table role=""presentation"" cellspacing=""0"" cellpadding=""0"" border=""0"" width=""100%"" style=""background-color: #fef2f2; border-radius: 8px;"">
                                <tr>
                                    <td style=""padding: 16px;"">
                                        <p style=""margin: 0; font-size: 13px; color: #991b1b; line-height: 1.5;"">
                                            <strong>⚠️ Security tip:</strong> Never share this code with anyone. Travlo staff will never ask for it.
                                        </p>
                                    </td>
                                </tr>
                            </table>
                        </td>
                    </tr>
                    
                    <!-- Footer -->
                    <tr>
                        <td style=""padding: 32px;"">
                            <table role=""presentation"" cellspacing=""0"" cellpadding=""0"" border=""0"" width=""100%"" style=""border-top: 1px solid #e5e7eb;"">
                                <tr>
                                    <td align=""center"" style=""padding-top: 24px;"">
                                        <p style=""margin: 0 0 8px 0; font-size: 13px; color: #999999; line-height: 1.5;"">
                                            Didn't request this? You can safely ignore this email.
                                        </p>
                                        <p style=""margin: 0 0 16px 0; font-size: 13px; color: #999999; line-height: 1.5;"">
                                            Need help? Contact us at <a href=""mailto:support@travlo.com"" style=""color: #1a7c7e; text-decoration: none;"">support@travlo.com</a>
                                        </p>
                                        
                                        <!-- Social Links -->
                                        <table role=""presentation"" cellspacing=""0"" cellpadding=""0"" border=""0"">
                                            <tr>
                                                <td style=""padding: 0 6px;"">
                                                    <a href=""#"" style=""display: inline-block; width: 32px; height: 32px; background-color: #f3f4f6; border-radius: 50%; text-align: center; line-height: 32px; text-decoration: none;"">
                                                        <span style=""font-size: 16px;"">📘</span>
                                                    </a>
                                                </td>
                                                <td style=""padding: 0 6px;"">
                                                    <a href=""#"" style=""display: inline-block; width: 32px; height: 32px; background-color: #f3f4f6; border-radius: 50%; text-align: center; line-height: 32px; text-decoration: none;"">
                                                        <span style=""font-size: 16px;"">🐦</span>
                                                    </a>
                                                </td>
                                                <td style=""padding: 0 6px;"">
                                                    <a href=""#"" style=""display: inline-block; width: 32px; height: 32px; background-color: #f3f4f6; border-radius: 50%; text-align: center; line-height: 32px; text-decoration: none;"">
                                                        <span style=""font-size: 16px;"">📷</span>
                                                    </a>
                                                </td>
                                            </tr>
                                        </table>
                                        
                                        <p style=""margin: 16px 0 0 0; font-size: 11px; color: #cccccc;"">
                                            © 2024 Travlo · Your journey begins here ✈️
                                        </p>
                                    </td>
                                </tr>
                            </table>
                        </td>
                    </tr>
                    
                </table>
            </td>
        </tr>
    </table>
    
</body>
</html>
";
        }
    }
}
