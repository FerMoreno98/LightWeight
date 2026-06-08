using FluentMigrator;

namespace LightWeight.Auth.Infrastructure.Migrations;

[Migration(202606081159)]
public class CreateDeviceTokenTable : Migration
{
    public override void Up()
    {
        Create.Table("auth_DeviceToken")
            .WithColumn("Id").AsGuid().PrimaryKey()
            .WithColumn("UserId").AsGuid().NotNullable().ForeignKey("auth_users", "Id").OnDelete(System.Data.Rule.Cascade)
            .WithColumn("DeviceIdentifier").AsString().NotNullable()
            .WithColumn("DeviceName").AsString(200).NotNullable()
            .WithColumn("Platform").AsString(50).NotNullable()
            .WithColumn("LastSeenAt").AsDateTime().NotNullable()
            .WithColumn("CreatedAt").AsDateTime().NotNullable()
            .WithColumn("RevokedAt").AsDateTime().Nullable();

        // Índice único compuesto UserId + DeviceIdentifier
        // (dos usuarios pueden registrarse desde el mismo dispositivo)
        Create.Index("IX_DeviceToken_UserId_DeviceIdentifier")
            .OnTable("auth_DeviceToken")
            .OnColumn("UserId").Ascending()
            .OnColumn("DeviceIdentifier").Ascending()
            .WithOptions().Unique();
    }

    public override void Down()
    {
        Delete.Table("auth_DeviceToken");
    }
}