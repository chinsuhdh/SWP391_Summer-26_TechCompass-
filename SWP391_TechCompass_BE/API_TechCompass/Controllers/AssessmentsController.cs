using Microsoft.AspNetCore.Mvc;
using System;
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

        public AssessmentsController(IAssessmentService assessmentService)
        {
            _assessmentService = assessmentService;
        }

        // Endpoint 1: Lấy đề thi trắc nghiệm
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

        // Endpoint 2: Nộp bài và chấm điểm
        // POST: api/assessments/submit
        [HttpPost("submit")]
        public async Task<IActionResult> SubmitQuiz([FromBody] QuizSubmissionDto submission)
        {
            if (submission == null || submission.Answers == null)
            {
                return BadRequest("Dữ liệu nộp bài không hợp lệ.");
            }

            try
            {
                var result = await _assessmentService.GradeAndSaveQuizAsync(submission);

                return Ok(new
                {
                    Message = "Chấm điểm thành công",
                    Score = result.TestScore,
                    Feedback = result.AiFeedback
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Error = ex.Message });
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
                // Ví dụ: tags = "Docker", skillNodeId = 5 (ID của node Docker trong DB của bạn)
                int count = await _quizSyncService.FetchAndSaveQuestionsAsync(skillNodeId, tags, limit);
                return Ok(new { Message = $"Đồng bộ thành công {count} câu hỏi vào Database." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Error = ex.Message });
            }
        }

        [HttpPost("submit-code")]
        public async Task<IActionResult> SubmitCodeTest([FromBody] CodeTestSubmissionDto submission)
        {
            if (string.IsNullOrEmpty(submission.SourceCode))
            {
                return BadRequest("Mã nguồn không được để trống.");
            }

            try
            {
                var result = await _assessmentService.GradeAndSaveCodeTestAsync(submission);

                return Ok(new
                {
                    Message = "Chấm bài và phân tích pattern thành công",
                    Score = result.TestScore,
                    AiReview = result.AiFeedback
                });
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
                // Gọi API lấy dữ liệu ĐỘNG 100% từ Database
                var nodes = await _assessmentService.GetAllSkillNodesAsync();

                return Ok(new { Message = "Lấy danh sách Skill Nodes thành công", Data = nodes });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Error = ex.Message });
            }
        }
    }
}