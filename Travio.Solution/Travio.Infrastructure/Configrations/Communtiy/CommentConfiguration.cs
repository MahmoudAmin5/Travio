using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Travio.Core.Domain.Entities.Community;

namespace Travio.Infrastructure.Configrations.Communtiy
{
    public class CommentConfiguration : IEntityTypeConfiguration<Comment>
    {
        public void Configure(EntityTypeBuilder<Comment> builder)
        {
            builder.ToTable("Comments","Community");
            builder.HasKey(e => e.Id);
            builder.Property(x => x.Content)
                .IsRequired();

            builder.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.HasOne(x => x.Post)
                .WithMany(p=>p.Comments)
                .HasForeignKey(x=>x.PostId)
                .OnDelete(DeleteBehavior.Cascade);

        }
    }
}
