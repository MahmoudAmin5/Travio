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
    public class PostImagesConfigurations : IEntityTypeConfiguration<PostImage>
    {
        public void Configure(EntityTypeBuilder<PostImage> builder)
        {
            builder.ToTable("PostImages", "Community");
            builder.HasKey(x => x.Id);
            builder.Property(i => i.ImageUrl)
                .IsRequired()
                .HasMaxLength(700);
            builder.HasOne(x => x.Post)
                .WithMany(p => p.Images)
                .HasForeignKey(x => x.PostId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
