import NextAuth from "next-auth";
import MicrosoftEntraID from "next-auth/providers/microsoft-entra-id";

interface RefreshedTokens {
  access_token: string;
  refresh_token?: string;
  expires_in: number;
}

async function refreshApiAccessToken(refreshToken: string): Promise<RefreshedTokens> {
  // Computed lazily (not at module scope) so this file can be imported -- e.g. during
  // `next build`'s page-data collection -- without AUTH_MICROSOFT_ENTRA_ID_ISSUER needing
  // to be present at build time, only at request time. Auth.js issuer is the
  // tenant-specific v2.0 authority (no trailing slash, see AUTH_MICROSOFT_ENTRA_ID_ISSUER:
  // ".../<tenant>/v2.0"). The OAuth2 token endpoint is ".../<tenant>/oauth2/v2.0/token" --
  // strip the issuer's own "/v2.0" suffix before appending, or this doubles up into
  // ".../v2.0/oauth2/v2.0/token" and 404s.
  const tokenEndpoint = `${process.env.AUTH_MICROSOFT_ENTRA_ID_ISSUER!.replace(/\/v2\.0\/?$/, "")}/oauth2/v2.0/token`;

  const response = await fetch(tokenEndpoint, {
    method: "POST",
    headers: { "Content-Type": "application/x-www-form-urlencoded" },
    body: new URLSearchParams({
      client_id: process.env.AUTH_MICROSOFT_ENTRA_ID_ID!,
      client_secret: process.env.AUTH_MICROSOFT_ENTRA_ID_SECRET!,
      grant_type: "refresh_token",
      refresh_token: refreshToken,
      scope: `openid profile email offline_access ${process.env.ENTRA_API_SCOPE}`,
    }),
  });

  if (!response.ok) {
    throw new Error(`Entra token refresh failed: ${response.status}`);
  }

  return (await response.json()) as RefreshedTokens;
}

export const { handlers, auth, signIn, signOut } = NextAuth({
  // Explicit because the callbacks below assume the full token lives in the
  // encrypted JWT cookie, not a database session.
  session: { strategy: "jwt" },
  providers: [
    MicrosoftEntraID({
      clientId: process.env.AUTH_MICROSOFT_ENTRA_ID_ID!,
      clientSecret: process.env.AUTH_MICROSOFT_ENTRA_ID_SECRET!,
      issuer: process.env.AUTH_MICROSOFT_ENTRA_ID_ISSUER!,
      authorization: {
        params: {
          scope:
            "openid profile email offline_access " +
            process.env.ENTRA_API_SCOPE,
        },
      },
    }),
  ],
  callbacks: {
    async jwt({ token, account }) {
      if (account) {
        // Initial sign-in: account.access_token is the token for ENTRA_API_SCOPE,
        // since that's the only non-OIDC-reserved scope requested.
        token.apiAccessToken = account.access_token;
        token.apiRefreshToken = account.refresh_token;
        token.apiAccessTokenExpiresAt = account.expires_at;
        delete token.apiAccessTokenError;
        return token;
      }

      const expiresAt = token.apiAccessTokenExpiresAt as number | undefined;
      if (expiresAt && Date.now() < expiresAt * 1000 - 60_000) {
        // Still valid, with a 60s safety margin.
        return token;
      }

      if (!token.apiRefreshToken) {
        return token;
      }

      try {
        const refreshed = await refreshApiAccessToken(token.apiRefreshToken as string);
        token.apiAccessToken = refreshed.access_token;
        token.apiAccessTokenExpiresAt = Math.floor(Date.now() / 1000) + refreshed.expires_in;
        // Entra doesn't always rotate the refresh token, keep the old one if so.
        if (refreshed.refresh_token) {
          token.apiRefreshToken = refreshed.refresh_token;
        }
        delete token.apiAccessTokenError;
      } catch (error) {
        console.error("Failed to refresh Entra API access token", error);
        // Also drop the refresh token: proxy.ts runs this callback on every
        // navigation, and a dead refresh token would otherwise trigger a
        // failed network call to Microsoft on every single page load.
        delete token.apiAccessToken;
        delete token.apiAccessTokenExpiresAt;
        delete token.apiRefreshToken;
        token.apiAccessTokenError = "RefreshFailed";
      }

      return token;
    },
    async session({ session }) {
      // Deliberately do not copy apiAccessToken/apiRefreshToken from the JWT
      // onto the session: this object is served verbatim to the browser via
      // /api/auth/session and useSession(). Server code must read the token
      // via getApiAccessToken() (lib/auth-token.ts), which decrypts the
      // session cookie directly instead of going through this callback.
      return session;
    },
  },
});

declare module "next-auth/jwt" {
  interface JWT {
    apiAccessToken?: string;
    apiRefreshToken?: string;
    apiAccessTokenExpiresAt?: number;
    apiAccessTokenError?: "RefreshFailed";
  }
}
