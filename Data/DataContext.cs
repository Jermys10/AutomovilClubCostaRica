namespace AutomovilClub.Backend.Data
{
    using AutomovilClub.Backend.Data.Entities;
    using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore;    
    using System;
    using System.Collections.Generic;
    using System.Reflection.Emit;

    public class DataContext : IdentityDbContext<User>
    {
        public DataContext(DbContextOptions<DataContext> options) : base(options)
        {
        }

        public DbSet<Country> Countries { get; set; }

        public DbSet<RequestLicenceSport> RequestLicenceSports { get; set; }

        public DbSet<RequestLicenceConcursanteSport> RequestLicenceConcursanteSports { get; set; }

        public DbSet<RequestLicenceSportInternational> RequestLicenceSportInternational { get; set; } = default!;

        public DbSet<RequestAssociateMembership> RequestAssociateMemberships { get; set; }

        public DbSet<RequestVirtualSportsOfficialLicenses> RequestVirtualSportsOfficialLicenses { get; set; }

        public DbSet<Configuration> Configurations { get; set; }

        public DbSet<Person> People { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            //modelBuilder
            //    .Entity<CreditInterest>()
            //    .HasOne(e => e.Interest)
            //    .WithMany(e => e.CreditInterests)
            //    .OnDelete(DeleteBehavior.ClientCascade);


            modelBuilder
            .Entity<RequestLicenceSportInternational>()
            .HasOne(e => e.DomicileCountry)
            .WithMany(e => e.DomicileCountries)
            .HasForeignKey(mu => mu.ResidenceCountryId)
            .OnDelete(DeleteBehavior.NoAction);

            modelBuilder
           .Entity<RequestLicenceSportInternational>()
           .HasOne(e => e.ResidenceCountry)
           .WithMany(e => e.ResidenceCountries)
           .HasForeignKey(mu => mu.ResidenceCountryId)
           .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<RequestAssociateMembership>()
              .HasOne(r => r.User)
              .WithMany()
              .HasForeignKey(r => r.UserId)
              .OnDelete(DeleteBehavior.Cascade);

            base.OnModelCreating(modelBuilder);
        }

    }
}
