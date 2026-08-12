import "server-only";
import { headers } from "next/headers";
import { getToken } from "next-auth/jwt";

// Reads the encrypted session cookie directly, bypassing the `session`
// callback in auth.ts so this never travels the same path as the
// browser-visible /api/auth/session response.
export async function getApiAccessToken(): Promise<string | null> {
  // getToken() defaults secureCookie to false, which only matches how the sign-in
  // flow actually named the cookie (__Secure-authjs.session-token vs authjs.session-token)
  // when the app is served over plain http. Over https -- any tunnel, and production --
  // the names diverge and this silently returns null. AUTH_URL is the same source of
  // truth Auth.js itself uses to decide the cookie's protocol at sign-in time.
  const secureCookie = process.env.AUTH_URL?.startsWith("https://") ?? false;

  const token = await getToken({
    req: { headers: await headers() },
    secret: process.env.AUTH_SECRET!,
    secureCookie,
  });

  if (!token || token.apiAccessTokenError || !token.apiAccessToken) {
    return null;
  }

  return token.apiAccessToken;
}
