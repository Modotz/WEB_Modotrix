namespace ERP.Web.Models;

public class LoginResultViewModel
{
    public string Token { get; set; } = string.Empty;

    public DateTimeOffset ExpiresAt { get; set; }

    public AuthUserViewModel User { get; set; } = new();
}

public class AuthUserViewModel
{
    public long Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Username { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public long RoleId { get; set; }

    public string? RoleName { get; set; }

    public string? Photo { get; set; }

    public bool IsAdmin { get; set; }
}

public class DevUserViewModel
{
    public string Username { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string? RoleName { get; set; }
}

public class MenuPermissionViewModel
{
    public bool View { get; set; }

    public bool Create { get; set; }

    public bool Update { get; set; }

    public bool Delete { get; set; }
}

public class SidebarMenuViewModel
{
    public long Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string? TitleEn { get; set; }

    public string? Url { get; set; }

    public string? Icon { get; set; }

    public List<SidebarMenuViewModel> Children { get; set; } = new();
}

public class UserAccessViewModel
{
    public AuthUserViewModel User { get; set; } = new();

    public bool IsAdmin { get; set; }

    public Dictionary<string, MenuPermissionViewModel> Permissions { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    public List<SidebarMenuViewModel> Sidebar { get; set; } = new();
}

public class AccessRoleMatrixViewModel
{
    public long RoleId { get; set; }

    public string RoleUid { get; set; } = string.Empty;

    public bool IsAdminRole { get; set; }

    public List<AccessRoleRoleViewModel> Roles { get; set; } = new();

    public List<AccessRoleMenuViewModel> Menus { get; set; } = new();
}

public class AccessRoleRoleViewModel
{
    public long Id { get; set; }

    public string Uid { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;
}

public class AccessRoleMenuViewModel
{
    public long Id { get; set; }

    public long? ParentId { get; set; }

    public string Title { get; set; } = string.Empty;

    public string? Url { get; set; }

    public string? Icon { get; set; }

    public bool CanView { get; set; }

    public bool CanCreate { get; set; }

    public bool CanUpdate { get; set; }

    public bool CanDelete { get; set; }
}

public class SaveAccessRoleViewModel
{
    public Guid RoleUid { get; set; }

    public List<SaveAccessRoleItemViewModel> Items { get; set; } = new();
}

public class SaveAccessRoleItemViewModel
{
    public long MenuId { get; set; }

    public bool CanView { get; set; }

    public bool CanCreate { get; set; }

    public bool CanUpdate { get; set; }

    public bool CanDelete { get; set; }
}
