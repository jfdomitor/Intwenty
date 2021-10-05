05. Apply database updates from earlier versions if needed
10. Update intwenty with nuget to v1.7.7
20. Review and update appsettings.json if needed
30. Make sure that v1.7.7 of wwwroot/js/intwenty.js is used
40. Review program.cs and update Polices:
- Remove old policy setting with:

 services.AddAuthorization(options =>
{
    options.AddPolicy("IntwentyAppAuthorizationPolicy", policy =>
    {
        //Anonymus = policy.AddRequirements(new IntwentyAllowAnonymousAuthorization());
        policy.RequireRole(IntwentyRoles.UserRoles);
    });

    options.AddPolicy("IntwentySystemAdminAuthorizationPolicy", policy =>
    {
        policy.RequireRole(IntwentyRoles.AdminRoles);

    });

});