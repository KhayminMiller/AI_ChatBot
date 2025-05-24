using Chatbot.Server.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Chatbot.Server.Data
{
    public class VisionDbContext : DbContext
    {
        public VisionDbContext(DbContextOptions<VisionDbContext> options) : base(options) { }

        public DbSet<FaceDetectionImage> FaceDetectionImages { get; set; }
        public DbSet<DetectedFace> DetectedFaces { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<FaceDetectionImage>()
                .HasMany(f => f.Faces)
                .WithOne(f => f.FaceDetectionImage)
                .HasForeignKey(f => f.FaceDetectionImageId);
        }
    }

}
