using UniConnect.Dtos;

namespace UniConnect.INterfface
{
    public interface ICommunityService
    {
        // Community CRUD - REMOVE CreateCommunityAsync for academic types
        Task<List<CommunityDto>> GetAllCommunitiesAsync();
        Task<CommunityDto> GetCommunityByIdAsync(string id);
        Task<CommunityDto> UpdateCommunityAsync(string id, UpdateCommunityDto updateCommunityDto);
        Task<bool> DeleteCommunityAsync(string id);

        // Community membership
        Task<bool> JoinCommunityAsync(string communityId, string userId);
        Task<bool> LeaveCommunityAsync(string communityId, string userId);
        Task<List<CommunityMemberDto>> GetCommunityMembersAsync(string communityId);

        // Automatic community creation for academic structures
        Task<CommunityDto> GetOrCreateFacultyCommunityAsync(string facultyId);
        Task<CommunityDto> GetOrCreateCourseCommunityAsync(string courseId);
        Task<CommunityDto> GetOrCreateGroupCommunityAsync(string groupId);

        // User-specific methods
        Task<List<CommunityDto>> GetUserCommunitiesAsync(string userEmail);

        // ONLY allow creation of Department communities manually
        Task<CommunityDto> CreateDepartmentCommunityAsync(CreateDepartmentCommunityDto createDepartmentDto);

    }
}

