using FluentMigrator;
[Migration(202606101151)]
public class CreateOtpCodesTable : Migration
{
    public override void Up()
    {
        Create.Table("auth_OtpCodes").InSchema("auth")
            .WithColumn("Id").AsGuid().PrimaryKey()
            .WithColumn("UserId").AsGuid().NotNullable()
                .ForeignKey("Fk_OtpCode_Users","auth","auth_Users", "Id").OnDelete(System.Data.Rule.Cascade)
            .WithColumn("Hash").AsString(255).NotNullable()
            .WithColumn("CreatedAt").AsDateTime().NotNullable()
            .WithColumn("ExpiresAt").AsDateTime().NotNullable()
            .WithColumn("UsedAt").AsDateTime().Nullable();
 
        Create.Index("IX_OtpCodes_UserId")
            .OnTable("auth_OtpCodes").InSchema("auth")
            .OnColumn("UserId").Ascending();
    }
 
    public override void Down()
    {
        Delete.Table("auth_OtpCodes");
    }
}
