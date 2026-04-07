using UniConnect.Dtos;

namespace UniConnect.INterfface
{
    public interface ITaskService
    {
        Task<TaskDto> CreateTaskAsync(CreateTaskDto createTaskDto, string teacherId);
        Task<List<TaskDto>> GetTasksBySubjectAsync(string subjectId, string userId);
        Task<TaskDto?> GetTaskByIdAsync(string id, string userId);
        Task<TaskDto> UpdateTaskAsync(string id, UpdateTaskDto updateTaskDto, string teacherId);
        Task<bool> DeleteTaskAsync(string id, string teacherId);
    }
}
