using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace ArchiveReader.DbModels;

public partial class ArchiveViewerContext : DbContext
{
    public ArchiveViewerContext()
    {
    }

    public ArchiveViewerContext(DbContextOptions<ArchiveViewerContext> options)
        : base(options)
    {
    }

    public virtual DbSet<AdminDetail> AdminDetails { get; set; }

    public virtual DbSet<Folder> Folders { get; set; }

    public virtual DbSet<GroupFolder> GroupFolders { get; set; }

    public virtual DbSet<LicenseAvailable> LicenseAvailables { get; set; }

    public virtual DbSet<LicenseUsed> LicenseUseds { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see http://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlServer("Server=localhost;Database=ArchiveViewer;MultipleActiveResultSets=true;Trusted_Connection=True;trustServerCertificate=true;");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AdminDetail>(entity =>
        {
            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.UserGroup).HasMaxLength(50);
            entity.Property(e => e.UserGroupOptions).HasMaxLength(50);
        });

        modelBuilder.Entity<Folder>(entity =>
        {
            entity.Property(e => e.Id).ValueGeneratedNever();

            entity.HasOne(d => d.FolderNavigation).WithMany(p => p.InverseFolderNavigation)
                .HasForeignKey(d => d.FolderId)
                .HasConstraintName("FK_Folders_Folders1");
        });

        modelBuilder.Entity<GroupFolder>(entity =>
        {
            entity.Property(e => e.Id).ValueGeneratedNever();

            entity.HasOne(d => d.Folder).WithMany(p => p.GroupFolders).HasForeignKey(d => d.FolderId);
        });

        modelBuilder.Entity<LicenseAvailable>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("LicenseAvailable");

            entity.Property(e => e.Capacity)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Consumption)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Customer).HasMaxLength(50);
            entity.Property(e => e.ExpiryDate).HasMaxLength(50);
            entity.Property(e => e.Licensekey)
                .HasMaxLength(200)
                .IsUnicode(false);
            entity.Property(e => e.Type).HasMaxLength(50);
        });

        modelBuilder.Entity<LicenseUsed>(entity =>
        {
            entity.HasKey(e => e.Unid);

            entity.ToTable("LicenseUsed");

            entity.Property(e => e.Unid)
                .ValueGeneratedNever()
                .HasColumnName("UNID");
            entity.Property(e => e.LicenseKey).HasColumnType("text");
            entity.Property(e => e.ReplicaId)
                .HasColumnType("text")
                .HasColumnName("ReplicaID");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
