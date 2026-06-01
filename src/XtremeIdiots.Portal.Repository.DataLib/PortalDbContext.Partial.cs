using Microsoft.EntityFrameworkCore;

namespace XtremeIdiots.Portal.Repository.DataLib;

public partial class PortalDbContext
{
    partial void OnModelCreatingPartial(ModelBuilder modelBuilder)
    {
        // Filtered unique indexes allow one active code per profile, but historical rows are retained.
        // The reverse-engineered one-to-one relationship cannot represent that cardinality correctly.
        modelBuilder.Entity<UserProfile>().Ignore(u => u.ConnectedPlayerActivationCode);
        modelBuilder.Entity<Player>().Ignore(player => player.ConnectedPlayerRegistrationToken);

        modelBuilder.Entity<ConnectedPlayerActivationCode>()
            .HasOne(code => code.UserProfile)
            .WithMany()
            .HasForeignKey(code => code.UserProfileId)
            .OnDelete(DeleteBehavior.ClientSetNull)
            .HasConstraintName("FK_dbo.ConnectedPlayerActivationCodes_dbo.UserProfiles_UserProfileId");

        modelBuilder.Entity<ConnectedPlayerRegistrationToken>()
            .HasOne(token => token.Player)
            .WithMany()
            .HasForeignKey(token => token.PlayerId)
            .OnDelete(DeleteBehavior.ClientSetNull)
            .HasConstraintName("FK_dbo.ConnectedPlayerRegistrationTokens_dbo.Players_PlayerId");
    }
}