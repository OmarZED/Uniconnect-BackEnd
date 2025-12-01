

using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using UniConnect.Models;

namespace UniConnect.Maping
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        // === ACADEMIC DB SETS ===
        public DbSet<Faculty> Faculties { get; set; }
        public DbSet<Course> Courses { get; set; }
        public DbSet<StudentGroup> StudentGroups { get; set; }

        // === COMMUNITY DB SETS ===
        public DbSet<Community> Communities { get; set; }
        public DbSet<CommunityMember> CommunityMembers { get; set; }
        public DbSet<Post> Posts { get; set; }
        public DbSet<PostReaction> PostReactions { get; set; }
        public DbSet<Comment> Comments { get; set; }
        public DbSet<CommentVote> CommentVotes { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure ApplicationUser entity
            modelBuilder.Entity<ApplicationUser>(entity =>
            {
                // Property configurations
                entity.Property(u => u.FirstName)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(u => u.LastName)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(u => u.Role)
                    .IsRequired()
                    .HasConversion<string>()
                    .HasMaxLength(20);

                entity.Property(u => u.CreatedAt)
                    .IsRequired();

                entity.Property(u => u.LastLogin)
                    .IsRequired(false);

                entity.Property(u => u.IsActive)
                    .IsRequired()
                    .HasDefaultValue(true);

                entity.Property(u => u.ProfilePictureUrl)
                    .IsRequired(false);

                entity.Property(u => u.Bio)
                    .IsRequired(false);

                // Relationships
                entity.HasOne(u => u.StudentGroup)
                    .WithMany(g => g.Students)
                    .HasForeignKey(u => u.StudentGroupId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(u => u.Course)
                    .WithMany()
                    .HasForeignKey(u => u.CourseId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(u => u.Faculty)
                    .WithMany(f => f.Teachers)
                    .HasForeignKey(u => u.FacultyId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasMany(u => u.ManagedFaculties)
                    .WithOne(f => f.Dean)
                    .HasForeignKey(f => f.DeanId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Configure Faculty entity
            modelBuilder.Entity<Faculty>(entity =>
            {
                entity.Property(f => f.Name)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(f => f.Code)
                    .IsRequired()
                    .HasMaxLength(20);

                entity.Property(f => f.Description)
                    .IsRequired(false);

                entity.Property(f => f.CreatedAt)
                    .IsRequired();

                entity.Property(f => f.IsActive)
                    .HasDefaultValue(true);
            });

            // Configure Course entity
            modelBuilder.Entity<Course>(entity =>
            {
                entity.Property(c => c.Name)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(c => c.Code)
                    .IsRequired()
                    .HasMaxLength(10);

                entity.Property(c => c.Year)
                    .IsRequired();

                entity.Property(c => c.CreatedAt)
                    .IsRequired();

                entity.Property(c => c.IsActive)
                    .HasDefaultValue(true);

                // Faculty relationship
                entity.HasOne(c => c.Faculty)
                    .WithMany(f => f.Courses)
                    .HasForeignKey(c => c.FacultyId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Configure StudentGroup entity
            modelBuilder.Entity<StudentGroup>(entity =>
            {
                entity.Property(g => g.Name)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(g => g.Code)
                    .IsRequired()
                    .HasMaxLength(20);

                entity.Property(g => g.Description)
                    .IsRequired(false);

                entity.Property(g => g.CreatedAt)
                    .IsRequired();

                entity.Property(g => g.IsActive)
                    .HasDefaultValue(true);

                // Course relationship
                entity.HasOne(g => g.Course)
                    .WithMany(c => c.Groups)
                    .HasForeignKey(g => g.CourseId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // === COMMUNITY CONFIGURATIONS ===

            // Configure Community entity
            modelBuilder.Entity<Community>(entity =>
            {
                entity.Property(c => c.Name)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(c => c.Description)
                    .IsRequired(false)
                    .HasMaxLength(500);

                entity.Property(c => c.Type)
                    .IsRequired()
                    .HasConversion<string>()
                    .HasMaxLength(20);

                entity.Property(c => c.CreatedAt)
                    .IsRequired();

                entity.Property(c => c.IsActive)
                    .HasDefaultValue(true);

                entity.Property(c => c.AllowPosts)
                    .HasDefaultValue(true);

                entity.Property(c => c.AutoJoin)
                    .HasDefaultValue(true);

                // Academic relationships (optional)
                entity.HasOne(c => c.Faculty)
                    .WithMany()
                    .HasForeignKey(c => c.FacultyId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(c => c.Course)
                    .WithMany()
                    .HasForeignKey(c => c.CourseId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(c => c.StudentGroup)
                    .WithMany()
                    .HasForeignKey(c => c.StudentGroupId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Configure CommunityMember entity
            modelBuilder.Entity<CommunityMember>(entity =>
            {
                entity.Property(cm => cm.Role)
                    .IsRequired()
                    .HasConversion<string>()
                    .HasMaxLength(20);

                entity.Property(cm => cm.JoinedAt)
                    .IsRequired();

                entity.Property(cm => cm.IsActive)
                    .HasDefaultValue(true);

                // Composite key to prevent duplicate memberships
                entity.HasIndex(cm => new { cm.CommunityId, cm.UserId })
                    .IsUnique();

                // Relationships
                entity.HasOne(cm => cm.Community)
                    .WithMany(c => c.Members)
                    .HasForeignKey(cm => cm.CommunityId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(cm => cm.User)
                    .WithMany()
                    .HasForeignKey(cm => cm.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Configure Post entity
            modelBuilder.Entity<Post>(entity =>
            {
                entity.Property(p => p.Title)
                    .IsRequired()
                    .HasMaxLength(200);

                entity.Property(p => p.Content)
                    .IsRequired();

                entity.Property(p => p.Visibility)
                    .IsRequired()
                    .HasConversion<string>()
                    .HasMaxLength(20);

                entity.Property(p => p.ImageUrl)
                    .IsRequired(false);

                entity.Property(p => p.FileUrl)
                    .IsRequired(false);

                entity.Property(p => p.FileName)
                    .IsRequired(false);

                entity.Property(p => p.CreatedAt)
                    .IsRequired();

                entity.Property(p => p.IsActive)
                    .HasDefaultValue(true);

                // Relationships
                entity.HasOne(p => p.Community)
                    .WithMany(c => c.Posts)
                    .HasForeignKey(p => p.CommunityId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(p => p.Author)
                    .WithMany()
                    .HasForeignKey(p => p.AuthorId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Configure PostReaction entity
            modelBuilder.Entity<PostReaction>(entity =>
            {
                entity.Property(pr => pr.ReactionType)
                    .IsRequired()
                    .HasConversion<string>()
                    .HasMaxLength(20);

                entity.Property(pr => pr.CreatedAt)
                    .IsRequired();

                // Composite key to prevent duplicate reactions
                entity.HasIndex(pr => new { pr.PostId, pr.UserId })
                    .IsUnique();

                // Relationships
                entity.HasOne(pr => pr.Post)
                    .WithMany(p => p.Reactions)
                    .HasForeignKey(pr => pr.PostId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(pr => pr.User)
                    .WithMany()
                    .HasForeignKey(pr => pr.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Configure Comment entity
            modelBuilder.Entity<Comment>(entity =>
            {
                entity.Property(c => c.Content)
                    .IsRequired();

                entity.Property(c => c.CreatedAt)
                    .IsRequired();

                entity.Property(c => c.IsActive)
                    .HasDefaultValue(true);

                // Relationships
                entity.HasOne(c => c.Post)
                    .WithMany(p => p.Comments)
                    .HasForeignKey(c => c.PostId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(c => c.Author)
                    .WithMany()
                    .HasForeignKey(c => c.AuthorId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(c => c.ParentComment)
                    .WithMany(c => c.Replies)
                    .HasForeignKey(c => c.ParentCommentId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Configure CommentVote entity
            modelBuilder.Entity<CommentVote>(entity =>
            {
                entity.Property(cv => cv.VoteType)
                    .IsRequired()
                    .HasConversion<int>(); // Store as int for VoteType enum

                entity.Property(cv => cv.CreatedAt)
                    .IsRequired();

                // Composite key to prevent duplicate votes
                entity.HasIndex(cv => new { cv.CommentId, cv.UserId })
                    .IsUnique();

                // Relationships
                entity.HasOne(cv => cv.Comment)
                    .WithMany(c => c.Votes)
                    .HasForeignKey(cv => cv.CommentId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(cv => cv.User)
                    .WithMany()
                    .HasForeignKey(cv => cv.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Remove old AdminType check constraints since we deleted AdminType
            // No more check constraints for AdminType
        }
    }
}