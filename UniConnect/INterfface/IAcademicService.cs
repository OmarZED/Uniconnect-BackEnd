using UniConnect.Dtos;

namespace UniConnect.INterfface
{
    public interface IAcademicService
    {
        // Faculty methods
        Task<List<FacultyDto>> GetAllFacultiesAsync();
        Task<FacultyDto> GetFacultyByIdAsync(string id);
        Task<FacultyDto> CreateFacultyAsync(CreateFacultyDto createFacultyDto);
        Task<FacultyDto> UpdateFacultyAsync(string id, CreateFacultyDto updateFacultyDto);
        Task<bool> DeleteFacultyAsync(string id);

        // Course methods
        Task<List<CourseDto>> GetAllCoursesAsync();
        Task<List<CourseDto>> GetCoursesByFacultyAsync(string facultyId);
        Task<CourseDto> GetCourseByIdAsync(string id);
        Task<CourseDto> CreateCourseAsync(CreateCourseDto createCourseDto);
        Task<CourseDto> UpdateCourseAsync(string id, CreateCourseDto updateCourseDto);
        Task<bool> DeleteCourseAsync(string id);

        // StudentGroup methods
        Task<List<StudentGroupDto>> GetAllGroupsAsync();
        Task<List<StudentGroupDto>> GetGroupsByCourseAsync(string courseId);
        Task<StudentGroupDto> GetGroupByIdAsync(string id);
        Task<StudentGroupDto> CreateGroupAsync(CreateStudentGroupDto createGroupDto);
        Task<StudentGroupDto> UpdateGroupAsync(string id, CreateStudentGroupDto updateGroupDto);
        Task<bool> DeleteGroupAsync(string id);

        // Helper methods for dropdowns
        Task<List<FacultyDto>> GetActiveFacultiesAsync();
        Task<List<CourseDto>> GetCoursesForDropdownAsync();
        Task<List<StudentGroupDto>> GetGroupsForDropdownAsync();
    }
}
