namespace LightWeight.Auth.Application.Commands.LoginOtp.SendOtpCode;

/// <summary>
/// Email with the login code. Written for email clients, not browsers: layout with tables,
/// inline styles, no external images (Gmail blocks them) and no web fonts (system fallbacks).
/// Dark, like the app, with its colors
/// </summary>
public static class OtpEmail
{
    // The code goes in the subject so it can be read in the phone notification
    public static string Subject(string code) => $"{code} es tu código de LightWeight";

    public static string Text(string code, int minutes) =>
        $"""
        LIGHTWEIGHT

        Tu código de acceso: {code}

        Escríbelo en la pantalla de inicio de sesión. Caduca en {minutes} minutos.

        Si no has pedido este código, ignora este correo: nadie puede entrar sin él.
        """;

    public static string Html(string code, int minutes) =>
        $$"""
        <!doctype html>
        <html lang="es">
        <head>
          <meta charset="utf-8">
          <meta name="viewport" content="width=device-width, initial-scale=1">
          <meta name="color-scheme" content="dark">
          <meta name="supported-color-schemes" content="dark">
          <title>Tu código de LightWeight</title>
        </head>
        <body style="margin:0;padding:0;background-color:#0f1412;">
          <!-- Preview text shown by the inbox next to the subject -->
          <div style="display:none;max-height:0;overflow:hidden;opacity:0;">Caduca en {{minutes}} minutos.</div>
          <table role="presentation" width="100%" cellpadding="0" cellspacing="0" border="0" style="background-color:#0f1412;">
            <tr>
              <td align="center" style="padding:32px 16px;">
                <table role="presentation" width="100%" cellpadding="0" cellspacing="0" border="0" style="max-width:480px;">
                  <tr>
                    <td style="padding:0 4px 20px;font-family:'Barlow Condensed','Arial Narrow',Arial,sans-serif;font-size:26px;font-weight:700;letter-spacing:2px;color:#e9efec;">
                      LIGHT<span style="color:#3ddc97;">WEIGHT</span>
                    </td>
                  </tr>
                  <tr>
                    <td style="background-color:#161d1a;border:1px solid #2c3833;border-radius:12px;padding:32px 28px;">
                      <p style="margin:0 0 8px;font-family:'Barlow Condensed','Arial Narrow',Arial,sans-serif;font-size:30px;line-height:1.1;font-weight:700;text-transform:uppercase;color:#e9efec;">
                        Tu código de acceso
                      </p>
                      <p style="margin:0 0 24px;font-family:Figtree,'Segoe UI',Helvetica,Arial,sans-serif;font-size:15px;line-height:1.5;color:#c9d3ce;">
                        Escríbelo en la pantalla de inicio de sesión para entrar.
                      </p>
                      <table role="presentation" width="100%" cellpadding="0" cellspacing="0" border="0">
                        <tr>
                          <td align="center" style="background-color:#0f1412;border:1px solid #3f4e48;border-radius:10px;padding:20px 8px;font-family:'SF Mono',Consolas,'Courier New',monospace;font-size:36px;font-weight:700;letter-spacing:10px;color:#3ddc97;">
                            {{code}}
                          </td>
                        </tr>
                      </table>
                      <p style="margin:20px 0 0;font-family:Figtree,'Segoe UI',Helvetica,Arial,sans-serif;font-size:14px;line-height:1.5;color:#a3b1ab;">
                        Caduca en <strong style="color:#e9efec;">{{minutes}} minutos</strong>.
                      </p>
                    </td>
                  </tr>
                  <tr>
                    <td style="padding:20px 4px 0;font-family:Figtree,'Segoe UI',Helvetica,Arial,sans-serif;font-size:13px;line-height:1.5;color:#8a9993;">
                      Si no has pedido este código, ignora este correo: nadie puede entrar en tu cuenta sin él.
                    </td>
                  </tr>
                </table>
              </td>
            </tr>
          </table>
        </body>
        </html>
        """;
}
