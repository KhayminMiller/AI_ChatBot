using Chatbot.Server.Models;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace Chatbot.Server.Data
{
    public class FaceDetecContext : DbContext
    {
        public FaceDetecContext(DbContextOptions<FaceDetecContext> options) : base(options) { }

        public DbSet<FaceDetectionDB> FaceDetectionDB { get; set; }
    }
}
