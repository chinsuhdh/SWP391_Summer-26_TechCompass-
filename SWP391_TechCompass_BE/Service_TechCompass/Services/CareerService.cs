using Repository_TechCompass.Interfaces;
using Service_TechCompass.DTOs;
using Service_TechCompass.Interfaces;

namespace Service_TechCompass.Services
{
    public class CareerService : ICareerService
    {
        private readonly IUserRepository _userRepo;

        public CareerService(IUserRepository userRepo)
        {
            _userRepo = userRepo;
        }

        // 1. Chức năng: Khảo sát định hướng nghề nghiệp
        public Task<(int StatusCode, string Message, string? AnalyzedResult)> SubmitSurveyAsync(Guid userId, SubmitSurveyDto request)
        {
            var student = _userRepo.GetStudentByUserId(userId);
            if (student == null) return Task.FromResult<(int, string, string?)>((404, "Không tìm thấy hồ sơ sinh viên.", null));

            // TƯƠNG LAI: Đoạn này bạn sẽ đẩy data 'request' sang API của OpenAI / Gemini để nhờ AI phân tích.
            // HIỆN TẠI (Giả lập AI): Hệ thống sẽ tự tạo ra một đoạn nhận xét dựa trên dữ liệu gửi lên.
            string aiAnalysis = $"[AI Phân tích]: Dựa trên việc bạn thích ngôn ngữ {request.ProgrammingLanguagePreference} và môi trường {request.WorkEnvironmentPreference}, kết hợp với kỹ năng giải quyết vấn đề: '{request.ProblemSolvingSkill}'. Bạn có tố chất logic rất tốt và phù hợp với con đường Backend Developer hoặc System Architect.";

            // Lưu kết quả đánh giá của AI vào hồ sơ tiềm năng của sinh viên
            student.LatentTalentSummary = aiAnalysis;
            student.UpdatedAt = DateTime.Now;

            _userRepo.UpdateStudent(student);
            _userRepo.SaveChanges();

            return Task.FromResult<(int, string, string?)>((200, "Đã nộp khảo sát thành công. AI đã hoàn tất phân tích tiềm năng của bạn.", aiAnalysis));
        }

        // 2. Chức năng: Chọn nghề nghiệp mục tiêu (Target Career Role)
        public Task<(int StatusCode, string Message)> SelectTargetRoleAsync(Guid userId, SelectTargetRoleDto request)
        {
            var student = _userRepo.GetStudentByUserId(userId);
            if (student == null) return Task.FromResult<(int, string)>((404, "Không tìm thấy hồ sơ sinh viên."));

            // Cập nhật TargetRoleId do sinh viên chọn
            student.TargetRoleId = request.TargetRoleId;
            student.UpdatedAt = DateTime.Now;

            _userRepo.UpdateStudent(student);
            _userRepo.SaveChanges();

            return Task.FromResult<(int, string)>((200, "Đã cập nhật mục tiêu nghề nghiệp thành công. Lộ trình học tập (Roadmap) sẽ được hệ thống sinh ra dựa trên lựa chọn này."));
        }
    }
}