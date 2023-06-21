﻿// <auto-generated> This file has been auto generated by EF Core Power Tools. </auto-generated>
#nullable enable
using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace CES.DataTier.Models
{
    public partial class CEsData_devContext : DbContext
    {
        public CEsData_devContext()
        {
        }

        public CEsData_devContext(DbContextOptions<CEsData_devContext> options)
            : base(options)
        {
        }

        public virtual DbSet<Account> Account { get; set; } = null!;
        public virtual DbSet<Category> Category { get; set; } = null!;
        public virtual DbSet<Company> Company { get; set; } = null!;
        public virtual DbSet<DebtNotes> DebtNotes { get; set; } = null!;
        public virtual DbSet<Discount> Discount { get; set; } = null!;
        public virtual DbSet<Order> Order { get; set; } = null!;
        public virtual DbSet<OrderDetail> OrderDetail { get; set; } = null!;
        public virtual DbSet<Product> Product { get; set; } = null!;
        public virtual DbSet<Project> Project { get; set; } = null!;
        public virtual DbSet<ProjectAccount> ProjectAccount { get; set; } = null!;
        public virtual DbSet<Receipts> Receipts { get; set; } = null!;
        public virtual DbSet<Role> Role { get; set; } = null!;
        public virtual DbSet<Transaction> Transaction { get; set; } = null!;
        public virtual DbSet<Wallet> Wallet { get; set; } = null!;
        public virtual DbSet<WalletTransaction> WalletTransaction { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Account>(entity =>
            {
                entity.Property(e => e.Id).ValueGeneratedNever();

                entity.Property(e => e.CreatedAt).HasColumnType("datetime");

                entity.Property(e => e.Email).HasMaxLength(200);

                entity.Property(e => e.Name).HasMaxLength(200);

                entity.Property(e => e.Password).HasMaxLength(200);

                entity.Property(e => e.Phone).HasMaxLength(20);

                entity.Property(e => e.UpdatedAt).HasColumnType("datetime");

                entity.HasOne(d => d.Company)
                    .WithMany(p => p.Account)
                    .HasForeignKey(d => d.CompanyId)
                    .HasConstraintName("FK_Account_Company");

                entity.HasOne(d => d.Role)
                    .WithMany(p => p.Account)
                    .HasForeignKey(d => d.RoleId)
                    .HasConstraintName("FK_Account_Role");
            });

            modelBuilder.Entity<Category>(entity =>
            {
                entity.Property(e => e.CreatedAt).HasColumnType("datetime");

                entity.Property(e => e.Name).HasMaxLength(200);

                entity.Property(e => e.UpdatedAt).HasColumnType("datetime");
            });

            modelBuilder.Entity<Company>(entity =>
            {
                entity.Property(e => e.ContactPerson).HasMaxLength(100);

                entity.Property(e => e.CreatedAt).HasColumnType("datetime");

                entity.Property(e => e.Name).HasMaxLength(200);

                entity.Property(e => e.Phone).HasMaxLength(20);

                entity.Property(e => e.UpdatedAt).HasColumnType("datetime");
            });

            modelBuilder.Entity<DebtNotes>(entity =>
            {
                entity.Property(e => e.Id).ValueGeneratedNever();

                entity.Property(e => e.CreatedAt).HasColumnType("datetime");

                entity.Property(e => e.InfoPayment).HasMaxLength(200);

                entity.Property(e => e.Name).HasMaxLength(200);

                entity.Property(e => e.UpdatedAt).HasColumnType("datetime");

                entity.HasOne(d => d.Company)
                    .WithMany(p => p.DebtNotes)
                    .HasForeignKey(d => d.CompanyId)
                    .HasConstraintName("FK_DebtNotes_Company");
            });

            modelBuilder.Entity<Discount>(entity =>
            {
                entity.Property(e => e.Id).ValueGeneratedNever();

                entity.Property(e => e.CreatedAt).HasColumnType("datetime");

                entity.Property(e => e.ExpirationDate).HasMaxLength(100);

                entity.Property(e => e.UpdatedAt).HasColumnType("datetime");

                entity.HasOne(d => d.Product)
                    .WithMany(p => p.Discount)
                    .HasForeignKey(d => d.ProductId)
                    .HasConstraintName("FK_Discount_Product");
            });

            modelBuilder.Entity<Order>(entity =>
            {
                entity.Property(e => e.Id).ValueGeneratedNever();

                entity.Property(e => e.Address).HasMaxLength(500);

                entity.Property(e => e.Code).HasMaxLength(100);

                entity.Property(e => e.CreatedAt).HasColumnType("datetime");

                entity.Property(e => e.Note).HasMaxLength(200);

                entity.Property(e => e.UpdatedAt).HasColumnType("datetime");

                entity.HasOne(d => d.Account)
                    .WithMany(p => p.Order)
                    .HasForeignKey(d => d.AccountId)
                    .HasConstraintName("FK_Order_Account");

                entity.HasOne(d => d.Debt)
                    .WithMany(p => p.Order)
                    .HasForeignKey(d => d.DebtId)
                    .HasConstraintName("FK_DebtNotes_Order");
            });

            modelBuilder.Entity<OrderDetail>(entity =>
            {
                entity.Property(e => e.Id).ValueGeneratedNever();

                entity.Property(e => e.CreatedAt).HasColumnType("datetime");

                entity.Property(e => e.Notes).HasMaxLength(200);

                entity.HasOne(d => d.Order)
                    .WithMany(p => p.OrderDetail)
                    .HasForeignKey(d => d.OrderId)
                    .HasConstraintName("FK_OrderDetail_Order");

                entity.HasOne(d => d.Product)
                    .WithMany(p => p.OrderDetail)
                    .HasForeignKey(d => d.ProductId)
                    .HasConstraintName("FK_OrderDetail_Product");
            });

            modelBuilder.Entity<Product>(entity =>
            {
                entity.Property(e => e.Id).ValueGeneratedNever();

                entity.Property(e => e.CreatedAt).HasColumnType("datetime");

                entity.Property(e => e.Name).HasMaxLength(200);

                entity.Property(e => e.ServiceDuration).HasMaxLength(100);

                entity.Property(e => e.UpdatedAt).HasColumnType("datetime");

                entity.HasOne(d => d.Category)
                    .WithMany(p => p.Product)
                    .HasForeignKey(d => d.CategoryId)
                    .HasConstraintName("FK_Product_Category");
            });

            modelBuilder.Entity<Project>(entity =>
            {
                entity.Property(e => e.Id).ValueGeneratedNever();

                entity.Property(e => e.CreatedAt).HasColumnType("datetime");

                entity.Property(e => e.Name).HasMaxLength(200);

                entity.Property(e => e.UpdatedAt).HasColumnType("datetime");
            });

            modelBuilder.Entity<ProjectAccount>(entity =>
            {
                entity.Property(e => e.Id).ValueGeneratedNever();

                entity.HasOne(d => d.Account)
                    .WithMany(p => p.ProjectAccount)
                    .HasForeignKey(d => d.AccountId)
                    .HasConstraintName("FK_ProjectAccount_Account");

                entity.HasOne(d => d.Project)
                    .WithMany(p => p.ProjectAccount)
                    .HasForeignKey(d => d.ProjectId)
                    .HasConstraintName("FK_ProjectAccount_Project");
            });

            modelBuilder.Entity<Receipts>(entity =>
            {
                entity.Property(e => e.Id).ValueGeneratedNever();

                entity.Property(e => e.CreatedAt).HasColumnType("datetime");

                entity.Property(e => e.Name).HasMaxLength(200);

                entity.Property(e => e.PaymentCode).HasMaxLength(100);

                entity.Property(e => e.UpdatedAt).HasColumnType("datetime");

                entity.HasOne(d => d.Company)
                    .WithMany(p => p.Receipts)
                    .HasForeignKey(d => d.CompanyId)
                    .HasConstraintName("FK_Receipts_Company");

                entity.HasOne(d => d.Debt)
                    .WithMany(p => p.Receipts)
                    .HasForeignKey(d => d.DebtId)
                    .HasConstraintName("FK_Receipts_DebtNotes");
            });

            modelBuilder.Entity<Role>(entity =>
            {
                entity.Property(e => e.CreatedAt).HasColumnType("datetime");

                entity.Property(e => e.Name).HasMaxLength(100);

                entity.Property(e => e.UpdatedAt).HasColumnType("datetime");
            });

            modelBuilder.Entity<Transaction>(entity =>
            {
                entity.Property(e => e.Id).ValueGeneratedNever();

                entity.Property(e => e.CreatedAt).HasColumnType("datetime");

                entity.Property(e => e.Description).HasMaxLength(200);

                entity.HasOne(d => d.Order)
                    .WithMany(p => p.Transaction)
                    .HasForeignKey(d => d.OrderId)
                    .HasConstraintName("FK_Transaction_Order");

                entity.HasOne(d => d.Wallet)
                    .WithMany(p => p.Transaction)
                    .HasForeignKey(d => d.WalletId)
                    .HasConstraintName("FK_Transaction_Wallet");
            });

            modelBuilder.Entity<Wallet>(entity =>
            {
                entity.Property(e => e.Id).ValueGeneratedNever();

                entity.Property(e => e.CreatedAt).HasColumnType("datetime");

                entity.Property(e => e.Name).HasMaxLength(50);

                entity.Property(e => e.UpdatedAt).HasColumnType("datetime");

                entity.HasOne(d => d.Account)
                    .WithMany(p => p.Wallet)
                    .HasForeignKey(d => d.AccountId)
                    .HasConstraintName("FK_Wallet_Account");
            });

            modelBuilder.Entity<WalletTransaction>(entity =>
            {
                entity.Property(e => e.Id).ValueGeneratedNever();

                entity.Property(e => e.CreatedAt).HasColumnType("datetime");

                entity.Property(e => e.UpdatedAt).HasColumnType("datetime");

                entity.HasOne(d => d.Wallet)
                    .WithMany(p => p.WalletTransaction)
                    .HasForeignKey(d => d.WalletId)
                    .HasConstraintName("FK_WalletTransaction_Wallet");
            });

            OnModelCreatingPartial(modelBuilder);
        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    }
}