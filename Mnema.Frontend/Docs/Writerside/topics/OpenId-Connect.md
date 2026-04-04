# OpenID Connect

As detailed in the [](Overview.md) guide, Mnema relies exclusively on **OpenID Connect (OIDC)** for user management and authentication. If you decide to disable authentication everyone will log in as `User` and has all roles

Access control is governed by granular roles. Mnema expects these roles to be present in the `roles` claim at the root of the identity token. Any logical grouping or mapping of users to these permissions should be managed directly within your Identity Provider (IdP).

<note>
    Mnema does not manage internal groups. All authorization is derived from the roles passed by your IdP.
</note>

## Role Reference

The following roles must be assigned to users in your IdP to grant specific permissions within the application:

| Role                         | Description                                                       |
|------------------------------|-------------------------------------------------------------------|
| `ManagePages`                | Permission to manage, edit, and organize content pages.           |
| `ManageSettings`             | Permission to modify global server and application settings.      |
| `Subscriptions`              | Permission to create and manage monitored series. (Legacy naming) |
| `HangFire`                   | Grants access to the HangFire dashboard (@ /hangfire).            |
| `CreateDirectory`            | Permission to create new directories within the storage system.   |
| `ManageExternalConnections`  | Permission to configure and manage external service integrations. |

