namespace Service;

public enum Role
{
    User,
    SuperUser,
    Admin
}

/// The single place that defines what each role may do.
///
///   User      - read users
///   SuperUser - create, read, update, delete users
///   Admin     - create, read, update, delete users, superusers and admins
///
/// "Users" means accounts with the role User. The API, the service layer and
/// the UI (via /api/auth/me) all derive their behavior from these methods
public static class RolePolicy
{
    public static IReadOnlyList<Role> Readable(Role actor) => actor switch
    {
        Role.Admin => [Role.User, Role.SuperUser, Role.Admin],
        _ => [Role.User]
    };

    public static IReadOnlyList<Role> Manageable(Role actor) => actor switch
    {
        Role.Admin => [Role.User, Role.SuperUser, Role.Admin],
        Role.SuperUser => [Role.User],
        _ => []
    };

    public static bool CanRead(Role actor, Role target) => Readable(actor).Contains(target);

    public static bool CanManage(Role actor, Role target) => Manageable(actor).Contains(target);

    public static bool TryParse(string? value, out Role role) =>
        Enum.TryParse(value, ignoreCase: false, out role) && Enum.IsDefined(role) && !int.TryParse(value, out _);
}
