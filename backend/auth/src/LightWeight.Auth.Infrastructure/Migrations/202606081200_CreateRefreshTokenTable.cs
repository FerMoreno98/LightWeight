using FluentMigrator;

namespace LightWeight.Auth.Infrastructure.Migrations;

[Migration(202606081200)]
public class CreateRefreshTokensTable : Migration
{
    public override void Up()
    {
        Create.Table("auth_RefreshTokens")
            .WithColumn("Id").AsGuid().PrimaryKey()
            .WithColumn("DeviceTokenId").AsGuid().NotNullable().ForeignKey("auth_DeviceToken", "Id").OnDelete(System.Data.Rule.Cascade)
            .WithColumn("Token").AsString(88).NotNullable()
            .WithColumn("ExpiresAt").AsDateTime().NotNullable()
            .WithColumn("RevokedAt").AsDateTime().Nullable()
            .WithColumn("ReplacedTokenBy").AsString(88).Nullable()
            .WithColumn("CreatedByIp").AsString(45).NotNullable();

        Create.Index("IX_RefreshTokens_Token")
            .OnTable("auth_RefreshTokens")
            .OnColumn("Token").Ascending()
            .WithOptions().Unique();
    }

    public override void Down()
    {
        Delete.Table("auth_RefreshTokens");
    }
}