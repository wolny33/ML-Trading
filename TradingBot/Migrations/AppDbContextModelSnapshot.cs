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

                    b.Property<Guid?>("BacktestId")
                        .HasColumnType("TEXT");

                    b.Property<double>("BuyingPower")
                        .HasColumnType("REAL");

                    b.Property<long>("CreationTimestamp")
                        .HasColumnType("INTEGER");

                    b.Property<double>("EquityValue")
                        .HasColumnType("REAL");

                    b.Property<string>("MainCurrency")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<int>("Mode")
                        .HasColumnType("INTEGER");

                    b.HasKey("Id");

                    b.HasIndex("BacktestId");

                    b.HasIndex("CreationTimestamp");

                    b.ToTable("AssetsStates");
                });

            modelBuilder.Entity("TradingBot.Database.Entities.BacktestEntity", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("TEXT");

                    b.Property<string>("Description")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<long?>("ExecutionEndTimestamp")
                        .HasColumnType("INTEGER");

                    b.Property<long>("ExecutionStartTimestamp")
                        .HasColumnType("INTEGER");

                    b.Property<DateOnly>("SimulationEnd")
                        .HasColumnType("TEXT");

                    b.Property<DateOnly>("SimulationStart")
                        .HasColumnType("TEXT");

                    b.Property<int>("State")
                        .HasColumnType("INTEGER");

                    b.Property<string>("StateDetails")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<bool>("UsePredictor")
                        .HasColumnType("INTEGER");

                    b.HasKey("Id");

                    b.HasIndex("ExecutionStartTimestamp");

                    b.ToTable("Backtests");
                });

            modelBuilder.Entity("TradingBot.Database.Entities.BuyLosersStrategyStateEntity", b =>
                {
                    b.Property<Guid>("BacktestId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("TEXT");

                    b.Property<DateOnly?>("NextEvaluationDay")
                        .HasColumnType("TEXT");

                    b.HasKey("BacktestId");

                    b.ToTable("BuyLosersStrategyStates");
                });

            modelBuilder.Entity("TradingBot.Database.Entities.BuyWinnersBuyActionEntity", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("TEXT");

                    b.Property<Guid>("ActionId")
                        .HasColumnType("TEXT");

                    b.Property<Guid>("EvaluationId")
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.HasIndex("EvaluationId");

                    b.ToTable("WinnerBuyActions");
                });

            modelBuilder.Entity("TradingBot.Database.Entities.BuyWinnersEvaluationEntity", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("TEXT");

                    b.Property<bool>("Bought")
                        .HasColumnType("INTEGER");

                    b.Property<DateOnly>("CreatedAt")
                        .HasColumnType("TEXT");

                    b.Property<Guid>("StrategyStateBacktestId")
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.HasIndex("StrategyStateBacktestId");

                    b.ToTable("BuyWinnersEvaluations");
                });

            modelBuilder.Entity("TradingBot.Database.Entities.BuyWinnersStrategyStateEntity", b =>
                {
                    b.Property<Guid>("BacktestId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("TEXT");

                    b.Property<DateOnly?>("NextEvaluationDay")
                        .HasColumnType("TEXT");

                    b.HasKey("BacktestId");

                    b.ToTable("BuyWinnersStrategyStates");
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

            modelBuilder.Entity("TradingBot.Database.Entities.LoserSymbolToBuyEntity", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("TEXT");

                    b.Property<Guid>("StrategyStateBacktestId")
                        .HasColumnType("TEXT");

                    b.Property<string>("Symbol")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.HasIndex("StrategyStateBacktestId");

                    b.ToTable("LoserSymbolsToBuy");
                });

            modelBuilder.Entity("TradingBot.Database.Entities.PcaDecompositionEntity", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("TEXT");

                    b.Property<Guid?>("BacktestId")
                        .HasColumnType("TEXT");

                    b.Property<DateOnly>("CreatedAt")
                        .HasColumnType("TEXT");

                    b.Property<long>("CreationTimestamp")
                        .HasColumnType("INTEGER");

                    b.Property<DateOnly>("ExpiresAt")
                        .HasColumnType("TEXT");

                    b.Property<string>("Means")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("PrincipalVectors")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("StandardDeviations")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("Symbols")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.HasIndex("CreationTimestamp");

                    b.ToTable("PcaDecompositions");
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

                    b.Property<int>("BuyLosersAnalysisLengthInDays")
                        .HasColumnType("INTEGER");

                    b.Property<int>("BuyLosersEvaluationFrequencyInDays")
                        .HasColumnType("INTEGER");

                    b.Property<int>("BuyWinnersAnalysisLengthInDays")
                        .HasColumnType("INTEGER");

                    b.Property<int>("BuyWinnersBuyWaitTimeInDays")
                        .HasColumnType("INTEGER");

                    b.Property<int>("BuyWinnersEvaluationFrequencyInDays")
                        .HasColumnType("INTEGER");

                    b.Property<int>("BuyWinnersSimultaneousEvaluations")
                        .HasColumnType("INTEGER");

                    b.Property<double>("LimitPriceDamping")
                        .HasColumnType("REAL");

                    b.Property<int>("MaxStocksBuyCount")
                        .HasColumnType("INTEGER");

                    b.Property<int>("MinDaysDecreasing")
                        .HasColumnType("INTEGER");

                    b.Property<int>("MinDaysIncreasing")
                        .HasColumnType("INTEGER");

                    b.Property<int>("PcaAnalysisLengthInDays")
                        .HasColumnType("INTEGER");

                    b.Property<int>("PcaDecompositionExpirationInDays")
                        .HasColumnType("INTEGER");

                    b.Property<double>("PcaUndervaluedThreshold")
                        .HasColumnType("REAL");

                    b.Property<double>("PcaVarianceFraction")
                        .HasColumnType("REAL");

                    b.Property<double>("TopGrowingSymbolsBuyRatio")
                        .HasColumnType("REAL");

                    b.HasKey("Id");

                    b.ToTable("StrategyParameters");
                });

            modelBuilder.Entity("TradingBot.Database.Entities.StrategySelectionEntity", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("TEXT");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.ToTable("StrategySelection");
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

                    b.Property<Guid?>("BacktestId")
                        .HasColumnType("TEXT");

                    b.Property<long?>("EndTimestamp")
                        .HasColumnType("INTEGER");

                    b.Property<int>("Mode")
                        .HasColumnType("INTEGER");

                    b.Property<long>("StartTimestamp")
                        .HasColumnType("INTEGER");

                    b.Property<int>("State")
                        .HasColumnType("INTEGER");

                    b.Property<string>("StateDetails")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.HasIndex("BacktestId");

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

            modelBuilder.Entity("TradingBot.Database.Entities.WinnerSymbolToBuyEntity", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("TEXT");

                    b.Property<Guid>("EvaluationId")
                        .HasColumnType("TEXT");

                    b.Property<string>("Symbol")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.HasIndex("EvaluationId");

                    b.ToTable("WinnerSymbolsToBuy");
                });

            modelBuilder.Entity("TradingBot.Database.Entities.AssetsStateEntity", b =>
                {
                    b.HasOne("TradingBot.Database.Entities.BacktestEntity", "Backtest")
                        .WithMany("AssetsStates")
                        .HasForeignKey("BacktestId");

                    b.Navigation("Backtest");
                });

            modelBuilder.Entity("TradingBot.Database.Entities.BuyWinnersBuyActionEntity", b =>
                {
                    b.HasOne("TradingBot.Database.Entities.BuyWinnersEvaluationEntity", "Evaluation")
                        .WithMany("Actions")
                        .HasForeignKey("EvaluationId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Evaluation");
                });

            modelBuilder.Entity("TradingBot.Database.Entities.BuyWinnersEvaluationEntity", b =>
                {
                    b.HasOne("TradingBot.Database.Entities.BuyWinnersStrategyStateEntity", "StrategyState")
                        .WithMany("Evaluations")
                        .HasForeignKey("StrategyStateBacktestId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("StrategyState");
                });

            modelBuilder.Entity("TradingBot.Database.Entities.LoserSymbolToBuyEntity", b =>
                {
                    b.HasOne("TradingBot.Database.Entities.BuyLosersStrategyStateEntity", "StrategyState")
                        .WithMany("SymbolsToBuy")
                        .HasForeignKey("StrategyStateBacktestId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("StrategyState");
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
                    b.HasOne("TradingBot.Database.Entities.BacktestEntity", "Backtest")
                        .WithMany("TradingTasks")
                        .HasForeignKey("BacktestId");

                    b.Navigation("Backtest");
                });

            modelBuilder.Entity("TradingBot.Database.Entities.WinnerSymbolToBuyEntity", b =>
                {
                    b.HasOne("TradingBot.Database.Entities.BuyWinnersEvaluationEntity", "Evaluation")
                        .WithMany("SymbolsToBuy")
                        .HasForeignKey("EvaluationId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Evaluation");
                });

            modelBuilder.Entity("TradingBot.Database.Entities.AssetsStateEntity", b =>
                {
                    b.Navigation("HeldPositions");
                });

            modelBuilder.Entity("TradingBot.Database.Entities.BacktestEntity", b =>
                {
                    b.Navigation("AssetsStates");

                    b.Navigation("TradingTasks");
                });

            modelBuilder.Entity("TradingBot.Database.Entities.BuyLosersStrategyStateEntity", b =>
                {
                    b.Navigation("SymbolsToBuy");
                });

            modelBuilder.Entity("TradingBot.Database.Entities.BuyWinnersEvaluationEntity", b =>
                {
                    b.Navigation("Actions");

                    b.Navigation("SymbolsToBuy");
                });

            modelBuilder.Entity("TradingBot.Database.Entities.BuyWinnersStrategyStateEntity", b =>
                {
                    b.Navigation("Evaluations");
                });

            modelBuilder.Entity("TradingBot.Database.Entities.TradingTaskEntity", b =>
                {
                    b.Navigation("TradingActions");
                });
#pragma warning restore 612, 618
        }
    }
}
