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

            modelBuilder.Entity("TradingBot.Database.Entities.AssetsStateEntity", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("TEXT");

                    b.Property<double>("AvailableCash")
                        .HasColumnType("REAL");

                    b.Property<long>("CreationTimestamp")
                        .HasColumnType("INTEGER");

                    b.Property<double>("EquityValue")
                        .HasColumnType("REAL");

                    b.Property<string>("MainCurrency")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.HasIndex("CreationTimestamp");

                    b.ToTable("AssetsStates");
                });

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

            modelBuilder.Entity("TradingBot.Database.Entities.PositionEntity", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("TEXT");

                    b.Property<Guid>("AssetsStateId")
                        .HasColumnType("TEXT");

                    b.Property<double>("AvailableQuantity")
                        .HasColumnType("REAL");

                    b.Property<double>("AverageEntryPrice")
                        .HasColumnType("REAL");

                    b.Property<double>("MarketValue")
                        .HasColumnType("REAL");

                    b.Property<double>("Quantity")
                        .HasColumnType("REAL");

                    b.Property<string>("Symbol")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<Guid>("SymbolId")
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.HasIndex("AssetsStateId");

                    b.ToTable("Positions");
                });

            modelBuilder.Entity("TradingBot.Database.Entities.StrategyParametersEntity", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("TEXT");

                    b.Property<int>("MaxStocksBuyCount")
                        .HasColumnType("INTEGER");

                    b.Property<int>("MinDaysDecreasing")
                        .HasColumnType("INTEGER");

                    b.Property<int>("MinDaysIncreasing")
                        .HasColumnType("INTEGER");

                    b.Property<double>("TopGrowingSymbolsBuyRatio")
                        .HasColumnType("REAL");

                    b.HasKey("Id");

                    b.ToTable("StrategyParameters");
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
                        .ValueGeneratedOnAdd()
                        .HasColumnType("TEXT");

                    b.Property<string>("HashedPassword")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("Username")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.ToTable("Credentials");
                });

            modelBuilder.Entity("TradingBot.Database.Entities.PositionEntity", b =>
                {
                    b.HasOne("TradingBot.Database.Entities.AssetsStateEntity", "AssetsState")
                        .WithMany("HeldPositions")
                        .HasForeignKey("AssetsStateId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("AssetsState");
                });

            modelBuilder.Entity("TradingBot.Database.Entities.TradingActionEntity", b =>
                {
                    b.HasOne("TradingBot.Database.Entities.TradingTaskEntity", "TradingTask")
                        .WithMany("TradingActions")
                        .HasForeignKey("TradingTaskId");

                    b.Navigation("TradingTask");
                });

            modelBuilder.Entity("TradingBot.Database.Entities.TradingTaskEntity", b =>
                {
                    b.Navigation("TradingActions");
                });

            modelBuilder.Entity("TradingBot.Database.Entities.AssetsStateEntity", b =>
                {
                    b.Navigation("HeldPositions");
                });
#pragma warning restore 612, 618
        }
    }
}
