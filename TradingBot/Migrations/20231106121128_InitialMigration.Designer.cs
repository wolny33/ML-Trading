﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using TradingBot.Database;

#nullable disable

namespace TradingBot.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20231106121128_InitialMigration")]
    partial class InitialMigration
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder.HasAnnotation("ProductVersion", "7.0.13");

            modelBuilder.Entity("TradingBot.Database.Entities.TradingActionDetailsEntity", b =>
                {
                    b.Property<Guid>("TradingActionId")
                        .HasColumnType("TEXT");

                    b.HasKey("TradingActionId");

                    b.ToTable("Details");
                });

            modelBuilder.Entity("TradingBot.Database.Entities.TradingActionEntity", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("TEXT");

                    b.Property<int>("InForce")
                        .HasColumnType("INTEGER");

                    b.Property<int>("OrderType")
                        .HasColumnType("INTEGER");

                    b.Property<double?>("Price")
                        .HasColumnType("REAL");

                    b.Property<double>("Quantity")
                        .HasColumnType("REAL");

                    b.Property<string>("Symbol")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.ToTable("TradingActions");
                });

            modelBuilder.Entity("TradingBot.Database.Entities.TradingActionDetailsEntity", b =>
                {
                    b.HasOne("TradingBot.Database.Entities.TradingActionEntity", "TradingAction")
                        .WithOne("Details")
                        .HasForeignKey("TradingBot.Database.Entities.TradingActionDetailsEntity", "TradingActionId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("TradingAction");
                });

            modelBuilder.Entity("TradingBot.Database.Entities.TradingActionEntity", b =>
                {
                    b.Navigation("Details")
                        .IsRequired();
                });
#pragma warning restore 612, 618
        }
    }
}
