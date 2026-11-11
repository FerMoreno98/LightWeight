using FluentMigrator;

namespace LightWeight.Auth.Infrastructure.Migrations;

/// <summary>Wrong guesses of each code: after OtpCode.MaxFailedAttempts it can no longer be used</summary>
[Migration(202611111000)]
public class AddFailedAttemptsToOtpCodes : Migration
{
    public override void Up()
    {
        Alter.Table("auth_OtpCodes").InSchema("auth")
            .AddColumn("FailedAttempts").AsInt32().NotNullable().WithDefaultValue(0);
    }

    public override void Down()
    {
        Delete.Column("FailedAttempts").FromTable("auth_OtpCodes").InSchema("auth");
    }
}
