using UniConnect.Dtos;

namespace UniConnect.INterfface
{
    public interface IAcademicService
    {
        // Faculty methods
        Task<List<FacultyDto>> GetAllFacultiesAsync();
        Task<List<FacultyDto>> GetFacultiesByDeanAsync(string deanId);
        Task<FacultyDto> GetFacultyByIdAsync(string id);
        Task<FacultyDto> CreateFacultyAsync(CreateFacultyDto createFacultyDto);
        Task<FacultyDto> UpdateFacultyAsync(string id, CreateFacultyDto updateFacultyDto);
        Task<bool> DeleteFacultyAsync(string id);

        // Course methods
        Task<List<CourseDto>> GetAllCoursesAsync();
        Task<List<CourseDto>> GetCoursesByDeanAsync(string deanId);
        Task<List<CourseDto>> GetCoursesByFacultyAsync(string facultyId);
        Task<CourseDto> GetCourseByIdAsync(string id);
        Task<CourseDto> CreateCourseAsync(CreateCourseDto createCourseDto);
        Task<CourseDto> UpdateCourseAsync(string id, CreateCourseDto updateCourseDto);
        Task<bool> DeleteCourseAsync(string id);

        // StudentGroup methods
        Task<List<StudentGroupDto>> GetAllGroupsAsync();
        Task<List<StudentGroupDto>> GetGroupsByDeanAsync(string deanId);
        Task<List<StudentGroupDto>> GetGroupsByCourseAsync(string courseId);
        Task<StudentGroupDto> GetGroupByIdAsync(string id);
        Task<StudentGroupDto> CreateGroupAsync(CreateStudentGroupDto createGroupDto);
        Task<StudentGroupDto> UpdateGroupAsync(string id, CreateStudentGroupDto updateGroupDto);
        Task<bool> DeleteGroupAsync(string id);

        // Subject methods
        Task<SubjectDto> CreateSubjectAsync(CreateSubjectDto createSubjectDto);
        Task<SubjectDto> GetSubjectByIdAsync(string id);
        Task<List<SubjectDto>> GetSubjectsByGroupAsync(string studentGroupId);
        Task<SubjectDto> JoinSubjectByCodeAsync(string userId, string code);

        // Helper methods for dropdowns
        Task<List<FacultyDto>> GetActiveFacultiesAsync();
        Task<List<CourseDto>> GetCoursesForDropdownAsync();
        Task<List<StudentGroupDto>> GetGroupsForDropdownAsync();
    }
}
