using FluentMigrator;

namespace LightWeight.DeviceIntegration.Infrastructure.Migrations;

// Every module shares the same FluentMigrator runner and VersionInfo table, so the versions of
// this module (2026111920xx) must not collide with the ones of the others
[Migration(202611192000)]
public class CreateDeviceIntegrationSchema : Migration
{
    public override void Up()
    {
        Execute.Sql("CREATE SCHEMA IF NOT EXISTS deviceintegration;");
    }

    public override void Down()
    {
        Execute.Sql("DROP SCHEMA IF EXISTS deviceintegration CASCADE;");
    }
}

[Migration(202611192001)]
public class CreateFatSecretConnectionsTable : Migration
{
    public override void Up()
    {
        // Tokens and secrets are encrypted (base64 of AES-GCM): text, not a fixed length
        Create.Table("deviceintegration_FatSecretConnections").InSchema("deviceintegration")
            .WithColumn("Id").AsGuid().PrimaryKey()
            .WithColumn("UserId").AsGuid().NotNullable()
            .WithColumn("AccessToken").AsCustom("text").Nullable()
            .WithColumn("AccessTokenSecret").AsCustom("text").Nullable()
            .WithColumn("LinkedAt").AsCustom("timestamp with time zone").Nullable()
            .WithColumn("RevokedAt").AsCustom("timestamp with time zone").Nullable()
            .WithColumn("HistoryImportedAt").AsCustom("timestamp with time zone").Nullable()
            .WithColumn("RequestToken").AsCustom("text").Nullable()
            .WithColumn("RequestTokenSecret").AsCustom("text").Nullable()
            .WithColumn("RequestedAt").AsCustom("timestamp with time zone").Nullable();

        Create.Index("Ux_FatSecretConnections_UserId").OnTable("deviceintegration_FatSecretConnections").InSchema("deviceintegration")
            .OnColumn("UserId").Ascending()
            .WithOptions().Unique();
    }

    public override void Down()
    {
        Delete.Table("deviceintegration_FatSecretConnections").InSchema("deviceintegration");
    }
}
