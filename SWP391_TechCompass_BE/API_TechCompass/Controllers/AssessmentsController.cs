using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Threading.Tasks;
using Service_TechCompass.Interfaces;
using Service_TechCompass.DTOs.Assessment;

namespace API_TechCompass.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AssessmentsController : ControllerBase
    {
        private readonly IAssessmentService _assessmentService;
        private readonly IRoadmapEngineService _roadmapEngineService;

        public AssessmentsController(
            IAssessmentService assessmentService,
            IRoadmapEngineService roadmapEngineService)
        {
            _assessmentService = assessmentService;
            _roadmapEngineService = roadmapEngineService;
        }

        // GET: api/assessments/quiz/{skillNodeId}
        [HttpGet("quiz/{skillNodeId}")]
        public async Task<IActionResult> GetQuiz(int skillNodeId)
        {
            try
            {
                var quiz = await _assessmentService.GetQuizBySkillNodeAsync(skillNodeId);
                return Ok(new { Message = "Lấy đề thành công", Data = quiz });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Error = ex.Message });
            }
        }

        // POST: api/assessments/submit-exam
        // ĐÃ THÊM: [FromQuery] isPlacementTest để phân nhánh luồng nghiệp vụ
        [HttpPost("submit-exam")]
        public async Task<IActionResult> SubmitFullExam([FromBody] SubmitFullExamDto submission, [FromQuery] bool isPlacementTest = false)
        {
            if (submission == null)
            {
                return BadRequest("Dữ liệu nộp bài không hợp lệ (Body rỗng).");
            }

            if (submission.StudentId == Guid.Empty)
            {
                return BadRequest("Lỗi: Frontend chưa truyền StudentId.");
            }

            // Nếu không phải Placement Test thì mới bắt buộc validate SkillNodeId
            if (!isPlacementTest && submission.SkillNodeId <= 0)
            {
                return BadRequest("Lỗi: SkillNodeId không hợp lệ (phải lớn hơn 0).");
            }

            if (submission.QuizAnswers == null || !submission.QuizAnswers.Any())
            {
                return BadRequest("Lỗi: Không có câu trả lời trắc nghiệm nào được gửi lên.");
            }

            if (submission.CodeSubmission == null || string.IsNullOrEmpty(submission.CodeSubmission.SourceCode))
            {
                return BadRequest("Lỗi: Mã nguồn bài thực hành không được để trống.");
            }

            try
            {
                // Chấm và lưu toàn bộ (Bảng Session, QuizDetails, CodeDetails)
                var result = await _assessmentService.GradeAndSaveFullExamAsync(submission);

                // PHÂN NHÁNH ĐỒNG BỘ TIẾN ĐỘ
                if (isPlacementTest)
                {
                    // LUỒNG 1: BÀI TEST TỔNG HỢP ĐẦU VÀO -> Quét hàng loạt Node
                    await _roadmapEngineService.SyncPlacementTestProgressAsync(submission.StudentId, result.QuizDetails.ToList());
                }
                else
                {
                    // LUỒNG 2: BÀI TEST TỪNG KỸ NĂNG -> Cập nhật 1 Node
                    await _roadmapEngineService.SyncProgressAfterAssessmentAsync(
                        submission.StudentId,
                        submission.SkillNodeId,
                        result.TotalQuizScore,
                        result.TotalCodeScore
                    );
                }

                return Ok(new
                {
                    Message = isPlacementTest ? "Hoàn tất bài đánh giá đầu vào. Lộ trình của bạn đã được cá nhân hóa!" : "Nộp bài, chấm điểm và cập nhật lộ trình hoàn tất!",
                    SessionId = result.SessionId,
                    QuizScore = result.TotalQuizScore,
                    CodeScore = result.TotalCodeScore
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine("\n================ [LỖI LƯU BÀI FULL EXAM] ================");
                Console.WriteLine($"Message: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner Exception: {ex.InnerException.Message}");
                }
                Console.WriteLine("=========================================================\n");

                return StatusCode(500, new { Error = "Lỗi hệ thống khi lưu bài", Detail = ex.Message });
            }
        }

        [HttpPost("sync-quiz-api")]
        public async Task<IActionResult> SyncQuestionsFromExternalApi(
            [FromServices] IQuizSyncService _quizSyncService,
            [FromQuery] int skillNodeId,
            [FromQuery] string tags,
            [FromQuery] int limit = 10)
        {
            try
            {
                int count = await _quizSyncService.FetchAndSaveQuestionsAsync(skillNodeId, tags, limit);
                return Ok(new { Message = $"Đồng bộ thành công {count} câu hỏi vào Database." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Error = ex.Message });
            }
        }

        // GET: api/assessments/all-skill-nodes
        [HttpGet("all-skill-nodes")]
        public async Task<IActionResult> GetAllSkillNodes()
        {
            try
            {
                var nodes = await _assessmentService.GetAllSkillNodesAsync();
                return Ok(new { Message = "Lấy danh sách Skill Nodes thành công", Data = nodes });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Error = ex.Message });
            }
        }

        // POST: api/assessments/generate-exercise/{skillNodeId}
        [HttpPost("generate-exercise/{skillNodeId}")]
        public async Task<IActionResult> GenerateOrGetCodingExercise(int skillNodeId)
        {
            try
            {
                var exercise = await _assessmentService.GetOrGenerateCodingExerciseAsync(skillNodeId);
                return Ok(new { Message = "Lấy đề bài thành công", Data = exercise });
            }
            catch (Exception ex)
            {
                Console.WriteLine("\n================ [LỖI GENERATE EXERCISE] ================");
                Console.WriteLine(ex.Message);
                Console.WriteLine("=========================================================\n");

                return StatusCode(500, new { Error = ex.Message });
            }
        }

        // GET: api/assessments/my-result/{studentId}/{skillNodeId}
        [HttpGet("my-result/{studentId}/{skillNodeId}")]
        public async Task<IActionResult> GetMyNodeResult(Guid studentId, int skillNodeId)
        {
            try
            {
                var result = await _assessmentService.GetMyLatestNodeResultAsync(studentId, skillNodeId);

                if (result == null)
                {
                    return NotFound(new { Message = "Bạn chưa hoàn thành bài kiểm tra cho kỹ năng này." });
                }

                return Ok(new { Message = "Lấy lịch sử làm bài thành công", Data = result });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Error = ex.Message });
            }
        }

        // GET: api/assessments/my-history/{studentId}
        [HttpGet("my-history/{studentId}")]
        public async Task<IActionResult> GetMyHistoryList(Guid studentId)
        {
            try
            {
                var history = await _assessmentService.GetMyAssessmentHistoryListAsync(studentId);
                return Ok(new { Message = "Lấy danh sách thành công", Data = history });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Error = ex.Message });
            }
        }

        // GET: api/assessments/history-detail/{assessmentId}
        [HttpGet("history-detail/{assessmentId}")]
        public async Task<IActionResult> GetHistoryDetail(Guid assessmentId)
        {
            try
            {
                var result = await _assessmentService.GetAssessmentDetailByIdAsync(assessmentId);
                if (result == null) return NotFound(new { Message = "Không tìm thấy dữ liệu bài làm này." });

                return Ok(new { Message = "Thành công", Data = result });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Error = ex.Message });
            }
        }

        [HttpGet("quiz/role/{roleId}")]
        public async Task<IActionResult> GetRoleQuiz(int roleId)
        {
            try
            {
                var quiz = await _assessmentService.GetComprehensiveQuizByRoleAsync(roleId);
                return Ok(new { Message = "Lấy đề đánh giá nghề nghiệp thành công", Data = quiz });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Error = ex.Message });
            }
        }

        [HttpPost("generate-exercise/role/{roleId}")]
        public async Task<IActionResult> GenerateRoleExercise(int roleId)
        {
            try
            {
                var exercise = await _assessmentService.GetComprehensiveCodingExerciseByRoleAsync(roleId);
                return Ok(new { Message = "Lấy đề bài code thành công", Data = exercise });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Error = ex.Message });
            }
        }

        // POST: api/assessments/self-declare
        [HttpPost("self-declare")]
        public async Task<IActionResult> SelfDeclareSkills([FromBody] SelfDeclareSkillDto request)
        {
            if (request == null || request.StudentId == Guid.Empty || request.AcquiredSkillNodeIds == null)
            {
                return BadRequest(new { Error = "Dữ liệu khai báo không hợp lệ." });
            }

            try
            {
                await _assessmentService.SaveSelfDeclaredSkillsAsync(request.StudentId, request.AcquiredSkillNodeIds);
                await _roadmapEngineService.RecalculateRoadmapAsync(request.StudentId);

                return Ok(new { Message = $"Đã ghi nhận {request.AcquiredSkillNodeIds.Count} kỹ năng. Lộ trình đang được cập nhật lại!" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Error = "Lỗi hệ thống khi lưu kỹ năng tự khai báo", Detail = ex.Message });
            }
        }
    }
}