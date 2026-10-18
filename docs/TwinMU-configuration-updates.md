# Applying the Flame repair on an existing server

Flame and its master variants use a ground-centered 1.5-tile radius. Every valid
enemy in that circle is eligible, including separate enemies on the same tile.
Normal attack accuracy and safe-zone restrictions still apply. The configuration
repair changes only Flame when upgrading an installation which already has
version 3 or 4 of the area-skill update.

Rebuilding the server does not update persisted skill definitions. After pulling
and rebuilding this source on the VPS, add this argument to the usual startup
command for one run:

```text
--ApplyConfigurationUpdate=8890E745-BF72-40C4-BD90-D2D637AEDF99
```

For example, from the published server directory:

```sh
dotnet MUnique.OpenMU.Startup.dll --ApplyConfigurationUpdate=8890E745-BF72-40C4-BD90-D2D637AEDF99
```

Keep any other arguments normally used by your deployment. The update is applied
before game servers start, using the existing database connection configuration.
It can also be applied through Admin Panel → Updates → Add Area Skill Settings.
The current update version is 5. Repeating the startup argument after successful
application is harmless; the server reports that it is already installed.

The helper fee remains server-authoritative: the default first-stage charge is
20 × total character level at startup and every five minutes. Higher stages and
timings remain configurable in the MU Helper feature plugin. TwinMU displays the
server-reported accumulated cost; it never subtracts a guessed client-side fee.
