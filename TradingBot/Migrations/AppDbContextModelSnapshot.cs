﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using TradingBot.Database;

#nullable disable

namespace TradingBot.Migrations
{
    [DbContext(typeof(AppDbContext))]
    partial class AppDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder.HasAnnotation("ProductVersion", "7.0.13");

            modelBuilder.Entity("TradingBot.Database.Entities.InvestmentConfigEntity", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("TEXT");

                    b.Property<bool>("Enabled")
                        .HasColumnType("INTEGER");

                    b.HasKey("Id");

                    b.ToTable("InvestmentConfiguration");
                });

            modelBuilder.Entity("TradingBot.Database.Entities.TestModeConfigEntity", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("TEXT");

                    b.Property<bool>("Enabled")
                        .HasColumnType("INTEGER");

                    b.HasKey("Id");

                    b.ToTable("TestModeConfiguration");
                });

            modelBuilder.Entity("TradingBot.Database.Entities.TradingActionEntity", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("TEXT");

                    b.Property<Guid?>("AlpacaId")
                        .HasColumnType("TEXT");

                    b.Property<double?>("AverageFillPrice")
                        .HasColumnType("REAL");

                    b.Property<long>("CreationTimestamp")
                        .HasColumnType("INTEGER");

                    b.Property<string>("ErrorCode")
                        .HasColumnType("TEXT");

                    b.Property<string>("ErrorMessage")
                        .HasColumnType("TEXT");

                    b.Property<long?>("ExecutionTimestamp")
                        .HasColumnType("INTEGER");

                    b.Property<int>("InForce")
                        .HasColumnType("INTEGER");

                    b.Property<int>("OrderType")
                        .HasColumnType("INTEGER");

                    b.Property<double?>("Price")
                        .HasColumnType("REAL");

                    b.Property<double>("Quantity")
                        .HasColumnType("REAL");

                    b.Property<int?>("Status")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Symbol")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<Guid?>("TradingTaskId")
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.HasIndex("TradingTaskId");

                    b.ToTable("TradingActions");
                });

            modelBuilder.Entity("TradingBot.Database.Entities.TradingTaskEntity", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("TEXT");

                    b.Property<long?>("EndTimestamp")
                        .HasColumnType("INTEGER");

                    b.Property<long>("StartTimestamp")
                        .HasColumnType("INTEGER");

                    b.Property<int>("State")
                        .HasColumnType("INTEGER");

                    b.Property<string>("StateDetails")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.ToTable("TradingTasks");
                });

            modelBuilder.Entity("TradingBot.Database.Entities.UserCredentialsEntity", b =>
                {
                    b.Property<Guid>("Id")
                        .HasColumnType("TEXT");

                    b.Property<string>("HashedPassword")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("Username")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.ToTable("Credentials");

                    b.HasData(
                        new
                        {
                            Id = new Guid("00000000-0000-0000-0000-000000000000"),
                            HashedPassword = "AQAAAAIAAYagAAAAEKYyNm9AKgWuGR19nYSNT/7HYWJDCeC63fZKh/MfFaIaNIMhTKXzHLRXjEQ2uX6Qog==",
                            Username = "admin"
                        });
                });

            modelBuilder.Entity("TradingBot.Database.Entities.TradingActionEntity", b =>
                {
                    b.HasOne("TradingBot.Database.Entities.TradingTaskEntity", "TradingTask")
                        .WithMany()
                        .HasForeignKey("TradingTaskId");

                    b.Navigation("TradingTask");
                });
#pragma warning restore 612, 618
        }
    }
}
