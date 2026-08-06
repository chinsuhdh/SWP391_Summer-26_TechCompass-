using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Repository_TechCompass.Models
{
    [Table("saved_jobs")] // Tên bảng sẽ tạo trong SQL Server
    public class SavedJob
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Column("user_id")]
        public Guid UserId { get; set; }

        [Column("job_id")]
        public Guid JobId { get; set; } // Nếu JobId của bạn trong bảng khác là INT thì sửa chỗ này thành int nhé!

        [Column("saved_at")]
        public DateTime SavedAt { get; set; } = DateTime.UtcNow;
    }
}