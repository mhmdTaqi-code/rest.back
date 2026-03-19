using SmartDiningSystem.Domain.Entities;

namespace SmartDiningSystem.Infrastructure.Services;

internal static class AdminAccountVisibility
{
    public static IQueryable<UserAccount> VisibleToAdminUi(this IQueryable<UserAccount> query)
    {
        return query.Where(account => account.Id != AdminAuthenticationService.DevelopmentAdminId);
    }
}
