# In-client test registration

The login dialog now has Login and Register tabs. The active tab is disabled. Registration has username, email, password and confirmation fields, plus Submit and Cancel. Cancel returns to Login; successful registration keeps the username and clears both passwords. Passwords are not saved by registration.

While Register is open, the game-server connection is deliberately suspended so its unauthenticated login timer cannot expire. The form stays open through late disconnect/error events. Returning to Login reconnects through the connect server and requests the previously selected game server. Registration HTTP failures stay on the form and can be retried.

## VPS setup (not deployed)

1. Deploy the updated OpenMU server build containing the AdminPanel API and middleware changes. The existing administrator-only `POST /api/accounts` remains protected.
2. Serve the API over HTTPS with a valid certificate. A website registration page is not required. If TLS terminates at a reverse proxy, configure trusted forwarded headers so ASP.NET sees the original HTTPS scheme and client IP. Only trust your proxy addresses.
3. In the server host configuration, enable `TestRegistration:Enabled` (or set the environment variable `TestRegistration__Enabled=true`) and restart the host. Without this setting the endpoint returns 404.
4. In the client's `MuOnlineSettings` configuration, set `RegistrationUrl` to the full address, for example `https://YOUR-HOST/api/accounts/test-registration`. Leave it empty until HTTPS is ready. For desktop local configuration this can go in `appsettings.local.json` beside the running build; for mobile set the bundled client configuration before building.
5. Create a disposable test account in the client, then log in with it. No administrator API key belongs in the client.

Example client override:

```json
{
  "MuOnlineSettings": {
    "RegistrationUrl": "https://YOUR-HOST/api/accounts/test-registration"
  }
}
```

The test endpoint permits five requests per IP per hour and queues none. Duplicate usernames return 409; invalid fields return 400; rate limits return 429. Usernames are 3–10 printable ASCII characters without spaces; passwords are 3–20 printable ASCII characters. Email is required. The server stores a BCrypt password hash. The normal game connection and this HTTPS endpoint use different ports/protocols; the connect-server address cannot be used as the registration URL.

Disable `TestRegistration:Enabled` when direct test registration is no longer wanted. Deployment, certificate setup and live VPS account creation remain pending.
