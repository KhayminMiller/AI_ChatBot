using Chatbot.Server.Models;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace ChatBot.Server.Data
{
    public class ORCContext : DbContext
    {
        public ORCContext(DbContextOptions<ORCContext> options) : base(options) { }

        public DbSet<ORCdB> ORCdB { get; set; }

    }
}