using UniConnect.Dtos;

namespace UniConnect.INterfface
{
    public interface ISubmissionService
    {
        Task<SubmissionDto> CreateSubmissionAsync(CreateSubmissionDto createSubmissionDto, string studentId);
        Task<List<SubmissionDto>> GetSubmissionsByTaskAsync(string taskId, string teacherId);
        Task<SubmissionDto?> GetSubmissionByIdAsync(string id, string userId, bool isTeacher);
        Task<SubmissionDto> UpdateSubmissionAsync(string id, UpdateSubmissionDto updateSubmissionDto, string studentId);
        Task<SubmissionDto> GradeSubmissionAsync(string id, GradeSubmissionDto gradeDto, string teacherId);
    }
}
