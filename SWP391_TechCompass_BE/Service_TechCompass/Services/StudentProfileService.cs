using Repository_TechCompass.Interfaces;
using Repository_TechCompass.Models;

using Service_TechCompass.Interfaces;

namespace Service_TechCompass.Services
{
    public class StudentProfileService : IStudentProfileService
    {
        private readonly IUserRepository _userRepo;

        public StudentProfileService(IUserRepository userRepo)
        {
            _userRepo = userRepo;
        }

        public Task<(int StatusCode, string Message, UserStudentProfileDto? Data)> GetProfileAsync(Guid userId)
        {
            // Lấy thông tin user (kèm theo thông tin Student tương ứng)
            var user = _userRepo.GetUserById(userId);
            var student = _userRepo.GetStudentByUserId(userId);

            if (user == null || student == null)
            {
                return Task.FromResult<(int, string, UserStudentProfileDto?)>((404, "Không tìm thấy hồ sơ người dùng.", null));
            }

            var profileData = new UserStudentProfileDto
            {
                UserId = user.UserId,
                Email = user.Email,
                FullName = student.FullName,
                StudentCode = student.StudentCode,
                LatentTalentSummary = student.LatentTalentSummary,
                TargetRoleId = student.TargetRoleId,
                UpdatedAt = student.UpdatedAt
            };

            return Task.FromResult<(int, string, UserStudentProfileDto?)>((200, "Lấy thông tin thành công.", profileData));
        }

        public Task<(int StatusCode, string Message)> UpdateProfileAsync(Guid userId, UpdateStudentProfileDto request)
        {
            var student = _userRepo.GetStudentByUserId(userId);

            if (student == null)
            {
                return Task.FromResult<(int, string)>((404, "Không tìm thấy hồ sơ sinh viên để cập nhật."));
            }

            // Cập nhật các trường thông tin
            student.FullName = request.FullName;
            student.StudentCode = request.StudentCode;
            student.LatentTalentSummary = request.LatentTalentSummary;
            student.TargetRoleId = request.TargetRoleId;
            student.UpdatedAt = DateTime.Now;

            // Lưu vào database
            _userRepo.UpdateStudent(student);
            _userRepo.SaveChanges();

            return Task.FromResult<(int, string)>((200, "Cập nhật hồ sơ thành công."));
        }
    }
}